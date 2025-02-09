using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Dustbreaker
{
	[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
	public partial struct WarehouseSystem : ISystem
	{
		private struct MoveEvent
		{
			public float3 Impulse;
			public Entity Entity;
		}

		private struct StoreEvent
		{
			public Entity Entity;
			public Entity Location;
		}

		private NativeQueue<MoveEvent> _moveQueue;
		private NativeQueue<StoreEvent> _storeQueue;
		private CollisionFilter _collisionFilter;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<PhysicsWorldSingleton>();

			_moveQueue = new NativeQueue<MoveEvent>(Allocator.Persistent);
			_storeQueue = new NativeQueue<StoreEvent>(Allocator.Persistent);

			_collisionFilter = new CollisionFilter
			{
				BelongsTo = 1 << 4, // Raycast
				CollidesWith = 1 << 2, // Item
			};
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{
			_moveQueue.Dispose();
			_storeQueue.Dispose();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

			state.Dependency = new ConveyorBeltJob
			{
				CollisionWorld = collisionWorld,
				MoveQueue = _moveQueue.AsParallelWriter(),
				CollisionFilter = _collisionFilter
			}.ScheduleParallel(state.Dependency);

			state.Dependency = new ConveyorMoveJob
			{
				MassLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true),
				VelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(),
				MoveQueue = _moveQueue,
				DeltaTime = SystemAPI.Time.DeltaTime,
			}.Schedule(state.Dependency);

			state.Dependency = new StorageJob
			{
				CollisionWorld = collisionWorld,
				StoreQueue = _storeQueue.AsParallelWriter(),
			}.ScheduleParallel(state.Dependency);

			// TODO: send to storage job (+ TBD event for mission validation / barter value)
		}

		[BurstCompile]
		private partial struct ConveyorBeltJob : IJobEntity
		{
			[ReadOnly] public CollisionWorld CollisionWorld;
			[WriteOnly] public NativeQueue<MoveEvent>.ParallelWriter MoveQueue;
			public CollisionFilter CollisionFilter;

			public void Execute(in ConveyorBeltComponent conveyorBelt, in LocalTransform localTransform)
			{
				// TODO: init transform values once
				LocalTransform transform = LocalTransform.FromMatrix(math.mul(localTransform.ToMatrix(), new float4x4(conveyorBelt.Rotation, conveyorBelt.Center)));
				quaternion rotation = transform.Rotation;
				float3 position = transform.Position;
				float3 impulse = transform.Forward() * conveyorBelt.Strength * -1f;

				DrawUtilities.DrawBox(position, rotation, conveyorBelt.Size, new float4(1f, 0f, 0f, 1f));

				NativeList<ColliderCastHit> outHits = new NativeList<ColliderCastHit>(Allocator.Temp);
				if (CollisionWorld.BoxCastAll(
					position,
					rotation, 
					conveyorBelt.Size / 2f, 
					new float3(0f, 1f, 0f),
					0.1f,
					ref outHits,
					CollisionFilter))
				{
					for (int i = 0; i < outHits.Length; i++)
					{
						DrawUtilities.DrawLine(outHits[i].Position, outHits[i].Position + impulse, new float4(1f, 0f, 0f, 1f));

						MoveQueue.Enqueue(new MoveEvent { Entity = outHits[i].Entity, Impulse = impulse });
					}
				}
				outHits.Dispose();
			}
		}

		[BurstCompile]
		private partial struct ConveyorMoveJob : IJob
		{
			[ReadOnly] public ComponentLookup<PhysicsMass> MassLookup;
			public ComponentLookup<PhysicsVelocity> VelocityLookup;
			public NativeQueue<MoveEvent> MoveQueue;
			public float DeltaTime;

			public void Execute()
			{
				while (MoveQueue.Count > 0)
				{
					MoveEvent moveEvent = MoveQueue.Dequeue();
					PhysicsVelocity velocity = VelocityLookup[moveEvent.Entity];
					velocity.ApplyLinearImpulse(MassLookup[moveEvent.Entity], moveEvent.Impulse * DeltaTime);
					VelocityLookup[moveEvent.Entity] = velocity;
				}
			}
		}

		[BurstCompile]
		private partial struct StorageJob : IJobEntity
		{
			[ReadOnly] public CollisionWorld CollisionWorld;
			[WriteOnly] public NativeQueue<StoreEvent>.ParallelWriter StoreQueue;

			public void Execute(in StorageComponent storage, in LocalTransform localTransform, in LocationReference location)
			{
				// TODO
				// TODO: debug cast box
			}
		}
	}
}