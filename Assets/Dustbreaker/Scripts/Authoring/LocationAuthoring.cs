using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

namespace Dustbreaker
{
	[Serializable]
	public struct SpawnPointManaged
	{
		public float3 Position;
		public float3 Rotation;
		public GameObject Prefab;
	}

	[DisallowMultipleComponent]
	public class LocationAuthoring : MonoBehaviour
	{
		public SpawnPointManaged[] SpawnAnchors = new SpawnPointManaged[0];
		public bool HasStorage;
		public float3 StoragePosition;

		public class Baker : Baker<LocationAuthoring>
		{
			public override void Bake(LocationAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<LocationTag>(entity);

				if (authoring.HasStorage)
				{
					AddComponent(entity, new StorageComponent { Position = authoring.StoragePosition });
				}

				DynamicBuffer<SpawnPoint> spawnPoints = AddBuffer<SpawnPoint>(entity);
				for (int i = 0; i < authoring.SpawnAnchors.Length; i++)
				{
					SpawnPointManaged spawnPoint = authoring.SpawnAnchors[i];
					spawnPoints.Add(new SpawnPoint
					{
						Rotation = quaternion.Euler(math.radians(spawnPoint.Rotation)),
						Position = spawnPoint.Position,
						Prefab = GetEntity(spawnPoint.Prefab, TransformUsageFlags.Dynamic),
					});
				}
			}
		}
	}
}