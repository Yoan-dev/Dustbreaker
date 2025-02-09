using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Dustbreaker
{
	[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
	public partial struct SwitchPhysicsBodySystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Dependency = new SwitchToKinematicJob().ScheduleParallel(state.Dependency);
			
			state.Dependency = new SwitchToDynamicJob().ScheduleParallel(state.Dependency);
		}

		[BurstCompile]
		public partial struct SwitchToKinematicJob : IJobEntity
		{
			public void Execute(ref PhysicsVelocity velocity, ref PhysicsMass mass, ref CachedPhysicsMass cachedMass, EnabledRefRW<SwitchToKinematicFlag> toKinematicFlag)
			{
				velocity = new PhysicsVelocity();
				cachedMass.InverseMass = mass.InverseMass;
				cachedMass.InverseInertia = mass.InverseInertia;
				mass.InverseMass = 0f;
				mass.InverseInertia = float3.zero;
				toKinematicFlag.ValueRW = false;
			}
		}

		[BurstCompile]
		public partial struct SwitchToDynamicJob : IJobEntity
		{
			public void Execute(ref PhysicsMass mass, ref CachedPhysicsMass cachedMass, EnabledRefRW<SwitchToDynamicFlag> toDynamicFlag)
			{
				mass.InverseMass = cachedMass.InverseMass;
				mass.InverseInertia = cachedMass.InverseInertia;
				toDynamicFlag.ValueRW = false;
			}
		}
	}
}