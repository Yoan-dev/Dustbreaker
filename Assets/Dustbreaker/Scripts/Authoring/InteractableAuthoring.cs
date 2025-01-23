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
			}
		}
	}
}