using Unity.Burst;
using Unity.Entities;

namespace Dustbreaker
{
	[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
	public partial struct InteractionSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Dependency = new InteractionJob { }.ScheduleParallel(state.Dependency);
		}
	}

    [BurstCompile]
    public partial struct InteractionJob : IJobEntity
	{
		public void Execute(ref InteractionController interactionController)
		{
			// TODO: trigger interaction

			// consume
			interactionController.Interaction = Action.None;
		}
    }
}