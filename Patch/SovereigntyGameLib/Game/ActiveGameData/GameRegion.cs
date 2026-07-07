using System;
using System.Collections.Generic;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class GameRegion
	{
		public event BorderStateDelegate OnBorderStateRequested;

		public BorderStates GetBorderState(WorkingUnit Unit)
		{
			BorderStates borderStates = BorderStates.Open;
			if (this.OnBorderStateRequested != null)
			{
				this.OnBorderStateRequested(this, Unit, ref borderStates);
			}
			return borderStates;
		}

		public TerrainData Terrain
		{
			get
			{
				if (this.TerrainString == null)
				{
					return null;
				}
				TerrainData terrainData = null;
				this.Game.GameCore.Data.Terrains.TryGetValue(this.TerrainString, out terrainData);
				return terrainData;
			}
		}

		public List<GameRegion> GetAllConnectedRegions()
		{
			List<GameRegion> list = new List<GameRegion>();
			if (this is WorkingProvince)
			{
				WorkingProvince workingProvince = this as WorkingProvince;
				ActivePathNode activePathNode = workingProvince.LandNode;
				if (activePathNode != null)
				{
					List<ActivePathNode> connectedNodes = activePathNode.GetConnectedNodes();
					if (connectedNodes != null)
					{
						foreach (ActivePathNode activePathNode2 in connectedNodes)
						{
							if (activePathNode2 == null)
							{
								continue;
							}
							GameRegion region = activePathNode2.GetRegion();
							if (region != null && region != this)
							{
								list.Add(region);
							}
						}
					}
				}
				activePathNode = workingProvince.HarbourNode;
				if (activePathNode != null)
				{
					List<ActivePathNode> connectedNodes2 = activePathNode.GetConnectedNodes();
					if (connectedNodes2 != null)
					{
						foreach (ActivePathNode activePathNode3 in connectedNodes2)
						{
							if (activePathNode3 == null)
							{
								continue;
							}
							GameRegion region2 = activePathNode3.GetRegion();
							if (region2 != null && region2 != this)
							{
								list.Add(region2);
							}
						}
					}
				}
				if (workingProvince.AdjacentZones != null && this.Game != null)
				{
					foreach (string text in workingProvince.AdjacentZones)
					{
						GameRegion zone = this.Game.GetZone(text);
						if (zone != null && zone != this)
						{
							list.Add(zone);
						}
					}
				}
			}
			if (this is WorkingZone)
			{
				WorkingZone workingZone = this as WorkingZone;
				if (workingZone.Nodes != null)
				{
					foreach (ActivePathNode activePathNode4 in workingZone.Nodes)
					{
						if (activePathNode4 == null)
						{
							continue;
						}
						List<ActivePathNode> connectedNodes3 = activePathNode4.GetConnectedNodes();
						if (connectedNodes3 == null)
						{
							continue;
						}
						foreach (ActivePathNode activePathNode5 in connectedNodes3)
						{
							if (activePathNode5 == null)
							{
								continue;
							}
							GameRegion region3 = activePathNode5.GetRegion();
							if (region3 != null && region3 != this)
							{
								list.Add(region3);
							}
						}
					}
				}
			}
			return list;
		}

		public SovereigntyGame Game;

		public string TerrainString;

		public string Name;

		public string DisplayName;

		public int RegionID;
	}
}
