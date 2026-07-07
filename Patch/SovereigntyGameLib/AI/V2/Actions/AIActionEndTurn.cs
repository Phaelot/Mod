using System;
using SovereigntyTK.Game;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionEndTurn : AIAction
	{
		public AIActionEndTurn(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
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
	}
}
