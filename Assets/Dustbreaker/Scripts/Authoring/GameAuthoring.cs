using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class GameAuthoring : MonoBehaviour
	{
		public GameConfig GameConfig;
		public StreamingConfig StreamingConfig;

		[Header("Prefabs")]
		public GameObject CharacterPrefab;
		public GameObject ColonyPrefab;
		public GameObject DustbreakerPrefab;
		public GameObject MainDeliveryPrefab;

		public class Baker : Baker<GameAuthoring>
		{
			public override void Bake(GameAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.None);
				AddComponent(entity, authoring.GameConfig);
				AddComponent(entity, authoring.StreamingConfig);
				AddComponent(entity, new GamePrefabs
				{
					CharacterPrefab = GetEntity(authoring.CharacterPrefab, TransformUsageFlags.Dynamic),
					ColonyPrefab = GetEntity(authoring.ColonyPrefab, TransformUsageFlags.Dynamic),
					DustbreakerPrefab = GetEntity(authoring.DustbreakerPrefab, TransformUsageFlags.Dynamic),
					MainDeliveryPrefab = GetEntity(authoring.MainDeliveryPrefab, TransformUsageFlags.Dynamic),
				});

				// event buffers
				AddBuffer<ActionEvent>(entity);
				AddBuffer<StreamingEvent>(entity);
			}
		}
	}
}