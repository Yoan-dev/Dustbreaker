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
			state.RequireForUpdate<PlayerTag>();
		}

		public void OnUpdate(ref SystemState state)
		{
			state.Dependency.Complete();

			Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
			InteractionController interactionController = SystemAPI.GetComponent<InteractionController>(playerEntity);

			Action primaryInteraction = Action.None;
			Action secondaryInteraction = Action.None;

			if (interactionController.Interaction == Action.None && interactionController.Target != Entity.Null)
			{
				InteractableComponent interactable = SystemAPI.GetComponent<InteractableComponent>(interactionController.Target);
				primaryInteraction = interactable.GetPrimaryInteraction();
				secondaryInteraction = interactable.GetSecondaryInteraction();
			}

			HUDController.Instance.UpdateInteraction(primaryInteraction, secondaryInteraction);

			bool missionsChanged = true; // temp
			if (missionsChanged)
			{
				HUDController.Instance.ClearMissions();
				foreach (var (mission, entity) in SystemAPI.Query<RefRO<MissionComponent>>().WithNone<SuccessTag>().WithEntityAccess())
				{
					HUDController.Instance.AddMission(entity, mission.ValueRO.Type, state.EntityManager);
				}
			}
		}
	}
}