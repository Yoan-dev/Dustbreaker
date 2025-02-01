using UnityEngine;
using Unity.Entities;

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class EnterExitAuthoring : MonoBehaviour
	{
		public EnterExitComponent EnterExit;

		public class Baker : Baker<EnterExitAuthoring>
		{
			public override void Bake(EnterExitAuthoring authoring)
			{
				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, authoring.EnterExit);
			}
		}
	}
}