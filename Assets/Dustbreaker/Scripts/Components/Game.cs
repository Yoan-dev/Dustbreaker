using System;
using Unity.Entities;

namespace Dustbreaker
{
	[Serializable]
	public struct GameConfig : IComponentData
	{
		public float TravelDistance;
	}

	public struct GamePrefabs : IComponentData
	{
		public Entity CharacterPrefab;
		public Entity ColonyPrefab;
		public Entity DustbreakerPrefab;
		public Entity MainDeliveryPrefab;
	}
}