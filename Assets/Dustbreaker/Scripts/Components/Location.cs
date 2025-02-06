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
}