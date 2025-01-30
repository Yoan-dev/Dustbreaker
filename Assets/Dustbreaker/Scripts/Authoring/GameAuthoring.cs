using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class GameAuthoring : MonoBehaviour
	{
		public StreamingConfig StreamingConfig;

		public class Baker : Baker<GameAuthoring>
		{
			public override void Bake(GameAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.None);
				AddComponent(entity, authoring.StreamingConfig);

				// event buffers
				AddBuffer<ActionEvent>(entity);
				AddBuffer<StreamingEvent>(entity);
			}
		}
	}
}