using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
			NativeArray<ActionEvent> actionEvents = SystemAPI.GetSingletonBuffer<ActionEvent>().AsNativeArray();

			for (int i = 0; i < actionEvents.Length; i++)
			{
				ActionEvent actionEvent = actionEvents[i];

				if (actionEvent.Action == Action.Pick)
				{
					// TODO: set carried item render in front

					state.EntityManager.SetComponentData(actionEvent.Source, new CarryComponent { Entity = actionEvent.Target });
					state.EntityManager.SetComponentData(actionEvent.Target, new PickableComponent { Entity = actionEvent.Source });
					state.EntityManager.SetComponentEnabled<CarryComponent>(actionEvent.Source, true);
					state.EntityManager.SetComponentEnabled<PickableComponent>(actionEvent.Target, true);

					PhysicsMass mass = state.EntityManager.GetComponentData<PhysicsMass>(actionEvent.Target);

					// create physics cache
					state.EntityManager.AddComponentData(actionEvent.Target, new CachedPhysicsProperties
					{
						PhysicsCollider = state.EntityManager.GetComponentData<PhysicsCollider>(actionEvent.Target),
						InverseInertia = mass.InverseInertia,
						InverseMass = mass.InverseMass,
					});

					// set kinematic
					mass.InverseMass = 0f;
					mass.InverseInertia = float3.zero;
					state.EntityManager.SetComponentData(actionEvent.Target, mass);

					// stop physics
					state.EntityManager.SetComponentData(actionEvent.Target, new PhysicsVelocity());
					state.EntityManager.RemoveComponent<PhysicsCollider>(actionEvent.Target);

					// parenting
					state.EntityManager.GetBuffer<Child>(actionEvent.Source).Add(new Child { Value = actionEvent.Target });
					state.EntityManager.AddComponentData(actionEvent.Target, new Parent { Value = actionEvent.Source });

					// transform
					LocalTransform transform = state.EntityManager.GetComponentData<LocalTransform>(actionEvent.Target);
					transform.Position = new float3(0f, 1f, 0.75f);
					transform.Rotation = quaternion.identity;
					state.EntityManager.SetComponentData(actionEvent.Target, transform);
				}
				else if (actionEvent.Action == Action.Drop)
				{
					// TODO: find safe drop position

					state.EntityManager.SetComponentData(actionEvent.Source, new CarryComponent { Entity = Entity.Null });
					state.EntityManager.SetComponentData(actionEvent.Target, new PickableComponent { Entity = Entity.Null });
					state.EntityManager.SetComponentEnabled<CarryComponent>(actionEvent.Source, false);
					state.EntityManager.SetComponentEnabled<PickableComponent>(actionEvent.Target, false);

					// restore physics
					CachedPhysicsProperties properties = state.EntityManager.GetComponentData<CachedPhysicsProperties>(actionEvent.Target);
					state.EntityManager.AddComponentData(actionEvent.Target, properties.PhysicsCollider);

					// set dynamic
					PhysicsMass mass = state.EntityManager.GetComponentData<PhysicsMass>(actionEvent.Target);
					mass.InverseMass = properties.InverseMass;
					mass.InverseInertia = properties.InverseInertia;
					state.EntityManager.SetComponentData(actionEvent.Target, mass);

					// transfer velocity
					float3 characterVelocity = state.EntityManager.GetComponentData<KinematicCharacterBody>(actionEvent.Source).RelativeVelocity;
					state.EntityManager.SetComponentData(actionEvent.Target, new PhysicsVelocity { Linear = characterVelocity });

					// remove physics cache
					state.EntityManager.RemoveComponent<CachedPhysicsProperties>(actionEvent.Target);

					// parenting
					DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(actionEvent.Source);
					for (int j = 0; j < children.Length; j++)
					{
						if (children[j].Value == actionEvent.Target)
						{
							children.RemoveAt(j);
							break;
						}
					}
					state.EntityManager.RemoveComponent<Parent>(actionEvent.Target);
					float scale = state.EntityManager.GetComponentData<LocalTransform>(actionEvent.Target).Scale;

					// transform
					LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(actionEvent.Source);
					transform.Position += transform.Forward() * 0.75f + new float3(0f, 1f, 0f);
					transform.Scale = scale;
					state.EntityManager.SetComponentData(actionEvent.Target, transform);
				}
			}

			SystemAPI.GetSingletonBuffer<ActionEvent>().Clear();
		}
	}
}