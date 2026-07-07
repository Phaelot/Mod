using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionPurchaseUnits : AIAction
	{
		public AIActionPurchaseUnits(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			foreach (UnitData unitData in this.UnitTypes)
			{
				if (this.AI.UnitsManager.Funds.CurrentGold >= this.AI.Realm.UnitPurchaseManager.GetUnitCost(unitData))
				{
					this.AI.Realm.QueueUnit(this.Game.CreateUnit(this.AI.Realm.ID, unitData), false, true);
				}
			}
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<UnitData> UnitTypes;
	}
}
