using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmTradeManager
	{
		public RealmTradeManager(SovereigntyGame Game, int RealmID)
		{
			this.Game = Game;
			this.RealmID = RealmID;
			this.ActiveTrades = new List<OngoingTrade>();
			this.RNG = new Random();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ActiveTrades.Count);
			foreach (OngoingTrade ongoingTrade in this.ActiveTrades)
			{
				ongoingTrade.Save(w);
			}
			w.Write(this.CurrentTolerance);
			w.Write(this.LimitMet);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.ActiveTrades = new List<OngoingTrade>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.ActiveTrades.Add(new OngoingTrade(this.Game, r, SaveVersion));
			}
			if (SaveVersion >= 48)
			{
				this.CurrentTolerance = r.ReadInt32();
				this.LimitMet = r.ReadBoolean();
			}
		}

		public OngoingTrade InitTrade(TradeOfferList Offers, WorkingRealm Realm)
		{
			OngoingTrade ongoingTrade = new OngoingTrade(this.Game, this.RealmID, Realm.ID, Offers);
			if (Offers.GoldPerTurn > 0)
			{
				ongoingTrade.Gold = Offers.GoldPerTurn;
			}
			foreach (KeyValuePair<ResourceData, int> keyValuePair in Offers.GetResourceTurn())
			{
				ongoingTrade.Resources.Add(keyValuePair.Key);
				ongoingTrade.ResourceQuantities.Add(keyValuePair.Value);
			}
			ongoingTrade.Treaty = Offers.Treaty;
			this.ActiveTrades.Add(ongoingTrade);
			return ongoingTrade;
		}

		public int GetToleranceLevel()
		{
			if (this.CurrentTolerance <= 0)
			{
				this.LimitMet = true;
				return 0;
			}
			WorkingRealm workingRealm = this.Game.AllRealms[this.RealmID];
			float num = (float)workingRealm.Traits[AITraits.Diplomat];
			num = 10f - num;
			num *= 1.5f;
			num += 15f;
			float num2 = 0.2f * num;
			int currentTolerance = this.CurrentTolerance;
			int num3 = this.RNG.Next((int)num2 * 2) - (int)num2;
			num3 += (int)num;
			this.CurrentTolerance -= num3;
			if (this.CurrentTolerance < 0)
			{
				this.CurrentTolerance = 0;
			}
			return currentTolerance;
		}

		public void UpdateTrades()
		{
			this.CurrentTolerance = 100;
			this.LimitMet = false;
			foreach (OngoingTrade ongoingTrade in this.ActiveTrades.ToList<OngoingTrade>())
			{
				if (!ongoingTrade.Update())
				{
					this.ActiveTrades.Remove(ongoingTrade);
					if (ongoingTrade.TargetRealm.AIPlayer != null)
					{
						ongoingTrade.TargetRealm.AIPlayer.RelationsManager.HandleFailedTrade(ongoingTrade);
					}
					if (ongoingTrade.Realm == this.Game.PlayerRealm)
					{
						this.Game.GameCore.FireEvent("PlayerTradeFailed", new object[] { ongoingTrade });
					}
					if (ongoingTrade.TargetRealm == this.Game.PlayerRealm)
					{
						this.Game.GameCore.FireEvent("AITradeFailed", new object[] { ongoingTrade });
					}
					if (ongoingTrade.Treaty != TreatyTypes.None)
					{
						this.Game.AllianceController.BreakCurrentTreaty(this.Game.AllRealms[this.RealmID], ongoingTrade.TargetRealm, true, false);
					}
					if (ongoingTrade.AssociatedTrade != null)
					{
						ongoingTrade.TargetRealm.TradeManager.ActiveTrades.Remove(ongoingTrade.AssociatedTrade);
					}
				}
				if (ongoingTrade.TimeLeft <= 0)
				{
					this.ActiveTrades.Remove(ongoingTrade);
					if (ongoingTrade.AssociatedTrade != null)
					{
						ongoingTrade.AssociatedTrade.Realm.TradeManager.ActiveTrades.Remove(ongoingTrade.AssociatedTrade);
					}
				}
			}
		}


		public static bool CanStartEconomicTrade(WorkingRealm SourceRealm, WorkingRealm TargetRealm, out string Reason)
		{
			Reason = null;
			if (SourceRealm == null || TargetRealm == null)
			{
				Reason = "Trade is currently unavailable.";
				return false;
			}
			if (SourceRealm == TargetRealm)
			{
				Reason = "You cannot trade with your own realm.";
				return false;
			}
			RelationStates relation = SourceRealm.DiplomacyManager.GetRelation(TargetRealm);
			if (relation == RelationStates.War || relation == RelationStates.ForcedPeace)
			{
				Reason = "Normal trade is unavailable while relations are hostile.";
				return false;
			}
			if (IsGoodVsTradeTabooPair(SourceRealm, TargetRealm))
			{
				Reason = "Good realms will not trade with orc or necromancer realms.";
				return false;
			}
			if (SourceRealm.DiplomacyManager.GetDisposition(TargetRealm) < 5f || TargetRealm.DiplomacyManager.GetDisposition(SourceRealm) < 5f)
			{
				Reason = "Relations are too poor for trade. Minimum disposition is 5.";
				return false;
			}
			return true;
		}

		public static bool IsPureEconomicTradeOffer(TradeOffer Offer)
		{
			if (Offer == null)
			{
				return false;
			}
			return IsPureEconomicTradeOfferList(Offer.SenderOffers) && IsPureEconomicTradeOfferList(Offer.TargetOffers);
		}

		public static bool IsPureEconomicTradeOfferList(TradeOfferList Offer)
		{
			if (Offer == null)
			{
				return false;
			}
			return Offer.Treaty == TreatyTypes.None && Offer.GetProvinces().Count == 0 && Offer.GetPrisoners().Count == 0;
		}

		private static bool IsGoodVsTradeTabooPair(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			return (Realm1.Alignment == RealmAlignments.Good && IsTradeTabooTarget(Realm2)) || (Realm2.Alignment == RealmAlignments.Good && IsTradeTabooTarget(Realm1));
		}

		private static bool IsTradeTabooTarget(WorkingRealm Realm)
		{
			if (Realm == null)
			{
				return false;
			}
			return Realm.Name == "Khazoth" || Realm.Name == "Iron Barony" || Realm.Name == "Maledor" || Realm.Name == "Palemoor";
		}

		public int GetTradeCount(WorkingRealm Realm)
		{
			return this.ActiveTrades.Count((OngoingTrade x) => x.TargetRealm == Realm);
		}

		public int GetTradeCount()
		{
			return this.ActiveTrades.Count;
		}

		public List<OngoingTrade> GetTrades()
		{
			return new List<OngoingTrade>(this.ActiveTrades);
		}

		internal OngoingTrade GetTradeByID(int ID)
		{
			return this.ActiveTrades.FirstOrDefault((OngoingTrade x) => x.TradeID == ID);
		}

		private SovereigntyGame Game;

		private List<OngoingTrade> ActiveTrades;

		private int RealmID;

		private Random RNG;

		private int CurrentTolerance;

		public bool LimitMet;
	}
}
