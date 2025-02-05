using Unity.Burst;
using Unity.Entities;

namespace Dustbreaker
{
	[UpdateBefore(typeof(UISystem))]
	public partial struct MissionSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
		}
	}
}