using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class ConveyorAuthoring : MonoBehaviour
	{
		[Header("Conveyor Belt")]
		public bool ConveyorBelt;
		public float3 ConveyorBeltCenter;
		public float3 ConveyorBeltRotation;
		public float3 ConveyorBeltSize;
		public float ConveyorBeltStrength;

		[Header("Storage")]
		public bool Storage;
		public float3 StorageCenter;
		public float3 StorageRotation;
		public float3 StorageSize;

		[Header("Item Spawner")]
		public bool ItemSpawner;
		public float3 ItemSpawnerPosition;
		public float3 ItemSpawnerRotation;

		public class Baker : Baker<ConveyorAuthoring>
		{
			public override void Bake(ConveyorAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<LocationReference>(entity);

				if (authoring.ConveyorBelt)
				{
					AddComponent(entity,new ConveyorBeltComponent
					{
						Center = authoring.ConveyorBeltCenter,
						Rotation = quaternion.Euler(math.radians(authoring.ConveyorBeltRotation)),
						Size = authoring.ConveyorBeltSize,
						Strength = authoring.ConveyorBeltStrength,
					});
				}

				if (authoring.Storage)
				{
					AddComponent(entity, new StorageComponent
					{
						Position = authoring.StorageCenter,
						Rotation = quaternion.Euler(math.radians(authoring.StorageRotation)),
						Size = authoring.StorageSize,
					});
				}

				if (authoring.ItemSpawner)
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