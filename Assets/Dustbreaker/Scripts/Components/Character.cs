using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.CharacterController;

// based on Character Controller-Standard Characters (see: LICENSE)

namespace Dustbreaker
{
	[Serializable]
	public struct CharacterComponent : IComponentData
	{
		public float GroundMaxSpeed;
		public float GroundedMovementSharpness;
		public float AirDrag;
		public float JumpSpeed;
		public float3 Gravity;
		public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;

		public float MinViewAngle;
		public float MaxViewAngle;

		public Entity ViewEntity;
		public float ViewPitchDegrees;
		public quaternion ViewLocalRotation;

		public float InteractionRange;
	}

	[Serializable]
	public struct CharacterControl : IComponentData
	{
		public float3 MoveVector;
		public float2 LookDegreesDelta;
		public bool Jump;
	}

	[Serializable]
	public struct CharacterView : IComponentData
	{
		public Entity CharacterEntity;
	}
}