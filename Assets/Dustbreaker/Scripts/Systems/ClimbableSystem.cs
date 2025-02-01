using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Dustbreaker
{
	[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
	[UpdateBefore(typeof(KinematicCharacterPhysicsUpdateGroup))]
	public partial struct ClimbableSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			// TODO/TBD: only query climbed (flag)
			// TODO: mounted vs static climbable
			state.Dependency = new ClimbableUpdateJob { TransformLookup = SystemAPI.GetComponentLookup<TrackedTransform>(true) }.ScheduleParallel(state.Dependency);
		}

		[BurstCompile]
		public partial struct ClimbableUpdateJob : IJobEntity
		{
			[ReadOnly] public ComponentLookup<TrackedTransform> TransformLookup;

			public void Execute(in LocalTransform transform, in Parent parent, ref ClimbableComponent climbable)
			{
				TrackedTransform parentTrackedTransform = TransformLookup[parent.Value];
				RigidTransform currentTransform = parentTrackedTransform.CurrentFixedRateTransform;
				climbable.Transform = new RigidTransform(math.mul(new float4x4(currentTransform.rot, currentTransform.pos), transform.ToMatrix()));
			}
		}
	}
}