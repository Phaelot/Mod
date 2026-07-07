using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class RetreatManager
	{
		public RetreatManager(SovereigntyGame Game, WorkingStack Stack)
		{
			this.Game = Game;
			this.Stack = Stack;
		}

		public int Retreat(WorkingRealm Capturer, ActivePathNode RetreatTarget = null)
		{
			RetreatData retreatList = this.GetRetreatList(this.Stack, RetreatTarget);
			int num = 0;
			foreach (KeyValuePair<WorkingUnit, ActivePathNode> keyValuePair in retreatList.RetreatTargets)
			{
				WorkingStack workingStack = keyValuePair.Value.GetRealmStack(this.Stack.Owner);
				if (workingStack == null)
				{
					workingStack = this.Game.CreateStack(keyValuePair.Key.OwnerRealmID, keyValuePair.Value.ID, false);
					if (keyValuePair.Value.Province != null && !keyValuePair.Value.Province.Occupied && keyValuePair.Value.Province.OwnerRealm != keyValuePair.Key.OwnerRealm)
					{
						keyValuePair.Value.AllyStacks.Add(workingStack.ID);
					}
					else
					{
						keyValuePair.Value.CurrentStackID = workingStack.ID;
					}
				}
				List<WorkingUnit> list = new List<WorkingUnit>();
				list.Add(keyValuePair.Key);
				Path path = this.Game.PathManager.GetPath(keyValuePair.Key.OwnerStack.Node, keyValuePair.Value, list, false, keyValuePair.Key.OwnerRealm, false);
				workingStack.TransferFromStack(keyValuePair.Key, path, false);
				num++;
				if (retreatList.HeroRetreatTarget == keyValuePair.Value && this.Stack.Hero != null)
				{
					workingStack.TransferHeroFromStack(this.Stack, this.Stack.Hero);
				}
			}
			if (Capturer != null)
			{
				foreach (WorkingUnit workingUnit in this.Stack.Units.ToList<WorkingUnit>())
				{
					if (!workingUnit.Disabled)
					{
						if (workingUnit.Class == UnitClasses.Naval && workingUnit.CarriedUnit == null)
						{
							workingUnit.ApplyRealDamage(100, DamageTypes.None, false, null, null);
						}
						else
						{
							WorkingUnit workingUnit2 = workingUnit;
							WorkingUnit carriedUnit = workingUnit.CarriedUnit;
							if (carriedUnit != null)
							{
								workingUnit2.ApplyRealDamage(100, DamageTypes.None, false, null, null);
								workingUnit2 = carriedUnit;
							}
							Capturer.Prison.CaptureUnit(workingUnit2);
							this.Stack.RemoveUnit(workingUnit2);
						}
					}
				}
			}
			return num;
		}

		public RetreatData GetRetreatList(WorkingStack Stack, ActivePathNode RetreatTarget)
		{
			RetreatData retreatData = new RetreatData();
			Dictionary<ActivePathNode, int> dictionary = new Dictionary<ActivePathNode, int>();
			foreach (WorkingUnit workingUnit in new List<WorkingUnit>(Stack.Units))
			{
				if (!workingUnit.Disabled && workingUnit.GetRetreatStatus(Stack.Node.Province) && workingUnit.Class != UnitClasses.Fort)
				{
					List<ActivePathNode> list = new List<ActivePathNode>();
					if (Stack.Node.Province != null && Stack.Owner.DiplomacyManager.GetRelation(Stack.Node.Province.OwnerRealm) == RelationStates.Alliance && !Stack.Node.Province.Occupied && this.Game.DestinationChecker.NodeOKForUnit(workingUnit, Stack.Node) == UnitMoveResult.OK)
					{
						list.Add(Stack.Node);
					}
					foreach (ActiveNodeConnection activeNodeConnection in Stack.Node.ConnectedNodes)
					{
						if (activeNodeConnection.TargetNode.NodeType == PathNodeTypes.Harbour || activeNodeConnection.TargetNode.NodeType == PathNodeTypes.RiverHarbour)
						{
							if (Stack.Node.NodeType != PathNodeTypes.Land)
							{
								continue;
							}
							using (List<ActiveNodeConnection>.Enumerator enumerator3 = activeNodeConnection.TargetNode.ConnectedNodes.GetEnumerator())
							{
								while (enumerator3.MoveNext())
								{
									ActiveNodeConnection activeNodeConnection2 = enumerator3.Current;
									if (activeNodeConnection2.TargetNode.Zone != null)
									{
										using (IEnumerator<ActivePathNode> enumerator4 = activeNodeConnection2.TargetNode.Zone.Nodes.GetEnumerator())
										{
											while (enumerator4.MoveNext())
											{
												ActivePathNode activePathNode = enumerator4.Current;
												if ((activePathNode.CurrentStack == null || activePathNode.CurrentStack.Owner == Stack.Owner) && this.Game.DestinationChecker.NodeOKForUnit(workingUnit, activePathNode) == UnitMoveResult.OK)
												{
													list.Add(activePathNode);
												}
											}
											break;
										}
									}
								}
								continue;
							}
						}
						if (activeNodeConnection.TargetNode.Province != Stack.Node.Province && this.Game.DestinationChecker.NodeOKForUnit(workingUnit, activeNodeConnection.TargetNode) == UnitMoveResult.OK && (activeNodeConnection.TargetNode.Province == null || activeNodeConnection.TargetNode.Province.OccupierRealm == Stack.Owner || (activeNodeConnection.TargetNode.Province.OwnerRealm.DiplomacyManager.GetRelation(Stack.Owner) == RelationStates.Alliance && !activeNodeConnection.TargetNode.Province.Occupied)))
						{
							list.Add(activeNodeConnection.TargetNode);
						}
					}
					if (RetreatTarget != null && list.Contains(RetreatTarget))
					{
						list.Remove(RetreatTarget);
						list.Insert(0, RetreatTarget);
					}
					if (retreatData.HeroRetreatTarget == null)
					{
						list = list.OrderBy(delegate(ActivePathNode x)
						{
							if (x.CurrentStack != null && x.CurrentStack.Hero != null)
							{
								return 1;
							}
							return 0;
						}).ToList<ActivePathNode>();
						if (RetreatTarget != null && (RetreatTarget.CurrentStack == null || RetreatTarget.CurrentStack.Hero == null) && list.Contains(RetreatTarget))
						{
							list.Remove(RetreatTarget);
							list.Insert(0, RetreatTarget);
						}
					}
					foreach (ActivePathNode activePathNode2 in list)
					{
						WorkingStack realmStack = activePathNode2.GetRealmStack(Stack.Owner);
						if (realmStack == null)
						{
							if (!dictionary.ContainsKey(activePathNode2))
							{
								dictionary[activePathNode2] = 0;
							}
						}
						else if (!dictionary.ContainsKey(activePathNode2))
						{
							dictionary[activePathNode2] = realmStack.Units.Count;
						}
						if (dictionary[activePathNode2] < 20)
						{
							Dictionary<ActivePathNode, int> dictionary2;
							ActivePathNode activePathNode3;
							(dictionary2 = dictionary)[activePathNode3 = activePathNode2] = dictionary2[activePathNode3] + 1;
							retreatData.RetreatTargets.Add(workingUnit, activePathNode2);
							if (Stack.Hero != null && retreatData.HeroRetreatTarget == null && (realmStack == null || realmStack.Hero == null))
							{
								retreatData.HeroRetreatTarget = activePathNode2;
								break;
							}
							break;
						}
					}
				}
			}
			return retreatData;
		}

		private WorkingStack Stack;

		private SovereigntyGame Game;
	}
}
