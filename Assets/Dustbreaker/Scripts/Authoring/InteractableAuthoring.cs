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

		[Header("Usage")]
		public bool InLocation;

		public class Baker : Baker<InteractableAuthoring>
		{
			public override void Bake(InteractableAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, authoring.Interactable);

				if (authoring.Interactable.HasAction(Action.Pick))
				{
					AddComponent<PickableTag>(entity);
					AddComponent<CachedPhysicsCollider>(entity);
					AddComponent<CachedPhysicsMass>(entity);
					AddComponent<SwitchToKinematicFlag>(entity);
					AddComponent<SwitchToDynamicFlag>(entity);
					SetComponentEnabled<SwitchToKinematicFlag>(entity, false);
					SetComponentEnabled<SwitchToDynamicFlag>(entity, false);
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

				if (authoring.InLocation)
				{
					// add in authoring
					AddComponent<LocationReference>(entity);
				}
			}
		}
	}
}