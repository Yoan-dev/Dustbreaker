using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class PlayerAuthoring : MonoBehaviour
	{
		public class Baker : Baker<PlayerAuthoring>
		{
			public override void Bake(PlayerAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<PlayerInputs>(entity);
				AddComponent<PlayerTag>(entity);

				// temp
				AddComponent<VehicleInputs>(entity);
			}
		}
	}
}