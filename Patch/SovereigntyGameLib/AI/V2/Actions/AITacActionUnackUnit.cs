using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AITacActionUnackUnit : AITacAction
	{
		public AITacActionUnackUnit(AIActionManager Manager, SovereigntyGame Game, TacticalBattleController Battle)
			: base(Manager, Game, Battle)
		{
		}

		public override void Execute()
		{
			this.Battle.UnpackUnit(this.Unit);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
		}

		public WorkingUnit Unit;
	}
}
