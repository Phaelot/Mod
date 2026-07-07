using System;
using System.Collections.Generic;
using SovereigntyTK.Game;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionMarketTrade : AIAction
	{
		public AIActionMarketTrade(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			foreach (MarketTradeData marketTradeData in this.Trades)
			{
				if (!marketTradeData.Purchase)
				{
					this.Game.Marketplace.SellToMarket(this.AI.Realm, marketTradeData.Resource, marketTradeData.Quantity);
				}
				else
				{
					this.Game.Marketplace.BuyFromMarket(this.AI.Realm, marketTradeData.Resource, marketTradeData.Quantity);
				}
			}
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<MarketTradeData> Trades;
	}
}
