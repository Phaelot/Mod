using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionIntercept : AIAction
	{
		public AIActionIntercept(AIActionManager Manager, SovereigntyGame Game)
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
				if (!this.AI.WarManager.InvasionTargets.ContainsKey(occupierRealm.ID))
				{
					throw new Exception("Attempted to invade invalid target realm");
				}
				this.Game.AllianceController.EstablishWar(this.AI.Realm, occupierRealm);
			}
			foreach (WorkingUnit workingUnit in this.Stack.Units)
			{
				workingUnit.Selected = false;
			}
			foreach (WorkingUnit workingUnit2 in this.Units)
			{
				workingUnit2.Selected = true;
			}
			this.Game.StartBattle(this.Stack, this.InterceptStack, null);
			if (this.Game.PendingBattle == null)
			{
				this.State = AiActionStates.Finished;
				return;
			}
			this.State = AiActionStates.Executing;
		}

		public override void Update()
		{
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public WorkingStack InterceptStack;

		public WorkingStack Stack;

		public List<WorkingUnit> Units;

		public ActivePathNode Node;
	}
}
