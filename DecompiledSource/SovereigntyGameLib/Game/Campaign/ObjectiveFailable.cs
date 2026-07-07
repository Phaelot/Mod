using System;

namespace SovereigntyTK.Game.Campaign
{
	public class ObjectiveFailable : Objective
	{
		public ObjectiveFailable(Sovereignty Game, string Event, ObjectiveBooleanDelegate Action)
			: base(Game, Event)
		{
			this.Action = Action;
			base.SetCompleted();
		}

		protected override void HandleEventFired(object[] Args)
		{
			switch (this.Action(Args))
			{
			case Results.Complete:
				base.SetCompleted();
				return;
			case Results.Incomplete:
				base.SetFailed();
				break;
			case Results.Ignore:
				break;
			default:
				return;
			}
		}

		private ObjectiveBooleanDelegate Action;
	}
}
