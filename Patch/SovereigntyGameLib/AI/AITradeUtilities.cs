// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.AI.AITradeUtilities
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.AI
{
	public class AITradeUtilities
	{
		private SovereigntyGame Game;

		private AIPlayer AI;

		private WorkingRealm Realm;

		private Dictionary<WorkingRealm, int> TradeOfferCooldowns;

		public bool ForceAcceptTrade;

		public AITradeUtilities(AIPlayer AI, SovereigntyGame Game)
		{
			this.AI = AI;
			this.Game = Game;
			Realm = AI.Realm;
			TradeOfferCooldowns = new Dictionary<WorkingRealm, int>();
		}

		private void LogTradeRule(string Text)
		{
			try
			{
				string basePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				if (string.IsNullOrEmpty(basePath))
				{
					basePath = ".";
				}
				string logDir = System.IO.Path.Combine(basePath, "SovereigntyTradeLogs");
				Directory.CreateDirectory(logDir);
				string filePath = System.IO.Path.Combine(logDir, "AITradeUtilities.log");
				string realmName = (Realm == null) ? "null" : Realm.Name;
				string prefix = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + realmName + "] ";
				File.AppendAllText(filePath, prefix + Text + Environment.NewLine);
			}
			catch
			{
			}
		}

		public void UpdateCooldowns()
		{
			foreach (WorkingRealm item in TradeOfferCooldowns.Keys.ToList())
			{
				TradeOfferCooldowns[item]--;
				if (TradeOfferCooldowns[item] == 0)
				{
					TradeOfferCooldowns.Remove(item);
				}
			}
		}

		internal float ValueTrade(WorkingRealm OfferingRealm, TradeOffer Offer)
		{
			float num = GetTradeValue(Offer.SenderOffers, OfferingRealm, TheirOffer: true);
			if (OfferingRealm.Lucky)
			{
				num *= 1.2f;
			}
			float tradeValue = GetTradeValue(Offer.TargetOffers, OfferingRealm, TheirOffer: false);
			float num2 = num - tradeValue;
			float disposition = Realm.DiplomacyManager.GetDisposition(OfferingRealm);
			disposition /= 500f;
			return num2 * (1f + disposition);
		}

		public AITradeResponses EvaluateTradeOffer(WorkingRealm OfferingRealm, TradeOffer Offer)
		{
			if (RealmTradeManager.IsPureEconomicTradeOffer(Offer))
			{
				string text;
				if (!RealmTradeManager.CanStartEconomicTrade(OfferingRealm, Realm, out text))
				{
					LogTradeRule("[TradeRule] Rejected trade offer " + OfferingRealm.Name + " -> " + Realm.Name + ": " + text);
					return AITradeResponses.NotPossible;
				}
			}
			if (!TradePossible(OfferingRealm, Offer))
			{
				return AITradeResponses.NotPossible;
			}
			if (ForceAcceptTrade)
			{
				return AITradeResponses.Acceptable;
			}
			float num = ValueTrade(OfferingRealm, Offer);
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

		private bool TradePossible(WorkingRealm OfferingRealm, TradeOffer Offer)
		{
			if (Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.War)
			{
				if (Offer.TargetOffers.Treaty != TreatyTypes.Peace)
				{
					return false;
				}
				int peaceDesire = AI.GetPeaceDesire(OfferingRealm);
				int num = -5;
				num -= AI.Realm.Traits[AITraits.Diplomat];
				num += AI.Realm.Traits[AITraits.Warmonger];
				if (peaceDesire < num)
				{
					return false;
				}
				if (AI.Realm.DiplomacyManager.GetRelationTime(OfferingRealm) < 5)
				{
					return false;
				}
			}
			if (Offer.TargetOffers.Treaty != TreatyTypes.Peace && Offer.TargetOffers.Treaty != TreatyTypes.None)
			{
				if (Offer.TargetOffers.GetPrisoners().Count < AI.Realm.Prison.GetRealmPrisoners(OfferingRealm).Count)
				{
					return false;
				}
				if (Offer.SenderOffers.GetPrisoners().Count < OfferingRealm.Prison.GetRealmPrisoners(AI.Realm).Count)
				{
					return false;
				}
			}
			int val = Game.EconomyController.GetRealmTotalIncome(Realm) - Game.EconomyController.GetTotalExpenses(Realm);
			val = Math.Max(val, 0);
			if ((int)Realm.Gold < Offer.TargetOffers.GoldLump)
			{
				return false;
			}
			if (val < Offer.TargetOffers.GoldPerTurn)
			{
				return false;
			}
			foreach (KeyValuePair<ResourceData, int> item in Offer.TargetOffers.GetResourceLump())
			{
				if (Realm.GetStockpiledResource(item.Key) < item.Value)
				{
					return false;
				}
			}
			foreach (KeyValuePair<ResourceData, int> item2 in Offer.TargetOffers.GetResourceTurn())
			{
				if (Realm.GetResourceIncome(item2.Key) < item2.Value)
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
					if (AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) < 20f)
					{
						return false;
					}
					if (AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.Peace)
					{
						return false;
					}
					break;
				case TreatyTypes.MutualDefence:
					if (AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) < 10f)
					{
						return false;
					}
					break;
				case TreatyTypes.NonAggression:
					if (AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) < -5f)
					{
						return false;
					}
					break;
			}
			return true;
		}

		public float GetTradeValue(TradeOfferList Offers, WorkingRealm OfferingRealm, bool TheirOffer)
		{
			float num = 0f;
			num += (float)Offers.GoldLump;
			num += (float)(Offers.GoldPerTurn * 12);
			foreach (KeyValuePair<ResourceData, int> item in Offers.GetResourceLump())
			{
				num += (float)(GetResourceValue(item.Key) * item.Value);
			}
			foreach (KeyValuePair<ResourceData, int> item2 in Offers.GetResourceTurn())
			{
				num += (float)(GetResourceValue(item2.Key) * item2.Value * 12);
			}
			foreach (int province in Offers.GetProvinces())
			{
				num += (float)(5000 * Game.AllProvinces[province].CurrentEconomy);
			}
			foreach (int prisoner in Offers.GetPrisoners())
			{
				WorkingUnit unit = Game.AllUnits[prisoner];
				num += GetPrisonerValue(unit);
			}
			if (TheirOffer)
			{
				switch (Offers.Treaty)
				{
					case TreatyTypes.Alliance:
						{
							float disposition2 = AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm);
							num = ((!(disposition2 < 0f)) ? (num + disposition2) : (num + disposition2 * 50f));
							if (AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.Peace)
							{
								num -= 1000f;
							}
							if (AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.NAP)
							{
								num -= 400f;
							}
							num -= (float)((OfferingRealm.Enemies.Count - 1) * 500);
							num -= AI.TreatyManager.GetWarValue(OfferingRealm) * 10f;
							break;
						}
					case TreatyTypes.MutualDefence:
						{
							float disposition = AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm);
							num = ((!(disposition < 0f)) ? (num + disposition) : (num + disposition * 30f));
							if (AI.Realm.DiplomacyManager.GetRelation(OfferingRealm) == RelationStates.Peace)
							{
								num -= 200f;
							}
							num -= (float)((OfferingRealm.Enemies.Count - 1) * 100);
							num -= AI.TreatyManager.GetWarValue(OfferingRealm) * 10f;
							break;
						}
					case TreatyTypes.NonAggression:
						num += AI.Realm.DiplomacyManager.GetDisposition(OfferingRealm) * 20f;
						num -= AI.TreatyManager.GetWarValue(OfferingRealm) * 10f;
						break;
					case TreatyTypes.Peace:
						{
							int num2 = AI.GetPeaceDesire(OfferingRealm);
							if (AI.Wars.Count > 0 && AI.Wars[OfferingRealm.ID].NoBloodSpilled())
							{
								num2 = 0;
							}
							num += (float)(num2 * 100);
							break;
						}
				}
			}
			return num;
		}

		public int GetResourceValue(ResourceData Resource)
		{
			float num = Game.Marketplace.GetCurrentPrice(Resource, PurchasePrice: true);
			if (Realm.UnitPurchaseManager.ResourceIsUseful(Resource))
			{
				num *= 1.25f;
			}
			if (Realm.GetStockpiledResource(Resource) > 20)
			{
				num *= 0.75f;
			}
			if (Realm.GetResourceIncome(Resource) > 2)
			{
				num *= 0.5f;
			}
			return (int)num;
		}

		public WorkingAgent GetFreeAgent()
		{
			return AI.Realm.Agents.FirstOrDefault((WorkingAgent x) => x.CurrentMode == AgentModes.Idle && x.HostRealm == x.OwnerRealm);
		}

		private List<WorkingUnit> GetAvailablePrisoners(WorkingRealm TargetRealm)
		{
			return (from x in AI.Realm.Prison.GetRealmPrisoners(TargetRealm)
					orderby GetPrisonerValue(x) descending
					select x).ToList();
		}

		private Dictionary<ResourceData, int> GetAvailableResources()
		{
			Dictionary<ResourceData, int> dictionary = new Dictionary<ResourceData, int>();
			foreach (ResourceData value in Game.GameCore.Data.Resources.Values)
			{
				if (!AI.Realm.UnitPurchaseManager.ResourceIsUseful(value))
				{
					int stockpiledResource = AI.Realm.GetStockpiledResource(value);
					if (stockpiledResource > 0)
					{
						dictionary.Add(value, stockpiledResource);
					}
					continue;
				}
				int stockpiledResource2 = AI.Realm.GetStockpiledResource(value);
				stockpiledResource2 -= 30;
				if (stockpiledResource2 > 0)
				{
					dictionary.Add(value, stockpiledResource2);
				}
			}
			return dictionary;
		}

		private List<WorkingProvince> GetAvailableProvinces()
		{
			List<WorkingProvince> list = new List<WorkingProvince>();
			foreach (WorkingProvince province in Realm.Provinces)
			{
				if (!province.IsCapitol && !province.Occupied)
				{
					list.Add(province);
				}
			}
			return list.OrderBy((WorkingProvince x) => x.CurrentEconomy).ToList();
		}

		internal bool AttemptPeaceTreaty(WorkingRealm TargetRealm, bool Immediate = false)
		{
			if (GetFreeAgent() == null)
			{
				return false;
			}
			if (Game.IgnoreHumanPlayer && TargetRealm == Game.PlayerRealm)
			{
				return false;
			}
			TradeOffer tradeOffer = new TradeOffer(Game, AI.Realm.ID, TargetRealm.ID);
			tradeOffer.SenderOffers.Treaty = TreatyTypes.Peace;
			tradeOffer.TargetOffers.Treaty = TreatyTypes.Peace;
			foreach (WorkingUnit realmPrisoner in TargetRealm.Prison.GetRealmPrisoners(AI.Realm))
			{
				tradeOffer.TargetOffers.AddPrisoner(realmPrisoner);
			}
			float num = GetRealmTradeValue(TargetRealm, tradeOffer);
			Dictionary<ResourceData, int> availableResources = GetAvailableResources();
			List<WorkingProvince> availableProvinces = GetAvailableProvinces();
			List<WorkingUnit> availablePrisoners = GetAvailablePrisoners(TargetRealm);
			bool flag = AI.RNG.Next(100) < 80;
			while (num >= 0f)
			{
				if (availablePrisoners.Count > 0)
				{
					tradeOffer.SenderOffers.AddPrisoner(availablePrisoners[0]);
					availablePrisoners.RemoveAt(0);
				}
				else if (flag)
				{
					if (availableResources.Count > 0)
					{
						ResourceData resourceData = availableResources.Keys.First();
						float num2 = 0f;
						num2 = ((TargetRealm.AIPlayer == null) ? ((float)GetResourceValue(resourceData)) : ((float)TargetRealm.AIPlayer.TradeManager.GetResourceValue(resourceData)));
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
						num = GetRealmTradeValue(TargetRealm, tradeOffer);
					}
					else
					{
						if (tradeOffer.SenderOffers.GoldLump != 0 || (int)AI.Realm.Gold <= 0)
						{
							break;
						}
						tradeOffer.SenderOffers.GoldLump = (int)Math.Min(Math.Max(num * 1.1f, 1f), (int)AI.Realm.Gold);
						num -= (float)tradeOffer.SenderOffers.GoldLump;
					}
				}
				else if (tradeOffer.SenderOffers.GoldLump == 0 && (int)AI.Realm.Gold > 0)
				{
					tradeOffer.SenderOffers.GoldLump = (int)Math.Min(Math.Max(num * 1.1f, 1f), (int)AI.Realm.Gold);
					num -= (float)tradeOffer.SenderOffers.GoldLump;
				}
				else
				{
					if (availableResources.Count <= 0)
					{
						break;
					}
					ResourceData resourceData2 = availableResources.Keys.First();
					float num4 = 0f;
					num4 = ((TargetRealm.AIPlayer == null) ? ((float)GetResourceValue(resourceData2)) : ((float)TargetRealm.AIPlayer.TradeManager.GetResourceValue(resourceData2)));
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
					num = GetRealmTradeValue(TargetRealm, tradeOffer);
				}
			}
			while (num >= 0f)
			{
				IList<int> prisoners = tradeOffer.SenderOffers.GetPrisoners();
				if (prisoners.Count <= 0)
				{
					break;
				}
				WorkingUnit unit = Game.AllUnits[prisoners[0]];
				num -= GetPrisonerValue(unit);
				tradeOffer.SenderOffers.RemovePrisoner(unit);
			}
			while (num >= 0f && availableProvinces.Count > 0 && AI.Wars[TargetRealm.ID].GetWarScore() < 0)
			{
				WorkingProvince province = availableProvinces[0];
				availableProvinces.RemoveAt(0);
				tradeOffer.SenderOffers.AddProvince(province);
				num = GetRealmTradeValue(TargetRealm, tradeOffer);
			}
			if (num < 0f)
			{
				TradeOfferCooldowns.Add(TargetRealm, 3 + AI.RNG.Next(3));
				if (Immediate)
				{
					WorkingAgent freeAgent = GetFreeAgent();
					freeAgent.TradeOffer = tradeOffer;
					freeAgent.Send(TargetRealm, null, AgentModes.CarryTradeOffer);
					WorkingRealm realm = Realm;
					WorkingRealm workingRealm = Game.AllRealms[tradeOffer.TargetRealmID];
					if (tradeOffer.SenderOffers.Treaty != TreatyTypes.None)
					{
						Game.AllianceController.FormTreaty(realm, workingRealm, tradeOffer.SenderOffers.Treaty);
					}
					foreach (int province2 in tradeOffer.SenderOffers.GetProvinces())
					{
						WorkingProvince workingProvince = Game.AllProvinces[province2];
						Game.ChangeProvinceOwner(workingProvince, workingRealm);
						if (workingProvince.LandNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince.LandNode.CurrentStack);
						}
						if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince.HarbourNode.CurrentStack);
						}
					}
					foreach (int province3 in tradeOffer.TargetOffers.GetProvinces())
					{
						WorkingProvince workingProvince2 = Game.AllProvinces[province3];
						Game.ChangeProvinceOwner(workingProvince2, realm);
						if (workingProvince2.LandNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince2.LandNode.CurrentStack);
						}
						if (workingProvince2.HarbourNode != null && workingProvince2.HarbourNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince2.HarbourNode.CurrentStack);
						}
					}
					foreach (int prisoner in tradeOffer.SenderOffers.GetPrisoners())
					{
						WorkingUnit unit2 = Game.AllUnits[prisoner];
						AI.Realm.Prison.ReleasePrisoner(unit2);
					}
					foreach (int prisoner2 in tradeOffer.TargetOffers.GetPrisoners())
					{
						WorkingUnit unit3 = Game.AllUnits[prisoner2];
						TargetRealm.Prison.ReleasePrisoner(unit3);
					}
				}
				else
				{
					AIAction aIAction = new AIAction(AIActionTypes.OfferTrade);
					aIAction.TradeOffer = tradeOffer;
					aIAction.Realm = TargetRealm;
					AI.SetAction(aIAction);
				}
				return true;
			}
			return false;
		}

		internal bool AttemptTrade(TradeOfferList Demands, WorkingRealm TargetRealm, bool Immediate = false)
		{
			if (RealmTradeManager.IsPureEconomicTradeOfferList(Demands))
			{
				string text;
				if (!RealmTradeManager.CanStartEconomicTrade(AI.Realm, TargetRealm, out text))
				{
					LogTradeRule("[TradeRule] Blocked AI trade " + AI.Realm.Name + " -> " + TargetRealm.Name + ": " + text);
					return false;
				}
			}
			if (GetFreeAgent() == null)
			{
				return false;
			}
			if (TradeOfferCooldowns.ContainsKey(TargetRealm))
			{
				return false;
			}
			if (Game.IgnoreHumanPlayer && TargetRealm == Game.PlayerRealm)
			{
				return false;
			}
			TradeOffer tradeOffer = new TradeOffer(Game, AI.Realm.ID, TargetRealm.ID);
			tradeOffer.TargetOffers.CopyFrom(Demands);
			tradeOffer.SenderOffers.Treaty = tradeOffer.TargetOffers.Treaty;
			if (tradeOffer.TargetOffers.Treaty != TreatyTypes.None && tradeOffer.TargetOffers.Treaty != TreatyTypes.Peace)
			{
				foreach (WorkingUnit realmPrisoner in AI.Realm.Prison.GetRealmPrisoners(TargetRealm))
				{
					tradeOffer.SenderOffers.AddPrisoner(realmPrisoner);
				}
				foreach (WorkingUnit realmPrisoner2 in TargetRealm.Prison.GetRealmPrisoners(AI.Realm))
				{
					tradeOffer.TargetOffers.AddPrisoner(realmPrisoner2);
				}
			}
			float num = GetRealmTradeValue(TargetRealm, tradeOffer);
			Dictionary<ResourceData, int> availableResources = GetAvailableResources();
			GetAvailableProvinces();
			List<WorkingUnit> availablePrisoners = GetAvailablePrisoners(TargetRealm);
			bool flag = AI.RNG.Next(100) < 80;
			while (num > 0f && !Immediate)
			{
				if (availablePrisoners.Count > 0 && AI.Realm.DiplomacyManager.GetDisposition(TargetRealm) > 0f)
				{
					tradeOffer.SenderOffers.AddPrisoner(availablePrisoners[0]);
					availablePrisoners.RemoveAt(0);
				}
				else if (flag)
				{
					if (availableResources.Count > 0)
					{
						ResourceData resourceData = availableResources.Keys.First();
						float num2 = 0f;
						num2 = ((TargetRealm.AIPlayer == null) ? ((float)GetResourceValue(resourceData)) : ((float)TargetRealm.AIPlayer.TradeManager.GetResourceValue(resourceData)));
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
						num = GetRealmTradeValue(TargetRealm, tradeOffer);
					}
					else
					{
						if (tradeOffer.SenderOffers.GoldLump != 0 || (int)AI.Realm.Gold <= 0)
						{
							break;
						}
						tradeOffer.SenderOffers.GoldLump = (int)Math.Min(Math.Max(num * 1.1f, 1f), (int)AI.Realm.Gold);
						num -= (float)tradeOffer.SenderOffers.GoldLump;
					}
				}
				else if (tradeOffer.SenderOffers.GoldLump == 0 && (int)AI.Realm.Gold > 0)
				{
					tradeOffer.SenderOffers.GoldLump = (int)Math.Min(Math.Max(num * 1.1f, 1f), (int)AI.Realm.Gold);
					num -= (float)tradeOffer.SenderOffers.GoldLump;
				}
				else
				{
					if (availableResources.Count <= 0)
					{
						break;
					}
					ResourceData resourceData2 = availableResources.Keys.First();
					float num4 = 0f;
					num4 = ((TargetRealm.AIPlayer == null) ? ((float)GetResourceValue(resourceData2)) : ((float)TargetRealm.AIPlayer.TradeManager.GetResourceValue(resourceData2)));
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
					num = GetRealmTradeValue(TargetRealm, tradeOffer);
				}
			}
			if (num < 0f || Immediate)
			{
				TradeOfferCooldowns.Add(TargetRealm, 3 + AI.RNG.Next(3));
				if (Immediate)
				{
					WorkingAgent freeAgent = GetFreeAgent();
					freeAgent.TradeOffer = tradeOffer;
					freeAgent.Send(TargetRealm, null, AgentModes.CarryTradeOffer);
					WorkingRealm realm = Realm;
					WorkingRealm workingRealm = Game.AllRealms[tradeOffer.TargetRealmID];
					if (tradeOffer.SenderOffers.Treaty != TreatyTypes.None)
					{
						Game.AllianceController.FormTreaty(realm, workingRealm, tradeOffer.SenderOffers.Treaty);
					}
					foreach (int province in tradeOffer.SenderOffers.GetProvinces())
					{
						WorkingProvince workingProvince = Game.AllProvinces[province];
						Game.ChangeProvinceOwner(workingProvince, workingRealm);
						if (workingProvince.LandNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince.LandNode.CurrentStack);
						}
						if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince.HarbourNode.CurrentStack);
						}
					}
					foreach (int province2 in tradeOffer.TargetOffers.GetProvinces())
					{
						WorkingProvince workingProvince2 = Game.AllProvinces[province2];
						Game.ChangeProvinceOwner(workingProvince2, realm);
						if (workingProvince2.LandNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince2.LandNode.CurrentStack);
						}
						if (workingProvince2.HarbourNode != null && workingProvince2.HarbourNode.CurrentStack != null)
						{
							Game.WithdrawStack(workingProvince2.HarbourNode.CurrentStack);
						}
					}
					foreach (int prisoner in tradeOffer.SenderOffers.GetPrisoners())
					{
						WorkingUnit workingUnit = Game.AllUnits[prisoner];
						AI.Realm.Prison.ReleasePrisoner(workingUnit);
						TargetRealm.QueueUnit(workingUnit, Instant: true);
					}
					foreach (int prisoner2 in tradeOffer.TargetOffers.GetPrisoners())
					{
						WorkingUnit workingUnit2 = Game.AllUnits[prisoner2];
						TargetRealm.Prison.ReleasePrisoner(workingUnit2);
						AI.Realm.QueueUnit(workingUnit2, Instant: true);
					}
				}
				else
				{
					AIAction aIAction = new AIAction(AIActionTypes.OfferTrade);
					aIAction.TradeOffer = tradeOffer;
					aIAction.Realm = TargetRealm;
					AI.SetAction(aIAction);
				}
				return true;
			}
			return false;
		}

		public float GetPrisonerValue(WorkingUnit Unit)
		{
			float num = 0f;
			switch (Unit.Rank)
			{
				case UnitRanks.Mercenary:
					num += (float)Unit.BaseCost;
					break;
				case UnitRanks.Standard:
					num += (float)(int)Unit.Upkeep;
					break;
				case UnitRanks.Elite:
					num += (float)(Unit.BaseCost * 4);
					break;
				case UnitRanks.Unique:
					num += (float)(Unit.BaseCost * 8);
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

		public float GetRealmTradeValue(WorkingRealm Realm, TradeOffer Offer)
		{
			float num = 0f;
			if (Realm.AIPlayer != null)
			{
				num = Realm.AIPlayer.TradeManager.ValueTrade(AI.Realm, Offer);
			}
			else
			{
				AIPlayer aIPlayer = new AIPlayer(Game, Realm.ID);
				num = aIPlayer.Trade.ValueTrade(AI.Realm, Offer);
				aIPlayer.Dispose();
				float num2 = (float)AI.RNG.NextDouble() * 0.1f;
				num2 -= 0.05f;
				num2 += 1f;
				num *= num2;
			}
			num -= 50f;
			return num * -1f;
		}
	}
}
