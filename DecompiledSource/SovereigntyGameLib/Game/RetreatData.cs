using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.Game
{
	public class RetreatData
	{
		public RetreatData()
		{
			this.RetreatTargets = new Dictionary<WorkingUnit, ActivePathNode>();
		}

		public Dictionary<WorkingUnit, ActivePathNode> RetreatTargets;

		public ActivePathNode HeroRetreatTarget;
	}
}
