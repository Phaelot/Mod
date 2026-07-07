using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AITacActionRetreat : AITacAction
	{
		public AITacActionRetreat(AIActionManager Manager, SovereigntyGame Game, TacticalBattleController Battle)
			: base(Manager, Game, Battle)
		{
		}

		public override void Execute()
		{
			if (this.Game.CurrentTacticalBattle.RetreatingPlayer != null)
			{
				this.State = AiActionStates.Finished;
				return;
			}
			this.Game.GameCore.MessageHandler.ShowInfoMessage("MSG_ENEMYRETREAT_TITLE", "MSG_ENEMYRETREAT_TEXT");
			this.Battle.RetreatPlayer(this.AI.Realm);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
		}
	}
}
