using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class InteractableAuthoring : MonoBehaviour
	{
		public InteractableComponent Interactable;

		// TODO: usage enum
		[Header("Usage")]
		public bool Ladder;
		public bool Pilot;
		public bool Deliver;

		public class Baker : Baker<InteractableAuthoring>
		{
			public override void Bake(InteractableAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, authoring.Interactable);

				if (authoring.Interactable.HasAction(Action.Pick))
				{
					AddComponent<PickableTag>(entity);
				}

				if (authoring.Ladder)
				{
					AddComponent<ClimbableTag>(entity);
					AddComponent<TrackedParentComponent>(entity);
				}
				else if (authoring.Pilot)
				{
					AddComponent<PilotTag>(entity);
					AddComponent<TrackedParentComponent>(entity);
				}
				else if (authoring.Deliver)
				{
					AddComponent<DeliverTag>(entity);
				}
			}
		}
	}
}