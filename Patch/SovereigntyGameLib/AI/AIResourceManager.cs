using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.AI
{
	public class AIResourceManager
	{
		public AIResourceManager(AIPlayer AI, SovereigntyGame Game)
		{
			this.AI = AI;
			this.Game = Game;
		}

		internal void UpdateResources()
		{
			bool flag = false;
			this.MarketTrades = new List<MarketTradeData>();
			using (Dictionary<string, ResourceData>.ValueCollection.Enumerator enumerator = this.Game.GameCore.Data.Resources.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ResourceData Resource = enumerator.Current;
					if (!this.AI.Realm.UnitPurchaseManager.ResourceIsUseful(Resource))
					{
						if (this.AI.RNG.Next(12) < this.Game.Data.AITraits[this.AI.Realm.Name].Economist)
						{
							this.SellResource(Resource);
						}
					}
					else if (this.AI.Realm.GetStockpiledResource(Resource) < this.AI.ResourceStockLimit)
					{
						bool flag2 = this.Game.Marketplace.GetQuantity(Resource) > 0;
						bool flag3 = this.Game.AllRealms.Values.Count((WorkingRealm x) => x != this.AI.Realm && x.GetStockpiledResource(Resource) > 0 && this.AI.Realm.DiplomacyManager.GetRelation(x) != RelationStates.War) > 0;
						if (flag)
						{
							flag3 = false;
						}
						if (flag3 || flag2)
						{
							bool flag4 = false;
							if (flag3 && !flag2)
							{
								flag4 = true;
							}
							if (flag2 && !flag3)
							{
								flag4 = false;
							}
							if (flag3 && flag2)
							{
								int trader = this.Game.Data.AITraits[this.AI.Realm.Name].Trader;
								int economist = this.Game.Data.AITraits[this.AI.Realm.Name].Economist;
								int num = this.AI.RNG.Next(trader + economist);
								flag4 = num < trader;
							}
							if (flag4)
							{
								if (this.TradeForResource(Resource))
								{
									flag = true;
								}
							}
							else
							{
								this.PurchaseResource(Resource);
							}
						}
					}
				}
			}
			if (this.MarketTrades.Count > 0)
			{
				AIAction aiaction = new AIAction(AIActionTypes.MarketTrade);
				aiaction.TradeList = this.MarketTrades;
				this.AI.SetAction(aiaction);
			}
		}

		private void SellResource(ResourceData Resource)
		{
			int stockpiledResource = this.AI.Realm.GetStockpiledResource(Resource);
			if (stockpiledResource == 0)
			{
				return;
			}
			MarketTradeData marketTradeData = new MarketTradeData();
			marketTradeData.Resource = Resource;
			marketTradeData.Purchase = false;
			marketTradeData.Quantity = stockpiledResource;
			this.MarketTrades.Add(marketTradeData);
		}

		internal bool TradeForResource(ResourceData Resource)
		{
			List<WorkingRealm> list = this.Game.AllRealms.Values.Where((WorkingRealm x) => !x.RealmIsDead && x != this.AI.Realm && x.GetStockpiledResource(Resource) > 2 && this.AI.Realm.DiplomacyManager.GetRelation(x) != RelationStates.War).ToList<WorkingRealm>();
			foreach (WorkingRealm workingRealm in from x in list
				orderby x.GetStockpiledResource(Resource) descending, this.AI.Realm.DiplomacyManager.GetDisposition(x) descending
				select x)
			{
				int num = workingRealm.GetStockpiledResource(Resource);
				int stockpiledResource = this.AI.Realm.GetStockpiledResource(Resource);
				while (stockpiledResource + num > this.AI.ResourceStockLimit)
				{
					num--;
				}
				TradeOfferList tradeOfferList = new TradeOfferList(this.Game, null, false);
				tradeOfferList.SetLumpResource(Resource, num);
				if (this.AI.Trade.AttemptTrade(tradeOfferList, workingRealm, false))
				{
					return true;
				}
			}
			return false;
		}

		internal void PurchaseResource(ResourceData Resource)
		{
			int num = this.Game.Marketplace.GetQuantity(Resource);
			int stockpiledResource = this.AI.Realm.GetStockpiledResource(Resource);
			while (stockpiledResource + num > this.AI.ResourceStockLimit)
			{
				num--;
			}
			int currentPrice = this.Game.Marketplace.GetCurrentPrice(Resource, true);
			while (currentPrice * num > this.AI.Realm.Gold)
			{
				num--;
			}
			if (num == 0)
			{
				return;
			}
			MarketTradeData marketTradeData = new MarketTradeData();
			marketTradeData.Resource = Resource;
			marketTradeData.Purchase = true;
			marketTradeData.Quantity = num;
			this.MarketTrades.Add(marketTradeData);
		}

		public AIPlayer AI;

		public SovereigntyGame Game;

		private List<MarketTradeData> MarketTrades;
	}
}
