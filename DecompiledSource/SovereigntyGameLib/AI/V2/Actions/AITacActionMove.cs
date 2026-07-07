using System;
using System.Drawing;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AITacActionMove : AITacAction
	{
		public AITacActionMove(AIActionManager Manager, SovereigntyGame Game, TacticalBattleController Battle)
			: base(Manager, Game, Battle)
		{
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleActionCompleted), "BattleActionCompleted");
		}

		private void HandleActionCompleted(string EventName, params object[] Args)
		{
			this.State = AiActionStates.Finished;
		}

		public override void Execute()
		{
			this.Battle.RequestMoveUnit(this.Unit, this.TargetTile, this.UnitActions);
			this.State = AiActionStates.Executing;
		}

		public override void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleActionCompleted), "BattleActionCompleted");
		}

		public WorkingUnit Unit;

		public UnitActionData UnitActions;

		public Point TargetTile;
	}
}
