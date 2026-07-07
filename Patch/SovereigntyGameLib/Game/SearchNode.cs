using System;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	internal class SearchNode
	{
		public bool CanLeaveZone()
		{
			return this.Node.NodeType != PathNodeTypes.Sea || this.Parent == null || (this.Parent.Node.NodeType != PathNodeTypes.Harbour && this.Parent.Node.NodeType != PathNodeTypes.RiverHarbour && (this.Parent.Node.NodeType != PathNodeTypes.Sea || this.Parent.Node.SeaZoneID == this.Node.SeaZoneID) && this.Parent.CanLeaveZone());
		}

		public ActivePathNode Node;

		public ConnectionTypes LinkType;

		public SearchNode Parent;

		public float MoveCost;

		public float PathCost;

		public float PathDistance;
	}
}
