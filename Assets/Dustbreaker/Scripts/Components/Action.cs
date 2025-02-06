using System;
using Unity.Entities;

namespace Dustbreaker
{
	[Flags]
	public enum Action
	{
		None = 0,
		Use = 1 << 0,
		Pick = 1 << 1,
		Drop = 1 << 2,
		Stop = 1 << 3,
	}

	[InternalBufferCapacity(0)]
	public struct ActionEvent : IBufferElementData
	{
		public Entity Source;
		public Entity Target;
		public Action Action;
	}
}