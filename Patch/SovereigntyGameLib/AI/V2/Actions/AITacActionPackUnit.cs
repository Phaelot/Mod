using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AITacActionPackUnit : AITacAction
	{
		public AITacActionPackUnit(AIActionManager Manager, SovereigntyGame Game, TacticalBattleController Battle)
			: base(Manager, Game, Battle)
		{
		}

		public override void Execute()
		{
			this.Battle.PackUnit(this.Unit, this.UnitType);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
		}

		public WorkingUnit Unit;

		public UnitData UnitType;
	}
}
