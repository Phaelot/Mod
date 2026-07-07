using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2
{
	public class AIUnitsManager
	{
		private const int MinAttackUnits = 10;
		private const int SafeCapitalGarrison = 5;
		private const int CautiousCapitalGarrison = 12;
		private const int ThreatenedCapitalGarrison = 20;
		private const int CapitalThreatDistance = 2;
		private const int CapitalCautionDistance = 4;
		private const int CapitalSafeRadius = 5;
		private const int IronBaronyMaxDynamicArmySlots = 5;
		private bool IronBaronySavingForOgre;
		private bool MaledorSavingForPriorityUnit;
		private string MaledorSavingForUnitName;
		private bool BoruvianSavingForPriorityUnit;
		private string BoruvianSavingForUnitName;

		public AIUnitsManager(AIPlayer AI)
		{
			this.AI = AI;
			this.Funds = new AIFundData();
			this.IgnoreProvinces = new List<int>();
		}

		private void LogWarGoals(string Text)
		{
			this.AI.Log(Text);
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "ai_wargoals.txt");
				string turn = (this.AI.Game != null && this.AI.Game.TurnController != null) ? "T" + this.AI.Game.TurnController.TurnNumber : "T?";
				System.IO.File.AppendAllText(file, turn + " [" + this.AI.Realm.Name + "] " + Text + "\r\n");
			}
			catch
			{
			}
		}

		private void LogIronBaronyDraft(string Text)
		{
			this.AI.Log(Text);
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyTurnDebugLogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "IronBaronyDraft.log");
				string turn = (this.AI.Game != null && this.AI.Game.TurnController != null) ? "T" + this.AI.Game.TurnController.TurnNumber : "T?";
				string realm = (this.AI != null && this.AI.Realm != null) ? this.AI.Realm.Name : "Unknown Realm";
				string stamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				System.IO.File.AppendAllText(file, stamp + " " + turn + " [" + realm + "] " + Text + "\r\n");
			}
			catch
			{
			}
		}

		private void LogDoctrineDraft(string Text)
		{
			this.AI.Log(Text);
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyTurnDebugLogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "RealmDoctrineDraft.log");
				string turn = (this.AI.Game != null && this.AI.Game.TurnController != null) ? "T" + this.AI.Game.TurnController.TurnNumber : "T?";
				string realm = (this.AI != null && this.AI.Realm != null) ? this.AI.Realm.Name : "Unknown Realm";
				string stamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				System.IO.File.AppendAllText(file, stamp + " " + turn + " [" + realm + "] " + Text + "\r\n");
			}
			catch
			{
			}
		}

		internal void Dispose()
		{
		}

		internal void DeployUnits()
		{
			this.AI.Log("");
			this.AI.Log("Unit manager updating (deploy phase)");
			this.AI.Log("  Not logging this for sanity reasons until new unit manager is done");
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
				if (this.IsIronBarony())
				{
					NodeUnitData preferredArmyTarget = this.FindIronBaronyDeploymentArmyTarget(unitQueueItem, list3);
					if (preferredArmyTarget != null)
					{
						int armySlotNumber = this.GetIronBaronyDeploymentArmySlotNumber(preferredArmyTarget, list3);
						preferredArmyTarget.Units.Add(unitQueueItem.UnitID);
						dictionary2.Add(unitQueueItem, preferredArmyTarget.Node);
						this.AI.Log("  Iron Barony deployment: placing " + unitQueueItem.Unit.DisplayName + " directly into Army #" + armySlotNumber + " at node " + preferredArmyTarget.Node.ID + " (" + preferredArmyTarget.Units.Count + "/20)");
						continue;
					}
				}
				List<NodeUnitData> list4 = new List<NodeUnitData>();
				foreach (NodeUnitData nodeUnitData in list3)
				{
					if (!this.AIQueueUnitCanDeployToNode(unitQueueItem.Unit, nodeUnitData.Node))
					{
						continue;
					}
					if (nodeUnitData.Units.Count < 20 && this.CanAddIronBaronyUnitToNodeData(unitQueueItem.Unit, nodeUnitData, "DeployUnits") && this.AI.Game.DestinationChecker.NodeOKToDeploy(unitQueueItem.Unit, this.AI.Realm, nodeUnitData.Node) == UnitMoveResult.OK)
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
			AIActionDeployUnits aiactionDeployUnits = this.AI.ActionManager.CreateAction<AIActionDeployUnits>();
			aiactionDeployUnits.DeployTargets = dictionary2;
			this.AI.ActionManager.AddAction(aiactionDeployUnits, true);
		}

		private bool AIQueueUnitCanDeployToNode(WorkingUnit Unit, ActivePathNode Node)
		{
			if (Unit == null || Node == null)
			{
				return false;
			}
			if (Unit.Class == UnitClasses.Naval)
			{
				return Node.NodeType == PathNodeTypes.Harbour || Node.NodeType == PathNodeTypes.RiverHarbour;
			}
			return Node.NodeType == PathNodeTypes.Land;
		}

		internal void MoveUnits()
		{
			this.AI.Log("");
			this.AI.Log("Unit manager updating (move phase)");
			this.AI.Log("  Not logging this for sanity reasons until new unit manager is done");
			if (this.IsIronBarony())
			{
				this.AI.Log("  Iron Barony: skipping generic value-map land movement; stable army-slot consolidation and war-goal movement handle land armies");
				this.MoveShips();
				return;
			}
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
			foreach (WorkingZone workingZone in this.AI.Game.AllZones.Values)
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
			int enemyDistance = this.GetEnemyDistanceFromCapital(CapitalSafeRadius);
			int capitalGarrison = this.GetCapitalGarrisonTarget(enemyDistance);
			List<UnitMoveData> list3 = new List<UnitMoveData>();
			foreach (WorkingUnit workingUnit in this.AI.Realm.Units)
			{
				if (workingUnit.OwnerStack == null || workingUnit.Class == UnitClasses.Fort)
				{
					continue;
				}
				if (this.IsIronBaronyCatapult(workingUnit))
				{
					continue;
				}
				if (workingUnit.OwnerStack.Node.Province != null && workingUnit.OwnerStack.Node.Province.OwnerRealm != this.AI.Realm && workingUnit.OwnerStack.Node.Province.OccupierRealm != this.AI.Realm)
				{
					continue;
				}
				if (workingUnit.OwnerStack.Node.Province != null && workingUnit.OwnerStack.Node.Province.OccupierRealm == this.AI.Realm && workingUnit.OwnerStack.Node.Province.OwnerRealm != this.AI.Realm)
				{
					continue;
				}
				if (workingUnit.OwnerStack.Node.Province != null && workingUnit.OwnerStack.Node.Province.IsCapitol && workingUnit.OwnerStack.Units.IndexOf(workingUnit) < capitalGarrison)
				{
					continue;
				}
				if (!this.AI.Realm.CapitolProvince.Occupied)
				{
					if (workingUnit.OwnerStack.Node.Province != null && workingUnit.OwnerStack.Node.Province.FortLevel > 0 && workingUnit.OwnerStack.Units.IndexOf(workingUnit) < 5)
					{
						continue;
					}
					if (workingUnit.OwnerStack.Node.Province != null && workingUnit.OwnerStack.Node.Province.Occupied && workingUnit.OwnerStack.Units.IndexOf(workingUnit) < 10)
					{
						continue;
					}
				}
				{
					NodeUnitData nodeUnitData = null;
					foreach (NodeUnitData nodeUnitData2 in list2.OrderByDescending((NodeUnitData x) => x.NodeValue))
					{
						if (nodeUnitData2.Units.Count < 20 && this.CanAddIronBaronyUnitToNodeData(workingUnit, nodeUnitData2, "MoveUnits") && this.AI.Game.DestinationChecker.NodeOKForUnit(workingUnit, nodeUnitData2.Node) == UnitMoveResult.OK)
						{
							List<WorkingUnit> list4 = new List<WorkingUnit>();
							list4.Add(workingUnit);
							SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(workingUnit.OwnerStack.Node, nodeUnitData2.Node, list4, true, this.AI.Realm, false);
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
			AIActionMoveUnits aiactionMoveUnits = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
			aiactionMoveUnits.DebugSource = "MoveUnits";
			aiactionMoveUnits.MoveTargets = list3;
			this.AI.Log("  [MoveUnits] queuing " + list3.Count + " unit moves from MoveUnits phase");
			foreach (UnitMoveData moveData in list3)
			{
				this.AI.Log("    -> unit " + moveData.Unit.DisplayName + " to node " + moveData.TargetNode.ID);
			}
			this.AI.ActionManager.AddAction(aiactionMoveUnits, true);
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
						SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(workingUnit.OwnerStack.Node, activePathNode, list2, true, this.AI.Realm, false);
						if (path.PathPoints.Count <= 0 || workingUnit.MovePoints < path.TotalMoveCost)
						{
							continue;
						}
						list.Add(new UnitMoveData(workingUnit, activePathNode, path));
						Dictionary<WorkingZone, int> dictionary3;
						WorkingZone workingZone2;
						(dictionary3 = dictionary2)[workingZone2 = workingZone] = dictionary3[workingZone2] + 1;
					}
					if (workingUnit.Class != UnitClasses.Naval && workingUnit.OwnerStack.Node.NodeType == PathNodeTypes.Land && workingUnit.OwnerStack.Node.Province.HarbourNode != null)
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
								SovereigntyTK.Game.Path path2 = this.AI.Game.PathManager.GetPath(workingUnit.OwnerStack.Node, workingUnit.OwnerStack.Node.Province.HarbourNode, list3, true, this.AI.Realm, false);
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
			AIActionMoveUnits aiactionMoveUnits = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
			aiactionMoveUnits.DebugSource = "MoveShips";
			aiactionMoveUnits.MoveTargets = list;
			this.AI.ActionManager.AddAction(aiactionMoveUnits, true);
		}

		internal void PurchaseUnits()
		{
			this.AI.Log("");
			this.AI.Log("Unit manager updating (purchase phase)");
			this.AI.Log("  Available funds: " + this.Funds.CurrentGold);
			if (this.IsIronBarony())
			{
				this.PurchaseIronBaronyUnits();
				this.DeployUnits();
				return;
			}
			if (this.IsMaledor())
			{
				this.PurchaseMaledorUnits();
				this.DeployUnits();
				return;
			}
			if (this.IsBoruvian())
			{
				this.PurchaseBoruvianUnits();
				this.DeployUnits();
				return;
			}
			List<UnitData> list = new List<UnitData>();
			int num = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			if (num < 0)
			{
				num = 0;
			}
			int num2 = this.Funds.CurrentGold;
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			while (this.PurchaseList.Count > 0)
			{
				UnitData unitData = this.PurchaseList[0];
				if (this.CanPurchaseUnitType(unitData))
				{
					int num3 = 0;
					int num4 = 0;
					foreach (KeyValuePair<string, int> keyValuePair in unitData.GetRequiredResources())
					{
						int num5 = keyValuePair.Value;
						if (resources.ContainsKey(keyValuePair.Key))
						{
							num5 -= resources[keyValuePair.Key];
						}
						if (num5 > 0)
						{
							num3 += num5;
						}
					}
					if (num3 > 0)
					{
						num4 = num3 * this.GetResourceReplaceCost();
					}
					if (this.AI.Realm.UnitPurchaseManager.GetUnitCost(unitData) + num4 > num2 || this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unitData) > num)
					{
						break;
					}
					list.Add(unitData);
					this.PurchaseList.RemoveAt(0);
					if (unitData.Class == UnitClasses.Siege && this.SiegeUnitNeeded)
					{
						this.SiegeUnitNeeded = false;
					}
					num -= this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unitData);
					num2 -= this.AI.Realm.UnitPurchaseManager.GetUnitCost(unitData) + num4;
					using (Dictionary<string, int>.Enumerator enumerator2 = unitData.GetRequiredResources().GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							KeyValuePair<string, int> keyValuePair2 = enumerator2.Current;
							if (resources.ContainsKey(keyValuePair2.Key))
							{
								int num6 = resources[keyValuePair2.Key];
								Dictionary<string, int> dictionary;
								string key;
								(dictionary = resources)[key = keyValuePair2.Key] = dictionary[key] - Math.Min(keyValuePair2.Value, num6);
							}
						}
						continue;
					}
				}
				this.PurchaseList.Remove(unitData);
			}
			if (list.Count > 0)
			{
				AIActionPurchaseUnits aiactionPurchaseUnits = this.AI.ActionManager.CreateAction<AIActionPurchaseUnits>();
				aiactionPurchaseUnits.UnitTypes = list;
				this.AI.ActionManager.AddAction(aiactionPurchaseUnits, true);
			}
			this.DeployUnits();
		}

		private void PurchaseIronBaronyUnits()
		{
			List<UnitData> list = new List<UnitData>();
			if (this.PurchaseList == null || this.PurchaseList.Count == 0)
			{
				this.LogIronBaronyDraft("  Iron Barony draft: no units requested");
				return;
			}
			int netIncome = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			if (netIncome <= 0)
			{
				this.LogIronBaronyDraft("  Iron Barony draft blocked: projected income is not positive (" + netIncome + ")");
				return;
			}
			int remainingGold = this.Funds.CurrentGold;
			int remainingIncome = netIncome;
			int requestedCost = this.GetUnitDataListCost(this.PurchaseList);
			int requestedUpkeep = this.GetUnitDataListUpkeep(this.PurchaseList);
			this.LogIronBaronyDraft("  Iron Barony draft projection start: income " + netIncome + ", unit funds " + remainingGold + ", requested cost " + requestedCost + ", requested upkeep " + requestedUpkeep + ", projected income if all queued " + (netIncome - requestedUpkeep) + ", projected unit funds if all queued " + (remainingGold - requestedCost));
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			foreach (UnitData unitData in this.PurchaseList)
			{
				int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unitData);
				int upkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unitData);
				if (!this.CanPurchaseUnitTypeWithPlanned(unitData, list))
				{
					this.LogIronBaronyDraft("  Iron Barony draft: skipping " + unitData.Name + " (limit reached; cost " + cost + ", upkeep " + upkeep + ", projected income remains " + remainingIncome + ", unit funds remain " + remainingGold + ")");
					continue;
				}
				if (!this.ResourcesAvailableForPlannedUnit(unitData, resources))
				{
					this.LogIronBaronyDraft("  Iron Barony draft: delaying " + unitData.Name + " until required resources are available (cost " + cost + ", upkeep " + upkeep + ", projected income if queued " + (remainingIncome - upkeep) + ", unit funds if queued " + (remainingGold - cost) + ")");
					continue;
				}
				if (cost > remainingGold)
				{
					this.LogIronBaronyDraft("  Iron Barony draft: cannot afford " + unitData.Name + " this turn (cost " + cost + ", unit funds remaining " + remainingGold + ", upkeep " + upkeep + ", projected income if queued " + (remainingIncome - upkeep) + ")");
					if (unitData.Name == "Ogre" && this.GetOwnedQueuedAndPlannedUnitCount("Ogre") < 4)
					{
						this.IronBaronySavingForOgre = false;
						this.LogIronBaronyDraft("  Iron Barony mixed draft: Ogre is unaffordable, continuing with cheaper mass units instead of freezing the draft (" + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4)");
					}
					continue;
				}
				if (upkeep > remainingIncome)
				{
					this.LogIronBaronyDraft("  Iron Barony draft: skipping " + unitData.Name + " to keep next-turn upkeep positive (upkeep " + upkeep + ", projected income if queued " + (remainingIncome - upkeep) + ", unit funds if queued " + (remainingGold - cost) + ")");
					continue;
				}
				list.Add(unitData);
				remainingGold -= cost;
				remainingIncome -= upkeep;
				this.LogIronBaronyDraft("  Iron Barony draft: queued " + unitData.Name + " projection: cost " + cost + ", upkeep " + upkeep + ", projected remaining income " + remainingIncome + ", projected remaining unit funds " + remainingGold);
				if (unitData.Name == "Ogre")
				{
					this.IronBaronySavingForOgre = false;
					this.LogIronBaronyDraft("  Iron Barony Ogre saving flag cleared during purchase: Ogre queued successfully (" + (this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + list.Count((UnitData x) => x.Name == "Ogre")) + "/4 counting current purchase batch)");
				}
				this.ReserveResourcesForPlannedUnit(unitData, resources);
			}
			this.LogIronBaronyDraft("  Iron Barony draft: queued " + list.Count + " unit type(s), projected remaining income " + remainingIncome + ", projected remaining unit funds " + remainingGold);
			this.LogIronBaronyDraft("  Iron Barony draft queued by type: " + this.FormatUnitDataListByType(list));
			this.LogIronBaronyDraft("  Iron Barony draft queued order: " + this.FormatUnitDataListOrder(list));
			if (list.Count > 0)
			{
				AIActionPurchaseUnits aiactionPurchaseUnits = this.AI.ActionManager.CreateAction<AIActionPurchaseUnits>();
				aiactionPurchaseUnits.UnitTypes = list;
				this.AI.ActionManager.AddAction(aiactionPurchaseUnits, true);
			}
		}

		private bool CanPurchaseUnitType(UnitData Data)
		{
			return (Data.Class != UnitClasses.Naval || this.AI.Realm.HasHarbour) && (Data.Rank != UnitRanks.Unique || this.AI.Realm.GetUnitTypeCount(Data) < 1) && (Data.Rank != UnitRanks.Elite || this.AI.Realm.GetUnitTypeCount(Data) < this.AI.Realm.EliteUnitLimit) && (!(Data.Realm != this.AI.Realm.Name) || this.AI.Realm.GetUnitTypeCount(Data) < 4);
		}

		private NodeUnitData FindIronBaronyDeploymentArmyTarget(UnitQueueItem QueueItem, List<NodeUnitData> NodeData)
		{
			if (!this.IsIronBarony() || QueueItem == null || QueueItem.Unit == null || NodeData == null)
			{
				return null;
			}
			WorkingUnit unit = QueueItem.Unit;
			string deployCompositionName = this.GetIronBaronyCompositionUnitName(unit);
			if (deployCompositionName == null)
			{
				return null;
			}
			// Do not direct-deploy catapults into ordinary army slots. Siege movement handles
			// them separately when a fortified war goal exists. Specials/uniques are allowed
			// here, because the restored 6/6/4 field caps need Ogre/Witchdoctor/Gravedigger/Urkai
			// to fill the remaining stack slots.
			if (this.IsIronBaronyCatapult(unit))
			{
				return null;
			}
			List<NodeUnitData> candidates = new List<NodeUnitData>();
			foreach (NodeUnitData nodeData in NodeData)
			{
				if (nodeData == null || nodeData.Node == null || nodeData.Node.Province == null)
				{
					continue;
				}
				if (nodeData.Node.Province.OwnerRealm != this.AI.Realm || nodeData.Node.Province.Occupied)
				{
					continue;
				}
				WorkingStack stack = nodeData.Node.GetRealmStack(this.AI.Realm);
				if (stack == null || stack.Units == null || stack.Units.Count == 0)
				{
					continue;
				}
				if (nodeData.Units.Count >= 20)
				{
					continue;
				}
				if (!this.CanAddIronBaronyUnitToNodeData(unit, nodeData, "DeployUnits_ArmySlot"))
				{
					continue;
				}
				if (this.AI.Game.DestinationChecker.NodeOKToDeploy(unit, this.AI.Realm, nodeData.Node) != UnitMoveResult.OK)
				{
					continue;
				}
				candidates.Add(nodeData);
			}
			if (candidates.Count == 0)
			{
				return null;
			}
			return (from x in candidates
				let stack = x.Node.GetRealmStack(this.AI.Realm)
				orderby this.GetIronBaronyArmySlotSortKey(stack), stack.Units.Count descending, x.Node.ID
				select x).First<NodeUnitData>();
		}

		private int GetIronBaronyDeploymentArmySlotNumber(NodeUnitData Target, List<NodeUnitData> NodeData)
		{
			if (Target == null || Target.Node == null || NodeData == null)
			{
				return 1;
			}
			List<NodeUnitData> candidates = new List<NodeUnitData>();
			foreach (NodeUnitData nodeData in NodeData)
			{
				if (nodeData == null || nodeData.Node == null || nodeData.Node.Province == null)
				{
					continue;
				}
				if (nodeData.Node.Province.OwnerRealm != this.AI.Realm || nodeData.Node.Province.Occupied)
				{
					continue;
				}
				WorkingStack stack = nodeData.Node.GetRealmStack(this.AI.Realm);
				if (stack == null || stack.Units == null || stack.Units.Count == 0)
				{
					continue;
				}
				candidates.Add(nodeData);
			}
			List<NodeUnitData> ordered = (from x in candidates
				let stack = x.Node.GetRealmStack(this.AI.Realm)
				orderby this.GetIronBaronyArmySlotSortKey(stack), stack.Units.Count descending, x.Node.ID
				select x).ToList<NodeUnitData>();
			for (int i = 0; i < ordered.Count; i++)
			{
				if (ordered[i].Node == Target.Node)
				{
					return i + 1;
				}
			}
			return 1;
		}

		private bool IsIronBarony()
		{
			return this.AI != null && this.AI.Realm != null && this.AI.Realm.Name == "Iron Barony";
		}

		private bool IsMaledor()
		{
			return this.AI != null && this.AI.Realm != null && this.AI.Realm.Name == "Maledor";
		}

		private bool IsBoruvian()
		{
			return this.AI != null && this.AI.Realm != null && this.AI.Realm.Name == "Boruvian Empire";
		}

		private bool IsIronBaronyUsableFieldUnitForArmySlots(WorkingUnit Unit)
		{
			if (Unit == null || Unit.Disabled || Unit.Class == UnitClasses.Fort)
			{
				return false;
			}
			if (Unit.Class == UnitClasses.Naval && !Unit.Transport)
			{
				return false;
			}
			if (this.IsIronBaronyCatapult(Unit) && !this.IronBaronyHasFortifiedWarGoalOrFrontier())
			{
				return false;
			}
			return this.GetIronBaronyCompositionUnitName(Unit) != null;
		}

		private int GetIronBaronyUsableFieldUnitCountForArmySlots()
		{
			if (!this.IsIronBarony())
			{
				return 0;
			}
			int count = 0;
			foreach (WorkingUnit unit in this.AI.Realm.Units)
			{
				if (this.IsIronBaronyUsableFieldUnitForArmySlots(unit))
				{
					count++;
				}
			}
			return count;
		}

		private int GetIronBaronyDynamicArmySlotCount()
		{
			int usable = this.GetIronBaronyUsableFieldUnitCountForArmySlots();
			int slots = usable / 20;
			if (usable >= this.GetWarGoalStackMinimum() && slots < 1)
			{
				slots = 1;
			}
			if (slots < 1)
			{
				slots = 1;
			}
			if (slots > IronBaronyMaxDynamicArmySlots)
			{
				slots = IronBaronyMaxDynamicArmySlots;
			}
			return slots;
		}

		private int GetMinAttackUnits()
		{
			// Use the same hard lower bound for Iron Barony as for the general AI.
			// Stack formation still tries to build full 20-unit armies; this value only
			// prevents tiny fragments from attacking before CanWinProvince's simulator runs.
			return MinAttackUnits;
		}

		private int GetWarGoalStackMinimum()
		{
			// War-goal eligibility should not hard-lock Iron Barony to 20 units.
			// Formation/consolidation still fills toward 20, but attack permission is
			// decided by CanWinProvince after this global minimum is met.
			return this.IsIronBarony() ? MinAttackUnits : 8;
		}

		private string GetIronBaronyCompositionUnitName(WorkingUnit Unit)
		{
			if (!this.IsIronBarony() || Unit == null)
			{
				return null;
			}
			if (this.IsIronBaronyCatapult(Unit))
			{
				return "Orcish Catapult";
			}
			string unitName = (Unit.BaseType != null) ? Unit.BaseType.Name : Unit.DisplayName;
			switch (unitName)
			{
			case "Orcish Raiders":
			case "Orcish Archers":
			case "Wolfriders":
			case "Ogre":
			case "Witchdoctor":
			case "Gravedigger":
			case "War Captain Urkai":
				return unitName;
			}
			if (Unit.BaseName != null)
			{
				string[] knownTypes = new string[]
				{
					"Orcish Raiders",
					"Orcish Archers",
					"Wolfriders",
					"Ogre",
					"Witchdoctor",
					"Gravedigger",
					"War Captain Urkai",
					"Orcish Catapult"
				};
				foreach (string knownType in knownTypes)
				{
					if (Unit.BaseName.EndsWith("." + knownType))
					{
						return knownType;
					}
				}
			}
			return null;
		}


		private string GetMaledorCompositionUnitName(WorkingUnit Unit)
		{
			if (!this.IsMaledor() || Unit == null)
			{
				return null;
			}
			string unitName = (Unit.BaseType != null) ? Unit.BaseType.Name : Unit.DisplayName;
			switch (unitName)
			{
			case "The Worm":
			case "Justiciar":
			case "Inquisitor":
			case "Necromancer":
			case "Crow Hag":
			case "Sallowcoil Thugee":
			case "Headhunter":
			case "Cultist":
				return unitName;
			}
			if (Unit.BaseName != null)
			{
				string[] knownTypes = new string[]
				{
					"The Worm",
					"Justiciar",
					"Inquisitor",
					"Necromancer",
					"Crow Hag",
					"Sallowcoil Thugee",
					"Headhunter",
					"Cultist"
				};
				foreach (string knownType in knownTypes)
				{
					if (Unit.BaseName.EndsWith("." + knownType))
					{
						return knownType;
					}
				}
			}
			return null;
		}

		private string GetBoruvianCompositionUnitName(WorkingUnit Unit)
		{
			if (!this.IsBoruvian() || Unit == null)
			{
				return null;
			}
			string unitName = (Unit.BaseType != null) ? Unit.BaseType.Name : Unit.DisplayName;
			switch (unitName)
			{
			case "King's Retinue":
			case "Hussars":
			case "Light Brigade":
			case "Golden Infantry":
			case "Royal Pikes":
			case "Mercenary Crossbows":
			case "Imperial Garrison":
				return unitName;
			}
			if (Unit.BaseName != null)
			{
				string[] knownTypes = new string[]
				{
					"King's Retinue",
					"Hussars",
					"Light Brigade",
					"Golden Infantry",
					"Royal Pikes",
					"Mercenary Crossbows",
					"Imperial Garrison"
				};
				foreach (string knownType in knownTypes)
				{
					if (Unit.BaseName.EndsWith("." + knownType))
					{
						return knownType;
					}
				}
			}
			return null;
		}

		private string GetDoctrineCompositionUnitName(WorkingUnit Unit)
		{
			if (this.IsIronBarony())
			{
				return this.GetIronBaronyCompositionUnitName(Unit);
			}
			if (this.IsMaledor())
			{
				return this.GetMaledorCompositionUnitName(Unit);
			}
			if (this.IsBoruvian())
			{
				return this.GetBoruvianCompositionUnitName(Unit);
			}
			return null;
		}

		private int GetDoctrineCompositionLimit(string UnitName)
		{
			if (this.IsIronBarony())
			{
				return this.GetIronBaronyCompositionLimit(UnitName);
			}
			if (this.IsMaledor())
			{
				switch (UnitName)
				{
				case "Headhunter":
					return 6;
				case "Sallowcoil Thugee":
					return 4;
				case "Cultist":
					return 2;
				default:
					return 20;
				}
			}
			if (this.IsBoruvian())
			{
				switch (UnitName)
				{
				case "Golden Infantry":
					return 6;
				case "Royal Pikes":
					return 4;
				case "Mercenary Crossbows":
					return 5;
				case "Hussars":
					return 2;
				case "Light Brigade":
					return 3;
				case "King's Retinue":
					return 1;
				default:
					return 20;
				}
			}
			return 20;
		}

		private string GetDoctrineNameForLog()
		{
			if (this.IsIronBarony())
			{
				return "Iron Barony";
			}
			if (this.IsMaledor())
			{
				return "Maledor";
			}
			if (this.IsBoruvian())
			{
				return "Boruvian Empire";
			}
			return "Doctrine";
		}


		private int GetIronBaronyCompositionLimit(string UnitName)
		{
			// Mixed-unique doctrine keeps the current 20-unit stack system, but restores
			// the old good field composition caps. A normal full stack is expected to be
			// 6 Raiders + 6 Archers + 4 Wolfriders + 4 special/unique units.
			// Catapults are normally held as siege support, but when a fortified war goal/frontier exists
			// the army may reserve up to four slots for them: 16 field core + 4 catapults = full assault stack.
			switch (UnitName)
			{
			case "Orcish Raiders":
				return 6;
			case "Orcish Archers":
				return 6;
			case "Wolfriders":
				return 4;
			case "Orcish Catapult":
				return this.IronBaronyHasFortifiedWarGoalOrFrontier() ? 4 : 0;
			default:
				return 20;
			}
		}


		private int GetIronBaronyCompositionMinimum(string UnitName)
		{
			// No hard per-type minimums. Full 20-unit mass is the readiness rule.
			return 0;
		}


		private bool IronBaronyCompositionMinimumsSatisfied(int NodeID)
		{
			return true;
		}

		private bool IsIronBaronyArmyReadyForWar(int NodeID)
		{
			int total = this.GetPendingNodeUnitCount(NodeID);
			int field = this.GetPendingIronBaronyFieldUnitCount(NodeID);
			int catapults = this.GetPendingIronBaronyCompositionCount(NodeID, "Orcish Catapult");
			if (this.IronBaronyHasFortifiedWarGoalOrFrontier() && total >= 20 && field >= 16 && catapults >= 4)
			{
				return true;
			}
			return field >= 20;
		}

		private int GetPendingIronBaronyFieldUnitCount(int NodeID)
		{
			int total = this.GetPendingNodeUnitCount(NodeID);
			int catapults = this.GetPendingIronBaronyCompositionCount(NodeID, "Orcish Catapult");
			return Math.Max(0, total - catapults);
		}

		private string GetIronBaronyCompositionSummary(int NodeID)
		{
			return "field " + this.GetPendingIronBaronyFieldUnitCount(NodeID) + "/20, R " + this.GetPendingIronBaronyCompositionCount(NodeID, "Orcish Raiders") + "/6"
				+ ", A " + this.GetPendingIronBaronyCompositionCount(NodeID, "Orcish Archers") + "/6"
				+ ", W " + this.GetPendingIronBaronyCompositionCount(NodeID, "Wolfriders") + "/4"
				+ ", Ogre " + this.GetPendingIronBaronyCompositionCount(NodeID, "Ogre")
				+ ", WD " + this.GetPendingIronBaronyCompositionCount(NodeID, "Witchdoctor")
				+ ", GD " + this.GetPendingIronBaronyCompositionCount(NodeID, "Gravedigger")
				+ ", Urkai " + this.GetPendingIronBaronyCompositionCount(NodeID, "War Captain Urkai")
				+ ", Cat " + this.GetPendingIronBaronyCompositionCount(NodeID, "Orcish Catapult") + " held";
		}


		private string GetDoctrineCompositionUnitNameByID(int UnitID)
		{
			foreach (WorkingUnit unit in this.AI.Realm.Units)
			{
				if (unit != null && unit.ID == UnitID)
				{
					return this.GetDoctrineCompositionUnitName(unit);
				}
			}
			foreach (UnitQueueItem queueItem in this.AI.Realm.GetCurrentUnitQueue())
			{
				if (queueItem.Unit != null && queueItem.Unit.ID == UnitID)
				{
					return this.GetDoctrineCompositionUnitName(queueItem.Unit);
				}
			}
			return null;
		}

		private int GetDoctrineUnitCountInUnitIds(List<int> UnitIDs, string UnitName)
		{
			if (UnitIDs == null || string.IsNullOrEmpty(UnitName))
			{
				return 0;
			}
			int count = 0;
			foreach (int unitID in UnitIDs)
			{
				if (this.GetDoctrineCompositionUnitNameByID(unitID) == UnitName)
				{
					count++;
				}
			}
			return count;
		}

		private bool CanAddIronBaronyUnitToNodeData(WorkingUnit Unit, NodeUnitData TargetData, string DebugSource)
		{
			if ((!this.IsIronBarony() && !this.IsMaledor() && !this.IsBoruvian()) || Unit == null || TargetData == null)
			{
				return true;
			}
			string unitName = this.GetDoctrineCompositionUnitName(Unit);
			if (unitName == null)
			{
				return true;
			}
			int limit = this.GetDoctrineCompositionLimit(unitName);
			int count = this.GetDoctrineUnitCountInUnitIds(TargetData.Units, unitName);
			if (count >= limit)
			{
				int nodeID = (TargetData.Node == null) ? -1 : TargetData.Node.ID;
				this.AI.Log("  " + this.GetDoctrineNameForLog() + " composition: holding " + Unit.DisplayName + " back from node " + nodeID + " during " + DebugSource + " (" + unitName + " " + count + "/" + limit + ")");
				return false;
			}
			return true;
		}

		private int GetPendingIronBaronyCompositionCount(int NodeID, string UnitName)
		{
			if (this.PendingIronBaronyUnitTypesByNode != null)
			{
				Dictionary<string, int> counts;
				if (this.PendingIronBaronyUnitTypesByNode.TryGetValue(NodeID, out counts) && counts.ContainsKey(UnitName))
				{
					return counts[UnitName];
				}
				return 0;
			}
			int count = 0;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack.Node == null || stack.Node.ID != NodeID)
				{
					continue;
				}
				foreach (WorkingUnit unit in stack.Units)
				{
					if (this.GetDoctrineCompositionUnitName(unit) == UnitName)
					{
						count++;
					}
				}
			}
			return count;
		}

		private bool CanSendIronBaronyUnitToNode(WorkingUnit Unit, ActivePathNode TargetNode, string DebugSource)
		{
			if ((!this.IsIronBarony() && !this.IsMaledor() && !this.IsBoruvian()) || Unit == null || TargetNode == null)
			{
				return true;
			}
			if (this.IsIronBarony() && this.IsIronBaronyUnitRecovering(Unit))
			{
				this.AI.Log("  Iron Barony recovery: holding damaged " + Unit.DisplayName + " out of " + DebugSource + " (health " + (int)Unit.Health + "%)");
				return false;
			}
			if (!this.CanDoctrineGarrisonOnlyUnitMoveToNode(Unit, TargetNode))
			{
				this.AI.Log("  " + this.GetDoctrineNameForLog() + " homeland guard: holding " + Unit.DisplayName + " back from " + ((TargetNode.Province != null) ? TargetNode.Province.Name : ("node " + TargetNode.ID)) + " during " + DebugSource);
				return false;
			}
			string unitName = this.GetDoctrineCompositionUnitName(Unit);
			if (unitName == null)
			{
				return true;
			}
			int limit = this.GetDoctrineCompositionLimit(unitName);
			int count = this.GetPendingIronBaronyCompositionCount(TargetNode.ID, unitName);
			if (count >= limit)
			{
				this.AI.Log("  " + this.GetDoctrineNameForLog() + " composition: holding " + Unit.DisplayName + " back from node " + TargetNode.ID + " during " + DebugSource + " (" + unitName + " " + count + "/" + limit + ")");
				return false;
			}
			return true;
		}

		private bool IronBaronyStackHasRoomForAnyUnit(WorkingStack Source, ActivePathNode TargetNode)
		{
			if ((!this.IsIronBarony() && !this.IsMaledor() && !this.IsBoruvian()) || Source == null || TargetNode == null)
			{
				return true;
			}
			foreach (WorkingUnit unit in Source.Units)
			{
				if (this.CanSendIronBaronyUnitToNode(unit, TargetNode, "FindNearestLargerStack"))
				{
					return true;
				}
			}
			return false;
		}

		private void SeedPendingIronBaronyCompositionUnit(int NodeID, WorkingUnit Unit)
		{
			if ((!this.IsIronBarony() && !this.IsMaledor() && !this.IsBoruvian()) || this.PendingIronBaronyUnitTypesByNode == null || Unit == null)
			{
				return;
			}
			string unitName = this.GetDoctrineCompositionUnitName(Unit);
			if (unitName == null)
			{
				return;
			}
			Dictionary<string, int> counts;
			if (!this.PendingIronBaronyUnitTypesByNode.TryGetValue(NodeID, out counts))
			{
				counts = new Dictionary<string, int>();
				this.PendingIronBaronyUnitTypesByNode[NodeID] = counts;
			}
			if (!counts.ContainsKey(unitName))
			{
				counts[unitName] = 0;
			}
			counts[unitName]++;
		}

		private void RegisterPendingIronBaronyCompositionUnit(int NodeID, WorkingUnit Unit)
		{
			this.SeedPendingIronBaronyCompositionUnit(NodeID, Unit);
		}

		private bool ResourcesAvailableForPlannedUnit(UnitData unitData, Dictionary<string, int> resources)
		{
			foreach (KeyValuePair<string, int> required in unitData.GetRequiredResources())
			{
				int available = 0;
				resources.TryGetValue(required.Key, out available);
				if (available < required.Value)
				{
					return false;
				}
			}
			return true;
		}

		private void ReserveResourcesForPlannedUnit(UnitData unitData, Dictionary<string, int> resources)
		{
			foreach (KeyValuePair<string, int> required in unitData.GetRequiredResources())
			{
				if (resources.ContainsKey(required.Key))
				{
					resources[required.Key] -= Math.Min(resources[required.Key], required.Value);
				}
			}
		}

		private bool CanPurchaseUnitTypeWithPlanned(UnitData Data, List<UnitData> PlannedUnits)
		{
			int count = this.GetOwnedAndQueuedUnitCount(Data) + PlannedUnits.Count((UnitData x) => x.Name == Data.Name);
			return (Data.Class != UnitClasses.Naval || this.AI.Realm.HasHarbour) && (Data.Rank != UnitRanks.Unique || count < 1) && (Data.Rank != UnitRanks.Elite || count < this.AI.Realm.EliteUnitLimit) && (!(Data.Realm != this.AI.Realm.Name) || count < 4);
		}

		private int GetOwnedAndQueuedUnitCount(UnitData unitData)
		{
			int count = 0;
			foreach (WorkingUnit unit in this.AI.Realm.Units)
			{
				if (unit.BaseType != null && unit.BaseType.Name == unitData.Name)
				{
					count++;
				}
			}
			foreach (UnitQueueItem queueItem in this.AI.Realm.GetCurrentUnitQueue())
			{
				WorkingUnit unit2 = queueItem.Unit;
				if (unit2 != null && unit2.BaseType != null && unit2.BaseType.Name == unitData.Name)
				{
					count++;
				}
			}
			return count;
		}

		private int GetOwnedQueuedAndPlannedUnitCount(UnitData unitData)
		{
			return this.GetOwnedQueuedAndPlannedUnitCount(unitData.Name);
		}

		private int GetOwnedQueuedAndPlannedUnitCount(string unitName)
		{
			int count = 0;
			foreach (WorkingUnit unit in this.AI.Realm.Units)
			{
				if (this.IronBaronyWorkingUnitMatchesName(unit, unitName))
				{
					count++;
				}
			}
			foreach (UnitQueueItem queueItem in this.AI.Realm.GetCurrentUnitQueue())
			{
				WorkingUnit queuedUnit = queueItem.Unit;
				if (this.IronBaronyWorkingUnitMatchesName(queuedUnit, unitName))
				{
					count++;
				}
			}
			if (this.PurchaseList != null)
			{
				count += this.PurchaseList.Count((UnitData x) => x.Name == unitName);
			}
			return count;
		}

		private bool IronBaronyWorkingUnitMatchesName(WorkingUnit unit, string unitName)
		{
			if (unit == null || string.IsNullOrEmpty(unitName))
			{
				return false;
			}
			if (unitName == "Orcish Catapult" && this.IsIronBaronyCatapult(unit))
			{
				return true;
			}
			if (unit.BaseType != null && unit.BaseType.Name == unitName)
			{
				return true;
			}
			if (unit.DisplayName == unitName)
			{
				return true;
			}
			return unit.BaseName != null && unit.BaseName.EndsWith("." + unitName);
		}

		private UnitData FindTrainableUnitByName(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, string unitName)
		{
			foreach (KeyValuePair<UnitData, UnitTrainStates> kvp in trainableUnits)
			{
				if (kvp.Key.Name == unitName)
				{
					return kvp.Key;
				}
			}
			return null;
		}

		private void AddIronBaronyUnitIfBelowTarget(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, string unitName, int targetCount)
		{
			UnitData unit = this.FindTrainableUnitByName(trainableUnits, unitName);
			if (unit == null)
			{
				return;
			}
			if (this.GetOwnedQueuedAndPlannedUnitCount(unit) < targetCount)
			{
				this.PurchaseList.Add(unit);
			}
		}

		private bool IronBaronyNeedsMoreOgres()
		{
			return this.GetOwnedQueuedAndPlannedUnitCount("Ogre") < 4;
		}

		private bool PrepareIronBaronyOgreSavingDraft(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, int netIncome)
		{
			if (!this.IronBaronyNeedsMoreOgres())
			{
				if (this.IronBaronySavingForOgre)
				{
					this.LogIronBaronyDraft("  Iron Barony Ogre saving flag cleared: Ogre target reached (" + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4)");
				}
				this.IronBaronySavingForOgre = false;
				return false;
			}
			UnitData ogre = this.FindTrainableUnitByName(trainableUnits, "Ogre");
			if (ogre == null)
			{
				this.IronBaronySavingForOgre = false;
				this.LogIronBaronyDraft("  Iron Barony Ogre saving: Ogre not trainable this turn; normal draft continues (" + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4)");
				return false;
			}
			int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(ogre);
			int upkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(ogre);
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			bool resourcesReady = this.ResourcesAvailableForPlannedUnit(ogre, resources);
			string marketSummary;
			bool marketCanSupply = this.ResourcesAvailableForPlannedUnitOrMarketCanSupply(ogre, resources, out marketSummary);
			bool canAfford = cost <= this.Funds.CurrentGold;
			bool incomeSafe = upkeep <= netIncome;
			if (!incomeSafe)
			{
				this.IronBaronySavingForOgre = false;
				this.LogIronBaronyDraft("  Iron Barony Ogre draft blocked by upkeep safety: Ogre " + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4, cost " + cost + ", upkeep " + upkeep + ", income surplus " + netIncome + ". No Ogre placeholder added; normal draft continues.");
				return false;
			}
			if (!resourcesReady)
			{
				if (marketCanSupply)
				{
					// This is a resource-target placeholder for AIResourcesManager. Since we return
					// immediately, it cannot squeeze mass-unit draft slots this turn.
					this.PurchaseList.Add(ogre);
					this.IronBaronySavingForOgre = true;
					this.LogIronBaronyDraft("  Iron Barony Ogre saving flag active: Ogre " + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4 needs resources, market can supply (" + marketSummary + "). Draft reserved so resource manager can buy requirements. Cost " + cost + ", unit funds " + this.Funds.CurrentGold + ".");
					return true;
				}
				this.IronBaronySavingForOgre = false;
				this.LogIronBaronyDraft("  Iron Barony Ogre resource wait: Ogre " + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4 needs resources, but market cannot supply now (" + marketSummary + "). No Ogre placeholder added; normal draft continues.");
				return false;
			}
			if (!canAfford)
			{
				// Resources are already ready; no PurchaseList placeholder is needed. Reserve the
				// draft without consuming one of the 20 purchase slots.
				this.IronBaronySavingForOgre = true;
				this.LogIronBaronyDraft("  Iron Barony Ogre saving flag active: Ogre " + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4, cost " + cost + ", upkeep " + upkeep + ", unit funds " + this.Funds.CurrentGold + ", income surplus " + netIncome + ", resources ready. Draft reserved for Ogre; no placeholder added.");
				return true;
			}
			this.PurchaseList.Add(ogre);
			this.IronBaronySavingForOgre = false;
			this.LogIronBaronyDraft("  Iron Barony Ogre saving flag cleared for this turn: queuing Ogre " + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4, cost " + cost + ", upkeep " + upkeep + ". Normal draft may continue with remaining funds.");
			return false;
		}

		private ResourceData GetResourceDataByName(string resourceName)
		{
			if (string.IsNullOrEmpty(resourceName))
			{
				return null;
			}
			foreach (ResourceData resource in this.AI.Game.GameCore.Data.Resources.Values)
			{
				if (resource != null && resource.ResourceName == resourceName)
				{
					return resource;
				}
			}
			return null;
		}

		private bool ResourcesAvailableForPlannedUnitOrMarketCanSupply(UnitData unitData, Dictionary<string, int> resources, out string Summary)
		{
			List<string> parts = new List<string>();
			bool ok = true;
			foreach (KeyValuePair<string, int> required in unitData.GetRequiredResources())
			{
				int available = 0;
				resources.TryGetValue(required.Key, out available);
				int missing = Math.Max(0, required.Value - available);
				if (missing <= 0)
				{
					parts.Add(required.Key + " ready " + available + "/" + required.Value);
					continue;
				}
				ResourceData resource = this.GetResourceDataByName(required.Key);
				int marketQty = (resource == null) ? 0 : this.AI.Game.Marketplace.GetQuantity(resource);
				parts.Add(required.Key + " missing " + missing + ", market " + marketQty);
				if (marketQty < missing)
				{
					ok = false;
				}
			}
			Summary = (parts.Count == 0) ? "no resource requirements" : string.Join("; ", parts.ToArray());
			return ok;
		}

		private bool AddIronBaronyMassUnit(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, string unitName, ref int projectedGold)
		{
			if (this.PurchaseList.Count >= 20)
			{
				return false;
			}
			UnitData unit = this.FindTrainableUnitByName(trainableUnits, unitName);
			if (unit == null)
			{
				return false;
			}
			int targetCount = this.GetIronBaronyMassDraftTarget(unitName);
			if (targetCount > 0 && this.GetOwnedQueuedAndPlannedUnitCount(unitName) >= targetCount)
			{
				return false;
			}
			int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unit);
			if (cost > projectedGold)
			{
				return false;
			}
			this.PurchaseList.Add(unit);
			projectedGold -= cost;
			return true;
		}

		private int GetIronBaronyDraftArmySlots()
		{
			int readyArmies = 0;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack != null && stack.Node != null && this.IsIronBaronyArmyReadyForWar(stack.Node.ID))
				{
					readyArmies++;
				}
			}
			// Fill one full 20-stack, then a second, then a third. Do not ask the economy
			// to build all 60 Raiders before the first army is ready.
			return Math.Max(1, Math.Min(3, readyArmies + 1));
		}

		private int GetIronBaronyMassDraftTarget(string unitName)
		{
			int slots = this.GetIronBaronyDraftArmySlots();
			switch (unitName)
			{
			case "Orcish Raiders":
				return 6 * slots;
			case "Orcish Archers":
				return 6 * slots;
			case "Wolfriders":
				return 4 * slots;
			default:
				return 0;
			}
		}


		private string GetIronBaronyMassDraftTargetSummary()
		{
			return "mixed unique slots " + this.GetIronBaronyDraftArmySlots()
				+ ", Raiders " + this.GetOwnedQueuedAndPlannedUnitCount("Orcish Raiders") + "/" + this.GetIronBaronyMassDraftTarget("Orcish Raiders")
				+ ", Archers " + this.GetOwnedQueuedAndPlannedUnitCount("Orcish Archers") + "/" + this.GetIronBaronyMassDraftTarget("Orcish Archers")
				+ ", Wolfriders " + this.GetOwnedQueuedAndPlannedUnitCount("Wolfriders") + "/" + this.GetIronBaronyMassDraftTarget("Wolfriders")
				+ ", Urkai " + this.GetOwnedQueuedAndPlannedUnitCount("War Captain Urkai") + "/1"
				+ ", Ogre " + this.GetOwnedQueuedAndPlannedUnitCount("Ogre") + "/4"
				+ ", Witchdoctor " + this.GetOwnedQueuedAndPlannedUnitCount("Witchdoctor") + "/4"
				+ ", Gravedigger " + this.GetOwnedQueuedAndPlannedUnitCount("Gravedigger") + "/4"
				+ ", Catapult " + this.GetOwnedQueuedAndPlannedUnitCount("Orcish Catapult") + "/" + this.GetIronBaronyDesiredCatapultCount();
		}


		private void TrimIronBaronyPurchaseListForEconomy(int netIncome)
		{
			while (this.PurchaseList.Count > 0 && this.GetPurchaseListUpkeep() > netIncome)
			{
				int removeIndex = -1;
				for (int i = this.PurchaseList.Count - 1; i >= 0; i--)
				{
					if (this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(this.PurchaseList[i]) > 0)
					{
						removeIndex = i;
						break;
					}
				}
				if (removeIndex < 0)
				{
					break;
				}
				int beforeUpkeep = this.GetPurchaseListUpkeep();
				UnitData removedUnit = this.PurchaseList[removeIndex];
				int removedUpkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(removedUnit);
				this.PurchaseList.RemoveAt(removeIndex);
				int afterUpkeep = this.GetPurchaseListUpkeep();
				this.LogIronBaronyDraft("  Iron Barony draft: removing " + removedUnit.Name + " to keep next-turn upkeep positive (unit upkeep " + removedUpkeep + ", list upkeep " + beforeUpkeep + " -> " + afterUpkeep + ", projected income " + (netIncome - beforeUpkeep) + " -> " + (netIncome - afterUpkeep) + ")");
			}
		}

		private int GetPurchaseListUpkeep()
		{
			return this.GetUnitDataListUpkeep(this.PurchaseList);
		}

		private int GetUnitDataListUpkeep(IEnumerable<UnitData> units)
		{
			int upkeep = 0;
			if (units == null)
			{
				return upkeep;
			}
			foreach (UnitData unit in units)
			{
				if (unit != null)
				{
					upkeep += this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unit);
				}
			}
			return upkeep;
		}

		private int GetUnitDataListCost(IEnumerable<UnitData> units)
		{
			int cost = 0;
			if (units == null)
			{
				return cost;
			}
			foreach (UnitData unit in units)
			{
				if (unit != null)
				{
					cost += this.AI.Realm.UnitPurchaseManager.GetUnitCost(unit);
				}
			}
			return cost;
		}

		private string FormatUnitDataListByType(IEnumerable<UnitData> units)
		{
			if (units == null)
			{
				return "none";
			}
			List<UnitData> unitList = units.Where((UnitData x) => x != null).ToList<UnitData>();
			if (unitList.Count == 0)
			{
				return "none";
			}
			List<string> parts = new List<string>();
			foreach (IGrouping<string, UnitData> group in unitList.GroupBy((UnitData x) => x.Name))
			{
				parts.Add(group.Count() + "x " + group.Key);
			}
			return string.Join(", ", parts.ToArray());
		}

		private string FormatUnitDataListOrder(IEnumerable<UnitData> units)
		{
			if (units == null)
			{
				return "none";
			}
			List<string> names = new List<string>();
			foreach (UnitData unit in units)
			{
				if (unit != null)
				{
					names.Add(unit.Name);
				}
			}
			if (names.Count == 0)
			{
				return "none";
			}
			return string.Join(" -> ", names.ToArray());
		}

		private int GetResourceReplaceCost()
		{
			switch (this.AI.Game.GameCore.Settings.GetEnumeratedSetting("Difficulty"))
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

		private int GetCapitalGarrisonTarget(int EnemyDistance)
		{
			if (EnemyDistance > 0 && EnemyDistance <= CapitalThreatDistance)
			{
				return ThreatenedCapitalGarrison;
			}
			if (EnemyDistance > CapitalThreatDistance && EnemyDistance <= CapitalCautionDistance)
			{
				return CautiousCapitalGarrison;
			}
			return SafeCapitalGarrison;
		}

		private int GetPlannedNodeUnitCount(ActivePathNode Node)
		{
			if (Node == null)
			{
				return 0;
			}
			int actualCount = this.GetActualNodeUnitCount(Node.ID);
			int pendingCount = actualCount;
			if (this.PendingUnitsByNode != null && this.PendingUnitsByNode.TryGetValue(Node.ID, out pendingCount))
			{
				return Math.Max(actualCount, pendingCount);
			}
			return actualCount;
		}

		private bool CapitalDefenseEmergencyActive()
		{
			WorkingProvince capitol = this.AI.Realm.CapitolProvince;
			if (capitol == null || capitol.Occupied || capitol.LandNode == null)
			{
				return false;
			}
			int enemyDistance = this.GetEnemyDistanceFromCapital(CapitalSafeRadius);
			int targetGarrison = this.GetCapitalGarrisonTarget(enemyDistance);
			return targetGarrison == ThreatenedCapitalGarrison && this.GetPlannedNodeUnitCount(capitol.LandNode) < targetGarrison;
		}

		private void ReinforceCapitol()
		{
			this.AI.Realm.StacksChanged();
			WorkingProvince capitol = this.AI.Realm.CapitolProvince;
			if (capitol == null || capitol.Occupied || capitol.LandNode == null)
			{
				return;
			}
			int enemyDistance = this.GetEnemyDistanceFromCapital(CapitalSafeRadius);
			int targetGarrison = this.GetCapitalGarrisonTarget(enemyDistance);
			bool emergency = enemyDistance > 0 && enemyDistance <= CapitalThreatDistance;
			int capitolUnits = this.GetPlannedNodeUnitCount(capitol.LandNode);
			if (capitolUnits >= targetGarrison)
			{
				return;
			}
			// Raider-swarm Iron Barony must not bleed its attack stacks back into the capital.
			// The old cautious/safe capital garrison rule created a loop:
			// consolidation filled the front to 20, then ReinforceCapitol stole one unit back.
			// Only a true capital emergency may override the swarm doctrine.
			if (this.IsIronBarony() && !emergency)
			{
				this.AI.Log("  Iron Barony raider swarm: skipping non-emergency capital reinforcement (capital " + capitolUnits + "/" + targetGarrison + "); attack stacks keep their mass");
				this.LogWarGoals("  Iron Barony raider swarm: skipping non-emergency capital reinforcement (capital " + capitolUnits + "/" + targetGarrison + "); attack stacks keep their mass");
				return;
			}
			int needed = targetGarrison - capitolUnits;
			if (emergency)
			{
				this.AI.Log("  Capitol defense emergency: enemy army " + enemyDistance + " province(s) away; garrison " + capitolUnits + "/" + targetGarrison + ", recalling " + needed + " units");
				this.LogWarGoals("  Capitol defense emergency: enemy army " + enemyDistance + " province(s) away; garrison " + capitolUnits + "/" + targetGarrison + ", recalling " + needed + " units");
			}
			else if (enemyDistance > 0)
			{
				this.AI.Log("  Enemy army " + enemyDistance + " provinces from capitol; garrison low (" + capitolUnits + "/" + targetGarrison + "), seeking " + needed + " reinforcements");
			}
			else
			{
				this.AI.Log("  No enemy army within " + CapitalSafeRadius + " provinces of capitol; minimal garrison low (" + capitolUnits + "/" + targetGarrison + "), seeking " + needed + " reinforcements");
			}
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			foreach (WorkingStack stack in this.AI.Realm.Stacks.ToList())
			{
				if (needed <= 0)
				{
					break;
				}
				if (stack.Node == null || stack.Node.Province == null || stack.Node.Province.IsCapitol || stack.Node.Province.Occupied)
				{
					continue;
				}
				if (this.IsIronBarony() && this.ShouldProtectIronBaronyStackFromCapitalRecall(stack, emergency))
				{
					continue;
				}
				if (stack.Units.Count <= 3)
				{
					continue;
				}
				int canSpare = stack.Units.Count - 3;
				foreach (WorkingUnit unit in stack.Units)
				{
					if (needed <= 0 || canSpare <= 0)
					{
						break;
					}
					if (unit.Class == UnitClasses.Fort || unit.MovePoints <= 0f)
					{
						continue;
					}
					if (unit.Class == UnitClasses.Naval && !unit.Transport)
					{
						continue;
					}
					List<WorkingUnit> unitList = new List<WorkingUnit>();
					unitList.Add(unit);
					SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(stack.Node, capitol.LandNode, unitList, true, this.AI.Realm, false);
					if (path.PathPoints.Count > 0 && unit.MovePoints >= path.TotalMoveCost)
					{
						moveList.Add(new UnitMoveData(unit, capitol.LandNode, path));
						this.RegisterPendingUnit(capitol.LandNode.ID);
						needed--;
						canSpare--;
					}
				}
			}
			if (moveList.Count > 0)
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = emergency ? "ReinforceCapitol_Emergency" : "ReinforceCapitol";
				moveAction.MoveTargets = moveList;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.AI.Log("  Reinforcing capitol with " + moveList.Count + " units");
			}
		}


		private bool ShouldProtectIronBaronyStackFromCapitalRecall(WorkingStack Stack, bool Emergency)
		{
			if (!this.IsIronBarony() || Stack == null || Stack.Node == null || Stack.Node.Province == null)
			{
				return false;
			}
			if (Stack.Node.Province.IsCapitol)
			{
				return false;
			}
			if (!Emergency)
			{
				return true;
			}
			int fieldCount = this.GetPendingIronBaronyFieldUnitCount(Stack.Node.ID);
			int totalCount = this.GetPendingNodeUnitCount(Stack.Node.ID);
			if (fieldCount >= this.GetWarGoalStackMinimum() || totalCount >= 20)
			{
				this.AI.Log("  Iron Barony capital emergency: preserving attack-capable stack at node " + Stack.Node.ID + " (field " + fieldCount + "/20, total " + totalCount + "/20, min " + this.GetWarGoalStackMinimum() + ")");
				return true;
			}
			return false;
		}

		private void ConsolidateArmies()
		{
			this.AI.Realm.StacksChanged();
			this.AI.Log("");
			this.AI.Log("Unit manager updating (consolidation phase)");
			if (this.IsIronBarony())
			{
				this.ConsolidateIronBaronyArmies();
				return;
			}
			this.ReinforceCapitol();
			WorkingStack heroStack = null;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack.Hero != null && stack.Node.Province != null && this.ProvinceCanServeAsArmyBase(stack.Node.Province))
				{
					if (heroStack == null || stack.Units.Count > heroStack.Units.Count)
					{
						heroStack = stack;
					}
				}
			}
			List<WorkingStack> smallStacks = new List<WorkingStack>();
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack.Units.Count > 0 && stack.Node.Province != null && this.ProvinceCanServeAsArmyBase(stack.Node.Province) && stack != heroStack)
				{
					if (stack.Node.Province.IsCapitol)
					{
						continue;
					}
					bool isSmall = stack.Units.Count < (this.IsIronBarony() ? 18 : 8);
					bool shouldFeedHero = heroStack != null && heroStack.Units.Count < 20 && stack.Units.Count < (this.IsIronBarony() ? 18 : 10) && stack.Hero == null;
					if (isSmall || shouldFeedHero)
					{
						smallStacks.Add(stack);
					}
				}
			}
			if (smallStacks.Count == 0)
			{
				this.AI.Log("  No small stacks to consolidate");
				return;
			}
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			foreach (WorkingStack smallStack in smallStacks)
			{
				WorkingStack mergeTarget = null;
				bool reinforceHero = heroStack != null && heroStack.Units.Count < 20 && this.AI.RNG.Next(100) < 70;
				if (reinforceHero)
				{
					mergeTarget = heroStack;
				}
				else
				{
					mergeTarget = this.FindNearestLargerStack(smallStack, this.IsIronBarony() ? null : smallStacks);
				}
				if (mergeTarget == null || mergeTarget == smallStack)
				{
					continue;
				}
				foreach (WorkingUnit unit in smallStack.Units)
				{
					if (this.IsIronBaronyCatapult(unit))
					{
						continue;
					}
					if (unit.MovePoints <= 0f || unit.Class == UnitClasses.Fort)
					{
						continue;
					}
					if (unit.Class == UnitClasses.Naval && !unit.Transport && mergeTarget.Node.NodeType == PathNodeTypes.Land)
					{
						continue;
					}
					if (!this.CanSendToNode(mergeTarget.Node.ID))
					{
						break;
					}
					if (!this.CanSendIronBaronyUnitToNode(unit, mergeTarget.Node, "ConsolidateArmies"))
					{
						continue;
					}
					List<WorkingUnit> unitList = new List<WorkingUnit>();
					unitList.Add(unit);
					SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(smallStack.Node, mergeTarget.Node, unitList, true, this.AI.Realm, false);
					if (path.PathPoints.Count > 0 && unit.MovePoints >= path.TotalMoveCost)
					{
						moveList.Add(new UnitMoveData(unit, mergeTarget.Node, path));
						this.RegisterPendingUnit(mergeTarget.Node.ID, unit);
						this.AI.Log("  Moving " + unit.DisplayName + " toward " + (mergeTarget.Hero != null ? "hero " : "") + "stack at " + mergeTarget.Node.ID);
					}
				}
			}
			if (moveList.Count > 0)
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = "ConsolidateArmies";
				moveAction.MoveTargets = moveList;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.AI.Log("  Consolidation: moving " + moveList.Count + " units");
			}
		}

		private void ConsolidateIronBaronyArmies()
		{
			this.ReinforceCapitol();
			this.RotateOutIronBaronyDamagedUnits();
			this.NormalizeIronBaronyOverfilledStacks();
			List<WorkingStack> armyStacks = new List<WorkingStack>();
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (this.IsEligibleIronBaronyArmyStack(stack))
				{
					armyStacks.Add(stack);
				}
			}
			if (armyStacks.Count == 0)
			{
				this.AI.Log("  Iron Barony formation: no eligible army stacks");
				return;
			}
			int dynamicArmySlots = this.GetIronBaronyDynamicArmySlotCount();
			int usableFieldUnits = this.GetIronBaronyUsableFieldUnitCountForArmySlots();
			List<WorkingStack> orderedTargets = (from x in armyStacks
				orderby this.GetIronBaronyArmySlotSortKey(x), x.Units.Count descending, ((x.Node != null) ? x.Node.ID : 0)
				select x).Take(dynamicArmySlots).ToList<WorkingStack>();
			this.AI.Log("  Iron Barony dynamic army slots: " + dynamicArmySlots + " from " + usableFieldUnits + " usable field units");
			this.AI.Log("  Iron Barony formation slots:");
			int slotNumber = 1;
			foreach (WorkingStack stack in orderedTargets)
			{
				this.AI.Log("    Army #" + slotNumber + ": node " + stack.Node.ID + ", units " + stack.Units.Count + this.GetIronBaronyArmySlotLabel(stack));
				slotNumber++;
			}
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			HashSet<int> movedUnitIDs = (this.PendingIronBaronyMovedUnitIDs != null) ? new HashSet<int>(this.PendingIronBaronyMovedUnitIDs) : new HashSet<int>();
			slotNumber = 1;
			foreach (WorkingStack target in orderedTargets)
			{
				if (target.Node == null)
				{
					slotNumber++;
					continue;
				}
				int targetCount = this.GetPendingIronBaronyFieldUnitCount(target.Node.ID);
				bool targetReady = this.IsIronBaronyArmyReadyForWar(target.Node.ID);
				if (targetReady && this.GetPendingNodeUnitCount(target.Node.ID) >= 20)
				{
					this.AI.Log("  Iron Barony Army #" + slotNumber + " already full mixed-stack ready at node " + target.Node.ID + " (" + targetCount + "/20 field, total " + this.GetPendingNodeUnitCount(target.Node.ID) + "/20, " + this.GetIronBaronyCompositionSummary(target.Node.ID) + "); leaving it intact");
					slotNumber++;
					continue;
				}
				if (targetReady)
				{
					this.AI.Log("  Iron Barony Army #" + slotNumber + " mixed-stack ready but not full at node " + target.Node.ID + " (" + targetCount + "/20 field, total " + this.GetPendingNodeUnitCount(target.Node.ID) + "/20, " + this.GetIronBaronyCompositionSummary(target.Node.ID) + "); topping up if donors can march");
				}
				else
				{
					this.AI.Log("  Iron Barony Army #" + slotNumber + " filling at node " + target.Node.ID + " (" + targetCount + "/20 field, total " + this.GetPendingNodeUnitCount(target.Node.ID) + "/20, " + this.GetIronBaronyCompositionSummary(target.Node.ID) + ")");
				}
				int safety = 0;
				while ((targetCount < 20 || this.GetPendingNodeUnitCount(target.Node.ID) < 20) && this.GetPendingNodeUnitCount(target.Node.ID) < 20 && safety < 40)
				{
					safety++;
					UnitMoveData moveData = this.FindBestIronBaronyFormationMove(target, armyStacks, movedUnitIDs);
					if (moveData == null)
					{
						break;
					}
					moveList.Add(moveData);
					movedUnitIDs.Add(moveData.Unit.ID);
					int sourceNodeID = moveData.Unit.OwnerStack.Node.ID;
					this.RegisterPendingUnitMove(sourceNodeID, target.Node.ID, moveData.Unit);
					targetCount = this.GetPendingIronBaronyFieldUnitCount(target.Node.ID);
					this.AI.Log("    Army #" + slotNumber + ": pulling " + moveData.Unit.DisplayName + " from node " + sourceNodeID + " to node " + target.Node.ID + " (" + targetCount + "/20 field, total " + this.GetPendingNodeUnitCount(target.Node.ID) + "/20, " + this.GetIronBaronyCompositionSummary(target.Node.ID) + ")");
				}
				if ((targetCount < 20 || this.GetPendingNodeUnitCount(target.Node.ID) < 20) && this.GetPendingNodeUnitCount(target.Node.ID) < 20)
				{
					List<UnitMoveData> marchMoves = this.FindBestIronBaronyStackMarchTowardArmy(target, armyStacks, movedUnitIDs);
					if (marchMoves != null && marchMoves.Count > 0)
					{
						int marchSourceNodeID = marchMoves[0].Unit.OwnerStack.Node.ID;
						int marchDestinationNodeID = marchMoves[0].TargetNode.ID;
						foreach (UnitMoveData marchMove in marchMoves)
						{
							moveList.Add(marchMove);
							movedUnitIDs.Add(marchMove.Unit.ID);
							this.RegisterPendingUnitMove(marchSourceNodeID, marchDestinationNodeID, marchMove.Unit);
						}
						this.AI.Log("    Army #" + slotNumber + ": moving donor stack from node " + marchSourceNodeID + " to staging node " + marchDestinationNodeID + " near army node " + target.Node.ID + " (" + marchMoves.Count + " unit(s)); no distant individual merge");
					}
				}
				if (!this.IsIronBaronyArmyReadyForWar(target.Node.ID))
				{
					this.AI.Log("  Iron Barony Army #" + slotNumber + " still below preferred 20-unit mixed-stack mass at node " + target.Node.ID + " (" + targetCount + "/20 field, total " + this.GetPendingNodeUnitCount(target.Node.ID) + "/20, " + this.GetIronBaronyCompositionSummary(target.Node.ID) + "); continuing to evaluate other dynamic army slots");
				}
				slotNumber++;
			}
			if (moveList.Count > 0)
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = "ConsolidateIronBaronyArmies";
				moveAction.MoveTargets = moveList;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.AI.Log("  Iron Barony formation: moving " + moveList.Count + " units into stable army slots");
			}
			else
			{
				this.AI.Log("  Iron Barony formation: no unit moves needed or possible");
			}
		}

		private void NormalizeIronBaronyOverfilledStacks()
		{
			if (!this.IsIronBarony())
			{
				return;
			}
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			int maxSurplusMovesPerTurn = 8;
			string[] limitedTypes = new string[]
			{
				"Orcish Raiders",
				"Orcish Archers",
				"Wolfriders",
				"Orcish Catapult"
			};
			foreach (WorkingStack stack in new List<WorkingStack>(this.AI.Realm.Stacks))
			{
				if (moveList.Count >= maxSurplusMovesPerTurn)
				{
					break;
				}
				if (!this.IsEligibleIronBaronyArmyStack(stack) || stack.Node.Province.IsCapitol)
				{
					continue;
				}
				foreach (string unitName in limitedTypes)
				{
					if (moveList.Count >= maxSurplusMovesPerTurn)
					{
						break;
					}
					int limit = this.GetIronBaronyCompositionLimit(unitName);
					int count = this.GetPendingIronBaronyCompositionCount(stack.Node.ID, unitName);
					while (count > limit && moveList.Count < maxSurplusMovesPerTurn)
					{
						WorkingUnit surplusUnit = this.FindIronBaronySurplusCompositionUnit(stack, unitName);
						if (surplusUnit == null)
						{
							break;
						}
						UnitMoveData moveData = this.FindIronBaronyDamagedUnitRetreatMove(surplusUnit, stack);
						if (moveData == null)
						{
							break;
						}
						moveList.Add(moveData);
						this.RegisterPendingUnitMove(stack.Node.ID, moveData.TargetNode.ID, surplusUnit);
						count = this.GetPendingIronBaronyCompositionCount(stack.Node.ID, unitName);
						this.AI.Log("  Iron Barony formation normalization: moving surplus " + surplusUnit.DisplayName + " out of node " + stack.Node.ID + " to node " + moveData.TargetNode.ID + " (" + unitName + " " + count + "/" + limit + " remains, " + this.GetIronBaronyCompositionSummary(stack.Node.ID) + ")");
					}
				}
			}
			if (moveList.Count > 0)
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = "IronBaronyNormalizeComposition";
				moveAction.MoveTargets = moveList;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.AI.Log("  Iron Barony formation normalization: moving " + moveList.Count + " surplus unit(s) out of overfilled stacks");
			}
		}

		private WorkingUnit FindIronBaronySurplusCompositionUnit(WorkingStack Stack, string UnitName)
		{
			if (Stack == null || string.IsNullOrEmpty(UnitName))
			{
				return null;
			}
			WorkingUnit best = null;
			int bestScore = int.MaxValue;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit == null || unit.OwnerStack == null || unit.MovePoints <= 0f || unit.Class == UnitClasses.Fort || unit.Disabled)
				{
					continue;
				}
				if (this.IsIronBaronyUnitAlreadyPendingMove(unit))
				{
					continue;
				}
				if (this.GetIronBaronyCompositionUnitName(unit) != UnitName)
				{
					continue;
				}
				int score = (int)unit.Health;
				if (this.IsIronBaronyUnitRecovering(unit))
				{
					score -= 100;
				}
				if (best == null || score < bestScore)
				{
					best = unit;
					bestScore = score;
				}
			}
			return best;
		}

		private void RotateOutIronBaronyDamagedUnits()
		{
			if (!this.IsIronBarony())
			{
				return;
			}
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			HashSet<int> movedUnitIDs = new HashSet<int>();
			int maxRotationsPerTurn = 4;
			foreach (WorkingStack stack in new List<WorkingStack>(this.AI.Realm.Stacks))
			{
				if (moveList.Count >= maxRotationsPerTurn)
				{
					break;
				}
				if (!this.IsEligibleIronBaronyArmyStack(stack) || stack.Node.Province.IsCapitol)
				{
					continue;
				}
				if (this.GetPendingNodeUnitCount(stack.Node.ID) < 12)
				{
					continue;
				}
				foreach (WorkingUnit unit in new List<WorkingUnit>(stack.Units))
				{
					if (moveList.Count >= maxRotationsPerTurn)
					{
						break;
					}
					if (unit == null || movedUnitIDs.Contains(unit.ID) || this.IsIronBaronyUnitAlreadyPendingMove(unit) || !this.ShouldRotateOutIronBaronyDamagedUnit(unit))
					{
						continue;
					}
					UnitMoveData moveData = this.FindIronBaronyDamagedUnitRetreatMove(unit, stack);
					if (moveData == null)
					{
						continue;
					}
					moveList.Add(moveData);
					movedUnitIDs.Add(unit.ID);
					this.RegisterPendingUnitMove(stack.Node.ID, moveData.TargetNode.ID, unit);
					this.AI.Log("  Iron Barony rotation: moving heavily damaged " + unit.DisplayName + " out of army at node " + stack.Node.ID + " to node " + moveData.TargetNode.ID + " (health " + (int)unit.Health + "%)");
				}
			}
			if (moveList.Count > 0)
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = "IronBaronyRotateDamagedUnits";
				moveAction.MoveTargets = moveList;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.AI.Log("  Iron Barony rotation: moving " + moveList.Count + " heavily damaged unit(s) out for recovery");
			}
		}

		private bool ShouldRotateOutIronBaronyDamagedUnit(WorkingUnit Unit)
		{
			if (!this.IsIronBaronyUnitRecovering(Unit) || Unit.OwnerStack == null)
			{
				return false;
			}
			if (Unit.Health >= 30f)
			{
				return false;
			}
			if (Unit.MovePoints <= 0f || Unit.Class == UnitClasses.Fort)
			{
				return false;
			}
			return true;
		}

		private bool IsIronBaronyUnitRecovering(WorkingUnit Unit)
		{
			return this.IsIronBarony() && Unit != null && Unit.OwnerRealm == this.AI.Realm && !Unit.Disabled && Unit.Health > 0f && Unit.Health < 90f;
		}

		private UnitMoveData FindIronBaronyDamagedUnitRetreatMove(WorkingUnit Unit, WorkingStack SourceStack)
		{
			if (Unit == null || SourceStack == null || SourceStack.Node == null)
			{
				return null;
			}
			UnitMoveData bestMove = null;
			float bestCost = float.MaxValue;
			WorkingProvince capitol = this.AI.Realm.CapitolProvince;
			if (capitol != null && capitol.LandNode != null)
			{
				UnitMoveData capitalMove = this.TryCreateIronBaronyDamagedUnitRetreatMove(Unit, SourceStack.Node, capitol.LandNode);
				if (capitalMove != null)
				{
					return capitalMove;
				}
			}
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack == null || stack == SourceStack || stack.Node == null || stack.Node.Province == null || stack.Node.Province.Occupied)
				{
					continue;
				}
				if (!stack.Node.Province.IsCapitol && this.IsIronBaronyArmyReadyForWar(stack.Node.ID))
				{
					continue;
				}
				if (!stack.Node.Province.IsCapitol && this.IronBaronyStackIsNearEnemy(stack))
				{
					continue;
				}
				UnitMoveData stackMove = this.TryCreateIronBaronyDamagedUnitRetreatMove(Unit, SourceStack.Node, stack.Node);
				if (stackMove == null)
				{
					continue;
				}
				if (stackMove.MovePath.TotalMoveCost < bestCost)
				{
					bestMove = stackMove;
					bestCost = stackMove.MovePath.TotalMoveCost;
				}
			}
			if (bestMove != null)
			{
				return bestMove;
			}
			if (capitol != null && capitol.LandNode != null)
			{
				SovereigntyTK.Game.Path stepPath;
				ActivePathNode stepNode = this.GetIronBaronyUnitStepToward(Unit, SourceStack.Node, capitol.LandNode, out stepPath);
				if (stepNode != null && stepPath != null)
				{
					return new UnitMoveData(Unit, stepNode, stepPath);
				}
			}
			return null;
		}

		private UnitMoveData TryCreateIronBaronyDamagedUnitRetreatMove(WorkingUnit Unit, ActivePathNode SourceNode, ActivePathNode TargetNode)
		{
			if (Unit == null || SourceNode == null || TargetNode == null || SourceNode == TargetNode)
			{
				return null;
			}
			if (!this.IsFriendlyMovementNode(TargetNode) || !this.CanSendToNode(TargetNode.ID))
			{
				return null;
			}
			WorkingStack targetStack = TargetNode.GetRealmStack(this.AI.Realm);
			if (targetStack != null && targetStack.Node != null && targetStack.Node.Province != null && !targetStack.Node.Province.IsCapitol && this.IsIronBaronyArmyReadyForWar(targetStack.Node.ID))
			{
				return null;
			}
			if (Unit.Class == UnitClasses.Naval && !Unit.Transport && TargetNode.NodeType == PathNodeTypes.Land)
			{
				return null;
			}
			List<WorkingUnit> unitList = new List<WorkingUnit>();
			unitList.Add(Unit);
			SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(SourceNode, TargetNode, unitList, true, this.AI.Realm, false);
			if (path.PathPoints.Count == 0 || Unit.MovePoints < path.TotalMoveCost)
			{
				return null;
			}
			return new UnitMoveData(Unit, TargetNode, path);
		}

		private ActivePathNode GetIronBaronyUnitStepToward(WorkingUnit Unit, ActivePathNode SourceNode, ActivePathNode TargetNode, out SovereigntyTK.Game.Path StepPath)
		{
			StepPath = null;
			if (Unit == null || SourceNode == null || TargetNode == null || Unit.MovePoints <= 0f)
			{
				return null;
			}
			List<WorkingUnit> unitList = new List<WorkingUnit>();
			unitList.Add(Unit);
			SovereigntyTK.Game.Path fullPath = this.AI.Game.PathManager.GetPath(SourceNode, TargetNode, unitList, false, this.AI.Realm, false);
			if (fullPath.PathPoints.Count <= 1)
			{
				return null;
			}
			float moveCost = 0f;
			ActivePathNode bestNode = null;
			for (int i = 1; i < fullPath.PathPoints.Count; i++)
			{
				PathPoint point = fullPath.PathPoints[i];
				if (!this.IsFriendlyMovementNode(point.Node))
				{
					break;
				}
				moveCost += point.MoveCost;
				if (moveCost > Unit.MovePoints)
				{
					break;
				}
				if (!this.CanSendToNode(point.Node.ID))
				{
					continue;
				}
				WorkingStack stackAtStep = point.Node.GetRealmStack(this.AI.Realm);
				if (stackAtStep == null || stackAtStep.Node == null || stackAtStep.Node.Province == null || stackAtStep.Node.Province.IsCapitol || !this.IsIronBaronyArmyReadyForWar(stackAtStep.Node.ID))
				{
					bestNode = point.Node;
				}
			}
			if (bestNode == null)
			{
				return null;
			}
			StepPath = this.AI.Game.PathManager.GetPath(SourceNode, bestNode, unitList, true, this.AI.Realm, false);
			if (StepPath.PathPoints.Count == 0 || StepPath.TotalMoveCost > Unit.MovePoints)
			{
				return null;
			}
			return bestNode;
		}

		private bool IsEligibleIronBaronyArmyStack(WorkingStack Stack)
		{
			return Stack != null && Stack.Units != null && Stack.Units.Count > 0 && Stack.Node != null && Stack.Node.Province != null && !Stack.Node.Province.Occupied;
		}

		private int GetIronBaronyArmySlotSortKey(WorkingStack Stack)
		{
			if (Stack == null || Stack.Node == null || Stack.Node.Province == null)
			{
				return 99;
			}
			bool isCapital = Stack.Node.Province.IsCapitol;
			bool nearEnemy = this.IronBaronyStackIsNearEnemy(Stack);
			int fieldCount = this.GetPendingIronBaronyFieldUnitCount(Stack.Node.ID);
			if (!isCapital && fieldCount >= 20 && Stack.Hero != null)
			{
				return 0;
			}
			if (!isCapital && fieldCount >= 20)
			{
				return 1;
			}
			if (!isCapital && Stack.Hero != null && fieldCount >= 12)
			{
				return 2;
			}
			if (!isCapital && nearEnemy)
			{
				return 3;
			}
			if (!isCapital)
			{
				return 4;
			}
			return 5;
		}

		private string GetIronBaronyArmySlotLabel(WorkingStack Stack)
		{
			string label = "";
			if (Stack.Hero != null)
			{
				label += ", hero";
			}
			if (Stack.Node != null && Stack.Node.Province != null)
			{
				if (Stack.Node.Province.IsCapitol)
				{
					label += ", capital/depot";
				}
				else if (this.IronBaronyStackIsNearEnemy(Stack))
				{
					label += ", front";
				}
			}
			if (Stack.Node != null)
			{
				label += ", " + this.GetIronBaronyCompositionSummary(Stack.Node.ID);
				if (this.IsIronBaronyArmyReadyForWar(Stack.Node.ID))
				{
					label += ", ready";
				}
			}
			return label;
		}

		private bool ShouldProtectIronBaronyArmySlotSource(WorkingStack Source, WorkingStack Target)
		{
			if (!this.IsIronBarony() || Source == null || Source == Target || Source.Node == null || Source.Node.Province == null)
			{
				return false;
			}
			if (Source.Node.Province.IsCapitol)
			{
				return false;
			}
			int sourcePending = this.GetPendingNodeUnitCount(Source.Node.ID);
			if (sourcePending >= 20)
			{
				return true;
			}
			if (Target != null && Target.Node != null && sourcePending >= this.GetWarGoalStackMinimum() && this.GetPendingNodeUnitCount(Target.Node.ID) >= this.GetWarGoalStackMinimum())
			{
				return true;
			}
			return false;
		}

		private bool IronBaronyStackIsNearEnemy(WorkingStack Stack)
		{
			if (Stack == null || Stack.Node == null || Stack.Node.Province == null)
			{
				return false;
			}
			foreach (GameRegion region in Stack.Node.Province.GetAllConnectedRegions())
			{
				WorkingProvince province = region as WorkingProvince;
				if (province != null && province.OwnerRealm != null && province.OwnerRealm != this.AI.Realm && this.AI.Realm.Enemies.Contains(province.OwnerRealm))
				{
					return true;
				}
			}
			return false;
		}

		private UnitMoveData FindBestIronBaronyFormationMove(WorkingStack Target, List<WorkingStack> ArmyStacks, HashSet<int> MovedUnitIDs)
		{
			UnitMoveData bestMove = null;
			int bestScore = int.MaxValue;
			foreach (WorkingStack source in ArmyStacks)
			{
				if (source == Target || source.Node == null || source.Node.Province == null)
				{
					continue;
				}
				if (!source.Node.Province.IsCapitol && this.GetPendingNodeUnitCount(source.Node.ID) >= 20)
				{
					continue;
				}
				if (this.ShouldProtectIronBaronyArmySlotSource(source, Target))
				{
					continue;
				}
				foreach (WorkingUnit unit in source.Units)
				{
					if (MovedUnitIDs != null && MovedUnitIDs.Contains(unit.ID))
					{
						continue;
					}
					if (!this.CanUseIronBaronyUnitForFormation(unit, Target))
					{
						continue;
					}
					List<WorkingUnit> unitList = new List<WorkingUnit>();
					unitList.Add(unit);
					SovereigntyTK.Game.Path fullPath = this.AI.Game.PathManager.GetPath(source.Node, Target.Node, unitList, false, this.AI.Realm, false);
					if (fullPath.PathPoints.Count == 0)
					{
						continue;
					}
					bool directMove = false;
					UnitMoveData candidateMove = this.TryCreateIronBaronyFormationMove(unit, source.Node, Target.Node, out directMove);
					if (candidateMove == null)
					{
						continue;
					}
					int score = this.GetIronBaronyFormationUnitPriority(unit, Target.Node) * 1000 + (directMove ? 0 : 500) + (int)(fullPath.TotalMoveCost * 10f) + ((source.Node != null) ? source.Node.ID : 0);
					if (bestMove == null || score < bestScore)
					{
						bestScore = score;
						bestMove = candidateMove;
					}
				}
			}
			return bestMove;
		}


		private List<UnitMoveData> FindBestIronBaronyStackMarchTowardArmy(WorkingStack Target, List<WorkingStack> ArmyStacks, HashSet<int> MovedUnitIDs)
		{
			List<UnitMoveData> bestMoves = null;
			int bestScore = int.MaxValue;
			foreach (WorkingStack source in ArmyStacks)
			{
				if (source == null || source == Target || source.Node == null || source.Node.Province == null || Target == null || Target.Node == null)
				{
					continue;
				}
				if (!source.Node.Province.IsCapitol && this.GetPendingNodeUnitCount(source.Node.ID) >= 20)
				{
					continue;
				}
				if (this.ShouldProtectIronBaronyArmySlotSource(source, Target))
				{
					continue;
				}
				if (!source.Node.Province.IsCapitol && this.IronBaronyStackIsNearEnemy(source))
				{
					continue;
				}
				List<WorkingUnit> marchingUnits = this.GetIronBaronyStackMarchUnits(source, Target, MovedUnitIDs);
				if (marchingUnits.Count == 0)
				{
					continue;
				}
				SovereigntyTK.Game.Path marchPath;
				ActivePathNode destination = this.GetIronBaronyStackMarchDestination(source, Target, marchingUnits, out marchPath);
				if (destination == null || marchPath == null || destination == source.Node || destination == Target.Node)
				{
					continue;
				}
				if (!this.CanSendToNode(destination.ID))
				{
					continue;
				}
				if (this.GetPendingNodeUnitCount(destination.ID) + marchingUnits.Count > 20 && destination.GetRealmStack(this.AI.Realm) != source)
				{
					continue;
				}
				List<UnitMoveData> moves = new List<UnitMoveData>();
				bool failed = false;
				foreach (WorkingUnit unit in marchingUnits)
				{
					List<WorkingUnit> singleUnitList = new List<WorkingUnit>();
					singleUnitList.Add(unit);
					SovereigntyTK.Game.Path unitPath = this.AI.Game.PathManager.GetPath(source.Node, destination, singleUnitList, true, this.AI.Realm, false);
					if (unitPath.PathPoints.Count == 0 || unit.MovePoints < unitPath.TotalMoveCost)
					{
						failed = true;
						break;
					}
					moves.Add(new UnitMoveData(unit, destination, unitPath));
				}
				if (failed || moves.Count == 0)
				{
					continue;
				}
				int score = this.GetIronBaronyStackMarchScore(source, Target, marchingUnits, marchPath);
				if (bestMoves == null || score < bestScore)
				{
					bestMoves = moves;
					bestScore = score;
				}
			}
			return bestMoves;
		}

		private List<WorkingUnit> GetIronBaronyStackMarchUnits(WorkingStack Source, WorkingStack Target, HashSet<int> MovedUnitIDs)
		{
			List<WorkingUnit> units = new List<WorkingUnit>();
			if (Source == null || Target == null || Target.Node == null)
			{
				return units;
			}
			foreach (WorkingUnit unit in Source.Units)
			{
				if (MovedUnitIDs != null && MovedUnitIDs.Contains(unit.ID))
				{
					continue;
				}
				if (unit == null || unit.MovePoints <= 0f || unit.Class == UnitClasses.Fort)
				{
					continue;
				}
				if (this.IsIronBaronyUnitRecovering(unit))
				{
					continue;
				}
				if (this.IsIronBaronyCatapult(unit) && !this.IronBaronyHasFortifiedWarGoalOrFrontier())
				{
					continue;
				}
				if (this.GetIronBaronyCompositionUnitName(unit) == null)
				{
					continue;
				}
				if (unit.Class == UnitClasses.Naval && !unit.Transport && Target.Node.NodeType == PathNodeTypes.Land)
				{
					continue;
				}
				units.Add(unit);
			}
			return units;
		}

		private ActivePathNode GetIronBaronyStackMarchDestination(WorkingStack Source, WorkingStack Target, List<WorkingUnit> Units, out SovereigntyTK.Game.Path MarchPath)
		{
			MarchPath = null;
			if (Source == null || Target == null || Source.Node == null || Target.Node == null || Units == null || Units.Count == 0)
			{
				return null;
			}
			SovereigntyTK.Game.Path fullPath = this.AI.Game.PathManager.GetPath(Source.Node, Target.Node, Units, false, this.AI.Realm, false);
			if (fullPath.PathPoints.Count <= 1)
			{
				return null;
			}
			float moveBudget = float.MaxValue;
			foreach (WorkingUnit unit in Units)
			{
				if (unit.MovePoints < moveBudget)
				{
					moveBudget = unit.MovePoints;
				}
			}
			float moveCost = 0f;
			ActivePathNode bestNode = null;
			for (int i = 1; i < fullPath.PathPoints.Count; i++)
			{
				PathPoint point = fullPath.PathPoints[i];
				if (point == null || point.Node == null)
				{
					break;
				}
				if (point.Node == Target.Node)
				{
					break;
				}
				if (!this.IsFriendlyMovementNode(point.Node))
				{
					break;
				}
				moveCost += point.MoveCost;
				if (moveCost > moveBudget)
				{
					break;
				}
				if (!this.CanSendToNode(point.Node.ID))
				{
					continue;
				}
				WorkingStack stackAtStep = point.Node.GetRealmStack(this.AI.Realm);
				if (stackAtStep == null || stackAtStep == Source || stackAtStep.Node == null || stackAtStep.Node.Province == null || stackAtStep.Node.Province.IsCapitol || !this.IsIronBaronyArmyReadyForWar(stackAtStep.Node.ID))
				{
					bestNode = point.Node;
				}
			}
			if (bestNode == null || bestNode == Source.Node)
			{
				return null;
			}
			MarchPath = this.AI.Game.PathManager.GetPath(Source.Node, bestNode, Units, true, this.AI.Realm, false);
			if (MarchPath.PathPoints.Count == 0 || MarchPath.TotalMoveCost > moveBudget)
			{
				return null;
			}
			return bestNode;
		}

		private int GetIronBaronyStackMarchScore(WorkingStack Source, WorkingStack Target, List<WorkingUnit> Units, SovereigntyTK.Game.Path Path)
		{
			int score = (Path == null) ? 10000 : (int)(Path.TotalMoveCost * 10f);
			score -= Units.Count * 25;
			foreach (WorkingUnit unit in Units)
			{
				string baseName = (unit.BaseType != null) ? unit.BaseType.Name : unit.DisplayName;
				if (baseName == "Ogre" || baseName == "Gravedigger" || baseName == "Witchdoctor" || baseName == "War Captain Urkai")
				{
					score -= 80;
				}
			}
			if (Source != null && Source.Node != null && Source.Node.Province != null && Source.Node.Province.IsCapitol)
			{
				score -= 20;
			}
			if (Source != null && Source.Node != null)
			{
				score += Source.Node.ID;
			}
			return score;
		}

		private UnitMoveData TryCreateIronBaronyFormationMove(WorkingUnit Unit, ActivePathNode SourceNode, ActivePathNode TargetNode, out bool DirectMove)
		{
			DirectMove = false;
			if (Unit == null || SourceNode == null || TargetNode == null || SourceNode == TargetNode)
			{
				return null;
			}
			List<WorkingUnit> unitList = new List<WorkingUnit>();
			unitList.Add(Unit);
			SovereigntyTK.Game.Path directPath = this.AI.Game.PathManager.GetPath(SourceNode, TargetNode, unitList, true, this.AI.Realm, false);
			if (directPath.PathPoints.Count > 0 && Unit.MovePoints >= directPath.TotalMoveCost && this.CanSendToNode(TargetNode.ID) && this.CanSendIronBaronyUnitToNode(Unit, TargetNode, "ConsolidateIronBaronyArmies"))
			{
				DirectMove = true;
				return new UnitMoveData(Unit, TargetNode, directPath);
			}
			return null;
		}

		private bool CanUseIronBaronyUnitForFormation(WorkingUnit Unit, WorkingStack Target)
		{
			if (Unit == null || Unit.OwnerStack == null || Unit.MovePoints <= 0f || Unit.Class == UnitClasses.Fort)
			{
				return false;
			}
			if (this.IsIronBaronyUnitRecovering(Unit))
			{
				return false;
			}
			if (this.IsIronBaronyCatapult(Unit) && !this.IronBaronyHasFortifiedWarGoalOrFrontier())
			{
				return false;
			}
			string compositionName = this.GetIronBaronyCompositionUnitName(Unit);
			if (compositionName == null)
			{
				return false;
			}
			if (Unit.Class == UnitClasses.Naval && !Unit.Transport && Target.Node.NodeType == PathNodeTypes.Land)
			{
				return false;
			}
			if (!this.CanSendToNode(Target.Node.ID))
			{
				return false;
			}
			if (!this.CanSendIronBaronyUnitToNode(Unit, Target.Node, "ConsolidateIronBaronyArmies"))
			{
				return false;
			}
			return true;
		}


		private int GetIronBaronyFormationUnitPriority(WorkingUnit Unit, ActivePathNode TargetNode)
		{
			string baseName = (Unit.BaseType != null) ? Unit.BaseType.Name : Unit.DisplayName;
			string compositionName = this.GetIronBaronyCompositionUnitName(Unit);
			if (baseName == "War Captain Urkai" || compositionName == "War Captain Urkai")
			{
				return 1;
			}
			if (compositionName == "Orcish Raiders")
			{
				return 10;
			}
			if (compositionName == "Orcish Archers")
			{
				return 20;
			}
			if (compositionName == "Wolfriders")
			{
				return 25;
			}
			if (compositionName == "Ogre")
			{
				return 30;
			}
			if (compositionName == "Witchdoctor")
			{
				return 35;
			}
			if (compositionName == "Gravedigger")
			{
				return 40;
			}
			return 100;
		}


		private int GetPendingNodeUnitCount(int NodeID)
		{
			int count = 0;
			if (this.PendingUnitsByNode != null && this.PendingUnitsByNode.TryGetValue(NodeID, out count))
			{
				return count;
			}
			return this.GetActualNodeUnitCount(NodeID);
		}

		private WorkingStack FindNearestLargerStack(WorkingStack Source, List<WorkingStack> ExcludeList)
		{
			WorkingStack best = null;
			int bestSize = Source.Units.Count;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack == Source || stack.Units.Count >= 20 || stack.Node.Province == null || !this.ProvinceCanServeAsArmyBase(stack.Node.Province))
				{
					continue;
				}
				if (ExcludeList != null && ExcludeList.Contains(stack))
				{
					continue;
				}
				if (stack.Units.Count > bestSize && this.IronBaronyStackHasRoomForAnyUnit(Source, stack.Node))
				{
					List<WorkingUnit> unitList = new List<WorkingUnit>();
					unitList.Add(Source.Units[0]);
					SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(Source.Node, stack.Node, unitList, false, this.AI.Realm, false);
					if (path.PathPoints.Count > 0 && path.PathPoints.Count <= 4)
					{
						best = stack;
						bestSize = stack.Units.Count;
					}
				}
			}
			return best;
		}

		private int GetNearbyEnemyReinforcements(WorkingProvince Province)
		{
			int strength = 0;
			foreach (GameRegion region in Province.GetAllConnectedRegions())
			{
				WorkingProvince neighbor = region as WorkingProvince;
				if (neighbor != null && neighbor.OwnerRealm == Province.OwnerRealm && neighbor.LandNode.CurrentStack != null)
				{
					strength += this.GetStackStrength(neighbor.LandNode.CurrentStack);
				}
			}
			return strength;
		}

		private int GetStackStrength(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return 0;
			}
			int strength = 0;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled)
				{
					strength += Math.Max(unit.Attack, unit.RangedAttack) + unit.Defence + 1;
				}
			}
			return strength;
		}

		private int GetEnemyDistanceFromCapital()
		{
			return this.GetEnemyDistanceFromCapital(CapitalThreatDistance);
		}

		private int GetEnemyDistanceFromCapital(int MaxDistance)
		{
			WorkingProvince capitol = this.AI.Realm.CapitolProvince;
			if (capitol == null || MaxDistance <= 0)
			{
				return 0;
			}
			HashSet<GameRegion> checkedRegions = new HashSet<GameRegion>();
			List<GameRegion> currentRing = new List<GameRegion>();
			checkedRegions.Add(capitol);
			currentRing.Add(capitol);
			for (int distance = 1; distance <= MaxDistance; distance++)
			{
				List<GameRegion> nextRing = new List<GameRegion>();
				foreach (GameRegion region in currentRing)
				{
					foreach (GameRegion neighbor in region.GetAllConnectedRegions())
					{
						if (checkedRegions.Contains(neighbor) || !(neighbor is WorkingProvince))
						{
							continue;
						}
						checkedRegions.Add(neighbor);
						nextRing.Add(neighbor);
						WorkingProvince prov = neighbor as WorkingProvince;
						if (prov.LandNode != null && prov.LandNode.CurrentStack != null && prov.LandNode.CurrentStack.Owner != this.AI.Realm && this.AI.Realm.Enemies.Contains(prov.LandNode.CurrentStack.Owner))
						{
							return distance;
						}
					}
				}
				if (nextRing.Count == 0)
				{
					break;
				}
				currentRing = nextRing;
			}
			return 0;
		}

		private WorkingStack GetNearbyEnemyForCapitalStrike()
		{
			WorkingProvince capitol = this.AI.Realm.CapitolProvince;
			if (capitol == null || capitol.LandNode.CurrentStack == null)
			{
				return null;
			}
			WorkingStack capitalArmy = capitol.LandNode.CurrentStack;
			List<WorkingStack> nearbyEnemies = new List<WorkingStack>();
			HashSet<GameRegion> visited = new HashSet<GameRegion>();
			List<GameRegion> currentRing = new List<GameRegion>();
			List<GameRegion> nextRing = new List<GameRegion>();
			visited.Add(capitol);
			currentRing.Add(capitol);
			for (int ring = 0; ring < 5; ring++)
			{
				nextRing.Clear();
				foreach (GameRegion region in currentRing)
				{
					foreach (GameRegion neighbor in region.GetAllConnectedRegions())
					{
						if (visited.Contains(neighbor) || !(neighbor is WorkingProvince))
						{
							continue;
						}
						visited.Add(neighbor);
						nextRing.Add(neighbor);
						WorkingProvince prov = neighbor as WorkingProvince;
						if (prov.LandNode.CurrentStack != null && prov.LandNode.CurrentStack.Owner != this.AI.Realm && this.AI.Realm.Enemies.Contains(prov.LandNode.CurrentStack.Owner))
						{
							nearbyEnemies.Add(prov.LandNode.CurrentStack);
						}
					}
				}
				currentRing = new List<GameRegion>(nextRing);
			}
			if (nearbyEnemies.Count != 1)
			{
				return null;
			}
			WorkingStack enemy = nearbyEnemies[0];
			int ourStrength = 0;
			foreach (WorkingUnit unit in capitalArmy.Units)
			{
				if (!unit.Disabled && unit.Class != UnitClasses.Fort)
				{
					ourStrength += Math.Max(unit.Attack, unit.RangedAttack) + unit.Defence;
				}
			}
			int enemyStrength = 0;
			foreach (WorkingUnit unit2 in enemy.Units)
			{
				if (!unit2.Disabled)
				{
					enemyStrength += Math.Max(unit2.Attack, unit2.RangedAttack) + unit2.Defence;
				}
			}
			if (ourStrength > enemyStrength * 1.3f)
			{
				return enemy;
			}
			return null;
		}

		private float GetStackHealthPercent(WorkingStack Stack)
		{
			if (Stack == null || Stack.Units.Count == 0)
			{
				return 0f;
			}
			int totalHealth = 0;
			int totalMaxHealth = 0;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled)
				{
					totalHealth += (int)unit.Health;
					totalMaxHealth += 100;
				}
			}
			if (totalMaxHealth == 0)
			{
				return 0f;
			}
			return (float)totalHealth / (float)totalMaxHealth;
		}

		private float GetStackMoralePercent(WorkingStack Stack)
		{
			if (Stack == null || Stack.Units.Count == 0)
			{
				return 1f;
			}
			int totalMorale = 0;
			int count = 0;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled && unit.Race != Races.Undead)
				{
					totalMorale += (int)unit.Morale;
					count++;
				}
			}
			if (count == 0)
			{
				return 1f;
			}
			return (float)totalMorale / (float)(count * 100);
		}

		private bool IsIronBaronyCatapult(WorkingUnit Unit)
		{
			if (!this.IsIronBarony() || Unit == null || Unit.Class != UnitClasses.Siege)
			{
				return false;
			}
			if (Unit.BaseType != null && Unit.BaseType.Name == "Orcish Catapult")
			{
				return true;
			}
			return Unit.DisplayName == "Orcish Catapult" || (Unit.BaseName != null && Unit.BaseName.EndsWith(".Orcish Catapult"));
		}

		private bool ProvinceHasActiveFortifications(WorkingProvince Province)
		{
			if (Province == null || Province.FortLevel <= 0 || Province.Forts == null)
			{
				return false;
			}
			int fortCount = Math.Min(Province.FortLevel, Province.Forts.Count);
			for (int i = 0; i < fortCount; i++)
			{
				WorkingUnit fort = Province.Forts[i];
				if (fort != null && !fort.Disabled && (int)fort.Health > 0)
				{
					return true;
				}
			}
			return false;
		}

		private bool IronBaronyHasFortifiedWarGoalOrFrontier()
		{
			if (!this.IsIronBarony())
			{
				return false;
			}
			// First check explicit invasion capture goals.
			foreach (KeyValuePair<int, InvasionTargetData> invasion in this.AI.WarManager.InvasionTargets)
			{
				WarGoalData goal = invasion.Value.WarGoal;
				if (goal == null || goal.GoalType != WarGoalTypes.CaptureProvinces)
				{
					continue;
				}
				foreach (int provinceID in goal.ProvinceTargets)
				{
					WorkingProvince prov;
					if (this.AI.Game.AllProvinces.TryGetValue(provinceID, out prov) && prov != null && this.ProvinceIsHostile(prov) && this.ProvinceHasActiveFortifications(prov))
					{
						return true;
					}
				}
			}
			// Then check the current border/frontier. This catches generated war goals before they are stored as explicit invasion targets.
			foreach (WorkingProvince prov in this.AI.Game.AllProvinces.Values)
			{
				if (prov == null || prov.LandNode == null || !this.ProvinceIsHostile(prov) || !this.ProvinceHasActiveFortifications(prov))
				{
					continue;
				}
				foreach (ActiveNodeConnection connection in prov.LandNode.ConnectedNodes)
				{
					ActivePathNode neighbor = connection.TargetNode;
					if (neighbor != null && neighbor.Province != null && this.GetProvinceController(neighbor.Province) == this.AI.Realm)
					{
						return true;
					}
				}
			}
			return false;
		}

		private int GetIronBaronyDesiredCatapultCount()
		{
			// Do not buy idle catapults for normal field armies. Catapults are expensive,
			// slow the army down, and composition logic keeps them out of normal stacks.
			// When a fortified war goal/frontier exists, raise the target to 4 so Iron Barony
			// can form a proper 16 field + 4 catapult siege stack.
			return this.IronBaronyHasFortifiedWarGoalOrFrontier() ? 4 : 0;
		}

		private bool ShouldMoveIronBaronyCatapultToTarget(WorkingUnit Unit, WorkingProvince TargetProvince)
		{
			if (!this.IsIronBaronyCatapult(Unit))
			{
				return true;
			}
			return this.ProvinceHasActiveFortifications(TargetProvince);
		}

		
			private bool AddIronBaronyHeroMoveWithArmy(WorkingStack SourceStack, ActivePathNode TargetNode, List<UnitMoveData> MoveList, string DebugSource)
			{
				if (!this.IsIronBarony() || SourceStack == null || SourceStack.Disposed || SourceStack.Hero == null || SourceStack.Node == null || TargetNode == null || MoveList == null || MoveList.Count == 0)
				{
					return false;
				}
				WorkingHero hero = SourceStack.Hero;
				WorkingStack targetStack = TargetNode.GetRealmStack(this.AI.Realm);
				if (targetStack != null && !targetStack.Disposed && targetStack.Hero != null && targetStack.Hero != hero)
				{
					this.LogWarGoals("  Hero " + hero.DisplayName + " stays with source during " + DebugSource + ": target node " + TargetNode.ID + " already has hero " + targetStack.Hero.DisplayName);
					return false;
				}
				List<WorkingUnit> heroList = new List<WorkingUnit>();
				heroList.Add(hero);
				SovereigntyTK.Game.Path heroPath = this.AI.Game.PathManager.GetPath(SourceStack.Node, TargetNode, heroList, true, this.AI.Realm, false);
				if (heroPath.PathPoints.Count == 0 && MoveList[0].MovePath != null && MoveList[0].MovePath.PathPoints.Count > 0)
				{
					this.LogWarGoals("  Hero " + hero.DisplayName + " has no separate hero path during " + DebugSource + "; using field-army path fallback");
					heroPath = MoveList[0].MovePath;
				}
				if (heroPath.PathPoints.Count == 0)
				{
					this.LogWarGoals("  Hero " + hero.DisplayName + " stays with source during " + DebugSource + ": no path to target node " + TargetNode.ID);
					return false;
				}
				MoveList.Add(new UnitMoveData(hero, TargetNode, heroPath));
				this.LogWarGoals("  Hero " + hero.DisplayName + " will follow field army to node " + TargetNode.ID + " during " + DebugSource);
				return true;
			}

			private void QueueIronBaronyCatapultSupport(WorkingProvince HostileTarget, ActivePathNode StagingNode, WorkingStack ExcludeStack, string DebugSource)
		{
			if (!this.IsIronBarony() || !this.ProvinceHasActiveFortifications(HostileTarget) || StagingNode == null)
			{
				return;
			}
			List<UnitMoveData> catapultMoves = new List<UnitMoveData>();
			foreach (WorkingUnit unit in this.AI.Realm.Units)
			{
				if (catapultMoves.Count >= 2)
				{
					break;
				}
				if (!this.IsIronBaronyCatapult(unit) || unit.OwnerStack == null || unit.OwnerStack == ExcludeStack)
				{
					continue;
				}
				if (unit.OwnerStack.Node == null || unit.MovePoints <= 0f)
				{
					continue;
				}
				if (unit.OwnerStack.Node.Province != null && unit.OwnerStack.Node.Province.Occupied)
				{
					continue;
				}
				if (!this.CanSendToNode(StagingNode.ID))
				{
					break;
				}
				if (this.AI.Game.DestinationChecker.NodeOKForUnit(unit, StagingNode) != UnitMoveResult.OK)
				{
					continue;
				}
				List<WorkingUnit> unitList = new List<WorkingUnit>();
				unitList.Add(unit);
				SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(unit.OwnerStack.Node, StagingNode, unitList, true, this.AI.Realm, false);
				if (path.PathPoints.Count > 0 && unit.MovePoints >= path.TotalMoveCost)
				{
					catapultMoves.Add(new UnitMoveData(unit, StagingNode, path));
					this.RegisterPendingUnit(StagingNode.ID, unit);
				}
			}
			if (catapultMoves.Count > 0)
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = DebugSource;
				moveAction.MoveTargets = catapultMoves;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.LogWarGoals("  Siege support: moving " + catapultMoves.Count + " Orcish Catapult(s) toward staging for fortified target " + HostileTarget.Name);
			}
		}

		private bool IsIronBaronyBesiegingStack(WorkingStack Stack)
		{
			if (!this.IsIronBarony() || Stack == null || Stack.Disposed || Stack.Node == null || Stack.Node.Province == null || Stack.Units.Count == 0)
			{
				return false;
			}
			WorkingProvince province = Stack.Node.Province;
			if (province.OwnerRealm == this.AI.Realm)
			{
				return false;
			}
			if (province.OccupierRealm != this.AI.Realm)
			{
				return false;
			}
			if (Stack.Units.Count >= 20)
			{
				return false;
			}
			if (Stack.Hero != null)
			{
				return true;
			}
			return this.GetIronBaronyActualFieldUnitCount(Stack) >= Math.Max(4, this.GetWarGoalStackMinimum() / 2);
		}

		private bool IsIronBaronySiegeReinforcementDonor(WorkingStack Donor, WorkingStack SiegeStack)
		{
			if (!this.IsIronBarony() || Donor == null || Donor.Disposed || Donor == SiegeStack || Donor.Node == null || Donor.Node.Province == null || Donor.Units.Count == 0)
			{
				return false;
			}
			if (Donor.Node.Province.Occupied || Donor.Node.Province.IsCapitol)
			{
				return false;
			}
			if (this.GetStackHealthPercent(Donor) < 0.5f || this.GetStackMoralePercent(Donor) < 0.5f)
			{
				return false;
			}
			// Keep a complete second army intact. Small/rear stacks are the reinforcement pool; ready armies should march as armies.
			if (this.GetIronBaronyActualFieldUnitCount(Donor) >= this.GetWarGoalStackMinimum() && Donor.Units.Count >= this.GetWarGoalStackMinimum())
			{
				return false;
			}
			return true;
		}

		private int GetIronBaronyStrategicPathLength(WorkingStack Stack, ActivePathNode TargetNode)
		{
			if (Stack == null || Stack.Node == null || TargetNode == null || Stack.Units.Count == 0)
			{
				return 9999;
			}
			WorkingUnit probeUnit = Stack.Units.FirstOrDefault((WorkingUnit x) => x != null && x.Class != UnitClasses.Fort && x.MovePoints > 0f && (x.Class != UnitClasses.Naval || x.Transport));
			if (probeUnit == null)
			{
				return 9999;
			}
			List<WorkingUnit> unitList = new List<WorkingUnit>();
			unitList.Add(probeUnit);
			SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(Stack.Node, TargetNode, unitList, false, this.AI.Realm, false);
			if (path == null || path.PathPoints.Count == 0)
			{
				return 9999;
			}
			return path.PathPoints.Count;
		}

		private bool IronBaronyStackCanReachNodeThisTurn(WorkingStack Stack, ActivePathNode TargetNode)
		{
			if (Stack == null || Stack.Node == null || TargetNode == null)
			{
				return false;
			}
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit == null || unit.Disabled || unit.Class == UnitClasses.Fort || unit.MovePoints <= 0f)
				{
					continue;
				}
				if (unit.Class == UnitClasses.Naval && !unit.Transport)
				{
					continue;
				}
				List<WorkingUnit> unitList = new List<WorkingUnit>();
				unitList.Add(unit);
				SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(Stack.Node, TargetNode, unitList, true, this.AI.Realm, false);
				if (path != null && path.PathPoints.Count > 0 && unit.MovePoints >= path.TotalMoveCost)
				{
					return true;
				}
			}
			return false;
		}

		private void ReinforceIronBaronyBesiegingArmies()
		{
			if (!this.IsIronBarony())
			{
				return;
			}
			List<WorkingStack> siegeStacks = new List<WorkingStack>();
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (this.IsIronBaronyBesiegingStack(stack))
				{
					siegeStacks.Add(stack);
				}
			}
			if (siegeStacks.Count == 0)
			{
				return;
			}
			this.LogWarGoals("  Iron Barony siege reinforcement phase: " + siegeStacks.Count + " underfilled besieging stack(s)");
			foreach (WorkingStack siegeStack in siegeStacks.OrderByDescending((WorkingStack x) => x.Hero != null).ThenBy((WorkingStack x) => x.Units.Count))
			{
				if (siegeStack.Node == null || siegeStack.Node.Province == null || !this.CanSendToNode(siegeStack.Node.ID))
				{
					continue;
				}
				WorkingProvince siegeProvince = siegeStack.Node.Province;
				int roomBefore = 20 - this.GetPendingNodeUnitCountForWarGoal(siegeStack.Node);
				this.LogWarGoals("  Besieging army at " + siegeProvince.Name + " has " + siegeStack.Units.Count + "/20 units, field " + this.GetIronBaronyActualFieldUnitCount(siegeStack) + "/18, room " + roomBefore);
				List<WorkingStack> donors = new List<WorkingStack>();
				foreach (WorkingStack donor in this.AI.Realm.Stacks)
				{
					if (this.IsIronBaronySiegeReinforcementDonor(donor, siegeStack))
					{
						donors.Add(donor);
					}
				}
				donors = donors.OrderBy((WorkingStack x) => this.GetIronBaronyStrategicPathLength(x, siegeStack.Node)).ThenByDescending((WorkingStack x) => x.Units.Count).ToList<WorkingStack>();
				foreach (WorkingStack donor in donors)
				{
					if (!this.CanSendToNode(siegeStack.Node.ID))
					{
						break;
					}
					ActivePathNode moveTarget = null;
					string moveTargetText = null;
					if (this.IronBaronyStackCanReachNodeThisTurn(donor, siegeStack.Node))
					{
						moveTarget = siegeStack.Node;
						moveTargetText = "besieging army at " + siegeProvince.Name;
					}
					else
					{
						SovereigntyTK.Game.Path stepPath;
						moveTarget = this.GetIronBaronyStackStepToward(donor, siegeStack.Node, siegeProvince, out stepPath);
						if (moveTarget != null)
						{
							moveTargetText = "step " + this.GetWarGoalNodeLabel(moveTarget) + " toward besieging army at " + siegeProvince.Name;
						}
					}
					if (moveTarget == null || !this.CanSendToNode(moveTarget.ID))
					{
						continue;
					}
					List<UnitMoveData> moves = new List<UnitMoveData>();
					foreach (WorkingUnit unit in donor.Units)
					{
						if (!this.CanSendToNode(moveTarget.ID))
						{
							break;
						}
						if (unit == null || unit.Disabled || unit.Class == UnitClasses.Fort || unit.MovePoints <= 0f)
						{
							continue;
						}
						if (unit.Class == UnitClasses.Naval && !unit.Transport)
						{
							continue;
						}
						if (!this.CanSendIronBaronyUnitToNode(unit, moveTarget, "ReinforceBesiegingArmies"))
						{
							continue;
						}
						if (!this.ShouldMoveIronBaronyCatapultToTarget(unit, siegeProvince))
						{
							this.LogWarGoals("  Siege reinforcement: holding Orcish Catapult back from " + siegeProvince.Name + ": target has no active fortifications");
							continue;
						}
						if (this.AI.Game.DestinationChecker.NodeOKForUnit(unit, moveTarget) != UnitMoveResult.OK)
						{
							continue;
						}
						List<WorkingUnit> unitList = new List<WorkingUnit>();
						unitList.Add(unit);
						SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(donor.Node, moveTarget, unitList, true, this.AI.Realm, false);
						if (path != null && path.PathPoints.Count > 0 && unit.MovePoints >= path.TotalMoveCost)
						{
							moves.Add(new UnitMoveData(unit, moveTarget, path));
							this.RegisterPendingUnitMove(donor.Node.ID, moveTarget.ID, unit);
						}
					}
					if (moves.Count > 0)
					{
						int moveCount = moves.Count;
						bool heroAdded = this.AddIronBaronyHeroMoveWithArmy(donor, moveTarget, moves, "ReinforceBesiegingArmies");
						AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
						moveAction.DebugSource = "ReinforceBesiegingArmies";
						moveAction.MoveTargets = moves;
						this.AI.ActionManager.AddAction(moveAction, true);
						this.LogWarGoals("  Siege reinforcement: moving " + moveCount + " unit(s)" + (heroAdded ? " + hero" : "") + " from " + donor.Node.Province.Name + " to " + moveTargetText);
					}
				}
			}
		}

		internal void BeginAttacks()
		{
			this.AI.Realm.StacksChanged();
			this.PendingUnitsByNode = new Dictionary<int, int>();
			this.PendingIronBaronyUnitTypesByNode = (this.IsIronBarony() || this.IsMaledor() || this.IsBoruvian()) ? new Dictionary<int, Dictionary<string, int>>() : null;
			this.PendingIronBaronyMovedUnitIDs = this.IsIronBarony() ? new HashSet<int>() : null;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack.Node != null)
				{
					if (!this.PendingUnitsByNode.ContainsKey(stack.Node.ID))
					{
						this.PendingUnitsByNode[stack.Node.ID] = 0;
					}
					this.PendingUnitsByNode[stack.Node.ID] += stack.Units.Count;
					foreach (WorkingUnit unit in stack.Units)
					{
						this.SeedPendingIronBaronyCompositionUnit(stack.Node.ID, unit);
					}
				}
			}
			this.LogStackDebug("=== BeginAttacks for " + this.AI.Realm.Name + " ===");
			this.LogStackDebug("Initial stack counts:");
			foreach (KeyValuePair<int, int> kvp in this.PendingUnitsByNode)
			{
				this.LogStackDebug("  Node " + kvp.Key + ": " + kvp.Value + " units");
			}
			bool capitalEmergencyAtStart = this.CapitalDefenseEmergencyActive();
			this.ConsolidateArmies();
			this.LogStackDebug("After ConsolidateArmies:");
			foreach (KeyValuePair<int, int> kvp in this.PendingUnitsByNode)
			{
				if (kvp.Value > 15)
				{
					this.LogStackDebug("  WARNING Node " + kvp.Key + ": " + kvp.Value + " units");
				}
			}
			this.ReinforceCapitol();
			this.LogStackDebug("After ReinforceCapitol:");
			foreach (KeyValuePair<int, int> kvp in this.PendingUnitsByNode)
			{
				if (kvp.Value > 15)
				{
					this.LogStackDebug("  WARNING Node " + kvp.Key + ": " + kvp.Value + " units");
				}
			}
			if (capitalEmergencyAtStart)
			{
				this.LogWarGoals("  Capital defense emergency active: skipping hero deployment and distant war goals; keeping reclaim and capital strike available");
				this.ReclaimOccupiedProvinces();
				this.CapitalStrike();
			}
			else
			{
				this.ReclaimOccupiedProvinces();
				this.DeployHeroToFront();
				this.CapitalStrike();
				this.ReinforceIronBaronyBesiegingArmies();
				this.PursueWarGoals();
			}
			this.LogStackDebug("After all phases - final counts:");
			foreach (KeyValuePair<int, int> kvp in this.PendingUnitsByNode)
			{
				if (kvp.Value > 15)
				{
					this.LogStackDebug("  WARNING Node " + kvp.Key + ": " + kvp.Value + " units");
				}
			}
			this.PendingUnitsByNode = null;
			this.PendingIronBaronyUnitTypesByNode = null;
			this.PendingIronBaronyMovedUnitIDs = null;
			this.RealmStackIDs = new List<int>();
			foreach (WorkingStack workingStack in this.AI.Realm.Stacks)
			{
				this.RealmStackIDs.Add(workingStack.ID);
			}
			this.CurrentStackID = 0;
			this.DoAttacks();
		}

		private void LogStackDebug(string text)
		{
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "ai_stack_debug.txt");
				string realm = (this.AI != null && this.AI.Realm != null) ? this.AI.Realm.Name : "Unknown Realm";
				System.IO.File.AppendAllText(file, System.DateTime.Now.ToString("HH:mm:ss.fff") + " [" + realm + "] " + text + "\r\n");
			}
			catch
			{
			}
		}

		private Dictionary<int, int> PendingUnitsByNode;

		private Dictionary<int, Dictionary<string, int>> PendingIronBaronyUnitTypesByNode;

		private HashSet<int> PendingIronBaronyMovedUnitIDs;

		private bool IsIronBaronyUnitAlreadyPendingMove(WorkingUnit Unit)
		{
			return Unit != null && this.PendingIronBaronyMovedUnitIDs != null && this.PendingIronBaronyMovedUnitIDs.Contains(Unit.ID);
		}

		private void RegisterPendingUnitMove(int SourceNodeID, int TargetNodeID, WorkingUnit Unit)
		{
			if (Unit != null && this.PendingIronBaronyMovedUnitIDs != null)
			{
				this.PendingIronBaronyMovedUnitIDs.Add(Unit.ID);
			}
			this.DeregisterPendingUnit(SourceNodeID, Unit);
			this.RegisterPendingUnit(TargetNodeID, Unit);
		}

		private void DeregisterPendingUnit(int NodeID, WorkingUnit Unit)
		{
			if (this.PendingUnitsByNode != null && this.PendingUnitsByNode.ContainsKey(NodeID) && this.PendingUnitsByNode[NodeID] > 0)
			{
				this.PendingUnitsByNode[NodeID]--;
			}
			this.DeregisterPendingIronBaronyCompositionUnit(NodeID, Unit);
		}

		private void DeregisterPendingIronBaronyCompositionUnit(int NodeID, WorkingUnit Unit)
		{
			if ((!this.IsIronBarony() && !this.IsMaledor() && !this.IsBoruvian()) || this.PendingIronBaronyUnitTypesByNode == null || Unit == null)
			{
				return;
			}
			string unitName = this.GetDoctrineCompositionUnitName(Unit);
			if (unitName == null)
			{
				return;
			}
			Dictionary<string, int> counts;
			if (this.PendingIronBaronyUnitTypesByNode.TryGetValue(NodeID, out counts) && counts.ContainsKey(unitName) && counts[unitName] > 0)
			{
				counts[unitName]--;
			}
		}

		private bool CanSendToNode(int NodeID)
		{
			int pending = 0;
			if (this.PendingUnitsByNode != null)
			{
				if (!this.PendingUnitsByNode.ContainsKey(NodeID))
				{
					int existingCount = this.GetActualNodeUnitCount(NodeID);
					this.PendingUnitsByNode[NodeID] = existingCount;
					if (existingCount > 0)
					{
						this.LogStackDebug("  CanSendToNode discovered untracked node " + NodeID + " with " + existingCount + " existing units");
					}
				}
				this.PendingUnitsByNode.TryGetValue(NodeID, out pending);
			}
			if (pending >= 20)
			{
				this.LogStackDebug("    CanSendToNode(" + NodeID + ") = FALSE (pending=" + pending + ")");
			}
			return pending < 20;
		}

		private int GetActualNodeUnitCount(int NodeID)
		{
			int total = 0;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack.Node != null && stack.Node.ID == NodeID)
				{
					total += stack.Units.Count;
				}
			}
			return total;
		}

		private void RegisterPendingUnit(int NodeID)
		{
			if (this.PendingUnitsByNode == null)
			{
				return;
			}
			if (!this.PendingUnitsByNode.ContainsKey(NodeID))
			{
				this.PendingUnitsByNode[NodeID] = 0;
			}
			this.PendingUnitsByNode[NodeID]++;
			if (this.PendingUnitsByNode[NodeID] > 18)
			{
				this.LogStackDebug("  CRITICAL RegisterPendingUnit node=" + NodeID + " now=" + this.PendingUnitsByNode[NodeID]);
			}
		}

		private void RegisterPendingUnit(int NodeID, WorkingUnit Unit)
		{
			this.RegisterPendingUnit(NodeID);
			this.RegisterPendingIronBaronyCompositionUnit(NodeID, Unit);
		}

		private void CapitalStrike()
		{
			if (this.AI.Realm.Enemies.Count == 0)
			{
				return;
			}
			WorkingProvince capitol = this.AI.Realm.CapitolProvince;
			if (capitol == null || capitol.LandNode.CurrentStack == null)
			{
				return;
			}
			WorkingStack capitalArmy = capitol.LandNode.CurrentStack;
			if (capitalArmy.Units.Count < this.GetMinAttackUnits())
			{
				return;
			}
			WorkingStack enemy = this.GetNearbyEnemyForCapitalStrike();
			if (enemy == null)
			{
				return;
			}
			if (enemy.Node.Province == null)
			{
				return;
			}
			this.LogWarGoals("  Capital strike: attacking " + enemy.Owner.Name + " at " + enemy.Node.Province.Name + " (" + enemy.Units.Count + " units)");
			List<WorkingUnit> attackUnits = new List<WorkingUnit>();
			foreach (WorkingUnit unit in capitalArmy.Units)
			{
				if (!unit.Disabled && unit.Class != UnitClasses.Fort && unit.MovePoints > 0f)
				{
					attackUnits.Add(unit);
				}
			}
			if (attackUnits.Count < this.GetMinAttackUnits())
			{
				return;
			}
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			foreach (WorkingUnit unit2 in attackUnits)
			{
				List<WorkingUnit> unitList = new List<WorkingUnit>();
				unitList.Add(unit2);
				SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(capitol.LandNode, enemy.Node, unitList, true, this.AI.Realm, false);
				if (path.PathPoints.Count > 0 && unit2.MovePoints >= path.TotalMoveCost)
				{
					moveList.Add(new UnitMoveData(unit2, enemy.Node, path));
				}
			}
			if (moveList.Count >= this.GetMinAttackUnits())
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = "CapitalStrike";
				moveAction.MoveTargets = moveList;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.LogWarGoals("  Capital strike: sending " + moveList.Count + " units to attack " + enemy.Node.Province.Name);
			}
		}

		private bool IsFriendlyMovementNode(ActivePathNode Node)
		{
			if (Node == null || Node.Province == null)
			{
				return true;
			}
			WorkingProvince province = Node.Province;
			if (province.OwnerRealm == this.AI.Realm)
			{
				return true;
			}
			if (province.OccupierRealm == this.AI.Realm)
			{
				return true;
			}
			return province.OwnerRealm != null && this.AI.Realm.DiplomacyManager.GetRelation(province.OwnerRealm) == RelationStates.Alliance;
		}

		private bool IsIronBaronyHeroControlledProvince(WorkingProvince Province)
		{
			if (Province == null)
			{
				return false;
			}
			if (Province.OwnerRealm == this.AI.Realm || Province.OccupierRealm == this.AI.Realm)
			{
				return true;
			}
			return Province.OwnerRealm != null && this.AI.Realm.DiplomacyManager.GetRelation(Province.OwnerRealm) == RelationStates.Alliance;
		}

		private bool IsIronBaronyHeroFrontTarget(WorkingStack Stack)
		{
			if (Stack == null || Stack.Disposed || Stack.Hero != null || Stack.Node == null || Stack.Node.Province == null)
			{
				return false;
			}
			if (Stack.Units.Count < this.GetWarGoalStackMinimum())
			{
				return false;
			}
			if (Stack.Node.Province.IsCapitol)
			{
				return false;
			}
			return this.IsIronBaronyHeroControlledProvince(Stack.Node.Province);
		}

		private WorkingStack FindIronBaronyHeroDeploymentTarget(bool RequireReadyArmy)
		{
			WorkingStack bestTarget = null;
			int bestScore = int.MinValue;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack == null || stack.Disposed || stack.Hero != null || stack.Node == null || stack.Node.Province == null)
				{
					continue;
				}
				if (!this.IsIronBaronyHeroControlledProvince(stack.Node.Province))
				{
					continue;
				}
				if (RequireReadyArmy && stack.Units.Count < this.GetWarGoalStackMinimum())
				{
					continue;
				}
				int score = this.GetHeroTransferTargetScore(stack);
				score += stack.Units.Count * 20;
				if (stack.Units.Count >= this.GetWarGoalStackMinimum())
				{
					score += 500;
				}
				if (stack.Node.Province.OccupierRealm == this.AI.Realm && stack.Node.Province.OwnerRealm != this.AI.Realm)
				{
					score += 250;
				}
				if (stack.Node.Province.IsCapitol)
				{
					score -= 200;
				}
				if (bestTarget == null || score > bestScore)
				{
					bestTarget = stack;
					bestScore = score;
				}
			}
			return bestTarget;
		}

		private ActivePathNode GetHeroStepToward(WorkingHero Hero, ActivePathNode SourceNode, ActivePathNode TargetNode, out SovereigntyTK.Game.Path StepPath)
		{
			StepPath = null;
			if (Hero == null || SourceNode == null || TargetNode == null || Hero.MovePoints <= 0f)
			{
				return null;
			}
			List<WorkingUnit> unitList = new List<WorkingUnit>();
			unitList.Add(Hero);
			SovereigntyTK.Game.Path fullPath = this.AI.Game.PathManager.GetPath(SourceNode, TargetNode, unitList, false, this.AI.Realm, false);
			if (fullPath.PathPoints.Count <= 1)
			{
				return null;
			}
			float moveCost = 0f;
			ActivePathNode bestNode = null;
			for (int i = 1; i < fullPath.PathPoints.Count; i++)
			{
				PathPoint point = fullPath.PathPoints[i];
				if (!this.IsFriendlyMovementNode(point.Node))
				{
					break;
				}
				moveCost += point.MoveCost;
				if (moveCost > Hero.MovePoints)
				{
					break;
				}
				WorkingStack stackAtStep = point.Node.GetRealmStack(this.AI.Realm);
				if (stackAtStep == null || stackAtStep.Hero == null || stackAtStep.Hero == Hero)
				{
					bestNode = point.Node;
				}
			}
			if (bestNode == null)
			{
				return null;
			}
			StepPath = this.AI.Game.PathManager.GetPath(SourceNode, bestNode, unitList, true, this.AI.Realm, false);
			if (StepPath.PathPoints.Count == 0 || StepPath.TotalMoveCost > Hero.MovePoints)
			{
				return null;
			}
			return bestNode;
		}

		private int GetHeroTransferTargetScore(WorkingStack Stack)
		{
			int score = Stack.Units.Count * 10;
			if (Stack.Node != null && Stack.Node.Province != null)
			{
				foreach (GameRegion neighbor in Stack.Node.Province.GetAllConnectedRegions())
				{
					WorkingProvince province = neighbor as WorkingProvince;
					if (province != null && province.OwnerRealm != null && this.AI.Realm.Enemies.Contains(province.OwnerRealm))
					{
						score += 100;
						break;
					}
				}
			}
			return score;
		}

		private void DeployHeroToFront()
		{
			if (this.AI.Realm.Enemies.Count == 0)
			{
				return;
			}
			WorkingStack bestTarget = this.IsIronBarony() ? this.FindIronBaronyHeroDeploymentTarget(true) : null;
			int bestTargetScore = int.MinValue;
			if (bestTarget == null)
			{
				foreach (WorkingStack stack in this.AI.Realm.Stacks)
				{
					if (stack.Units.Count < this.GetWarGoalStackMinimum() || stack.Hero != null)
					{
						continue;
					}
					if (stack.Node == null || stack.Node.Province == null || stack.Node.Province.IsCapitol || stack.Node.Province.Occupied)
					{
						continue;
					}
					int score = this.GetHeroTransferTargetScore(stack);
					if (bestTarget == null || score > bestTargetScore)
					{
						bestTarget = stack;
						bestTargetScore = score;
					}
				}
			}
			if (bestTarget == null)
			{
				return;
			}
			WorkingStack sourceStack = null;
			WorkingHero hero = null;
			int bestHeroScore = int.MinValue;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack == bestTarget || stack.Hero == null || stack.Node == null || stack.Node.Province == null)
				{
					continue;
				}
				if (this.IsIronBarony() && !this.IsIronBaronyHeroControlledProvince(stack.Node.Province))
				{
					continue;
				}
				if (!this.IsIronBarony() && stack.Node.Province.Occupied)
				{
					continue;
				}
				if (stack.Hero.MovePoints <= 0f)
				{
					continue;
				}
				int score = 0;
				if (stack.Node.Province.IsCapitol)
				{
					score += 100;
				}
				if (stack.Units.Count < this.GetWarGoalStackMinimum())
				{
					score += 80;
				}
				if (bestTarget.Units.Count >= stack.Units.Count + 3)
				{
					score += 30;
				}
				if (this.IsIronBarony() && bestTarget.Node.Province.OccupierRealm == this.AI.Realm && bestTarget.Node.Province.OwnerRealm != this.AI.Realm)
				{
					score += 40;
				}
				if (score > bestHeroScore)
				{
					sourceStack = stack;
					hero = stack.Hero;
					bestHeroScore = score;
				}
			}
			if (hero == null || sourceStack == null || bestHeroScore <= 0)
			{
				return;
			}
			SovereigntyTK.Game.Path path;
			ActivePathNode stepNode = this.GetHeroStepToward(hero, sourceStack.Node, bestTarget.Node, out path);
			if (stepNode == null || path == null)
			{
				this.LogWarGoals("  Hero deployment blocked: " + hero.DisplayName + " cannot find a friendly reachable step toward army at node " + bestTarget.Node.ID);
				return;
			}
			if (!this.CanSendToNode(stepNode.ID))
			{
				return;
			}
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			moveList.Add(new UnitMoveData(hero, stepNode, path));
			this.RegisterPendingUnit(stepNode.ID);
			AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
			moveAction.DebugSource = "DeployHeroToFront";
			moveAction.MoveTargets = moveList;
			this.AI.ActionManager.AddAction(moveAction, true);
			string targetText = (stepNode == bestTarget.Node) ? "army" : "front";
			this.LogWarGoals("  Deploying hero " + hero.DisplayName + " from " + sourceStack.Node.Province.Name + " toward " + targetText + " at node " + bestTarget.Node.ID + " via node " + stepNode.ID);
		}


		private string GetWarGoalNodeLabel(ActivePathNode Node)
		{
			if (Node == null)
			{
				return "null";
			}
			string text = "node " + Node.ID;
			if (Node.Province != null)
			{
				WorkingRealm controller = this.GetProvinceController(Node.Province);
				text += " / " + Node.Province.Name;
				text += " owner=" + ((Node.Province.OwnerRealm != null) ? Node.Province.OwnerRealm.Name : "null");
				text += " controller=" + ((controller != null) ? controller.Name : "null");
				text += " occupied=" + Node.Province.Occupied;
			}
			return text;
		}

		private string GetWarGoalPathText(SovereigntyTK.Game.Path Path)
		{
			if (Path == null || Path.PathPoints == null || Path.PathPoints.Count == 0)
			{
				return "empty";
			}
			List<string> parts = new List<string>();
			foreach (PathPoint point in Path.PathPoints)
			{
				if (point == null || point.Node == null)
				{
					continue;
				}
				string part = point.Node.ID.ToString();
				if (point.Node.Province != null)
				{
					part += ":" + point.Node.Province.Name;
				}
				part += "(" + point.MoveCost.ToString("0.##") + ")";
				parts.Add(part);
			}
			return string.Join(" -> ", parts.ToArray()) + " total=" + Path.TotalMoveCost.ToString("0.##");
		}

		private void LogWarGoalPathProbe(string Context, WorkingStack Stack, WorkingProvince Goal, List<WorkingUnit> Units, SovereigntyTK.Game.Path StrategicPath)
		{
			try
			{
				if (Stack == null || Stack.Node == null || Goal == null || Goal.LandNode == null || Units == null || Units.Count == 0)
				{
					this.LogWarGoals("    Path probe " + Context + ": missing stack/goal/unit data");
					return;
				}
				bool endReachableThisTurn = Stack.Node.ReachableNodes != null && Stack.Node.ReachableNodes.Contains(Goal.LandNode.ID);
				this.LogWarGoals("    Path probe " + Context + ": from " + this.GetWarGoalNodeLabel(Stack.Node) + " to " + this.GetWarGoalNodeLabel(Goal.LandNode) + ", unit=" + Units[0].DisplayName + " move=" + Units[0].MovePoints.ToString("0.##") + ", end in current-turn ReachableNodes=" + endReachableThisTurn);
				SovereigntyTK.Game.Path turnPath = this.AI.Game.PathManager.GetPath(Stack.Node, Goal.LandNode, Units, true, this.AI.Realm, false);
				this.LogWarGoals("      Current-turn GetPath(CheckUnitMoves=true): " + this.GetWarGoalPathText(turnPath));
				this.LogWarGoals("      Strategic GetPath(CheckUnitMoves=false): " + this.GetWarGoalPathText(StrategicPath));
				if (StrategicPath != null && StrategicPath.PathPoints != null)
				{
					foreach (PathPoint point in StrategicPath.PathPoints)
					{
						if (point == null || point.Node == null)
						{
							continue;
						}
						int pendingCount = this.GetPendingNodeUnitCountForWarGoal(point.Node);
						this.LogWarGoals("      Strategic path node: " + this.GetWarGoalNodeLabel(point.Node) + ", our units/pending=" + pendingCount + "/20, moveCost=" + point.MoveCost.ToString("0.##"));
					}
				}
				foreach (ActiveNodeConnection connection in Stack.Node.ConnectedNodes)
				{
					if (connection == null || connection.TargetNode == null)
					{
						continue;
					}
					bool targetReachable = Stack.Node.ReachableNodes != null && Stack.Node.ReachableNodes.Contains(connection.TargetNode.ID);
					this.LogWarGoals("      Neighbor " + connection.ConnectionType + ": " + this.GetWarGoalNodeLabel(connection.TargetNode) + ", current-turn reachable=" + targetReachable);
				}
			}
			catch (Exception ex)
			{
				this.LogWarGoals("    Path probe " + Context + " failed: " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		private int GetPendingNodeUnitCountForWarGoal(ActivePathNode Node)
		{
			if (Node == null)
			{
				return 20;
			}
			if (this.PendingUnitsByNode != null)
			{
				if (!this.PendingUnitsByNode.ContainsKey(Node.ID))
				{
					this.PendingUnitsByNode[Node.ID] = this.GetActualNodeUnitCount(Node.ID);
				}
				return this.PendingUnitsByNode[Node.ID];
			}
			return this.GetActualNodeUnitCount(Node.ID);
		}

		private int GetWarGoalMovableStackUnitCount(WorkingStack Stack, WorkingProvince HostileTarget)
		{
			if (Stack == null || Stack.Units == null)
			{
				return 0;
			}
			int count = 0;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit == null || unit.Class == UnitClasses.Fort || unit.MovePoints <= 0f)
				{
					continue;
				}
				if (unit.Class == UnitClasses.Naval && !unit.Transport)
				{
					continue;
				}
				if (!this.ShouldMoveIronBaronyCatapultToTarget(unit, HostileTarget))
				{
					continue;
				}
				if (this.IsIronBaronyUnitRecovering(unit))
				{
					continue;
				}
				count++;
			}
			return count;
		}

		private bool WarGoalNodeHasRoomForStack(WorkingStack Stack, ActivePathNode TargetNode, WorkingProvince HostileTarget, string Context)
		{
			if (Stack == null || TargetNode == null)
			{
				return false;
			}
			if (TargetNode == Stack.Node)
			{
				return true;
			}
			int movingCount = this.GetWarGoalMovableStackUnitCount(Stack, HostileTarget);
			int pendingCount = this.GetPendingNodeUnitCountForWarGoal(TargetNode);
			int room = 20 - pendingCount;
			if (room < movingCount)
			{
				this.LogWarGoals("    " + Context + ": node " + this.GetWarGoalNodeLabel(TargetNode) + " blocked for full army move; room " + room + "/" + movingCount + " (existing/pending " + pendingCount + "/20)");
				return false;
			}
			return true;
		}

		private int GetWarGoalNodeRoom(ActivePathNode TargetNode)
		{
			if (TargetNode == null)
			{
				return 0;
			}
			return Math.Max(0, 20 - this.GetPendingNodeUnitCountForWarGoal(TargetNode));
		}

		private ActivePathNode GetIronBaronyStackStepToward(WorkingStack Stack, ActivePathNode TargetNode, WorkingProvince HostileTarget, out SovereigntyTK.Game.Path StepPath)
		{
			StepPath = null;
			if (Stack == null || Stack.Node == null || TargetNode == null)
			{
				return null;
			}
			WorkingUnit probeUnit = Stack.Units.FirstOrDefault((WorkingUnit x) => x != null && x.Class != UnitClasses.Fort && x.MovePoints > 0f && (x.Class != UnitClasses.Naval || x.Transport));
			if (probeUnit == null)
			{
				return null;
			}
			List<WorkingUnit> unitList = new List<WorkingUnit>();
			unitList.Add(probeUnit);
			SovereigntyTK.Game.Path fullPath = this.AI.Game.PathManager.GetPath(Stack.Node, TargetNode, unitList, false, this.AI.Realm, false);
			if (fullPath.PathPoints.Count <= 1)
			{
				return null;
			}
			float moveCost = 0f;
			ActivePathNode bestNode = null;
			ActivePathNode partialRoomNode = null;
			for (int i = 1; i < fullPath.PathPoints.Count; i++)
			{
				PathPoint point = fullPath.PathPoints[i];
				if (point == null || point.Node == null)
				{
					continue;
				}
				if (!this.IsFriendlyMovementNode(point.Node))
				{
					break;
				}
				moveCost += point.MoveCost;
				if (moveCost > probeUnit.MovePoints)
				{
					break;
				}
				int movingCount = this.GetWarGoalMovableStackUnitCount(Stack, HostileTarget);
				int pendingCount = this.GetPendingNodeUnitCountForWarGoal(point.Node);
				int room = 20 - pendingCount;
				if (room <= 0)
				{
					this.LogWarGoals("    War goal step: node " + this.GetWarGoalNodeLabel(point.Node) + " blocked; no room (existing/pending " + pendingCount + "/20)");
					continue;
				}
				WorkingStack stackAtStep = point.Node.GetRealmStack(this.AI.Realm);
				if (stackAtStep != null && stackAtStep != Stack && stackAtStep.Node != null && stackAtStep.Node.Province != null && !stackAtStep.Node.Province.IsCapitol && this.IsIronBaronyArmyReadyForWar(stackAtStep.Node.ID))
				{
					this.LogWarGoals("    War goal step: skipping ready army stack at " + this.GetWarGoalNodeLabel(point.Node));
					continue;
				}
				if (room < movingCount)
				{
					this.LogWarGoals("    War goal step: node " + this.GetWarGoalNodeLabel(point.Node) + " has partial room " + room + "/" + movingCount + " (existing/pending " + pendingCount + "/20); using as fallback instead of leaving army idle");
					if (partialRoomNode == null)
					{
						partialRoomNode = point.Node;
					}
					continue;
				}
				bestNode = point.Node;
			}
			if (bestNode == null)
			{
				bestNode = partialRoomNode;
			}
			if (bestNode == null)
			{
				return null;
			}
			StepPath = this.AI.Game.PathManager.GetPath(Stack.Node, bestNode, unitList, true, this.AI.Realm, false);
			if (StepPath.PathPoints.Count == 0 || StepPath.TotalMoveCost > probeUnit.MovePoints)
			{
				return null;
			}
			return bestNode;
		}

		private ActivePathNode GetWarGoalMoveTarget(WorkingStack Stack, ActivePathNode StrategicStagingNode, WorkingProvince HostileTarget, out string MoveTargetText)
		{
			MoveTargetText = "staging";
			if (Stack == null || Stack.Node == null || StrategicStagingNode == null || StrategicStagingNode == Stack.Node)
			{
				return StrategicStagingNode;
			}
			WorkingUnit probeUnit = Stack.Units.FirstOrDefault((WorkingUnit x) => x != null && x.Class != UnitClasses.Fort && x.MovePoints > 0f && (x.Class != UnitClasses.Naval || x.Transport));
			if (probeUnit == null)
			{
				return StrategicStagingNode;
			}
			List<WorkingUnit> unitList = new List<WorkingUnit>();
			unitList.Add(probeUnit);
			SovereigntyTK.Game.Path directPath = this.AI.Game.PathManager.GetPath(Stack.Node, StrategicStagingNode, unitList, true, this.AI.Realm, false);
			if (directPath.PathPoints.Count > 0 && directPath.TotalMoveCost <= probeUnit.MovePoints)
			{
				if (this.WarGoalNodeHasRoomForStack(Stack, StrategicStagingNode, HostileTarget, "War goal direct staging"))
				{
					MoveTargetText = "staging " + this.GetWarGoalNodeLabel(StrategicStagingNode);
					return StrategicStagingNode;
				}
				if (this.GetWarGoalNodeRoom(StrategicStagingNode) > 0)
				{
					MoveTargetText = "partial-room staging " + this.GetWarGoalNodeLabel(StrategicStagingNode);
					return StrategicStagingNode;
				}
			}
			SovereigntyTK.Game.Path stepPath;
			ActivePathNode stepNode = this.GetIronBaronyStackStepToward(Stack, StrategicStagingNode, HostileTarget, out stepPath);
			if (stepNode != null)
			{
				MoveTargetText = "reachable full-stack step " + this.GetWarGoalNodeLabel(stepNode) + " toward staging " + this.GetWarGoalNodeLabel(StrategicStagingNode);
				return stepNode;
			}
			MoveTargetText = "staging " + this.GetWarGoalNodeLabel(StrategicStagingNode) + " (no reachable current-turn full-stack step found)";
			return null;
		}

		private void PursueWarGoals()
		{
			this.AI.Realm.StacksChanged();
			if (this.AI.WarManager.InvasionTargets.Count == 0 && this.AI.WarManager.Wars.Count == 0)
			{
				return;
			}
			this.AI.Log("");
			this.LogWarGoals("Unit manager updating (war goals phase)");
			List<WorkingProvince> goalProvinces = new List<WorkingProvince>();
			foreach (KeyValuePair<int, InvasionTargetData> invasion in this.AI.WarManager.InvasionTargets)
			{
				WarGoalData goal = invasion.Value.WarGoal;
				if (goal.GoalType == WarGoalTypes.CaptureProvinces)
				{
					foreach (int provinceID in goal.ProvinceTargets)
					{
						WorkingProvince prov = this.AI.Game.AllProvinces.Values.FirstOrDefault((WorkingProvince x) => x.ID == provinceID);
						if (prov != null && prov.OwnerID != this.AI.Realm.ID)
						{
							WorkingRealm controller = this.GetProvinceController(prov);
							if (controller == this.AI.Realm)
							{
								this.LogWarGoals("  War goal: capture " + prov.Name + " is already controlled by us; treating it as bridgehead, not target");
								continue;
							}
							if (!this.ProvinceIsHostile(prov))
							{
								this.LogWarGoals("  War goal: capture " + prov.Name + " skipped: not hostile under current controller");
								continue;
							}
							goalProvinces.Add(prov);
							this.LogWarGoals("  War goal: capture " + prov.Name + " (owner: " + prov.OwnerRealm.Name + ")");
						}
					}
				}
				else if (goal.GoalType == WarGoalTypes.EliminateRealm)
				{
					WorkingRealm enemy = this.AI.Game.AllRealms.Values.FirstOrDefault((WorkingRealm x) => x.ID == invasion.Key);
					if (enemy != null && enemy.CapitolProvince != null && !enemy.RealmIsDead)
					{
						if (this.ProvinceIsHostile(enemy.CapitolProvince))
						{
							goalProvinces.Add(enemy.CapitolProvince);
							this.LogWarGoals("  War goal: eliminate " + enemy.Name + " (capital: " + enemy.CapitolProvince.Name + ")");
						}
						else
						{
							this.LogWarGoals("  War goal: eliminate " + enemy.Name + " skipped capital " + enemy.CapitolProvince.Name + ": already controlled/friendly, generating frontier goals instead");
						}
					}
				}
			}
			if (goalProvinces.Count == 0)
			{
				this.LogWarGoals("  No active war goal provinces, generating new goals");
				foreach (WorkingRealm enemy in this.AI.Realm.Enemies)
				{
					if (enemy.RealmIsDead)
					{
						continue;
					}
					foreach (WorkingProvince enemyProv in enemy.Provinces)
					{
						if (enemyProv == null || enemyProv.LandNode == null)
						{
							continue;
						}
						WorkingRealm enemyProvController = this.GetProvinceController(enemyProv);
						if (enemyProvController == this.AI.Realm)
						{
							this.LogWarGoals("  Bridgehead: " + enemyProv.Name + " is enemy-owned but controlled by us; using it to search deeper goals");
							continue;
						}
						if (!this.ProvinceIsHostile(enemyProv))
						{
							continue;
						}
						bool adjacentToUs = false;
						string adjacentProvinceName = null;
						foreach (GameRegion neighbor in enemyProv.GetAllConnectedRegions())
						{
							WorkingProvince neighborProvince = neighbor as WorkingProvince;
							if (neighborProvince != null && this.ProvinceIsFriendlyOrAllied(neighborProvince))
							{
								adjacentToUs = true;
								adjacentProvinceName = neighborProvince.Name;
								break;
							}
						}
						if (adjacentToUs && !goalProvinces.Contains(enemyProv))
						{
							goalProvinces.Add(enemyProv);
							this.LogWarGoals("  New goal: " + enemyProv.Name + " (frontier province of " + enemy.Name + ", adjacent to " + adjacentProvinceName + ")");
						}
					}
				}
			}
			if (goalProvinces.Count == 0)
			{
				this.LogWarGoals("  No valid war goal provinces found");
				return;
			}
			this.LogWarGoals("  Total war goal provinces: " + goalProvinces.Count);
			WorkingStack heroStack = null;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack.Hero != null && stack.Units.Count >= this.GetWarGoalStackMinimum() && stack.Node.Province != null && this.ProvinceCanServeAsArmyBase(stack.Node.Province) && (!stack.Node.Province.IsCapitol || this.IsIronBarony()))
				{
					if (heroStack == null || stack.Units.Count > heroStack.Units.Count)
					{
						heroStack = stack;
					}
				}
			}
			if (heroStack == null)
			{
				foreach (WorkingStack stack in this.AI.Realm.Stacks)
				{
					if (stack.Units.Count >= this.GetWarGoalStackMinimum() && stack.Node.Province != null && this.ProvinceCanServeAsArmyBase(stack.Node.Province) && (!stack.Node.Province.IsCapitol || this.IsIronBarony()))
					{
						if (heroStack == null || stack.Units.Count > heroStack.Units.Count)
						{
							heroStack = stack;
						}
					}
				}
			}
			if (heroStack != null)
			{
				this.LogWarGoals("  Primary army: " + heroStack.Units.Count + " units at " + (heroStack.Node.Province != null ? heroStack.Node.Province.Name : "node " + heroStack.Node.ID) + (heroStack.Hero != null ? " (hero: " + heroStack.Hero.DisplayName + ")" : " (no hero)"));
			}
			if (heroStack == null)
			{
				this.LogWarGoals("  No suitable army to pursue war goals");
				return;
			}
			float healthPercent = this.GetStackHealthPercent(heroStack);
			if (healthPercent < 0.5f)
			{
				this.LogWarGoals("  Army at " + (int)(healthPercent * 100f) + "% strength, resting to recover");
				return;
			}
			float moralePercent = this.GetStackMoralePercent(heroStack);
			if (moralePercent < 0.5f)
			{
				this.LogWarGoals("  Army morale at " + (int)(moralePercent * 100f) + "%, recovering");
				return;
			}
			WorkingProvince bestGoal = null;
			WorkingProvince bestHostileTarget = null;
			WorkingProvince bestStagingTarget = null;
			int bestDist = 999;
			foreach (WorkingProvince goal in goalProvinces)
			{
				List<WorkingUnit> unitList = new List<WorkingUnit>();
				unitList.Add(heroStack.Units[0]);
				SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(heroStack.Node, goal.LandNode, unitList, false, this.AI.Realm, false);
				SovereigntyTK.Game.Path currentTurnProbePath = this.AI.Game.PathManager.GetPath(heroStack.Node, goal.LandNode, unitList, true, this.AI.Realm, false);
				if (currentTurnProbePath.PathPoints.Count == 0 && path.PathPoints.Count > 0)
				{
					this.LogWarGoals("    Goal " + goal.Name + ": strategic path exists but current-turn CheckUnitMoves path is empty");
				}
				WorkingProvince pathHostile;
				WorkingProvince pathStaging;
				string blockReason = "unknown reason";
				if (path.PathPoints.Count > 0 && this.TryGetWarPathInfo(path, out pathHostile, out pathStaging, out blockReason))
				{
					if (path.PathPoints.Count < bestDist)
					{
						bestGoal = goal;
						bestHostileTarget = pathHostile;
						bestStagingTarget = pathStaging;
						bestDist = path.PathPoints.Count;
						this.LogWarGoals("    Goal " + goal.Name + ": " + path.PathPoints.Count + " hops via staging " + pathStaging.Name + " -> hostile " + pathHostile.Name + " (new best)");
					}
					else
					{
						this.LogWarGoals("    Goal " + goal.Name + ": " + path.PathPoints.Count + " hops via staging " + pathStaging.Name + " -> hostile " + pathHostile.Name + " (farther)");
					}
				}
				else if (path.PathPoints.Count > 0)
				{
					this.LogWarGoals("    Goal " + goal.Name + ": blocked - " + blockReason);
					this.LogWarGoalPathProbe("primary blocked", heroStack, goal, unitList, path);
				}
				else
				{
					this.LogWarGoals("    Goal " + goal.Name + ": no strategic path found");
					this.LogWarGoalPathProbe("primary", heroStack, goal, unitList, path);
				}
			}
			if (bestGoal == null || bestHostileTarget == null || bestStagingTarget == null)
			{
				this.LogWarGoals("  No reachable war goal through friendly/allied territory");
				return;
			}
			this.LogWarGoals("  Pursuing war goal: " + bestGoal.Name + " (" + bestDist + " hops). Hostile target: " + bestHostileTarget.Name + "; staging: " + bestStagingTarget.Name);
			WorkingStack stagedWarGoalStack = (bestStagingTarget.LandNode != null) ? bestStagingTarget.LandNode.GetRealmStack(this.AI.Realm) : null;
			if (stagedWarGoalStack != null && stagedWarGoalStack != heroStack && stagedWarGoalStack.Units.Count >= this.GetWarGoalStackMinimum() && this.GetStackHealthPercent(stagedWarGoalStack) >= 0.5f && this.GetStackMoralePercent(stagedWarGoalStack) >= 0.5f)
			{
				this.LogWarGoals("  War goal staging already has ready army at " + bestStagingTarget.Name + " (" + stagedWarGoalStack.Units.Count + " units); using it as assault army instead of pulling it backward");
				heroStack = stagedWarGoalStack;
			}
			ActivePathNode actualAssaultAnchorNode = heroStack.Node;
			if (bestStagingTarget.LandNode == heroStack.Node)
			{
				this.LogWarGoals("  Primary army already staged next to " + bestHostileTarget.Name + "; attack phase will handle battle");
				this.QueueIronBaronyCatapultSupport(bestHostileTarget, bestStagingTarget.LandNode, heroStack, "PursueWarGoals_Catapults");
			}
			else
			{
				string primaryMoveTargetText;
				ActivePathNode primaryMoveTargetNode = this.GetWarGoalMoveTarget(heroStack, bestStagingTarget.LandNode, bestHostileTarget, out primaryMoveTargetText);
				if (primaryMoveTargetNode == null || !this.CanSendToNode(primaryMoveTargetNode.ID))
				{
					this.LogWarGoals("  Staging target " + bestStagingTarget.Name + " / move target " + primaryMoveTargetText + " already being filled or unavailable, skipping primary army move");
				}
				else
				{
					if (primaryMoveTargetNode != bestStagingTarget.LandNode)
					{
						this.LogWarGoals("  Staging target " + bestStagingTarget.Name + " is beyond current move; using " + primaryMoveTargetText);
					}
					List<UnitMoveData> moveList = new List<UnitMoveData>();
				foreach (WorkingUnit unit in heroStack.Units)
				{
						if (!this.CanSendToNode(primaryMoveTargetNode.ID))
					{
						break;
					}
						if (!this.CanSendIronBaronyUnitToNode(unit, primaryMoveTargetNode, "PursueWarGoals"))
					{
						continue;
					}
					if (unit.Class == UnitClasses.Fort || unit.MovePoints <= 0f)
					{
						continue;
					}
					if (unit.Class == UnitClasses.Naval && !unit.Transport)
					{
						continue;
					}
					if (!this.ShouldMoveIronBaronyCatapultToTarget(unit, bestHostileTarget))
					{
						this.LogWarGoals("  Holding Orcish Catapult back: " + bestHostileTarget.Name + " has no active fortifications");
						continue;
					}
					List<WorkingUnit> unitList = new List<WorkingUnit>();
					unitList.Add(unit);
						SovereigntyTK.Game.Path unitPath = this.AI.Game.PathManager.GetPath(heroStack.Node, primaryMoveTargetNode, unitList, true, this.AI.Realm, false);
					if (unitPath.PathPoints.Count > 0 && unit.MovePoints >= unitPath.TotalMoveCost)
					{
							moveList.Add(new UnitMoveData(unit, primaryMoveTargetNode, unitPath));
							this.RegisterPendingUnit(primaryMoveTargetNode.ID, unit);
					}
				}
					if (moveList.Count > 0)
					{
						int fieldMoveCount = moveList.Count;
						bool heroAdded = this.AddIronBaronyHeroMoveWithArmy(heroStack, primaryMoveTargetNode, moveList, "PursueWarGoals");
						AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
						moveAction.DebugSource = "PursueWarGoals";
						moveAction.MoveTargets = moveList;
						this.AI.ActionManager.AddAction(moveAction, true);
						actualAssaultAnchorNode = primaryMoveTargetNode;
						this.LogWarGoals("  War goals: moving " + fieldMoveCount + " units" + (heroAdded ? " + hero" : "") + " to " + primaryMoveTargetText + " for " + bestGoal.Name);
					}
				}
				this.QueueIronBaronyCatapultSupport(bestHostileTarget, bestStagingTarget.LandNode, heroStack, "PursueWarGoals_Catapults");
			}
			// Dynamic front roles: the primary army is the assault stack for the chosen high-priority front.
			// Other attack-capable stacks become support stacks. They do not choose their own enemy target here;
			// they follow the assault stack's current province, or the province the assault stack just left after a move.
			int supportSlots = Math.Max(0, this.GetIronBaronyDynamicArmySlotCount() - 1);
			ActivePathNode supportAnchorNode = actualAssaultAnchorNode;
			WorkingProvince supportAnchorProvince = (supportAnchorNode != null) ? supportAnchorNode.Province : null;
			if (supportSlots > 0 && supportAnchorNode != null && supportAnchorProvince != null)
			{
				List<WorkingStack> supportArmies = new List<WorkingStack>();
				foreach (WorkingStack stack in this.AI.Realm.Stacks)
				{
					if (stack == heroStack || stack.Units.Count < this.GetWarGoalStackMinimum())
					{
						continue;
					}
					if (stack.Node == null || stack.Node.Province == null || stack.Node.Province.IsCapitol || !this.ProvinceCanServeAsArmyBase(stack.Node.Province))
					{
						this.LogWarGoals("  Support candidate at " + ((stack.Node != null && stack.Node.Province != null) ? stack.Node.Province.Name : "unknown") + " (" + stack.Units.Count + " units): skipped (capital/hostile-occupied/no province)");
						continue;
					}
					if (bestStagingTarget != null && bestStagingTarget.LandNode != null && stack.Node == bestStagingTarget.LandNode)
					{
						this.LogWarGoals("  Support candidate at " + stack.Node.Province.Name + " (" + stack.Units.Count + " units): already at war-goal staging, not pulling it backward");
						continue;
					}
					if (stack.Node.Province.OwnerRealm != this.AI.Realm && this.GetProvinceController(stack.Node.Province) == this.AI.Realm)
					{
						this.LogWarGoals("  Support candidate at " + stack.Node.Province.Name + " (" + stack.Units.Count + " units): bridgehead/frontline stack, not pulling it backward");
						continue;
					}
					if (this.GetStackHealthPercent(stack) < 0.5f || this.GetStackMoralePercent(stack) < 0.5f)
					{
						this.LogWarGoals("  Support candidate at " + stack.Node.Province.Name + " (" + stack.Units.Count + " units): skipped (damaged/morale low)");
						continue;
					}
					supportArmies.Add(stack);
				}
				supportArmies = supportArmies.OrderBy((WorkingStack x) => this.GetIronBaronyStrategicPathLength(x, supportAnchorNode)).ThenByDescending((WorkingStack x) => x.Units.Count).Take(supportSlots).ToList<WorkingStack>();
				this.LogWarGoals("  Front assignment: assault at " + supportAnchorProvince.Name + " -> " + bestHostileTarget.Name + "; support slots " + supportArmies.Count + "/" + supportSlots);
				foreach (WorkingStack support in supportArmies)
				{
					if (support.Node == supportAnchorNode)
					{
						this.LogWarGoals("  Support stack at " + support.Node.Province.Name + " already with assault stack");
						continue;
					}
					ActivePathNode supportMoveTarget = null;
					string supportMoveText = null;
					if (this.IronBaronyStackCanReachNodeThisTurn(support, supportAnchorNode) && this.WarGoalNodeHasRoomForStack(support, supportAnchorNode, bestHostileTarget, "Support follow anchor"))
					{
						supportMoveTarget = supportAnchorNode;
						supportMoveText = "assault anchor " + this.GetWarGoalNodeLabel(supportAnchorNode);
					}
					else
					{
						SovereigntyTK.Game.Path supportStepPath;
						supportMoveTarget = this.GetIronBaronyStackStepToward(support, supportAnchorNode, bestHostileTarget, out supportStepPath);
						if (supportMoveTarget != null)
						{
							supportMoveText = "support step " + this.GetWarGoalNodeLabel(supportMoveTarget) + " toward assault anchor " + this.GetWarGoalNodeLabel(supportAnchorNode);
						}
					}
					if (supportMoveTarget == null || !this.CanSendToNode(supportMoveTarget.ID))
					{
						this.LogWarGoals("  Support stack at " + support.Node.Province.Name + " cannot find a current-turn follow step toward assault at " + supportAnchorProvince.Name);
						continue;
					}
					List<UnitMoveData> supportMoveList = new List<UnitMoveData>();
					foreach (WorkingUnit unit2 in support.Units)
					{
						if (!this.CanSendToNode(supportMoveTarget.ID))
						{
							break;
						}
						if (!this.CanSendIronBaronyUnitToNode(unit2, supportMoveTarget, "PursueWarGoals_Support"))
						{
							continue;
						}
						if (unit2.Class == UnitClasses.Fort || unit2.MovePoints <= 0f)
						{
							continue;
						}
						if (unit2.Class == UnitClasses.Naval && !unit2.Transport)
						{
							continue;
						}
						if (!this.ShouldMoveIronBaronyCatapultToTarget(unit2, bestHostileTarget))
						{
							this.LogWarGoals("  Holding Orcish Catapult back from support move: " + bestHostileTarget.Name + " has no active fortifications");
							continue;
						}
						List<WorkingUnit> unitList3 = new List<WorkingUnit>();
						unitList3.Add(unit2);
						SovereigntyTK.Game.Path unitPath2 = this.AI.Game.PathManager.GetPath(support.Node, supportMoveTarget, unitList3, true, this.AI.Realm, false);
						if (unitPath2.PathPoints.Count > 0 && unit2.MovePoints >= unitPath2.TotalMoveCost)
						{
							supportMoveList.Add(new UnitMoveData(unit2, supportMoveTarget, unitPath2));
							this.RegisterPendingUnitMove(support.Node.ID, supportMoveTarget.ID, unit2);
						}
					}
					if (supportMoveList.Count > 0)
					{
						int supportFieldMoveCount = supportMoveList.Count;
						bool supportHeroAdded = this.AddIronBaronyHeroMoveWithArmy(support, supportMoveTarget, supportMoveList, "PursueWarGoals_Support");
						AIActionMoveUnits supportMoveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
						supportMoveAction.DebugSource = "PursueWarGoals_Support";
						supportMoveAction.MoveTargets = supportMoveList;
						this.AI.ActionManager.AddAction(supportMoveAction, true);
						this.LogWarGoals("  Support stack at node " + support.Node.ID + ": moving " + supportFieldMoveCount + " units" + (supportHeroAdded ? " + hero" : "") + " to " + supportMoveText + " behind assault on " + bestHostileTarget.Name);
					}
				}
			}

		}

		private void ReclaimOccupiedProvinces()
		{
			this.AI.Realm.StacksChanged();
			if (this.AI.Realm.OccupiedProvinces.Count == 0)
			{
				return;
			}
			this.AI.Log("");
			this.AI.Log("Unit manager updating (reclaim phase)");
			int capitalEnemyDistance = this.GetEnemyDistanceFromCapital(CapitalSafeRadius);
			int capitalTargetGarrison = this.GetCapitalGarrisonTarget(capitalEnemyDistance);
			List<UnitMoveData> moveList = new List<UnitMoveData>();
			foreach (WorkingProvince occupied in this.AI.Realm.OccupiedProvinces)
			{
				int enemyStrength = 0;
				if (occupied.LandNode.CurrentStack != null)
				{
					enemyStrength = this.GetStackStrength(occupied.LandNode.CurrentStack);
				}
				enemyStrength += this.GetNearbyEnemyReinforcements(occupied);
				WorkingStack bestStack = null;
				int bestStrength = 0;
				SovereigntyTK.Game.Path bestPath = null;
				foreach (WorkingStack stack in this.AI.Realm.Stacks)
				{
					if (stack.Units.Count < 4 || stack.Node.Province == null || !this.ProvinceCanServeAsArmyBase(stack.Node.Province))
					{
						continue;
					}
					if (stack.Node.Province.IsCapitol && this.GetPlannedNodeUnitCount(stack.Node) <= capitalTargetGarrison)
					{
						continue;
					}
					int stackStrength = this.GetStackStrength(stack);
					if (enemyStrength > 0 && (float)stackStrength < (float)enemyStrength * 0.7f)
					{
						continue;
					}
					List<WorkingUnit> unitList = new List<WorkingUnit>();
					unitList.Add(stack.Units[0]);
					SovereigntyTK.Game.Path path = this.AI.Game.PathManager.GetPath(stack.Node, occupied.LandNode, unitList, true, this.AI.Realm, false);
					if (path.PathPoints.Count > 0 && path.PathPoints.Count <= 6)
					{
						if (stackStrength > bestStrength)
						{
							bestStack = stack;
							bestStrength = stackStrength;
							bestPath = path;
						}
					}
				}
				if (bestStack == null)
				{
					this.AI.Log("  No suitable force to reclaim " + occupied.Name);
					continue;
				}
				this.AI.Log("  Sending " + bestStack.Units.Count + " units to reclaim " + occupied.Name + " (our strength " + bestStrength + " vs enemy " + enemyStrength + ")");
				float reclaimHealth = this.GetStackHealthPercent(bestStack);
				if (reclaimHealth < 0.5f)
				{
					this.AI.Log("  Reclaim force at " + (int)(reclaimHealth * 100f) + "% strength, waiting to recover");
					continue;
				}
				float reclaimMorale = this.GetStackMoralePercent(bestStack);
				if (reclaimMorale < 0.5f)
				{
					this.AI.Log("  Reclaim force morale at " + (int)(reclaimMorale * 100f) + "%, recovering");
					continue;
				}




	
				int sourcePlannedUnits = this.GetPlannedNodeUnitCount(bestStack.Node);
				foreach (WorkingUnit unit in bestStack.Units)
				{
					if (bestStack.Node.Province != null && bestStack.Node.Province.IsCapitol && sourcePlannedUnits <= capitalTargetGarrison)
					{
						break;
					}
					if (!this.CanSendToNode(occupied.LandNode.ID))
					{
						break;
					}
					if (!this.CanSendIronBaronyUnitToNode(unit, occupied.LandNode, "ReclaimOccupiedProvinces"))
					{
						continue;
					}
					if (unit.Class == UnitClasses.Fort || unit.MovePoints <= 0f)
					{
						continue;
					}
					if (unit.Class == UnitClasses.Naval && !unit.Transport)
					{
						continue;
					}
					List<WorkingUnit> unitList = new List<WorkingUnit>();
					unitList.Add(unit);
					SovereigntyTK.Game.Path unitPath = this.AI.Game.PathManager.GetPath(bestStack.Node, occupied.LandNode, unitList, true, this.AI.Realm, false);
					if (unitPath.PathPoints.Count > 0 && unit.MovePoints >= unitPath.TotalMoveCost)
					{
						moveList.Add(new UnitMoveData(unit, occupied.LandNode, unitPath));
						this.RegisterPendingUnit(occupied.LandNode.ID, unit);
						sourcePlannedUnits--;
					}
				}
			}
			if (moveList.Count > 0)
			{
				AIActionMoveUnits moveAction = this.AI.ActionManager.CreateAction<AIActionMoveUnits>();
				moveAction.DebugSource = "ReclaimOccupiedProvinces";
				moveAction.MoveTargets = moveList;
				this.AI.ActionManager.AddAction(moveAction, true);
				this.AI.Log("  Reclaim: moving " + moveList.Count + " units toward occupied provinces");
			}
		}

		private bool RealmIsEnemy(WorkingRealm Realm)
		{
			return this.AI.Realm != Realm && (this.AI.Realm.Enemies.Contains(Realm) || this.AI.WarManager.InvasionTargets.ContainsKey(Realm.ID));
		}

		private WorkingRealm GetProvinceController(WorkingProvince Province)
		{
			if (Province == null)
			{
				return null;
			}
			if (Province.OccupierRealm != null)
			{
				return Province.OccupierRealm;
			}
			return Province.OwnerRealm;
		}

		private bool ProvinceIsFriendlyOrAllied(WorkingProvince Province)
		{
			WorkingRealm controller = this.GetProvinceController(Province);
			if (controller == null)
			{
				return false;
			}
			if (controller == this.AI.Realm)
			{
				return true;
			}
			return controller.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Alliance;
		}

		private bool ProvinceIsHostile(WorkingProvince Province)
		{
			WorkingRealm controller = this.GetProvinceController(Province);
			return controller != null && this.RealmIsEnemy(controller);
		}

private bool ProvinceCanServeAsArmyBase(WorkingProvince Province)
		{
			if (Province == null)
			{
				return false;
			}
			if (!Province.Occupied)
			{
				return true;
			}
			return this.GetProvinceController(Province) == this.AI.Realm;
		}

		private bool IsDoctrineGarrisonOnlyUnit(WorkingUnit Unit)
		{
			if (Unit == null)
			{
				return false;
			}
			string unitName = (Unit.BaseType != null) ? Unit.BaseType.Name : Unit.DisplayName;
			if (this.IsMaledor())
			{
				return unitName == "The Worm" || unitName == "Inquisitor";
			}
			if (this.IsBoruvian())
			{
				return unitName == "King's Retinue";
			}
			return false;
		}

		private bool CanDoctrineGarrisonOnlyUnitMoveToNode(WorkingUnit Unit, ActivePathNode TargetNode)
		{
			if (!this.IsDoctrineGarrisonOnlyUnit(Unit))
			{
				return true;
			}
			if (TargetNode == null || TargetNode.Province == null)
			{
				return true;
			}
			return TargetNode.Province.OwnerRealm == this.AI.Realm;
		}

				private bool TryGetWarPathInfo(SovereigntyTK.Game.Path Path, out WorkingProvince FirstHostileProvince, out WorkingProvince StagingProvince, out string BlockReason)
		{
			FirstHostileProvince = null;
			StagingProvince = null;
			BlockReason = null;
			if (Path == null || Path.PathPoints == null || Path.PathPoints.Count == 0)
			{
				BlockReason = "no path";
				return false;
			}
			WorkingProvince lastFriendlyOrAllied = null;
			foreach (PathPoint point in Path.PathPoints)
			{
				if (point.Node == null || point.Node.Province == null)
				{
					continue;
				}
				WorkingProvince province = point.Node.Province;
				if (this.ProvinceIsFriendlyOrAllied(province))
				{
					lastFriendlyOrAllied = province;
					continue;
				}
				if (this.ProvinceIsHostile(province))
				{
					FirstHostileProvince = province;
					StagingProvince = lastFriendlyOrAllied;
					if (StagingProvince == null)
					{
						BlockReason = "no friendly staging province before hostile province " + province.Name;
						return false;
					}
					return true;
				}
				WorkingRealm controller = this.GetProvinceController(province);
				BlockReason = "blocked by neutral/non-enemy province " + province.Name + ((controller != null) ? " (controlled by " + controller.Name + ")" : "");
				return false;
			}
			BlockReason = "path never reaches a hostile province";
			return false;
		}

		public void AddIgnoreProvince(WorkingProvince Province)
		{
			this.IgnoreProvinces.Add(Province.ID);
		}

		private ActivePathNode GetAttackNodeForStack(WorkingStack Stack, WorkingProvince Province)
		{
			if (Province == null)
			{
				return null;
			}
			bool navalSource = Stack != null && Stack.Node != null && Stack.Node.GetRegion() is WorkingZone;
			if (navalSource && Province.HarbourNode != null)
			{
				WorkingStack harbourStack = Province.HarbourNode.CurrentStack;
				if (harbourStack != null && !harbourStack.Disposed && harbourStack.Units.Count > 0 && harbourStack.Owner != this.AI.Realm && this.RealmIsEnemy(harbourStack.Owner))
				{
					return Province.HarbourNode;
				}
				return Province.LandNode;
			}
			return Province.LandNode;
		}

		private ActivePathNode GetTargetValueNode(WorkingProvince Province, bool NavalAttack)
		{
			if (Province == null)
			{
				return null;
			}
			if (NavalAttack && Province.HarbourNode != null)
			{
				WorkingStack harbourStack = Province.HarbourNode.CurrentStack;
				if (harbourStack != null && !harbourStack.Disposed && harbourStack.Units.Count > 0 && harbourStack.Owner != this.AI.Realm && this.RealmIsEnemy(harbourStack.Owner))
				{
					return Province.HarbourNode;
				}
			}
			return Province.LandNode;
		}

		private float GetTargetValue(WorkingProvince Province, bool NavalAttack)
		{
			float num = 0f;
			ActivePathNode activePathNode = this.GetTargetValueNode(Province, NavalAttack);
			if (activePathNode == null)
			{
				return -9999f;
			}
			if (Province.OccupierRealm == this.AI.Game.RebelRealm)
			{
				num += 10f;
			}
			if (activePathNode.CurrentStack == null)
			{
				num += 20f;
			}
			else
			{
				float num2 = 10f;
				num2 -= (float)activePathNode.CurrentStack.Units.Count;
				num2 -= (float)Province.FortLevel;
				num += num2;
			}
			num += (float)Province.ResearchPoints;
			if (Province.Resource != null && this.AI.Realm.UnitPurchaseManager.ResourceIsUseful(Province.Resource))
			{
				num += 10f;
			}
			num -= (float)Province.CurrentEconomy;
			int num3 = 0;
			foreach (GameRegion gameRegion in Province.GetAllConnectedRegions())
			{
				if (!(gameRegion is WorkingZone) && (gameRegion as WorkingProvince).OwnerID == this.AI.Realm.ID)
				{
					num3++;
				}
			}
			num3 = 4 - num3;
			num -= (float)(num3 * 5);
			num += (float)Province.AILust;
			num += (float)this.AI.WarManager.GetLustModifier(Province.Name);
			num += this.GetWarGoalBonus(Province);
			if (Province.IsCapitol && Province.OwnerRealm != this.AI.Realm && !this.AI.IgnoreCapitolLust)
			{
				num -= 40f;
			}
			return num;
		}

		private float GetWarGoalBonus(WorkingProvince Province)
		{
			float bonus = 0f;
			foreach (KeyValuePair<int, InvasionTargetData> invasion in this.AI.WarManager.InvasionTargets)
			{
				WarGoalData goal = invasion.Value.WarGoal;
				switch (goal.GoalType)
				{
				case WarGoalTypes.CaptureProvinces:
					if (goal.ProvinceTargets.Contains(Province.ID))
					{
						bonus += 30f;
					}
					break;
				case WarGoalTypes.EliminateRealm:
					if (Province.OwnerID == invasion.Key)
					{
						bonus += 15f;
						if (Province.IsCapitol)
						{
							bonus += 40f;
						}
					}
					break;
				case WarGoalTypes.LootRealm:
					if (Province.OwnerID == invasion.Key && Province.CurrentEconomy >= 4)
					{
						bonus += 20f;
					}
					break;
				case WarGoalTypes.DamageProvinces:
					if (Province.OwnerID == invasion.Key && Province.CurrentEconomy >= 3)
					{
						bonus += 15f;
					}
					break;
				case WarGoalTypes.CauseCasualties:
					if (Province.OwnerID == invasion.Key && Province.LandNode.CurrentStack != null)
					{
						bonus += 20f;
					}
					break;
				}
			}
			foreach (WarData war in this.AI.WarManager.Wars.Values)
			{
				if (Province.OwnerID == war.EnemyID)
				{
					if (Province.Landmark != null)
					{
						bonus += 15f;
					}
					if (Province.Cradle != ArtScienceTypes.None)
					{
						bonus += 10f;
					}
					if (Province.Resource != null)
					{
						bonus += 10f;
					}
				}
			}
			if (Province.OwnerHistory.RealmHasClaim(this.AI.Realm.ID))
			{
				int claimAge = Province.OwnerHistory.GetRealmClaimAge(this.AI.Realm.ID);
				if (claimAge == 0)
				{
					bonus += 25f;
				}
				else if (claimAge < 10)
				{
					bonus += 20f;
				}
				else
				{
					bonus += 10f;
				}
			}
			if (Province.OwnerHistory.OriginalOwnerID == this.AI.Realm.ID)
			{
				bonus += 15f;
			}
			return bonus;
		}


		private int GetIronBaronyActualFieldUnitCount(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return 0;
			}
			int count = 0;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit == null || unit.Disabled || unit.Class == UnitClasses.Fort)
				{
					continue;
				}
				if (unit.Class == UnitClasses.Naval && !unit.Transport)
				{
					continue;
				}
				if (this.IsIronBaronyCatapult(unit))
				{
					continue;
				}
				count++;
			}
			return count;
		}

		private bool IsIronBaronyAttackArmyCandidate(WorkingStack Stack)
		{
			if (!this.IsIronBarony() || Stack == null || Stack.Node == null || Stack.Node.Province == null)
			{
				return false;
			}
			if (Stack.Node.Province.Occupied)
			{
				return false;
			}
			if (this.GetIronBaronyActualFieldUnitCount(Stack) < this.GetWarGoalStackMinimum())
			{
				return false;
			}
			if (this.GetStackHealthPercent(Stack) < 0.5f || this.GetStackMoralePercent(Stack) < 0.5f)
			{
				return false;
			}
			return true;
		}

		private WorkingStack GetIronBaronyPrimaryAssaultStack()
		{
			if (!this.IsIronBarony())
			{
				return null;
			}
			WorkingStack bestHeroStack = null;
			WorkingStack bestStack = null;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (!this.IsIronBaronyAttackArmyCandidate(stack))
				{
					continue;
				}
				if (bestStack == null || this.GetIronBaronyActualFieldUnitCount(stack) > this.GetIronBaronyActualFieldUnitCount(bestStack) || (this.GetIronBaronyActualFieldUnitCount(stack) == this.GetIronBaronyActualFieldUnitCount(bestStack) && stack.Units.Count > bestStack.Units.Count))
				{
					bestStack = stack;
				}
				if (stack.Hero != null && (bestHeroStack == null || this.GetIronBaronyActualFieldUnitCount(stack) > this.GetIronBaronyActualFieldUnitCount(bestHeroStack) || (this.GetIronBaronyActualFieldUnitCount(stack) == this.GetIronBaronyActualFieldUnitCount(bestHeroStack) && stack.Units.Count > bestHeroStack.Units.Count)))
				{
					bestHeroStack = stack;
				}
			}
			return (bestHeroStack != null) ? bestHeroStack : bestStack;
		}

		private bool IronBaronyStackCanAttackTargetNow(WorkingStack Stack, WorkingProvince Target)
		{
			if (!this.IsIronBaronyAttackArmyCandidate(Stack) || Target == null || Target.LandNode == null)
			{
				return false;
			}
			ActivePathNode activePathNode = Target.LandNode;
			if (Stack.Node.GetRegion() is WorkingZone && Target.HarbourNode != null)
			{
				activePathNode = Target.HarbourNode;
			}
			int movableAttackers = 0;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit == null || unit.Disabled || unit.Class == UnitClasses.Fort || !unit.HasMoves())
				{
					continue;
				}
				if (unit.Class == UnitClasses.Naval && !unit.Transport)
				{
					continue;
				}
				if (!this.ShouldMoveIronBaronyCatapultToTarget(unit, Target))
				{
					continue;
				}
				if (this.AI.Game.DestinationChecker.NodeOKForUnit(unit, activePathNode) == UnitMoveResult.OK)
				{
					movableAttackers++;
				}
			}
			return movableAttackers >= this.GetMinAttackUnits();
		}

		private bool HasIronBaronySecondArmyReadyToAttack(WorkingStack PrimaryStack, WorkingProvince Target, out string SupportText)
		{
			SupportText = "none";
			WorkingStack bestReady = null;
			int bestField = 0;
			WorkingStack bestPartial = null;
			int bestPartialField = 0;
			foreach (WorkingStack stack in this.AI.Realm.Stacks)
			{
				if (stack == PrimaryStack || stack == null || stack.Node == null || stack.Node.Province == null)
				{
					continue;
				}
				int fieldCount = this.GetIronBaronyActualFieldUnitCount(stack);
				if (fieldCount > bestPartialField && stack.Node.Province != null && !stack.Node.Province.Occupied)
				{
					bestPartial = stack;
					bestPartialField = fieldCount;
				}
				if (this.IronBaronyStackCanAttackTargetNow(stack, Target))
				{
					if (bestReady == null || fieldCount > bestField || (fieldCount == bestField && stack.Units.Count > bestReady.Units.Count))
					{
						bestReady = stack;
						bestField = fieldCount;
					}
				}
			}
			if (bestReady != null)
			{
				SupportText = bestReady.Node.Province.Name + " (field " + bestField + "/20, total " + bestReady.Units.Count + "/20" + (bestReady.Hero != null ? ", hero" : "") + ")";
				return true;
			}
			if (bestPartial != null && bestPartialField > 0)
			{
				SupportText = "best second army is " + bestPartial.Node.Province.Name + " (field " + bestPartialField + "/20, total " + bestPartial.Units.Count + "/20" + (bestPartial.Hero != null ? ", hero" : "") + ")";
			}
			return false;
		}

		private bool HasIronBaronyPrimaryArmyReadyToAttack(WorkingStack SupportStack, WorkingProvince Target, out string PrimaryText)
		{
			PrimaryText = "none";
			WorkingStack primary = this.GetIronBaronyPrimaryAssaultStack();
			if (primary == null || primary == SupportStack)
			{
				return false;
			}
			if (!this.IronBaronyStackCanAttackTargetNow(primary, Target))
			{
				if (primary.Node != null && primary.Node.Province != null)
				{
					PrimaryText = primary.Node.Province.Name + " (field " + this.GetIronBaronyActualFieldUnitCount(primary) + "/20, total " + primary.Units.Count + "/20" + (primary.Hero != null ? ", hero" : "") + ")";
				}
				return false;
			}
			PrimaryText = primary.Node.Province.Name + " (field " + this.GetIronBaronyActualFieldUnitCount(primary) + "/20, total " + primary.Units.Count + "/20" + (primary.Hero != null ? ", hero" : "") + ")";
			return true;
		}

		private bool ShouldIronBaronyHoldAttackForArmyWave(WorkingStack Stack, WorkingProvince Target, out string Reason)
		{
			Reason = null;
			if (!this.IsIronBarony() || Stack == null || Target == null || !this.IsIronBaronyAttackArmyCandidate(Stack))
			{
				return false;
			}
			if (Target.OwnerRealm == this.AI.Realm)
			{
				return false;
			}
			WorkingStack primary = this.GetIronBaronyPrimaryAssaultStack();
			bool isPrimary = (primary == Stack);
			if (isPrimary)
			{
				// Do not make Army #1 wait for Army #2. The previous strict two-army gate made
				// Iron Barony stall in front of empty enemy provinces while the second army was
				// still forming. Army #1 is the spearhead: it should keep taking legal targets.
				string supportText;
				if (this.HasIronBaronySecondArmyReadyToAttack(Stack, Target, out supportText))
				{
					this.AI.Log("      Iron Barony support doctrine: second army can join attack on " + Target.Name + " from " + supportText);
				}
				else
				{
					this.AI.Log("      Iron Barony support doctrine: primary army will not wait for second army before attacking " + Target.Name + " (" + supportText + ")");
				}
				return false;
			}
			string primaryText;
			if (this.HasIronBaronyPrimaryArmyReadyToAttack(Stack, Target, out primaryText))
			{
				this.AI.Log("      Iron Barony support doctrine: primary army ready for " + Target.Name + " at " + primaryText);
				return false;
			}
			Reason = "support army will not attack " + Target.Name + " alone; waiting for primary army (" + primaryText + ")";
			return true;
		}

		private bool CanWinProvince(WorkingStack Stack, WorkingProvince Province, bool NavalAttack)
		{
			ActivePathNode activePathNode = this.GetAttackNodeForStack(Stack, Province);
			if (activePathNode == null)
			{
				this.LogStackDebug("    CanWinProvince: " + this.AI.Realm.Name + " stack=" + Stack.Units.Count + " units at node " + Stack.Node.ID + " vs province " + Province.Name + " -> REJECT: no valid attack node");
				return false;
			}
			this.LogStackDebug("    CanWinProvince: " + this.AI.Realm.Name + " stack=" + Stack.Units.Count + " units at node " + Stack.Node.ID + " vs province " + Province.Name + " using node " + activePathNode.ID + " type=" + activePathNode.NodeType);
			if (Province.OwnerRealm == this.AI.Game.RebelRealm && this.AI.IgnoreRebels > 0)
			{
				this.LogStackDebug("      -> REJECT: ignoring rebels");
				return false;
			}
			if (this.AI.Realm.Restrictions.IgnoreProvinces.Contains(Province.Name))
			{
				this.LogStackDebug("      -> REJECT: restricted province");
				return false;
			}
			if (Stack.Units.Count < this.GetMinAttackUnits())
			{
				this.LogStackDebug("      -> REJECT: too small (" + Stack.Units.Count + "/" + this.GetMinAttackUnits() + ")");
				this.AI.Log("      Stack too small (" + Stack.Units.Count + " units, minimum " + this.GetMinAttackUnits() + ")");
				return false;
			}
			float stackHealth = this.GetStackHealthPercent(Stack);
			if (stackHealth < 0.5f)
			{
				this.LogStackDebug("      -> REJECT: health " + (int)(stackHealth * 100f) + "% (need 50%)");
				this.AI.Log("      Stack at " + (int)(stackHealth * 100f) + "% strength, resting");
				return false;
			}
			float stackMorale = this.GetStackMoralePercent(Stack);
			if (stackMorale < 0.5f)
			{
				this.LogStackDebug("      -> REJECT: morale " + (int)(stackMorale * 100f) + "% (need 50%)");
				this.AI.Log("      Stack morale at " + (int)(stackMorale * 100f) + "%, recovering");
				return false;
			}
			int woundedCount = 0;
			int totalCount = 0;
			foreach (WorkingUnit wu in Stack.Units)
			{
				if (!wu.Disabled)
				{
					totalCount++;
					if ((int)wu.Health <= 25)
					{
						woundedCount++;
					}
				}
			}
			if (totalCount > 0 && woundedCount > totalCount / 3)
			{
				this.LogStackDebug("      -> REJECT: " + woundedCount + "/" + totalCount + " units critically wounded (HP<=25)");
				this.AI.Log("      Too many wounded units (" + woundedCount + "/" + totalCount + "), resting");
				return false;
			}
			float num;
			if (activePathNode.CurrentStack == null)
			{
				this.LogStackDebug("      Empty province. Attacking.");
				if (activePathNode.Province != null && this.ProvinceHasActiveFortifications(activePathNode.Province))
				{
					int neededCatapults = this.IsIronBarony() ? 4 : 1;
					int siegeCount = Stack.Units.Count((WorkingUnit x) => x.Class == UnitClasses.Siege);
					if (siegeCount < neededCatapults)
					{
						this.SiegeUnitNeeded = true;
						this.LogStackDebug("      -> REJECT: fortified empty province needs siege support (siege " + siegeCount + "/" + neededCatapults + "). SiegeUnitNeeded set.");
						return false;
					}
				}
				num = 100f;
			}
			else
			{
				int defenderStrength = this.GetStackStrength(activePathNode.CurrentStack);
				int attackerStrength = this.GetStackStrength(Stack);
				int defenderUnits = activePathNode.CurrentStack.Units.Count;
				this.LogStackDebug("      Defended. Our strength=" + attackerStrength + "(" + Stack.Units.Count + "u) vs defender=" + defenderStrength + "(" + defenderUnits + "u)");
				if (defenderStrength > 0 && (float)defenderStrength > (float)attackerStrength * 1.3f)
				{
					this.LogStackDebug("      -> REJECT: defender too strong (" + defenderStrength + " > " + attackerStrength + " * 1.3)");
					this.AI.Log("      Defender too strong (" + defenderStrength + " vs our " + attackerStrength + ")");
					return false;
				}
				List<WorkingUnit> list = new List<WorkingUnit>(activePathNode.CurrentStack.Units);
				int num2 = activePathNode.Province.FortLevel;
				int num3 = 0;
				foreach (WorkingUnit workingUnit in activePathNode.Province.Forts)
				{
					if (num3 >= num2)
					{
						break;
					}
					if (!workingUnit.Disabled)
					{
						list.Add(workingUnit);
						num3++;
					}
				}
				if (num3 > 0)
				{
					int neededCatapults = this.IsIronBarony() ? Math.Min(4, num3) : 1;
					int siegeCount = Stack.Units.Count((WorkingUnit x) => x.Class == UnitClasses.Siege);
					if (siegeCount < neededCatapults)
					{
						this.SiegeUnitNeeded = true;
						this.LogStackDebug("      -> REJECT: fort detected (" + num3 + " active forts), siege support too small (siege " + siegeCount + "/" + neededCatapults + "). SiegeUnitNeeded set.");
						return false;
					}
				}
				List<WorkingUnit> list2 = new List<WorkingUnit>();
				foreach (WorkingUnit workingUnit2 in Stack.Units)
				{
					if (this.AI.Game.DestinationChecker.NodeOKForUnit(workingUnit2, activePathNode) == UnitMoveResult.OK)
					{
						if (!this.ShouldMoveIronBaronyCatapultToTarget(workingUnit2, Province))
						{
							continue;
						}
						list2.Add(workingUnit2);
					}
				}
				if (list2.Count == 0)
				{
					return false;
				}
				if (list2.Count < this.GetMinAttackUnits())
				{
					this.LogStackDebug("      -> REJECT: only " + list2.Count + " eligible attackers (minimum " + this.GetMinAttackUnits() + ")");
					this.AI.Log("      Too few eligible attackers (" + list2.Count + ", minimum " + this.GetMinAttackUnits() + ")");
					return false;
				}
				if (list.Count == 0)
				{
					return true;
				}
				int damageToDefender = 0;
				int damageToAttacker = 0;
				foreach (WorkingUnit attacker in list2)
				{
					WorkingUnit target = list[this.AI.RNG.Next(list.Count)];
					bool ranged = attacker.RangedAttack > 0;
					CombatResults combatResults = CombatManager.PerformCombat(attacker, target, CombatType.Simulated, ranged, true, false);
					if (combatResults != null)
					{
						int dealt = combatResults.DefenderCasualties;
						if (ranged)
						{
							dealt = (int)(dealt * 1.5f);
						}
						damageToDefender += dealt;
						damageToAttacker += combatResults.AttackerCasualties;
					}
				}
				foreach (WorkingUnit defender in list)
				{
					WorkingUnit target2 = list2[this.AI.RNG.Next(list2.Count)];
					bool ranged2 = defender.RangedAttack > 0;
					CombatResults combatResults2 = CombatManager.PerformCombat(defender, target2, CombatType.Simulated, ranged2, false, false);
					if (combatResults2 != null)
					{
						int dealt2 = combatResults2.DefenderCasualties;
						if (ranged2)
						{
							dealt2 = (int)(dealt2 * 1.5f);
						}
						damageToAttacker += dealt2;
					}
				}
				string terrain = "";
				if (activePathNode.Province != null && activePathNode.GetRegion() != null && activePathNode.GetRegion().Terrain != null)
				{
					terrain = activePathNode.GetRegion().Terrain.BaseType.ToLowerInvariant();
				}
				float terrainAttackerPenalty = 1.0f;
				float terrainDefenderBonus = 1.0f;
				if (terrain == "hills")
				{
					terrainDefenderBonus = 1.2f;
					terrainAttackerPenalty = 0.9f;
				}
				else if (terrain == "mountain")
				{
					terrainDefenderBonus = 1.4f;
					terrainAttackerPenalty = 0.7f;
				}
				else if (terrain == "lt forest")
				{
					terrainDefenderBonus = 1.15f;
					terrainAttackerPenalty = 0.9f;
				}
				else if (terrain == "old forest")
				{
					terrainDefenderBonus = 1.25f;
					terrainAttackerPenalty = 0.85f;
				}
				else if (terrain == "swamp")
				{
					terrainDefenderBonus = 1.3f;
					terrainAttackerPenalty = 0.75f;
				}
				else if (terrain == "wasteland")
				{
					terrainDefenderBonus = 1.1f;
					terrainAttackerPenalty = 0.9f;
				}
				bool riverCrossing = false;
				try
				{
					SovereigntyTK.Game.Path attackPath = this.AI.Game.PathManager.GetPath(Stack.Node, activePathNode, list2, true, this.AI.Realm, false);
					if (attackPath == null || attackPath.PathPoints.Count == 0)
					{
						attackPath = this.AI.Game.PathManager.GetPath(Stack.Node, activePathNode, list2, false, this.AI.Realm, false);
					}
					riverCrossing = attackPath != null && attackPath.DoesCrossRiver();
				}
				catch
				{
					riverCrossing = false;
				}
				if (riverCrossing)
				{
					terrainAttackerPenalty *= 0.5f;
				}
				damageToDefender = (int)(damageToDefender * terrainAttackerPenalty);
				damageToAttacker = (int)(damageToAttacker * terrainDefenderBonus);
				damageToDefender = Math.Max(1, damageToDefender);
				damageToAttacker = Math.Max(1, damageToAttacker);
				num = (float)damageToDefender / (float)damageToAttacker;
				this.LogStackDebug("      Combat sim: we deal=" + damageToDefender + " we receive=" + damageToAttacker + " (attackers=" + list2.Count + " defenders=" + list.Count + " terrain=" + (terrain.Length > 0 ? terrain : "none") + ", riverCrossing=" + riverCrossing + ") ratio=" + num.ToString("F2"));
			}
			float num6 = 0.75f;
			num6 -= (float)this.AI.Game.Data.AITraits[this.AI.Realm.Name].Warmonger * 0.1f;
			if (Province.OccupierRealm == this.AI.Game.RebelRealm)
			{
				num6 -= (float)this.AI.Game.Data.AITraits[this.AI.Realm.Name].Opportunist * 0.1f;
			}
			if (this.ProvinceHasActiveFortifications(Province))
			{
				num6 = Math.Max(num6, this.IsIronBarony() ? 0.85f : 0.5f);
			}
			bool result = num > num6;
			this.LogStackDebug("      Final: ratio=" + num.ToString("F2") + " threshold=" + num6.ToString("F2") + " warmonger=" + this.AI.Game.Data.AITraits[this.AI.Realm.Name].Warmonger + " -> " + (result ? "ATTACK" : "REJECT"));
			return result;
		}

		internal void HandleHeroOffer(WorkingHero Hero, int Cost)
		{
			if (this.Funds.CurrentGold < Cost)
			{
				return;
			}
			if (this.IsIronBarony())
			{
				WorkingStack frontTarget = this.FindIronBaronyHeroDeploymentTarget(true);
				if (frontTarget == null)
				{
					frontTarget = this.FindIronBaronyHeroDeploymentTarget(false);
				}
				if (frontTarget != null)
				{
					this.AI.Game.DeployHero(Hero, frontTarget.Node);
					this.Funds.CurrentGold -= Cost;
					this.LogWarGoals("  Iron Barony hero offer: deployed " + Hero.DisplayName + " directly to stack at " + (frontTarget.Node.Province != null ? frontTarget.Node.Province.Name : ("node " + frontTarget.Node.ID)) + " (" + frontTarget.Units.Count + " units)");
					return;
				}
			}
			List<WorkingStack> list = this.AI.Realm.Stacks.Where((WorkingStack x) => x.Hero == null).ToList<WorkingStack>();
			if (list.Count == 0)
			{
				return;
			}
			List<WorkingStack> list2 = list.Where((WorkingStack x) => x.Node != null && x.Node.Province != null).ToList<WorkingStack>();
			List<WorkingStack> list3 = list.Where((WorkingStack x) => x.Node != null && x.Node.Zone != null).ToList<WorkingStack>();
			if (list2.Count > 0)
			{
				WorkingStack workingStack = list2[this.AI.RNG.Next(list2.Count)];
				this.AI.Game.DeployHero(Hero, workingStack.Node);
				this.Funds.CurrentGold -= Cost;
				return;
			}
			if (list3.Count > 0)
			{
				WorkingStack workingStack2 = list3[this.AI.RNG.Next(list3.Count)];
				this.AI.Game.DeployHero(Hero, workingStack2.Node);
				this.Funds.CurrentGold -= Cost;
			}
		}

		internal void DoAttacks()
		{
			this.AI.Realm.StacksChanged();
			this.AI.Log("");
			this.AI.Log("Unit manager updating (attack phase)");
			if (this.AI.Game.AIAttacksDisabled)
			{
				this.AI.Log("  AI Attacks are disabled, aborting");
				return;
			}
			while (this.CurrentStackID < this.RealmStackIDs.Count)
			{
				WorkingStack workingStack = null;
				this.AI.Game.AllStacks.TryGetValue(this.RealmStackIDs[this.CurrentStackID], out workingStack);
				this.CurrentStackID++;
				if (workingStack != null)
				{
					this.AI.Log("  Considering attack targets for stack " + workingStack.ID);
					bool currentProvinceIsHostile = workingStack.Node.Province != null && this.RealmIsEnemy(workingStack.Node.Province.OccupierRealm);
					if (workingStack.Node.Province != null && workingStack.Node.Province.Occupied && !this.ProvinceCanServeAsArmyBase(workingStack.Node.Province) && !currentProvinceIsHostile)
					{
						this.AI.Log("    Stack is occupying " + workingStack.Node.Province.Name + " under non-hostile/invalid control, will not be used to attack");
					}
					else
					{
						Dictionary<WorkingProvince, float> dictionary = new Dictionary<WorkingProvince, float>();
						if (workingStack.Node.NodeType == PathNodeTypes.Harbour)
						{
							if (this.RealmIsEnemy(workingStack.Node.Province.OccupierRealm))
							{
								dictionary.Add(workingStack.Node.Province, this.GetTargetValue(workingStack.Node.Province, false));
							}
						}
						else
						{
							WorkingProvince currentProvince = workingStack.Node.Province;
							if (currentProvince != null && this.RealmIsEnemy(currentProvince.OccupierRealm) && !dictionary.ContainsKey(currentProvince))
							{
								dictionary.Add(currentProvince, this.GetTargetValue(currentProvince, workingStack.Node.GetRegion() is WorkingZone));
							}
							foreach (GameRegion gameRegion in workingStack.Node.GetRegion().GetAllConnectedRegions())
							{
								if (!(gameRegion is WorkingZone))
								{
									WorkingProvince workingProvince = gameRegion as WorkingProvince;
									if ((workingProvince.HarbourNode == null || workingProvince.HarbourNode.CurrentStack == null || workingProvince.HarbourNode.CurrentStack.Owner != this.AI.Realm) && this.RealmIsEnemy(workingProvince.OccupierRealm) && !dictionary.ContainsKey(workingProvince))
									{
										dictionary.Add(workingProvince, this.GetTargetValue(workingProvince, workingStack.Node.GetRegion() is WorkingZone));
									}
								}
							}
						}
						foreach (KeyValuePair<WorkingProvince, float> keyValuePair in dictionary.OrderByDescending((KeyValuePair<WorkingProvince, float> x) => x.Value))
						{
							this.AI.Log("    Considering attack on " + keyValuePair.Key.Name);
							if (this.IgnoreProvinces.Contains(keyValuePair.Key.ID))
							{
								this.AI.Log("      Province is on ignore list, aborting");
							}
							else if (this.AI.Realm.CodeOfWar && keyValuePair.Key.OccupierRealm.DiplomacyManager.GetRelationTime(this.AI.Realm) == 0)
							{
								this.AI.Log("      Code of war prevents attacks this turn");
							}
							else
							{
								if (keyValuePair.Key.IsCapitol && !this.ShouldAttackCapitol(keyValuePair.Key.OwnerRealm, keyValuePair.Key.OccupierRealm))
								{
									this.AI.Log("      Capitol attack blocked, aborting");
								}
								string ironBaronyWaveHoldReason;
								if (this.ShouldIronBaronyHoldAttackForArmyWave(workingStack, keyValuePair.Key, out ironBaronyWaveHoldReason))
								{
									this.AI.Log("      Iron Barony support doctrine: holding support attack - " + ironBaronyWaveHoldReason);
								}
								else if (!this.CanWinProvince(workingStack, keyValuePair.Key, workingStack.Node.GetRegion() is WorkingZone))
								{
									this.AI.Log("      Unlikely to win fight, aborting");
								}
								else
								{
									ActivePathNode activePathNode = this.GetAttackNodeForStack(workingStack, keyValuePair.Key);
									if (activePathNode == null)
									{
										this.AI.Log("      No valid attack node for this province, aborting");
										continue;
									}
									List<WorkingUnit> list = new List<WorkingUnit>();
									foreach (WorkingUnit workingUnit in workingStack.Units)
									{
										if (this.AI.Game.DestinationChecker.NodeOKForUnit(workingUnit, activePathNode) == UnitMoveResult.OK && workingUnit.HasMoves())
										{
											if (!this.CanDoctrineGarrisonOnlyUnitMoveToNode(workingUnit, activePathNode))
											{
												this.AI.Log("      Homeland guard held back: " + workingUnit.DisplayName + " cannot leave owned provinces");
												continue;
											}
											if (!this.ShouldMoveIronBaronyCatapultToTarget(workingUnit, keyValuePair.Key))
											{
												this.AI.Log("      Holding Orcish Catapult back: target has no active fortifications");
												continue;
											}
											list.Add(workingUnit);
										}
									}
									if (list.Count < this.GetMinAttackUnits())
									{
										this.AI.Log("      Too few movable attackers (" + list.Count + ", minimum " + this.GetMinAttackUnits() + "), aborting");
									}
									else
									{
										if (workingStack.Node.Zone != null && activePathNode.Province != null && activePathNode.Province.OccupierRealm != workingStack.Owner)
										{
											WorkingStack interceptStack = this.AI.Game.GetInterceptStack(activePathNode.Province.OccupierRealm, workingStack.Node.Zone, workingStack);
											if (interceptStack != null)
											{
												this.AI.Log("      Attacking, intercepted by " + workingStack.Owner.Name);
												AIActionIntercept aiactionIntercept = this.AI.ActionManager.CreateAction<AIActionIntercept>();
												aiactionIntercept.Stack = workingStack;
												aiactionIntercept.Units = list;
												aiactionIntercept.Node = activePathNode;
												aiactionIntercept.InterceptStack = interceptStack;
												this.AI.ActionManager.AddAction(aiactionIntercept, true);
												list = new List<WorkingUnit>();
												foreach (WorkingUnit workingUnit2 in workingStack.Units)
												{
													if (this.AI.Game.DestinationChecker.NodeOKForUnit(workingUnit2, activePathNode) == UnitMoveResult.OK)
													{
														if (!this.CanDoctrineGarrisonOnlyUnitMoveToNode(workingUnit2, activePathNode))
														{
															continue;
														}
														if (!this.ShouldMoveIronBaronyCatapultToTarget(workingUnit2, keyValuePair.Key))
														{
															continue;
														}
														list.Add(workingUnit2);
													}
												}
												if (list.Count < this.GetMinAttackUnits())
												{
													this.AI.Log("      After interception, too few units left which can attack (" + list.Count + ", minimum " + this.GetMinAttackUnits() + "), aborting");
													continue;
												}
											}
										}
										this.AI.Log("      Attacking province");
										AIActionAttack aiactionAttack = this.AI.ActionManager.CreateAction<AIActionAttack>();
										aiactionAttack.Province = keyValuePair.Key;
										aiactionAttack.Stack = workingStack;
										aiactionAttack.Units = list;
										aiactionAttack.Node = activePathNode;
										aiactionAttack.Realm = keyValuePair.Key.OccupierRealm;
										this.AI.ActionManager.AddAction(aiactionAttack, true);
										if (this.AI.Disposed)
										{
											return;
										}
										break;
									}
								}
							}
						}
					}
				}
			}
		}

		private bool ShouldAttackCapitol(WorkingRealm CapitolOwner, WorkingRealm CapitolOccupier)
		{
			if (CapitolOwner == this.AI.Realm)
			{
				return true;
			}
			if (CapitolOccupier != CapitolOwner && CapitolOwner.DiplomacyManager.GetRelation(this.AI.Realm) != RelationStates.Alliance)
			{
				return false;
			}
			float num = 100f;
			if (this.AI.Realm.CodeOfWar)
			{
				num *= 0.1f;
			}
			if (this.AI.Realm.Alignment == RealmAlignments.Good && (CapitolOccupier.Alignment == RealmAlignments.Good || CapitolOccupier.Alignment == RealmAlignments.Neutral))
			{
				num *= 0.25f;
			}
			if (this.AI.Realm.Alignment == RealmAlignments.Evil && CapitolOccupier.Alignment == RealmAlignments.Evil)
			{
				num *= 0.25f;
			}
			if (this.AI.Realm.Alignment == RealmAlignments.Neutral)
			{
				num *= 0.05f;
			}
			if (this.AI.Realm.Race == CapitolOccupier.Race)
			{
				num *= 0.15f;
			}
			return (float)this.AI.RNG.Next(100) <= num;
		}

		internal void LoadLegacyStackList(BinaryReader r, int SaveVersion)
		{
			this.CurrentStackID = r.ReadInt32();
			this.RealmStackIDs = new List<int>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.RealmStackIDs.Add(r.ReadInt32());
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.CurrentStackID);
			w.Write(this.IgnoreProvinces.Count);
			foreach (int num in this.IgnoreProvinces)
			{
				w.Write(num);
			}
			if (this.RealmStackIDs == null)
			{
				w.Write(false);
			}
			else
			{
				w.Write(true);
				w.Write(this.RealmStackIDs.Count);
				foreach (int num2 in this.RealmStackIDs)
				{
					w.Write(num2);
				}
			}
			this.Funds.Save(w);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.CurrentStackID = r.ReadInt32();
			int num = r.ReadInt32();
			this.IgnoreProvinces = new List<int>();
			for (int i = 0; i < num; i++)
			{
				this.IgnoreProvinces.Add(r.ReadInt32());
			}
			if (r.ReadBoolean())
			{
				this.RealmStackIDs = new List<int>();
				num = r.ReadInt32();
				for (int j = 0; j < num; j++)
				{
					this.RealmStackIDs.Add(r.ReadInt32());
				}
			}
			this.Funds.Load(r, SaveVersion);
		}

		public int GetRequiredResource(ResourceData Resource)
		{
			int num = 0;
			if (this.PurchaseList == null)
			{
				return num;
			}
			foreach (UnitData unitData in this.PurchaseList)
			{
				num += unitData.GetRequiredResourceCount(Resource);
			}
			return num;
		}

		internal void PreparePurchaseList()
		{
			bool flag = this.AI.Realm.Enemies.Count > 1;
			this.PurchaseList = new List<UnitData>();
			List<KeyValuePair<UnitData, UnitTrainStates>> availableUnitTypes = this.AI.Realm.UnitPurchaseManager.GetAvailableUnitTypes();
			List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits = availableUnitTypes
				.Where((KeyValuePair<UnitData, UnitTrainStates> x) => x.Value == UnitTrainStates.OK || x.Value == UnitTrainStates.NoResources || x.Value == UnitTrainStates.CannotAfford)
				.ToList();
			if (this.IsIronBarony())
			{
				this.PrepareIronBaronyPurchaseList(trainableUnits);
				return;
			}
			if (this.IsMaledor())
			{
				this.PrepareMaledorPurchaseList(trainableUnits);
				return;
			}
			if (this.IsBoruvian())
			{
				this.PrepareBoruvianPurchaseList(trainableUnits);
				return;
			}
			if (this.SiegeUnitNeeded)
			{
				UnitData siegeUnit = null;
				foreach (KeyValuePair<UnitData, UnitTrainStates> kvp in trainableUnits)
				{
					if (kvp.Key.Class == UnitClasses.Siege)
					{
						siegeUnit = kvp.Key;
						break;
					}
				}
				if (siegeUnit != null)
				{
					this.PurchaseList.Add(siegeUnit);
					this.LogStackDebug("  SiegeUnitNeeded: added " + siegeUnit.Name + " to purchase list (priority)");
				}
				else
				{
					this.SiegeUnitNeeded = false;
				}
			}
			this.PrepareWeightedGeneralPurchaseList(trainableUnits, flag);
		}

		private void PrepareWeightedGeneralPurchaseList(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, bool AtWar)
		{
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			int remainingGold = this.Funds.CurrentGold - this.GetUnitDataListCost(this.PurchaseList);
			int netIncome = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			int remainingIncome = Math.Max(0, netIncome - this.GetUnitDataListUpkeep(this.PurchaseList));
			while (remainingGold > 0 && this.PurchaseList.Count < 20)
			{
				Dictionary<UnitData, int> weightedCandidates = new Dictionary<UnitData, int>();
				foreach (KeyValuePair<UnitData, UnitTrainStates> pair in trainableUnits)
				{
					UnitData unit = pair.Key;
					if (unit == null || unit.Class == UnitClasses.Siege)
					{
						continue;
					}
					int missingResourceCost = this.GetMissingResourceReplacementCost(unit, resources);
					int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unit) + missingResourceCost;
					int upkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unit);
					if (cost > remainingGold || upkeep > remainingIncome)
					{
						continue;
					}
					int existingCount = this.AI.Realm.GetUnitTypeCount(unit) + this.PurchaseList.Count((UnitData x) => x.Name == unit.Name);
					if (unit.Rank == UnitRanks.Unique && existingCount >= 1)
					{
						continue;
					}
					if (unit.Rank == UnitRanks.Elite && existingCount >= 4)
					{
						continue;
					}
					if (unit.Realm != this.AI.Realm.Name && existingCount >= 4)
					{
						continue;
					}
					int weight = AtWar ? unit.WarWeight : unit.PeaceWeight;
					if (weight <= 0)
					{
						continue;
					}
					weightedCandidates[unit] = weight;
				}
				if (weightedCandidates.Count == 0)
				{
					break;
				}
				UnitData picked = this.PickWeightedUnit(weightedCandidates);
				if (picked == null)
				{
					break;
				}
				this.PurchaseList.Add(picked);
				remainingGold -= this.AI.Realm.UnitPurchaseManager.GetUnitCost(picked) + this.GetMissingResourceReplacementCost(picked, resources);
				remainingIncome -= this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(picked);
				this.ReserveResourcesForPlannedUnit(picked, resources);
			}
			this.LogStackDebug("  Weighted draft purchase list for " + this.AI.Realm.Name + ": " + this.FormatUnitDataListByType(this.PurchaseList));
		}

		private UnitData PickWeightedUnit(Dictionary<UnitData, int> weightedCandidates)
		{
			int totalWeight = weightedCandidates.Sum((KeyValuePair<UnitData, int> x) => x.Value);
			if (totalWeight <= 0)
			{
				return null;
			}
			int roll = this.AI.RNG.Next(totalWeight);
			int running = 0;
			UnitData picked = null;
			foreach (KeyValuePair<UnitData, int> candidate in weightedCandidates)
			{
				running += candidate.Value;
				picked = candidate.Key;
				if (running > roll)
				{
					break;
				}
			}
			return picked;
		}

		private int GetMissingResourceReplacementCost(UnitData unitData, Dictionary<string, int> resources)
		{
			int missing = 0;
			foreach (KeyValuePair<string, int> required in unitData.GetRequiredResources())
			{
				int available = 0;
				resources.TryGetValue(required.Key, out available);
				int need = required.Value - available;
				if (need > 0)
				{
					missing += need;
				}
			}
			return missing * this.GetResourceReplaceCost();
		}


		private void PurchaseMaledorUnits()
		{
			List<UnitData> list = new List<UnitData>();
			if (this.PurchaseList == null || this.PurchaseList.Count == 0)
			{
				this.LogDoctrineDraft("  Maledor draft: no units requested");
				return;
			}
			int netIncome = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			if (netIncome <= 0)
			{
				this.LogDoctrineDraft("  Maledor draft blocked: projected income is not positive (" + netIncome + ")");
				return;
			}
			int remainingGold = this.Funds.CurrentGold;
			int remainingIncome = netIncome;
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			foreach (UnitData unitData in this.PurchaseList)
			{
				int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unitData);
				int upkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unitData);
				if (!this.CanPurchaseUnitTypeWithPlanned(unitData, list))
				{
					this.LogDoctrineDraft("  Maledor draft: skipping " + unitData.Name + " (limit reached)");
					continue;
				}
				if (!this.ResourcesAvailableForPlannedUnit(unitData, resources))
				{
					this.LogDoctrineDraft("  Maledor draft: delaying " + unitData.Name + " until required resources are available");
					continue;
				}
				if (cost > remainingGold)
				{
					this.LogDoctrineDraft("  Maledor draft: saving gold for " + unitData.Name + " (cost " + cost + ", unit funds " + remainingGold + ")");
					continue;
				}
				if (upkeep > remainingIncome)
				{
					this.LogDoctrineDraft("  Maledor draft: skipping " + unitData.Name + " to keep next-turn upkeep positive");
					continue;
				}
				list.Add(unitData);
				remainingGold -= cost;
				remainingIncome -= upkeep;
				this.ReserveResourcesForPlannedUnit(unitData, resources);
				this.LogDoctrineDraft("  Maledor draft: queued " + unitData.Name + " projection: remaining income " + remainingIncome + ", unit funds " + remainingGold);
			}
			this.LogDoctrineDraft("  Maledor draft queued by type: " + this.FormatUnitDataListByType(list));
			this.LogDoctrineDraft("  Maledor draft queued order: " + this.FormatUnitDataListOrder(list));
			if (list.Count > 0)
			{
				AIActionPurchaseUnits aiactionPurchaseUnits = this.AI.ActionManager.CreateAction<AIActionPurchaseUnits>();
				aiactionPurchaseUnits.UnitTypes = list;
				this.AI.ActionManager.AddAction(aiactionPurchaseUnits, true);
			}
		}

		private bool PrepareMaledorPriorityUnit(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, string unitName, int targetCount, int netIncome)
		{
			if (this.GetOwnedQueuedAndPlannedUnitCount(unitName) >= targetCount)
			{
				return false;
			}
			UnitData unit = this.FindTrainableUnitByName(trainableUnits, unitName);
			if (unit == null)
			{
				this.LogDoctrineDraft("  Maledor doctrine: " + unitName + " is not trainable this turn (" + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + ")");
				return false;
			}
			int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unit);
			int upkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unit);
			int projectedGold = this.Funds.CurrentGold - this.GetUnitDataListCost(this.PurchaseList);
			int projectedIncome = netIncome - this.GetUnitDataListUpkeep(this.PurchaseList);
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			bool resourcesReady = this.ResourcesAvailableForPlannedUnit(unit, resources);
			string marketSummary;
			bool marketCanSupply = this.ResourcesAvailableForPlannedUnitOrMarketCanSupply(unit, resources, out marketSummary);
			if (upkeep > projectedIncome)
			{
				this.LogDoctrineDraft("  Maledor doctrine: skipping " + unitName + " for upkeep safety (upkeep " + upkeep + ", projected income " + projectedIncome + ")");
				return false;
			}
			if (!resourcesReady)
			{
				if (marketCanSupply)
				{
					this.PurchaseList.Add(unit);
					this.MaledorSavingForPriorityUnit = true;
					this.MaledorSavingForUnitName = unitName;
					this.LogDoctrineDraft("  Maledor resource reservation: " + unitName + " " + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + " needs resources; market can supply (" + marketSummary + "). Draft reserved for resource purchase.");
					return true;
				}
				this.LogDoctrineDraft("  Maledor resource wait: " + unitName + " needs resources, but market cannot supply now (" + marketSummary + ")");
				return false;
			}
			if (cost > projectedGold)
			{
				this.MaledorSavingForPriorityUnit = true;
				this.MaledorSavingForUnitName = unitName;
				this.LogDoctrineDraft("  Maledor gold reservation: saving for " + unitName + " " + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + " (cost " + cost + ", projected unit funds " + projectedGold + ")");
				return true;
			}
			this.PurchaseList.Add(unit);
			this.MaledorSavingForPriorityUnit = false;
			this.MaledorSavingForUnitName = null;
			this.LogDoctrineDraft("  Maledor doctrine: planned " + unitName + " " + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + " (cost " + cost + ", upkeep " + upkeep + ")");
			return false;
		}

		private int GetMaledorDraftArmySlots()
		{
			int usable = 0;
			foreach (WorkingUnit unit in this.AI.Realm.Units)
			{
				if (unit == null || unit.Disabled || unit.Class == UnitClasses.Fort || unit.Class == UnitClasses.Naval)
				{
					continue;
				}
				if (this.GetMaledorCompositionUnitName(unit) != null)
				{
					usable++;
				}
			}
			// Ask for one field army until the first 20-stack exists, then begin filling a second/third.
			return Math.Max(1, Math.Min(3, (usable / 20) + 1));
		}

		private int GetMaledorMassDraftTarget(string unitName)
		{
			int slots = this.GetMaledorDraftArmySlots();
			switch (unitName)
			{
			case "Headhunter":
				return Math.Min(12, 6 * slots);
			case "Sallowcoil Thugee":
				return Math.Min(12, 4 * slots);
			case "Cultist":
				return Math.Min(4, 2 * slots);
			default:
				return 0;
			}
		}

		private bool AddMaledorMassUnit(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, string unitName, ref int projectedGold)
		{
			if (this.PurchaseList.Count >= 20)
			{
				return false;
			}
			UnitData unit = this.FindTrainableUnitByName(trainableUnits, unitName);
			if (unit == null)
			{
				return false;
			}
			int targetCount = this.GetMaledorMassDraftTarget(unitName);
			if (targetCount > 0 && this.GetOwnedQueuedAndPlannedUnitCount(unitName) >= targetCount)
			{
				return false;
			}
			int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unit);
			if (cost > projectedGold)
			{
				return false;
			}
			this.PurchaseList.Add(unit);
			projectedGold -= cost;
			return true;
		}

		private string GetMaledorDraftTargetSummary()
		{
			return "slots " + this.GetMaledorDraftArmySlots()
				+ ", Worm " + this.GetOwnedQueuedAndPlannedUnitCount("The Worm") + "/1"
				+ ", Justiciar " + this.GetOwnedQueuedAndPlannedUnitCount("Justiciar") + "/4"
				+ ", Inquisitor " + this.GetOwnedQueuedAndPlannedUnitCount("Inquisitor") + "/1"
				+ ", Necromancer " + this.GetOwnedQueuedAndPlannedUnitCount("Necromancer") + "/4"
				+ ", Crow Hag " + this.GetOwnedQueuedAndPlannedUnitCount("Crow Hag") + "/4"
				+ ", Thugee " + this.GetOwnedQueuedAndPlannedUnitCount("Sallowcoil Thugee") + "/" + this.GetMaledorMassDraftTarget("Sallowcoil Thugee")
				+ ", Headhunter " + this.GetOwnedQueuedAndPlannedUnitCount("Headhunter") + "/" + this.GetMaledorMassDraftTarget("Headhunter");
		}

		private void PrepareMaledorPurchaseList(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits)
		{
			int netIncome = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			this.LogDoctrineDraft("  Maledor doctrine active: income surplus " + netIncome + ", unit funds " + this.Funds.CurrentGold);
			this.MaledorSavingForPriorityUnit = false;
			this.MaledorSavingForUnitName = null;
			if (netIncome <= 0)
			{
				this.LogDoctrineDraft("  Maledor doctrine: skipped because next-turn income is not positive");
				return;
			}
			if (this.PrepareMaledorPriorityUnit(trainableUnits, "The Worm", 1, netIncome)) return;
			if (this.PrepareMaledorPriorityUnit(trainableUnits, "Justiciar", 4, netIncome)) return;
			if (this.PrepareMaledorPriorityUnit(trainableUnits, "Inquisitor", 1, netIncome)) return;
			if (this.PrepareMaledorPriorityUnit(trainableUnits, "Necromancer", 4, netIncome)) return;
			if (this.PrepareMaledorPriorityUnit(trainableUnits, "Crow Hag", 4, netIncome)) return;
			this.LogDoctrineDraft("  Maledor draft targets: " + this.GetMaledorDraftTargetSummary());
			int projectedGold = this.Funds.CurrentGold - this.GetUnitDataListCost(this.PurchaseList);
			if (projectedGold < 0)
			{
				projectedGold = 0;
			}
			bool added = true;
			string[] massCycle = new string[]
			{
				"Headhunter",
				"Sallowcoil Thugee",
				"Headhunter",
				"Headhunter",
				"Sallowcoil Thugee"
			};
			while (added && this.PurchaseList.Count < 20)
			{
				added = false;
				foreach (string massUnitName in massCycle)
				{
					if (this.PurchaseList.Count >= 20)
					{
						break;
					}
					added |= this.AddMaledorMassUnit(trainableUnits, massUnitName, ref projectedGold);
				}
			}
			if (this.PurchaseList.Count == 0 && this.GetOwnedQueuedAndPlannedUnitCount("Cultist") < this.GetMaledorMassDraftTarget("Cultist"))
			{
				this.AddMaledorMassUnit(trainableUnits, "Cultist", ref projectedGold);
			}
			int plannedCost = this.GetUnitDataListCost(this.PurchaseList);
			int plannedUpkeep = this.GetUnitDataListUpkeep(this.PurchaseList);
			this.LogDoctrineDraft("  Maledor planned list: " + this.PurchaseList.Count + " unit(s), cost " + plannedCost + ", upkeep " + plannedUpkeep + ", projected income after list " + (netIncome - plannedUpkeep) + ", projected unit funds after list " + (this.Funds.CurrentGold - plannedCost));
			this.LogDoctrineDraft("  Maledor planned by type: " + this.FormatUnitDataListByType(this.PurchaseList));
			this.LogDoctrineDraft("  Maledor planned order: " + this.FormatUnitDataListOrder(this.PurchaseList));
		}


		private void PurchaseBoruvianUnits()
		{
			List<UnitData> list = new List<UnitData>();
			if (this.PurchaseList == null || this.PurchaseList.Count == 0)
			{
				this.LogDoctrineDraft("  Boruvian draft: no units requested");
				return;
			}
			int netIncome = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			if (netIncome <= 0)
			{
				this.LogDoctrineDraft("  Boruvian draft blocked: projected income is not positive (" + netIncome + ")");
				return;
			}
			int remainingGold = this.Funds.CurrentGold;
			int remainingIncome = netIncome;
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			foreach (UnitData unitData in this.PurchaseList)
			{
				int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unitData);
				int upkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unitData);
				if (!this.CanPurchaseUnitTypeWithPlanned(unitData, list))
				{
					this.LogDoctrineDraft("  Boruvian draft: skipping " + unitData.Name + " (limit reached)");
					continue;
				}
				if (!this.ResourcesAvailableForPlannedUnit(unitData, resources))
				{
					this.LogDoctrineDraft("  Boruvian draft: delaying " + unitData.Name + " until required resources are available");
					continue;
				}
				if (cost > remainingGold)
				{
					this.LogDoctrineDraft("  Boruvian draft: saving gold for " + unitData.Name + " (cost " + cost + ", unit funds " + remainingGold + ")");
					continue;
				}
				if (upkeep > remainingIncome)
				{
					this.LogDoctrineDraft("  Boruvian draft: skipping " + unitData.Name + " to keep next-turn upkeep positive");
					continue;
				}
				list.Add(unitData);
				remainingGold -= cost;
				remainingIncome -= upkeep;
				this.ReserveResourcesForPlannedUnit(unitData, resources);
				this.LogDoctrineDraft("  Boruvian draft: queued " + unitData.Name + " projection: remaining income " + remainingIncome + ", unit funds " + remainingGold);
			}
			this.LogDoctrineDraft("  Boruvian draft queued by type: " + this.FormatUnitDataListByType(list));
			this.LogDoctrineDraft("  Boruvian draft queued order: " + this.FormatUnitDataListOrder(list));
			if (list.Count > 0)
			{
				AIActionPurchaseUnits aiactionPurchaseUnits = this.AI.ActionManager.CreateAction<AIActionPurchaseUnits>();
				aiactionPurchaseUnits.UnitTypes = list;
				this.AI.ActionManager.AddAction(aiactionPurchaseUnits, true);
			}
		}

		private bool PrepareBoruvianPriorityUnit(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, string unitName, int targetCount, int netIncome)
		{
			if (this.GetOwnedQueuedAndPlannedUnitCount(unitName) >= targetCount)
			{
				return false;
			}
			UnitData unit = this.FindTrainableUnitByName(trainableUnits, unitName);
			if (unit == null)
			{
				this.LogDoctrineDraft("  Boruvian doctrine: " + unitName + " is not trainable this turn (" + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + ")");
				return false;
			}
			int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unit);
			int upkeep = this.AI.Realm.UnitPurchaseManager.GetUnitUpkeep(unit);
			int projectedGold = this.Funds.CurrentGold - this.GetUnitDataListCost(this.PurchaseList);
			int projectedIncome = netIncome - this.GetUnitDataListUpkeep(this.PurchaseList);
			Dictionary<string, int> resources = this.AI.Realm.GetResources();
			bool resourcesReady = this.ResourcesAvailableForPlannedUnit(unit, resources);
			string marketSummary;
			bool marketCanSupply = this.ResourcesAvailableForPlannedUnitOrMarketCanSupply(unit, resources, out marketSummary);
			if (upkeep > projectedIncome)
			{
				this.LogDoctrineDraft("  Boruvian doctrine: skipping " + unitName + " for upkeep safety (upkeep " + upkeep + ", projected income " + projectedIncome + ")");
				return false;
			}
			if (!resourcesReady)
			{
				if (marketCanSupply)
				{
					this.PurchaseList.Add(unit);
					this.BoruvianSavingForPriorityUnit = true;
					this.BoruvianSavingForUnitName = unitName;
					this.LogDoctrineDraft("  Boruvian resource reservation: " + unitName + " " + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + " needs resources; market can supply (" + marketSummary + "). Draft reserved for resource purchase.");
					return true;
				}
				this.LogDoctrineDraft("  Boruvian resource wait: " + unitName + " needs resources, but market cannot supply now (" + marketSummary + ")");
				return false;
			}
			if (cost > projectedGold)
			{
				this.BoruvianSavingForPriorityUnit = true;
				this.BoruvianSavingForUnitName = unitName;
				this.LogDoctrineDraft("  Boruvian gold reservation: saving for " + unitName + " " + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + " (cost " + cost + ", projected unit funds " + projectedGold + ")");
				return true;
			}
			this.PurchaseList.Add(unit);
			this.BoruvianSavingForPriorityUnit = false;
			this.BoruvianSavingForUnitName = null;
			this.LogDoctrineDraft("  Boruvian doctrine: planned " + unitName + " " + this.GetOwnedQueuedAndPlannedUnitCount(unitName) + "/" + targetCount + " (cost " + cost + ", upkeep " + upkeep + ")");
			return false;
		}

		private int GetBoruvianDraftArmySlots()
		{
			int usable = 0;
			foreach (WorkingUnit unit in this.AI.Realm.Units)
			{
				if (unit == null || unit.Disabled || unit.Class == UnitClasses.Fort || unit.Class == UnitClasses.Naval)
				{
					continue;
				}
				string unitName = (unit.BaseType != null) ? unit.BaseType.Name : unit.DisplayName;
				if (unitName == "Golden Infantry" || unitName == "Royal Pikes" || unitName == "Mercenary Crossbows" || unitName == "Light Brigade" || unitName == "Hussars")
				{
					usable++;
				}
			}
			return Math.Max(1, Math.Min(4, (usable / 20) + 1));
		}

		private int GetBoruvianDraftTarget(string unitName)
		{
			int slots = this.GetBoruvianDraftArmySlots();
			switch (unitName)
			{
			case "King's Retinue":
				return 1;
			case "Hussars":
				return 4;
			case "Light Brigade":
				return Math.Min(8, 2 * slots);
			case "Golden Infantry":
				return 6 * slots;
			case "Royal Pikes":
				return Math.Min(20, 4 * slots);
			case "Mercenary Crossbows":
				return Math.Min(20, 5 * slots);
			case "Imperial Garrison":
				return 20;
			default:
				return 0;
			}
		}

		private int GetBoruvianGoldenInfantryDraftLimit(int netIncome)
		{
			// Golden Infantry is Boruvia's army backbone, but production should scale
			// with actual net income after upkeep instead of always drafting at full speed.
			// No middle-low tier is used for now: below 400 net income Boruvia saves money.
			if (netIncome >= 1000) return 6;
			if (netIncome >= 700) return 4;
			if (netIncome >= 400) return 2;
			return 0;
		}

		private int AddBoruvianMassUnits(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits, string unitName, int maxToAdd, ref int projectedGold)
		{
			int added = 0;
			while (added < maxToAdd && this.PurchaseList.Count < 20)
			{
				UnitData unit = this.FindTrainableUnitByName(trainableUnits, unitName);
				if (unit == null)
				{
					break;
				}
				int targetCount = this.GetBoruvianDraftTarget(unitName);
				if (targetCount > 0 && this.GetOwnedQueuedAndPlannedUnitCount(unitName) >= targetCount)
				{
					break;
				}
				int cost = this.AI.Realm.UnitPurchaseManager.GetUnitCost(unit);
				if (cost > projectedGold)
				{
					break;
				}
				this.PurchaseList.Add(unit);
				projectedGold -= cost;
				added++;
			}
			return added;
		}

		private string GetBoruvianDraftTargetSummary()
		{
			return "slots " + this.GetBoruvianDraftArmySlots()
				+ ", Retinue " + this.GetOwnedQueuedAndPlannedUnitCount("King's Retinue") + "/1"
				+ ", Hussars " + this.GetOwnedQueuedAndPlannedUnitCount("Hussars") + "/4"
				+ ", Light Brigade " + this.GetOwnedQueuedAndPlannedUnitCount("Light Brigade") + "/" + this.GetBoruvianDraftTarget("Light Brigade")
				+ ", Golden Infantry " + this.GetOwnedQueuedAndPlannedUnitCount("Golden Infantry") + "/" + this.GetBoruvianDraftTarget("Golden Infantry")
				+ ", Royal Pikes " + this.GetOwnedQueuedAndPlannedUnitCount("Royal Pikes") + "/" + this.GetBoruvianDraftTarget("Royal Pikes")
				+ ", Crossbows " + this.GetOwnedQueuedAndPlannedUnitCount("Mercenary Crossbows") + "/" + this.GetBoruvianDraftTarget("Mercenary Crossbows")
				+ ", Garrison " + this.GetOwnedQueuedAndPlannedUnitCount("Imperial Garrison") + "/20 emergency";
		}

		private bool BoruvianNeedsEmergencyGarrison()
		{
			if (this.AI.Realm.Enemies == null || this.AI.Realm.Enemies.Count == 0)
			{
				return false;
			}
			return this.GetEnemyDistanceFromCapital(CapitalCautionDistance) > 0;
		}

		private void PrepareBoruvianPurchaseList(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits)
		{
			int netIncome = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			this.LogDoctrineDraft("  Boruvian doctrine active: income surplus " + netIncome + ", unit funds " + this.Funds.CurrentGold);
			this.BoruvianSavingForPriorityUnit = false;
			this.BoruvianSavingForUnitName = null;
			if (netIncome <= 0)
			{
				this.LogDoctrineDraft("  Boruvian doctrine: skipped because next-turn income is not positive");
				return;
			}
			if (this.PrepareBoruvianPriorityUnit(trainableUnits, "King's Retinue", 1, netIncome)) return;
			if (this.PrepareBoruvianPriorityUnit(trainableUnits, "Hussars", 4, netIncome)) return;
			if (this.PrepareBoruvianPriorityUnit(trainableUnits, "Light Brigade", this.GetBoruvianDraftTarget("Light Brigade"), netIncome)) return;
			if (this.PrepareBoruvianPriorityUnit(trainableUnits, "Light Brigade", this.GetBoruvianDraftTarget("Light Brigade"), netIncome)) return;
			this.LogDoctrineDraft("  Boruvian draft targets: " + this.GetBoruvianDraftTargetSummary());
			int projectedGold = this.Funds.CurrentGold - this.GetUnitDataListCost(this.PurchaseList);
			if (projectedGold < 0)
			{
				projectedGold = 0;
			}
			int goldenDraftLimit = this.GetBoruvianGoldenInfantryDraftLimit(netIncome);
			this.LogDoctrineDraft("  Boruvian Golden Infantry draft limit from net income " + netIncome + ": " + goldenDraftLimit);
			int addedGolden = this.AddBoruvianMassUnits(trainableUnits, "Golden Infantry", goldenDraftLimit, ref projectedGold);
			int addedPikes = this.AddBoruvianMassUnits(trainableUnits, "Royal Pikes", 2, ref projectedGold);
			int addedCrossbows = this.AddBoruvianMassUnits(trainableUnits, "Mercenary Crossbows", 3, ref projectedGold);
			if (this.PurchaseList.Count == 0 && this.BoruvianNeedsEmergencyGarrison() && this.GetOwnedQueuedAndPlannedUnitCount("Imperial Garrison") < this.GetBoruvianDraftTarget("Imperial Garrison"))
			{
				this.AddBoruvianMassUnits(trainableUnits, "Imperial Garrison", 4, ref projectedGold);
			}
			int plannedCost = this.GetUnitDataListCost(this.PurchaseList);
			int plannedUpkeep = this.GetUnitDataListUpkeep(this.PurchaseList);
			this.LogDoctrineDraft("  Boruvian planned list: " + this.PurchaseList.Count + " unit(s), cost " + plannedCost + ", upkeep " + plannedUpkeep + ", projected income after list " + (netIncome - plannedUpkeep) + ", projected unit funds after list " + (this.Funds.CurrentGold - plannedCost));
			this.LogDoctrineDraft("  Boruvian planned batch counts: Golden +" + addedGolden + "/" + goldenDraftLimit + ", Pikes +" + addedPikes + ", Crossbows +" + addedCrossbows + ", emergency garrison " + (this.BoruvianNeedsEmergencyGarrison() ? "possible" : "no"));
			this.LogDoctrineDraft("  Boruvian planned by type: " + this.FormatUnitDataListByType(this.PurchaseList));
			this.LogDoctrineDraft("  Boruvian planned order: " + this.FormatUnitDataListOrder(this.PurchaseList));
		}

		private void PrepareIronBaronyPurchaseList(List<KeyValuePair<UnitData, UnitTrainStates>> trainableUnits)
		{
			int netIncome = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			this.LogIronBaronyDraft("  Iron Barony OGRE/SIEGE mixed stack doctrine active: income surplus " + netIncome + ", unit funds " + this.Funds.CurrentGold);
			if (netIncome <= 0)
			{
				this.LogIronBaronyDraft("  Iron Barony mixed draft: skipped because next-turn income is not positive");
				return;
			}
			// Ogres are again a real priority: if gold/resources are nearly solveable, reserve the draft for them.
			bool reservedForOgre = this.PrepareIronBaronyOgreSavingDraft(trainableUnits, netIncome);
			if (reservedForOgre)
			{
				this.LogIronBaronyDraft("  Iron Barony mixed draft: reserved for Ogre/resource purchase; no cheaper mass units added this turn");
				return;
			}
			this.AddIronBaronyUnitIfBelowTarget(trainableUnits, "War Captain Urkai", 1);
			this.AddIronBaronyUnitIfBelowTarget(trainableUnits, "Witchdoctor", 4);
			this.AddIronBaronyUnitIfBelowTarget(trainableUnits, "Gravedigger", 4);
			int catapultTarget = this.GetIronBaronyDesiredCatapultCount();
			this.AddIronBaronyUnitIfBelowTarget(trainableUnits, "Orcish Catapult", catapultTarget);
			if (catapultTarget >= 4)
			{
				this.LogIronBaronyDraft("  Iron Barony siege doctrine: fortified war goal/frontier detected, Catapult target raised to 4");
			}
			this.LogIronBaronyDraft("  Iron Barony mixed draft targets: " + this.GetIronBaronyMassDraftTargetSummary());
			int projectedGold = this.Funds.CurrentGold - this.GetUnitDataListCost(this.PurchaseList);
			if (projectedGold < 0)
			{
				projectedGold = 0;
			}
			bool added = true;
			string[] massCycle = new string[]
			{
				"Orcish Raiders",
				"Orcish Archers",
				"Wolfriders",
				"Orcish Raiders",
				"Orcish Archers"
			};
			while (added && this.PurchaseList.Count < 20)
			{
				added = false;
				// Round-robin toward the stack caps: 6 Raiders / 6 Archers / 4 Wolfriders.
				// The old 5R/3A/2W batch front-loaded Raiders and could stop mid-cycle at
				// 10R/4A/2W when specials were already in PurchaseList. This cycle reaches
				// 6R/6A/4W cleanly before overflow units begin filling the next stack.
				foreach (string massUnitName in massCycle)
				{
					if (this.PurchaseList.Count >= 20)
					{
						break;
					}
					added |= this.AddIronBaronyMassUnit(trainableUnits, massUnitName, ref projectedGold);
				}
			}
			int preTrimCost = this.GetUnitDataListCost(this.PurchaseList);
			int preTrimUpkeep = this.GetUnitDataListUpkeep(this.PurchaseList);
			this.LogIronBaronyDraft("  Iron Barony mixed draft projection before upkeep trim: planned cost " + preTrimCost + ", planned upkeep " + preTrimUpkeep + ", projected income " + (netIncome - preTrimUpkeep) + ", projected unit funds " + (this.Funds.CurrentGold - preTrimCost) + ", mass-cycle funds left " + projectedGold);
			this.TrimIronBaronyPurchaseListForEconomy(netIncome);
			int plannedCost = this.GetUnitDataListCost(this.PurchaseList);
			int plannedUpkeep = this.GetUnitDataListUpkeep(this.PurchaseList);
			this.LogIronBaronyDraft("  Iron Barony mixed unique purchase list: " + this.PurchaseList.Count + " unit(s), projected list cost " + plannedCost + ", projected list upkeep " + plannedUpkeep + ", projected income after list " + (netIncome - plannedUpkeep) + ", projected unit funds after list " + (this.Funds.CurrentGold - plannedCost));
			this.LogIronBaronyDraft("  Iron Barony mixed unique planned by type: " + this.FormatUnitDataListByType(this.PurchaseList));
			this.LogIronBaronyDraft("  Iron Barony mixed unique planned order: " + this.FormatUnitDataListOrder(this.PurchaseList));
		}


		private int GetUnitCombatScore(UnitData unit)
		{
			int score = Math.Max(unit.Attack, unit.RangedAttack) * 2;
			score += unit.Defence;
			score += unit.Speed;
			if (unit.HealRate > 0)
			{
				score += 3;
			}
			return score;
		}

		public AIPlayer AI;

		public List<int> RealmStackIDs;

		public List<int> IgnoreProvinces;

		private int CurrentStackID;

		public AIFundData Funds;

		public List<UnitData> PurchaseList;

		internal bool SiegeUnitNeeded;
	}
}
