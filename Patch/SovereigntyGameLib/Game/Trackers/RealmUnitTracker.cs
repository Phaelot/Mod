using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.Game.Trackers
{
	public class RealmUnitTracker
	{
		public event UnitDelegate OnUnitEnter;

		public event UnitDelegate OnUnitLeave;

		public RealmUnitTracker(SovereigntyGame Game, WorkingRealm Realm)
		{
			this.Game = Game;
			this.Realm = Realm;
			this.UnitTrackers = new Dictionary<WorkingProvince, ProvinceUnitTracker>();
			this.ProvinceTracker = new RealmProvinceTracker(Game, Realm);
			this.ProvinceTracker.OnProvinceEnter += this.ProvinceTracker_OnProvinceEnter;
			this.ProvinceTracker.OnProvinceLeave += this.ProvinceTracker_OnProvinceLeave;
			this.ActiveProvinces = new List<int>();
			this.ActiveUnits = new List<int>();
		}

		public void Init()
		{
			this.ProvinceTracker.Init();
		}

		private void ProvinceTracker_OnProvinceLeave(WorkingProvince Province)
		{
			if (!this.ActiveProvinces.Contains(Province.ID))
			{
				return;
			}
			this.UnitTrackers[Province].Dispose();
			this.UnitTrackers.Remove(Province);
			this.ActiveProvinces.Remove(Province.ID);
		}

		private void ProvinceTracker_OnProvinceEnter(WorkingProvince Province)
		{
			if (this.ActiveProvinces.Contains(Province.ID))
			{
				return;
			}
			ProvinceUnitTracker provinceUnitTracker = new ProvinceUnitTracker(this.Game, Province);
			this.UnitTrackers.Add(Province, provinceUnitTracker);
			this.ActiveProvinces.Add(Province.ID);
			provinceUnitTracker.OnUnitEnter += this.Tracker_OnUnitEnter;
			provinceUnitTracker.OnUnitLeave += this.Tracker_OnUnitLeave;
			provinceUnitTracker.Init();
		}

		public void Dispose()
		{
			this.ProvinceTracker.Dispose();
		}

		private void Tracker_OnUnitLeave(WorkingUnit Unit)
		{
			if (!this.ActiveUnits.Contains(Unit.ID))
			{
				return;
			}
			this.ActiveUnits.Remove(Unit.ID);
			if (this.OnUnitLeave != null)
			{
				this.OnUnitLeave(Unit);
			}
		}

		private void Tracker_OnUnitEnter(WorkingUnit Unit)
		{
			if (this.ActiveUnits.Contains(Unit.ID))
			{
				return;
			}
			this.ActiveUnits.Add(Unit.ID);
			if (this.OnUnitEnter != null)
			{
				this.OnUnitEnter(Unit);
			}
		}

		private SovereigntyGame Game;

		private WorkingRealm Realm;

		private RealmProvinceTracker ProvinceTracker;

		private Dictionary<WorkingProvince, ProvinceUnitTracker> UnitTrackers;

		private List<int> ActiveProvinces;

		private List<int> ActiveUnits;
	}
}
