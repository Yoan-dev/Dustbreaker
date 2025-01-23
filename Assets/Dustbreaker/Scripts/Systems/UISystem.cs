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
			// we assume there is only one interaction controller
			InteractionController interactionController = SystemAPI.GetSingleton<InteractionController>();

			Action firstInteraction = Action.None;
			Action secondInteraction = Action.None;

			if (interactionController.Target != Entity.Null)
			{
				InteractableComponent interactable = SystemAPI.GetComponent<InteractableComponent>(interactionController.Target);
				firstInteraction = interactable.GetFirstInteraction();
				secondInteraction = interactable.GetSecondInteraction();
			}

			HUDController.Instance.UpdateHUD(firstInteraction, secondInteraction);
		}
	}
}