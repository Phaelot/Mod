using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.Trackers
{
	public class RealmProvinceTracker
	{
		public event ProvinceDelegate OnProvinceEnter;

		public event ProvinceDelegate OnProvinceLeave;

		public RealmProvinceTracker(SovereigntyGame Game, WorkingRealm Realm)
		{
			this.Game = Game;
			this.Realm = Realm;
			Game.OnProvinceOwnerChanged += this.Game_OnProvinceOwnerChanged;
			this.ActiveProvinces = new List<int>();
		}

		public void Init()
		{
			foreach (WorkingProvince workingProvince in this.Realm.Provinces)
			{
				this.Game_OnProvinceOwnerChanged(workingProvince, null, this.Realm);
			}
		}

		private void Game_OnProvinceOwnerChanged(WorkingProvince Province, WorkingRealm OldRealm, WorkingRealm Realm)
		{
			if (Realm == this.Realm && !this.ActiveProvinces.Contains(Province.ID))
			{
				this.ActiveProvinces.Add(Province.ID);
				if (this.OnProvinceEnter != null)
				{
					this.OnProvinceEnter(Province);
				}
			}
			if (Realm != this.Realm && this.ActiveProvinces.Contains(Province.ID))
			{
				this.ActiveProvinces.Remove(Province.ID);
				if (this.OnProvinceLeave != null)
				{
					this.OnProvinceLeave(Province);
				}
			}
		}

		public void Dispose()
		{
			foreach (WorkingProvince workingProvince in this.Realm.Provinces)
			{
				this.Game_OnProvinceOwnerChanged(workingProvince, this.Realm, null);
			}
			this.OnProvinceEnter = null;
			this.OnProvinceLeave = null;
		}

		private SovereigntyGame Game;

		private WorkingRealm Realm;

		private List<int> ActiveProvinces;
	}
}
