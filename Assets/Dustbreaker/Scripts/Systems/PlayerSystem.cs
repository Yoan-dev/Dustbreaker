using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.CharacterController;
using UnityEngine.InputSystem;
using Unity.Physics;

namespace Dustbreaker
{
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

				if (Input.GetKeyDown(KeyCode.E))
				{
					playerInputs.ValueRW.PrimaryInteractionPressed.Set(tick);
				}

				if (Input.GetKeyDown(KeyCode.F))
				{
					playerInputs.ValueRW.SecondaryInteractionPressed.Set(tick);
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
		private CollisionFilter _interactionFilter;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<FixedTickSystem.Singleton>();
			state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlayerInputs>().WithAllRW<CharacterControl>().Build());

			_interactionFilter = new CollisionFilter
			{
				BelongsTo = 1 << 4, // Raycast
				CollidesWith = 1 << 3, // Interactable
			};
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;
			CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

			// Movement
			foreach (var (playerInputs, localTransform, characterControlRW) in SystemAPI.Query<PlayerInputs, LocalTransform, RefRW<CharacterControl>>().WithAll<Simulate>())
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

			// Interaction
			foreach (var (playerInputs, character, interactionControllerRW, interactionFlagRW) in 
				SystemAPI.Query<PlayerInputs, CharacterComponent, RefRW<InteractionController>, EnabledRefRW<InteractionFlag>>().WithPresent<InteractionFlag>().WithAll<Simulate>())
			{
				ref InteractionController interactionController = ref interactionControllerRW.ValueRW;
				LocalToWorld viewLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(character.ViewEntity);
				float3 start = viewLocalToWorld.Position;
				float3 end = start + viewLocalToWorld.Forward * character.InteractionRange;

				RaycastInput raycastInput = new RaycastInput
				{
					Start = start,
					End = end,
					Filter = _interactionFilter,
				};
				
				if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit closestHit))
				{
					// target detection
					interactionController.Target = closestHit.Entity;

					// we assume the target has an interactable component
					InteractableComponent interactable = SystemAPI.GetComponent<InteractableComponent>(interactionController.Target);

					if (playerInputs.PrimaryInteractionPressed.IsSet(tick))
					{
						interactionController.Interaction = interactable.GetPrimaryInteraction();
					}
					else if (playerInputs.SecondaryInteractionPressed.IsSet(tick))
					{
						interactionController.Interaction = interactable.GetSecondaryInteraction();
					}

					// trigger interaction
					if (interactionController.Interaction != Action.None)
					{
						interactionFlagRW.ValueRW = true;
						Debug.Log(interactionController.Interaction.ToString());
					}
				}
				else
				{
					interactionController.Target = Entity.Null;
					interactionController.Interaction = Action.None;
					interactionFlagRW.ValueRW = false;
				}
			}
		}
	}
}