using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class OngoingTrade
	{
		public OngoingTrade AssociatedTrade
		{
			get
			{
				if (this.TargetRealm == null)
				{
					return null;
				}
				return this.TargetRealm.TradeManager.GetTradeByID(this.AssociatedTradeID);
			}
		}

		public WorkingRealm Realm
		{
			get
			{
				return this.Game.AllRealms[this.RealmID];
			}
		}

		public WorkingRealm TargetRealm
		{
			get
			{
				return this.Game.AllRealms[this.TargetRealmID];
			}
		}

		public OngoingTrade(SovereigntyGame Game, int RealmID, int TargetID, TradeOfferList Offers)
		{
			this.Game = Game;
			this.TargetRealmID = TargetID;
			this.RealmID = RealmID;
			this.OriginalTrade = Offers;
			this.TradeID = Game.NextTradeID++;
			this.Resources = new List<ResourceData>();
			this.ResourceQuantities = new List<int>();
			this.TimeLeft = 12;
		}

		public OngoingTrade(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.Resources = new List<ResourceData>();
			this.ResourceQuantities = new List<int>();
			this.RealmID = r.ReadInt32();
			this.TargetRealmID = r.ReadInt32();
			this.Gold = r.ReadInt32();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string text = r.ReadString();
				ResourceData resourceData = null;
				Game.Data.Resources.TryGetValue(text, out resourceData);
				int num2 = r.ReadInt32();
				if (resourceData != null)
				{
					this.Resources.Add(resourceData);
					this.ResourceQuantities.Add(num2);
				}
			}
			this.TimeLeft = r.ReadInt32();
			this.OriginalTrade = new TradeOfferList(Game, null, true);
			if (SaveVersion >= 47)
			{
				this.OriginalTrade.Load(r, SaveVersion);
				this.TradeID = r.ReadInt32();
				this.AssociatedTradeID = r.ReadInt32();
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.RealmID);
			w.Write(this.TargetRealmID);
			w.Write(this.Gold);
			w.Write(this.Resources.Count);
			for (int i = 0; i < this.Resources.Count; i++)
			{
				w.Write(this.Resources[i].ResourceName);
				w.Write(this.ResourceQuantities[i]);
			}
			w.Write(this.TimeLeft);
			this.OriginalTrade.Save(w);
			w.Write(this.TradeID);
			w.Write(this.AssociatedTradeID);
		}

		public bool Update()
		{
			this.TimeLeft--;
			if (this.Gold > 0)
			{
				if (this.Realm.GetTradeGold() < this.Gold)
				{
					this.GoldFailed = true;
					return false;
				}
				this.Realm.SpendTradeGold(this.Gold);
				this.TargetRealm.Gold.Value += this.Gold;
			}
			for (int i = 0; i < this.Resources.Count; i++)
			{
				if (this.Realm.GetStockpiledResource(this.Resources[i]) < this.ResourceQuantities[i])
				{
					this.FailedResource = this.Resources[i];
					return false;
				}
				this.Realm.RemoveResource(this.Resources[i], this.ResourceQuantities[i], false);
				this.TargetRealm.GrantResource(this.Resources[i], this.ResourceQuantities[i]);
				this.Game.GameCore.FireEvent("TradeGoodGiven", new object[]
				{
					this.Realm,
					this.TargetRealm,
					this.Resources[i],
					this.ResourceQuantities[i]
				});
			}
			return true;
		}

		public WorkingRealm GetOtherRealm(WorkingRealm Realm)
		{
			if (this.Realm == Realm)
			{
				return this.TargetRealm;
			}
			return this.Realm;
		}

		private SovereigntyGame Game;

		private int TargetRealmID;

		private int RealmID;

		public int TradeID;

		public int Gold;

		public List<ResourceData> Resources;

		public List<int> ResourceQuantities;

		public int TimeLeft;

		public TreatyTypes Treaty;

		public bool GoldFailed;

		public ResourceData FailedResource;

		public int AssociatedTradeID;

		public TradeOfferList OriginalTrade;
	}
}
