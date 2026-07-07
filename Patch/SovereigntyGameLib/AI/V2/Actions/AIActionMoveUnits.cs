using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionMoveUnits : AIAction
	{
		public AIActionMoveUnits(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
		}

		private void HandleBattleEnd(string EventName, params object[] Args)
		{
			this.State = AiActionStates.Finished;
		}

		private void LogMoveTrace(string Text)
		{
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "ai_move_trace.txt");
				System.IO.File.AppendAllText(file, System.DateTime.Now.ToString("HH:mm:ss.fff") + " [" + this.AI.Realm.Name + "] [" + this.DebugSource + "] " + Text + "\r\n");
			}
			catch
			{
			}
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

		private string GetPathText(UnitMoveData MoveData)
		{
			try
			{
				if (MoveData == null || MoveData.MovePath == null || MoveData.MovePath.PathPoints == null)
				{
					return "no path";
				}
				List<string> nodes = new List<string>();
				foreach (PathPoint point in MoveData.MovePath.PathPoints)
				{
					if (point.Node != null)
					{
						nodes.Add(point.Node.ID.ToString());
					}
				}
				return string.Join(" -> ", nodes.ToArray()) + " cost=" + MoveData.MovePath.TotalMoveCost.ToString("0.00");
			}
			catch
			{
				return "path error";
			}
		}

		private WorkingStack FindExistingRealmStackOnNode(ActivePathNode Node)
		{
			if (Node == null || this.AI == null || this.AI.Realm == null)
			{
				return null;
			}
			WorkingStack stack = Node.GetRealmStack(this.AI.Realm);
			if (stack != null && !stack.Disposed)
			{
				return stack;
			}
			if (Node.CurrentStack != null && !Node.CurrentStack.Disposed && Node.CurrentStack.Owner == this.AI.Realm)
			{
				return Node.CurrentStack;
			}
			if (Node.AllyStacks != null)
			{
				foreach (int stackID in Node.AllyStacks)
				{
					WorkingStack allyStack;
					if (this.Game.AllStacks.TryGetValue(stackID, out allyStack) && allyStack != null && !allyStack.Disposed && allyStack.Owner == this.AI.Realm && allyStack.Node == Node)
					{
						return allyStack;
					}
				}
			}
			return null;
		}

		private WorkingStack GetHeroStack(WorkingHero Hero)
		{
			if (Hero == null)
			{
				return null;
			}
			WorkingStack ownerStack = Hero.OwnerStack;
			if (ownerStack != null && !ownerStack.Disposed)
			{
				return ownerStack;
			}
			if (this.AI != null && this.AI.Realm != null)
			{
				foreach (WorkingStack stack in this.AI.Realm.Stacks)
				{
					if (stack != null && !stack.Disposed && stack.Hero == Hero)
					{
						return stack;
					}
				}
			}
			return null;
		}

		private void CleanupEmptyStack(WorkingStack Stack, string Label)
		{
			if (Stack == null || Stack.Disposed)
			{
				return;
			}
			if (Stack.Units.Count == 0 && Stack.Hero == null)
			{
				WorkingProvince province = (Stack.Node != null) ? Stack.Node.Province : null;
				this.LogMoveTrace("Removing empty " + Label + " stack " + Stack.ID);
				this.Game.RemoveStack(Stack);
				if (province != null && !province.Occupied)
				{
					this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { province });
				}
			}
		}

		private bool ExecuteHeroMove(UnitMoveData MoveData, WorkingStack TargetStack)
		{
			WorkingHero hero = MoveData.Unit as WorkingHero;
			if (hero == null)
			{
				return false;
			}
			WorkingStack sourceStack = this.GetHeroStack(hero);
			this.LogMoveTrace("Hero move detected: hero=" + hero.DisplayName + " heroID=" + hero.ID + " source=" + this.GetStackText(sourceStack) + " target=" + this.GetStackText(TargetStack));
			if (sourceStack == null || sourceStack.Node == null)
			{
				this.LogMoveTrace("Hero move skipped: source stack is missing or has no node. Hero.OwnerStackID=" + hero.OwnerStackID);
				hero.ClearMoves();
				return true;
			}
			if (TargetStack == null || TargetStack.Disposed || TargetStack.Node == null)
			{
				this.LogMoveTrace("Hero move skipped: target stack is missing/disposed or has no node.");
				hero.ClearMoves();
				return true;
			}
			if (TargetStack == sourceStack)
			{
				this.LogMoveTrace("Hero move skipped: source and target are the same stack.");
				hero.ClearMoves();
				return true;
			}
			if (TargetStack.Hero != null && TargetStack.Hero != hero)
			{
				this.LogMoveTrace("Hero move skipped: target stack already has hero " + TargetStack.Hero.DisplayName + " heroID=" + TargetStack.Hero.ID);
				hero.ClearMoves();
				return true;
			}
			hero.Move(4f);
			TargetStack.TransferHeroFromStack(sourceStack, hero);
			hero.ClearMoves();
			this.LogMoveTrace("After hero move: source=" + this.GetStackText(sourceStack) + " target=" + this.GetStackText(TargetStack));
			this.CleanupEmptyStack(sourceStack, "source");
			this.CleanupEmptyStack(TargetStack, "target");
			return true;
		}

		private RelationStates GetRelationTo(WorkingRealm OtherRealm)
		{
			if (this.AI == null || this.AI.Realm == null || OtherRealm == null)
			{
				return RelationStates.Peace;
			}
			if (OtherRealm == this.AI.Realm)
			{
				return RelationStates.Alliance;
			}
			return this.AI.Realm.DiplomacyManager.GetRelation(OtherRealm);
		}

		private bool IsWarEnemy(WorkingRealm OtherRealm)
		{
			return OtherRealm != null && OtherRealm != this.AI.Realm && this.GetRelationTo(OtherRealm) == RelationStates.War;
		}

		private bool IsAllianceWith(WorkingRealm OtherRealm)
		{
			return OtherRealm != null && OtherRealm != this.AI.Realm && this.GetRelationTo(OtherRealm) == RelationStates.Alliance;
		}

		private bool IsStackListedAsAllyStack(WorkingStack Stack, ActivePathNode Node)
		{
			return Stack != null && Node != null && Node.AllyStacks != null && Node.AllyStacks.Contains(Stack.ID);
		}

		private bool IsIllegalAllyStackPlacement(WorkingStack Stack, ActivePathNode Node)
		{
			if (Stack == null || Node == null || Node.Province == null || Stack.Owner == null)
			{
				return false;
			}
			if (!this.IsStackListedAsAllyStack(Stack, Node))
			{
				return false;
			}
			WorkingRealm provinceOwner = Node.Province.OwnerRealm;
			if (provinceOwner == null || provinceOwner == Stack.Owner)
			{
				return false;
			}
			return Stack.Owner.DiplomacyManager.GetRelation(provinceOwner) != RelationStates.Alliance;
		}

		private bool NodeHasEnemyActiveFortifications(ActivePathNode Node)
		{
			if (Node == null || Node.Province == null)
			{
				return false;
			}
			WorkingRealm defenderRealm = Node.Province.OccupierRealm;
			if (defenderRealm == null)
			{
				defenderRealm = Node.Province.OwnerRealm;
			}
			if (!this.IsWarEnemy(defenderRealm))
			{
				return false;
			}
			if (Node.Province.FortLevel <= 0 || Node.Province.Forts == null)
			{
				return false;
			}
			int fortCount = Math.Min(Node.Province.FortLevel, Node.Province.Forts.Count);
			for (int i = 0; i < fortCount; i++)
			{
				WorkingUnit fort = Node.Province.Forts[i];
				if (fort != null && !fort.Disabled && (int)fort.Health > 0)
				{
					return true;
				}
			}
			return false;
		}

		private WorkingStack CreateFortDefenderStack(ActivePathNode Node)
		{
			WorkingRealm defenderRealm = Node.Province.OccupierRealm;
			if (defenderRealm == null)
			{
				defenderRealm = Node.Province.OwnerRealm;
			}
			if (defenderRealm == null)
			{
				return null;
			}
			return this.Game.CreateStack(defenderRealm.ID, Node.ID, false);
		}

		private void SelectAllUnitsInStack(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return;
			}
			foreach (WorkingUnit workingUnit in Stack.Units)
			{
				workingUnit.Selected = true;
			}
			if (Stack.Hero != null)
			{
				Stack.Hero.Selected = true;
			}
		}

		private void SelectMoveTargetsForBattle(WorkingStack SourceStack, ActivePathNode TargetNode)
		{
			if (SourceStack == null)
			{
				return;
			}
			foreach (WorkingUnit workingUnit in SourceStack.Units)
			{
				workingUnit.Selected = false;
			}
			if (SourceStack.Hero != null)
			{
				SourceStack.Hero.Selected = false;
			}
			if (this.MoveTargets == null)
			{
				return;
			}
			foreach (UnitMoveData moveData in this.MoveTargets)
			{
				if (moveData == null || moveData.Unit == null || moveData.TargetNode != TargetNode)
				{
					continue;
				}
				if (moveData.Unit is WorkingHero)
				{
					if (SourceStack.Hero == moveData.Unit)
					{
						SourceStack.Hero.Selected = true;
					}
					continue;
				}
				if (moveData.Unit.OwnerStack == SourceStack)
				{
					moveData.Unit.Selected = true;
				}
			}
		}

		private bool HasSelectedBattleUnits(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return false;
			}
			foreach (WorkingUnit workingUnit in Stack.Units)
			{
				if (workingUnit.Selected)
				{
					return true;
				}
			}
			return false;
		}

		private bool StartBattleAndStopAction(WorkingStack Attacker, WorkingStack Defender, Path AttackPath, string Reason)
		{
			if (Attacker == null || Attacker.Disposed || Attacker.Node == null || Defender == null || Defender.Disposed || Defender.Units.Count <= 0)
			{
				this.LogMoveTrace("Battle start skipped (invalid attacker/defender): reason=" + Reason + " attacker=" + this.GetStackText(Attacker) + " defender=" + this.GetStackText(Defender));
				return false;
			}
			if (!this.HasSelectedBattleUnits(Attacker))
			{
				this.LogMoveTrace("Battle start blocked movement: no selected attacking units. reason=" + Reason + " attacker=" + this.GetStackText(Attacker) + " defender=" + this.GetStackText(Defender));
				this.State = AiActionStates.Finished;
				return true;
			}
			this.LogMoveTrace("STARTING BATTLE from AIActionMoveUnits: reason=" + Reason + " attacker=" + this.GetStackText(Attacker) + " defender=" + this.GetStackText(Defender));
			this.Game.StartBattle(Attacker, Defender, AttackPath);
			if (this.Game.PendingBattle == null)
			{
				this.LogMoveTrace("StartBattle returned with PendingBattle=null; finishing movement action.");
				this.State = AiActionStates.Finished;
			}
			else
			{
				this.State = AiActionStates.Executing;
			}
			return true;
		}

		private bool TryResolveIllegalExistingAllyStack(WorkingStack ExistingRealmStack, ActivePathNode TargetNode)
		{
			if (!this.IsIllegalAllyStackPlacement(ExistingRealmStack, TargetNode))
			{
				return false;
			}
			WorkingProvince province = TargetNode.Province;
			WorkingRealm provinceOwner = (province == null) ? null : province.OwnerRealm;
			RelationStates relation = (provinceOwner == null || ExistingRealmStack.Owner == null) ? RelationStates.Peace : ExistingRealmStack.Owner.DiplomacyManager.GetRelation(provinceOwner);
			this.LogMoveTrace("FIX: existing illegal AllyStacks placement found before move. stack=" + this.GetStackText(ExistingRealmStack) + " province=" + ((province == null) ? "null" : province.Name) + " provinceOwner=" + ((provinceOwner == null) ? "null" : provinceOwner.Name) + " relation=" + relation.ToString());
			WorkingStack defender = TargetNode.CurrentStack;
			if (defender != null && !defender.Disposed && defender.Units.Count > 0 && ExistingRealmStack.Owner != null && defender.Owner != null && ExistingRealmStack.Owner.DiplomacyManager.GetRelation(defender.Owner) == RelationStates.War)
			{
				if (ExistingRealmStack.Units.Count > 0)
				{
					this.SelectAllUnitsInStack(ExistingRealmStack);
					return this.StartBattleAndStopAction(ExistingRealmStack, defender, null, "existing illegal AllyStacks stack overlaps hostile CurrentStack");
				}
				this.LogMoveTrace("FIX: removing empty illegal AllyStacks stack before hostile CurrentStack check. stack=" + this.GetStackText(ExistingRealmStack));
				this.CleanupEmptyStack(ExistingRealmStack, "illegal empty ally");
				return false;
			}
			if (TargetNode.AllyStacks.Contains(ExistingRealmStack.ID))
			{
				TargetNode.AllyStacks.Remove(ExistingRealmStack.ID);
			}
			if (TargetNode.CurrentStack == null || TargetNode.CurrentStack.Disposed || TargetNode.CurrentStack.Units.Count <= 0)
			{
				this.LogMoveTrace("FIX: promoting existing illegal AllyStacks stack to CurrentStack because there is no live defender current stack. stack=" + this.GetStackText(ExistingRealmStack));
				TargetNode.CurrentStackID = ExistingRealmStack.ID;
				if (province != null && !province.Occupied)
				{
					this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { province });
				}
				StackAnomalyLogger.CheckNode(this.Game, TargetNode, "AIActionMoveUnits.AfterPromoteIllegalAllyStack", this.AI.Realm);
			}
			return false;
		}

		private bool TryStartBattleAgainstHostileCurrent(UnitMoveData MoveData, WorkingStack SourceStack)
		{
			if (MoveData == null || MoveData.TargetNode == null || SourceStack == null || SourceStack.Disposed)
			{
				return false;
			}
			WorkingStack defender = MoveData.TargetNode.CurrentStack;
			if (defender == null || defender.Disposed || defender.Units.Count <= 0 || defender.Owner == null || defender.Owner == this.AI.Realm)
			{
				return false;
			}
			RelationStates relation = this.GetRelationTo(defender.Owner);
			if (relation == RelationStates.Alliance)
			{
				return false;
			}
			if (relation != RelationStates.War)
			{
				this.LogMoveTrace("FIX: movement into non-allied non-war CurrentStack blocked instead of creating overlap. defender=" + this.GetStackText(defender) + " relation=" + relation.ToString());
				MoveData.Unit.ClearMoves();
				this.State = AiActionStates.Finished;
				return true;
			}
			this.SelectMoveTargetsForBattle(SourceStack, MoveData.TargetNode);
			return this.StartBattleAndStopAction(SourceStack, defender, MoveData.MovePath, "movement target has hostile CurrentStack");
		}

		private bool TryStartBattleAgainstEnemyFortifications(UnitMoveData MoveData, WorkingStack SourceStack)
		{
			if (MoveData == null || MoveData.TargetNode == null || SourceStack == null || SourceStack.Disposed)
			{
				return false;
			}
			if (!this.NodeHasEnemyActiveFortifications(MoveData.TargetNode))
			{
				return false;
			}
			if (MoveData.TargetNode.CurrentStack != null && MoveData.TargetNode.CurrentStack.Units.Count <= 0)
			{
				this.CleanupEmptyStack(MoveData.TargetNode.CurrentStack, "stale defender");
			}
			WorkingStack fortStack = this.CreateFortDefenderStack(MoveData.TargetNode);
			if (fortStack == null)
			{
				return false;
			}
			this.SelectMoveTargetsForBattle(SourceStack, MoveData.TargetNode);
			return this.StartBattleAndStopAction(SourceStack, fortStack, MoveData.MovePath, "movement target has enemy active fortifications");
		}

		private bool IsIllegalEmptyNavalLanding(UnitMoveData MoveData)
		{
			if (MoveData == null || MoveData.Unit == null || MoveData.TargetNode == null)
			{
				return false;
			}
			return MoveData.TargetNode.NodeType == PathNodeTypes.Land && MoveData.Unit.Class == UnitClasses.Naval && MoveData.Unit.CarriedUnit == null;
		}

		public override void Execute()
		{
			this.LogMoveTrace("Execute start, moves=" + ((this.MoveTargets == null) ? 0 : this.MoveTargets.Count));
			foreach (UnitMoveData unitMoveData in this.MoveTargets)
			{
				if (this.IsIllegalEmptyNavalLanding(unitMoveData))
				{
					this.LogMoveTrace("Skipping illegal naval landing: unit=" + unitMoveData.Unit.DisplayName + " unitID=" + unitMoveData.Unit.ID + " target=" + this.GetNodeText(unitMoveData.TargetNode) + ". Naval unit has no carried unit, so WorkingStack.AddUnit would throw.");
					unitMoveData.Unit.ClearMoves();
					continue;
				}
				WorkingStack workingStack = this.FindExistingRealmStackOnNode(unitMoveData.TargetNode);
				WorkingStack ownerStack = unitMoveData.Unit.OwnerStack;
				int targetCountBefore = (workingStack == null) ? 0 : workingStack.Units.Count;
				int sourceCountBefore = (ownerStack == null) ? 0 : ownerStack.Units.Count;
				this.LogMoveTrace("Before move: unit=" + unitMoveData.Unit.DisplayName + " unitID=" + unitMoveData.Unit.ID + " from " + this.GetStackText(ownerStack) + " to " + this.GetNodeText(unitMoveData.TargetNode) + " targetStackBefore=" + this.GetStackText(workingStack) + " sourceCountBefore=" + sourceCountBefore + " targetCountBefore=" + targetCountBefore + " path=" + this.GetPathText(unitMoveData));
				if (targetCountBefore >= 20)
				{
					this.LogMoveTrace("OVERFLOW BLOCKED BEFORE MoveUnit: target already has " + targetCountBefore + " units. Unit movement skipped to prevent stack overflow.");
					unitMoveData.Unit.ClearMoves();
					continue;
				}
				if (workingStack != null && !workingStack.Disposed && this.TryResolveIllegalExistingAllyStack(workingStack, unitMoveData.TargetNode))
				{
					return;
				}
				if (workingStack != null && !workingStack.Disposed && this.IsIllegalAllyStackPlacement(workingStack, unitMoveData.TargetNode))
				{
					// The illegal stack was promoted to CurrentStack by TryResolveIllegalExistingAllyStack. Re-read the target.
					workingStack = this.FindExistingRealmStackOnNode(unitMoveData.TargetNode);
				}
				if (workingStack == null || workingStack.Disposed)
				{
					if (unitMoveData.TargetNode.CurrentStack != null && unitMoveData.TargetNode.CurrentStack.Units.Count <= 0)
					{
						this.CleanupEmptyStack(unitMoveData.TargetNode.CurrentStack, "stale defender");
					}
					if (this.TryStartBattleAgainstHostileCurrent(unitMoveData, ownerStack))
					{
						return;
					}
					if (this.TryStartBattleAgainstEnemyFortifications(unitMoveData, ownerStack))
					{
						return;
					}
					this.LogMoveTrace("Creating target stack at " + this.GetNodeText(unitMoveData.TargetNode));
					workingStack = this.Game.CreateStack(this.AI.Realm.ID, unitMoveData.TargetNode.ID, false);
					WorkingProvince targetProvince = unitMoveData.TargetNode.Province;
					bool useAllyStacks = targetProvince != null && !targetProvince.Occupied && targetProvince.OwnerRealm != this.AI.Realm && this.IsAllianceWith(targetProvince.OwnerRealm);
					if (useAllyStacks)
					{
						RelationStates targetRelation = this.AI.Realm.DiplomacyManager.GetRelation(targetProvince.OwnerRealm);
						this.LogMoveTrace("PLACEMENT DECISION: AllyStacks branch for allied foreign unoccupied province. targetProvince=" + targetProvince.Name + " provinceOwner=" + targetProvince.OwnerRealm.Name + " relation=" + targetRelation.ToString() + " occupied=" + targetProvince.Occupied + " activeResistance=" + targetProvince.ActiveResistance);
						if (!unitMoveData.TargetNode.AllyStacks.Contains(workingStack.ID))
						{
							unitMoveData.TargetNode.AllyStacks.Add(workingStack.ID);
						}
					}
					else
					{
						RelationStates targetRelation = (targetProvince == null || targetProvince.OwnerRealm == null || targetProvince.OwnerRealm == this.AI.Realm) ? RelationStates.Alliance : this.AI.Realm.DiplomacyManager.GetRelation(targetProvince.OwnerRealm);
						this.LogMoveTrace("PLACEMENT DECISION: CurrentStack branch. targetProvince=" + ((targetProvince == null) ? "null" : targetProvince.Name) + " provinceOwner=" + ((targetProvince == null || targetProvince.OwnerRealm == null) ? "null" : targetProvince.OwnerRealm.Name) + " relation=" + targetRelation.ToString() + " currentStackBefore=" + ((unitMoveData.TargetNode.CurrentStack == null) ? "null" : unitMoveData.TargetNode.CurrentStack.ID.ToString()));
						unitMoveData.TargetNode.CurrentStackID = workingStack.ID;
					}
					this.LogMoveTrace("Created target " + this.GetStackText(workingStack));
					StackAnomalyLogger.CheckNode(this.Game, unitMoveData.TargetNode, "AIActionMoveUnits.AfterCreateTargetStack", this.AI.Realm);
				}
				if (this.ExecuteHeroMove(unitMoveData, workingStack))
				{
					continue;
				}
				try
				{
					if (ownerStack == null || ownerStack.Disposed || ownerStack.Node == null)
					{
						this.LogMoveTrace("Unit move skipped: source stack is missing/disposed or has no node.");
						unitMoveData.Unit.ClearMoves();
						continue;
					}
					if (!this.Game.MoveUnit(ownerStack, workingStack, unitMoveData.Unit, unitMoveData.MovePath, false))
					{
						this.LogMoveTrace("MoveUnit returned false; move skipped/blocked.");
						unitMoveData.Unit.ClearMoves();
						continue;
					}
				}
				catch (Exception ex)
				{
					this.LogMoveTrace("EXCEPTION during MoveUnit: " + ex.GetType().Name + ": " + ex.Message + " | unit=" + unitMoveData.Unit.DisplayName + " unitID=" + unitMoveData.Unit.ID + " source=" + this.GetStackText(ownerStack) + " target=" + this.GetStackText(workingStack) + " targetNode=" + this.GetNodeText(unitMoveData.TargetNode) + " path=" + this.GetPathText(unitMoveData));
					throw;
				}
				this.LogMoveTrace("After MoveUnit: source=" + this.GetStackText(ownerStack) + " target=" + this.GetStackText(workingStack));
				StackAnomalyLogger.CheckNode(this.Game, unitMoveData.TargetNode, "AIActionMoveUnits.AfterMoveUnit", this.AI.Realm);
				unitMoveData.Unit.ClearMoves();
				this.CleanupEmptyStack(ownerStack, "source");
				this.CleanupEmptyStack(workingStack, "target");
			}
			this.LogMoveTrace("Execute finished");
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
			base.Dispose();
		}

		public string DebugSource = "Unknown";

		public List<UnitMoveData> MoveTargets;
	}
}
