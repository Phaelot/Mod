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
		public AIUnitsManager(AIPlayer AI)
		{
			this.AI = AI;
			this.Funds = new AIFundData();
			this.IgnoreProvinces = new List<int>();
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
				List<NodeUnitData> list4 = new List<NodeUnitData>();
				foreach (NodeUnitData nodeUnitData in list3)
				{
					if (nodeUnitData.Units.Count < 20 && this.AI.Game.DestinationChecker.NodeOKToDeploy(unitQueueItem.Unit, this.AI.Realm, nodeUnitData.Node) == UnitMoveResult.OK)
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

		internal void MoveUnits()
		{
			this.AI.Log("");
			this.AI.Log("Unit manager updating (move phase)");
			this.AI.Log("  Not logging this for sanity reasons until new unit manager is done");
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
			List<UnitMoveData> list3 = new List<UnitMoveData>();
			foreach (WorkingUnit workingUnit in this.AI.Realm.Units)
			{
				if (workingUnit.OwnerStack != null && workingUnit.Class != UnitClasses.Fort && (workingUnit.OwnerStack.Node.Province == null || !workingUnit.OwnerStack.Node.Province.IsCapitol || workingUnit.OwnerStack.Units.IndexOf(workingUnit) >= 10) && (this.AI.Realm.CapitolProvince.Occupied || ((workingUnit.OwnerStack.Node.Province == null || workingUnit.OwnerStack.Node.Province.FortLevel <= 0 || workingUnit.OwnerStack.Units.IndexOf(workingUnit) >= 5) && (workingUnit.OwnerStack.Node.Province == null || !workingUnit.OwnerStack.Node.Province.Occupied || workingUnit.OwnerStack.Units.IndexOf(workingUnit) >= 10))))
				{
					NodeUnitData nodeUnitData = null;
					foreach (NodeUnitData nodeUnitData2 in list2.OrderByDescending((NodeUnitData x) => x.NodeValue))
					{
						if (nodeUnitData2.Units.Count < 20 && this.AI.Game.DestinationChecker.NodeOKForUnit(workingUnit, nodeUnitData2.Node) == UnitMoveResult.OK)
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
			aiactionMoveUnits.MoveTargets = list3;
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
			aiactionMoveUnits.MoveTargets = list;
			this.AI.ActionManager.AddAction(aiactionMoveUnits, true);
		}

		internal void PurchaseUnits()
		{
			this.AI.Log("");
			this.AI.Log("Unit manager updating (purchase phase)");
			this.AI.Log("  Available funds: " + this.Funds.CurrentGold);
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

		private bool CanPurchaseUnitType(UnitData Data)
		{
			return (Data.Class != UnitClasses.Naval || this.AI.Realm.HasHarbour) && (Data.Rank != UnitRanks.Unique || this.AI.Realm.GetUnitTypeCount(Data) < 1) && (Data.Rank != UnitRanks.Elite || this.AI.Realm.GetUnitTypeCount(Data) < this.AI.Realm.EliteUnitLimit) && (!(Data.Realm != this.AI.Realm.Name) || this.AI.Realm.GetUnitTypeCount(Data) < 4);
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

		internal void BeginAttacks()
		{
			this.RealmStackIDs = new List<int>();
			foreach (WorkingStack workingStack in this.AI.Realm.Stacks)
			{
				this.RealmStackIDs.Add(workingStack.ID);
			}
			this.CurrentStackID = 0;
			this.DoAttacks();
		}

		private bool RealmIsEnemy(WorkingRealm Realm)
		{
			return this.AI.Realm != Realm && (this.AI.Realm.Enemies.Contains(Realm) || this.AI.WarManager.InvasionTargets.ContainsKey(Realm.ID));
		}

		public void AddIgnoreProvince(WorkingProvince Province)
		{
			this.IgnoreProvinces.Add(Province.ID);
		}

		private float GetTargetValue(WorkingProvince Province, bool NavalAttack)
		{
			float num = 0f;
			ActivePathNode activePathNode = Province.LandNode;
			if (NavalAttack && Province.HarbourNode != null)
			{
				activePathNode = Province.HarbourNode;
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
			if (Province.IsCapitol && Province.OwnerRealm != this.AI.Realm && !this.AI.IgnoreCapitolLust)
			{
				num -= 40f;
			}
			return num;
		}

		private bool CanWinProvince(WorkingStack Stack, WorkingProvince Province, bool NavalAttack)
		{
			ActivePathNode activePathNode = Province.LandNode;
			if (NavalAttack && Province.HarbourNode != null)
			{
				activePathNode = Province.HarbourNode;
			}
			if (Province.OwnerRealm == this.AI.Game.RebelRealm && this.AI.IgnoreRebels > 0)
			{
				return false;
			}
			if (this.AI.Realm.Restrictions.IgnoreProvinces.Contains(Province.Name))
			{
				return false;
			}
			float num;
			if (activePathNode.CurrentStack == null)
			{
				num = 100f;
			}
			else
			{
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
				List<WorkingUnit> list2 = new List<WorkingUnit>();
				foreach (WorkingUnit workingUnit2 in Stack.Units)
				{
					if (this.AI.Game.DestinationChecker.NodeOKForUnit(workingUnit2, activePathNode) == UnitMoveResult.OK)
					{
						list2.Add(workingUnit2);
					}
				}
				if (list2.Count == 0)
				{
					return false;
				}
				if (list.Count == 0)
				{
					return true;
				}
				int num4 = 0;
				int num5 = 0;
				foreach (WorkingUnit workingUnit3 in list2)
				{
					bool flag = workingUnit3.RangedAttack > 0;
					WorkingUnit workingUnit4 = list[this.AI.RNG.Next(list.Count)];
					CombatResults combatResults = CombatManager.PerformCombat(workingUnit3, workingUnit4, CombatType.Simulated, flag, true, false);
					if (combatResults != null)
					{
						num4 += combatResults.AttackerCasualties;
						num5 += combatResults.DefenderCasualties;
					}
				}
				num = (float)num5 / (float)num4;
			}
			float num6 = 0.75f;
			num6 -= (float)this.AI.Game.Data.AITraits[this.AI.Realm.Name].Warmonger * 0.1f;
			if (Province.OccupierRealm == this.AI.Game.RebelRealm)
			{
				num6 -= (float)this.AI.Game.Data.AITraits[this.AI.Realm.Name].Opportunist * 0.1f;
			}
			return num > num6;
		}

		internal void HandleHeroOffer(WorkingHero Hero, int Cost)
		{
			if (this.Funds.CurrentGold < Cost)
			{
				return;
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
				WorkingStack workingStack2 = list3[this.AI.RNG.Next(list2.Count)];
				this.AI.Game.DeployHero(Hero, workingStack2.Node);
				this.Funds.CurrentGold -= Cost;
			}
		}

		internal void DoAttacks()
		{
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
					if (workingStack.Node.Province != null && workingStack.Node.Province.Occupied)
					{
						this.AI.Log("    Stack is occupying " + workingStack.Node.Province.Name + ", will not be used to attack");
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
								if (!this.CanWinProvince(workingStack, keyValuePair.Key, workingStack.Node.GetRegion() is WorkingZone))
								{
									this.AI.Log("      Unlikely to win fight, aborting");
								}
								else
								{
									ActivePathNode activePathNode = keyValuePair.Key.LandNode;
									if (workingStack.Node.GetRegion() is WorkingZone && keyValuePair.Key.HarbourNode != null)
									{
										activePathNode = keyValuePair.Key.HarbourNode;
									}
									List<WorkingUnit> list = new List<WorkingUnit>();
									foreach (WorkingUnit workingUnit in workingStack.Units)
									{
										if (this.AI.Game.DestinationChecker.NodeOKForUnit(workingUnit, activePathNode) == UnitMoveResult.OK && workingUnit.HasMoves())
										{
											list.Add(workingUnit);
										}
									}
									if (list.Count == 0)
									{
										this.AI.Log("      No units in army can move here, aborting");
									}
									else
									{
										if ((activePathNode.NodeType == PathNodeTypes.Harbour || activePathNode.NodeType == PathNodeTypes.RiverHarbour) && workingStack.Node.GetRegion() is WorkingZone && activePathNode.Province.OccupierRealm != workingStack.Owner)
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
														list.Add(workingUnit2);
													}
												}
												if (list.Count == 0)
												{
													this.AI.Log("      After interception, no units left which can attack, aborting");
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
			while (this.PurchaseList.Count < 20)
			{
				Dictionary<UnitData, int> dictionary = new Dictionary<UnitData, int>();
				using (IEnumerator<KeyValuePair<UnitData, UnitTrainStates>> enumerator = availableUnitTypes.Where((KeyValuePair<UnitData, UnitTrainStates> x) => x.Value == UnitTrainStates.OK || x.Value == UnitTrainStates.NoResources || x.Value == UnitTrainStates.CannotAfford).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<UnitData, UnitTrainStates> Pair = enumerator.Current;
						KeyValuePair<UnitData, UnitTrainStates> pair = Pair;
						if (pair.Key.Rank == UnitRanks.Elite)
						{
							WorkingRealm realm = this.AI.Realm;
							KeyValuePair<UnitData, UnitTrainStates> pair2 = Pair;
							int num = realm.GetUnitTypeCount(pair2.Key) + this.PurchaseList.Count(delegate(UnitData x)
							{
								string name = x.Name;
								KeyValuePair<UnitData, UnitTrainStates> pair11 = Pair;
								return name == pair11.Key.Name;
							});
							if (num >= 4)
							{
								continue;
							}
						}
						KeyValuePair<UnitData, UnitTrainStates> pair3 = Pair;
						if (pair3.Key.Rank == UnitRanks.Unique)
						{
							WorkingRealm realm2 = this.AI.Realm;
							KeyValuePair<UnitData, UnitTrainStates> pair4 = Pair;
							int num2 = realm2.GetUnitTypeCount(pair4.Key) + this.PurchaseList.Count(delegate(UnitData x)
							{
								string name2 = x.Name;
								KeyValuePair<UnitData, UnitTrainStates> pair12 = Pair;
								return name2 == pair12.Key.Name;
							});
							if (num2 >= 1)
							{
								continue;
							}
						}
						KeyValuePair<UnitData, UnitTrainStates> pair5 = Pair;
						if (pair5.Key.Realm != this.AI.Realm.Name)
						{
							WorkingRealm realm3 = this.AI.Realm;
							KeyValuePair<UnitData, UnitTrainStates> pair6 = Pair;
							int num3 = realm3.GetUnitTypeCount(pair6.Key) + this.PurchaseList.Count(delegate(UnitData x)
							{
								string name3 = x.Name;
								KeyValuePair<UnitData, UnitTrainStates> pair13 = Pair;
								return name3 == pair13.Key.Name;
							});
							if (num3 >= 4)
							{
								continue;
							}
						}
						if (flag)
						{
							Dictionary<UnitData, int> dictionary2 = dictionary;
							KeyValuePair<UnitData, UnitTrainStates> pair7 = Pair;
							UnitData key = pair7.Key;
							KeyValuePair<UnitData, UnitTrainStates> pair8 = Pair;
							dictionary2.Add(key, pair8.Key.WarWeight);
						}
						else
						{
							Dictionary<UnitData, int> dictionary3 = dictionary;
							KeyValuePair<UnitData, UnitTrainStates> pair9 = Pair;
							UnitData key2 = pair9.Key;
							KeyValuePair<UnitData, UnitTrainStates> pair10 = Pair;
							dictionary3.Add(key2, pair10.Key.PeaceWeight);
						}
					}
				}
				if (dictionary.Count == 0)
				{
					return;
				}
				int num4 = dictionary.Sum((KeyValuePair<UnitData, int> x) => x.Value);
				int num5 = this.AI.RNG.Next(num4);
				int num6 = 0;
				UnitData unitData = null;
				foreach (KeyValuePair<UnitData, int> keyValuePair in dictionary)
				{
					num6 += keyValuePair.Value;
					unitData = keyValuePair.Key;
					if (num6 >= num5)
					{
						break;
					}
				}
				if (unitData == null)
				{
					return;
				}
				this.PurchaseList.Add(unitData);
			}
		}

		public AIPlayer AI;

		public List<int> RealmStackIDs;

		public List<int> IgnoreProvinces;

		private int CurrentStackID;

		public AIFundData Funds;

		public List<UnitData> PurchaseList;
	}
}
