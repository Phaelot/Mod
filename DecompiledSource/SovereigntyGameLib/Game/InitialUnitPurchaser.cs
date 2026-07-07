using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class InitialUnitPurchaser
	{
		public InitialUnitPurchaser(SovereigntyGame Game)
		{
			this.Game = Game;
			this.RNG = new Random();
		}

		public void PurchaseInitialUnits()
		{
			foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
			{
				this.PurchaseInitialUnits(workingRealm);
			}
		}

		private WorkingProvince GetDeployProvince(WorkingRealm Realm)
		{
			WorkingProvince capitolProvince = Realm.CapitolProvince;
			if (capitolProvince.LandNode.CurrentStack == null || capitolProvince.LandNode.CurrentStack.Units.Count < 20)
			{
				return capitolProvince;
			}
			List<WorkingProvince> list = Realm.Provinces.Where((WorkingProvince x) => x.LandNode.CurrentStack == null || x.LandNode.CurrentStack.Units.Count < 20).ToList<WorkingProvince>();
			if (list.Count == 0)
			{
				return null;
			}
			return list[this.RNG.Next(list.Count)];
		}

		private void PurchaseInitialUnits(WorkingRealm Realm)
		{
			List<UnitData> list = this.Game.GameCore.Data.Units.Values.Where((UnitData x) => x.Realm != null && x.Realm == Realm.Name && x.Class != UnitClasses.Naval).ToList<UnitData>();
			foreach (UnitData unitData in list)
			{
				int i = unitData.InitialPurchaseCount;
				while (i > 0)
				{
					i--;
					WorkingProvince deployProvince = this.GetDeployProvince(Realm);
					if (deployProvince == null)
					{
						return;
					}
					this.DeployUnit(Realm, unitData, deployProvince, false);
				}
			}
			list = list.Where((UnitData x) => x.AllowPurchase).ToList<UnitData>();
			using (List<UnitData>.Enumerator enumerator2 = list.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					UnitData Unit2 = enumerator2.Current;
					if (Unit2.Rank == UnitRanks.Unique)
					{
						if (Realm.Units.Count((WorkingUnit x) => x.BaseType == Unit2) <= 0 && Realm.GetUnitGold() >= Realm.UnitPurchaseManager.GetUnitCost(Unit2))
						{
							WorkingProvince deployProvince2 = this.GetDeployProvince(Realm);
							if (deployProvince2 == null)
							{
								return;
							}
							this.DeployUnit(Realm, Unit2, deployProvince2, true);
						}
					}
				}
			}
			list = list.Where((UnitData x) => x.Rank != UnitRanks.Unique).ToList<UnitData>();
			while (list.Count > 0)
			{
				using (List<UnitData>.Enumerator enumerator3 = list.Where((UnitData x) => x.Rank == UnitRanks.Elite).ToList<UnitData>().GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						Func<WorkingUnit, bool> func = null;
						UnitData Unit = enumerator3.Current;
						IEnumerable<WorkingUnit> units = Realm.Units;
						if (func == null)
						{
							func = (WorkingUnit x) => x.BaseType == Unit;
						}
						if (units.Count(func) > 3)
						{
							list.Remove(Unit);
						}
					}
				}
				list = list.Where((UnitData x) => Realm.GetUnitGold() >= Realm.UnitPurchaseManager.GetUnitCost(x)).ToList<UnitData>();
				if (list.Count == 0)
				{
					return;
				}
				int num = list.Sum((UnitData x) => x.PeaceWeight);
				if (num == 0)
				{
					return;
				}
				int num2 = this.RNG.Next(num);
				int num3 = 0;
				foreach (UnitData unitData2 in list)
				{
					num3 += unitData2.PeaceWeight;
					if (num2 < num3)
					{
						WorkingProvince deployProvince3 = this.GetDeployProvince(Realm);
						if (deployProvince3 == null)
						{
							return;
						}
						this.DeployUnit(Realm, unitData2, deployProvince3, true);
						break;
					}
				}
			}
		}

		private void DeployUnit(WorkingRealm Realm, UnitData Unit, WorkingProvince Target, bool ChargeForResources)
		{
			WorkingUnit workingUnit = this.Game.CreateUnit(Realm.ID, Unit);
			ActivePathNode landNode = Target.LandNode;
			if (landNode.CurrentStack == null)
			{
				this.Game.CreateStack(Realm.ID, landNode.ID, true);
			}
			landNode.CurrentStack.AddUnit(workingUnit, false, false);
			if (ChargeForResources)
			{
				Realm.SpendUnitsGold(Realm.UnitPurchaseManager.GetUnitCost(Unit));
			}
		}

		public SovereigntyGame Game;

		private Random RNG;
	}
}
