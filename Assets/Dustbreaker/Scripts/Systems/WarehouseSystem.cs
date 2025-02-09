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
	[UpdateBefore(typeof(SwitchPhysicsBodySystem))]
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

			state.Dependency = new ConveyorStorageJob
			{
				CollisionWorld = collisionWorld,
				StoreQueue = _storeQueue.AsParallelWriter(),
				CollisionFilter = _collisionFilter
			}.ScheduleParallel(state.Dependency);


			state.Dependency = new StorageJob
			{
				StorageLookup = SystemAPI.GetComponentLookup<StorageComponent>(true),
				SwitchToKinematicLookup = SystemAPI.GetComponentLookup<SwitchToKinematicFlag>(),
				TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
				StoreQueue = _storeQueue,
			}.Schedule(state.Dependency);
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
				float3 impulse = transform.Forward() * conveyorBelt.Strength * -1f;

				DrawUtilities.DrawBox(transform.Position, transform.Rotation, conveyorBelt.Size, new float4(1f, 0f, 0f, 1f));

				NativeList<ColliderCastHit> outHits = new NativeList<ColliderCastHit>(Allocator.Temp);
				if (CollisionWorld.BoxCastAll(
					transform.Position,
					transform.Rotation, 
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
		private partial struct ConveyorStorageJob : IJobEntity
		{
			[ReadOnly] public CollisionWorld CollisionWorld;
			[WriteOnly] public NativeQueue<StoreEvent>.ParallelWriter StoreQueue;
			public CollisionFilter CollisionFilter;

			public void Execute(in ConveyorStorageComponent conveyorStorage, in LocalTransform localTransform, in LocationReference location)
			{
				// TODO: init transform values once
				LocalTransform transform = LocalTransform.FromMatrix(math.mul(localTransform.ToMatrix(), new float4x4(conveyorStorage.Rotation, conveyorStorage.Center)));

				DrawUtilities.DrawBox(transform.Position, transform.Rotation, conveyorStorage.Size, new float4(0f, 1f, 0f, 1f));

				NativeList<ColliderCastHit> outHits = new NativeList<ColliderCastHit>(Allocator.Temp);
				if (CollisionWorld.BoxCastAll(
					transform.Position,
					transform.Rotation,
					conveyorStorage.Size / 2f,
					new float3(0f, 1f, 0f),
					0.1f,
					ref outHits,
					CollisionFilter))
				{
					for (int i = 0; i < outHits.Length; i++)
					{
						StoreQueue.Enqueue(new StoreEvent { Entity = outHits[i].Entity, Location = location.Entity });
					}
				}
				outHits.Dispose();
			}
		}

		[BurstCompile]
		private partial struct StorageJob : IJob
		{
			[ReadOnly] public ComponentLookup<StorageComponent> StorageLookup;
			public ComponentLookup<SwitchToKinematicFlag> SwitchToKinematicLookup;
			public ComponentLookup<LocalTransform> TransformLookup;
			public NativeQueue<StoreEvent> StoreQueue;

			public void Execute()
			{
				while (StoreQueue.Count > 0)
				{
					StoreEvent storeEvent = StoreQueue.Dequeue();

					if (StorageLookup.TryGetComponent(storeEvent.Location, out StorageComponent storage))
					{
						// TODO/TBD: switch to no physics as well
						SwitchToKinematicLookup.SetComponentEnabled(storeEvent.Entity, true);	
						
						LocalTransform transform = TransformLookup[storeEvent.Entity];
						transform.Position = storage.Position;
						TransformLookup[storeEvent.Entity] = transform;
					}
				}
			}
		}
	}
}