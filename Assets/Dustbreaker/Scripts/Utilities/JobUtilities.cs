using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Dustbreaker
{
	[BurstCompile]
	public partial struct WriteQueueToBufferJob<T> : IJob where T : unmanaged, IBufferElementData
	{
		public Entity LookupEntity;
		public BufferLookup<T> BufferLookup;
		public NativeQueue<T> Queue;

		public void Execute()
		{
			DynamicBuffer<T> buffer = BufferLookup[LookupEntity];
			while (Queue.Count > 0)
			{
				buffer.Add(Queue.Dequeue());
			}
		}
	}
}