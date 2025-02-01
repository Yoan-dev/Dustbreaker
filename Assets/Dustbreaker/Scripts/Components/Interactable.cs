using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Dustbreaker
{
	public struct CarryComponent : IComponentData
	{
		public Entity Entity;
	}

	public struct PickableTag : IComponentData { }

	public struct ClimbableTag : IComponentData { }

	public struct PilotTag : IComponentData { }

	public struct CachedPhysicsProperties : IComponentData
	{
		public PhysicsCollider PhysicsCollider;
		public float3 InverseInertia;
		public float InverseMass;
	}

	public struct TrackedParentComponent : IComponentData
	{
		public RigidTransform Transform;
	}

	[Serializable]
	public struct EnterExitComponent : IComponentData
	{
		public float3 EnterDisplacement;
		public float3 ExitDisplacement;
		public bool IgnoreY;
	}
}