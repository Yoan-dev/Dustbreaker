using Unity.Entities;

namespace Dustbreaker
{
	public enum MissionType
	{
		None = 0,
		Delivery,
		Repair,
	}

	public struct MissionReference : IComponentData
	{
		public Entity Entity;
	}

	public struct ItemReference : IComponentData
	{
		public Entity Entity;
	}

	public struct LocationReference : IComponentData
	{
		public Entity Entity;
	}

	[InternalBufferCapacity(0)]
	public struct DeliveryElement : IBufferElementData
	{
		// TODO: item id/type
		public int Count;
	}

	public struct RewardComponent : IComponentData
	{
		public int Value;
	}

	public struct MainMissionTag : IComponentData { }
}