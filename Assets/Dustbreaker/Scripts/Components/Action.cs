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
	}

	public struct InteractionController : IComponentData
	{
		public Entity Target;
	}

	public struct InteractionFlag : IComponentData, IEnableableComponent { }

	[Serializable]
	public struct InteractableComponent : IComponentData
	{
		public Action Actions;
		public float Range;

		public bool HasAction(Action action)
		{
			return (Actions & action) != 0;
		}

		public Action GetFirstInteraction()
		{
			return HasAction(Action.Use) ? Action.Use : Action.None;
		}

		public Action GetSecondInteraction()
		{
			return HasAction(Action.Pick) ? Action.Pick : Action.None;
		}
	}
}