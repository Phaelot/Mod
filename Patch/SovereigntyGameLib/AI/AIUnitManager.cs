using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI
{
	public class AIUnitManager
	{
		public AIUnitManager(AIPlayer AI, SovereigntyGame Game)
		{
			this.AI = AI;
			this.Game = Game;
		}

		internal void DeployUnits()
		{
			List<UnitQueueItem> list = (from x in this.AI.Realm.GetCurrentUnitQueue()
				where x.TurnsLeft == 0
				select x).ToList<UnitQueueItem>();
			if (list.Count == 0)
			{
				return;
			}
			Dictionary<GameRegion, float> dictionary = this.AI.Utility.GenerateValueMap();
			dictionary[this.AI.Realm.CapitolProvince] = 500f;
			List<WorkingProvince> list2 = new List<WorkingProvince>();
			list2.AddRange(this.AI.Realm.Provinces.Where((WorkingProvince x) => !x.Occupied));
			list2.AddRange(this.AI.Realm.OccupiedProvinces);
			List<NodeUnitData> list3 = new List<NodeUnitData>();
			foreach (WorkingProvince workingProvince in list2)
			{
				if (!dictionary.ContainsKey(workingProvince))
				{
					dictionary[workingProvince] = 10f;
				}
				list3.Add(new NodeUnitData(workingProvince.LandNode, dictionary[workingProvince], this.AI.Realm));
				if (workingProvince.HarbourNode != null)
				{
					list3.Add(new NodeUnitData(workingProvince.HarbourNode, dictionary[workingProvince], this.AI.Realm));
				}
			}
			Dictionary<UnitQueueItem, ActivePathNode> dictionary2 = new Dictionary<UnitQueueItem, ActivePathNode>();
			foreach (UnitQueueItem unitQueueItem in list)
			{
				List<NodeUnitData> list4 = new List<NodeUnitData>();
				foreach (NodeUnitData nodeUnitData in list3)
				{
					if (nodeUnitData.Units.Count < 20 && this.Game.DestinationChecker.NodeOKToDeploy(unitQueueItem.Unit, this.AI.Realm, nodeUnitData.Node) == UnitMoveResult.OK)
					{
						list4.Add(nodeUnitData);
					}
				}
				if (list4.Count != 0)
				{
					NodeUnitData nodeUnitData2 = list4.OrderByDescending((NodeUnitData x) => x.NodeValue).First<NodeUnitData>();
					nodeUnitData2.Units.Add(unitQueueItem.UnitID);
					dictionary2.Add(unitQueueItem, nodeUnitData2.Node);
				}
			}
			AIAction aiaction = new AIAction(AIActionTypes.DeployUnits);
			aiaction.DeployTargets = dictionary2;
			this.AI.SetAction(aiaction);
		}

		internal void MoveUnits()
		{
			Dictionary<GameRegion, float> dictionary = this.AI.Utility.GenerateValueMap();
			List<WorkingProvince> list = new List<WorkingProvince>();
			list.AddRange(this.AI.Realm.Provinces.Where((WorkingProvince x) => !x.Occupied));
			list.AddRange(this.AI.Realm.OccupiedProvinces);
			List<NodeUnitData> list2 = new List<NodeUnitData>();
			foreach (WorkingProvince workingProvince in list)
			{
				if (!dictionary.ContainsKey(workingProvince))
				{
					dictionary[workingProvince] = 10f;
				}
				list2.Add(new NodeUnitData(workingProvince.LandNode, dictionary[workingProvince], this.AI.Realm));
				if (workingProvince.HarbourNode != null && (workingProvince.HarbourNode.CurrentStack == null || workingProvince.HarbourNode.CurrentStack.Owner == this.AI.Realm))
				{
					list2.Add(new NodeUnitData(workingProvince.HarbourNode, dictionary[workingProvince], this.AI.Realm));
				}
			}
			foreach (WorkingZone workingZone in this.Game.AllZones.Values)
			{
				foreach (ActivePathNode activePathNode in workingZone.Nodes)
				{
					float num = 0f;
					if (dictionary.ContainsKey(workingZone))
					{
						num = dictionary[workingZone];
					}
					list2.Add(new NodeUnitData(activePathNode, num, this.AI.Realm));
				}
			}
			List<UnitMoveData> list3 = new List<UnitMoveData>();
			foreach (WorkingUnit workingUnit in this.AI.Realm.Units)
			{
				if (workingUnit.OwnerStack != null && workingUnit.Class != UnitClasses.Fort && (workingUnit.OwnerStack.Node.Province == null || !workingUnit.OwnerStack.Node.Province.IsCapitol || workingUnit.OwnerStack.Units.IndexOf(workingUnit) >= 10) && (workingUnit.OwnerStack.Node.Province == null || workingUnit.OwnerStack.Node.Province.FortLevel <= 0 || workingUnit.OwnerStack.Units.IndexOf(workingUnit) >= 5))
				{
					NodeUnitData nodeUnitData = null;
					foreach (NodeUnitData nodeUnitData2 in list2.OrderByDescending((NodeUnitData x) => x.NodeValue))
					{
						if (nodeUnitData2.Units.Count < 20 && this.Game.DestinationChecker.NodeOKForUnit(workingUnit, nodeUnitData2.Node) == UnitMoveResult.OK)
						{
							List<WorkingUnit> list4 = new List<WorkingUnit>();
							list4.Add(workingUnit);
							Path path = this.Game.PathManager.GetPath(workingUnit.OwnerStack.Node, nodeUnitData2.Node, list4, true, this.AI.Realm, false);
							if (path.PathPoints.Count > 0 && workingUnit.MovePoints >= path.TotalMoveCost)
							{
								nodeUnitData2.MovePath = path;
								nodeUnitData = nodeUnitData2;
								break;
							}
						}
					}
					if (nodeUnitData != null && nodeUnitData.MovePath.PathPoints.Count >= 2)
					{
						nodeUnitData.Units.Add(workingUnit.ID);
						list3.Add(new UnitMoveData(workingUnit, nodeUnitData.Node, nodeUnitData.MovePath));
					}
				}
			}
			AIAction aiaction = new AIAction(AIActionTypes.MoveUnits);
			aiaction.MoveTargets = list3;
			this.AI.SetAction(aiaction);
			this.MoveShips();
		}

		internal void MoveShips()
		{
			List<UnitMoveData> list = new List<UnitMoveData>();
			Dictionary<ActivePathNode, int> dictionary = new Dictionary<ActivePathNode, int>();
			Dictionary<WorkingZone, int> dictionary2 = new Dictionary<WorkingZone, int>();
			foreach (WorkingUnit workingUnit in this.AI.Realm.Units)
			{
				if (workingUnit.OwnerStack != null && workingUnit.MovePoints != 0f)
				{
					if (workingUnit.OwnerStack.Node.NodeType == PathNodeTypes.Harbour)
					{
						WorkingZone workingZone = workingUnit.OwnerStack.Node.Province.GetAllConnectedRegions().FirstOrDefault((GameRegion x) => x is WorkingZone) as WorkingZone;
						if (workingZone == null)
						{
							continue;
						}
						if (!dictionary2.ContainsKey(workingZone))
						{
							int friendlyCount = workingZone.GetFriendlyCount(this.AI.Realm);
							dictionary2.Add(workingZone, friendlyCount);
						}
						if (dictionary2[workingZone] >= 20 || this.AI.RNG.Next(100) < 50)
						{
							continue;
						}
						ActivePathNode activePathNode = workingZone.Nodes.FirstOrDefault((ActivePathNode x) => x.CurrentStack == null || (x.CurrentStack.Owner == this.AI.Realm && x.CurrentStack.Units.Count < 20));
						if (activePathNode == null)
						{
							continue;
						}
						List<WorkingUnit> list2 = new List<WorkingUnit>();
						list2.Add(workingUnit);
						Path path = this.Game.PathManager.GetPath(workingUnit.OwnerStack.Node, activePathNode, list2, true, this.AI.Realm, false);
						if (path.PathPoints.Count <= 0 || workingUnit.MovePoints < path.TotalMoveCost)
						{
							continue;
						}
						list.Add(new UnitMoveData(workingUnit, activePathNode, path));
						Dictionary<WorkingZone, int> dictionary3;
						WorkingZone workingZone2;
						(dictionary3 = dictionary2)[workingZone2 = workingZone] = dictionary3[workingZone2] + 1;
					}
					if (workingUnit.OwnerStack.Node.NodeType == PathNodeTypes.Land && workingUnit.OwnerStack.Node.Province.HarbourNode != null)
					{
						WorkingStack currentStack = workingUnit.OwnerStack.Node.Province.HarbourNode.CurrentStack;
						if (currentStack == null || currentStack.Owner == this.AI.Realm)
						{
							if (!dictionary.ContainsKey(workingUnit.OwnerStack.Node.Province.HarbourNode))
							{
								int num = 0;
								WorkingStack realmStack = workingUnit.OwnerStack.Node.Province.HarbourNode.GetRealmStack(this.AI.Realm);
								if (realmStack != null)
								{
									num = realmStack.Units.Count;
								}
								dictionary.Add(workingUnit.OwnerStack.Node.Province.HarbourNode, num);
							}
							if (dictionary[workingUnit.OwnerStack.Node.Province.HarbourNode] < 20 && this.AI.RNG.Next(100) >= 90)
							{
								List<WorkingUnit> list3 = new List<WorkingUnit>();
								list3.Add(workingUnit);
								Path path2 = this.Game.PathManager.GetPath(workingUnit.OwnerStack.Node, workingUnit.OwnerStack.Node.Province.HarbourNode, list3, true, this.AI.Realm, false);
								if (path2.PathPoints.Count > 0 && workingUnit.MovePoints >= path2.TotalMoveCost)
								{
									list.Add(new UnitMoveData(workingUnit, workingUnit.OwnerStack.Node.Province.HarbourNode, path2));
									Dictionary<ActivePathNode, int> dictionary4;
									ActivePathNode harbourNode;
									(dictionary4 = dictionary)[harbourNode = workingUnit.OwnerStack.Node.Province.HarbourNode] = dictionary4[harbourNode] + 1;
								}
							}
						}
					}
				}
			}
			AIAction aiaction = new AIAction(AIActionTypes.MoveUnits);
			aiaction.MoveTargets = list;
			this.AI.SetAction(aiaction);
		}

		internal void PurchaseUnits()
		{
			List<UnitData> list = new List<UnitData>();
			bool flag = this.AI.Realm.Enemies.Count > 1;
			int num = this.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			if (num < 0)
			{
				num = 0;
			}
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			int i = this.AI.Realm.Gold;
			if (!flag)
			{
				i = (int)((float)i * 0.6f);
			}
			else
			{
				i = (int)((float)i * 0.9f);
			}
			while (i > 0)
			{
				List<KeyValuePair<UnitData, UnitTrainStates>> availableUnitTypes = this.AI.Realm.UnitPurchaseManager.GetAvailableUnitTypes();
				Dictionary<UnitData, int> dictionary = new Dictionary<UnitData, int>();
				using (IEnumerator<KeyValuePair<UnitData, UnitTrainStates>> enumerator = availableUnitTypes.Where((KeyValuePair<UnitData, UnitTrainStates> x) => x.Value == UnitTrainStates.OK || x.Value == UnitTrainStates.NoResources).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<UnitData, UnitTrainStates> Pair = enumerator.Current;
						int num2 = 0;
						int num3 = 0;
						KeyValuePair<UnitData, UnitTrainStates> pair = Pair;
						foreach (KeyValuePair<string, int> keyValuePair in pair.Key.GetRequiredResources())
						{
							int num4 = keyValuePair.Value;
							if (resources.ContainsKey(keyValuePair.Key))
							{
								num4 -= resources[keyValuePair.Key];
							}
							if (num4 > 0)
							{
								num2 += num4;
							}
						}
						if (num2 > 0)
						{
							num3 = num2 * this.GetResourceReplaceCost();
						}
						UnitPurchaseManager unitPurchaseManager = this.AI.Realm.UnitPurchaseManager;
						KeyValuePair<UnitData, UnitTrainStates> pair2 = Pair;
						if (unitPurchaseManager.GetUnitCost(pair2.Key) + num3 <= i)
						{
							UnitPurchaseManager unitPurchaseManager2 = this.AI.Realm.UnitPurchaseManager;
							KeyValuePair<UnitData, UnitTrainStates> pair3 = Pair;
							if (unitPurchaseManager2.GetUnitUpkeep(pair3.Key) <= num)
							{
								KeyValuePair<UnitData, UnitTrainStates> pair4 = Pair;
								if (pair4.Key.Rank == UnitRanks.Elite)
								{
									WorkingRealm realm = this.AI.Realm;
									KeyValuePair<UnitData, UnitTrainStates> pair5 = Pair;
									if (realm.GetUnitTypeCount(pair5.Key) + list.Count(delegate(UnitData x)
									{
										Type type = x.GetType();
										KeyValuePair<UnitData, UnitTrainStates> pair14 = Pair;
										return type == pair14.Key.GetType();
									}) >= 4)
									{
										continue;
									}
								}
								KeyValuePair<UnitData, UnitTrainStates> pair6 = Pair;
								if (pair6.Key.Rank == UnitRanks.Unique)
								{
									WorkingRealm realm2 = this.AI.Realm;
									KeyValuePair<UnitData, UnitTrainStates> pair7 = Pair;
									if (realm2.GetUnitTypeCount(pair7.Key) + list.Count(delegate(UnitData x)
									{
										Type type2 = x.GetType();
										KeyValuePair<UnitData, UnitTrainStates> pair15 = Pair;
										return type2 == pair15.Key.GetType();
									}) >= 1)
									{
										continue;
									}
								}
								KeyValuePair<UnitData, UnitTrainStates> pair8 = Pair;
								if (pair8.Key.Realm != this.AI.Realm.Name)
								{
									WorkingRealm realm3 = this.AI.Realm;
									KeyValuePair<UnitData, UnitTrainStates> pair9 = Pair;
									if (realm3.GetUnitTypeCount(pair9.Key) + list.Count(delegate(UnitData x)
									{
										Type type3 = x.GetType();
										KeyValuePair<UnitData, UnitTrainStates> pair16 = Pair;
										return type3 == pair16.Key.GetType();
									}) >= 4)
									{
										continue;
									}
								}
								if (flag)
								{
									Dictionary<UnitData, int> dictionary2 = dictionary;
									KeyValuePair<UnitData, UnitTrainStates> pair10 = Pair;
									UnitData key = pair10.Key;
									KeyValuePair<UnitData, UnitTrainStates> pair11 = Pair;
									dictionary2.Add(key, pair11.Key.WarWeight);
								}
								else
								{
									Dictionary<UnitData, int> dictionary3 = dictionary;
									KeyValuePair<UnitData, UnitTrainStates> pair12 = Pair;
									UnitData key2 = pair12.Key;
									KeyValuePair<UnitData, UnitTrainStates> pair13 = Pair;
									dictionary3.Add(key2, pair13.Key.PeaceWeight);
								}
							}
						}
					}
				}
				if (dictionary.Count == 0)
				{
					break;
				}
				int num5 = dictionary.Sum((KeyValuePair<UnitData, int> x) => x.Value);
				int num6 = this.AI.RNG.Next(num5);
				int num7 = 0;
				UnitData unitData = null;
				foreach (KeyValuePair<UnitData, int> keyValuePair2 in dictionary)
				{
					num7 += keyValuePair2.Value;
					unitData = keyValuePair2.Key;
					if (num7 >= num6)
					{
						break;
					}
				}
				if (unitData == null)
				{
					break;
				}
				list.Add(unitData);
				int num8 = 0;
				int num9 = 0;
				foreach (KeyValuePair<string, int> keyValuePair3 in unitData.GetRequiredResources())
				{
					int num10 = keyValuePair3.Value;
					if (resources.ContainsKey(keyValuePair3.Key))
					{
						num10 -= resources[keyValuePair3.Key];
					}
					if (num10 > 0)
					{
						num8 += num10;
					}
				}
				if (num8 > 0)
				{
					num9 = num8 * this.GetResourceReplaceCost();
				}
				num -= this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unitData);
				i -= this.AI.Realm.UnitPurchaseManager.GetUnitCost(unitData) + num9;
				foreach (KeyValuePair<string, int> keyValuePair4 in unitData.GetRequiredResources())
				{
					if (resources.ContainsKey(keyValuePair4.Key))
					{
						int num11 = resources[keyValuePair4.Key];
						Dictionary<string, int> dictionary4;
						string key3;
						(dictionary4 = resources)[key3 = keyValuePair4.Key] = dictionary4[key3] - Math.Min(keyValuePair4.Value, num11);
					}
				}
			}
			AIAction aiaction = new AIAction(AIActionTypes.PurchaseUnits);
			aiaction.UnitTypes = list;
			this.AI.SetAction(aiaction);
			this.DeployUnits();
		}

		private int GetResourceReplaceCost()
		{
			switch (this.Game.GameCore.Settings.GetEnumeratedSetting("Difficulty"))
			{
			case 1:
				return 500;
			case 2:
				return 300;
			case 3:
				return 200;
			case 4:
				return 150;
			case 5:
				return 75;
			default:
				return 1000;
			}
		}

		public AIPlayer AI;

		private SovereigntyGame Game;
	}
}
