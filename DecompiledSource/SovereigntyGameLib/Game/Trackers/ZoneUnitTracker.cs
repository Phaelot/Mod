using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.Game.Trackers
{
	public class ZoneUnitTracker
	{
		public event UnitDelegate OnUnitEnter;

		public event UnitDelegate OnUnitLeave;

		public ZoneUnitTracker(SovereigntyGame Game, WorkingZone Zone)
		{
			this.Game = Game;
			this.Zone = Zone;
			this.Trackers = new List<NodeUnitTracker>();
			foreach (ActivePathNode activePathNode in Zone.Nodes)
			{
				NodeUnitTracker nodeUnitTracker = new NodeUnitTracker(Game, activePathNode);
				nodeUnitTracker.OnUnitEnterNode += this.Tracker_OnUnitEnterNode;
				nodeUnitTracker.OnUnitLeaveNode += this.Tracker_OnUnitLeaveNode;
				this.Trackers.Add(nodeUnitTracker);
			}
		}

		private void Tracker_OnUnitLeaveNode(WorkingUnit Unit)
		{
			if (this.OnUnitLeave != null)
			{
				this.OnUnitLeave(Unit);
			}
		}

		private void Tracker_OnUnitEnterNode(WorkingUnit Unit)
		{
			if (this.OnUnitEnter != null)
			{
				this.OnUnitEnter(Unit);
			}
		}

		public void Init()
		{
			foreach (NodeUnitTracker nodeUnitTracker in this.Trackers)
			{
				nodeUnitTracker.Init();
			}
		}

		public void Dispose()
		{
			foreach (NodeUnitTracker nodeUnitTracker in this.Trackers)
			{
				nodeUnitTracker.Dispose();
			}
			this.Trackers.Clear();
			this.OnUnitEnter = null;
			this.OnUnitLeave = null;
		}

		private SovereigntyGame Game;

		public WorkingZone Zone;

		private List<NodeUnitTracker> Trackers;
	}
}
