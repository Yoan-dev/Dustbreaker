using System;
using Unity.Entities;
using Unity.Mathematics;

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

	public struct InteractionController : IComponentData
	{
		public Entity Target;
		public Action Interaction;
	}

	public struct InteractionFlag : IComponentData, IEnableableComponent { }

	[Serializable]
	public struct InteractableComponent : IComponentData
	{
		public Action Actions;

		public bool HasAction(Action action)
		{
			return (Actions & action) != 0;
		}

		public Action GetPrimaryInteraction()
		{
			return HasAction(Action.Use) ? Action.Use : Action.None;
		}

		public Action GetSecondaryInteraction()
		{
			return HasAction(Action.Pick) ? Action.Pick : Action.None;
		}
	}

	[InternalBufferCapacity(0)]
	public struct ActionEvent : IBufferElementData
	{
		public Entity Source;
		public Entity Target;
		public Action Action;
	}
}