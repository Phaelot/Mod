using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI
{
	public class UnitMoveData
	{
		public UnitMoveData(WorkingUnit Unit, ActivePathNode Node, Path MovePath)
		{
			this.Unit = Unit;
			this.TargetNode = Node;
			this.MovePath = MovePath;
		}

		public WorkingUnit Unit;

		public ActivePathNode TargetNode;

		public Path MovePath;
	}
}
