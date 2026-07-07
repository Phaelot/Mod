// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.MapArrow
using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using SovereigntyTK;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.UI.Map
{
	public class MapArrow
	{
		private ArrowSprite OutlineArrow;

		private ArrowSprite FillArrow;

		private ArrowSprite ShadowArrow;

		private List<Vector3> GuideLine;

		private Sovereignty Game;

		public bool Visible = true;

		private float SegmentLength;

		public MapArrow(List<Vector3> Points, Sovereignty Game, float ArrowWidth = 8f, float SegmentLength = 20f)
		{
			if (Points.Count < 2)
			{
				throw new Exception("Cannot create arrow with less than 2 points");
			}
			this.Game = Game;
			this.SegmentLength = SegmentLength;
			Points.Reverse();
			for (int i = 0; i < Points.Count; i++)
			{
				Points[i] = new Vector3(Points[i].X, Points[i].Y, Points[i].Z);
			}
			if (Points.Count == 2)
			{
				AddExtraPoint(Points);
			}
			GuideLine = CreateGuideLine(Points, ArrowWidth);
			GetLineLength(GuideLine);
			float num = 0f;
			bool flag = true;
			bool flag2 = false;
			List<Vector3> list = new List<Vector3>();
			List<Vector3> list2 = new List<Vector3>();
			for (int j = 0; j < GuideLine.Count; j++)
			{
				Vector3 vector2;
				if (j == 0)
				{
					Vector3 vector = GuideLine[1] - GuideLine[0];
					vector.Normalize();
					vector2 = new Vector3(0f - vector.Z, 0f, vector.X);
				}
				else if (j == GuideLine.Count - 1)
				{
					Vector3 vector3 = GuideLine[GuideLine.Count - 1] - GuideLine[GuideLine.Count - 2];
					vector3.Normalize();
					vector2 = new Vector3(0f - vector3.Z, 0f, vector3.X);
				}
				else
				{
					Vector3 vector4 = GuideLine[j] - GuideLine[j - 1];
					vector4 += GuideLine[j + 1] - GuideLine[j];
					vector4.Normalize();
					vector2 = new Vector3(0f - vector4.Z, 0f, vector4.X);
				}
				if (!flag && !flag2 && j < GuideLine.Count - 1)
				{
					GuideLine[j] = GuideLine[j + 1];
					flag2 = true;
				}
				Vector3 item = GuideLine[j] + vector2 * num;
				Vector3 item2 = GuideLine[j] - vector2 * num;
				if (flag)
				{
					if (num < ArrowWidth * 2f)
					{
						num += ArrowWidth / 2f;
					}
					else
					{
						flag = false;
					}
				}
				else
				{
					num = ArrowWidth;
				}
				list.Add(item);
				list2.Add(item2);
			}
			List<Vector3> list3 = new List<Vector3>();
			list2.Reverse();
			list3.AddRange(list);
			list3.AddRange(list2);
			CreateArrows(list3, ArrowWidth);
		}

		private void CreateArrows(List<Vector3> FinalPoints, float ArrowWidth)
		{
			List<PointF> list = new List<PointF>();
			PointF[] array = new PointF[FinalPoints.Count];
			PointF[] array2 = new PointF[FinalPoints.Count];
			PointF[] array3 = new PointF[FinalPoints.Count];
			float num = ArrowWidth * 0.5f;
			for (int i = 0; i < FinalPoints.Count; i++)
			{
				ref PointF reference = ref array[i];
				reference = new PointF(FinalPoints[i].X, FinalPoints[i].Z);
				ref PointF reference2 = ref array2[i];
				reference2 = new PointF(FinalPoints[i].X + num, FinalPoints[i].Z - num);
				Vector3 vector = ((i != 0) ? FinalPoints[i - 1] : FinalPoints[FinalPoints.Count - 1]);
				Vector3 vector2 = ((i != FinalPoints.Count - 1) ? FinalPoints[i + 1] : FinalPoints[0]);
				Vector3 vector3 = FinalPoints[i] - vector;
				vector3 += vector2 - FinalPoints[i];
				vector3.Normalize();
				Vector3 vector4 = new Vector3(0f - vector3.Z, 0f, vector3.X);
				vector4 *= 0.25f * ArrowWidth;
				list.Add(new PointF((FinalPoints[i] - vector4).X, (FinalPoints[i] - vector4).Z));
			}
			list.RemoveAt(0);
			list.RemoveAt(list.Count - 1);
			PointF pointF = list[0];
			PointF pointF2 = list[list.Count - 1];
			PointF pointF3 = list[1];
			PointF pointF4 = list[list.Count - 2];
			Vector2 vector5 = new Vector2(pointF.X, pointF.Y);
			Vector2 vector6 = new Vector2(pointF2.X, pointF2.Y);
			Vector2 vector7 = new Vector2(pointF3.X, pointF3.Y);
			Vector2 vector8 = new Vector2(pointF4.X, pointF4.Y);
			Vector2 vector9 = vector7 - vector5;
			Vector2 vector10 = vector8 - vector6;
			vector9.Normalize();
			vector10.Normalize();
			Vector2 pe = vector5 - vector9 * 50f;
			Vector2 pe2 = vector6 - vector10 * 50f;
			Vector2 intersect = GetIntersect(vector5, pe, vector6, pe2);
			list.Insert(0, new PointF(intersect.X, intersect.Y));
			array3 = list.ToArray();
			ShadowArrow = new ArrowSprite(array2, Game);
			OutlineArrow = new ArrowSprite(array, Game);
			FillArrow = new ArrowSprite(array3, Game);
			ShadowArrow.SetColour(Color.FromArgb(128, 0, 0, 0));
			OutlineArrow.SetColour(Color.FromArgb(255, 255, 250, 220));
			FillArrow.SetColour(Color.FromArgb(255, 47, 65, 38));
		}

		private Vector2 GetIntersect(Vector2 ps1, Vector2 pe1, Vector2 ps2, Vector2 pe2)
		{
			float num = pe1.Y - ps1.Y;
			float num2 = ps1.X - pe1.X;
			float num3 = num * ps1.X + num2 * ps1.Y;
			float num4 = pe2.Y - ps2.Y;
			float num5 = ps2.X - pe2.X;
			float num6 = num4 * ps2.X + num5 * ps2.Y;
			float num7 = num * num5 - num4 * num2;
			if (num7 == 0f)
			{
				throw new Exception("Lines are parallel");
			}
			return new Vector2((num5 * num3 - num2 * num6) / num7, (num * num6 - num4 * num3) / num7);
		}

		private float GetLineLength(List<Vector3> Line)
		{
			float num = 0f;
			for (int i = 1; i < Line.Count; i++)
			{
				num += (Line[i] - Line[i - 1]).Length;
			}
			return num;
		}

		private List<Vector3> CreateGuideLine(List<Vector3> TargetPoints, float ArrowWidth)
		{
			PointF[] array = new PointF[TargetPoints.Count];
			for (int i = 0; i < TargetPoints.Count; i++)
			{
				ref PointF reference = ref array[i];
				reference = new PointF(TargetPoints[i].X, TargetPoints[i].Z);
			}
			GetCurveControlPoints(array, out var firstControlPoints, out var secondControlPoints);
			List<Vector3> list = new List<Vector3>();
			for (int j = 0; j < firstControlPoints.Length; j++)
			{
				Vector2 vector = new Vector2(array[j].X, array[j].Y);
				Vector2 vector2 = new Vector2(array[j + 1].X, array[j + 1].Y);
				Vector2 control = new Vector2(firstControlPoints[j].X, firstControlPoints[j].Y);
				Vector2 control2 = new Vector2(secondControlPoints[j].X, secondControlPoints[j].Y);
				float length = (vector2 - vector).Length;
				float num = length / SegmentLength;
				float num2 = 1f / num;
				if (j == 0)
				{
					float num3 = ArrowWidth * 12f / length;
					float num4 = num3;
					float num5 = num3 / 20f;
					for (int k = 1; k < 20; k++)
					{
						Vector2 cubicBezierPoint = GetCubicBezierPoint(num5 * (float)k, vector, control, control2, vector2);
						list.Add(new Vector3(cubicBezierPoint.X, 0f, cubicBezierPoint.Y));
					}
					for (float num6 = num4; num6 <= 1f; num6 += num2)
					{
						Vector2 cubicBezierPoint2 = GetCubicBezierPoint(num6, vector, control, control2, vector2);
						list.Add(new Vector3(cubicBezierPoint2.X, 0f, cubicBezierPoint2.Y));
					}
				}
				else
				{
					for (float num7 = 0f; num7 <= 1f; num7 += num2)
					{
						Vector2 cubicBezierPoint3 = GetCubicBezierPoint(num7, vector, control, control2, vector2);
						list.Add(new Vector3(cubicBezierPoint3.X, 0f, cubicBezierPoint3.Y));
					}
				}
			}
			return list;
		}

		private static float[] GetFirstControlPoints(float[] rhs)
		{
			int num = rhs.Length;
			float[] array = new float[num];
			float[] array2 = new float[num];
			float num2 = 2f;
			array[0] = rhs[0] / num2;
			for (int i = 1; i < num; i++)
			{
				array2[i] = 1f / num2;
				num2 = ((i < num - 1) ? 4f : 3.5f) - array2[i];
				array[i] = (rhs[i] - array[i - 1]) / num2;
			}
			for (int j = 1; j < num; j++)
			{
				array[num - j - 1] -= array2[num - j] * array[num - j];
			}
			return array;
		}

		public Vector2 GetCubicBezierPoint(float t, Vector2 Start, Vector2 Control1, Vector2 Control2, Vector2 End)
		{
			float num = t * t * t;
			float num2 = t * t;
			float num3 = 3f * (Control1.X - Start.X);
			float num4 = 3f * (Control1.Y - Start.Y);
			float num5 = 3f * (Control2.X - Control1.X) - num3;
			float num6 = 3f * (Control2.Y - Control1.Y) - num4;
			float num7 = End.X - Start.X - num3 - num5;
			float num8 = End.Y - Start.Y - num4 - num6;
			float x = num7 * num + num5 * num2 + num3 * t + Start.X;
			float y = num8 * num + num6 * num2 + num4 * t + Start.Y;
			return new Vector2(x, y);
		}

		public static void GetCurveControlPoints(PointF[] knots, out PointF[] firstControlPoints, out PointF[] secondControlPoints)
		{
			if (knots == null)
			{
				throw new ArgumentNullException("knots");
			}
			int num = knots.Length - 1;
			if (num < 1)
			{
				throw new ArgumentException("At least two knot points required", "knots");
			}
			if (num == 1)
			{
				firstControlPoints = new PointF[1];
				firstControlPoints[0].X = (2f * knots[0].X + knots[1].X) / 3f;
				firstControlPoints[0].Y = (2f * knots[0].Y + knots[1].Y) / 3f;
				secondControlPoints = new PointF[1];
				secondControlPoints[0].X = 2f * firstControlPoints[0].X - knots[0].X;
				secondControlPoints[0].Y = 2f * firstControlPoints[0].Y - knots[0].Y;
				return;
			}
			float[] array = new float[num];
			for (int i = 1; i < num - 1; i++)
			{
				array[i] = 4f * knots[i].X + 2f * knots[i + 1].X;
			}
			array[0] = knots[0].X + 2f * knots[1].X;
			array[num - 1] = (8f * knots[num - 1].X + knots[num].X) / 2f;
			float[] firstControlPoints2 = GetFirstControlPoints(array);
			for (int j = 1; j < num - 1; j++)
			{
				array[j] = 4f * knots[j].Y + 2f * knots[j + 1].Y;
			}
			array[0] = knots[0].Y + 2f * knots[1].Y;
			array[num - 1] = (8f * knots[num - 1].Y + knots[num].Y) / 2f;
			float[] firstControlPoints3 = GetFirstControlPoints(array);
			firstControlPoints = new PointF[num];
			secondControlPoints = new PointF[num];
			for (int k = 0; k < num; k++)
			{
				ref PointF reference = ref firstControlPoints[k];
				reference = new PointF(firstControlPoints2[k], firstControlPoints3[k]);
				if (k < num - 1)
				{
					ref PointF reference2 = ref secondControlPoints[k];
					reference2 = new PointF(2f * knots[k + 1].X - firstControlPoints2[k + 1], 2f * knots[k + 1].Y - firstControlPoints3[k + 1]);
				}
				else
				{
					ref PointF reference3 = ref secondControlPoints[k];
					reference3 = new PointF((knots[num].X + firstControlPoints2[num - 1]) / 2f, (knots[num].Y + firstControlPoints3[num - 1]) / 2f);
				}
			}
		}

		private void AddExtraPoint(List<Vector3> Points)
		{
			Vector3 vector = Points[0];
			Vector3 vector2 = Points[1];
			Random random = new Random((int)(vector.X + vector.Z + vector2.X + vector2.Z));
			Vector3 vector3 = vector2 - vector;
			float length = vector3.Length;
			if (!(length < 150f))
			{
				vector3.Normalize();
				Vector3 vector4 = new Vector3(0f - vector3.Z, 0f, vector3.X);
				Vector3 item = vector + vector3 * (length / 2f);
				float num = length / 5f;
				float num2 = random.Next((int)num);
				if (num2 < 10f)
				{
					num2 = 10f;
				}
				if (random.Next(2) == 1)
				{
					num2 *= -1f;
				}
				item += vector4 * num2;
				Points.Insert(1, item);
			}
		}

		public void SetColour(Color Col)
		{
			if (FillArrow != null)
			{
				FillArrow.SetColour(Col);
			}
		}

		public void SetGreen()
		{
			if (FillArrow != null)
			{
				FillArrow.SetColour(Color.FromArgb(255, 47, 65, 38));
			}
		}

		public void SetRed()
		{
			if (FillArrow != null)
			{
				FillArrow.SetColour(Color.FromArgb(255, 121, 27, 27));
			}
		}

		public void Render()
		{
			if (Visible)
			{
				if (ShadowArrow != null)
				{
					ShadowArrow.Render();
				}
				if (OutlineArrow != null)
				{
					OutlineArrow.Render();
				}
				if (FillArrow != null)
				{
					FillArrow.Render();
				}
			}
		}

		internal void Dispose()
		{
			if (ShadowArrow != null)
			{
				ShadowArrow.Dispose();
			}
			if (OutlineArrow != null)
			{
				OutlineArrow.Dispose();
			}
			if (FillArrow != null)
			{
				FillArrow.Dispose();
			}
			ShadowArrow = null;
			OutlineArrow = null;
			FillArrow = null;
		}

		internal void CreateHeightOffsets()
		{
			if (OutlineArrow != null)
			{
				OutlineArrow.CreateHeightOffsets(GuideLine);
			}
			if (FillArrow != null)
			{
				FillArrow.CreateHeightOffsets(GuideLine);
			}
		}
	}
}