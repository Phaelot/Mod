using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OpenTK;

namespace SovereigntyTK.Game.Data
{
	public class CombatMapData
	{
		public CombatMapData(Sovereignty Game, string Filename)
		{
			this.Game = Game;
			Stream stream = File.OpenRead(Filename);
			BinaryReader binaryReader = new BinaryReader(stream);
			int num = binaryReader.ReadInt32();
			if (num >= 3 && num <= 7)
			{
				int num2 = binaryReader.ReadInt32();
				int num3 = binaryReader.ReadInt32();
				int num4 = binaryReader.ReadInt32();
				for (int i = 0; i < num2; i++)
				{
					CombatTerrainData Terrain = new CombatTerrainData(binaryReader, num, Game.Data);
					if (Game.Data.CombatTerrainList.Count((KeyValuePair<int, CombatTerrainData> x) => x.Value.ID == Terrain.ID) == 0)
					{
						Game.Data.CombatTerrainList.Add(Terrain.ID, Terrain);
					}
				}
				for (int j = 0; j < num3; j++)
				{
					CombatFeatureData combatFeatureData = new CombatFeatureData(binaryReader);
					Game.Data.CombatFeatureList.Add(combatFeatureData.ID, combatFeatureData);
				}
				for (int k = 0; k < num4; k++)
				{
					CombatRoadData combatRoadData = new CombatRoadData(binaryReader);
					Game.Data.CombatRoadList.Add(combatRoadData.ID, combatRoadData);
				}
			}
			this.TilesX = binaryReader.ReadInt32();
			this.TilesY = binaryReader.ReadInt32();
			this.MapWidth = binaryReader.ReadInt32();
			this.MapHeight = binaryReader.ReadInt32();
			this.ScaleX = binaryReader.ReadSingle();
			this.ScaleY = binaryReader.ReadSingle();
			binaryReader.ReadInt32();
			this.TileData = new MapTileData[this.TilesX, this.TilesY];
			for (int l = 0; l < this.TilesX; l++)
			{
				for (int m = 0; m < this.TilesY; m++)
				{
					byte[] array = binaryReader.ReadBytes(Marshal.SizeOf(typeof(MapTileData)));
					GCHandle gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
					this.TileData[l, m] = (MapTileData)Marshal.PtrToStructure(gchandle.AddrOfPinnedObject(), typeof(MapTileData));
					gchandle.Free();
				}
			}
			binaryReader.Close();
			this.TileSizeX = 128f / this.ScaleX;
			this.TileSizeY = 128f / this.ScaleY;
			this.CalculateBorderValues();
			this.CalculateCoastValues();
		}

		private void CalculateCoastValues()
		{
			for (int i = 0; i < this.TilesX; i++)
			{
				for (int j = 0; j < this.TilesY; j++)
				{
					if (this.TileData[i, j].TerrainID != -1 && this.Game.Data.CombatTerrainList[this.TileData[i, j].TerrainID].Hascoast)
					{
						int num = 0;
						for (int k = 0; k < 6; k++)
						{
							int num2 = i;
							int num3 = j;
							if (i % 2 == 1)
							{
								num2 += this.HEX_X_ODD[k];
								num3 += this.HEX_Y_ODD[k];
							}
							else
							{
								num2 += this.HEX_X_EVEN[k];
								num3 += this.HEX_Y_EVEN[k];
							}
							if (num2 >= 0 && num3 >= 0 && num2 < this.TilesX && num3 < this.TilesY && this.TileData[num2, num3].TerrainID != -1 && this.Game.Data.CombatTerrainList[this.TileData[i, j].TerrainID].BaseType == this.Game.Data.CombatTerrainList[this.TileData[num2, num3].TerrainID].BaseType)
							{
								num += (int)Math.Pow(2.0, (double)k);
							}
						}
						this.TileData[i, j].FeatureIndexValue = num;
					}
				}
			}
		}

		private void CalculateBorderValues()
		{
			this.BorderValues = new byte[this.TilesX, this.TilesY];
			for (int i = 0; i < this.TilesX; i++)
			{
				for (int j = 0; j < this.TilesY; j++)
				{
					byte b = 0;
					for (int k = 0; k < 6; k++)
					{
						int num = i;
						int num2 = j;
						if (i % 2 == 1)
						{
							num += this.HEX_X_ODD[k];
							num2 += this.HEX_Y_ODD[k];
						}
						else
						{
							num += this.HEX_X_EVEN[k];
							num2 += this.HEX_Y_EVEN[k];
						}
						if (num >= 0 && num2 >= 0 && num < this.TilesX && num2 < this.TilesY && this.TileData[i, j].ProvinceID == this.TileData[num, num2].ProvinceID)
						{
							b += (byte)Math.Pow(2.0, (double)k);
						}
					}
					this.BorderValues[i, j] = b;
				}
			}
		}

		public RectangleF GetCameraBounds(Rectangle MapBounds)
		{
			PointF tileCoords = this.GetTileCoords(MapBounds.Left, MapBounds.Top);
			PointF tileCoords2 = this.GetTileCoords(MapBounds.Right, MapBounds.Bottom);
			tileCoords.X /= this.ScaleX;
			tileCoords.Y /= this.ScaleY;
			tileCoords2.X /= this.ScaleX;
			tileCoords2.Y /= this.ScaleY;
			tileCoords.X += 10f;
			tileCoords.Y += 10f;
			tileCoords2.X -= 10f;
			tileCoords2.Y -= 10f;
			return new RectangleF(tileCoords.X, tileCoords.Y, tileCoords2.X - tileCoords.X, tileCoords2.Y - tileCoords.Y);
		}

		public Point GetTileAtPoint(Vector3 Coords)
		{
			float num = Coords.X * this.ScaleX;
			float num2 = Coords.Z * this.ScaleY;
			Point point = default(Point);
			num += 3f;
			num2 -= 6f;
			float num3 = num / 96f;
			float num4 = (num2 - (float)((int)num3 % 2) * 56f) / 112f;
			point.X = (int)num3;
			point.Y = (int)num4;
			int num5 = (int)((num3 - (float)point.X) * 96f);
			int num6 = (int)((num4 - (float)point.Y) * 112f);
			if (num5 < 32)
			{
				if (num6 < 56)
				{
					float num7 = -1.75f;
					float num8 = 56f;
					num8 += (float)num5 * num7;
					if ((float)num6 < num8)
					{
						if (point.X % 2 == 0)
						{
							point.Y--;
						}
						point.X--;
					}
				}
				else
				{
					float num9 = 1.75f;
					float num10 = 56f;
					num10 += (float)num5 * num9;
					if ((float)num6 > num10)
					{
						if (point.X % 2 == 1)
						{
							point.Y++;
						}
						point.X--;
					}
				}
			}
			return point;
		}

		public PointF GetTileCoords(int TileX, int TileY)
		{
			float num = (float)(TileX * 96);
			float num2 = (float)(TileY * 112);
			if (TileX % 2 != 0)
			{
				num2 += 56f;
			}
			num += 64f;
			num2 += 64f;
			return new PointF(num, num2);
		}

		public Rectangle GetBattleRegion(List<Point> Tiles)
		{
			int num = 0;
			int num2 = 0;
			int num3 = this.TilesX;
			int num4 = this.TilesY;
			foreach (Point point in Tiles)
			{
				if (point.X < num3)
				{
					num3 = point.X;
				}
				if (point.X > num)
				{
					num = point.X;
				}
				if (point.Y < num4)
				{
					num4 = point.Y;
				}
				if (point.Y > num2)
				{
					num2 = point.Y;
				}
			}
			num3 -= 3;
			num4 -= 3;
			num += 3;
			num2 += 3;
			int num5 = num3 + (num - num3) / 2;
			int num6 = num4 + (num2 - num4) / 2;
			PointF tileCoords = this.GetTileCoords(num5, num6);
			tileCoords.X /= this.ScaleX;
			tileCoords.Y /= this.ScaleY;
			Vector3 camPos = this.Game.Camera.CamPos;
			this.Game.Camera.MinZoomLevel = 150f;
			this.Game.Camera.SetPosition(tileCoords.X, 300f, tileCoords.Y);
			this.Game.Camera.ViewportChanged();
			Vector3 vector = this.Game.Camera.GetTerrainIntersect(0, 0);
			Point tileAtPoint = this.Game.Data.CombatMap.GetTileAtPoint(vector);
			vector = this.Game.Camera.GetTerrainIntersect(this.Game.GetViewport().Width, this.Game.GetViewport().Height);
			Point tileAtPoint2 = this.Game.Data.CombatMap.GetTileAtPoint(vector);
			this.Game.Camera.MinZoomLevel = 500f;
			this.Game.Camera.SetPosition(camPos.X, camPos.Y, camPos.Z);
			this.Game.Camera.ViewportChanged();
			if (tileAtPoint.X < num3)
			{
				num3 = tileAtPoint.X;
			}
			if (tileAtPoint.Y < num4)
			{
				num4 = tileAtPoint.Y;
			}
			if (tileAtPoint2.X > num)
			{
				num = tileAtPoint2.X;
			}
			if (tileAtPoint2.Y > num2)
			{
				num2 = tileAtPoint2.Y;
			}
			if (num3 % 2 != 0)
			{
				num3--;
			}
			if (num % 2 != 0)
			{
				num++;
			}
			if (num3 < 0)
			{
				num3 = 0;
			}
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num >= this.TilesX)
			{
				num = this.TilesX - 1;
			}
			if (num2 >= this.TilesY)
			{
				num2 = this.TilesY - 1;
			}
			return new Rectangle(num3, num4, num - num3, num2 - num4);
		}

		public List<Point> GetRegionTiles(int RegionID)
		{
			List<Point> list = new List<Point>();
			for (int i = 0; i < this.TilesX; i++)
			{
				for (int j = 0; j < this.TilesY; j++)
				{
					if (this.TileData[i, j].ProvinceID == RegionID)
					{
						list.Add(new Point(i, j));
					}
				}
			}
			return list;
		}

		public List<Point> GetRegionDeployTiles(int RegionID, DeployZone DeployType)
		{
			List<Point> list = new List<Point>();
			for (int i = 0; i < this.TilesX; i++)
			{
				for (int j = 0; j < this.TilesY; j++)
				{
					if (this.TileData[i, j].ProvinceID == RegionID && this.TileData[i, j].Deployment == DeployType)
					{
						list.Add(new Point(i, j));
					}
				}
			}
			return list;
		}

		public List<Point> GetBorderTiles(List<Point> Tiles, int TargetProvinceID)
		{
			List<Point> list = new List<Point>();
			foreach (Point point in Tiles)
			{
				foreach (Point point2 in this.GetAdjacentTiles(point.X, point.Y))
				{
					if (this.TileData[point2.X, point2.Y].ProvinceID == TargetProvinceID)
					{
						list.Add(point);
						break;
					}
				}
			}
			return list;
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
				if (num >= 0 && num2 >= 0 && num < this.TilesX && num2 < this.TilesY)
				{
					list.Add(new Point(num, num2));
				}
			}
			return list;
		}

		public List<Point> GetattackerTiles(int TargetRegionID, int AttackerRegionID, bool Naval)
		{
			List<Point> list = new List<Point>();
			int num = 0;
			int num2 = 0;
			bool flag = TargetRegionID == AttackerRegionID;
			List<Point> list2 = new List<Point>();
			for (int i = 0; i < this.TilesX; i++)
			{
				for (int j = 0; j < this.TilesY; j++)
				{
					if (this.TileData[i, j].ProvinceID == AttackerRegionID && (!flag || this.Game.Data.CombatTerrainList[this.TileData[i, j].TerrainID].BaseType.IsAnyType(new string[] { "sea" })))
					{
						list2.Add(new Point(i, j));
					}
				}
			}
			if (flag)
			{
				return list2;
			}
			using (List<Point>.Enumerator enumerator = list2.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Point point = enumerator.Current;
					foreach (Point point2 in this.GetAdjacentTiles(point.X, point.Y))
					{
						if (this.TileData[point2.X, point2.Y].ProvinceID == TargetRegionID)
						{
							list.Add(new Point(point.X, point.Y));
							if (!Naval && !this.Game.Data.CombatTerrainList[this.TileData[point.X, point.Y].TerrainID].BaseType.IsAnyType(new string[] { "sea" }))
							{
								num2++;
							}
							if (Naval && this.Game.Data.CombatTerrainList[this.TileData[point.X, point.Y].TerrainID].BaseType.IsAnyType(new string[] { "sea" }))
							{
								num2++;
								break;
							}
							break;
						}
					}
				}
				goto IL_03A0;
			}
			IL_022D:
			num++;
			List<Point> list3 = new List<Point>();
			foreach (Point point3 in list2)
			{
				if (!list.Contains(point3))
				{
					foreach (Point point4 in this.GetAdjacentTiles(point3.X, point3.Y))
					{
						if (list.Contains(point4))
						{
							list3.Add(new Point(point3.X, point3.Y));
							if (!Naval && !this.Game.Data.CombatTerrainList[this.TileData[point3.X, point3.Y].TerrainID].BaseType.IsAnyType(new string[] { "sea" }))
							{
								num2++;
							}
							if (Naval && this.Game.Data.CombatTerrainList[this.TileData[point3.X, point3.Y].TerrainID].BaseType.IsAnyType(new string[] { "sea" }))
							{
								num2++;
								break;
							}
							break;
						}
					}
				}
			}
			list.AddRange(list3);
			IL_03A0:
			if (num >= 3 && num2 >= 50)
			{
				return list;
			}
			goto IL_022D;
		}

		public PointF GetScaledTileCoords(int x, int y)
		{
			PointF tileCoords = this.GetTileCoords(x, y);
			tileCoords.X /= this.ScaleX;
			tileCoords.Y /= this.ScaleY;
			return tileCoords;
		}

		public bool TilesAdjacent(Point TileA, Point TileB)
		{
			return this.GetAdjacentTiles(TileA.X, TileA.Y).Contains(TileB);
		}

		internal int GetDirection(Point Tile1, Point Tile2)
		{
			for (int i = 0; i < 6; i++)
			{
				int num = Tile1.X;
				int num2 = Tile1.Y;
				if (Tile1.X % 2 == 1)
				{
					num += this.HEX_X_ODD[i];
					num2 += this.HEX_Y_ODD[i];
				}
				else
				{
					num += this.HEX_X_EVEN[i];
					num2 += this.HEX_Y_EVEN[i];
				}
				if (num >= 0 && num2 >= 0 && num < this.TilesX && num2 < this.TilesY && num == Tile2.X && num2 == Tile2.Y)
				{
					return i;
				}
			}
			return -1;
		}

		internal Point GetTileInDirection(Point Tile, int Direction)
		{
			int num = Tile.X;
			int num2 = Tile.Y;
			if (Tile.X % 2 == 1)
			{
				num += this.HEX_X_ODD[Direction];
				num2 += this.HEX_Y_ODD[Direction];
			}
			else
			{
				num += this.HEX_X_EVEN[Direction];
				num2 += this.HEX_Y_EVEN[Direction];
			}
			return new Point(num, num2);
		}

		private Sovereignty Game;

		public int TilesX;

		public int TilesY;

		public int MapWidth;

		public int MapHeight;

		public float ScaleX;

		public float ScaleY;

		public MapTileData[,] TileData;

		public byte[,] BorderValues;

		private int[] HEX_Y_EVEN = new int[] { -1, -1, 0, 1, 0, -1 };

		private int[] HEX_X_EVEN = new int[] { 0, 1, 1, 0, -1, -1 };

		private int[] HEX_Y_ODD = new int[] { -1, 0, 1, 1, 1, 0 };

		private int[] HEX_X_ODD = new int[] { 0, 1, 1, 0, -1, -1 };

		private int[] ADJACENTVALUES = new int[] { 1, 2, 4, 8, 16, 32 };

		public float TileSizeX;

		public float TileSizeY;
	}
}
