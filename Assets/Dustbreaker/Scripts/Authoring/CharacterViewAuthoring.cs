using Unity.Entities;
using UnityEngine;

// based on Character Controller-Standard Characters (see: LICENSE)

namespace Dustbreaker
{
	[DisallowMultipleComponent]
	public class CharacterViewAuthoring : MonoBehaviour
	{
		public GameObject Character;

		public class Baker : Baker<CharacterViewAuthoring>
		{
			public override void Bake(CharacterViewAuthoring authoring)
			{
				if (authoring.transform.parent != authoring.Character.transform)
				{
					UnityEngine.Debug.LogError("ERROR: the Character View must be a direct 1st-level child of the character authoring GameObject. Conversion will be aborted");
					return;
				}

				Entity entity = GetEntity(TransformUsageFlags.Dynamic);
				AddComponent(entity, new CharacterView { CharacterEntity = GetEntity(authoring.Character, TransformUsageFlags.Dynamic) });
			}
		}
	}
}