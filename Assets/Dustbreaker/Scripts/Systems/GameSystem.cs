using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dustbreaker
{
	public partial struct GameSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<GameConfig>();
			state.RequireForUpdate<GamePrefabs>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			GameConfig config = SystemAPI.GetSingleton<GameConfig>();
			GamePrefabs prefabs = SystemAPI.GetSingleton<GamePrefabs>();

			float halfSize = config.TravelDistance / 2f;
			float3 startPosition = new float3(-halfSize, 0f, -halfSize);
			float3 endPosition = new float3(halfSize, 0f, halfSize);
			quaternion startRotation = quaternion.Euler(0f, math.radians(45f), 0f);
			quaternion endRotation = quaternion.Euler(0f, math.radians(45f), 0f);

			// main entities
			Entity character = state.EntityManager.Instantiate(prefabs.CharacterPrefab);
			Entity startingColony = state.EntityManager.Instantiate(prefabs.ColonyPrefab);
			Entity endColony = state.EntityManager.Instantiate(prefabs.ColonyPrefab);
			Entity dustbreaker = state.EntityManager.Instantiate(prefabs.DustbreakerPrefab);
			Entity mainDelivery = state.EntityManager.Instantiate(prefabs.MainDeliveryPrefab);
			state.EntityManager.SetComponentData(startingColony, LocalTransform.FromPositionRotation(startPosition, startRotation));
			state.EntityManager.SetComponentData(character, LocalTransform.FromPositionRotation(startPosition + new float3(0f, 1.55f, 0f), startRotation));
			state.EntityManager.SetComponentData(dustbreaker, LocalTransform.FromPositionRotation(startPosition + new float3(0f, 1.5f, 0f), startRotation));
			state.EntityManager.SetComponentData(mainDelivery, LocalTransform.FromPositionRotationScale(startPosition + new float3(2f, 1.9f, 1f), quaternion.identity, 0.5f));
			state.EntityManager.SetComponentData(endColony, LocalTransform.FromPositionRotation(endPosition, endRotation));

			// main mission
			Entity mainMission = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(mainDelivery, new MissionReference { Entity = mainMission });
			state.EntityManager.AddComponentData(mainMission, new ItemReference { Entity = mainDelivery });
			state.EntityManager.AddComponentData(mainMission, new LocationReference { Entity = endColony });
			state.EntityManager.AddComponent<MainMissionTag>(mainMission);

			state.Enabled = false;
		}
	}
}