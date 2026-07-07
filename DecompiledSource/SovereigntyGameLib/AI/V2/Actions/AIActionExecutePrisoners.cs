using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionExecutePrisoners : AIAction
	{
		public AIActionExecutePrisoners(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.AI.PrisonManager.ShowPrisonerMessage(this.Units, "PRISON_AI_EXECUTE_TITLE", "PRISON_AI_EXECUTE_TEXT");
			this.Game.PrisonerController.ExecuteUnits(this.Units, this.AI.Realm);
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<WorkingUnit> Units;
	}
}
