using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class ConveyorAuthoring : MonoBehaviour
	{
		[Header("Conveyor Belt")]
		public bool IsConveyorBelt;
		public float3 BeltCenter;
		public float3 BeltRotation;
		public float3 BeltSize;
		public float BeltStrength;

		[Header("Storage")]
		public bool IsStorage;
		public float3 StorageCenter;
		public float3 StorageRotation;
		public float3 StorageSize;

		[Header("Item Spawner")]
		public bool IsItemSpawner;
		public float3 ItemSpawnerPosition;
		public float3 ItemSpawnerRotation;

		public class Baker : Baker<ConveyorAuthoring>
		{
			public override void Bake(ConveyorAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<LocationReference>(entity);

				if (authoring.IsConveyorBelt)
				{
					AddComponent(entity,new ConveyorBeltComponent
					{
						Center = authoring.BeltCenter,
						Rotation = quaternion.Euler(math.radians(authoring.BeltRotation)),
						Size = authoring.BeltSize,
						Strength = authoring.BeltStrength,
					});
				}

				if (authoring.IsStorage)
				{
					AddComponent(entity, new ConveyorStorageComponent
					{
						Center = authoring.StorageCenter,
						Rotation = quaternion.Euler(math.radians(authoring.StorageRotation)),
						Size = authoring.StorageSize,
					});
				}

				if (authoring.IsItemSpawner)
				{
					AddComponent(entity, new ItemSpawnerComponent
					{
						Position = authoring.ItemSpawnerPosition,
						Rotation = quaternion.Euler(math.radians(authoring.ItemSpawnerRotation)),
					});
				}
			}
		}
	}
}