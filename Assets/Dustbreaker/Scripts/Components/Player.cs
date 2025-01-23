using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct PlayerInputs : IComponentData
{
	public float2 MoveInput;
	public float2 LookInput;
	public FixedInputEvent JumpPressed;
}