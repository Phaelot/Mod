using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class MarketPlaceData
	{
		public MarketPlaceData(SovereigntyGame Game)
		{
			this.Game = Game;
			this.MarketResources = new Dictionary<ResourceData, int>();
			this.MarketPriceHistory = new Dictionary<ResourceData, List<int>>();
			this.RNG = new Random();
			foreach (ResourceData resourceData in Game.GameCore.Data.Resources.Values)
			{
				this.MarketResources.Add(resourceData, 0);
				List<int> list = new List<int>();
				list.Add(this.GetCurrentPrice(resourceData, true));
				this.MarketPriceHistory.Add(resourceData, list);
			}
		}

		public List<int> GetPriceHistory(ResourceData Resource)
		{
			if (!this.MarketPriceHistory.ContainsKey(Resource))
			{
				return new List<int>();
			}
			return this.MarketPriceHistory[Resource];
		}

		public void Update()
		{
			foreach (KeyValuePair<ResourceData, List<int>> keyValuePair in this.MarketPriceHistory)
			{
				if (keyValuePair.Value.Count == 50)
				{
					keyValuePair.Value.RemoveAt(0);
				}
				keyValuePair.Value.Add((int)this.GetCurrentValue(keyValuePair.Key));
				float num = (float)(this.MarketResources[keyValuePair.Key] - 5);
				num /= 20f;
				num *= 100f;
				if ((float)this.RNG.Next(100) < num)
				{
					Dictionary<ResourceData, int> marketResources;
					ResourceData key;
					(marketResources = this.MarketResources)[key = keyValuePair.Key] = marketResources[key] - this.RNG.Next((int)((float)this.MarketResources[keyValuePair.Key] * 0.5f));
				}
			}
		}

		public void BuyFromMarket(WorkingRealm Realm, ResourceData Resource, int Quantity)
		{
			int currentPrice = this.GetCurrentPrice(Resource, true);
			int num = currentPrice * Quantity;
			if (Realm.GetMarketGold() < num)
			{
				return;
			}
			Realm.SpendMarketGold(num);
			this.MarketResources[Resource] = Math.Max(0, this.MarketResources[Resource] - Quantity);
			Realm.GrantResource(Resource, Quantity);
			this.Game.GameCore.FireEvent("MarketplacePurchase", new object[] { Realm, Resource, Quantity });
		}

		public void SellToMarket(WorkingRealm Realm, ResourceData Resource, int Quantity)
		{
			int currentPrice = this.GetCurrentPrice(Resource, false);
			int num = currentPrice * Quantity;
			if (Realm.GetStockpiledResource(Resource) < Quantity)
			{
				return;
			}
			Realm.SpendMarketGold(-num);
			Realm.RemoveResource(Resource, Quantity, false);
			Dictionary<ResourceData, int> marketResources;
			(marketResources = this.MarketResources)[Resource] = marketResources[Resource] + Quantity;
			this.Game.GameCore.FireEvent("MarketplaceSale", new object[] { Realm, Resource, Quantity });
		}

		public float GetCurrentValue(ResourceData Resource)
		{
			float num = (float)(this.MarketResources[Resource] - 10);
			num *= 0.15f;
			num = (float)Math.Pow(2.718281828459045, (double)num);
			num += 1f;
			num = 232f / num;
			return num + 10f;
		}

		public int GetCurrentPrice(ResourceData Resource, bool PurchasePrice)
		{
			float num = this.GetCurrentValue(Resource);
			if (PurchasePrice)
			{
				num *= MarketPlaceData.PURCHASE_MULT;
			}
			else
			{
				num *= MarketPlaceData.SELL_MULT;
			}
			return (int)num;
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string text = r.ReadString();
				int num2 = r.ReadInt32();
				ResourceData resourceData = null;
				this.Game.GameCore.Data.Resources.TryGetValue(text, out resourceData);
				if (resourceData != null)
				{
					this.MarketResources[resourceData] = num2;
				}
			}
			for (int j = 0; j < num; j++)
			{
				string text2 = r.ReadString();
				int num3 = r.ReadInt32();
				List<int> list = new List<int>();
				for (int k = 0; k < num3; k++)
				{
					list.Add(r.ReadInt32());
				}
				ResourceData resourceData2 = null;
				this.Game.GameCore.Data.Resources.TryGetValue(text2, out resourceData2);
				if (resourceData2 != null)
				{
					this.MarketPriceHistory[resourceData2] = list;
				}
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.MarketResources.Count);
			foreach (KeyValuePair<ResourceData, int> keyValuePair in this.MarketResources)
			{
				w.Write(keyValuePair.Key.ResourceName);
				w.Write(keyValuePair.Value);
			}
			foreach (KeyValuePair<ResourceData, List<int>> keyValuePair2 in this.MarketPriceHistory)
			{
				w.Write(keyValuePair2.Key.ResourceName);
				w.Write(keyValuePair2.Value.Count);
				foreach (int num in keyValuePair2.Value)
				{
					w.Write(num);
				}
			}
		}

		public int GetQuantity(ResourceData Resource)
		{
			return this.MarketResources[Resource];
		}

		internal void ModifyResources()
		{
		}

		public void ForceResource(ResourceData Resource, int Quantity)
		{
			this.MarketResources[Resource] = Quantity;
		}

		private Dictionary<ResourceData, int> MarketResources;

		private Dictionary<ResourceData, List<int>> MarketPriceHistory;

		public static float PURCHASE_MULT = 1.5f;

		public static float SELL_MULT = 0.4f;

		private Random RNG;

		private SovereigntyGame Game;
	}
}
