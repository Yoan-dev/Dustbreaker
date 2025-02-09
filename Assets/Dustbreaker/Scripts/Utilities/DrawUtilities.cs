using Unity.Mathematics;
using UnityEngine;

namespace Dustbreaker
{
	public static class DrawUtilities
	{
		public static Color ToColor(this float4 color)
		{
			return new Color(color.x, color.y, color.z, color.w);
		}

		public static void DrawLine(float3 start, float3 end, float4 color, float duration = 0f)
		{
			Debug.DrawLine(start, end, color.ToColor(), duration);
		}

		public static void DrawBox(float3 center, quaternion rotation, float3 size, float4 color, float duration = 0f)
		{
			Matrix4x4 matrix =  Matrix4x4.TRS(center, rotation, size);
			Color lineColor = color.ToColor();

			float3 top1 = matrix.MultiplyPoint(new float3(0.5f, 0.5f, 0.5f));
			float3 top2 = matrix.MultiplyPoint(new float3(-0.5f, 0.5f, 0.5f));
			float3 top3 = matrix.MultiplyPoint(new float3(0.5f, 0.5f, -0.5f));
			float3 top4 = matrix.MultiplyPoint(new float3(-0.5f, 0.5f, -0.5f));

			float3 bottom1 = matrix.MultiplyPoint(new float3(0.5f, -0.5f, 0.5f));
			float3 bottom2 = matrix.MultiplyPoint(new float3(-0.5f, -0.5f, 0.5f));
			float3 bottom3 = matrix.MultiplyPoint(new float3(0.5f, -0.5f, -0.5f));
			float3 bottom4 = matrix.MultiplyPoint(new float3(-0.5f, -0.5f, -0.5f));

			Debug.DrawLine(top1, top2, lineColor, duration);
			Debug.DrawLine(top1, top3, lineColor, duration);
			Debug.DrawLine(top1, bottom1, lineColor, duration);
			Debug.DrawLine(top2, top4, lineColor, duration);
			Debug.DrawLine(top2, bottom2, lineColor, duration);
			Debug.DrawLine(top3, top4, lineColor, duration);
			Debug.DrawLine(top3, bottom3, lineColor, duration);
			Debug.DrawLine(top4, bottom4, lineColor, duration);

			Debug.DrawLine(bottom1, bottom2, lineColor, duration);
			Debug.DrawLine(bottom1, bottom3, lineColor, duration);
			Debug.DrawLine(bottom2, bottom4, lineColor, duration);
			Debug.DrawLine(bottom3, bottom4, lineColor, duration);
		}
	}
}