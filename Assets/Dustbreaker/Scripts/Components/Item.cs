using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Dustbreaker
{
	public struct ItemReference : IComponentData
	{
		public Entity Entity;
	}

	public struct CarryComponent : IComponentData
	{
		public Entity Entity;
	}

	public struct PickableTag : IComponentData { }
}