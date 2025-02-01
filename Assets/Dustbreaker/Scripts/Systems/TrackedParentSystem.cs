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
	public partial struct TrackedParentSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Dependency = new TrackedParentUpdateJob { TransformLookup = SystemAPI.GetComponentLookup<TrackedTransform>(true) }.ScheduleParallel(state.Dependency);
		}

		[BurstCompile]
		public partial struct TrackedParentUpdateJob : IJobEntity
		{
			[ReadOnly] public ComponentLookup<TrackedTransform> TransformLookup;

			public void Execute(in LocalTransform transform, in Parent parent, ref TrackedParentComponent trackedParent)
			{
				// TODO/TBD: process current vs previous
				TrackedTransform trackedTransform = TransformLookup[parent.Value];
				RigidTransform currentTransform = trackedTransform.CurrentFixedRateTransform;
				trackedParent.Transform = new RigidTransform(math.mul(new float4x4(currentTransform.rot, currentTransform.pos), transform.ToMatrix()));
			}
		}
	}
}