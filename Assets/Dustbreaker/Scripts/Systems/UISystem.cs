using Unity.Burst;
using Unity.Entities;

namespace Dustbreaker
{
	[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
	public partial struct UISystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<InteractionController>();
		}

		public void OnUpdate(ref SystemState state)
		{
			state.Dependency.Complete();

			// we assume there is only one interaction controller
			InteractionController interactionController = SystemAPI.GetSingleton<InteractionController>();

			Action primaryInteraction = Action.None;
			Action secondaryInteraction = Action.None;

			if (interactionController.Target != Entity.Null)
			{
				InteractableComponent interactable = SystemAPI.GetComponent<InteractableComponent>(interactionController.Target);
				primaryInteraction = interactable.GetPrimaryInteraction();
				secondaryInteraction = interactable.GetSecondaryInteraction();
			}

			HUDController.Instance.UpdateHUD(primaryInteraction, secondaryInteraction);
		}
	}
}