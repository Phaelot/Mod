using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class UnitPurchaseManager
	{
		public event RealmCostModifier OnRequestUnitCostMod;

		public event RealmCostModifier OnRequestUnitUpkeepMod;

		public event RealmCostModifier OnRequestUnitTimeMod;

		private WorkingRealm Realm
		{
			get
			{
				WorkingRealm workingRealm = null;
				this.Game.AllRealms.TryGetValue(this.RealmID, out workingRealm);
				return workingRealm;
			}
		}

		public UnitPurchaseManager(SovereigntyGame Game, int RealmID)
		{
			this.Game = Game;
			this.RealmID = RealmID;
		}

		public int GetUnitTime(UnitData Unit)
		{
			int trainTime = Unit.TrainTime;
			if (this.OnRequestUnitTimeMod != null)
			{
				this.OnRequestUnitTimeMod(Unit, ref trainTime);
			}
			return trainTime;
		}

		public int GetUnitUpkeep(UnitData Unit)
		{
			int upkeep = Unit.Upkeep;
			if (this.OnRequestUnitUpkeepMod != null)
			{
				this.OnRequestUnitUpkeepMod(Unit, ref upkeep);
			}
			return upkeep;
		}

		public int GetUnitCost(UnitData Unit)
		{
			int cost = Unit.Cost;
			if (this.OnRequestUnitCostMod != null)
			{
				this.OnRequestUnitCostMod(Unit, ref cost);
			}
			return cost;
		}

		public int GetUnitCost(WorkingUnit Unit)
		{
			if (Unit.BaseType == null)
			{
				return Unit.BaseCost;
			}
			int baseCost = Unit.BaseCost;
			if (this.OnRequestUnitCostMod != null)
			{
				this.OnRequestUnitCostMod(Unit.BaseType, ref baseCost);
			}
			return baseCost;
		}

		public List<Tuple<ResourceData, int>> GetUnitResourceCost(WorkingUnit Unit)
		{
			string text = Unit.BaseName.Split(new char[] { '.' })[1];
			string text2 = Unit.BaseName.Split(new char[] { '.' })[0];
			UnitData unit = this.GetUnit(text, text2);
			List<Tuple<ResourceData, int>> list = new List<Tuple<ResourceData, int>>();
			foreach (KeyValuePair<string, int> keyValuePair in unit.GetRequiredResources())
			{
				list.Add(new Tuple<ResourceData, int>(this.Game.GameCore.Data.Resources[keyValuePair.Key], keyValuePair.Value));
			}
			return list;
		}

		public UnitData GetUnit(string UnitName)
		{
			return this.GetUnit(UnitName, this.Realm.Name);
		}

		public UnitData GetUnit(string UnitName, string RealmName)
		{
			return this.Game.GameCore.Data.Units.Values.FirstOrDefault((UnitData x) => x.Realm != null && x.Realm == RealmName && x.Name == UnitName);
		}

		public List<KeyValuePair<UnitData, UnitTrainStates>> GetAvailableUnitTypes()
		{
			if (this.Realm == null)
			{
				return new List<KeyValuePair<UnitData, UnitTrainStates>>();
			}
			List<UnitData> list = this.Game.GameCore.Data.Units.Values.Where((UnitData x) => x.Realm != null && x.Realm == this.Realm.Name && x.Cost > 0).ToList<UnitData>();
			list.AddRange(this.GetAuxiliaryUnits());
			list = list.Where((UnitData x) => this.CanPurchaseUnit(x)).ToList<UnitData>();
			List<KeyValuePair<UnitData, UnitTrainStates>> list2 = new List<KeyValuePair<UnitData, UnitTrainStates>>();
			bool flag = this.Realm.GetCurrentUnitQueue().Count >= 20;
			foreach (UnitData unitData in list)
			{
				if (unitData.Class == UnitClasses.Naval && !this.Realm.HasHarbour)
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.NoHarbour));
				}
				else if (unitData.Rank == UnitRanks.Unique && this.Realm.GetUnitTypeCount(unitData) >= 1)
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.TooManyUniques));
				}
				else if (unitData.Rank == UnitRanks.Elite && this.Realm.GetUnitTypeCount(unitData) >= this.Realm.EliteUnitLimit)
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.TooManyElites));
				}
				else if (unitData.Realm != this.Realm.Name && this.Realm.GetUnitTypeCount(unitData) >= 4)
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.TooManyAux));
				}
				else if (flag)
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.QueueFull));
				}
				else if (this.Realm.GetUnitGold() < this.GetUnitCost(unitData))
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.CannotAfford));
				}
				else if (!this.ResourcesForUnitAvailable(unitData))
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.NoResources));
				}
				else
				{
					list2.Add(new KeyValuePair<UnitData, UnitTrainStates>(unitData, UnitTrainStates.OK));
				}
			}
			return list2;
		}

		private bool ResourcesForUnitAvailable(UnitData Data)
		{
			foreach (KeyValuePair<string, int> keyValuePair in Data.GetRequiredResources())
			{
				if (this.Realm.GetStockpiledResource(this.Game.GameCore.Data.Resources[keyValuePair.Key]) < keyValuePair.Value)
				{
					return false;
				}
			}
			return true;
		}

		private bool CanPurchaseUnit(UnitData Unit)
		{
			return this.Realm.Restrictions.AllowUnits.Contains(Unit.Name) || (!this.Realm.Restrictions.DenyUnits.Contains(Unit.Name) && Unit.AllowPurchase);
		}

		public UnitData GetLocalAuxiliary()
		{
			return this.Game.GameCore.Data.Units.Values.FirstOrDefault((UnitData x) => x.Realm != null && x.Realm == this.Realm.Name && x.Abilities.Contains("Auxiliary"));
		}

		public List<UnitData> GetAuxiliaryUnits()
		{
			List<UnitData> list = new List<UnitData>();
			using (IEnumerator<WorkingRealm> enumerator = this.Realm.Allies.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					WorkingRealm Ally = enumerator.Current;
					List<UnitData> list2 = this.Game.GameCore.Data.Units.Values.Where((UnitData x) => x.Realm != null && x.Realm == Ally.Name && x.Cost > 0).ToList<UnitData>();
					foreach (UnitData unitData in list2)
					{
						if (unitData.Abilities.Contains("Auxiliary"))
						{
							list.Add(unitData);
						}
					}
				}
			}
			return list;
		}

		public bool ResourceIsUseful(ResourceData Resource)
		{
			List<UnitData> list = this.Game.GameCore.Data.Units.Values.Where((UnitData x) => x.Realm != null && x.Realm == this.Realm.Name && x.Cost > 0).ToList<UnitData>();
			list.AddRange(this.GetAuxiliaryUnits());
			return list.Count((UnitData x) => this.CanPurchaseUnit(x) && x.GetRequiredResources().ContainsKey(Resource.ResourceName)) > 0;
		}

		public UnitData GetUnitByClass(UnitClasses Class)
		{
			return this.Game.GameCore.Data.Units.Values.FirstOrDefault((UnitData x) => x.Realm != null && x.Realm == this.Realm.Name && x.Class == Class);
		}

		public List<UnitData> GetUnitsInClass(UnitClasses Class)
		{
			return this.Game.GameCore.Data.Units.Values.Where((UnitData x) => x.Realm != null && x.Realm == this.Realm.Name && x.Class == Class).ToList<UnitData>();
		}

		internal object GetUnitByRace(Races Race)
		{
			return this.Game.GameCore.Data.Units.Values.FirstOrDefault((UnitData x) => x.Realm != null && x.Realm == this.Realm.Name && x.Race == Race);
		}

		private SovereigntyGame Game;

		private int RealmID;
	}
}
