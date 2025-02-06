using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Dustbreaker
{
	public class HUDController : MonoBehaviour
	{
		public static HUDController Instance;

		[SerializeField]
		private TMP_Text _interactionText;

		[SerializeField]
		private Transform _missionParent;

		[SerializeField]
		private MissionElement _missionPrefab;

		private List<MissionElement> _missionList;

		public void Awake()
		{
			Instance = this;
		}

		public void Start()
		{
			_missionList = new List<MissionElement>();
		}

		public void UpdateInteraction(Action primaryInteraction, Action secondaryInteraction)
		{
			_interactionText.text =
				(primaryInteraction == Action.None ? "" : "(E) " + primaryInteraction.ToString() + "\n") +
				(secondaryInteraction == Action.None ? "" : "(F) " + secondaryInteraction.ToString());
		}

		public void ClearMissions()
		{
			for (int i = 0; i < _missionList.Count; i++)
			{
				Destroy(_missionList[i].gameObject);
			}
			_missionList.Clear();
		}

		public void AddMission(Entity entity, MissionType type, EntityManager entityManager)
		{
			MissionElement mission = Instantiate(_missionPrefab, _missionParent);
			mission.Initialize(entity, type, entityManager);
			_missionList.Add(mission);
		}
	}
}