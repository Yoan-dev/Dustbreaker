using UnityEngine;
using Unity.Entities;

[DisallowMultipleComponent]
public class PlayerAuthoring : MonoBehaviour
{
	public class Baker : Baker<PlayerAuthoring>
	{
		public override void Bake(PlayerAuthoring authoring)
		{
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent<PlayerInputs>(entity);
		}
	}
}