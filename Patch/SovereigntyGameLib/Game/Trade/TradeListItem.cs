using System;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Trade
{
	public class TradeListItem
	{
		public TradeListItem(Tradecategories Category, bool Header, bool Offer)
		{
			this.Category = Category;
			this.Header = Header;
			this.Offer = Offer;
		}

		public bool IsSameType(TradeListItem Item)
		{
			return this.Category == Item.Category && this.Header == Item.Header && this.Resource == Item.Resource && this.ProvinceID == Item.ProvinceID && this.Treaty == Item.Treaty;
		}

		public void UpdateText()
		{
			if (this.Text == null)
			{
				return;
			}
			this.Text.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", new object[] { this.Value }));
		}

		public Tradecategories Category;

		public bool Header;

		public bool Offer;

		public ResourceData Resource;

		public int ProvinceID;

		public int Value;

		public TreatyTypes Treaty;

		public ControlText Text;

		public int UnitID;
	}
}
