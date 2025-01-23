using UnityEngine;

public class CursorStateController : MonoBehaviour
{
#if UNITY_EDITOR
	private void OnApplicationFocus(bool hasFocus)
	{
		SetCursorState(hasFocus);
	}

	private void SetCursorState(bool newState)
	{
		Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
	}
#endif
}