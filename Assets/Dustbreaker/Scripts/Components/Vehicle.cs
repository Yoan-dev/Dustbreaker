using Unity.Entities;
using Unity.Mathematics;

// based on EntityComponentSystemSamples-PhysicsSamples (see: LICENSE)

namespace Dustbreaker
{
	public struct VehicleInputs : IComponentData
	{
		public float Steering;
		public float Throttle;
	}

	public struct VehicleTag : IComponentData { }

	public struct VehicleSpeed : IComponentData
	{
		public float TopSpeed;
		public float DesiredSpeed;
		public float Damping;
		public byte DriveEngaged;
	}

	public struct VehicleSteering : IComponentData
	{
		public float MaxSteeringAngle;
		public float DesiredSteeringAngle;
		public float Damping;
	}

	// configuration properties of the vehicle mechanics, which change with low frequency at run-time
	public struct VehicleConfiguration : IComponentData
	{
		public float wheelBase;
		public float wheelFrictionRight;
		public float wheelFrictionForward;
		public float wheelMaxImpulseRight;
		public float wheelMaxImpulseForward;
		public float suspensionLength;
		public float suspensionStrength;
		public float suspensionDamping;
		public float invWheelCount;
		public byte drawDebugInformation;
	}

	// physics properties of the vehicle rigid body, which change with high frequency at run-time
	public struct VehicleBody : IComponentData
	{
		public float SlopeSlipFactor;
		public float3 WorldCenterOfMass;
	}

	public struct Wheel : IComponentData
	{
		public Entity Vehicle;
		public Entity GraphicalRepresentation;
		public byte UsedForSteering;
		public byte UsedForDriving;
		public RigidTransform ChassisFromSuspension;
	}

	public struct StandstillComponent : IComponentData
	{
		public float CachedInverseMass;
		public float3 CachedInverseInertia;
	}

	public struct TrackedParentComponent : IComponentData
	{
		public RigidTransform Transform;
	}
}