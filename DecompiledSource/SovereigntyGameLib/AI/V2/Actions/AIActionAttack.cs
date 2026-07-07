using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionAttack : AIAction
	{
		public AIActionAttack(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
		}

		private void HandleBattleEnd(string EventName, params object[] Args)
		{
			this.State = AiActionStates.Finished;
		}

		public override void Execute()
		{
			WorkingRealm occupierRealm = this.Node.Province.OccupierRealm;
			if (this.AI.Realm.DiplomacyManager.GetRelation(occupierRealm) != RelationStates.War)
			{
				if (this.AI.WarManager.InvasionTargets.ContainsKey(occupierRealm.ID))
				{
					this.Game.AllianceController.EstablishWar(this.AI.Realm, occupierRealm);
				}
				else if (!this.IgnoreInvasionCheck)
				{
					throw new Exception("Attempted to invade invalid target realm");
				}
			}
			Path path = this.Game.PathManager.GetPath(this.Stack.Node, this.Node, this.Units, false, this.Stack.Owner, false);
			if (this.Node.CurrentStack == null || this.Node.CurrentStack.Units.Count <= 0)
			{
				WorkingStack workingStack = this.Node.CurrentStack;
				if (workingStack == null || workingStack.Disposed)
				{
					workingStack = this.Game.CreateStack(this.AI.Realm.ID, this.Node.ID, true);
				}
				foreach (WorkingUnit workingUnit in this.Units)
				{
					if (workingStack.Disposed)
					{
						workingStack = this.Game.CreateStack(this.AI.Realm.ID, this.Node.ID, true);
					}
					this.Game.MoveUnit(this.Stack, workingStack, workingUnit, path, false);
					workingUnit.ClearMoves();
				}
				if (this.Stack.Units.Count == 0)
				{
					this.Game.RemoveStack(this.Stack);
				}
				if (this.Node.Province != null)
				{
					this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { this.Node.Province });
				}
				this.State = AiActionStates.Finished;
				return;
			}
			foreach (WorkingUnit workingUnit2 in this.Stack.Units)
			{
				workingUnit2.Selected = false;
			}
			foreach (WorkingUnit workingUnit3 in this.Units)
			{
				workingUnit3.Selected = true;
			}
			this.Game.StartBattle(this.Stack, this.Node.CurrentStack, path);
			if (this.Game.PendingBattle == null)
			{
				this.State = AiActionStates.Finished;
				return;
			}
			this.State = AiActionStates.Executing;
		}

		public override void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
			base.Dispose();
		}

		public WorkingProvince Province;

		public WorkingStack Stack;

		public WorkingRealm Realm;

		public List<WorkingUnit> Units;

		public ActivePathNode Node;

		public AutoBattleController AutoBattle;

		public TacticalBattleController TacticalBattle;

		public bool IgnoreInvasionCheck;
	}
}
