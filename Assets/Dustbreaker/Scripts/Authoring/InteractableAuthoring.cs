using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class InteractableAuthoring : MonoBehaviour
	{
		public InteractableComponent Interactable;
		public bool Ladder;
		public bool Pilot;

		public class Baker : Baker<InteractableAuthoring>
		{
			public override void Bake(InteractableAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, authoring.Interactable);

				if (authoring.Interactable.HasAction(Action.Pick))
				{
					AddComponent<PickableComponent>(entity);
					SetComponentEnabled<PickableComponent>(entity, false);
				}

				if (authoring.Ladder)
				{
					AddComponent<ClimbableTag>(entity);
					AddComponent<TrackedParentComponent>(entity);
				}
				else if (authoring.Pilot)
				{
					//AddComponent<PilotComponent>(entity);
					//AddComponent<TrackedParentComponent>(entity);
				}
			}
		}
	}
}