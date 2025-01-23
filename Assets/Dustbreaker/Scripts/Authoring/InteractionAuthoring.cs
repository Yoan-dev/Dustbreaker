using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class InteractionAuthoring : MonoBehaviour
	{
		public class Baker : Baker<InteractionAuthoring>
		{
			public override void Bake(InteractionAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<InteractionController>(entity);
				AddComponent<InteractionFlag>(entity);
				SetComponentEnabled<InteractionFlag>(entity, false);
			}
		}
	}
}