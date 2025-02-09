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

	public struct ConveyorBeltComponent : IComponentData
	{
		public quaternion Rotation;
		public float3 Center;
		public float3 Size;
		public float Strength;
	}

	public struct ConveyorStorageComponent : IComponentData
	{
		public quaternion Rotation;
		public float3 Center;
		public float3 Size;
	}

	public struct StorageComponent : IComponentData
	{
		public float3 Position;
	}

	public struct ItemSpawnerComponent : IComponentData
	{
		public quaternion Rotation;
		public float3 Position;
	}
}