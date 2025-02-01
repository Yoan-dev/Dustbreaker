using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// based on EntityComponentSystemSamples-PhysicsSamples (see: LICENSE)

namespace Dustbreaker
{
	[RequireMatchingQueriesForUpdate]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	partial struct VehicleInputHandlingSystem : ISystem
	{
		private bool _autoPilot; // temp

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<VehicleInputs>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			// TODO: entity specific control instead of global
			// (currently, control all vehicles at once)

			var input = SystemAPI.GetSingleton<VehicleInputs>();

			if (Input.GetKeyDown(KeyCode.P))
			{
				_autoPilot = !_autoPilot;
			}

			foreach (var (speed, steering) in SystemAPI.Query<RefRW<VehicleSpeed>, RefRW<VehicleSteering>>())
			{
				float x = input.Steering;
				float a = _autoPilot ? 1f : input.Throttle;

				var newSpeed = a * speed.ValueRW.TopSpeed;
				speed.ValueRW.DriveEngaged = (byte)(newSpeed == 0f ? 0 : 1);
				speed.ValueRW.DesiredSpeed = math.lerp(speed.ValueRW.DesiredSpeed, newSpeed, speed.ValueRW.Damping);

				var newSteeringAngle = x * steering.ValueRW.MaxSteeringAngle;
				steering.ValueRW.DesiredSteeringAngle = math.lerp(steering.ValueRW.DesiredSteeringAngle, newSteeringAngle, steering.ValueRW.Damping);
			}
		}
	}
}