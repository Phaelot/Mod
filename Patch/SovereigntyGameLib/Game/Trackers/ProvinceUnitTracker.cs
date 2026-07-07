using System;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.Game.Trackers
{
	public class ProvinceUnitTracker
	{
		public event UnitDelegate OnUnitEnter;

		public event UnitDelegate OnUnitLeave;

		public ProvinceUnitTracker(SovereigntyGame Game, WorkingProvince Province)
		{
			this.Game = Game;
			this.Province = Province;
			if (Province.LandNode != null)
			{
				this.LandTracker = new NodeUnitTracker(Game, Province.LandNode);
				this.LandTracker.OnUnitEnterNode += this.LandTracker_OnUnitEnterNode;
				this.LandTracker.OnUnitLeaveNode += this.LandTracker_OnUnitLeaveNode;
			}
			if (Province.HarbourNode != null)
			{
				this.HarbourTracker = new NodeUnitTracker(Game, Province.HarbourNode);
				this.HarbourTracker.OnUnitEnterNode += this.LandTracker_OnUnitEnterNode;
				this.HarbourTracker.OnUnitLeaveNode += this.LandTracker_OnUnitLeaveNode;
			}
		}

		public void Init()
		{
			if (this.LandTracker != null)
			{
				this.LandTracker.Init();
			}
			if (this.HarbourTracker != null)
			{
				this.HarbourTracker.Init();
			}
		}

		public void Dispose()
		{
			if (this.LandTracker != null)
			{
				this.LandTracker.Dispose();
				this.LandTracker = null;
			}
			if (this.HarbourTracker != null)
			{
				this.HarbourTracker.Dispose();
				this.HarbourTracker = null;
			}
			this.OnUnitEnter = null;
			this.OnUnitLeave = null;
		}

		private void LandTracker_OnUnitLeaveNode(WorkingUnit Unit)
		{
			if (this.OnUnitLeave != null)
			{
				this.OnUnitLeave(Unit);
			}
		}

		private void LandTracker_OnUnitEnterNode(WorkingUnit Unit)
		{
			if (this.OnUnitEnter != null)
			{
				this.OnUnitEnter(Unit);
			}
		}

		private SovereigntyGame Game;

		public WorkingProvince Province;

		private NodeUnitTracker LandTracker;

		private NodeUnitTracker HarbourTracker;
	}
}
