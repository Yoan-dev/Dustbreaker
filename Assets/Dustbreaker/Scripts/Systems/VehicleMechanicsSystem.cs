using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// based on EntityComponentSystemSamples-PhysicsSamples (see: LICENSE)

namespace Dustbreaker
{
	[UpdateInGroup(typeof(PhysicsSystemGroup))]
	[UpdateAfter(typeof(PhysicsInitializeGroup))]
	[UpdateBefore(typeof(PhysicsSimulationGroup))]
	public partial struct VehicleMechanicsSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<VehicleConfiguration>();
			state.RequireForUpdate<PhysicsWorldSingleton>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			// update vehicle properties first
			state.Dependency = new PrepareVehiclesJob().ScheduleParallel(state.Dependency);

			state.Dependency.Complete();

			// this sample makes direct modifications to impulses between PhysicsInitializeGroup and PhysicsSimulationGroup
			// we thus use PhysicsWorldExtensions rather than modifying component data, since they have already been consumed by BuildPhysicsWorld
			PhysicsWorld world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;
			//state.EntityManager.CompleteDependencyBeforeRW<PhysicsWorldSingleton>();

			// update each wheel
			var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

			foreach (var (localTransform, wheel, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Wheel>>().WithEntityAccess())
			{
				var newLocalTransform = localTransform;

				Entity ce = wheel.ValueRO.Vehicle;
				if (ce == Entity.Null) return;
				int ceIdx = world.GetRigidBodyIndex(ce);
				if (-1 == ceIdx || ceIdx >= world.NumDynamicBodies) return;

				var mechanics = SystemAPI.GetComponent<VehicleConfiguration>(ce);
				var vehicleBody = SystemAPI.GetComponent<VehicleBody>(ce);

				var t = SystemAPI.GetComponent<LocalTransform>(ce);
				float3 cePosition = t.Position;
				quaternion ceRotation = t.Rotation;

				float3 ceCenterOfMass = vehicleBody.WorldCenterOfMass;
				float3 ceUp = math.mul(ceRotation, new float3(0f, 1f, 0f));
				float3 ceForward = math.mul(ceRotation, new float3(0f, 0f, 1f));
				float3 ceRight = math.mul(ceRotation, new float3(1f, 0f, 0f));

				CollisionFilter filter = world.GetCollisionFilter(ceIdx);

				float driveDesiredSpeed = 0f;
				bool driveEngaged = false;
				if (SystemAPI.HasComponent<VehicleSpeed>(ce))
				{
					var vehicleSpeed = SystemAPI.GetComponent<VehicleSpeed>(ce);
					driveDesiredSpeed = vehicleSpeed.DesiredSpeed;
					driveEngaged = vehicleSpeed.DriveEngaged != 0;
				}

				float desiredSteeringAngle = SystemAPI.HasComponent<VehicleSteering>(ce)
					? SystemAPI.GetComponent<VehicleSteering>(ce).DesiredSteeringAngle
					: 0f;

				RigidTransform worldFromChassis = new RigidTransform
				{
					pos = cePosition,
					rot = ceRotation
				};

				RigidTransform suspensionFromWheel = new RigidTransform
				{
					pos = localTransform.ValueRO.Position,
					rot = localTransform.ValueRO.Rotation
				};

				RigidTransform chassisFromWheel = math.mul(wheel.ValueRO.ChassisFromSuspension, suspensionFromWheel);
				RigidTransform worldFromLocal = math.mul(worldFromChassis, chassisFromWheel);

				// create a raycast from the suspension point on the chassis
				var worldFromSuspension = math.mul(worldFromChassis, wheel.ValueRO.ChassisFromSuspension);
				float3 rayStart = worldFromSuspension.pos;
				float3 rayEnd = (-ceUp * (mechanics.suspensionLength + mechanics.wheelBase)) + rayStart;

				if (mechanics.drawDebugInformation != 0)
					Debug.DrawRay(rayStart, rayEnd - rayStart);

				var raycastInput = new RaycastInput
				{
					Start = rayStart,
					End = rayEnd,
					Filter = filter
				};

				var hit = world.CastRay(raycastInput, out var rayResult);

				var invWheelCount = mechanics.invWheelCount;

				// Calculate a simple slip factor based on chassis tilt.
				float slopeSlipFactor = vehicleBody.SlopeSlipFactor;

				float3 wheelPos = math.select(raycastInput.End, rayResult.Position, hit);
				wheelPos -= (cePosition - ceCenterOfMass);

				float3 velocityAtWheel = world.GetLinearVelocity(ceIdx, wheelPos);

				float3 weUp = ceUp;
				float3 weRight = ceRight;
				float3 weForward = ceForward;

				// Assumed hierarchy:
				// - chassis
				//  - mechanics
				//   - suspension
				//    - wheel (rotates about yaw axis and translates along suspension up)
				//     - graphic (rotates about pitch axis)

				#region handle wheel steering
				{
					// update yaw angle if wheel is used for steering
					if (wheel.ValueRO.UsedForSteering != 0)
					{
						quaternion wRotation = quaternion.AxisAngle(ceUp, desiredSteeringAngle);
						weRight = math.rotate(wRotation, weRight);
						weForward = math.rotate(wRotation, weForward);

						newLocalTransform.ValueRW.Rotation = quaternion.AxisAngle(math.up(), desiredSteeringAngle);
					}
				}
				#endregion

				float currentSpeedUp = math.dot(velocityAtWheel, weUp);
				float currentSpeedForward = math.dot(velocityAtWheel, weForward);
				float currentSpeedRight = math.dot(velocityAtWheel, weRight);

				#region handle wheel rotation
				{
					// update rotation of graphical representation about axle
					bool isDriven = driveEngaged && wheel.ValueRO.UsedForDriving != 0;
					float weRotation = isDriven
						? (driveDesiredSpeed / mechanics.wheelBase)
						: (currentSpeedForward / mechanics.wheelBase);

					weRotation = math.radians(weRotation);

					newLocalTransform.ValueRW.Rotation = math.mul(localTransform.ValueRO.Rotation, quaternion.AxisAngle(new float3(1f, 0f, 0f), weRotation));         // TODO Should this use newLocalTransform to read from?
				}
				#endregion

				var parentFromWorld = math.inverse(worldFromSuspension);
				if (!hit)
				{
					float3 wheelDesiredPos = (-ceUp * mechanics.suspensionLength) + rayStart;
					var worldPosition = math.lerp(worldFromLocal.pos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);
					
					// update translation of wheels along suspension column
					newLocalTransform.ValueRW.Position = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz;
				}
				else
				{
					// remove the wheelbase to get wheel position.
					float fraction = rayResult.Fraction - (mechanics.wheelBase) / (mechanics.suspensionLength + mechanics.wheelBase);

					float3 wheelDesiredPos = math.lerp(rayStart, rayEnd, fraction);
					// update translation of wheels along suspension column
					var worldPosition = math.lerp(worldFromLocal.pos, wheelDesiredPos, mechanics.suspensionDamping / mechanics.suspensionStrength);

					newLocalTransform.ValueRW.Position = math.mul(parentFromWorld, new float4(worldPosition, 1f)).xyz;

					#region Suspension
					{
						// Calculate and apply the impulses
						var posA = rayEnd;
						var posB = rayResult.Position;
						var lvA = currentSpeedUp * weUp;
						var lvB = world.GetLinearVelocity(rayResult.RigidBodyIndex, posB);

						var impulse = mechanics.suspensionStrength * (posB - posA) + mechanics.suspensionDamping * (lvB - lvA);
						impulse = impulse * invWheelCount;
						float impulseUp = math.dot(impulse, weUp);

						// Suspension shouldn't necessarily pull the vehicle down!
						float downForceLimit = -0.25f;
						if (downForceLimit < impulseUp)
						{
							impulse = impulseUp * weUp;

							UnityEngine.Assertions.Assert.IsTrue(math.all(math.isfinite(impulse)));
							world.ApplyImpulse(ceIdx, impulse, posA);

							if (mechanics.drawDebugInformation != 0)
								Debug.DrawRay(wheelDesiredPos, impulse, Color.green);
						}
					}
					#endregion

					#region Sideways friction
					{
						float deltaSpeedRight = (0.0f - currentSpeedRight);
						deltaSpeedRight = math.clamp(deltaSpeedRight, -mechanics.wheelMaxImpulseRight, mechanics.wheelMaxImpulseRight);
						deltaSpeedRight *= mechanics.wheelFrictionRight;
						deltaSpeedRight *= slopeSlipFactor;

						float3 impulse = deltaSpeedRight * weRight;
						float effectiveMass = world.GetEffectiveMass(ceIdx, impulse, wheelPos);
						impulse = impulse * effectiveMass * invWheelCount;

						UnityEngine.Assertions.Assert.IsTrue(math.all(math.isfinite(impulse)));
						world.ApplyImpulse(ceIdx, impulse, wheelPos);
						world.ApplyImpulse(rayResult.RigidBodyIndex, -impulse, wheelPos);

						if (mechanics.drawDebugInformation != 0)
							Debug.DrawRay(wheelDesiredPos, impulse, Color.red);
					}
					#endregion

					#region Drive
					{
						if (driveEngaged && wheel.ValueRO.UsedForDriving != 0)
						{
							float deltaSpeedForward = (driveDesiredSpeed - currentSpeedForward);
							deltaSpeedForward = math.clamp(deltaSpeedForward, -mechanics.wheelMaxImpulseForward, mechanics.wheelMaxImpulseForward);
							deltaSpeedForward *= mechanics.wheelFrictionForward;
							deltaSpeedForward *= slopeSlipFactor;

							float3 impulse = deltaSpeedForward * weForward;

							float effectiveMass = world.GetEffectiveMass(ceIdx, impulse, wheelPos);
							impulse = impulse * effectiveMass * invWheelCount;

							UnityEngine.Assertions.Assert.IsTrue(math.all(math.isfinite(impulse)));
							world.ApplyImpulse(ceIdx, impulse, wheelPos);
							world.ApplyImpulse(rayResult.RigidBodyIndex, -impulse, wheelPos);

							if (mechanics.drawDebugInformation != 0)
								Debug.DrawRay(wheelDesiredPos, impulse, Color.blue);
						}
					}
					#endregion
				}

				if (!newLocalTransform.ValueRO.Equals(localTransform.ValueRO))
				{
					commandBuffer.SetComponent(entity, newLocalTransform.ValueRO);
				}
			}

			commandBuffer.Playback(state.EntityManager);
			commandBuffer.Dispose();
		}

		[BurstCompile]
		partial struct PrepareVehiclesJob : IJobEntity
		{
			private void Execute(Entity entity, ref VehicleBody vehicleBody, in VehicleConfiguration mechanics, in PhysicsMass mass, in LocalTransform localTransform)
			{
				vehicleBody.WorldCenterOfMass = mass.GetCenterOfMassWorldSpace(localTransform.Position, localTransform.Rotation);

				// calculate a simple slip factor based on chassis tilt
				float3 worldUp = math.mul(localTransform.Rotation, math.up());

				vehicleBody.SlopeSlipFactor = math.pow(math.abs(math.dot(worldUp, math.up())), 4f);
			}
		}
	}
	[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
	public partial struct VehicleStandstillSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			state.Dependency = new VehicleStandstillJob().ScheduleParallel(state.Dependency);
		}

		[BurstCompile]
		public partial struct VehicleStandstillJob : IJobEntity
		{
			public void Execute(ref PhysicsVelocity velocity, ref PhysicsMass mass, ref StandstillComponent standstill, in VehicleSpeed speed, in VehicleSteering steering)
			{
				bool isMoving = math.length(velocity.Linear) > 0.1f || math.length(velocity.Angular) > 0.003f;
				bool isDriving = math.abs(speed.DesiredSpeed) > 0.1f || math.abs(steering.DesiredSteeringAngle) > 0.03f;

				//Debug.Log(
				//	"(" + mass.IsKinematic + ") " +
				//	"Velocity (" + isMoving + "): " + math.length(velocity.Linear) + " - " + math.length(velocity.Angular) + 
				//	", Drive (" + isDriving + "): " + speed.DesiredSpeed + " - " + steering.DesiredSteeringAngle);

				if (!mass.IsKinematic && !isMoving && !isDriving)
				{
					// switch to kinematic
					standstill.CachedInverseMass = mass.InverseMass;
					standstill.CachedInverseInertia = mass.InverseInertia;
					mass.InverseMass = 0f;
					mass.InverseInertia = float3.zero;
					velocity.Linear = float3.zero;
					velocity.Angular = float3.zero;
				}
				else if (mass.IsKinematic && isDriving)
				{
					// go back to dynamic
					mass.InverseMass = standstill.CachedInverseMass;
					mass.InverseInertia = standstill.CachedInverseInertia;
				}
			}
		}
	}
}