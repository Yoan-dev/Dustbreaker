using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class InteractableAuthoring : MonoBehaviour
	{
		public InteractableComponent Interactable;

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
			}
		}
	}
}