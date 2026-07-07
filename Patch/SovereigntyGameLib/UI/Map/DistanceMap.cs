using System;
using System.Collections.Generic;
using System.Drawing;

namespace SovereigntyTK.UI.Map
{
	public class DistanceMap
	{
		public float this[int X, int Y]
		{
			get
			{
				return this.Distances[X - this.Bounds.X, Y - this.Bounds.Y];
			}
		}

		public DistanceMap(Rectangle Bounds)
		{
			this.Bounds = Bounds;
			this.Distances = new float[Bounds.Width, Bounds.Height];
		}

		public List<Point> GetAdjacentTiles(int X, int Y)
		{
			List<Point> list = new List<Point>();
			for (int i = 0; i < 6; i++)
			{
				int num;
				int num2;
				if (X % 2 == 1)
				{
					num = X + this.HEX_X_ODD[i];
					num2 = Y + this.HEX_Y_ODD[i];
				}
				else
				{
					num = X + this.HEX_X_EVEN[i];
					num2 = Y + this.HEX_Y_EVEN[i];
				}
				if (num >= 0 && num2 >= 0 && num < this.Bounds.Width && num2 < this.Bounds.Height)
				{
					list.Add(new Point(num, num2));
				}
			}
			return list;
		}

		public void AddAdjacentTiles(Point p, Point[] Tiles, ref int Index, bool AdjustValues = false)
		{
			if (AdjustValues)
			{
				p.X -= this.Bounds.X;
				p.Y -= this.Bounds.Y;
			}
			foreach (Point point in this.GetAdjacentTiles(p.X, p.Y))
			{
				Tiles[Index++] = point;
				if (Index >= 5000)
				{
					Index = 0;
				}
			}
		}

		public void GenerateMap(List<Point> TargetPoints, DistanceMapValidDelegate TileValidFunc = null, DistanceMapValidDelegate TargetTileValidfunc = null, DistanceDelegate DistanceFunc = null)
		{
			this.ClearMap();
			Point[] array = new Point[5000];
			int num = 0;
			foreach (Point point in TargetPoints)
			{
				this.Distances[point.X - this.Bounds.X, point.Y - this.Bounds.Y] = 0f;
				this.AddAdjacentTiles(point, array, ref num, true);
			}
			bool flag = true;
			while (flag)
			{
				flag = false;
				Point[] array2 = new Point[5000];
				num = 0;
				foreach (Point point2 in array)
				{
					if (point2.X == 0)
					{
						break;
					}
					if (this.Distances[point2.X, point2.Y] == -1f && (TileValidFunc == null || TileValidFunc(new Point(point2.X + this.Bounds.X, point2.Y + this.Bounds.Y))))
					{
						float num2 = float.MaxValue;
						for (int j = 0; j < 6; j++)
						{
							int num3 = point2.X;
							int num4 = point2.Y;
							if (point2.X % 2 == 1)
							{
								num3 += this.HEX_X_ODD[j];
								num4 += this.HEX_Y_ODD[j];
							}
							else
							{
								num3 += this.HEX_X_EVEN[j];
								num4 += this.HEX_Y_EVEN[j];
							}
							if (num3 >= 0 && num4 >= 0 && num3 < this.Bounds.Width && num4 < this.Bounds.Height && this.Distances[num3, num4] != -1f && (TargetTileValidfunc == null || TargetTileValidfunc(new Point(num3 + this.Bounds.X, num4 + this.Bounds.Y))) && this.Distances[num3, num4] < num2)
							{
								num2 = this.Distances[num3, num4];
							}
						}
						if (num2 != 3.4028235E+38f)
						{
							float num5 = 1f;
							if (DistanceFunc != null)
							{
								num5 = DistanceFunc(new Point(point2.X + this.Bounds.X, point2.Y + this.Bounds.Y));
							}
							float num6 = num2 + num5;
							if (num6 <= this.MaxDist)
							{
								this.Distances[point2.X, point2.Y] = num6;
								this.AddAdjacentTiles(point2, array2, ref num, false);
								flag = true;
							}
						}
					}
				}
				array = array2;
			}
		}

		public void ClearMap()
		{
			for (int i = 0; i < this.Bounds.Width; i++)
			{
				for (int j = 0; j < this.Bounds.Height; j++)
				{
					this.Distances[i, j] = -1f;
				}
			}
		}

		private int[] HEX_Y_EVEN = new int[] { -1, -1, 0, 1, 0, -1 };

		private int[] HEX_X_EVEN = new int[] { 0, 1, 1, 0, -1, -1 };

		private int[] HEX_Y_ODD = new int[] { -1, 0, 1, 1, 1, 0 };

		private int[] HEX_X_ODD = new int[] { 0, 1, 1, 0, -1, -1 };

		private int[] ADJACENTVALUES = new int[] { 1, 2, 4, 8, 16, 32 };

		private Rectangle Bounds;

		private float[,] Distances;

		public float MaxDist = float.MaxValue;
	}
}
