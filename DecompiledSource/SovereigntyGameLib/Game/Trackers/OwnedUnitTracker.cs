using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.Game.Trackers
{
	public class OwnedUnitTracker
	{
		public event UnitDelegate OnUnitEnter;

		public event UnitDelegate OnUnitLeave;

		public OwnedUnitTracker(SovereigntyGame Game, WorkingRealm Realm)
		{
			this.Game = Game;
			this.Realm = Realm;
			Game.OnUnitCreated += this.Game_OnUnitCreated;
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleUnitDestroyed), "UnitDestroyed");
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleOwnerChanged), "UnitOwnerChanged");
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleOwnershipChanged), "UnitOwnershipChanged");
			this.ActiveUnits = new List<int>();
		}

		private void RemoveUnit(WorkingUnit Unit)
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

		private void AddUnit(WorkingUnit Unit)
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

		private void HandleOwnershipChanged(string EventName, params object[] Args)
		{
			WorkingUnit workingUnit = Args[0] as WorkingUnit;
			WorkingRealm workingRealm = Args[1] as WorkingRealm;
			WorkingRealm workingRealm2 = Args[2] as WorkingRealm;
			if (workingRealm == this.Realm)
			{
				this.RemoveUnit(workingUnit);
			}
			if (workingRealm2 == this.Realm)
			{
				this.AddUnit(workingUnit);
			}
		}

		private void HandleOwnerChanged(string EventName, params object[] Args)
		{
			WorkingUnit workingUnit = Args[0] as WorkingUnit;
			int num = (int)Args[1];
			int num2 = (int)Args[2];
			if (num == this.Realm.ID)
			{
				this.RemoveUnit(workingUnit);
			}
			if (num2 == this.Realm.ID)
			{
				this.AddUnit(workingUnit);
			}
		}

		private void HandleUnitDestroyed(string EventName, params object[] Args)
		{
			WorkingUnit workingUnit = Args[0] as WorkingUnit;
			if (workingUnit.OwnerRealmID == this.Realm.ID)
			{
				this.RemoveUnit(workingUnit);
			}
		}

		private void Game_OnUnitCreated(WorkingUnit Unit)
		{
			if (Unit.OwnerRealmID == this.Realm.ID)
			{
				this.AddUnit(Unit);
			}
		}

		public void Init()
		{
			foreach (WorkingUnit workingUnit in this.Realm.Units)
			{
				this.AddUnit(workingUnit);
			}
		}

		public void Dispose()
		{
			this.Game.OnUnitCreated -= this.Game_OnUnitCreated;
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleUnitDestroyed), "UnitDestroyed");
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleOwnerChanged), "UnitOwnerChanged");
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleOwnershipChanged), "UnitOwnershipChanged");
			foreach (int num in this.ActiveUnits.ToList<int>())
			{
				WorkingUnit workingUnit = null;
				this.Game.AllUnits.TryGetValue(num, out workingUnit);
				if (workingUnit != null)
				{
					this.RemoveUnit(workingUnit);
				}
			}
			this.OnUnitEnter = null;
			this.OnUnitLeave = null;
		}

		private SovereigntyGame Game;

		private WorkingRealm Realm;

		private List<int> ActiveUnits;
	}
}
