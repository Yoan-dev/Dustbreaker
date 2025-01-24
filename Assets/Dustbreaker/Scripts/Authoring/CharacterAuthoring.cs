using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.CharacterController;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class CharacterAuthoring : MonoBehaviour
	{
		public GameObject ViewEntity;
		public AuthoringKinematicCharacterProperties CharacterProperties = AuthoringKinematicCharacterProperties.GetDefault();

		public float GroundMaxSpeed = 10f;
		public float GroundedMovementSharpness = 15f;
		public float AirDrag = 0f;
		public float JumpSpeed = 10f;
		public float3 Gravity = math.up() * -30f;
		public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling = BasicStepAndSlopeHandlingParameters.GetDefault();
		public float MinViewAngle = -90f;
		public float MaxViewAngle = 90f;
		public float InteractionRange = 1.5f;

		public class Baker : Baker<CharacterAuthoring>
		{
			public override void Bake(CharacterAuthoring authoring)
			{
				KinematicCharacterUtilities.BakeCharacter(this, authoring.gameObject, authoring.CharacterProperties);

				Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

				AddComponent(entity, new CharacterComponent
				{
					GroundMaxSpeed = authoring.GroundMaxSpeed,
					GroundedMovementSharpness = authoring.GroundedMovementSharpness,
					AirDrag = authoring.AirDrag,
					JumpSpeed = authoring.JumpSpeed,
					Gravity = authoring.Gravity,
					StepAndSlopeHandling = authoring.StepAndSlopeHandling,
					MinViewAngle = authoring.MinViewAngle,
					MaxViewAngle = authoring.MaxViewAngle,

					ViewEntity = GetEntity(authoring.ViewEntity, TransformUsageFlags.Dynamic),
					ViewPitchDegrees = 0f,
					ViewLocalRotation = quaternion.identity,

					InteractionRange = authoring.InteractionRange,
				});
				AddComponent(entity, new CharacterControl());
			}
		}
	}
}