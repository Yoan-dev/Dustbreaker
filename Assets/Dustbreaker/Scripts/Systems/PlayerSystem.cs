using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.CharacterController;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PlayerInputsSystem : SystemBase
{
	private static bool _focusActionsSetUp;
	private bool _ignoreInput;

	protected override void OnCreate()
	{
#if UNITY_EDITOR
		if (!_focusActionsSetUp)
		{
			var ignoreInput = new InputAction(binding: "/Keyboard/escape");
			ignoreInput.performed += context => _ignoreInput = true;
			ignoreInput.Enable();

			var enableInput = new InputAction(binding: "/Mouse/leftButton");
			enableInput.performed += context => _ignoreInput = false;
			enableInput.Enable();

			_focusActionsSetUp = true;
		}
#endif

		RequireForUpdate<FixedTickSystem.Singleton>();
		RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlayerInputs>().Build());
	}

	protected override void OnUpdate()
	{
		uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;

		foreach (var playerInputs in SystemAPI.Query<RefRW<PlayerInputs>>())
		{
			if (_ignoreInput)
			{
				playerInputs.ValueRW.MoveInput = float2.zero;
				playerInputs.ValueRW.LookInput = float2.zero;
				continue;
			}

			playerInputs.ValueRW.MoveInput = new float2
			{
				x = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f),
				y = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f),
			};

			playerInputs.ValueRW.LookInput = new float2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

			if (Input.GetKeyDown(KeyCode.Space))
			{
				playerInputs.ValueRW.JumpPressed.Set(tick);
			}
		}
	}
}

/// <summary>
/// Apply inputs that need to be read at a variable rate
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerVariableStepControlSystem : ISystem
{
	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlayerInputs>().WithAllRW<CharacterControl>().Build());
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		foreach (var (playerInputs, characterControlRW) in SystemAPI.Query<PlayerInputs, RefRW<CharacterControl>>().WithAll<Simulate>())
		{
			characterControlRW.ValueRW.LookDegreesDelta = playerInputs.LookInput;
		}
	}
}

/// <summary>
/// Apply inputs that need to be read at a fixed rate.
/// It is necessary to handle this as part of the fixed step group, in case your framerate is lower than the fixed step rate.
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct PlayerFixedStepControlSystem : ISystem
{
	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<FixedTickSystem.Singleton>();
		state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlayerInputs>().WithAllRW<CharacterControl>().Build());
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;

		foreach (var (playerInputs, localTransform, characterControlRW, entity) in SystemAPI.Query<PlayerInputs, LocalTransform, RefRW<CharacterControl>>().WithAll<Simulate>().WithEntityAccess())
		{
			ref CharacterControl characterControl = ref characterControlRW.ValueRW;
			quaternion characterRotation = localTransform.Rotation;

			// Move
			float3 characterForward = MathUtilities.GetForwardFromRotation(characterRotation);
			float3 characterRight = MathUtilities.GetRightFromRotation(characterRotation);
			characterControl.MoveVector = (playerInputs.MoveInput.y * characterForward) + (playerInputs.MoveInput.x * characterRight);
			characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

			// Jump
			characterControl.Jump = playerInputs.JumpPressed.IsSet(tick);
		}
	}
}