using Unity.Physics.Authoring;
using UnityEngine;

namespace Dustbreaker
{
	public class ManagedReferences : MonoBehaviour
	{
		public static ManagedReferences Instance;

		public Material TerrainMaterial;

		public void Awake()
		{
			Instance = this;
		}
	}
}