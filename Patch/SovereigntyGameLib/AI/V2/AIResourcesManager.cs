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
	public class AIResourcesManager
	{
		public AIResourcesManager(AIPlayer AI)
		{
			this.AI = AI;
			this.Funds = new AIFundData();
		}

		internal void Dispose()
		{
		}

		public int GetResourceDesiredStockpile(ResourceData Resource)
		{
			int requiredResource = this.AI.UnitsManager.GetRequiredResource(Resource);
			int requiredResource2 = this.AI.ConstructionManager.GetRequiredResource(Resource);
			return requiredResource + requiredResource2;
		}

		private void SellResource(ResourceData Resource, int MaxQuantity)
		{
			int stockpiledResource = this.AI.Realm.GetStockpiledResource(Resource);
			if (stockpiledResource == 0)
			{
				return;
			}
			if (MaxQuantity == 0)
			{
				MaxQuantity = stockpiledResource;
			}
			this.AI.Log(string.Concat(new object[] { "    Selling ", stockpiledResource, " ", Resource.ResourceName, " on the marketplace" }));
			MarketTradeData marketTradeData = new MarketTradeData();
			marketTradeData.Resource = Resource;
			marketTradeData.Purchase = false;
			marketTradeData.Quantity = MaxQuantity;
			this.MarketTrades.Add(marketTradeData);
		}

		internal bool TradeForResource(ResourceData Resource)
		{
			this.AI.Log("    Attempting trade for " + Resource.ResourceName);
			List<WorkingRealm> list = this.AI.Game.AllRealms.Values.Where((WorkingRealm x) => !x.RealmIsDead && x != this.AI.Realm && x.GetStockpiledResource(Resource) > 2 && this.AI.Realm.DiplomacyManager.GetRelation(x) != RelationStates.War).ToList<WorkingRealm>();
			if (list.Count == 0)
			{
				this.AI.Log("    No trade partner available, trade aborted");
				return false;
			}
			foreach (WorkingRealm workingRealm in from x in list
				orderby x.GetStockpiledResource(Resource) descending, this.AI.Realm.DiplomacyManager.GetDisposition(x) descending
				select x)
			{
				int num = workingRealm.GetStockpiledResource(Resource);
				int stockpiledResource = this.AI.Realm.GetStockpiledResource(Resource);
				int resourceDesiredStockpile = this.GetResourceDesiredStockpile(Resource);
				while (stockpiledResource + num > resourceDesiredStockpile)
				{
					num--;
				}
				TradeOfferList tradeOfferList = new TradeOfferList(this.AI.Game, null, false);
				tradeOfferList.SetLumpResource(Resource, num);
				this.AI.Log(string.Concat(new object[] { "    Attempting to find deal for ", num, " ", Resource.ResourceName, " from ", workingRealm.Name }));
				if (this.AI.TradeManager.AttemptTrade(tradeOfferList, workingRealm, false))
				{
					return true;
				}
			}
			return false;
		}

		private bool ShouldForceDoctrineUnitResourceMarketPurchase(ResourceData Resource)
		{
			if (this.AI == null || this.AI.Realm == null || this.AI.UnitsManager == null)
			{
				return false;
			}
			if (this.AI.Realm.Name != "Iron Barony" && this.AI.Realm.Name != "Maledor" && this.AI.Realm.Name != "Boruvian Empire")
			{
				return false;
			}
			return this.AI.UnitsManager.GetRequiredResource(Resource) > 0;
		}

		private void BorrowUnitFundsForDoctrineResource(ResourceData Resource, int NeededGold)
		{
			if (!this.ShouldForceDoctrineUnitResourceMarketPurchase(Resource))
			{
				return;
			}
			if (NeededGold <= this.Funds.CurrentGold || this.AI.UnitsManager.Funds.CurrentGold <= 0)
			{
				return;
			}
			int num = Math.Min(this.AI.UnitsManager.Funds.CurrentGold, NeededGold - this.Funds.CurrentGold);
			if (num <= 0)
			{
				return;
			}
			this.AI.UnitsManager.Funds.CurrentGold -= num;
			this.Funds.CurrentGold += num;
			this.AI.Log(string.Concat(new object[] { "    Doctrine unit-resource priority: moving ", num, " gold from unit funds to resource funds for ", Resource.ResourceName }));
		}

		internal void PurchaseResource(ResourceData Resource)
		{
			int num = this.AI.Game.Marketplace.GetQuantity(Resource);
			int stockpiledResource = this.AI.Realm.GetStockpiledResource(Resource);
			this.AI.Log("    Attempting to purchase " + Resource.ResourceName + " on the marketplace");
			if (num == 0)
			{
				this.AI.Log("    None in stock, aborting");
				return;
			}
			int resourceDesiredStockpile = this.GetResourceDesiredStockpile(Resource);
			while (stockpiledResource + num > resourceDesiredStockpile)
			{
				num--;
			}
			int currentPrice = this.AI.Game.Marketplace.GetCurrentPrice(Resource, true);
			this.BorrowUnitFundsForDoctrineResource(Resource, currentPrice * num);
			while (currentPrice * num > this.Funds.CurrentGold)
			{
				num--;
			}
			if (num == 0)
			{
				this.AI.Log("    Unable to afford, aborting");
				return;
			}
			MarketTradeData marketTradeData = new MarketTradeData();
			marketTradeData.Resource = Resource;
			marketTradeData.Purchase = true;
			marketTradeData.Quantity = num;
			this.MarketTrades.Add(marketTradeData);
		}

		internal void UpdateResources()
		{
			this.AI.Log("");
			this.AI.Log("Resource manager updating");
			this.AI.Log("  Available funds: " + this.Funds.CurrentGold);
			bool flag = false;
			this.MarketTrades = new List<MarketTradeData>();
			using (Dictionary<string, ResourceData>.ValueCollection.Enumerator enumerator = this.AI.Game.GameCore.Data.Resources.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ResourceData Resource = enumerator.Current;
					if (!this.AI.Realm.UnitPurchaseManager.ResourceIsUseful(Resource))
					{
						this.AI.Log("  Resource " + Resource.ResourceName + " is not useful");
						if (this.AI.RNG.Next(12) < this.AI.Game.Data.AITraits[this.AI.Realm.Name].Economist)
						{
							this.SellResource(Resource, 0);
						}
					}
					else if (this.AI.Realm.GetStockpiledResource(Resource) >= this.GetResourceDesiredStockpile(Resource))
					{
						this.AI.Log("  Resource " + Resource.ResourceName + " stockpile is full");
						if (this.AI.RNG.Next(12) < this.AI.Game.Data.AITraits[this.AI.Realm.Name].Economist)
						{
							int num = this.AI.Realm.GetStockpiledResource(Resource) - this.GetResourceDesiredStockpile(Resource);
							this.SellResource(Resource, num);
						}
					}
					else
					{
						bool flag2 = this.AI.Game.Marketplace.GetQuantity(Resource) > 0;
						bool flag3 = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => x != this.AI.Realm && x.GetStockpiledResource(Resource) > 0 && this.AI.Realm.DiplomacyManager.GetRelation(x) != RelationStates.War) > 0;
						if (this.ShouldForceDoctrineUnitResourceMarketPurchase(Resource) && flag2)
						{
							this.AI.Log("  Doctrine unit-resource priority: forcing market purchase for " + Resource.ResourceName);
							this.PurchaseResource(Resource);
							continue;
						}
						if (flag)
						{
							flag3 = false;
						}
						if (!flag3 && !flag2)
						{
							this.AI.Log("  Resource " + Resource.ResourceName + " no sources available");
						}
						else
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
								int trader = this.AI.Game.Data.AITraits[this.AI.Realm.Name].Trader;
								int economist = this.AI.Game.Data.AITraits[this.AI.Realm.Name].Economist;
								int num2 = this.AI.RNG.Next(trader + economist);
								flag4 = num2 < trader;
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
				AIActionMarketTrade aiactionMarketTrade = this.AI.ActionManager.CreateAction<AIActionMarketTrade>();
				aiactionMarketTrade.Trades = this.MarketTrades;
				this.AI.ActionManager.AddAction(aiactionMarketTrade, true);
			}
		}

		internal void Save(BinaryWriter w)
		{
			this.Funds.Save(w);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.Funds.Load(r, SaveVersion);
		}

		public AIPlayer AI;

		private List<MarketTradeData> MarketTrades;

		public AIFundData Funds;
	}
}
