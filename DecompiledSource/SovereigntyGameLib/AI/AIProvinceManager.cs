using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI
{
	public class AIProvinceManager
	{
		public AIProvinceManager(AIPlayer AI, SovereigntyGame Game)
		{
			this.AI = AI;
			this.Game = Game;
		}

		internal void Construct()
		{
			using (IEnumerator<WorkingProvince> enumerator = this.AI.Realm.Provinces.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					WorkingProvince Province = enumerator.Current;
					if (!Province.Occupied)
					{
						List<BuildingEffect> list = BuildingEffect.GetAllEffects(Province.Game);
						list = (from x in list
							where this.ShouldDisplay(x, Province)
							orderby this.Game.GameCore.Data.BuildingAffinities[x.BuildingName].GetAffinity(this.AI.Realm.Name)
							select x).Take(10).ToList<BuildingEffect>();
						if (list.Count != 0)
						{
							list = list.Where((BuildingEffect x) => Province.CanBuild(x)).ToList<BuildingEffect>();
							if (list.Count != 0)
							{
								int num = list.Sum((BuildingEffect x) => this.Game.GameCore.Data.BuildingAffinities[x.BuildingName].GetAffinity(this.AI.Realm.Name));
								int num2 = this.AI.RNG.Next(num) + 1;
								int i = 0;
								int num3 = -1;
								while (i < num2)
								{
									num3++;
									i += this.Game.GameCore.Data.BuildingAffinities[list[num3].BuildingName].GetAffinity(this.AI.Realm.Name);
								}
								AIAction aiaction = new AIAction(AIActionTypes.ConstructBuilding);
								aiaction.Province = Province;
								aiaction.Building = list[num3];
								this.AI.SetAction(aiaction);
							}
						}
					}
				}
			}
		}

		private bool ShouldDisplay(BuildingEffect Building, WorkingProvince Province)
		{
			if (Province.Game.GameCore.Data.BuildingAffinities[Building.BuildingName].GetAffinity(Province.OwnerRealm.Name) == 0)
			{
				return false;
			}
			if (Building.Data.Tier > Province.CurrentEconomy)
			{
				return false;
			}
			if (!Building.CanBuildInProvince(Province.OwnerRealm, Province))
			{
				return false;
			}
			if (Building.BuildingName == "Fort")
			{
				if (Province.Buildings.Count((BuildingEffect x) => x.BuildingName == "Fort") >= Province.BaseFortLevel)
				{
					return false;
				}
			}
			return true;
		}

		internal void RepairProvinces()
		{
			Random random = new Random();
			Dictionary<WorkingProvince, int> dictionary = new Dictionary<WorkingProvince, int>();
			List<WorkingProvince> list = new List<WorkingProvince>();
			foreach (WorkingProvince workingProvince in this.AI.Realm.Provinces)
			{
				if (!workingProvince.Occupied && workingProvince.EconomyDamaged)
				{
					dictionary.Add(workingProvince, this.GetProvinceRepairValue(workingProvince));
				}
			}
			foreach (KeyValuePair<WorkingProvince, int> keyValuePair in dictionary.OrderByDescending((KeyValuePair<WorkingProvince, int> x) => x.Value))
			{
				int improveCost = keyValuePair.Key.GetImproveCost(this.AI.Realm);
				if (this.AI.Realm.Gold >= improveCost && random.Next(100) < 75)
				{
					this.AI.Realm.Gold.Value -= improveCost;
					list.Add(keyValuePair.Key);
				}
			}
			if (list.Count > 0)
			{
				AIAction aiaction = new AIAction(AIActionTypes.ImproveProvinces);
				aiaction.ProvinceList = list;
				this.AI.SetAction(aiaction);
			}
		}

		internal void UpgradeProvinces()
		{
			Random random = new Random();
			Dictionary<WorkingProvince, int> dictionary = new Dictionary<WorkingProvince, int>();
			List<WorkingProvince> list = new List<WorkingProvince>();
			foreach (WorkingProvince workingProvince in this.AI.Realm.Provinces)
			{
				if (!workingProvince.Occupied && !workingProvince.EconomyDamaged)
				{
					dictionary.Add(workingProvince, this.GetProvinceRepairValue(workingProvince));
				}
			}
			foreach (KeyValuePair<WorkingProvince, int> keyValuePair in dictionary.OrderByDescending((KeyValuePair<WorkingProvince, int> x) => x.Value))
			{
				int improveCost = keyValuePair.Key.GetImproveCost(this.AI.Realm);
				if (this.AI.Realm.Gold >= improveCost && random.Next(100) < 50)
				{
					this.AI.Realm.Gold.Value -= improveCost;
					list.Add(keyValuePair.Key);
				}
			}
			if (list.Count > 0)
			{
				AIAction aiaction = new AIAction(AIActionTypes.ImproveProvinces);
				aiaction.ProvinceList = list;
				this.AI.SetAction(aiaction);
			}
		}

		private int GetProvinceRepairValue(WorkingProvince Province)
		{
			int num = 0;
			float num2 = (float)Province.GetImproveCost(this.AI.Realm);
			float num3 = (float)this.Game.EconomyController.GetProvinceEconValue(Province, this.AI.Realm.CapitolProvince);
			int num4 = (int)(num2 / num3);
			num -= num4;
			if (Province.CurrentEconomy == 0)
			{
				num += 100;
			}
			if (Province.PlaguePossible())
			{
				num += 100;
			}
			return num;
		}

		public AIPlayer AI;

		public SovereigntyGame Game;
	}
}
