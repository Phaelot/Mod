using System;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class PathPoint
	{
		public void Dispose()
		{
			this.Node = null;
		}

		public ActivePathNode Node;

		public float MoveCost;

		public ConnectionTypes LinkType;
	}
}
