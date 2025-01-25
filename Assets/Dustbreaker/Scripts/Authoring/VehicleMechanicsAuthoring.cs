using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

// based on EntityComponentSystemSamples-PhysicsSamples (see: LICENSE)

namespace Dustbreaker
{
	[RequireComponent(typeof(PhysicsBodyAuthoring))]
	public class VehicleMechanics : MonoBehaviour
	{
		[Header("Wheel Parameters...")]
		public List<GameObject> wheels = new List<GameObject>();
		public float wheelBase = 0.5f;
		public float wheelFrictionRight = 0.5f;
		public float wheelFrictionForward = 0.5f;
		public float wheelMaxImpulseRight = 10.0f;
		public float wheelMaxImpulseForward = 10.0f;
		[Header("Suspension Parameters...")]
		public float suspensionLength = 0.5f;
		public float suspensionStrength = 1.0f;
		public float suspensionDamping = 0.1f;
		[Header("Steering Parameters...")]
		public List<GameObject> steeringWheels = new List<GameObject>();
		[Header("Drive Parameters...")]
		public List<GameObject> driveWheels = new List<GameObject>();
		[Header("Miscellaneous Parameters...")]
		public bool drawDebugInformation = false;
	}

	struct WheelBakingInfo
	{
		public Entity Wheel;
		public Entity GraphicalRepresentation;
		public RigidTransform WorldFromSuspension;
		public RigidTransform WorldFromChassis;
	}

	[TemporaryBakingType]
	struct VehicleMechanicsForBaking : IComponentData
	{
		public NativeArray<WheelBakingInfo> Wheels;
		public NativeArray<Entity> steeringWheels;
		public NativeArray<Entity> driveWheels;
	}

	partial class VehicleMechanicsBaker : Baker<VehicleMechanics>
	{
		public override void Bake(VehicleMechanics authoring)
		{
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent<VehicleBody>(entity);
			AddComponent(entity, new VehicleConfiguration
			{
				wheelBase = authoring.wheelBase,
				wheelFrictionRight = authoring.wheelFrictionRight,
				wheelFrictionForward = authoring.wheelFrictionForward,
				wheelMaxImpulseRight = authoring.wheelMaxImpulseRight,
				wheelMaxImpulseForward = authoring.wheelMaxImpulseForward,
				suspensionLength = authoring.suspensionLength,
				suspensionStrength = authoring.suspensionStrength,
				suspensionDamping = authoring.suspensionDamping,
				invWheelCount = 1f / authoring.wheels.Count,
				drawDebugInformation = (byte)(authoring.drawDebugInformation ? 1 : 0)
			});
			AddComponent(entity, new VehicleMechanicsForBaking()
			{
				Wheels = GetWheelInfo(authoring.wheels, Allocator.Temp),
				steeringWheels = ToNativeArray(authoring.steeringWheels, Allocator.Temp),
				driveWheels = ToNativeArray(authoring.driveWheels, Allocator.Temp)
			});
		}

		NativeArray<WheelBakingInfo> GetWheelInfo(List<GameObject> wheels, Allocator allocator)
		{
			if (wheels == null)
				return default;

			var array = new NativeArray<WheelBakingInfo>(wheels.Count, allocator);
			int i = 0;
			foreach (var wheel in wheels)
			{
				RigidTransform worldFromSuspension = new RigidTransform
				{
					pos = wheel.transform.parent.position,
					rot = wheel.transform.parent.rotation
				};

				RigidTransform worldFromChassis = new RigidTransform
				{
					pos = wheel.transform.parent.parent.parent.position,
					rot = wheel.transform.parent.parent.parent.rotation
				};

				array[i++] = new WheelBakingInfo()
				{
					Wheel = GetEntity(wheel, TransformUsageFlags.Dynamic),
					GraphicalRepresentation = GetEntity(wheel.transform.GetChild(0), TransformUsageFlags.Dynamic),
					WorldFromSuspension = worldFromSuspension,
					WorldFromChassis = worldFromChassis,
				};
			}

			return array;
		}

		NativeArray<Entity> ToNativeArray(List<GameObject> list, Allocator allocator)
		{
			if (list == null)
				return default;

			var array = new NativeArray<Entity>(list.Count, allocator);
			for (int i = 0; i < list.Count; ++i)
				array[i] = GetEntity(list[i], TransformUsageFlags.Dynamic);

			return array;
		}
	}

	[RequireMatchingQueriesForUpdate]
	[UpdateAfter(typeof(EndColliderBakingSystem))]
	[UpdateAfter(typeof(PhysicsBodyBakingSystem))]
	[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
	partial struct VehicleMechanicsBakingSystem : ISystem
	{
		public void OnUpdate(ref SystemState state)
		{
			EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

			foreach (var (m, vehicleEntity)
					 in SystemAPI.Query<RefRO<VehicleMechanicsForBaking>>().WithEntityAccess().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities))
			{
				foreach (var wheel in m.ValueRO.Wheels)
				{
					var wheelEntity = wheel.Wheel;

					// Assumed hierarchy:
					// - chassis
					//  - mechanics
					//   - suspension
					//    - wheel (rotates about yaw axis and translates along suspension up)
					//     - graphic (rotates about pitch axis)

					RigidTransform worldFromSuspension = wheel.WorldFromSuspension;

					RigidTransform worldFromChassis = wheel.WorldFromChassis;

					var chassisFromSuspension = math.mul(math.inverse(worldFromChassis), worldFromSuspension);

					commandBuffer.AddComponent(wheelEntity, new Wheel
					{
						Vehicle = vehicleEntity,
						GraphicalRepresentation = wheel.GraphicalRepresentation, // assume wheel has a single child with rotating graphic
																				 // TODO assume for now that driving/steering wheels also appear in this list
						UsedForSteering = (byte)(m.ValueRO.steeringWheels.Contains(wheelEntity) ? 1 : 0),
						UsedForDriving = (byte)(m.ValueRO.driveWheels.Contains(wheelEntity) ? 1 : 0),
						ChassisFromSuspension = chassisFromSuspension
					});
				}
			}

			commandBuffer.Playback(state.EntityManager);
		}
	}
}