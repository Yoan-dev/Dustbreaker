using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class ConveyorAuthoring : MonoBehaviour
	{
		public bool ConveyorBelt;
		public bool Storage;
		public bool ItemSpawner;

		public ConveyorBeltComponent ConveyorBeltComponent;
		public StorageComponent StorageComponent;
		public ItemSpawnerComponent ItemSpawnerComponent;

		public class Baker : Baker<ConveyorAuthoring>
		{
			public override void Bake(ConveyorAuthoring authoring)
			{
				// TODO: rotation to rad quat from euler

				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent<LocationReference>(entity);

				if (authoring.ConveyorBelt)
				{
					AddComponent(entity, authoring.ConveyorBeltComponent);
				}

				if (authoring.Storage)
				{
					AddComponent(entity, authoring.StorageComponent);
				}

				if (authoring.ItemSpawner)
				{
					AddComponent(entity, authoring.ItemSpawnerComponent);
				}
			}
		}
	}
}