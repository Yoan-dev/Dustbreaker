using UnityEngine;
using Unity.Entities;

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