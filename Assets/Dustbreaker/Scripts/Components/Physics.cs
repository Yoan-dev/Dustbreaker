using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Dustbreaker
{
	public struct CachedPhysicsCollider : IComponentData
	{
		public PhysicsCollider Value;
	}

	public struct CachedPhysicsMass : IComponentData
	{
		public float3 InverseInertia;
		public float InverseMass;
	}

    public struct SwitchToKinematicFlag : IComponentData, IEnableableComponent { }

    public struct SwitchToDynamicFlag : IComponentData, IEnableableComponent { }
}