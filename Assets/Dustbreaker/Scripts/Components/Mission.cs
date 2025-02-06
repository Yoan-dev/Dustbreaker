using Unity.Entities;

namespace Dustbreaker
{
	public enum MissionType
	{
		None = 0,
		Delivery,
		Repair,
	}

	public enum RewardType
	{
		None = 0,
		Item,
		BarterValue,
	}

	public struct MissionComponent : IComponentData
	{
		public MissionType Type;
	}

	public struct MissionReference : IComponentData
	{
		public Entity Entity;
	}

	[InternalBufferCapacity(0)]
	public struct DeliveryElement : IBufferElementData
	{
		public int ExternalId; // TBD
		public int Count;
	}

	public struct RewardComponent : IComponentData
	{
		public RewardType Type;
		public int ExternalId; // TBD
		public int Count;
	}

	public struct SuccessTag : IComponentData { }

	public struct MainMissionTag : IComponentData { }
}