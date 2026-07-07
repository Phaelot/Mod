using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.AI.V2
{
	public class AITradeManager
	{
		public IList<WorkingAgent> Agents
		{
			get
			{
				return this.AI.Game.AllAgents.Values.Where((WorkingAgent x) => this.AgentIDs.Contains(x.ID)).ToList<WorkingAgent>().AsReadOnly();
			}
		}

		public AITradeManager(AIPlayer AI)
		{
			this.AI = AI;
			this.TradeOfferCooldowns = new Dictionary<WorkingRealm, int>();
			this.Funds = new AIFundData();
			this.AgentIDs = new List<int>();
		}

		internal void Dispose()
		{
		}

		internal void UpdateCooldowns()
		{
			foreach (WorkingRealm workingRealm in this.TradeOfferCooldowns.Keys.ToList<WorkingRealm>())
			{
				Dictionary<WorkingRealm, int> tradeOfferCooldowns;
				WorkingRealm workingRealm2;
				(tradeOfferCooldowns = this.TradeOfferCooldowns)[workingRealm2 = workingRealm] = tradeOfferCooldowns[workingRealm2] - 1;
				if (this.TradeOfferCooldowns[workingRealm] == 0)
				{
					this.TradeOfferCooldowns.Remove(workingRealm);
				}
			}
		}

		internal float GetPrisonerValue(WorkingUnit Unit)
		{
			float num = 0f;
			switch (Unit.Rank)
			{
			case UnitRanks.Standard:
				num += (float)Unit.Upkeep;
				break;
			case UnitRanks.Elite:
				num += (float)(Unit.BaseCost * 4);
				break;
			case UnitRanks.Unique:
				num += (float)(Unit.BaseCost * 8);
				break;
			case UnitRanks.Mercenary:
				num += (float)Unit.BaseCost;
				break;
			}
			switch (Unit.Medals)
			{
			case 1:
				num *= 2f;
				break;
			case 2:
				num *= 2.5f;
				break;
			case 3:
				num *= 3f;
				break;
			case 4:
				num *= 4f;
				break;
			}
			return num;
		}

		internal bool AttemptTrade(TradeOfferList Demands, WorkingRealm TargetRealm, bool Immediate = false)
		{
			if (this.AI.Realm.GetTradeAgent() == null)
			{
				this.AI.Log("    Trade failed, no agent available");
				return false;
			}
			if (this.TradeOfferCooldowns.ContainsKey(TargetRealm))
			{
				this.AI.Log("    Trade failed, traded with " + TargetRealm.Name + " too recently");
				return false;
			}
			if (this.AI.Game.IgnoreHumanPlayer && TargetRealm == this.AI.Game.PlayerRealm)
			{
				this.AI.Log("    Trade failed, human realm is being ignored");
				return false;
			}
			TradeOffer tradeOffer = new TradeOffer(this.AI.Game, this.AI.Realm.ID, TargetRealm.ID);
			tradeOffer.TargetOffers.CopyFrom(Demands);
			tradeOffer.SenderOffers.Treaty = tradeOffer.TargetOffers.Treaty;
			if (tradeOffer.TargetOffers.Treaty != TreatyTypes.None && tradeOffer.TargetOffers.Treaty != TreatyTypes.Peace)
			{
				this.AI.Log("    Checking for prisoners...");
				List<WorkingUnit> realmPrisoners = this.AI.Realm.Prison.GetRealmPrisoners(TargetRealm);
				List<WorkingUnit> realmPrisoners2 = TargetRealm.Prison.GetRealmPrisoners(this.AI.Realm);
				if (realmPrisoners.Count > 0)
				{
					this.AI.Log("    Offering " + realmPrisoners.Count + " prisoners");
					foreach (WorkingUnit workingUnit in realmPrisoners)
					{
						tradeOffer.SenderOffers.AddPrisoner(workingUnit);
					}
				}
				if (realmPrisoners2.Count > 0)
				{
					this.AI.Log("    Demanding " + realmPrisoners2.Count + " prisoners");
					foreach (WorkingUnit workingUnit2 in realmPrisoners2)
					{
						tradeOffer.TargetOffers.AddPrisoner(workingUnit2);
					}
				}
				if (realmPrisoners2.Count + realmPrisoners.Count == 0)
				{
					this.AI.Log("    No prisoners to be exchanged");
				}
			}
			float num = this.GetRealmTradeValue(TargetRealm, tradeOffer);
			this.AI.Log("    Trade value calculated at " + num);
			Dictionary<ResourceData, int> availableResources = this.GetAvailableResources();
			this.GetAvailableProvinces();
			List<WorkingUnit> availablePrisoners = this.GetAvailablePrisoners(TargetRealm);
			bool flag = this.AI.RNG.Next(100) < 80;
			if (flag)
			{
				this.AI.Log("    Resources wil be used before gold");
			}
			else
			{
				this.AI.Log("    Gold will be used before resources");
			}
			while (num > 0f && !Immediate)
			{
				if (availablePrisoners.Count > 0 && this.AI.Realm.DiplomacyManager.GetDisposition(TargetRealm) > 0f)
				{
					tradeOffer.SenderOffers.AddPrisoner(availablePrisoners[0]);
					availablePrisoners.RemoveAt(0);
					num = this.GetRealmTradeValue(TargetRealm, tradeOffer);
					this.AI.Log("    Added prisoner, value is now " + num);
				}
				else if (flag)
				{
					if (availableResources.Count > 0)
					{
						ResourceData resourceData = availableResources.Keys.First<ResourceData>();
						float num2;
						if (TargetRealm.AIPlayer != null)
						{
							num2 = (float)TargetRealm.AIPlayer.TradeManager.GetResourceValue(resourceData);
						}
						else
						{
							num2 = (float)this.GetResourceValue(resourceData);
						}
						int num3 = availableResources[resourceData];
						while ((float)num3 * num2 > num)
						{
							num3--;
						}
						if (num3 == 0)
						{
							num3 = 1;
						}
						availableResources.Remove(resourceData);
						tradeOffer.SenderOffers.SetLumpResource(resourceData, num3);
						num = this.GetRealmTradeValue(TargetRealm, tradeOffer);
						this.AI.Log(string.Concat(new object[] { "    Added ", num3, " ", resourceData.ResourceName, ", value is now ", num }));
					}
					else
					{
						if (tradeOffer.SenderOffers.GoldLump != 0 || this.Funds.CurrentGold <= 0)
						{
							break;
						}
						tradeOffer.SenderOffers.GoldLump = (int)Math.Min(Math.Max(num * 1.1f, 1f), (float)this.Funds.CurrentGold);
						num -= (float)tradeOffer.SenderOffers.GoldLump;
						this.AI.Log(string.Concat(new object[]
						{
							"    Added ",
							tradeOffer.SenderOffers.GoldLump,
							" gold, value is now ",
							num
						}));
					}
				}
				else if (tradeOffer.SenderOffers.GoldLump == 0 && this.Funds.CurrentGold > 0)
				{
					tradeOffer.SenderOffers.GoldLump = (int)Math.Min(Math.Max(num * 1.1f, 1f), (float)this.Funds.CurrentGold);
					num -= (float)tradeOffer.SenderOffers.GoldLump;
					this.AI.Log(string.Concat(new object[]
					{
						"    Added ",
						tradeOffer.SenderOffers.GoldLump,
						" gold, value is now ",
						num
					}));
				}
				else
				{
					if (availableResources.Count <= 0)
					{
						break;
					}
					ResourceData resourceData2 = availableResources.Keys.First<ResourceData>();
					float num4;
					if (TargetRealm.AIPlayer != null)
					{
						num4 = (float)TargetRealm.AIPlayer.TradeManager.GetResourceValue(resourceData2);
					}
					else
					{
						num4 = (float)this.GetResourceValue(resourceData2);
					}
					int num5 = availableResources[resourceData2];
					while ((float)num5 * num4 > num)
					{
						num5--;
					}
					if (num5 == 0)
					{
						num5 = 1;
					}
					availableResources.Remove(resourceData2);
					tradeOffer.SenderOffers.SetLumpResource(resourceData2, num5);
					num = this.GetRealmTradeValue(TargetRealm, tradeOffer);
					this.AI.Log(string.Concat(new object[] { "    Added ", num5, " ", resourceData2.ResourceName, ", value is now ", num }));
				}
			}
			if (num < 0f || Immediate)
			{
				this.TradeOfferCooldowns.Add(TargetRealm, 3 + this.AI.RNG.Next(3));
				this.AI.Log("    Trade is balanced, offer sent to " + TargetRealm.Name);
				if (Immediate)
				{
					WorkingAgent tradeAgent = this.AI.Realm.GetTradeAgent();
					tradeAgent.TradeOffer = tradeOffer;
					tradeAgent.Send(TargetRealm, null, AgentModes.CarryTradeOffer);
					WorkingRealm realm = this.AI.Realm;
					WorkingRealm workingRealm = this.AI.Game.AllRealms[tradeOffer.TargetRealmID];
					if (tradeOffer.SenderOffers.Treaty != TreatyTypes.None)
					{
						this.AI.Game.AllianceController.FormTreaty(realm, workingRealm, tradeOffer.SenderOffers.Treaty);
					}
					foreach (int num6 in tradeOffer.SenderOffers.GetProvinces())
					{
						WorkingProvince workingProvince = this.AI.Game.AllProvinces[num6];
						this.AI.Game.ChangeProvinceOwner(workingProvince, workingRealm);
						if (workingProvince.LandNode.CurrentStack != null)
						{
							this.AI.Game.WithdrawStack(workingProvince.LandNode.CurrentStack);
						}
						if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
						{
							this.AI.Game.WithdrawStack(workingProvince.HarbourNode.CurrentStack);
						}
					}
					foreach (int num7 in tradeOffer.TargetOffers.GetProvinces())
					{
						WorkingProvince workingProvince2 = this.AI.Game.AllProvinces[num7];
						this.AI.Game.ChangeProvinceOwner(workingProvince2, realm);
						if (workingProvince2.LandNode.CurrentStack != null)
						{
							this.AI.Game.WithdrawStack(workingProvince2.LandNode.CurrentStack);
						}
						if (workingProvince2.HarbourNode != null && workingProvince2.HarbourNode.CurrentStack != null)
						{
							this.AI.Game.WithdrawStack(workingProvince2.HarbourNode.CurrentStack);
						}
					}
					foreach (int num8 in tradeOffer.SenderOffers.GetPrisoners())
					{
						WorkingUnit workingUnit3 = this.AI.Game.AllUnits[num8];
						this.AI.Realm.Prison.ReleasePrisoner(workingUnit3);
						TargetRealm.QueueUnit(workingUnit3, true, false);
					}
					using (IEnumerator<int> enumerator6 = tradeOffer.TargetOffers.GetPrisoners().GetEnumerator())
					{
						while (enumerator6.MoveNext())
						{
							int num9 = enumerator6.Current;
							WorkingUnit workingUnit4 = this.AI.Game.AllUnits[num9];
							TargetRealm.Prison.ReleasePrisoner(workingUnit4);
							this.AI.Realm.QueueUnit(workingUnit4, true, false);
						}
						return true;
					}
				}
				AIActionOfferTrade aiactionOfferTrade = this.AI.ActionManager.CreateAction<AIActionOfferTrade>();
				aiactionOfferTrade.Offer = tradeOffer;
				aiactionOfferTrade.TargetRealm = TargetRealm;
				this.AI.ActionManager.AddAction(aiactionOfferTrade, true);
				return true;
			}
			this.AI.Log("    Unable to balance trade, aborting");
			return false;
		}

		private List<WorkingUnit> GetAvailablePrisoners(WorkingRealm TargetRealm)
		{
			return (from x in this.AI.Realm.Prison.GetRealmPrisoners(TargetRealm)
				orderby this.GetPrisonerValue(x) descending
				select x).ToList<WorkingUnit>();
		}

		private Dictionary<ResourceData, int> GetAvailableResources()
		{
			Dictionary<ResourceData, int> dictionary = new Dictionary<ResourceData, int>();
			foreach (ResourceData resourceData in this.AI.Game.GameCore.Data.Resources.Values)
			{
				if (!this.AI.Realm.UnitPurchaseManager.ResourceIsUseful(resourceData))
				{
					int stockpiledResource = this.AI.Realm.GetStockpiledResource(resourceData);
					if (stockpiledResource > 0)
					{
						dictionary.Add(resourceData, stockpiledResource);
					}
				}
				else
				{
					int num = this.AI.Realm.GetStockpiledResource(resourceData);
					num -= 30;
					if (num > 0)
					{
						dictionary.Add(resourceData, num);
					}
				}
			}
			return dictionary;
		}

		private List<WorkingProvince> GetAvailableProvinces()
		{
			List<WorkingProvince> list = new List<WorkingProvince>();
			foreach (WorkingProvince workingProvince in this.AI.Realm.Provinces)
			{
				if (!workingProvince.IsCapitol && !workingProvince.Occupied)
				{
					list.Add(workingProvince);
				}
			}
			list = list.OrderBy((WorkingProvince x) => x.CurrentEconomy).ToList<WorkingProvince>();
			return list;
		}

		public int GetResourceValue(ResourceData Resource)
		{
			float currentValue = this.AI.Game.Marketplace.GetCurrentValue(Resource);
			float num = (float)this.AI.ResourcesManager.GetResourceDesiredStockpile(Resource);
			float num2 = (float)this.AI.Realm.GetStockpiledResource(Resource);
			float num3 = (float)this.AI.Realm.GetResourceIncome(Resource, true);
			num3 -= (float)this.AI.Realm.GetResourceExpenses(Resource);
			float num4 = 1f;
			num4 += (num2 - num) * 0.05f;
			num4 += num3 * -0.1f;
			if (num4 > 2f)
			{
				num4 = 2f;
			}
			if (num4 < 0.5f)
			{
				num4 = 0.5f;
			}
			return (int)(currentValue * num4);
		}

		public float GetTradeValue(TradeOfferList Offers, WorkingRealm OfferingRealm, bool TheirOffer)
		{
			float num = 0f;
			num += (float)Offers.GoldLump;
			num += (float)(Offers.GoldPerTurn * 12);
			foreach (KeyValuePair<ResourceData, int> keyValuePair in Offers.GetResourceLump())
			{
				num += (float)(this.GetResourceValue(keyValuePair.Key) * keyValuePair.Value);
			}
			foreach (KeyValuePair<ResourceData, int> keyValuePair2 in Offers.GetResourceTurn())
			{
				num += (float)(this.GetResourceValue(keyValuePair2.Key) * keyValuePair2.Value * 12);
			}
			foreach (int num2 in Offers.GetProvinces())
			{
				num += (float)(5000 * this.AI.Game.AllProvinces[num2].CurrentEconomy);
			}
			foreach (int num3 in Offers.GetPrisoners())
			{
				WorkingUnit workingUnit = this.AI.Game.AllUnits[num3];
				num += this.GetPrisonerValue(workingUnit);
			}
			if (TheirOffer)
			{
				switch (Offers.Treaty)
				{
				case TreatyTypes.Alliance:
				{
					float disposition = this.AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm);
					if (disposition < 0f)
					{
						num += disposition * 50f;
					}
					else
					{
						num += disposition;
					}
					if (this.AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.Peace)
					{
						num -= 1000f;
					}
					if (this.AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.NAP)
					{
						num -= 400f;
					}
					num -= (float)((OfferingRealm.Enemies.Count - 1) * 500);
					num -= this.AI.WarManager.GetWarValue(OfferingRealm) * 10f;
					break;
				}
				case TreatyTypes.NonAggression:
					num += this.AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) * 20f;
					num -= this.AI.WarManager.GetWarValue(OfferingRealm) * 10f;
					break;
				case TreatyTypes.MutualDefence:
				{
					float disposition2 = this.AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm);
					if (disposition2 < 0f)
					{
						num += disposition2 * 30f;
					}
					else
					{
						num += disposition2;
					}
					if (this.AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.Peace)
					{
						num -= 200f;
					}
					num -= (float)((OfferingRealm.Enemies.Count - 1) * 100);
					num -= this.AI.WarManager.GetWarValue(OfferingRealm) * 10f;
					break;
				}
				case TreatyTypes.Peace:
				{
					int num4 = this.AI.WarManager.GetPeaceDesire(OfferingRealm);
					if (this.AI.WarManager.Wars.Count > 0)
					{
						if (OfferingRealm.ID == this.AI.Realm.ID)
						{
							OfferingRealm = this.AI.Game.PlayerRealm;
						}
						if (this.AI.WarManager.Wars[OfferingRealm.ID].NoBloodSpilled())
						{
							num4 = 0;
						}
					}
					num += (float)(num4 * 100);
					break;
				}
				}
			}
			return num;
		}

		internal float ValueTrade(WorkingRealm OfferingRealm, TradeOffer Offer)
		{
			float num = this.GetTradeValue(Offer.SenderOffers, OfferingRealm, true);
			if (OfferingRealm.Lucky)
			{
				num *= 1.2f;
			}
			float tradeValue = this.GetTradeValue(Offer.TargetOffers, OfferingRealm, false);
			float num2 = num - tradeValue;
			float num3 = this.AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm);
			num3 /= 500f;
			return num2 * (1f + num3);
		}

		public float GetRealmTradeValue(WorkingRealm Realm, TradeOffer Offer)
		{
			float num;
			if (Realm.AIPlayer != null)
			{
				num = Realm.AIPlayer.TradeManager.ValueTrade(this.AI.Realm, Offer);
			}
			else
			{
				num = this.ValueTrade(this.AI.Realm, Offer);
				float num2 = (float)this.AI.RNG.NextDouble() * 0.1f;
				num2 -= 0.05f;
				num2 += 1f;
				num *= num2;
			}
			num -= 50f;
			return num * -1f;
		}

		private bool TradePossible(WorkingRealm OfferingRealm, TradeOffer Offer)
		{
			if (this.AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.War)
			{
				if (Offer.TargetOffers.Treaty != TreatyTypes.Peace)
				{
					return false;
				}
				int peaceDesire = this.AI.WarManager.GetPeaceDesire(OfferingRealm);
				int num = -5;
				num -= this.AI.Realm.Traits[AITraits.Diplomat];
				num += this.AI.Realm.Traits[AITraits.Warmonger];
				if (peaceDesire < num)
				{
					return false;
				}
				if (this.AI.Realm.DiplomacyManager.GetRelationTime(OfferingRealm) < 5)
				{
					return false;
				}
			}
			if (Offer.TargetOffers.Treaty != TreatyTypes.Peace && Offer.TargetOffers.Treaty != TreatyTypes.None)
			{
				if (Offer.TargetOffers.GetPrisoners().Count < this.AI.Realm.Prison.GetRealmPrisoners(OfferingRealm).Count)
				{
					return false;
				}
				if (Offer.SenderOffers.GetPrisoners().Count < OfferingRealm.Prison.GetRealmPrisoners(this.AI.Realm).Count)
				{
					return false;
				}
			}
			int num2 = this.AI.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.AI.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			num2 = Math.Max(num2, 0);
			if (this.Funds.CurrentGold < Offer.TargetOffers.GoldLump)
			{
				return false;
			}
			if (num2 < Offer.TargetOffers.GoldPerTurn)
			{
				return false;
			}
			foreach (KeyValuePair<ResourceData, int> keyValuePair in Offer.TargetOffers.GetResourceLump())
			{
				if (this.AI.Realm.GetStockpiledResource(keyValuePair.Key) < keyValuePair.Value)
				{
					return false;
				}
			}
			foreach (KeyValuePair<ResourceData, int> keyValuePair2 in Offer.TargetOffers.GetResourceTurn())
			{
				if (this.AI.Realm.GetResourceIncome(keyValuePair2.Key, false) < keyValuePair2.Value)
				{
					return false;
				}
			}
			if (Offer.TargetOffers.GetProvinces().Count > 0 && Offer.TargetOffers.Treaty != TreatyTypes.Peace)
			{
				return false;
			}
			switch (Offer.TargetOffers.Treaty)
			{
			case TreatyTypes.Alliance:
				if (this.AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) < 20f)
				{
					return false;
				}
				if (this.AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.Peace)
				{
					return false;
				}
				break;
			case TreatyTypes.NonAggression:
				if (this.AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) < -5f)
				{
					return false;
				}
				break;
			case TreatyTypes.MutualDefence:
				if (this.AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) < 10f)
				{
					return false;
				}
				break;
			}
			return true;
		}

		public AITradeResponses EvaluateTradeOffer(WorkingRealm OfferingRealm, TradeOffer Offer)
		{
			if (!this.TradePossible(OfferingRealm, Offer))
			{
				return AITradeResponses.NotPossible;
			}
			if (this.ForceAcceptTrade)
			{
				return AITradeResponses.Acceptable;
			}
			float num = this.ValueTrade(OfferingRealm, Offer);
			if (num < -500f)
			{
				return AITradeResponses.Insult;
			}
			if (num < -100f)
			{
				return AITradeResponses.Insufficient;
			}
			if (num < 50f)
			{
				return AITradeResponses.Almost;
			}
			if (num < 250f)
			{
				return AITradeResponses.Acceptable;
			}
			if (num < 1000f)
			{
				return AITradeResponses.Good;
			}
			return AITradeResponses.Fantastic;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ForceAcceptTrade);
			w.Write(this.TradeOfferCooldowns.Count);
			foreach (KeyValuePair<WorkingRealm, int> keyValuePair in this.TradeOfferCooldowns)
			{
				w.Write(keyValuePair.Key.ID);
				w.Write(keyValuePair.Value);
			}
			this.Funds.Save(w);
			w.Write(this.AgentIDs.Count);
			foreach (int num in this.AgentIDs)
			{
				w.Write(num);
			}
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.ForceAcceptTrade = r.ReadBoolean();
			this.TradeOfferCooldowns = new Dictionary<WorkingRealm, int>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = r.ReadInt32();
				int num3 = r.ReadInt32();
				this.TradeOfferCooldowns.Add(this.AI.Game.AllRealms[num2], num3);
			}
			this.Funds.Load(r, SaveVersion);
			num = r.ReadInt32();
			this.AgentIDs = new List<int>();
			for (int j = 0; j < num; j++)
			{
				this.AgentIDs.Add(r.ReadInt32());
			}
		}

		public void AssignAgent(WorkingAgent Agent)
		{
			this.AgentIDs.Add(Agent.ID);
		}

		public WorkingAgent GetFreeAgent()
		{
			return this.Agents.FirstOrDefault((WorkingAgent x) => x.CurrentMode == AgentModes.Idle && x.HostRealm == x.OwnerRealm && x.TurnsLeft == 0);
		}

		public AIPlayer AI;

		private Dictionary<WorkingRealm, int> TradeOfferCooldowns;

		public bool ForceAcceptTrade;

		public AIFundData Funds;

		public List<int> AgentIDs;
	}
}
