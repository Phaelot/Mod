using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI
{
	internal class NodeUnitData
	{
		public NodeUnitData(ActivePathNode Node, float Value, WorkingRealm Realm)
		{
			this.Node = Node;
			this.NodeValue = Value;
			this.Units = new List<int>();
			WorkingStack realmStack = Node.GetRealmStack(Realm);
			if (realmStack != null)
			{
				foreach (WorkingUnit workingUnit in realmStack.Units)
				{
					this.Units.Add(workingUnit.ID);
				}
			}
		}

		public ActivePathNode Node;

		public List<int> Units;

		public float NodeValue;

		public Path MovePath;
	}
}
