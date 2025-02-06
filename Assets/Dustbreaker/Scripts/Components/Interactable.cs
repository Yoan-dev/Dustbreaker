using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Dustbreaker
{
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

	public struct ClimbableTag : IComponentData { }

	public struct PilotTag : IComponentData { }

	public struct DeliverTag : IComponentData { }

	[Serializable]
	public struct EnterExitComponent : IComponentData
	{
		public float3 EnterDisplacement;
		public float3 ExitDisplacement;
		public bool IgnoreY;
	}
}