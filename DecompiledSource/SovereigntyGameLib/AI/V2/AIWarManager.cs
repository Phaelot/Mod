// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.AI.V2.AIWarManager
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.AI.V2
{
	public class AIWarManager
	{
		public AIPlayer AI;

		public Dictionary<int, InvasionTargetData> InvasionTargets;

		public Dictionary<string, int> LustModifiers;

		public Dictionary<int, WarData> Wars;

		public Dictionary<int, RealmWarStatus> WarStatuses;

		private float WarThreshold = 20f;

		public float WarJoinThreshold = 15f;

		private int Cooldown;

		private int LastProvinceAdded;

		private float OurStrengthValue;

		private float RealmIncome;

		private float RealmExpenses;

		private Dictionary<WorkingRealm, float> RealmStrengthValues;

		public AIWarManager(AIPlayer AI)
		{
			this.AI = AI;
			InvasionTargets = new Dictionary<int, InvasionTargetData>();
			LustModifiers = new Dictionary<string, int>();
			Wars = new Dictionary<int, WarData>();
			AI.Game.AllianceController.OnWarDeclared += AllianceController_OnWarDeclared;
			AI.Game.AllianceController.OnWarEnded += AllianceController_OnWarEnded;
			WarStatuses = new Dictionary<int, RealmWarStatus>();
			foreach (WorkingRealm value in AI.Game.AllRealms.Values)
			{
				if (value != AI.Game.RebelRealm)
				{
					WarStatuses.Add(value.ID, new RealmWarStatus(AI.Game, value));
				}
			}
		}

		private void AllianceController_OnWarEnded(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			WorkingRealm workingRealm = null;
			if (Realm1 == AI.Realm)
			{
				workingRealm = Realm2;
			}
			if (Realm2 == AI.Realm)
			{
				workingRealm = Realm1;
			}
			if (workingRealm != null && Wars.ContainsKey(workingRealm.ID))
			{
				Wars[workingRealm.ID].Dispose();
				Wars.Remove(workingRealm.ID);
			}
		}

		private void AllianceController_OnWarDeclared(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			WorkingRealm workingRealm = null;
			if (Realm1 == AI.Realm)
			{
				workingRealm = Realm2;
			}
			if (Realm2 == AI.Realm)
			{
				workingRealm = Realm1;
			}
			if (workingRealm != null && !Wars.ContainsKey(workingRealm.ID))
			{
				Wars.Add(workingRealm.ID, new WarData(AI.Game, workingRealm, AI));
			}
		}

		internal void Dispose()
		{
			AI.Game.AllianceController.OnWarDeclared -= AllianceController_OnWarDeclared;
			AI.Game.AllianceController.OnWarEnded -= AllianceController_OnWarEnded;
		}

		private int GetPeaceEnemiesValue()
		{
			int num = AI.Realm.Enemies.Count - 2;
			return num * 2;
		}

		private int GetPeaceDamageValue()
		{
			int num = AI.Realm.Provinces.Count((WorkingProvince x) => x.GetUnmodifiedEconomy() == 0);
			int num2 = AI.Realm.Provinces.Count / 5;
			num -= num2;
			if (num < 0)
			{
				num = 0;
			}
			return num * 5;
		}

		private int GetPeaceIncomeValue()
		{
			int num = AI.Game.EconomyController.GetRealmTotalIncome(AI.Realm) - AI.Game.EconomyController.GetTotalExpenses(AI.Realm);
			num /= 400;
			num *= -1;
			if (num < -20)
			{
				num = -20;
			}
			if (num > 20)
			{
				num = 20;
			}
			return num;
		}

		private float GetPeaceStrategicValue(WorkingRealm Enemy)
		{
			if (VictoryIsPossible(Enemy))
			{
				return -10f;
			}
			if (DefeatIsLikely(Enemy, UseCachedValues: false))
			{
				return 10f;
			}
			return 0f;
		}

		public bool DefeatIsLikely(WorkingRealm Realm, bool UseCachedValues)
		{
			float num = 0f;
			if (AI.Game.IgnoreHumanPlayer && Realm == AI.Game.PlayerRealm)
			{
				return false;
			}
			float num2 = 0f;
			float num3 = 0f;
			if (UseCachedValues)
			{
				num2 = OurStrengthValue;
				num3 = RealmStrengthValues[Realm];
			}
			else
			{
				num2 = AI.Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
				num3 = Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			}
			foreach (WorkingRealm ally in AI.Realm.Allies)
			{
				num2 = ((!UseCachedValues) ? (num2 + ally.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f)) : (num2 + RealmStrengthValues[ally] / 4f));
			}
			foreach (WorkingRealm ally2 in Realm.Allies)
			{
				num3 = ((!UseCachedValues) ? (num3 + ally2.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f)) : (num3 + RealmStrengthValues[ally2] / 4f));
			}
			num3 /= 3f;
			num = num3 / num2;
			num -= (float)AI.Game.Data.AITraits[AI.Realm.Name].Warmonger * 0.02f;
			num += (float)AI.Realm.Enemies.Count * 0.2f;
			float num4 = 0f;
			num4 = ((!UseCachedValues) ? ((float)(AI.Game.EconomyController.GetRealmTotalIncome(AI.Realm) - AI.Game.EconomyController.GetTotalExpenses(AI.Realm))) : RealmIncome);
			if (num4 < 0f)
			{
				num -= num4 * 0.0001f;
			}
			int count = AI.Realm.Provinces.Count;
			count = Math.Max(0, 3 - count) + 1;
			num += 0.3f * (float)count;
			return num > 1f;
		}

		public bool VictoryIsPossible(WorkingRealm Realm)
		{
			float num = 0f;
			if (AI.Game.IgnoreHumanPlayer && Realm == AI.Game.PlayerRealm)
			{
				return false;
			}
			float num2 = AI.Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			float num3 = Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			foreach (WorkingRealm ally in AI.Realm.Allies)
			{
				num2 += ally.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f);
			}
			foreach (WorkingRealm ally2 in Realm.Allies)
			{
				num3 += ally2.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f);
			}
			num = num2 / num3;
			float disposition = AI.Realm.DiplomacyManager.GetDisposition(Realm);
			if (disposition < 0f)
			{
				disposition *= -1f;
				num += disposition * 0.005f;
			}
			num += (float)AI.Game.Data.AITraits[AI.Realm.Name].Warmonger * 0.01f;
			num -= (float)AI.Realm.Enemies.Count * 0.2f;
			float num4 = AI.Game.EconomyController.GetRealmTotalIncome(AI.Realm) - AI.Game.EconomyController.GetTotalExpenses(AI.Realm);
			num += num4 * 0.0001f;
			return num > 1f;
		}

		public int GetPeaceDesire(WorkingRealm Enemy)
		{
			if (Enemy.ID == AI.Realm.ID)
			{
				return GetPeaceDesire(AI.Game.PlayerRealm);
			}
			if (!Wars.ContainsKey(Enemy.ID))
			{
				throw new Exception("Getting peace desire failed - war does not exist\n\nAI: " + AI.Realm.ID + "\nTarget: " + Enemy.ID);
			}
			float num = 0f;
			num += (float)GetPeaceIncomeValue();
			num += (float)GetPeaceDamageValue();
			num -= (float)Wars[Enemy.ID].GetWarScore();
			num -= GetWarValue(Enemy) / 10f;
			num += (float)GetPeaceEnemiesValue();
			num += GetPeaceStrategicValue(Enemy);
			num += AI.Realm.DiplomacyManager.GetDisposition(Enemy) / 5f;
			if (num > 100f)
			{
				num = 100f;
			}
			if (num < -100f)
			{
				num = -100f;
			}
			return (int)num;
		}

		public float GetWarValue(WorkingRealm Realm)
		{
			float num = 0f;
			num += (float)(Realm.GetLandmarkCount() * 5);
			foreach (ResourceData item in Realm.GetResourcesInRealm())
			{
				if (AI.Realm.UnitPurchaseManager.ResourceIsUseful(item))
				{
					num += 2f;
				}
			}
			num -= AI.Realm.DiplomacyManager.GetDisposition(Realm);
			num -= Realm.DiplomacyManager.GetDisposition(AI.Realm) / 2f;
			num += (float)AI.Game.Data.AITraits[AI.Realm.Name].Warmonger;
			num -= (float)AI.Game.Data.AITraits[AI.Realm.Name].Diplomat;
			num += (float)Realm.Provinces.Sum((WorkingProvince x) => x.AILust);
			foreach (WorkingProvince province in Realm.Provinces)
			{
				num += (float)GetLustModifier(province.Name);
			}
			return num - (float)(Realm.Provinces.Count((WorkingProvince x) => x.GetUnmodifiedEconomy() == 0) * 5);
		}

		public void ModifyProvinceLust(string ProvinceName, int LustModifier)
		{
			if (!LustModifiers.ContainsKey(ProvinceName))
			{
				LustModifiers.Add(ProvinceName, 0);
			}
			LustModifiers[ProvinceName] += LustModifier;
		}

		public int GetLustModifier(string ProvinceName)
		{
			int value = 0;
			LustModifiers.TryGetValue(ProvinceName, out value);
			return value;
		}

		internal void UpdateInvasionTargets()
		{
			foreach (int item in InvasionTargets.Keys.ToList())
			{
				InvasionTargets[item].TurnsLeft--;
				if (InvasionTargets[item].TurnsLeft == 0)
				{
					InvasionTargets.Remove(item);
				}
				else if (!AI.Realm.Restrictions.CanDeclareWar(AI.Game.AllRealms[item]))
				{
					InvasionTargets.Remove(item);
				}
			}
			LastProvinceAdded++;
		}

		internal void MakePeaceOffers()
		{
			AI.Log("");
			AI.Log("War manager updating (peace phase)");
			foreach (WorkingRealm enemy in AI.Realm.Enemies)
			{
				if (enemy == AI.Game.RebelRealm)
				{
					continue;
				}
				AI.Log("  Considering " + enemy.Name);
				if (!AI.Realm.Restrictions.CanOfferPeace(enemy))
				{
					AI.Log("  Not allowed to offer peace");
					continue;
				}
				int peaceDesire = GetPeaceDesire(enemy);
				int num = AI.RNG.Next(100);
				AI.Log("  Peace desire is " + peaceDesire + ", Rolled " + num);
				if (num > peaceDesire)
				{
					AI.Log("  Offering Peace Treaty");
					TradeOfferList tradeOfferList = new TradeOfferList(AI.Game, null, IsOffer: false);
					tradeOfferList.Treaty = TreatyTypes.Peace;
					if (AI.TradeManager.AttemptTrade(tradeOfferList, enemy))
					{
						break;
					}
				}
				else
				{
					AI.Log("  Not offering Peace Treaty");
				}
			}
		}

		public bool WarLimitReached()
		{
			int num = 0;
			int count = AI.Realm.Provinces.Count;
			num = ((count < 5) ? 1 : ((count < 10) ? 2 : ((count < 20) ? 3 : ((count >= 35) ? 5 : 4))));
			return AI.Realm.Enemies.Count + InvasionTargets.Count >= num;
		}

		private bool HasSharedBorder(WorkingRealm Realm)
		{
			foreach (WorkingProvince province in AI.Realm.Provinces)
			{
				foreach (GameRegion allConnectedRegion in province.GetAllConnectedRegions())
				{
					if (allConnectedRegion is WorkingProvince && (allConnectedRegion as WorkingProvince).OwnerRealm == Realm)
					{
						return true;
					}
					if (!(allConnectedRegion is WorkingZone))
					{
						continue;
					}
					foreach (GameRegion allConnectedRegion2 in allConnectedRegion.GetAllConnectedRegions())
					{
						_ = allConnectedRegion2;
						if (allConnectedRegion is WorkingProvince && (allConnectedRegion as WorkingProvince).OwnerRealm == Realm)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public void ForcePeaceOffer(WorkingRealm Enemy)
		{
			TradeOfferList tradeOfferList = new TradeOfferList(AI.Game, null, IsOffer: false);
			tradeOfferList.Treaty = TreatyTypes.Peace;
			AI.TradeManager.AttemptTrade(tradeOfferList, Enemy, Immediate: true);
		}

		public List<Tuple<string, int>> GetPeaceDesireBreakdown(WorkingRealm Enemy)
		{
			List<Tuple<string, int>> list = new List<Tuple<string, int>>();
			list.Add(new Tuple<string, int>("Income:", GetPeaceIncomeValue()));
			list.Add(new Tuple<string, int>("Damaged provinces:", GetPeaceDamageValue()));
			if (AI.Realm == AI.Game.PlayerRealm)
			{
				list.AddRange(Enemy.AIPlayer.WarManager.Wars[AI.Realm.ID].GetWarScoreBreakDown());
			}
			else
			{
				foreach (Tuple<string, int> item in Wars[Enemy.ID].GetWarScoreBreakDown())
				{
					list.Add(new Tuple<string, int>(item.Item1, item.Item2 * -1));
				}
			}
			list.Add(new Tuple<string, int>("Value of war:", -1 * ((int)GetWarValue(Enemy) / 10)));
			list.Add(new Tuple<string, int>("Other wars:", GetPeaceEnemiesValue()));
			list.Add(new Tuple<string, int>("Expecting to win:", (int)GetPeaceStrategicValue(Enemy)));
			list.Add(new Tuple<string, int>("Disposition:", (int)(AI.Realm.DiplomacyManager.GetDisposition(Enemy) / 5f)));
			return list;
		}

		internal bool ShouldJoinWar(WorkingRealm Realm, WorkingRealm Target)
		{
			if (!AI.Realm.Restrictions.CanDeclareWar(Target))
			{
				return false;
			}
			if (GetWarValue(Target) < WarJoinThreshold)
			{
				return false;
			}
			if (!VictoryIsPossible(Target))
			{
				return false;
			}
			return true;
		}

		internal void DeclareWars()
		{
			AI.Log("");
			AI.Log("War manager updating (new war phase)");
			if (AI.Game.TurnController.TurnNumber < 3)
			{
				AI.Log("  Cannot declare wars, not on turn 3 yet");
				return;
			}
			if (Cooldown > 0)
			{
				AI.Log("  Cannot declare any wars, cooldown in effect");
				Cooldown--;
				return;
			}
			Cooldown = AI.RNG.Next(3) + 2;
			if (AI.Realm.HasStatus("HolyArbiter"))
			{
				AI.Log("  Cannot declare any wars, Holy Arbiter in effect");
				return;
			}
			if (WarLimitReached())
			{
				AI.Log("  Cannot declare any wars, already in too many");
				return;
			}
			List<WorkingRealm> source = AI.Game.AllRealms.Values.Where((WorkingRealm x) => AI.Realm.Restrictions.CanDeclareWar(x)).ToList();
			source = source.Where((WorkingRealm x) => !InvasionTargets.ContainsKey(x.ID) && HasSharedBorder(x)).ToList();
			AI.Log("  Possible targets: " + source.Count);
			new Dictionary<WorkingRealm, float>();
			foreach (WorkingRealm item in source)
			{
				AI.Log("  Considering " + item.Name);
				List<WarReasonData> list = new List<WarReasonData>();
				foreach (KeyValuePair<WarReasons, WarReasonData> item2 in WarStatuses[item.ID].Status)
				{
					if (ReasonAboveThreshold(item2.Key, item2.Value.CurrentValue, item))
					{
						list.Add(item2.Value);
					}
				}
				if (list.Count == 0)
				{
					continue;
				}
				foreach (WarReasonData item3 in list)
				{
					List<WarGoalData> possibleWarGoals = GetPossibleWarGoals(item, item3);
					foreach (WarGoalData item4 in possibleWarGoals)
					{
						if (VictoryIsPossible(item, item4))
						{
							if (!AI.Realm.CodeOfWar || AI.Realm.HasStatus("IgnoreCode"))
							{
								InvasionTargets.Add(item.ID, new InvasionTargetData(item4, 5));
							}
							else
							{
								AIActionDeclareWar aIActionDeclareWar = AI.ActionManager.CreateAction<AIActionDeclareWar>();
								aIActionDeclareWar.Target = item;
								AI.ActionManager.AddAction(aIActionDeclareWar);
							}
							Cooldown = AI.RNG.Next(3) + 2;
							return;
						}
					}
				}
			}
		}

		private bool VictoryIsPossible(WorkingRealm Realm, WarGoalData Goal)
		{
			return VictoryIsPossible(Realm);
		}

		private List<WarGoalData> GetPossibleWarGoals(WorkingRealm Realm, WarReasonData ReasonData)
		{
			List<WarGoalData> list = new List<WarGoalData>();
			switch (ReasonData.ReasonType)
			{
				case WarReasons.Hatred:
				case WarReasons.Spying:
					{
						WarGoalData warGoalData2 = new WarGoalData();
						warGoalData2.GoalType = WarGoalTypes.LootRealm;
						warGoalData2.NumericTarget = AI.RNG.Next(1000) + 1000;
						list.Add(warGoalData2);
						int AverageEconomny = (int)Realm.Provinces.Average((WorkingProvince x) => x.CurrentEconomy);
						List<WorkingProvince> list3 = Realm.Provinces.Where((WorkingProvince x) => x.CurrentEconomy >= AverageEconomny && ProvinceAdjacentToRealm(x, AI.Realm)).ToList();
						if (list3.Count == 0)
						{
							break;
						}
						List<List<WorkingProvince>> allCombos2 = GetAllCombos(list3);
						foreach (List<WorkingProvince> item in allCombos2)
						{
							warGoalData2 = new WarGoalData();
							warGoalData2.GoalType = WarGoalTypes.CaptureProvinces;
							foreach (WorkingProvince item2 in item)
							{
								warGoalData2.ProvinceTargets.Add(item2.ID);
							}
							list.Add(warGoalData2);
						}
						warGoalData2 = new WarGoalData();
						warGoalData2.GoalType = WarGoalTypes.EliminateTargets;
						foreach (WorkingUnit item3 in Realm.Units.Where((WorkingUnit x) => !x.IsPrisoner && (x.Rank == UnitRanks.Elite || x.Rank == UnitRanks.Unique)))
						{
							if (AI.RNG.Next(100) >= 40)
							{
								warGoalData2.UnitTargets.Add(item3.ID);
							}
						}
						if (warGoalData2.UnitTargets.Count > 0)
						{
							list.Add(warGoalData2);
						}
						break;
					}
				case WarReasons.LandExpansion:
					{
						int AverageEconomny2 = (int)Realm.Provinces.Average((WorkingProvince x) => x.CurrentEconomy);
						List<WorkingProvince> list6 = Realm.Provinces.Where((WorkingProvince x) => x.CurrentEconomy >= AverageEconomny2 && ProvinceAdjacentToRealm(x, AI.Realm)).ToList();
						if (list6.Count == 0)
						{
							break;
						}
						List<List<WorkingProvince>> allCombos5 = GetAllCombos(list6);
						foreach (List<WorkingProvince> item4 in allCombos5)
						{
							WarGoalData warGoalData6 = new WarGoalData();
							warGoalData6.GoalType = WarGoalTypes.CaptureProvinces;
							foreach (WorkingProvince item5 in item4)
							{
								warGoalData6.ProvinceTargets.Add(item5.ID);
							}
							list.Add(warGoalData6);
						}
						break;
					}
				case WarReasons.LandReclaim:
					{
						List<WorkingProvince> list4 = Realm.Provinces.Where((WorkingProvince x) => x.OwnerHistory.RealmHasClaim(AI.Realm.ID)).ToList();
						if (list4.Count == 0)
						{
							break;
						}
						List<List<WorkingProvince>> allCombos3 = GetAllCombos(list4);
						foreach (List<WorkingProvince> item6 in allCombos3)
						{
							WarGoalData warGoalData3 = new WarGoalData();
							warGoalData3.GoalType = WarGoalTypes.CaptureProvinces;
							foreach (WorkingProvince item7 in item6)
							{
								warGoalData3.ProvinceTargets.Add(item7.ID);
							}
							list.Add(warGoalData3);
						}
						break;
					}
				case WarReasons.Looting:
					{
						WarGoalData warGoalData4 = new WarGoalData();
						warGoalData4.GoalType = WarGoalTypes.LootRealm;
						warGoalData4.NumericTarget = ReasonData.QuantityValue;
						list.Add(warGoalData4);
						break;
					}
				case WarReasons.Resources:
					{
						List<WorkingProvince> list5 = Realm.Provinces.Where((WorkingProvince x) => ReasonData.ProvinceIDs.Contains(x.Value)).ToList();
						if (list5.Count == 0)
						{
							break;
						}
						List<List<WorkingProvince>> allCombos4 = GetAllCombos(list5);
						foreach (List<WorkingProvince> item8 in allCombos4)
						{
							WarGoalData warGoalData5 = new WarGoalData();
							warGoalData5.GoalType = WarGoalTypes.CaptureProvinces;
							foreach (WorkingProvince item9 in item8)
							{
								warGoalData5.ProvinceTargets.Add(item9.ID);
							}
							list.Add(warGoalData5);
						}
						break;
					}
				case WarReasons.StealLandmark:
					{
						List<WorkingProvince> list2 = Realm.Provinces.Where((WorkingProvince x) => ReasonData.ProvinceIDs.Contains(x.Value)).ToList();
						if (list2.Count == 0)
						{
							break;
						}
						List<List<WorkingProvince>> allCombos = GetAllCombos(list2);
						foreach (List<WorkingProvince> item10 in allCombos)
						{
							WarGoalData warGoalData = new WarGoalData();
							warGoalData.GoalType = WarGoalTypes.CaptureProvinces;
							foreach (WorkingProvince item11 in item10)
							{
								warGoalData.ProvinceTargets.Add(item11.ID);
							}
							list.Add(warGoalData);
						}
						break;
					}
			}
			return list;
		}

		public List<List<T>> GetAllCombos<T>(List<T> list)
		{
			int num = (int)Math.Pow(2.0, list.Count) - 1;
			List<List<T>> list2 = new List<List<T>>();
			for (int i = 1; i < num + 1; i++)
			{
				list2.Add(new List<T>());
				for (int j = 0; j < list.Count; j++)
				{
					if ((i >> j) % 2 != 0)
					{
						list2.Last().Add(list[j]);
					}
				}
			}
			return list2;
		}

		private bool ProvinceAdjacentToRealm(WorkingProvince Province, WorkingRealm Realm)
		{
			foreach (GameRegion allConnectedRegion in Province.GetAllConnectedRegions())
			{
				if (allConnectedRegion is WorkingProvince && (allConnectedRegion as WorkingProvince).OwnerRealm == Realm)
				{
					return true;
				}
			}
			return false;
		}

		private bool ReasonAboveThreshold(WarReasons Reasons, float Value, WorkingRealm TargetRealm)
		{
			int num = 10;
			if (TargetRealm.HasStatus("HolyArbiter"))
			{
				num = 30;
			}
			return Value > (float)num;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(InvasionTargets.Count);
			foreach (KeyValuePair<int, InvasionTargetData> invasionTarget in InvasionTargets)
			{
				w.Write(invasionTarget.Key);
				invasionTarget.Value.Save(w);
			}
			w.Write(LustModifiers.Count);
			foreach (KeyValuePair<string, int> lustModifier in LustModifiers)
			{
				w.Write(lustModifier.Key);
				w.Write(lustModifier.Value);
			}
			w.Write(Wars.Count);
			foreach (KeyValuePair<int, WarData> war in Wars)
			{
				w.Write(war.Key);
				war.Value.Save(w);
			}
			w.Write(Cooldown);
			w.Write(WarStatuses.Count);
			foreach (KeyValuePair<int, RealmWarStatus> warStatus in WarStatuses)
			{
				warStatus.Value.Save(w);
			}
			w.Write(LastProvinceAdded);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			InvasionTargets = new Dictionary<int, InvasionTargetData>();
			LustModifiers = new Dictionary<string, int>();
			Wars = new Dictionary<int, WarData>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int key = r.ReadInt32();
				InvasionTargetData value = new InvasionTargetData(r, SaveVersion);
				InvasionTargets.Add(key, value);
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				LustModifiers.Add(r.ReadString(), r.ReadInt32());
			}
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				int key2 = r.ReadInt32();
				WarData value2 = new WarData(AI.Game, AI, r, SaveVersion);
				Wars.Add(key2, value2);
			}
			Cooldown = r.ReadInt32();
			if (SaveVersion >= 58)
			{
				WarStatuses.Clear();
				num = r.ReadInt32();
				for (int l = 0; l < num; l++)
				{
					RealmWarStatus realmWarStatus = new RealmWarStatus(AI.Game, null);
					realmWarStatus.Load(r, SaveVersion);
					WarStatuses.Add(realmWarStatus.RealmID, realmWarStatus);
				}
				LastProvinceAdded = r.ReadInt32();
			}
		}

		internal void UpdateWarStatus()
		{
			OurStrengthValue = AI.Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			RealmStrengthValues = new Dictionary<WorkingRealm, float>();
			foreach (WorkingRealm value in AI.Game.AllRealms.Values)
			{
				RealmStrengthValues.Add(value, value.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue()));
			}
			RealmExpenses = AI.Game.EconomyController.GetTotalExpenses(AI.Realm);
			RealmIncome = (float)AI.Game.EconomyController.GetRealmTotalIncome(AI.Realm) - RealmExpenses;
			Dictionary<ResourceData, int> dictionary = new Dictionary<ResourceData, int>();
			foreach (ResourceData value2 in AI.Game.Data.Resources.Values)
			{
				dictionary.Add(value2, AI.ResourcesManager.GetResourceDesiredStockpile(value2));
			}
			int expansionDesire = GetExpansionDesire();
			float num = (float)expansionDesire * 0.01f;
			foreach (RealmWarStatus value3 in WarStatuses.Values)
			{
				WorkingRealm workingRealm = AI.Game.AllRealms[value3.RealmID];
				if (workingRealm.RealmIsDead)
				{
					continue;
				}
				value3.Status[WarReasons.Hatred].CurrentValue += AI.Realm.DiplomacyManager.GetDisposition(workingRealm) * 0.075f;
				if (value3.Status[WarReasons.Hatred].CurrentValue < 0f)
				{
					value3.Status[WarReasons.Hatred].CurrentValue = 0f;
				}
				foreach (ResourceData value4 in AI.Game.Data.Resources.Values)
				{
					int num2 = dictionary[value4];
					int stockpiledResource = AI.Realm.GetStockpiledResource(value4);
					if (workingRealm.GetResourceIncome(value4) == 0)
					{
						value3.SetResourceStatus(value4, 0f);
					}
					else if (stockpiledResource >= num2)
					{
						value3.ModifyResourceStatus(value4, -1f);
					}
					else
					{
						value3.ModifyResourceStatus(value4, num2 - stockpiledResource);
					}
				}
				value3.Status[WarReasons.Resources].CurrentValue = value3.ResourceStatus.Sum((KeyValuePair<ResourceData, float> x) => x.Value);
				value3.Status[WarReasons.Spying].CurrentValue -= 1f;
				if (AI.EspionageManager.RecentSpies.Contains(workingRealm.ID))
				{
					AI.EspionageManager.RecentSpies.Remove(workingRealm.ID);
					value3.Status[WarReasons.Spying].CurrentValue += 15f;
				}
				if (value3.Status[WarReasons.Spying].CurrentValue < 0f)
				{
					value3.Status[WarReasons.Spying].CurrentValue = 0f;
				}
				float num3 = 0f;
				float num4 = 0f;
				foreach (WorkingProvince province in workingRealm.Provinces)
				{
					num3 += (float)province.CurrentEconomy;
					num4 += (float)AI.Personality.GetTerrainAffinity(province);
				}
				num3 /= (float)workingRealm.Provinces.Count;
				num4 /= (float)workingRealm.Provinces.Count;
				float num5 = num3 + num4 / 2f;
				num5 *= num;
				value3.Status[WarReasons.LandExpansion].CurrentValue = num5;
				int num6 = 0;
				foreach (WorkingProvince province2 in workingRealm.Provinces)
				{
					int realmClaimAge = province2.OwnerHistory.GetRealmClaimAge(AI.Realm.ID);
					if (realmClaimAge >= 0)
					{
						num6 += (30 - realmClaimAge) / 2;
					}
				}
				value3.Status[WarReasons.LandReclaim].CurrentValue = num6;
				num6 = 0;
				foreach (WorkingProvince province3 in workingRealm.Provinces)
				{
					if (province3.HasLandmark())
					{
						num6 += Math.Abs(AI.Personality.GetLandmarkAffinity(province3.Landmark));
					}
				}
				value3.Status[WarReasons.StealLandmark].CurrentValue = num6;
				int num7 = (int)(AI.Realm.GetTotalGold() / Math.Abs(RealmExpenses));
				num6 = 20 - num7;
				if (num6 > 0)
				{
					int num8 = 0;
					foreach (WorkingProvince province4 in workingRealm.Provinces)
					{
						if (!province4.Occupied)
						{
							num8 += province4.CurrentLoot;
						}
					}
					num6 += num8 / 1000;
				}
				else
				{
					num6 = 0;
				}
				value3.Status[WarReasons.Looting].CurrentValue = num6;
			}
		}

		private int GetExpansionDesire()
		{
			int num = 0;
			num += Math.Min(LastProvinceAdded / 2, 20);
			int count = AI.Realm.Provinces.Count;
			if (count < 10)
			{
				num += 5;
			}
			if (count < 5)
			{
				num += 10;
			}
			if (count < 2)
			{
				num += 20;
			}
			int num2 = 0;
			foreach (WorkingProvince province in AI.Realm.Provinces)
			{
				if (province.Buildings.Count >= province.CurrentEconomy)
				{
					num2++;
				}
			}
			float num3 = (float)num2 / (float)count;
			num3 -= 0.5f;
			num3 *= 25f;
			num += (int)num3;
			float num4 = 0f;
			float num5 = 0f;
			foreach (WorkingProvince province2 in AI.Realm.Provinces)
			{
				num5 += 1f;
				num4 += (float)AI.Personality.GetTerrainAffinity(province2);
			}
			if (num5 > 0f)
			{
				float num6 = num4 / num5;
				num6 = 100f - num6;
				num6 -= 50f;
				if (num6 < 0f)
				{
					num6 = 0f;
				}
				num += (int)(num6 * 0.25f);
			}
			num = (int)((float)num * 1f);
			if (num < 0)
			{
				num = 0;
			}
			if (num > 100)
			{
				num = 100;
			}
			return num;
		}

		internal void ResetProvinceGainTimer()
		{
			LastProvinceAdded = 0;
		}
	}
}