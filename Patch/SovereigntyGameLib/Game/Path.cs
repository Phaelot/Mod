using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class Path
	{
		public Path()
		{
			this.PathPoints = new List<PathPoint>();
		}

		public void Dispose()
		{
			foreach (PathPoint pathPoint in this.PathPoints)
			{
				pathPoint.Dispose();
			}
			this.PathPoints.Clear();
		}

		public bool PassesThroughZone(int Zone)
		{
			return this.PathPoints.Count((PathPoint x) => x.Node.SeaZoneID == Zone) > 0;
		}

		public bool PassesThroughProvince(int Province)
		{
			return this.PathPoints.Count((PathPoint x) => x.Node.ProvinceID == Province) > 0;
		}

		public virtual bool DoesCrossRiver()
		{
			foreach (PathPoint pathPoint in this.PathPoints)
			{
				if (pathPoint.LinkType == ConnectionTypes.River || pathPoint.LinkType == ConnectionTypes.TemporaryBridge)
				{
					return true;
				}
			}
			return false;
		}

		public virtual bool DoesCrossRiver(int NodeID)
		{
			int num = 0;
			foreach (PathPoint pathPoint in this.PathPoints)
			{
				if (pathPoint.LinkType == ConnectionTypes.River || pathPoint.LinkType == ConnectionTypes.TemporaryBridge)
				{
					ActivePathNode node = pathPoint.Node;
					ActivePathNode activePathNode = null;
					if (num > 0)
					{
						activePathNode = this.PathPoints[num - 1].Node;
					}
					if (node.ID == NodeID)
					{
						return true;
					}
					if (activePathNode.ID == NodeID)
					{
						return true;
					}
				}
				num++;
			}
			return false;
		}

		public virtual bool DoesCrossSea()
		{
			foreach (PathPoint pathPoint in this.PathPoints)
			{
				if (pathPoint.Node.SeaZoneID != -1)
				{
					return true;
				}
			}
			return false;
		}

		public List<Point> GetPointList()
		{
			List<Point> list = new List<Point>();
			foreach (PathPoint pathPoint in this.PathPoints)
			{
				list.Add(pathPoint.Node.MapCoords);
			}
			return list;
		}

		internal List<Vector3> GetVertices()
		{
			List<Vector3> list = new List<Vector3>();
			foreach (PathPoint pathPoint in this.PathPoints)
			{
				list.Add(new Vector3((float)pathPoint.Node.MapCoords.X, 0f, (float)pathPoint.Node.MapCoords.Y));
			}
			return list;
		}

		internal List<int> GetRegionIDs()
		{
			List<int> list = new List<int>();
			foreach (PathPoint pathPoint in this.PathPoints)
			{
				if (!list.Contains(pathPoint.Node.RegionID))
				{
					list.Add(pathPoint.Node.RegionID);
				}
			}
			return list;
		}

		internal int GetUniqueRegionCount()
		{
			List<int> regionIDs = this.GetRegionIDs();
			return regionIDs.Count;
		}

		public List<PathPoint> PathPoints;

		public float TotalMoveCost;
	}
}
