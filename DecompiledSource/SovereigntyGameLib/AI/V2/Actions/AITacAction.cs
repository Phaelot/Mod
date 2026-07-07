using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AITacAction : AIAction
	{
		public AITacAction(AIActionManager Manager, SovereigntyGame Game, TacticalBattleController Battle)
			: base(Manager, Game)
		{
			this.Battle = Battle;
		}

		public override void Execute()
		{
			this.Game.TurnController.RequestEndTurn();
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public TacticalBattleController Battle;
	}
}
