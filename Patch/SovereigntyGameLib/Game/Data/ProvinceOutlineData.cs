using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using OpenTK;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.Data
{
	public class ProvinceOutlineData
	{
		public ProvinceOutlineData(BinaryReader r)
		{
			this.RegionName = r.ReadString();
			this.RegionID = r.ReadInt32();
		}

		public RectangleF GetBoundingBox()
		{
			RectangleF rectangleF = this.BoundingBoxes[0];
			for (int i = 1; i < this.BoundingBoxes.Count; i++)
			{
				rectangleF = RectangleF.Union(rectangleF, this.BoundingBoxes[i]);
			}
			return rectangleF;
		}

		public void RenderIcon(string Filename, Bitmap MapImage)
		{
			Rectangle rectangle = Rectangle.Round(this.GetFullBoundingBox());
			int num = 0;
			int num2 = 0;
			int num3 = rectangle.Width;
			int num4 = rectangle.Height;
			if (num3 > num4)
			{
				num2 = (num3 - num4) / 2;
				num4 = num3;
			}
			else
			{
				num = (num4 - num3) / 2;
				num3 = num4;
			}
			Bitmap bitmap = new Bitmap(num3, num4);
			for (int i = rectangle.X; i < rectangle.X + rectangle.Width; i++)
			{
				for (int j = rectangle.Y; j < rectangle.Y + rectangle.Height; j++)
				{
					if (this.PointInside(new PointF((float)i, (float)j)))
					{
						bitmap.SetPixel(num + i - rectangle.X, num2 + (rectangle.Height - 1) - (j - rectangle.Y), MapImage.GetPixel(i, MapImage.Height - j));
					}
				}
			}
			Bitmap bitmap2 = ProvinceOutlineData.ResizeImage(bitmap, new Size(32, 32));
			bitmap2.Save(Filename);
			bitmap2.Dispose();
			bitmap.Dispose();
		}

		public static Bitmap ResizeImage(Bitmap imgToResize, Size size)
		{
			Bitmap bitmap = new Bitmap(size.Width, size.Height);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(imgToResize, 0, 0, size.Width, size.Height);
			}
			return bitmap;
		}

		private RectangleF GetFullBoundingBox()
		{
			RectangleF rectangleF = this.BoundingBoxes[0];
			for (int i = 1; i < this.BoundingBoxes.Count; i++)
			{
				rectangleF = RectangleF.Union(rectangleF, this.BoundingBoxes[i]);
			}
			return rectangleF;
		}

		public bool PointInside(PointF p)
		{
			for (int i = 0; i < this.VertexPoints.Count; i++)
			{
				if (this.BoundingBoxes[i].Contains(p) && GeomUtils.IsPointInPolygon(p, this.VertexPoints[i]))
				{
					return true;
				}
			}
			return false;
		}

		public void LoadData(BinaryReader r)
		{
			this.VertexPoints = new List<PointF[]>();
			this.VertexPointData = new List<VertexPoint[]>();
			this.BoundingBoxes = new List<RectangleF>();
			this.MeshVertices = new List<Vector3>();
			byte b = r.ReadByte();
			int num = 0;
			while (b != 0)
			{
				int num2 = r.ReadInt32();
				PointF[] array = new PointF[num2];
				VertexPoint[] array2 = new VertexPoint[num2];
				for (int i = 0; i < num2; i++)
				{
					array[i] = new PointF(r.ReadSingle(), r.ReadSingle());
					array[i].Y = array[i].Y;
					array2[i] = new VertexPoint();
					array2[i].Point = array[i];
					array2[i].ProvinceID = this.RegionID;
					array2[i].PolygonID = num;
					int num3 = r.ReadInt32();
					for (int j = 0; j < num3; j++)
					{
						int num4 = r.ReadInt32();
						if (num4 <= 262)
						{
							array2[i].AttachedProvinces.Add(num4);
						}
					}
				}
				this.VertexPoints.Add(array);
				this.VertexPointData.Add(array2);
				b = r.ReadByte();
				num++;
			}
			foreach (VertexPoint[] array3 in this.VertexPointData)
			{
				for (int k = 0; k < array3.Length; k++)
				{
					VertexPoint vertexPoint;
					if (k < array3.Length - 1)
					{
						vertexPoint = array3[k + 1];
					}
					else
					{
						vertexPoint = array3[0];
					}
					VertexPoint vertexPoint2;
					if (k > 0)
					{
						vertexPoint2 = array3[k - 1];
					}
					else
					{
						vertexPoint2 = array3[array3.Length - 1];
					}
					array3[k].NextPoint = vertexPoint;
					array3[k].PrevPoint = vertexPoint2;
				}
			}
			int num5 = r.ReadInt32();
			if (num5 == 0)
			{
				return;
			}
			int num6 = num5 / 3;
			for (int l = 0; l < num5; l++)
			{
				PointF pointF = new PointF(r.ReadSingle(), r.ReadSingle());
				this.MeshVertices.Add(new Vector3(pointF.X, 0f, pointF.Y));
			}
			this.CreateBoundingBoxes();
		}

		private void CreateBoundingBoxes()
		{
			foreach (PointF[] array in this.VertexPoints)
			{
				float num = array[0].X;
				float num2 = array[0].X;
				float num3 = array[0].Y;
				float num4 = array[0].Y;
				for (int i = 1; i < array.Length; i++)
				{
					PointF pointF = array[i];
					num = Math.Min(pointF.X, num);
					num2 = Math.Max(pointF.X, num2);
					num3 = Math.Min(pointF.Y, num3);
					num4 = Math.Max(pointF.Y, num4);
				}
				this.BoundingBoxes.Add(new RectangleF(num, num3, num2 - num, num4 - num3));
			}
		}

		public VertexPoint GetPointAt(PointF p)
		{
			VertexPoint vertexPoint = null;
			foreach (VertexPoint[] array in this.VertexPointData)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].Point == p)
					{
						vertexPoint = array[i];
						break;
					}
				}
			}
			return vertexPoint;
		}

		internal void Dispose()
		{
		}

		public ProvinceData Province;

		public SeaZoneData Zone;

		public string RegionName;

		public int RegionID;

		private List<PointF[]> VertexPoints;

		public List<VertexPoint[]> VertexPointData;

		private List<RectangleF> BoundingBoxes;

		public List<Vector3> MeshVertices;
	}
}
