using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI.Map
{
	public class MapTextManager
	{
		public MapTextManager(Sovereignty Game)
		{
			this.Game = Game;
			this.CurrentTexts = new List<MapText>();
		}

		public void DisposeText(MapText Text)
		{
			this.CurrentTexts.Remove(Text);
			Text.Dispose();
		}

		public MapText CreateText(List<int> ProvinceIDs, GameText Text)
		{
			if (ProvinceIDs.Count == 0)
			{
				return null;
			}
			List<ProvinceData> list = new List<ProvinceData>();
			foreach (int num in ProvinceIDs)
			{
				list.Add(this.Game.Data.ProvincesByID[num] as ProvinceData);
			}
			RegionOutlineRenderer regionOutlineRenderer = new RegionOutlineRenderer(this.Game);
			regionOutlineRenderer.GetRegionPoints(list);
			RectangleF rectangleF = list[0].Outline.GetBoundingBox();
			for (int i = 1; i < list.Count; i++)
			{
				rectangleF = RectangleF.Union(rectangleF, list[i].Outline.GetBoundingBox());
			}
			int num2 = 0;
			int num3 = 0;
			float num4 = 30f;
			for (float num5 = rectangleF.Left; num5 <= rectangleF.Right; num5 += num4)
			{
				num2++;
			}
			for (float num6 = rectangleF.Top; num6 <= rectangleF.Bottom; num6 += num4)
			{
				num3++;
			}
			int[,] array = new int[num2, num3];
			int num7 = 0;
			int num8 = 0;
			for (float num9 = rectangleF.Left; num9 <= rectangleF.Right; num9 += num4)
			{
				for (float num10 = rectangleF.Top; num10 <= rectangleF.Bottom; num10 += num4)
				{
					bool flag = false;
					foreach (ProvinceData provinceData in list)
					{
						if (provinceData.Outline.PointInside(new PointF(num9, num10)))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						num8++;
					}
					else
					{
						array[num7, num8] = 1;
						num8++;
					}
				}
				num7++;
				num8 = 0;
			}
			for (int j = 0; j < num2; j++)
			{
				for (int k = 0; k < num3; k++)
				{
					if (array[j, k] != 0)
					{
						bool flag2 = false;
						bool flag3 = false;
						bool flag4 = false;
						bool flag5 = false;
						if (j == 0 || array[j - 1, k] == 0)
						{
							flag2 = true;
						}
						if (j == num2 - 1 || array[j + 1, k] == 0)
						{
							flag3 = true;
						}
						if (k == 0 || array[j, k - 1] == 0)
						{
							flag4 = true;
						}
						if (k == num3 - 1 || array[j, k + 1] == 0)
						{
							flag5 = true;
						}
						if (flag2 || flag3 || flag4 || flag5)
						{
							array[j, k] = 2;
						}
					}
				}
			}
			bool flag6 = true;
			int num11 = 3;
			while (flag6)
			{
				List<Point> list2 = new List<Point>();
				flag6 = false;
				for (int l = 0; l < num2; l++)
				{
					for (int m = 0; m < num3; m++)
					{
						if (array[l, m] == 1)
						{
							bool flag7 = false;
							bool flag8 = false;
							bool flag9 = false;
							bool flag10 = false;
							if (array[l - 1, m] == num11 - 1)
							{
								flag7 = true;
							}
							if (array[l + 1, m] == num11 - 1)
							{
								flag8 = true;
							}
							if (array[l, m - 1] == num11 - 1)
							{
								flag9 = true;
							}
							if (array[l, m + 1] == num11 - 1)
							{
								flag10 = true;
							}
							if (flag7 || flag8 || flag9 || flag10)
							{
								list2.Add(new Point(l, m));
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					flag6 = true;
					foreach (Point point in list2)
					{
						array[point.X, point.Y] = num11;
					}
					num11++;
				}
			}
			num7 = 0;
			num8 = 0;
			List<Vector2> list3 = new List<Vector2>();
			for (float num12 = rectangleF.Left; num12 <= rectangleF.Right; num12 += num4)
			{
				for (float num13 = rectangleF.Top; num13 <= rectangleF.Bottom; num13 += num4)
				{
					if (array[num7, num8] != num11 - 1)
					{
						num8++;
					}
					else
					{
						list3.Add(new Vector2(num12, num13));
						num8++;
					}
				}
				num7++;
				num8 = 0;
			}
			List<TextLine> list4 = new List<TextLine>();
			foreach (Vector2 vector in list3)
			{
				List<TextLine> list5 = new List<TextLine>();
				for (float num14 = -80f; num14 <= 80f; num14 += 10f)
				{
					float num15 = this.DegreesToRadians(num14);
					Vector2 zero = Vector2.Zero;
					zero.X += (float)(Math.Cos((double)num15) * 50.0);
					zero.Y += (float)(Math.Sin((double)num15) * 50.0);
					Vector2 vector2 = vector;
					Vector2 vector3 = vector;
					while (this.PointInsidePolygons(list, vector2.X, vector2.Y))
					{
						vector2 += zero;
					}
					vector2 -= zero;
					while (this.PointInsidePolygons(list, vector3.X, vector3.Y))
					{
						vector3 -= zero;
					}
					vector3 += zero;
					list5.Add(new TextLine
					{
						StartPoint = vector3,
						EndPoint = vector2,
						LineLength = (vector3 - vector2).Length,
						Angle = num14
					});
				}
				float BestLength = list5.Max((TextLine x) => x.LineLength);
				list4.Add(list5.First((TextLine X) => X.LineLength == BestLength));
			}
			float FinalBestLength = list4.Max((TextLine x) => x.LineLength);
			TextLine textLine = list4.First((TextLine X) => X.LineLength == FinalBestLength);
			int num16 = 25;
			num16 += 5 * list.Count;
			if (num16 > 100)
			{
				num16 = 100;
			}
			Vector2 vector4 = textLine.EndPoint - textLine.StartPoint;
			float num17 = vector4.Length / 2f;
			vector4.Normalize();
			vector4 *= num17;
			Vector2 vector5 = textLine.StartPoint + vector4;
			float num18 = textLine.Angle - 90f;
			num18 = this.DegreesToRadians(num18);
			Vector2 zero2 = Vector2.Zero;
			zero2.X += (float)(Math.Cos((double)num18) * 50.0);
			zero2.Y += (float)(Math.Sin((double)num18) * 50.0);
			float num19 = 0f;
			float num20 = 0f;
			Vector2 vector6 = vector5;
			Vector2 vector7 = vector5;
			while (this.PointInsidePolygons(list, vector6.X, vector6.Y))
			{
				vector6 += zero2;
				num19 += 50f;
			}
			while (this.PointInsidePolygons(list, vector7.X, vector7.Y))
			{
				vector7 -= zero2;
				num20 += 50f;
			}
			zero2.Normalize();
			if (num19 >= num20)
			{
				vector5 += zero2 * ((float)num16 * 1.5f);
			}
			else
			{
				vector5 -= zero2 * ((float)num16 * 1.5f);
			}
			textLine.ControlPoint = vector5;
			MapText mapText = new MapText(this.Game, textLine, (int)((float)num16 * 1.5f), Text);
			this.CurrentTexts.Add(mapText);
			return mapText;
		}

		public bool PointInsidePolygons(List<ProvinceData> Provinces, float X, float Y)
		{
			bool flag = false;
			foreach (ProvinceData provinceData in Provinces)
			{
				if (provinceData.Outline.PointInside(new PointF(X, Y)))
				{
					flag = true;
					break;
				}
			}
			return flag;
		}

		public float DegreesToRadians(float Value)
		{
			return Value * 3.1415927f / 180f;
		}

		public void Render()
		{
			foreach (MapText mapText in this.CurrentTexts)
			{
				mapText.Render();
			}
		}

		private Sovereignty Game;

		private List<MapText> CurrentTexts;
	}
}
