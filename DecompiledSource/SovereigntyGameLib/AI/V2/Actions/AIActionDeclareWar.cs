using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionDeclareWar : AIAction
	{
		public AIActionDeclareWar(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.Game.AllianceController.EstablishWar(this.AI.Realm, this.Target);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public WorkingRealm Target;
	}
}
