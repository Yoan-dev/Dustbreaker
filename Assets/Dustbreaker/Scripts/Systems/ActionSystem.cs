using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Dustbreaker
{
	[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
	public partial struct ActionSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<ActionEvent>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			DynamicBuffer<ActionEvent> actionEvents = SystemAPI.GetSingletonBuffer<ActionEvent>();

			for (int i = 0; i < actionEvents.Length; i++)
			{
				ActionEvent actionEvent = actionEvents[i];

				// do stuff
				Debug.Log(actionEvent.Source.ToString() + " " + actionEvent.Action.ToString() + " " + actionEvent.Target.ToString());
			}

			actionEvents.Clear();
		}
	}
}