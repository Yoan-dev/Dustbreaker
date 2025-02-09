using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Dustbreaker
{
	public struct LocationReference : IComponentData
	{
		public Entity Entity;
	}

	[InternalBufferCapacity(0)]
	public struct SpawnPoint : IBufferElementData
	{
		public quaternion Rotation;
		public float3 Position;
		public Entity Prefab;
	}

	public struct LocationTag : IComponentData { }

	[Serializable]
	public struct ConveyorBeltComponent : IComponentData
	{
		public RigidTransform Transform;
		public float3 Size;
		public float Strength;
	}

	[Serializable]
	public struct StorageComponent : IComponentData
	{
		public RigidTransform Transform;
		public float3 Size;
	}

	[Serializable]
	public struct ItemSpawnerComponent : IComponentData
	{
		public RigidTransform SpawnPoint;
	}
}