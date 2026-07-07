using System;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.Game
{
	public class OngoingTradeData
	{
		public OngoingTrade Trade;

		public Tuple<WorkingRealm, TradeOfferList, int> PendingTrade;
	}
}
