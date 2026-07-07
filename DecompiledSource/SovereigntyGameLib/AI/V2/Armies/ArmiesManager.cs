using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Armies
{
	internal class ArmiesManager
	{
		public ArmiesManager()
		{
			this.CurrentArmies = new List<ArmyManager>();
			this.UnmanagedStacks = new List<WorkingStack>();
		}

		private List<ArmyManager> CurrentArmies;

		private List<WorkingStack> UnmanagedStacks;
	}
}
