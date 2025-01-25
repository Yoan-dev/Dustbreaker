using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Dustbreaker
{
	public struct CarryComponent : IComponentData, IEnableableComponent
	{
		public Entity Entity;
	}

	public struct PickableComponent : IComponentData, IEnableableComponent
	{
		public Entity Entity;
	}

	public struct CachedPhysicsProperties : IComponentData
	{
		public PhysicsCollider PhysicsCollider;
		public float3 InverseInertia;
		public float InverseMass;
	}
}