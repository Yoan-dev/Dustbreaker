using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dustbreaker
{
	public partial struct InitLocationSystem : ISystem
	{
		private struct LocationSpawnEvent
		{
			public LocalTransform Transform;
			public Entity Location;
			public Entity Prefab;
		}

		private NativeQueue<LocationSpawnEvent> _spawnQueue;
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			_spawnQueue = new NativeQueue<LocationSpawnEvent>(Allocator.Persistent);
			_query = SystemAPI.QueryBuilder().WithAll<SpawnPoint>().Build();

			state.RequireForUpdate(_query);
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{
			_spawnQueue.Dispose();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Dependency = new StorageInitJob().ScheduleParallel(state.Dependency);

			state.Dependency = new SpawnPointsJob { SpawnQueue = _spawnQueue.AsParallelWriter() }.ScheduleParallel(state.Dependency);

			state.Dependency.Complete();

			while (_spawnQueue.Count > 0)
			{
				LocationSpawnEvent spawnEvent = _spawnQueue.Dequeue();
				Entity entity = state.EntityManager.Instantiate(spawnEvent.Prefab);
				state.EntityManager.SetComponentData(entity, spawnEvent.Transform);
				state.EntityManager.SetComponentData(entity, new LocationReference { Entity = spawnEvent.Location });
			}

			state.EntityManager.RemoveComponent<SpawnPoint>(_query);
		}

		// TODO: reassess using SpawnPoint as init flag (no spawn point location with storage ?)
		[BurstCompile]
		[WithAll(typeof(SpawnPoint))]
		private partial struct StorageInitJob : IJobEntity
		{
			public void Execute(ref StorageComponent storage, in LocalTransform localTransform)
			{
				storage.Position = localTransform.TransformPoint(storage.Position);
			}
		}

		[BurstCompile]
		private partial struct SpawnPointsJob : IJobEntity
		{
			[WriteOnly] public NativeQueue<LocationSpawnEvent>.ParallelWriter SpawnQueue;

			public void Execute(Entity entity, in DynamicBuffer<SpawnPoint> spawnPoints, in LocalTransform localTransform)
			{
				Matrix4x4 matrix = localTransform.ToMatrix();
				for (int i = 0; i < spawnPoints.Length; i++)
				{
					SpawnPoint spawnPoint = spawnPoints[i];
					SpawnQueue.Enqueue(new LocationSpawnEvent
					{
						Transform = LocalTransform.FromMatrix(math.mul(matrix, new float4x4(spawnPoint.Rotation, spawnPoint.Position))),
						Location = entity,
						Prefab = spawnPoint.Prefab,
					});
				}
			}
		}
	}
}