using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dustbreaker
{
	[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
	[UpdateBefore(typeof(TerrainSystem))]
	public partial struct StreamingSystem : ISystem
	{
		private NativeQueue<StreamingEvent> _streamingQueue;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<StreamingConfig>();
			state.RequireForUpdate<StreamingEvent>();
			state.RequireForUpdate<PlayerTag>();

			Entity entity = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(entity, new QuadrantCollection(0));

			_streamingQueue = new NativeQueue<StreamingEvent>(Allocator.Persistent);
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{
			_streamingQueue.Dispose();
			SystemAPI.GetSingleton<QuadrantCollection>().Dispose();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			StreamingConfig config = SystemAPI.GetSingleton<StreamingConfig>();

			Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
			float2 playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.xz;

			state.Dependency = new StreamInJob
			{
				Quadrants = SystemAPI.GetSingleton<QuadrantCollection>().Map,
				StreamingQueue = _streamingQueue.AsParallelWriter(),
				Config = config,
				PlayerPosition = playerPosition,
			}.Schedule(state.Dependency);

			state.Dependency = new StreamOutJob
			{
				StreamingQueue = _streamingQueue.AsParallelWriter(),
				Config = config,
				PlayerPosition = playerPosition,
			}.Schedule(state.Dependency);

			state.Dependency = new WriteQueueToBufferJob<StreamingEvent>
			{
				Queue = _streamingQueue,
				LookupEntity = SystemAPI.GetSingletonEntity<StreamingEvent>(),
				BufferLookup = SystemAPI.GetBufferLookup<StreamingEvent>(),
			}.Schedule(state.Dependency);
		}

		[BurstCompile]
		public partial struct StreamInJob : IJob
		{
			[ReadOnly] public NativeParallelHashMap<int2, Entity> Quadrants;
			public NativeQueue<StreamingEvent>.ParallelWriter StreamingQueue;
			public StreamingConfig Config;
			public float2 PlayerPosition;

			public void Execute()
			{
				int2 playerCoordinates = (int2)(PlayerPosition / Config.QuadrantSize);
				int size = (int)(Config.StreamingRange / Config.QuadrantSize);

				for (int y = -size; y < size; y++)
				{
					for (int x = -size; x < size; x++)
					{
						int2 coordinates = new int2(playerCoordinates.x + x, playerCoordinates.y + y);
						float2 position = coordinates * Config.QuadrantSize;

						if (math.distancesq(PlayerPosition, position) <= Config.StreamingRange * Config.StreamingRange && !Quadrants.ContainsKey(coordinates))
						{
							StreamingQueue.Enqueue(new StreamingEvent { Coordinates = coordinates, Create = true });
						}
					}
				}
			}
		}

		[BurstCompile]
		public partial struct StreamOutJob : IJobEntity
		{
			public NativeQueue<StreamingEvent>.ParallelWriter StreamingQueue;
			public StreamingConfig Config;
			public float2 PlayerPosition;

			public void Execute(in Quadrant quadrant)
			{
				float2 position = new float2(quadrant.Coordinates.x * Config.QuadrantSize, quadrant.Coordinates.y * Config.QuadrantSize);
			
				if (math.distancesq(PlayerPosition, position) > Config.StreamingRange * Config.StreamingRange)
				{
					StreamingQueue.Enqueue(new StreamingEvent { Coordinates = quadrant.Coordinates, Create = false });
				}
			}
		}
	}
}