using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AITacActionEndTurn : AITacAction
	{
		public AITacActionEndTurn(AIActionManager Manager, SovereigntyGame Game, TacticalBattleController Battle)
			: base(Manager, Game, Battle)
		{
		}

		public override void Execute()
		{
			this.Battle.RequestEndTurn(this.AI.Realm, false);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
		}
	}
}
