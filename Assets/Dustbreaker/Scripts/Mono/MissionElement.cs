using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Dustbreaker
{
	public class MissionElement : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text title;

		[SerializeField]
		private TMP_Text description;

		public void Initialize(Entity entity, MissionType type, EntityManager entityManager)
		{
			title.text = entityManager.HasComponent<MainMissionTag>(entity) ? "Main Mission" : type.ToString();

			if (type == MissionType.Delivery)
			{
				// TODO: multi-items delivery

				ItemReference item = entityManager.GetComponentData<ItemReference>(entity);
				LocationReference location = entityManager.GetComponentData<LocationReference>(entity);
				description.text = "Deliver " + item.Entity.ToString() + " to " + location.Entity.ToString();
			}
			else
			{
				description.text = "N/A";
			}
		
			// TODO: reward
		}
	}
}