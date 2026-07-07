using System;

namespace SovereigntyTK.Game.Campaign
{
	public class ObjectiveBoolean : Objective
	{
		public ObjectiveBoolean(Sovereignty Game, bool Repeatable, string Event, ObjectiveBooleanDelegate Action)
			: base(Game, Event)
		{
			this.Action = Action;
			this.Repeatable = Repeatable;
		}

		protected override void HandleEventFired(object[] Args)
		{
			switch (this.Action(Args))
			{
			case Results.Complete:
				if (!this.Repeatable)
				{
					base.Disable();
				}
				base.SetCompleted();
				return;
			case Results.Incomplete:
				base.SetIncomplete();
				break;
			case Results.Ignore:
				break;
			default:
				return;
			}
		}

		private ObjectiveBooleanDelegate Action;

		private bool Repeatable;
	}
}
