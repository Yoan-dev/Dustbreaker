using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class GameAuthoring : MonoBehaviour
	{
		public class Baker : Baker<GameAuthoring>
		{
			public override void Bake(GameAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.None);

				// event buffers
				AddBuffer<ActionEvent>(entity);
			}
		}
	}
}