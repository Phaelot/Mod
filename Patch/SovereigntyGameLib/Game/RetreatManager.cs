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


		private void LogRetreatDebug(string Text)
		{
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "retreat_debug.txt");
				string realmName = (this.Stack == null || this.Stack.Owner == null) ? "unknown" : this.Stack.Owner.Name;
				System.IO.File.AppendAllText(file, System.DateTime.Now.ToString("HH:mm:ss.fff") + " [" + realmName + "] " + Text + "\r\n");
			}
			catch
			{
			}
		}

		private bool IsAlliance(WorkingRealm RealmA, WorkingRealm RealmB)
		{
			return RealmA != null && RealmB != null && RealmA != RealmB && RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.Alliance;
		}

		private string GetNodeText(ActivePathNode Node)
		{
			if (Node == null)
			{
				return "null";
			}
			string text = "node=" + Node.ID;
			if (Node.Province != null)
			{
				text += " province=" + Node.Province.Name;
			}
			if (Node.Zone != null)
			{
				text += " zone=" + Node.Zone.Name;
			}
			return text;
		}

		private string GetStackText(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return "null";
			}
			string text = "stack=" + Stack.ID + " units=" + Stack.Units.Count;
			if (Stack.Node != null)
			{
				text += " node=" + Stack.Node.ID;
			}
			if (Stack.Hero != null)
			{
				text += " hero=yes";
			}
			return text;
		}

		private string GetUnitDebugName(WorkingUnit Unit)
		{
			if (Unit == null)
			{
				return "null";
			}
			if (!string.IsNullOrEmpty(Unit.DisplayName))
			{
				return Unit.DisplayName;
			}
			if (!string.IsNullOrEmpty(Unit.BaseName))
			{
				return Unit.BaseName;
			}
			return "unit#" + Unit.ID.ToString();
		}

		private bool CanRetreatOccupyOrShareNode(WorkingRealm Realm, ActivePathNode Node)
		{
			if (Realm == null || Node == null)
			{
				return false;
			}
			if (Node.CurrentStack != null && !Node.CurrentStack.Disposed && Node.CurrentStack.Units.Count > 0 && Node.CurrentStack.Owner != Realm && !this.IsAlliance(Realm, Node.CurrentStack.Owner))
			{
				return false;
			}
			if (Node.Province == null)
			{
				return true;
			}
			if (Node.Province.OwnerRealm == Realm || Node.Province.OccupierRealm == Realm)
			{
				return true;
			}
			return !Node.Province.Occupied && this.IsAlliance(Realm, Node.Province.OwnerRealm);
		}

		private bool ShouldUseAllyStacks(WorkingRealm Realm, ActivePathNode Node)
		{
			if (Realm == null || Node == null)
			{
				return false;
			}
			if (Node.CurrentStack != null && !Node.CurrentStack.Disposed && Node.CurrentStack.Units.Count > 0 && Node.CurrentStack.Owner != Realm)
			{
				return this.IsAlliance(Realm, Node.CurrentStack.Owner);
			}
			return Node.Province != null && !Node.Province.Occupied && Node.Province.OwnerRealm != Realm && this.IsAlliance(Realm, Node.Province.OwnerRealm);
		}

		public int Retreat(WorkingRealm Capturer, ActivePathNode RetreatTarget = null)
		{
			RetreatData retreatList = this.GetRetreatList(this.Stack, RetreatTarget);
			HashSet<int> assignedRetreatUnitIDs = new HashSet<int>();
			HashSet<int> successfulRetreatUnitIDs = new HashSet<int>();
			HashSet<int> blockedRetreatUnitIDs = new HashSet<int>();
			foreach (WorkingUnit workingUnit in retreatList.RetreatTargets.Keys)
			{
				assignedRetreatUnitIDs.Add(workingUnit.ID);
			}
			this.LogRetreatDebug("RETREAT START: source=" + this.GetStackText(this.Stack) + " capturer=" + ((Capturer == null) ? "null" : (Capturer.Name + "#" + Capturer.ID.ToString())) + " assignedTargets=" + retreatList.RetreatTargets.Count.ToString());
			int num = 0;
			foreach (KeyValuePair<WorkingUnit, ActivePathNode> keyValuePair in retreatList.RetreatTargets)
			{
				WorkingStack workingStack = keyValuePair.Value.GetRealmStack(this.Stack.Owner);
				if (workingStack == null || workingStack.Disposed)
				{
					if (!this.CanRetreatOccupyOrShareNode(keyValuePair.Key.OwnerRealm, keyValuePair.Value))
					{
						this.LogRetreatDebug("BLOCKED illegal retreat target: unit=" + keyValuePair.Key.ID + " target=" + this.GetNodeText(keyValuePair.Value) + " current=" + this.GetStackText(keyValuePair.Value.CurrentStack));
						blockedRetreatUnitIDs.Add(keyValuePair.Key.ID);
						continue;
					}
					workingStack = this.Game.CreateStack(keyValuePair.Key.OwnerRealmID, keyValuePair.Value.ID, false);
					if (this.ShouldUseAllyStacks(keyValuePair.Key.OwnerRealm, keyValuePair.Value))
					{
						this.LogRetreatDebug("PLACEMENT DECISION: AllyStacks branch for legal allied retreat. unit=" + keyValuePair.Key.ID + " target=" + this.GetNodeText(keyValuePair.Value) + " stack=" + workingStack.ID);
						if (!keyValuePair.Value.AllyStacks.Contains(workingStack.ID))
						{
							keyValuePair.Value.AllyStacks.Add(workingStack.ID);
						}
					}
					else
					{
						this.LogRetreatDebug("PLACEMENT DECISION: CurrentStack branch for retreat. unit=" + keyValuePair.Key.ID + " target=" + this.GetNodeText(keyValuePair.Value) + " currentStackBefore=" + ((keyValuePair.Value.CurrentStack == null) ? "null" : keyValuePair.Value.CurrentStack.ID.ToString()) + " stack=" + workingStack.ID);
						keyValuePair.Value.CurrentStackID = workingStack.ID;
					}
				}
				List<WorkingUnit> list = new List<WorkingUnit>();
				list.Add(keyValuePair.Key);
				Path path = this.Game.PathManager.GetPath(keyValuePair.Key.OwnerStack.Node, keyValuePair.Value, list, false, keyValuePair.Key.OwnerRealm, false);
				workingStack.TransferFromStack(keyValuePair.Key, path, false);
				successfulRetreatUnitIDs.Add(keyValuePair.Key.ID);
				num++;
				if (retreatList.HeroRetreatTarget == keyValuePair.Value && this.Stack.Hero != null)
				{
					workingStack.TransferHeroFromStack(this.Stack, this.Stack.Hero);
				}
			}
			if (Capturer != null)
			{
				this.LogRetreatDebug("RETREAT CAPTURE CLEANUP START: capturer=" + Capturer.Name + "#" + Capturer.ID.ToString() + " sourceRemaining=" + this.Stack.Units.Count.ToString() + " prisonTotalBefore=" + Capturer.Prison.PrisonerCount.ToString());
				foreach (WorkingUnit workingUnit in this.Stack.Units.ToList<WorkingUnit>())
				{
					string retreatStatus = "no-retreat-target";
					if (successfulRetreatUnitIDs.Contains(workingUnit.ID))
					{
						retreatStatus = "successfully-retreated-but-still-in-source-WARNING";
					}
					else if (blockedRetreatUnitIDs.Contains(workingUnit.ID))
					{
						retreatStatus = "blocked-illegal-target";
					}
					else if (assignedRetreatUnitIDs.Contains(workingUnit.ID))
					{
						retreatStatus = "assigned-target-but-not-moved";
					}
					if (!workingUnit.Disabled)
					{
						if (workingUnit.Class == UnitClasses.Naval && workingUnit.CarriedUnit == null)
						{
							this.LogRetreatDebug("RETREAT CAPTURE CLEANUP: unit=" + workingUnit.ID + " name=" + this.GetUnitDebugName(workingUnit) + " status=" + retreatStatus + " empty naval -> destroyed, not captured");
							workingUnit.ApplyRealDamage(100, DamageTypes.None, false, null, null);
						}
						else
						{
							WorkingUnit workingUnit2 = workingUnit;
							WorkingUnit carriedUnit = workingUnit.CarriedUnit;
							if (carriedUnit != null)
							{
								this.LogRetreatDebug("RETREAT CAPTURE CLEANUP: transport=" + workingUnit.ID + " name=" + this.GetUnitDebugName(workingUnit) + " status=" + retreatStatus + " carriedUnit=" + carriedUnit.ID + " -> transport destroyed, carried unit captured");
								workingUnit2.ApplyRealDamage(100, DamageTypes.None, false, null, null);
								workingUnit2 = carriedUnit;
							}
							int beforeTotal = Capturer.Prison.PrisonerCount;
							int beforeOwner = (workingUnit2.OwnerRealm == null) ? -1 : Capturer.Prison.GetRealmPrisoners(workingUnit2.OwnerRealm).Count;
							bool beforeContains = Capturer.Prison.AllPrisoners.Contains(workingUnit2);
							this.LogRetreatDebug("RETREAT CAPTURE CALL: unit=" + workingUnit2.ID + " name=" + this.GetUnitDebugName(workingUnit2) + " originalStackUnit=" + workingUnit.ID + " status=" + retreatStatus + " capturer=" + Capturer.Name + " prisonTotalBefore=" + beforeTotal.ToString() + " ownerBucketBefore=" + beforeOwner.ToString() + " containsBefore=" + beforeContains.ToString());
							Capturer.Prison.CaptureUnit(workingUnit2);
							int afterTotal = Capturer.Prison.PrisonerCount;
							int afterOwner = (workingUnit2.OwnerRealm == null) ? -1 : Capturer.Prison.GetRealmPrisoners(workingUnit2.OwnerRealm).Count;
							bool afterContains = Capturer.Prison.AllPrisoners.Contains(workingUnit2);
							this.LogRetreatDebug("RETREAT CAPTURE RESULT: unit=" + workingUnit2.ID + " name=" + this.GetUnitDebugName(workingUnit2) + " status=" + retreatStatus + " capturer=" + Capturer.Name + " prisonTotal " + beforeTotal.ToString() + "->" + afterTotal.ToString() + " ownerBucket " + beforeOwner.ToString() + "->" + afterOwner.ToString() + " contains " + beforeContains.ToString() + "->" + afterContains.ToString() + " prisonerFlag=" + workingUnit2.IsPrisoner.ToString());
							if (!afterContains || !workingUnit2.IsPrisoner)
							{
								this.LogRetreatDebug("RETREAT CAPTURE ERROR: unit was not found in capturer prison after CaptureUnit. unit=" + workingUnit2.ID + " capturer=" + Capturer.Name);
							}
							this.Stack.RemoveUnit(workingUnit2);
							this.LogRetreatDebug("RETREAT CAPTURE REMOVE SOURCE: unit=" + workingUnit2.ID + " sourceRemaining=" + this.Stack.Units.Count.ToString());
						}
					}
					else
					{
						this.LogRetreatDebug("RETREAT CAPTURE CLEANUP: unit=" + workingUnit.ID + " name=" + this.GetUnitDebugName(workingUnit) + " status=" + retreatStatus + " disabled -> skipped by original logic");
					}
				}
				this.LogRetreatDebug("RETREAT CAPTURE CLEANUP END: capturer=" + Capturer.Name + "#" + Capturer.ID.ToString() + " sourceRemaining=" + this.Stack.Units.Count.ToString() + " prisonTotalAfter=" + Capturer.Prison.PrisonerCount.ToString());
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
					if (this.CanRetreatOccupyOrShareNode(Stack.Owner, Stack.Node) && Stack.Node.Province != null && this.IsAlliance(Stack.Owner, Stack.Node.Province.OwnerRealm) && !Stack.Node.Province.Occupied && this.Game.DestinationChecker.NodeOKForUnit(workingUnit, Stack.Node) == UnitMoveResult.OK)
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
												if (this.CanRetreatOccupyOrShareNode(Stack.Owner, activePathNode) && this.Game.DestinationChecker.NodeOKForUnit(workingUnit, activePathNode) == UnitMoveResult.OK)
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
						if (activeNodeConnection.TargetNode.Province != Stack.Node.Province && this.CanRetreatOccupyOrShareNode(Stack.Owner, activeNodeConnection.TargetNode) && this.Game.DestinationChecker.NodeOKForUnit(workingUnit, activeNodeConnection.TargetNode) == UnitMoveResult.OK)
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
