using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dustbreaker
{
	[Serializable]
	public struct StreamingConfig : IComponentData
	{
		public float StreamingRange;
		public int QuadrantSize;
	}

	public struct Quadrant : IComponentData
	{
		public int2 Coordinates;
	}

	public struct QuadrantCollection : IComponentData, IDisposable
	{
		public NativeParallelHashMap<int2, Entity> Map;

		public QuadrantCollection(int initialCapacity = 0)
		{
			Map = new NativeParallelHashMap<int2, Entity>(initialCapacity, Allocator.Persistent);
		}

		public void Dispose()
		{
			Map.Dispose();
		}
	}

	[InternalBufferCapacity(0)]
	public struct StreamingEvent : IBufferElementData
	{
		public int2 Coordinates;
		public bool Create;
	}
}