using System;

namespace SovereigntyTK.Game.Trade
{
	public class TradeItemData
	{
		public TradeItemData(Tradecategories Category, int ID, int Value)
		{
			this.Category = Category;
			this.ID = ID;
			this.Value = Value;
		}

		public Tradecategories Category;

		public int ID;

		public int Value;
	}
}
