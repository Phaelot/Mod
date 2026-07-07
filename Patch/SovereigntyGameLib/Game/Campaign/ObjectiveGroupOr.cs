using System;
using System.Collections.Generic;

namespace SovereigntyTK.Game.Campaign
{
	public class ObjectiveGroupOr : Objective
	{
		public ObjectiveGroupOr(Sovereignty Game)
			: base(Game, "")
		{
			this.SubObjectives = new List<Objective>();
		}

		public void AddObjective(Objective SubObjective)
		{
			this.SubObjectives.Add(SubObjective);
			SubObjective.OnComplete = (ObjectiveDelegate)Delegate.Combine(SubObjective.OnComplete, new ObjectiveDelegate(this.SubObjectiveComplete));
		}

		private void SubObjectiveComplete(Objective Obj)
		{
			base.SetCompleted();
		}

		protected override void HandleEventFired(object[] Args)
		{
			throw new NotImplementedException();
		}

		private List<Objective> SubObjectives;
	}
}
