// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Game.PathManager
using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

public class PathManager
{
	public SovereigntyGame Game;

	public PathManager(SovereigntyGame Game)
	{
		this.Game = Game;
	}

	private int CompareSearchNodes(SearchNode Node1, SearchNode Node2)
	{
		return Node1.PathDistance.CompareTo(Node2.PathDistance);
	}

	public List<ActivePathNode> GetSpellTargetNodes(WorkingRealm Caster, int Range)
	{
		List<GameRegion> list = new List<GameRegion>();
		list.AddRange(Caster.Provinces);
		for (int i = 0; i < Range; i++)
		{
			foreach (GameRegion item in list.ToList())
			{
				list.AddRange(item.GetAllConnectedRegions());
			}
			list = list.Distinct().ToList();
		}
		List<ActivePathNode> list2 = new List<ActivePathNode>();
		foreach (GameRegion item2 in list)
		{
			if (item2 is WorkingProvince)
			{
				list2.Add((item2 as WorkingProvince).LandNode);
				ActivePathNode harbourNode = (item2 as WorkingProvince).HarbourNode;
				if (harbourNode != null)
				{
					list2.Add(harbourNode);
				}
			}
			if (!(item2 is WorkingZone))
			{
				continue;
			}
			foreach (ActivePathNode node in (item2 as WorkingZone).Nodes)
			{
				list2.Add(node);
			}
		}
		return list2;
	}

	public List<WorkingProvince> GetAreaProvinces(WorkingProvince InitialProvince, int Range)
	{
		List<GameRegion> list = new List<GameRegion>();
		list.Add(InitialProvince);
		for (int i = 0; i < Range; i++)
		{
			foreach (GameRegion item in list.ToList())
			{
				list.AddRange(item.GetAllConnectedRegions());
			}
			list = list.Distinct().ToList();
		}
		List<WorkingProvince> list2 = new List<WorkingProvince>();
		foreach (GameRegion item2 in list.Where((GameRegion x) => x is WorkingProvince))
		{
			list2.Add(item2 as WorkingProvince);
		}
		return list2;
	}

	public List<WorkingProvince> GetAreaProvinces(WorkingZone InitialZone, int Range)
	{
		List<GameRegion> list = new List<GameRegion>();
		list.Add(InitialZone);
		for (int i = 0; i < Range; i++)
		{
			foreach (GameRegion item in list.ToList())
			{
				list.AddRange(item.GetAllConnectedRegions());
			}
			list = list.Distinct().ToList();
		}
		List<WorkingProvince> list2 = new List<WorkingProvince>();
		foreach (GameRegion item2 in list.Where((GameRegion x) => x is WorkingProvince))
		{
			list2.Add(item2 as WorkingProvince);
		}
		return list2;
	}

	public List<WorkingProvince> GetSpellTargetProvinces(WorkingRealm Caster, int Range)
	{
		List<GameRegion> list = new List<GameRegion>();
		list.AddRange(Caster.Provinces);
		for (int i = 0; i < Range; i++)
		{
			foreach (GameRegion item in list.ToList())
			{
				list.AddRange(item.GetAllConnectedRegions());
			}
			list = list.Distinct().ToList();
		}
		List<WorkingProvince> list2 = new List<WorkingProvince>();
		foreach (GameRegion item2 in list.Where((GameRegion x) => x is WorkingProvince))
		{
			list2.Add(item2 as WorkingProvince);
		}
		return list2;
	}

	public List<WorkingZone> GetSpellTargetZones(WorkingRealm Caster, int Range)
	{
		List<GameRegion> list = new List<GameRegion>();
		list.AddRange(Caster.Provinces);
		for (int i = 0; i < Range; i++)
		{
			foreach (GameRegion item in list.ToList())
			{
				list.AddRange(item.GetAllConnectedRegions());
			}
			list = list.Distinct().ToList();
		}
		List<WorkingZone> list2 = new List<WorkingZone>();
		foreach (GameRegion item2 in list.Where((GameRegion x) => x is WorkingZone))
		{
			list2.Add(item2 as WorkingZone);
		}
		return list2;
	}

	public Path GetPath(ActivePathNode StartNode, ActivePathNode EndNode, IEnumerable<WorkingUnit> Units, bool CheckUnitMoves, WorkingRealm MovingRealm, bool IgnoreTerrainFeatures = false)
	{
		Path path = new Path();
		if (CheckUnitMoves)
		{
			if (Units.Count() == 0)
			{
				return path;
			}
			if (!StartNode.ReachableNodes.Contains(EndNode.ID) && Units.Count((WorkingUnit x) => x.TeleportActive) == 0)
			{
				return path;
			}
		}
		List<ActivePathNode> list = new List<ActivePathNode>();
		List<SearchNode> list2 = new List<SearchNode>();
		SearchNode searchNode = null;
		SearchNode searchNode2 = new SearchNode();
		searchNode2.Node = StartNode;
		searchNode2.PathCost = 0f;
		searchNode2.PathDistance = 0f;
		searchNode2.MoveCost = 0f;
		list2.Add(searchNode2);
		bool flag = false;
		if (Units != null)
		{
			flag = Units.Count((WorkingUnit x) => x.HasStatus("Bridging")) > 0;
		}
		while (list2.Count > 0)
		{
			list2.Sort(CompareSearchNodes);
			SearchNode CurrentNode = list2[0];
			if (CurrentNode.Node == EndNode)
			{
				searchNode = CurrentNode;
				break;
			}
			list.Add(CurrentNode.Node);
			list2.Remove(CurrentNode);
			List<ActiveNodeConnection> list3 = CurrentNode.Node.ConnectedNodes;
			if (CheckUnitMoves && CurrentNode.Node != StartNode && CurrentNode.Node.Province != null && MovingRealm != CurrentNode.Node.Province.OwnerRealm && MovingRealm.DiplomacyManager.GetRelation(CurrentNode.Node.Province.OwnerRealm) != RelationStates.Alliance)
			{
				list3 = new List<ActiveNodeConnection>();
			}
			foreach (ActiveNodeConnection Connection in list3)
			{
				if (list.Contains(Connection.TargetNode) || Connection.ConnectionType == ConnectionTypes.Blocked)
				{
					continue;
				}
				float MoveCost = 0f;
				float num = Math.Abs(CurrentNode.Node.MapCoords.X - Connection.TargetNode.MapCoords.X);
				float num2 = Math.Abs(CurrentNode.Node.MapCoords.Y - Connection.TargetNode.MapCoords.Y);
				float num3 = (float)Math.Sqrt(num * num + num2 * num2);
				num3 /= 1000f;
				if (Units != null)
				{
					if ((CurrentNode.Node.NodeType == PathNodeTypes.RiverHarbour && Connection.TargetNode.NodeType == PathNodeTypes.Sea && Units.Count((WorkingUnit x) => !x.HasStatus("Riverboat")) > 0) || (CurrentNode.Node.NodeType == PathNodeTypes.Sea && Connection.TargetNode.NodeType == PathNodeTypes.RiverHarbour && Units.Count((WorkingUnit x) => !x.HasStatus("Riverboat")) > 0) || (!CurrentNode.CanLeaveZone() && Connection.TargetNode.SeaZoneID != CurrentNode.Node.SeaZoneID))
					{
						continue;
					}
					if (Connection.TargetNode.NodeType == PathNodeTypes.Land)
					{
						foreach (WorkingUnit Unit in Units)
						{
							Unit.CanEnterTerrain(Connection.TargetNode.GetRegion());
						}
						if (CurrentNode.Node.NodeType != PathNodeTypes.Land)
						{
							MoveCost += 4f;
						}
						else
						{
							foreach (WorkingUnit Unit2 in Units)
							{
								TerrainData terrainData = Game.GameCore.Data.Terrains["Plains"];
								float num4 = terrainData.UnitMoveCost;
								switch (Connection.ConnectionType)
								{
									case ConnectionTypes.Bridge:
									case ConnectionTypes.TemporaryBridge:
									case ConnectionTypes.Road:
										num4 = 1f;
										break;
									case ConnectionTypes.River:
										num4 += 1f;
										if (flag)
										{
											num4 = 1f;
										}
										break;
								}
								if (Unit2.TeleportActive && Connection.TargetNode.Province != null && Connection.TargetNode.Province.OwnerID == Unit2.OwnerRealmID)
								{
									num4 = 0f;
								}
								if (num4 > MoveCost)
								{
									MoveCost = num4;
								}
							}
						}
					}
					if (Connection.TargetNode.NodeType == PathNodeTypes.Sea)
					{
						if (Connection.TargetNode.SeaZoneID == CurrentNode.Node.SeaZoneID)
						{
							MoveCost += 0f;
						}
						else
						{
							MoveCost += 4f;
						}
					}
					if (Connection.TargetNode.NodeType == PathNodeTypes.Harbour || Connection.TargetNode.NodeType == PathNodeTypes.RiverHarbour)
					{
						MoveCost += 0f;
					}
					if (CheckUnitMoves)
					{
						int num5 = Units.Count((WorkingUnit x) => x.MovePoints < CurrentNode.PathCost + MoveCost);
						if (num5 > 0)
						{
							continue;
						}
					}
					num3 += MoveCost;
					if (Connection.TargetNode.Province != null && Connection.TargetNode.Province.OwnerRealm != MovingRealm)
					{
						num3 += 100f;
					}
				}
				else
				{
					MoveCost = Connection.TargetNode.GetRegion().Terrain.TradeMoveCost;
					if (!IgnoreTerrainFeatures)
					{
						switch (Connection.ConnectionType)
						{
							case ConnectionTypes.Bridge:
								MoveCost = 0.2f;
								break;
							case ConnectionTypes.Road:
								MoveCost = 0.2f;
								break;
							case ConnectionTypes.River:
								MoveCost += 0.5f;
								break;
						}
					}
					num3 += MoveCost;
				}
				SearchNode searchNode3 = list2.SingleOrDefault((SearchNode x) => x.Node == Connection.TargetNode);
				if (searchNode3 == null)
				{
					searchNode3 = new SearchNode();
					searchNode3.Parent = CurrentNode;
					searchNode3.LinkType = Connection.ConnectionType;
					searchNode3.Node = Connection.TargetNode;
					list2.Add(searchNode3);
				}
				else
				{
					if (!(CurrentNode.PathDistance + num3 < searchNode3.PathDistance))
					{
						continue;
					}
					searchNode3.Parent = CurrentNode;
				}
				searchNode3.PathCost = CurrentNode.PathCost + MoveCost;
				searchNode3.PathDistance = CurrentNode.PathDistance + num3;
				searchNode3.MoveCost = MoveCost;
			}
		}
		path.PathPoints = new List<PathPoint>();
		while (searchNode != null)
		{
			PathPoint pathPoint = new PathPoint();
			pathPoint.Node = searchNode.Node;
			pathPoint.MoveCost = searchNode.MoveCost;
			pathPoint.LinkType = searchNode.LinkType;
			if (Units != null && searchNode.LinkType == ConnectionTypes.River && flag)
			{
				pathPoint.LinkType = ConnectionTypes.TemporaryBridge;
			}
			path.TotalMoveCost += searchNode.MoveCost;
			path.PathPoints.Add(pathPoint);
			searchNode = searchNode.Parent;
		}
		path.PathPoints.Reverse();
		return path;
	}
}
