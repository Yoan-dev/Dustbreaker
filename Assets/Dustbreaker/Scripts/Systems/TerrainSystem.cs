using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Mesh;
using Collider = Unity.Physics.Collider;
using Material = Unity.Physics.Material;
using MeshCollider = Unity.Physics.MeshCollider;

namespace Dustbreaker
{
	public struct StreamingJobQueue<T> : IDisposable where T : unmanaged, IJob, IDisposable
	{
		public NativeQueue<int2> Requests;
		public NativeList<int2> Processed;
		public T Job;
		public JobHandle Handle;
		public int2 Current;
		public bool IsProcessing;

		public bool IsCompleted => Handle.IsCompleted;
		public bool HasRequest => Requests.Count > 0;

		public void Dispose()
		{
			Requests.Dispose();
			Processed.Dispose();

			if (IsProcessing)
			{
				Handle.Complete();
				Job.Dispose();
			}
		}

		public bool TryGetNextRequest(out int2 coordinates)
		{
			do { coordinates = Requests.Dequeue(); } while (HasRequest && Processed.Contains(coordinates));

			return HasRequest || !Processed.Contains(coordinates);
		}

		public void TryEnqueue(int2 coordinates)
		{
			if (!Processed.Contains(coordinates))
			{
				Requests.Enqueue(coordinates);
			}
		}

		public void Schedule(int2 coordinates, T job)
		{
			IsProcessing = true;
			Current = coordinates;
			Processed.Add(Current);
			Job = job;
			Handle = job.Schedule();
		}

		public void EndJob()
		{
			IsProcessing = false;
			Job.Dispose();
		}
	}

	[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
	[BurstCompile]
	public partial class TerrainSystem : SystemBase
	{
		private StreamingJobQueue<GetMeshDataJob> _meshQueue;
		private StreamingJobQueue<GetColliderDataJob> _colliderQueue;
		private Dictionary<int2, Mesh> _meshCache;
		private Dictionary<int2, BlobAssetReference<Collider>> _colliderCache;
		private CollisionFilter _collisionFilter;
		private Material _material;
		private Entity _prefab;

		protected override void OnCreate()
		{
			RequireForUpdate<QuadrantCollection>();
			RequireForUpdate<StreamingConfig>();
			RequireForUpdate<StreamingEvent>();

			_prefab = EntityManager.CreateEntity();
			EntityManager.AddComponent<Prefab>(_prefab);
			EntityManager.AddComponent<Quadrant>(_prefab);
			EntityManager.AddComponent<LocalTransform>(_prefab);
			EntityManager.AddComponent<LocalToWorld>(_prefab);

			_meshQueue = new StreamingJobQueue<GetMeshDataJob>
			{
				Requests = new NativeQueue<int2>(Allocator.Persistent),
				Processed = new NativeList<int2>(Allocator.Persistent),
			};
			_colliderQueue = new StreamingJobQueue<GetColliderDataJob>
			{
				Requests = new NativeQueue<int2>(Allocator.Persistent),
				Processed = new NativeList<int2>(Allocator.Persistent),
			};
			_meshCache = new Dictionary<int2, Mesh>();
			_colliderCache = new Dictionary<int2, BlobAssetReference<Collider>>();

			// TODO: from data
			_collisionFilter = new CollisionFilter
			{
				BelongsTo = 1 << 0, // Terrain
				CollidesWith = 1 << 1 | 1 << 2 | 1 << 5, // Character, Item, Vehicle
			};
			_material = new Material
			{
				FrictionCombinePolicy = Material.CombinePolicy.Maximum,
				RestitutionCombinePolicy = Material.CombinePolicy.Maximum,
				Friction = 0.5f,
				Restitution = 0.5f,
			};
		}

		protected override void OnDestroy()
		{

			_meshQueue.Dispose();
			_colliderQueue.Dispose();

			foreach (var pair in _colliderCache)
			{
				pair.Value.Dispose();
			}
		}

		protected override void OnUpdate()
		{
			ref QuadrantCollection quadrantCollection = ref SystemAPI.GetSingletonRW<QuadrantCollection>().ValueRW;
			StreamingConfig config = SystemAPI.GetSingleton<StreamingConfig>();
			NativeArray<StreamingEvent> streamingEvents = SystemAPI.GetSingletonBuffer<StreamingEvent>().ToNativeArray(Allocator.Temp);

			// process new requests
			for (int i = 0; i < streamingEvents.Length; i++)
			{
				StreamingEvent streamingEvent = streamingEvents[i];
				int2 coordinates = streamingEvent.Coordinates;

				if (streamingEvent.Create)
				{
					// TODO: batch
					Entity entity = EntityManager.Instantiate(_prefab);
					EntityManager.SetComponentData(entity, new Quadrant { Coordinates = coordinates });
					EntityManager.SetComponentData(entity, new LocalTransform
					{
						Position = new float3(coordinates.x * config.QuadrantSize, 0f, coordinates.y * config.QuadrantSize),
						Rotation = quaternion.identity,
						Scale = 1f,
					});

					quadrantCollection.Map.Add(coordinates, entity);

					if (_meshCache.ContainsKey(coordinates))
					{
						AddRendering(entity, EntityManager, _meshCache[coordinates]);

						if (_colliderCache.ContainsKey(coordinates))
						{
							AddCollision(entity, EntityManager, _colliderCache[coordinates]);
						}
						else
						{
							_colliderQueue.TryEnqueue(coordinates);
						}
					}
					else
					{
						_meshQueue.TryEnqueue(coordinates);
						_colliderQueue.TryEnqueue(coordinates);
					}
				}
				else // destroy
				{
					EntityManager.DestroyEntity(quadrantCollection.Map[coordinates]);
					quadrantCollection.Map.Remove(coordinates);
				}
			}

			streamingEvents.Dispose();
			SystemAPI.GetSingletonBuffer<StreamingEvent>().Clear();

			// check ongoing mesh request
			if (_meshQueue.IsProcessing && _meshQueue.IsCompleted)
			{
				_meshQueue.Handle.Complete();

				Mesh mesh = new Mesh();
				mesh.SetVertices(_meshQueue.Job.Vertices);
				mesh.SetUVs(0, _meshQueue.Job.Uvs);
				mesh.SetTriangles(_meshQueue.Job.Indices.ToArray(), 0);
				mesh.RecalculateNormals();
				mesh.RecalculateBounds(); // TBC

				int2 coordinates = _meshQueue.Current;
				_meshCache.Add(coordinates, mesh);
				_meshQueue.EndJob();

				// add to quadrant if streamed
				if (quadrantCollection.Map.ContainsKey(coordinates))
				{
					AddRendering(quadrantCollection.Map[coordinates], EntityManager, mesh);
				}
			}

			// queue next mesh request
			if (!_meshQueue.IsProcessing && _meshQueue.HasRequest)
			{
				if (_meshQueue.TryGetNextRequest(out int2 coordinates))
				{
					int vertexCount = (config.QuadrantSize + 1) * (config.QuadrantSize + 1);
					int triangleCount = config.QuadrantSize * config.QuadrantSize * 6;

					_meshQueue.Schedule(coordinates, new GetMeshDataJob
					{
						Vertices = new NativeArray<float3>(vertexCount, Allocator.Persistent),
						Uvs = new NativeArray<float2>(vertexCount, Allocator.Persistent),
						Indices = new NativeArray<int>(triangleCount, Allocator.Persistent),
						Coordinates = coordinates,
						Size = config.QuadrantSize,
					});
				}
			}

			// check ongoing collider request
			if (_colliderQueue.IsProcessing && _colliderQueue.IsCompleted)
			{
				_colliderQueue.Handle.Complete();

				BlobAssetReference<Collider> collider = _colliderQueue.Job.ColliderRef.Value;

				int2 coordinates = _colliderQueue.Current;
				_colliderCache.Add(coordinates, collider);
				_colliderQueue.EndJob();

				// add to quadrant if streamed
				if (quadrantCollection.Map.ContainsKey(coordinates))
				{
					AddCollision(quadrantCollection.Map[coordinates], EntityManager, collider);
				}
			}

			// queue next collider request
			if (!_colliderQueue.IsProcessing && _colliderQueue.HasRequest)
			{
				if (_colliderQueue.TryGetNextRequest(out int2 coordinates))
				{
					if (_meshCache.ContainsKey(coordinates))
					{
						_colliderQueue.Schedule(coordinates, new GetColliderDataJob
						{
							MeshDataArray = AcquireReadOnlyMeshData(_meshCache[coordinates]),
							CollisionFilter = _collisionFilter,
							Material = _material,
							ColliderRef = new NativeReference<BlobAssetReference<Collider>>(Allocator.Persistent),
						});
					}
					else
					{
						// TBC
						// mesh not ready, wait
						_colliderQueue.Requests.Enqueue(coordinates);
					}
				}
			}
		}

		public static void AddRendering(Entity entity, EntityManager entityManager, Mesh mesh)
		{
			RenderMeshDescription description = new RenderMeshDescription(ShadowCastingMode.On);
			RenderMeshArray array = new RenderMeshArray(new UnityEngine.Material[] { ManagedReferences.Instance.TerrainMaterial }, new Mesh[] { mesh });
			MaterialMeshInfo info = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

			RenderMeshUtility.AddComponents(entity, entityManager, in description, array, info);
			entityManager.SetComponentData(entity, new RenderBounds { Value = mesh.bounds.ToAABB() }); // TBC
		}

		public static void AddCollision(Entity entity, EntityManager entityManager, BlobAssetReference<Collider> ColliderRef)
		{
			entityManager.AddComponentData(entity, new PhysicsCollider { Value = ColliderRef });
			entityManager.AddSharedComponentManaged(entity, new PhysicsWorldIndex { Value = 0 });
		}

		[BurstCompile]
		public partial struct GetMeshDataJob : IJob, IDisposable
		{
			public NativeArray<float3> Vertices;
			public NativeArray<float2> Uvs;
			public NativeArray<int> Indices;
			public int2 Coordinates;
			public int Size;

			public void Execute()
			{
				int2 offset = Coordinates * Size;

				for (int y = 0, vi = 0; y <= Size; y++)
				{
					for (int x = 0; x <= Size; x++, vi++)
					{
						// temp heightmap
						int worldX = x + offset.x;
						int worldY = y + offset.y;
						float height =
							noise.snoise(new float2(worldX * 0.01f, worldY * 0.01f)) +
							noise.snoise(new float2(worldX * 0.05f, worldY * 0.05f)) +
							noise.snoise(new float2(worldX * 0.1f, worldY * 0.1f)) / 2f;

						Vertices[vi] = new float3(x - Size / 2f, height * 0.1f, y - Size / 2f);
						Uvs[vi] = new float2(x / (float)Size, y / (float)Size);
					}
				}
				for (int y = 0, vi = 0, ti = 0; y < Size; y++, vi++)
				{
					for (int x = 0; x < Size; x++, vi++, ti += 6)
					{
						Indices[ti] = vi;
						Indices[ti + 3] = Indices[ti + 2] = vi + 1;
						Indices[ti + 4] = Indices[ti + 1] = vi + Size + 1;
						Indices[ti + 5] = vi + Size + 2;
					}
				}
			}

			public void Dispose()
			{
				Vertices.Dispose();
				Uvs.Dispose();
				Indices.Dispose();
			}
		}

		[BurstCompile]
		public partial struct GetColliderDataJob : IJob, IDisposable
		{
			[ReadOnly] public MeshDataArray MeshDataArray;
			public CollisionFilter CollisionFilter;
			public Material Material;
			public NativeReference<BlobAssetReference<Collider>> ColliderRef;

			public void Execute()
			{
				MeshData meshData = MeshDataArray[0];

				// mesh is forced to index format Uint16 and one SubMesh
				NativeArray<ushort> indices = meshData.GetIndexData<ushort>();
				SubMeshDescriptor subMesh = meshData.GetSubMesh(0);

				NativeArray<float3> vertices = new NativeArray<float3>(meshData.vertexCount, Allocator.Temp);
				NativeArray<int3> triangles = new NativeArray<int3>(subMesh.indexCount / 3, Allocator.Temp);

				meshData.GetVertices(vertices.Reinterpret<Vector3>());

				ushort ti = 0;
				for (int i = 0; i < subMesh.indexCount; i += 3, ti++)
				{
					triangles[ti] = (int3)new uint3(indices[i], indices[i + 1], indices[i + 2]);
				}

				ColliderRef.Value = MeshCollider.Create(vertices, triangles, CollisionFilter, Material);

				vertices.Dispose();
				triangles.Dispose();
			}

			public void Dispose()
			{
				MeshDataArray.Dispose();
				ColliderRef.Dispose();
			}
		}
	}
}