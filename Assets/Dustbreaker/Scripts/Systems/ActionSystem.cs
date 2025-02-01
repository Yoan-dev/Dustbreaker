using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace Dustbreaker
{
	[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
	public partial struct ActionSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<ActionEvent>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			NativeArray<ActionEvent> actionEvents = SystemAPI.GetSingletonBuffer<ActionEvent>().ToNativeArray(Allocator.Temp);

			for (int i = 0; i < actionEvents.Length; i++)
			{
				ActionEvent actionEvent = actionEvents[i];

				if (actionEvent.Action == Action.Use)
				{
					if (SystemAPI.HasComponent<ClimbableTag>(actionEvent.Target))
					{
						Attach(actionEvent.Source, actionEvent.Target, ref state);
					}
				}
				else if (actionEvent.Action == Action.Stop)
				{
					Stop(actionEvent.Source, ref state);
				}
				else if (actionEvent.Action == Action.Pick)
				{
					Pick(actionEvent.Source, actionEvent.Target, ref state);
				}
				else if (actionEvent.Action == Action.Drop)
				{
					Drop(actionEvent.Source, actionEvent.Target, ref state);
				}
			}

			actionEvents.Dispose();
			SystemAPI.GetSingletonBuffer<ActionEvent>().Clear();
		}

		public void Pick(Entity source, Entity target, ref SystemState state)
		{
			// TODO: prevent picking an item if already carrying one
			// TODO: set carried item render in front

			state.EntityManager.SetComponentData(source, new CarryComponent { Entity = target });
			state.EntityManager.SetComponentEnabled<CarryComponent>(source, true);

			PhysicsMass mass = state.EntityManager.GetComponentData<PhysicsMass>(target);

			// create physics cache
			state.EntityManager.AddComponentData(target, new CachedPhysicsProperties
			{
				PhysicsCollider = state.EntityManager.GetComponentData<PhysicsCollider>(target),
				InverseInertia = mass.InverseInertia,
				InverseMass = mass.InverseMass,
			});

			// set kinematic
			mass.InverseMass = 0f;
			mass.InverseInertia = float3.zero;
			state.EntityManager.SetComponentData(target, mass);

			// stop physics
			state.EntityManager.SetComponentData(target, new PhysicsVelocity());
			state.EntityManager.RemoveComponent<PhysicsCollider>(target);
			state.EntityManager.RemoveComponent<PhysicsGraphicalSmoothing>(target); // temp

			// parenting
			state.EntityManager.GetBuffer<Child>(source).Add(new Child { Value = target });
			state.EntityManager.AddComponentData(target, new Parent { Value = source });

			// transform
			LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(target);
			transform.Position = new float3(0f, 1f, 0.75f);
			transform.Rotation = quaternion.identity;
			state.EntityManager.SetComponentData(target, transform);
		}

		public void Drop(Entity source, Entity target, ref SystemState state)
		{
			// TODO: find safe drop position
			// TODO: fix flicker on reactivate smoothing

			state.EntityManager.SetComponentData(source, new CarryComponent { Entity = Entity.Null });
			state.EntityManager.SetComponentEnabled<CarryComponent>(source, false);

			// restore physics
			CachedPhysicsProperties properties = state.EntityManager.GetComponentData<CachedPhysicsProperties>(target);
			state.EntityManager.AddComponentData(target, properties.PhysicsCollider);
			state.EntityManager.AddComponentData(target, new PhysicsGraphicalSmoothing { ApplySmoothing = 1 }); // temp

			// set dynamic
			PhysicsMass mass = state.EntityManager.GetComponentData<PhysicsMass>(target);
			mass.InverseMass = properties.InverseMass;
			mass.InverseInertia = properties.InverseInertia;
			state.EntityManager.SetComponentData(target, mass);

			// transfer velocity
			KinematicCharacterBody characterBody = state.EntityManager.GetComponentData<KinematicCharacterBody>(source);
			float3 characterVelocity = characterBody.RelativeVelocity + characterBody.ParentVelocity;
			state.EntityManager.SetComponentData(target, new PhysicsVelocity { Linear = characterVelocity });

			// remove physics cache
			state.EntityManager.RemoveComponent<CachedPhysicsProperties>(target);

			// parenting
			DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(source);
			for (int j = 0; j < children.Length; j++)
			{
				if (children[j].Value == target)
				{
					children.RemoveAt(j);
					break;
				}
			}
			state.EntityManager.RemoveComponent<Parent>(target);
			float scale = state.EntityManager.GetComponentData<LocalTransform>(target).Scale;

			// transform
			LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(source);
			transform.Position += transform.Forward() * 0.75f + new float3(0f, 1f, 0f);
			transform.Scale = scale;
			state.EntityManager.SetComponentData(target, transform);
		}

		public void Attach(Entity source, Entity target, ref SystemState state)
		{
			ref KinematicCharacterProperties characterProperties = ref SystemAPI.GetComponentRW<KinematicCharacterProperties>(source).ValueRW;
			ref KinematicCharacterBody characterBody = ref SystemAPI.GetComponentRW<KinematicCharacterBody>(source).ValueRW;
			characterProperties.EvaluateGrounding = false;
			characterProperties.DetectMovementCollisions = false;
			characterProperties.DecollideFromOverlaps = false;
			characterBody.IsGrounded = false;

			state.EntityManager.SetComponentData(source, new AttachedComponent { Target = target });
			state.EntityManager.SetComponentEnabled<AttachedFlag>(source, true);
		}

		public void Stop(Entity source, ref SystemState state)
		{
			// detach from ladder/pilot/else
			if (state.EntityManager.IsComponentEnabled<AttachedFlag>(source))
			{
				ref KinematicCharacterProperties characterProperties = ref SystemAPI.GetComponentRW<KinematicCharacterProperties>(source).ValueRW;
				characterProperties.EvaluateGrounding = true;
				characterProperties.DetectMovementCollisions = true;
				characterProperties.DecollideFromOverlaps = true;

				state.EntityManager.SetComponentEnabled<AttachedFlag>(source, false);
			}
		}
	}
}