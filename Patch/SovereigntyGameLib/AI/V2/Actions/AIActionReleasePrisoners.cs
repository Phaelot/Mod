using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionReleasePrisoners : AIAction
	{
		public AIActionReleasePrisoners(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.AI.PrisonManager.ShowPrisonerMessage(this.Units, "PRISON_AI_RELEASE_TITLE", "PRISON_AI_RELEASE_TEXT");
			this.Game.PrisonerController.ReleaseUnits(this.Units, this.AI.Realm);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<WorkingUnit> Units;
	}
}
