using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionEndTreaties : AIAction
	{
		public AIActionEndTreaties(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			foreach (WorkingRealm workingRealm in this.Realms)
			{
				this.Game.AllianceController.BreakCurrentTreaty(this.AI.Realm, workingRealm, true, false);
			}
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<WorkingRealm> Realms;
	}
}
