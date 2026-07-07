using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionRecruitPrisoners : AIAction
	{
		public AIActionRecruitPrisoners(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.AI.PrisonManager.ShowPrisonerMessage(this.Units, "PRISON_AI_RECRUIT_TITLE", "PRISON_AI_RECRUIT_TEXT");
			this.Game.PrisonerController.RecruitUnits(this.Units, this.AI.Realm, false);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<WorkingUnit> Units;
	}
}
