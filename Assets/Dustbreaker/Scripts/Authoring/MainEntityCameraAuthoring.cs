using UnityEngine;
using Unity.Entities;

// based on Character Controller-Standard Characters (see: LICENSE)

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class MainEntityCameraAuthoring : MonoBehaviour
	{
		public class Baker : Baker<MainEntityCameraAuthoring>
		{
			public override void Bake(MainEntityCameraAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<MainEntityCameraTag>(entity);
			}
		}
	}
}