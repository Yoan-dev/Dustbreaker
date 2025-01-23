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

		public void UpdateHUD(Action firstInteraction, Action secondInteraction)
		{
			InteractionText.text =
				(firstInteraction == Action.None ? "" : "(E) " + firstInteraction.ToString() + "\n") +
				(secondInteraction == Action.None ? "" : "(F) " + secondInteraction.ToString());
		}
	}
}