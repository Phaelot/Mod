using System;
using System.IO;

namespace SovereigntyTK.Game.Trade
{
	public class TradeOffer
	{
		public event Action OnTradeChanged;

		public TradeOffer(SovereigntyGame Game, int SenderID, int TargetID)
		{
			this.Game = Game;
			this.SenderRealmID = SenderID;
			this.TargetRealmID = TargetID;
			this.SenderOffers = new TradeOfferList(Game, this, true);
			this.TargetOffers = new TradeOfferList(Game, this, false);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.SenderRealmID = r.ReadInt32();
			this.TargetRealmID = r.ReadInt32();
			this.SenderOffers.Load(r, SaveVersion);
			this.TargetOffers.Load(r, SaveVersion);
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.SenderRealmID);
			w.Write(this.TargetRealmID);
			this.SenderOffers.Save(w);
			this.TargetOffers.Save(w);
		}

		internal void HandleChangedOffers()
		{
			if (this.OnTradeChanged != null)
			{
				this.OnTradeChanged();
			}
		}

		public void Clear()
		{
			this.SenderOffers.Clear();
			this.TargetOffers.Clear();
		}

		public bool OfferIsEmpty()
		{
			return this.SenderOffers.IsEmpty() && this.TargetOffers.IsEmpty();
		}

		public void SwitchSides()
		{
			int senderRealmID = this.SenderRealmID;
			TradeOfferList senderOffers = this.SenderOffers;
			this.SenderRealmID = this.TargetRealmID;
			this.SenderOffers = this.TargetOffers;
			this.SenderOffers.IsOffer = true;
			this.TargetRealmID = senderRealmID;
			this.TargetOffers = senderOffers;
			this.TargetOffers.IsOffer = false;
		}

		internal bool TreatyOnly()
		{
			return this.SenderOffers.TreatyOnly() && this.TargetOffers.TreatyOnly();
		}

		public void Cleanup()
		{
			this.TargetOffers.Cleanup();
			this.SenderOffers.Cleanup();
		}

		private SovereigntyGame Game;

		public int SenderRealmID;

		public int TargetRealmID;

		public TradeOfferList SenderOffers;

		public TradeOfferList TargetOffers;
	}
}
