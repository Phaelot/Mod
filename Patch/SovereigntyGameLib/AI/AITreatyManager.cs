using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.AI
{
	public class AITreatyManager
	{
		public AITreatyManager(AIPlayer AI, SovereigntyGame Game)
		{
			this.Game = Game;
			this.AI = AI;
			this.Cooldown = AI.RNG.Next(5) + 2;
		}

		internal void EndTreaties()
		{
			List<WorkingRealm> list = new List<WorkingRealm>();
			foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
			{
				int num = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
				switch (workingRealm.DiplomacyManager.GetRelation(this.AI.Realm))
				{
				case RelationStates.Alliance:
					if (num < 10)
					{
						list.Add(workingRealm);
					}
					break;
				case RelationStates.Defence:
					if (num < 0)
					{
						list.Add(workingRealm);
					}
					break;
				case RelationStates.NAP:
					if (num < -15)
					{
						list.Add(workingRealm);
					}
					break;
				}
			}
			if (list.Count > 0)
			{
				AIAction aiaction = new AIAction(AIActionTypes.EndTreaties);
				aiaction.Realms = list;
				this.AI.SetAction(aiaction);
			}
		}

		public int GetDistance(WorkingRealm RealmA, WorkingRealm RealmB)
		{
			Path path = this.Game.PathManager.GetPath(RealmA.CapitolProvince.LandNode, RealmB.CapitolProvince.LandNode, null, false, this.AI.Realm, false);
			List<WorkingRealm> list = new List<WorkingRealm>();
			List<WorkingZone> list2 = new List<WorkingZone>();
			foreach (PathPoint pathPoint in path.PathPoints)
			{
				WorkingZone zone = pathPoint.Node.Zone;
				WorkingProvince province = pathPoint.Node.Province;
				if (zone != null && !list2.Contains(zone))
				{
					list2.Add(zone);
				}
				if (province != null && !list.Contains(province.OwnerRealm))
				{
					list.Add(province.OwnerRealm);
				}
			}
			return list.Count + list2.Count - 2;
		}

		internal void OfferTreaties()
		{
			foreach (WorkingRealm workingRealm in from x in this.Game.AllRealms.Values
				where !x.RealmIsDead
				orderby this.AI.Realm.DiplomacyManager.GetDisposition(x) descending
				select x)
			{
				if (workingRealm != this.Game.RebelRealm)
				{
					switch (workingRealm.DiplomacyManager.GetRelation(this.AI.Realm))
					{
					case RelationStates.Defence:
						if (this.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm, TreatyTypes.Alliance) && this.Game.AllianceController.CanOfferAlliance(this.AI.Realm, workingRealm))
						{
							if (this.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Alliance) < 5)
							{
								if (this.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.HasTreaty(this.AI.Realm)) < 6 && this.GetDistance(this.AI.Realm, workingRealm) <= 2)
								{
									int num = 20;
									int num2 = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
									num2 -= 15 * (workingRealm.Enemies.Count - 1);
									if (num2 >= num)
									{
										TradeOfferList tradeOfferList = new TradeOfferList(this.Game, null, false);
										tradeOfferList.Treaty = TreatyTypes.Alliance;
										if (this.AI.Trade.AttemptTrade(tradeOfferList, workingRealm, false))
										{
											return;
										}
									}
								}
							}
						}
						break;
					case RelationStates.NAP:
						if (this.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm, TreatyTypes.MutualDefence))
						{
							if (this.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Defence) < 3)
							{
								if (this.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.HasTreaty(this.AI.Realm)) < 6 && this.GetDistance(this.AI.Realm, workingRealm) <= 2)
								{
									int num3 = 10;
									int num4 = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
									num4 -= 10 * (workingRealm.Enemies.Count - 1);
									if (num4 >= num3)
									{
										TradeOfferList tradeOfferList2 = new TradeOfferList(this.Game, null, false);
										tradeOfferList2.Treaty = TreatyTypes.MutualDefence;
										if (this.AI.Trade.AttemptTrade(tradeOfferList2, workingRealm, false))
										{
											return;
										}
									}
								}
							}
						}
						break;
					case RelationStates.Peace:
						if (this.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm, TreatyTypes.NonAggression))
						{
							if (this.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.NAP) < 3)
							{
								if (this.Game.AllRealms.Values.Count((WorkingRealm x) => !x.RealmIsDead && x.DiplomacyManager.HasTreaty(this.AI.Realm)) < 6)
								{
									int num5 = -5;
									int num6 = (int)this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm);
									if (num6 >= num5)
									{
										TradeOfferList tradeOfferList3 = new TradeOfferList(this.Game, null, false);
										tradeOfferList3.Treaty = TreatyTypes.NonAggression;
										if (this.AI.Trade.AttemptTrade(tradeOfferList3, workingRealm, false))
										{
											return;
										}
									}
								}
							}
						}
						break;
					}
				}
			}
		}

		internal void MakePeaceOffers()
		{
			foreach (WorkingRealm workingRealm in this.AI.Realm.Enemies)
			{
				if (workingRealm != this.Game.RebelRealm && this.AI.Realm.Restrictions.CanOfferPeace(workingRealm))
				{
					int peaceDesire = this.AI.GetPeaceDesire(workingRealm);
					if (this.AI.RNG.Next(100) < peaceDesire)
					{
						TradeOfferList tradeOfferList = new TradeOfferList(this.Game, null, false);
						tradeOfferList.Treaty = TreatyTypes.Peace;
						if (this.AI.Trade.AttemptTrade(tradeOfferList, workingRealm, false))
						{
							break;
						}
					}
				}
			}
		}

		public void ForcePeaceOffer(WorkingRealm Enemy)
		{
			TradeOfferList tradeOfferList = new TradeOfferList(this.Game, null, false);
			tradeOfferList.Treaty = TreatyTypes.Peace;
			this.AI.Trade.AttemptTrade(tradeOfferList, Enemy, true);
		}

		private bool WarLimitReached()
		{
			int count = this.AI.Realm.Provinces.Count;
			int num;
			if (count < 5)
			{
				num = 1;
			}
			else if (count < 10)
			{
				num = 2;
			}
			else if (count < 20)
			{
				num = 3;
			}
			else if (count < 35)
			{
				num = 4;
			}
			else
			{
				num = 5;
			}
			return this.AI.Realm.Enemies.Count + this.AI.InvasionTargets.Count >= num;
		}

		internal void DeclareWars()
		{
			if (this.Cooldown > 0)
			{
				this.Cooldown--;
				return;
			}
			this.Cooldown = this.AI.RNG.Next(3) + 2;
			if (this.AI.Realm.HasStatus("HolyArbiter", new object[0]))
			{
				return;
			}
			if (this.WarLimitReached())
			{
				return;
			}
			List<WorkingRealm> list = this.Game.AllRealms.Values.Where((WorkingRealm x) => this.AI.Realm.Restrictions.CanDeclareWar(x)).ToList<WorkingRealm>();
			list = list.Where((WorkingRealm x) => !this.AI.InvasionTargets.ContainsKey(x.ID) && this.HasSharedBorder(x)).ToList<WorkingRealm>();
			Dictionary<WorkingRealm, float> dictionary = new Dictionary<WorkingRealm, float>();
			foreach (WorkingRealm workingRealm in list)
			{
				dictionary.Add(workingRealm, this.GetWarValue(workingRealm));
			}
			foreach (KeyValuePair<WorkingRealm, float> keyValuePair in dictionary.OrderByDescending((KeyValuePair<WorkingRealm, float> x) => x.Value))
			{
				if (keyValuePair.Value < this.WarThreshold)
				{
					break;
				}
				if (this.VictoryIsPossible(keyValuePair.Key))
				{
					if (!this.AI.Realm.CodeOfWar || this.AI.Realm.HasStatus("IgnoreCode", new object[0]))
					{
						this.AI.InvasionTargets.Add(keyValuePair.Key.ID, 5);
					}
					else
					{
						AIAction aiaction = new AIAction(AIActionTypes.DeclareWar);
						aiaction.Realm = keyValuePair.Key;
						this.AI.SetAction(aiaction);
					}
					break;
				}
			}
		}

		public bool DefeatIsLikely(WorkingRealm Realm)
		{
			if (this.Game.IgnoreHumanPlayer && Realm == this.Game.PlayerRealm)
			{
				return false;
			}
			float num = this.AI.Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			float num2 = Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			foreach (WorkingRealm workingRealm in this.AI.Realm.Allies)
			{
				num += workingRealm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f);
			}
			foreach (WorkingRealm workingRealm2 in Realm.Allies)
			{
				num2 += workingRealm2.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f);
			}
			num2 /= 3f;
			float num3 = num2 / num;
			num3 -= (float)this.Game.Data.AITraits[this.AI.Realm.Name].Warmonger * 0.02f;
			num3 += (float)this.AI.Realm.Enemies.Count * 0.2f;
			float num4 = (float)(this.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.Game.EconomyController.GetTotalExpenses(this.AI.Realm));
			if (num4 < 0f)
			{
				num3 -= num4 * 0.0001f;
			}
			int num5 = this.AI.Realm.Provinces.Count;
			num5 = Math.Max(0, 3 - num5) + 1;
			num3 += 0.3f * (float)num5;
			return num3 > 1f;
		}

		public bool VictoryIsPossible(WorkingRealm Realm)
		{
			if (this.Game.IgnoreHumanPlayer && Realm == this.Game.PlayerRealm)
			{
				return false;
			}
			float num = this.AI.Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			float num2 = Realm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue());
			foreach (WorkingRealm workingRealm in this.AI.Realm.Allies)
			{
				num += workingRealm.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f);
			}
			foreach (WorkingRealm workingRealm2 in Realm.Allies)
			{
				num2 += workingRealm2.Units.Where((WorkingUnit x) => x.OwnerStackID > 0 || x.Class == UnitClasses.Fort).Sum((WorkingUnit x) => x.GetValue() / 4f);
			}
			float num3 = num / num2;
			float num4 = this.AI.Realm.DiplomacyManager.GetDisposition(Realm);
			if (num4 < 0f)
			{
				num4 *= -1f;
				num3 += num4 * 0.005f;
			}
			num3 += (float)this.Game.Data.AITraits[this.AI.Realm.Name].Warmonger * 0.01f;
			num3 -= (float)this.AI.Realm.Enemies.Count * 0.2f;
			float num5 = (float)(this.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.Game.EconomyController.GetTotalExpenses(this.AI.Realm));
			num3 += num5 * 0.0001f;
			return num3 > 1f;
		}

		public float GetWarValue(WorkingRealm Realm)
		{
			float num = 0f;
			num += (float)(Realm.GetLandmarkCount() * 5);
			foreach (ResourceData resourceData in Realm.GetResourcesInRealm())
			{
				if (this.AI.Realm.UnitPurchaseManager.ResourceIsUseful(resourceData))
				{
					num += 2f;
				}
			}
			num -= this.AI.Realm.DiplomacyManager.GetDisposition(Realm);
			num -= Realm.DiplomacyManager.GetDisposition(this.AI.Realm) / 2f;
			num += (float)this.Game.Data.AITraits[this.AI.Realm.Name].Warmonger;
			num -= (float)this.Game.Data.AITraits[this.AI.Realm.Name].Diplomat;
			num += (float)Realm.Provinces.Sum((WorkingProvince x) => x.AILust);
			foreach (WorkingProvince workingProvince in Realm.Provinces)
			{
				num += (float)this.AI.GetLustModifier(workingProvince.Name);
			}
			num -= (float)(Realm.Provinces.Count((WorkingProvince x) => x.GetUnmodifiedEconomy() == 0) * 5);
			return num;
		}

		private bool HasSharedBorder(WorkingRealm Realm)
		{
			foreach (WorkingProvince workingProvince in this.AI.Realm.Provinces)
			{
				foreach (GameRegion gameRegion in workingProvince.GetAllConnectedRegions())
				{
					if (gameRegion is WorkingProvince && (gameRegion as WorkingProvince).OwnerRealm == Realm)
					{
						return true;
					}
					if (gameRegion is WorkingZone)
					{
						foreach (GameRegion gameRegion2 in gameRegion.GetAllConnectedRegions())
						{
							if (gameRegion2 is WorkingProvince && (gameRegion2 as WorkingProvince).OwnerRealm == Realm)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public AIPlayer AI;

		public SovereigntyGame Game;

		private int Cooldown;

		private float WarThreshold = 20f;

		public float WarJoinThreshold = 15f;
	}
}
