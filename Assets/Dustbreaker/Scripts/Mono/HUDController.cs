using TMPro;
using UnityEngine;

namespace Dustbreaker
{
	public class HUDController : MonoBehaviour
	{
		public static HUDController Instance;

		[SerializeField]
		private TMP_Text InteractionText;

		public void Awake()
		{
			Instance = this;
		}

		public void UpdateHUD(Action primaryInteraction, Action secondaryInteraction)
		{
			InteractionText.text =
				(primaryInteraction == Action.None ? "" : "(E) " + primaryInteraction.ToString() + "\n") +
				(secondaryInteraction == Action.None ? "" : "(F) " + secondaryInteraction.ToString());
		}
	}
}