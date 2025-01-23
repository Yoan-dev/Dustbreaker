using UnityEngine;

namespace Dustbreaker
{
	public class MainGameObjectCamera : MonoBehaviour
	{
		public static Camera Instance;

		void Awake()
		{
			Instance = GetComponent<Camera>();
		}
	}
}