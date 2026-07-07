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
				ActivePathNode activePathNode = (this as WorkingProvince).LandNode;
				foreach (ActivePathNode activePathNode2 in activePathNode.GetConnectedNodes())
				{
					GameRegion region = activePathNode2.GetRegion();
					if (region != this)
					{
						list.Add(activePathNode2.GetRegion());
					}
				}
				activePathNode = (this as WorkingProvince).HarbourNode;
				if (activePathNode != null)
				{
					using (List<ActivePathNode>.Enumerator enumerator2 = activePathNode.GetConnectedNodes().GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							ActivePathNode activePathNode3 = enumerator2.Current;
							GameRegion region2 = activePathNode3.GetRegion();
							if (region2 != this)
							{
								list.Add(activePathNode3.GetRegion());
							}
						}
						goto IL_0108;
					}
				}
				foreach (string text in (this as WorkingProvince).AdjacentZones)
				{
					list.Add(this.Game.GetZone(text));
				}
			}
			IL_0108:
			if (this is WorkingZone)
			{
				foreach (ActivePathNode activePathNode4 in (this as WorkingZone).Nodes)
				{
					foreach (ActivePathNode activePathNode5 in activePathNode4.GetConnectedNodes())
					{
						GameRegion region3 = activePathNode5.GetRegion();
						if (region3 != this)
						{
							list.Add(activePathNode5.GetRegion());
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
