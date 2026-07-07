// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Map.RegionOutlineRenderer
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	public class RegionOutlineRenderer
	{
		public List<MapOutlineVertex[]> Vertices;

		public List<GLVertexBuffer> Buffers;

		private List<int> TriangleCounts;

		private static GLShader ShaderEffect;

		public Vector4 Colour;

		private Sovereignty Game;

		public float LineWidth = 7f;

		public string ProvinceName;

		public RegionOutlineRenderer(Sovereignty Game)
		{
			this.Game = Game;
			if (ShaderEffect == null)
			{
				ShaderEffect = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Outline.vert", "Data\\Shaders\\Outline.frag", UsesCamera: true);
			}
			Colour = new Vector4(0.4f, 0.22f, 0.35f, 1f);
		}

		public void Update(List<string> RegionNames)
		{
			if (Buffers == null)
			{
				Buffers = new List<GLVertexBuffer>();
				TriangleCounts = new List<int>();
				Vertices = new List<MapOutlineVertex[]>();
			}
			foreach (GLVertexBuffer buffer in Buffers)
			{
				buffer.Dispose();
			}
			Buffers.Clear();
			TriangleCounts.Clear();
			Vertices.Clear();
			List<List<ProvinceData>> list = new List<List<ProvinceData>>();
			List<ProvinceData> list2 = Game.Data.ActiveProvinces.Values.Where((ProvinceData x) => RegionNames.Contains(x.Name)).ToList();
			while (list2.Count > 0)
			{
				List<ProvinceData> list3 = new List<ProvinceData>();
				list3.Add(list2[0]);
				list2.RemoveAt(0);
				bool flag = true;
				while (flag)
				{
					flag = false;
					foreach (ProvinceData item in list3.ToList())
					{
						foreach (ProvinceLink adjacentProvince in item.AdjacentProvinces)
						{
							ProvinceData provinceData = Game.Data.ProvincesByID[adjacentProvince.LinkedProvinceID] as ProvinceData;
							if (!adjacentProvince.IgnoreForBorders && adjacentProvince.LinkType != ProvinceLinkTypes.River && adjacentProvince.LinkType != ProvinceLinkTypes.Bridge && !list3.Contains(provinceData) && RegionNames.Contains(provinceData.Name))
							{
								list2.Remove(provinceData);
								list3.Add(provinceData);
								flag = true;
							}
						}
					}
				}
				list.Add(list3);
			}
			foreach (List<ProvinceData> item2 in list)
			{
				List<VertexPoint> regionPoints = GetRegionPoints(item2);
				List<PointF> list4 = new List<PointF>();
				if (regionPoints.Count == 0)
				{
					continue;
				}
				foreach (VertexPoint item3 in regionPoints)
				{
					Vector2 vector = new Vector2(item3.PrevPoint.Point.X, item3.PrevPoint.Point.Y);
					Vector2 vector2 = new Vector2(item3.Point.X, item3.Point.Y);
					Vector2 vector3 = new Vector2(item3.NextPoint.Point.X, item3.NextPoint.Point.Y);
					Vector2 vector4 = vector2 - vector;
					_ = vector2 - vector3;
					Vector2 vector5 = new Vector2(0f - vector4.Y, vector4.X) * -1f;
					vector5.Normalize();
					vector5 *= LineWidth;
					vector2 += vector5;
					list4.Add(new PointF(vector2.X, vector2.Y));
				}
				MapOutlineVertex[] array = new MapOutlineVertex[(regionPoints.Count + 1) * 2];
				int num = 0;
				for (int num2 = 0; num2 < regionPoints.Count; num2++)
				{
					array[num].Position = new Vector3(regionPoints[num2].Point.X, 0f, regionPoints[num2].Point.Y);
					array[num++].ExtraData.X = 1f;
					array[num].Position = new Vector3(list4[num2].X, 0f, list4[num2].Y);
					array[num++].ExtraData.X = 0f;
				}
				array[num].Position = new Vector3(regionPoints[0].Point.X, 0f, regionPoints[0].Point.Y);
				array[num++].ExtraData.X = 1f;
				array[num].Position = new Vector3(list4[0].X, 0f, list4[0].Y);
				array[num++].ExtraData.X = 0f;
				Vertices.Add(array);
			}
			foreach (MapOutlineVertex[] vertex in Vertices)
			{
				TriangleCounts.Add(vertex.Length - 2);
				GLVertexBuffer gLVertexBuffer = new GLVertexBuffer(MapOutlineVertex.GetFormat(ShaderEffect.GetID()));
				gLVertexBuffer.SetBufferData(vertex, BufferUsageHint.StaticDraw);
				Buffers.Add(gLVertexBuffer);
			}
		}

		public void Render()
		{
			if (Buffers == null)
			{
				return;
			}
			int num = 0;
			ShaderEffect.SetVector4("Colour", ref Colour);
			foreach (GLVertexBuffer buffer in Buffers)
			{
				buffer.SetActive();
				ShaderEffect.SetActive();
				GL.DrawArrays(PrimitiveType.TriangleStrip, 0, TriangleCounts[num] + 2);
				ShaderEffect.SetInactive();
				buffer.SetInactive();
				num++;
			}
		}

		public List<VertexPoint> GetRegionPoints(List<ProvinceData> RegionProvinces)
		{
			List<VertexPoint> list = new List<VertexPoint>();
			ProvinceData provinceData = null;
			foreach (ProvinceData RegionProvince in RegionProvinces)
			{
				if (RegionProvince.AdjacentZones.Count > 0)
				{
					provinceData = RegionProvince;
					break;
				}
				foreach (ProvinceLink adjacentProvince in RegionProvince.AdjacentProvinces)
				{
					if (adjacentProvince.LinkType == ProvinceLinkTypes.River || adjacentProvince.LinkType == ProvinceLinkTypes.Bridge)
					{
						provinceData = RegionProvince;
						break;
					}
					if (!RegionProvinces.Contains(Game.Data.ProvincesByID[adjacentProvince.LinkedProvinceID] as ProvinceData))
					{
						provinceData = RegionProvince;
						break;
					}
				}
			}
			if (provinceData == null)
			{
				return list;
			}
			VertexPoint vertexPoint = null;
			foreach (VertexPoint[] vertexPointDatum in provinceData.Outline.VertexPointData)
			{
				VertexPoint[] array = vertexPointDatum;
				foreach (VertexPoint vertexPoint2 in array)
				{
					if (vertexPoint2.AttachedProvinces.Count == 1)
					{
						vertexPoint = vertexPoint2;
						break;
					}
					if (vertexPoint2.AttachedProvinces.Count > 2)
					{
						continue;
					}
					foreach (int attachedProvince in vertexPoint2.AttachedProvinces)
					{
						if (Game.Data.ProvinceOutlines[attachedProvince].Province != null)
						{
							ProvinceData province = Game.Data.ProvinceOutlines[attachedProvince].Province;
							if (!RegionProvinces.Contains(province))
							{
								vertexPoint = vertexPoint2;
								break;
							}
						}
					}
				}
				if (vertexPoint != null)
				{
					break;
				}
			}
			if (vertexPoint == null)
			{
				return list;
			}
			list.Add(vertexPoint);
			list.Add(vertexPoint.NextPoint);
			VertexPoint vertexPoint3 = vertexPoint.NextPoint;
			while (vertexPoint3 != vertexPoint)
			{
				List<VertexPoint> sharedPoints = GetSharedPoints(vertexPoint3);
				List<PolygonSegment> list2 = new List<PolygonSegment>();
				bool flag = false;
				foreach (VertexPoint item in sharedPoints)
				{
					ProvinceData province2 = Game.Data.ProvinceOutlines[item.ProvinceID].Province;
					if (RegionProvinces.Contains(province2))
					{
						if (list.Count > 2 && item.NextPoint == vertexPoint)
						{
							flag = true;
						}
						if (list.Count > 2 && item.PrevPoint == vertexPoint)
						{
							flag = true;
						}
						if (!list.Contains(item.NextPoint))
						{
							PolygonSegment polygonSegment = new PolygonSegment();
							polygonSegment.Point1 = item;
							polygonSegment.Point2 = item.NextPoint;
							list2.Add(polygonSegment);
						}
						if (!list.Contains(item.PrevPoint))
						{
							PolygonSegment polygonSegment2 = new PolygonSegment();
							polygonSegment2.Point1 = item;
							polygonSegment2.Point2 = item.PrevPoint;
							list2.Add(polygonSegment2);
						}
					}
				}
				if (flag)
				{
					break;
				}
				if (list2.Count == 0)
				{
					return new List<VertexPoint>();
				}
				if (list2.Count == 1)
				{
					list.Add(list2[0].Point2);
					vertexPoint3 = list2[0].Point2;
					continue;
				}
				bool flag2 = true;
				List<VertexPoint> list3 = new List<VertexPoint>();
				foreach (PolygonSegment item2 in list2)
				{
					bool flag3 = false;
					bool flag4 = false;
					foreach (int attachedProvince2 in item2.Point2.AttachedProvinces)
					{
						ProvinceData province3 = Game.Data.ProvinceOutlines[attachedProvince2].Province;
						if (RegionProvinces.Contains(province3))
						{
							flag3 = true;
						}
						else
						{
							flag4 = true;
						}
					}
					if (flag3 && flag4)
					{
						list.Add(item2.Point2);
						vertexPoint3 = item2.Point2;
						flag2 = false;
						break;
					}
					if (flag3 && item2.Point2.AttachedProvinces.Count == 1)
					{
						list.Add(item2.Point2);
						vertexPoint3 = item2.Point2;
						flag2 = false;
						break;
					}
					if (flag3)
					{
						list3.Add(item2.Point2);
					}
				}
				if (!flag2)
				{
					continue;
				}
				if (list3.Count == 0)
				{
					break;
				}
				foreach (VertexPoint p in list3)
				{
					Func<VertexPoint, bool> predicate = (VertexPoint x) => PointsMatch(x, p);
					if (list.Count(predicate) == 0)
					{
						list.Add(p);
						vertexPoint3 = p;
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					break;
				}
			}
			return list;
		}

		private bool PointsMatch(VertexPoint P1, VertexPoint P2)
		{
			return P1.Point == P2.Point;
		}

		private List<VertexPoint> GetSharedPoints(VertexPoint LastPoint)
		{
			List<VertexPoint> list = new List<VertexPoint>();
			list.Add(LastPoint);
			ProvinceOutlineData provinceOutlineData = Game.Data.ProvinceOutlines[LastPoint.ProvinceID];
			foreach (ProvinceLink adjacentProvince in provinceOutlineData.Province.AdjacentProvinces)
			{
				ProvinceData provinceData = Game.Data.ProvincesByID[adjacentProvince.LinkedProvinceID] as ProvinceData;
				if (provinceData.Outline != null)
				{
					VertexPoint pointAt = provinceData.Outline.GetPointAt(LastPoint.Point);
					if (pointAt != null)
					{
						list.Add(pointAt);
					}
				}
			}
			return list;
		}

		internal void Dispose()
		{
			foreach (GLVertexBuffer buffer in Buffers)
			{
				buffer.Dispose();
			}
			Buffers.Clear();
			Buffers = null;
		}
	}
}