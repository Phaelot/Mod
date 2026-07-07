using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionRaisePrisoners : AIAction
	{
		public AIActionRaisePrisoners(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.AI.PrisonManager.ShowPrisonerMessage(this.Units, "PRISON_AI_RAISE_TITLE", "PRISON_AI_RAISE_TEXT");
			this.Game.PrisonerController.RecruitUnits(this.Units, this.AI.Realm, true);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<WorkingUnit> Units;
	}
}
