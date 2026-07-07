using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI
{
	public class AIUtilities
	{
		public AIUtilities(AIPlayer AI, SovereigntyGame Game)
		{
			this.AI = AI;
			this.Game = Game;
		}

		public Dictionary<GameRegion, float> GenerateValueMap()
		{
			List<WorkingProvince> list = new List<WorkingProvince>();
			foreach (WorkingProvince workingProvince in this.AI.Realm.Provinces)
			{
				if (workingProvince.OccupierRealm != this.AI.Realm)
				{
					list.Add(workingProvince);
				}
				foreach (GameRegion gameRegion in workingProvince.GetAllConnectedRegions())
				{
					if (!(gameRegion is WorkingZone))
					{
						WorkingProvince workingProvince2 = gameRegion as WorkingProvince;
						if (workingProvince2.OccupierRealm != this.AI.Realm && workingProvince2.OccupierRealm.DiplomacyManager.GetRelation(this.AI.Realm) != RelationStates.Alliance)
						{
							list.Add(workingProvince2);
						}
					}
				}
			}
			foreach (WorkingProvince workingProvince3 in this.AI.Realm.OccupiedProvinces)
			{
				foreach (GameRegion gameRegion2 in workingProvince3.GetAllConnectedRegions())
				{
					if (!(gameRegion2 is WorkingZone))
					{
						WorkingProvince workingProvince4 = gameRegion2 as WorkingProvince;
						if (workingProvince4.OccupierRealm != this.AI.Realm && workingProvince4.OccupierRealm.DiplomacyManager.GetRelation(this.AI.Realm) != RelationStates.Alliance)
						{
							list.Add(workingProvince4);
						}
					}
				}
			}
			Dictionary<GameRegion, float> dictionary = new Dictionary<GameRegion, float>();
			foreach (WorkingProvince workingProvince5 in list.Distinct<WorkingProvince>())
			{
				dictionary.Add(workingProvince5, this.GetProvinceMilitaryValue(workingProvince5));
			}
			Dictionary<GameRegion, float> dictionary2 = new Dictionary<GameRegion, float>();
			foreach (WorkingProvince workingProvince6 in this.AI.Realm.Provinces)
			{
				if (workingProvince6.OccupierRealm == this.AI.Realm)
				{
					float num = float.MinValue;
					foreach (GameRegion gameRegion3 in workingProvince6.GetAllConnectedRegions())
					{
						if (dictionary.ContainsKey(gameRegion3) && dictionary[gameRegion3] > num)
						{
							num = dictionary[gameRegion3];
						}
					}
					if (num != -3.4028235E+38f)
					{
						dictionary2.Add(workingProvince6, num);
					}
				}
			}
			foreach (WorkingProvince workingProvince7 in this.AI.Realm.OccupiedProvinces)
			{
				float num2 = float.MinValue;
				foreach (GameRegion gameRegion4 in workingProvince7.GetAllConnectedRegions())
				{
					if (dictionary.ContainsKey(gameRegion4) && dictionary[gameRegion4] > num2)
					{
						num2 = dictionary[gameRegion4];
					}
				}
				if (num2 != -3.4028235E+38f)
				{
					dictionary2.Add(workingProvince7, num2);
				}
			}
			bool flag = false;
			while (!flag)
			{
				GameRegion gameRegion5 = null;
				foreach (KeyValuePair<GameRegion, float> keyValuePair in dictionary2.OrderByDescending((KeyValuePair<GameRegion, float> x) => x.Value))
				{
					if (gameRegion5 != null)
					{
						break;
					}
					foreach (GameRegion gameRegion6 in keyValuePair.Key.GetAllConnectedRegions())
					{
						if (!dictionary2.ContainsKey(gameRegion6) && (!(gameRegion6 is WorkingProvince) || (gameRegion6 as WorkingProvince).OccupierRealm == this.AI.Realm))
						{
							if (gameRegion6 is WorkingZone)
							{
								if ((gameRegion6 as WorkingZone).GetAllConnectedRegions().Count((GameRegion x) => x is WorkingProvince && (x as WorkingProvince).OccupierRealm == this.AI.Realm) == 0)
								{
									continue;
								}
							}
							gameRegion5 = keyValuePair.Key;
							break;
						}
					}
				}
				if (gameRegion5 == null)
				{
					break;
				}
				foreach (GameRegion gameRegion7 in gameRegion5.GetAllConnectedRegions())
				{
					if (!dictionary2.ContainsKey(gameRegion7) && (!(gameRegion7 is WorkingProvince) || (gameRegion7 as WorkingProvince).OccupierRealm == this.AI.Realm))
					{
						if (gameRegion7 is WorkingZone)
						{
							if ((gameRegion7 as WorkingZone).GetAllConnectedRegions().Count((GameRegion x) => x is WorkingProvince && (x as WorkingProvince).OccupierRealm == this.AI.Realm) == 0)
							{
								continue;
							}
						}
						dictionary2.Add(gameRegion7, dictionary2[gameRegion5] - 5f);
						if (gameRegion7 is WorkingProvince && (gameRegion7 as WorkingProvince).GetUnmodifiedEconomy() == 0)
						{
							Dictionary<GameRegion, float> dictionary3;
							GameRegion gameRegion8;
							(dictionary3 = dictionary2)[gameRegion8 = gameRegion7] = dictionary3[gameRegion8] + 25f;
						}
					}
				}
			}
			WorkingProvince capitolProvince = this.AI.Realm.CapitolProvince;
			if (capitolProvince != null)
			{
				int num3 = 0;
				if (capitolProvince.LandNode.CurrentStack != null)
				{
					num3 = capitolProvince.LandNode.CurrentStack.Units.Count;
				}
				int num4 = 10 - num3;
				num4 *= 50;
				if (num4 < 0)
				{
					num4 = 0;
				}
				if (!dictionary2.ContainsKey(capitolProvince))
				{
					dictionary2.Add(capitolProvince, 100f);
				}
				Dictionary<GameRegion, float> dictionary4;
				GameRegion gameRegion9;
				(dictionary4 = dictionary2)[gameRegion9 = capitolProvince] = dictionary4[gameRegion9] + (float)num4;
			}
			foreach (WorkingProvince workingProvince8 in this.AI.Realm.Provinces)
			{
				if (!workingProvince8.Occupied && workingProvince8.FortLevel != 0)
				{
					int num5 = 0;
					if (workingProvince8.LandNode.CurrentStack != null)
					{
						num5 = workingProvince8.LandNode.CurrentStack.Units.Count;
					}
					int num6 = 10 - num5;
					num6 *= 25;
					if (num6 < 0)
					{
						num6 = 0;
					}
					if (!dictionary2.ContainsKey(workingProvince8))
					{
						dictionary2.Add(workingProvince8, 0f);
					}
					Dictionary<GameRegion, float> dictionary5;
					GameRegion gameRegion10;
					(dictionary5 = dictionary2)[gameRegion10 = workingProvince8] = dictionary5[gameRegion10] + (float)num6;
				}
			}
			return dictionary2;
		}

		public float GetProvinceMilitaryValue(WorkingProvince Province)
		{
			float num = 0f;
			num += this.GetProvinceLust(Province);
			num += this.GetRealmLust(Province.OwnerRealm);
			num += this.GetProvinceThreat(Province);
			return num + this.GetRealmThreat(Province.OwnerRealm);
		}

		private float GetRealmThreat(WorkingRealm Realm)
		{
			float num = 0f;
			float num2 = Realm.DiplomacyManager.GetDisposition(this.AI.Realm);
			num2 *= -1f;
			num += num2 / 5f;
			float num3 = Realm.Units.Sum((WorkingUnit x) => x.GetValue());
			num3 /= 2500f;
			return num + num3;
		}

		private float GetProvinceThreat(WorkingProvince Province)
		{
			float num = 0f;
			if (Province.LandNode.CurrentStack != null)
			{
				float num2 = Province.LandNode.CurrentStack.Units.Sum((WorkingUnit x) => x.GetValue());
				num2 /= 500f;
				num += num2;
			}
			return num + (float)(2 * Province.FortLevel);
		}

		private float GetRealmLust(WorkingRealm Realm)
		{
			float num = 0f;
			float num2 = 0f;
			foreach (WorkingProvince workingProvince in Realm.Provinces)
			{
				num += 1f;
				num2 += this.GetProvinceLust(workingProvince);
			}
			float num3 = num2 / num;
			float num4 = this.AI.Realm.DiplomacyManager.GetDisposition(Realm);
			if (num4 < 0f)
			{
				num4 *= -1f;
			}
			num3 += num4 / 5f;
			if (this.AI.Realm.DiplomacyManager.GetRelation(Realm) == RelationStates.War)
			{
				num3 += 10f;
			}
			return num3;
		}

		private float GetProvinceLust(WorkingProvince Province)
		{
			float num = 0f;
			num += (float)(Province.GetImprovementLevel() * 2);
			if (Province.Landmark != "")
			{
				num += 10f;
			}
			num += (float)(Province.ResearchPoints / 5);
			if (Province.Resource != null && this.AI.Realm.UnitPurchaseManager.ResourceIsUseful(Province.Resource))
			{
				int num2 = 30 - this.AI.Realm.GetStockpiledResource(Province.Resource);
				if (num2 < 0)
				{
					num2 = 0;
				}
				num2 /= 5;
				num += (float)num2;
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Resource == Province.Resource) == 0)
				{
					num += 5f;
				}
			}
			int num3 = 0;
			foreach (GameRegion gameRegion in Province.GetAllConnectedRegions())
			{
				if (!(gameRegion is WorkingZone) && (gameRegion as WorkingProvince).OwnerID == this.AI.RealmID)
				{
					num3++;
				}
			}
			if (num3 > 4)
			{
				num += 10f;
			}
			else if (num3 > 2)
			{
				num += 5f;
			}
			return num;
		}

		private AIPlayer AI;

		private SovereigntyGame Game;
	}
}
