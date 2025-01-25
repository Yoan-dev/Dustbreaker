using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Dustbreaker
{
	[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
	[UpdateBefore(typeof(ActionSystem))]
	public partial struct InteractionSystem : ISystem
	{
		private NativeQueue<ActionEvent> _actionQueue;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<ActionEvent>();
			_actionQueue = new NativeQueue<ActionEvent>(Allocator.Persistent);
		}

		[BurstCompile]
		public void OnDestroy(ref SystemState state)
		{
			_actionQueue.Dispose();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Dependency = new InteractionJob { ActionQueue = _actionQueue.AsParallelWriter() }.ScheduleParallel(state.Dependency);

			state.Dependency = new WriteQueueToBufferJob<ActionEvent>
			{
				Queue = _actionQueue,
				LookupEntity = SystemAPI.GetSingletonEntity<ActionEvent>(),
				BufferLookup = SystemAPI.GetBufferLookup<ActionEvent>(),
			}.Schedule(state.Dependency);
		}

		[BurstCompile]
		public partial struct InteractionJob : IJobEntity
		{
			public NativeQueue<ActionEvent>.ParallelWriter ActionQueue;

			public void Execute(Entity entity, ref InteractionController interactionController, EnabledRefRW<InteractionFlag> interactionFlag)
			{
				ActionQueue.Enqueue(new ActionEvent
				{
					Source = entity,
					Target = interactionController.Target,
					Action = interactionController.Interaction,
				});

				// consume
				interactionController.Interaction = Action.None;
				interactionFlag.ValueRW = false;
			}
		}
	}
}