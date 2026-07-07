using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.Trade
{
	public class TradeOfferList
	{
		public int GoldLump
		{
			get
			{
				return this.m_GoldLump;
			}
			set
			{
				this.m_GoldLump = value;
				if (this.m_GoldLump < 0)
				{
					this.m_GoldLump = 0;
				}
				if (this.Offer != null)
				{
					this.Offer.HandleChangedOffers();
				}
			}
		}

		public int GoldPerTurn
		{
			get
			{
				return this.m_GoldTurn;
			}
			set
			{
				this.m_GoldTurn = value;
				if (this.m_GoldTurn < 0)
				{
					this.m_GoldTurn = 0;
				}
				if (this.Offer != null)
				{
					this.Offer.HandleChangedOffers();
				}
			}
		}

		public TreatyTypes Treaty
		{
			get
			{
				return this.m_Treaty;
			}
			set
			{
				this.m_Treaty = value;
				if (this.Offer != null)
				{
					this.Offer.HandleChangedOffers();
				}
			}
		}

		public TradeOfferList(SovereigntyGame Game, TradeOffer Offer, bool IsOffer)
		{
			this.Offer = Offer;
			this.IsOffer = IsOffer;
			this.Game = Game;
			this.ResourceLump = new Dictionary<ResourceData, int>();
			this.ResourceTurn = new Dictionary<ResourceData, int>();
			this.Provinces = new List<int>();
			this.Prisoners = new List<int>();
			this.m_Treaty = TreatyTypes.None;
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.m_GoldLump = r.ReadInt32();
			this.m_GoldTurn = r.ReadInt32();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ResourceData resourceData = null;
				this.Game.Data.Resources.TryGetValue(r.ReadString(), out resourceData);
				int num2 = r.ReadInt32();
				if (resourceData != null)
				{
					this.ResourceLump.Add(resourceData, num2);
				}
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				ResourceData resourceData2 = null;
				this.Game.Data.Resources.TryGetValue(r.ReadString(), out resourceData2);
				int num3 = r.ReadInt32();
				if (resourceData2 != null)
				{
					this.ResourceTurn.Add(resourceData2, num3);
				}
			}
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				this.Provinces.Add(r.ReadInt32());
			}
			this.m_Treaty = (TreatyTypes)r.ReadInt16();
			if (SaveVersion >= GlobalData.SAVEVERSION_EA3)
			{
				num = r.ReadInt32();
				for (int l = 0; l < num; l++)
				{
					this.Prisoners.Add(r.ReadInt32());
				}
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.m_GoldLump);
			w.Write(this.m_GoldTurn);
			w.Write(this.ResourceLump.Count);
			foreach (KeyValuePair<ResourceData, int> keyValuePair in this.ResourceLump)
			{
				w.Write(keyValuePair.Key.ResourceName);
				w.Write(keyValuePair.Value);
			}
			w.Write(this.ResourceTurn.Count);
			foreach (KeyValuePair<ResourceData, int> keyValuePair2 in this.ResourceTurn)
			{
				w.Write(keyValuePair2.Key.ResourceName);
				w.Write(keyValuePair2.Value);
			}
			w.Write(this.Provinces.Count);
			foreach (int num in this.Provinces)
			{
				w.Write(num);
			}
			w.Write((short)this.m_Treaty);
			w.Write(this.Prisoners.Count);
			foreach (int num2 in this.Prisoners)
			{
				w.Write(num2);
			}
		}

		public List<TradeListItem> GetList()
		{
			List<TradeListItem> list = new List<TradeListItem>();
			if (this.m_GoldLump > 0)
			{
				list.Add(new TradeListItem(Tradecategories.GoldLump, true, this.IsOffer));
				list.Add(new TradeListItem(Tradecategories.GoldLump, false, this.IsOffer)
				{
					Value = this.m_GoldLump
				});
			}
			if (this.m_GoldTurn > 0)
			{
				list.Add(new TradeListItem(Tradecategories.GoldPerTurn, true, this.IsOffer));
				list.Add(new TradeListItem(Tradecategories.GoldPerTurn, false, this.IsOffer)
				{
					Value = this.m_GoldTurn
				});
			}
			if (this.ResourceLump.Count > 0)
			{
				list.Add(new TradeListItem(Tradecategories.ResourceLump, true, this.IsOffer));
				foreach (KeyValuePair<ResourceData, int> keyValuePair in this.ResourceLump)
				{
					list.Add(new TradeListItem(Tradecategories.ResourceLump, false, this.IsOffer)
					{
						Resource = keyValuePair.Key,
						Value = keyValuePair.Value
					});
				}
			}
			if (this.ResourceTurn.Count > 0)
			{
				list.Add(new TradeListItem(Tradecategories.ResourcePerTurn, true, this.IsOffer));
				foreach (KeyValuePair<ResourceData, int> keyValuePair2 in this.ResourceTurn)
				{
					list.Add(new TradeListItem(Tradecategories.ResourcePerTurn, false, this.IsOffer)
					{
						Resource = keyValuePair2.Key,
						Value = keyValuePair2.Value
					});
				}
			}
			if (this.Provinces.Count > 0)
			{
				list.Add(new TradeListItem(Tradecategories.Province, true, this.IsOffer));
				foreach (int num in this.Provinces)
				{
					list.Add(new TradeListItem(Tradecategories.Province, false, this.IsOffer)
					{
						ProvinceID = num
					});
				}
			}
			if (this.Prisoners.Count > 0)
			{
				list.Add(new TradeListItem(Tradecategories.Prisoner, true, this.IsOffer));
				foreach (int num2 in this.Prisoners)
				{
					list.Add(new TradeListItem(Tradecategories.Prisoner, false, this.IsOffer)
					{
						UnitID = num2
					});
				}
			}
			if (this.Treaty != TreatyTypes.None)
			{
				list.Add(new TradeListItem(Tradecategories.Treaty, true, this.IsOffer));
				list.Add(new TradeListItem(Tradecategories.Treaty, false, this.IsOffer)
				{
					Treaty = this.m_Treaty
				});
			}
			return list;
		}

		public void AddPrisoner(WorkingUnit Unit)
		{
			this.Prisoners.Add(Unit.ID);
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public void RemovePrisoner(WorkingUnit Unit)
		{
			this.Prisoners.Remove(Unit.ID);
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public void RemovePrisoner(int UnitID)
		{
			this.Prisoners.Remove(UnitID);
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public void AddProvince(WorkingProvince Province)
		{
			this.Provinces.Add(Province.ID);
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public void RemoveProvince(WorkingProvince Province)
		{
			this.Provinces.Remove(Province.ID);
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public void RemoveProvince(int ProvinceID)
		{
			this.Provinces.Remove(ProvinceID);
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public void SetLumpResource(ResourceData Resource, int Quantity)
		{
			if (Quantity > 0)
			{
				if (!this.ResourceLump.ContainsKey(Resource))
				{
					this.ResourceLump.Add(Resource, 0);
				}
				this.ResourceLump[Resource] = Quantity;
			}
			else if (this.ResourceLump.ContainsKey(Resource))
			{
				this.ResourceLump.Remove(Resource);
			}
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public void SetTurnResource(ResourceData Resource, int Quantity)
		{
			if (Quantity > 0)
			{
				if (!this.ResourceTurn.ContainsKey(Resource))
				{
					this.ResourceTurn.Add(Resource, 0);
				}
				this.ResourceTurn[Resource] = Quantity;
			}
			else if (this.ResourceTurn.ContainsKey(Resource))
			{
				this.ResourceTurn.Remove(Resource);
			}
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public IDictionary<ResourceData, int> GetResourceLump()
		{
			return new Dictionary<ResourceData, int>(this.ResourceLump);
		}

		public IDictionary<ResourceData, int> GetResourceTurn()
		{
			return new Dictionary<ResourceData, int>(this.ResourceTurn);
		}

		public IList<int> GetProvinces()
		{
			return new List<int>(this.Provinces);
		}

		public IList<int> GetPrisoners()
		{
			return new List<int>(this.Prisoners);
		}

		public void RemoveItem(TradeListItem Item)
		{
			switch (Item.Category)
			{
			case Tradecategories.GoldLump:
				this.GoldLump = 0;
				return;
			case Tradecategories.GoldPerTurn:
				this.GoldPerTurn = 0;
				return;
			case Tradecategories.ResourceLump:
				this.SetLumpResource(Item.Resource, 0);
				return;
			case Tradecategories.ResourcePerTurn:
				this.SetTurnResource(Item.Resource, 0);
				return;
			case Tradecategories.Province:
				this.RemoveProvince(Item.ProvinceID);
				return;
			case Tradecategories.Treaty:
				this.Treaty = TreatyTypes.None;
				return;
			default:
				return;
			}
		}

		public int GetItemQuantity(TradeListItem Item)
		{
			switch (Item.Category)
			{
			case Tradecategories.GoldLump:
				return this.GoldLump;
			case Tradecategories.GoldPerTurn:
				return this.GoldPerTurn;
			case Tradecategories.ResourceLump:
				return this.ResourceLump[Item.Resource];
			case Tradecategories.ResourcePerTurn:
				return this.ResourceTurn[Item.Resource];
			default:
				return 0;
			}
		}

		public void ModifyItemQuantity(TradeListItem Item, int Amount)
		{
			switch (Item.Category)
			{
			case Tradecategories.GoldLump:
				this.GoldLump += Amount;
				return;
			case Tradecategories.GoldPerTurn:
				this.GoldPerTurn += Amount;
				return;
			case Tradecategories.ResourceLump:
				this.SetLumpResource(Item.Resource, this.ResourceLump[Item.Resource] + Amount);
				return;
			case Tradecategories.ResourcePerTurn:
				this.SetTurnResource(Item.Resource, this.ResourceTurn[Item.Resource] + Amount);
				return;
			default:
				return;
			}
		}

		internal void Clear()
		{
			this.ResourceLump = new Dictionary<ResourceData, int>();
			this.ResourceTurn = new Dictionary<ResourceData, int>();
			this.Provinces = new List<int>();
			this.m_Treaty = TreatyTypes.None;
			this.m_GoldLump = 0;
			this.m_GoldTurn = 0;
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public bool IsEmpty()
		{
			if (this.m_GoldLump > 0)
			{
				return false;
			}
			if (this.m_GoldTurn > 0)
			{
				return false;
			}
			if (this.Provinces.Count > 0)
			{
				return false;
			}
			if (this.ResourceLump.Sum((KeyValuePair<ResourceData, int> x) => x.Value) > 0)
			{
				return false;
			}
			return this.ResourceTurn.Sum((KeyValuePair<ResourceData, int> x) => x.Value) <= 0 && this.Treaty == TreatyTypes.None;
		}

		internal void CopyFrom(TradeOfferList List)
		{
			this.ResourceLump = new Dictionary<ResourceData, int>(List.ResourceLump);
			this.ResourceTurn = new Dictionary<ResourceData, int>(List.ResourceTurn);
			this.Provinces = new List<int>(List.Provinces);
			this.m_Treaty = List.m_Treaty;
			this.m_GoldLump = List.m_GoldLump;
			this.m_GoldTurn = List.m_GoldTurn;
			if (this.Offer != null)
			{
				this.Offer.HandleChangedOffers();
			}
		}

		public bool HasPerTurn()
		{
			return this.ResourceTurn.Sum((KeyValuePair<ResourceData, int> x) => x.Value) + this.m_GoldTurn > 0;
		}

		internal bool PerTurnOnly()
		{
			if (this.ResourceTurn.Sum((KeyValuePair<ResourceData, int> x) => x.Value) + this.m_GoldTurn == 0)
			{
				return false;
			}
			if (this.Treaty != TreatyTypes.None)
			{
				return false;
			}
			if (this.m_GoldLump > 0)
			{
				return false;
			}
			if (this.Provinces.Count > 0)
			{
				return false;
			}
			return this.ResourceLump.Sum((KeyValuePair<ResourceData, int> x) => x.Value) <= 0;
		}

		internal bool TreatyOnly()
		{
			if (this.Treaty == TreatyTypes.None)
			{
				return false;
			}
			if (this.m_GoldLump > 0)
			{
				return false;
			}
			if (this.m_GoldTurn > 0)
			{
				return false;
			}
			if (this.Provinces.Count > 0)
			{
				return false;
			}
			if (this.ResourceLump.Sum((KeyValuePair<ResourceData, int> x) => x.Value) > 0)
			{
				return false;
			}
			return this.ResourceTurn.Sum((KeyValuePair<ResourceData, int> x) => x.Value) <= 0;
		}

		public int GetResourceLumpValue(ResourceData Resource)
		{
			if (this.ResourceLump.ContainsKey(Resource))
			{
				return this.ResourceLump[Resource];
			}
			return 0;
		}

		public int GetResourceTurnValue(ResourceData Resource)
		{
			if (this.ResourceTurn.ContainsKey(Resource))
			{
				return this.ResourceTurn[Resource];
			}
			return 0;
		}

		public bool HasProvince(WorkingProvince Province)
		{
			return this.Provinces.Contains(Province.ID);
		}

		public bool HasPrisoner(WorkingUnit Unit)
		{
			return this.Prisoners.Contains(Unit.ID);
		}

		public bool HasResources()
		{
			return this.ResourceLump.Sum((KeyValuePair<ResourceData, int> x) => x.Value) + this.ResourceTurn.Sum((KeyValuePair<ResourceData, int> x) => x.Value) > 0;
		}

		public void AddItem(TradeItemData Data)
		{
			switch (Data.Category)
			{
			case Tradecategories.GoldLump:
				if (this.GoldLump > 0)
				{
					return;
				}
				this.GoldLump = Data.Value;
				return;
			case Tradecategories.GoldPerTurn:
				if (this.GoldPerTurn > 0)
				{
					return;
				}
				this.GoldPerTurn = Data.Value;
				return;
			case Tradecategories.ResourceLump:
			{
				ResourceData resourceData = this.Game.GameCore.Data.Resources.Values.ElementAt(Data.ID);
				if (this.ResourceLump.ContainsKey(resourceData) && this.ResourceLump[resourceData] > 0)
				{
					return;
				}
				if (!this.ResourceLump.ContainsKey(resourceData))
				{
					this.ResourceLump.Add(resourceData, Data.Value);
				}
				else
				{
					this.ResourceLump[resourceData] = Data.Value;
				}
				this.Game.GameCore.FireEvent("TradeResourceAdded", new object[] { resourceData });
				return;
			}
			case Tradecategories.ResourcePerTurn:
			{
				ResourceData resourceData2 = this.Game.GameCore.Data.Resources.Values.ElementAt(Data.ID);
				if (this.ResourceTurn.ContainsKey(resourceData2) && this.ResourceTurn[resourceData2] > 0)
				{
					return;
				}
				if (!this.ResourceTurn.ContainsKey(resourceData2))
				{
					this.ResourceTurn.Add(resourceData2, Data.Value);
				}
				else
				{
					this.ResourceTurn[resourceData2] = Data.Value;
				}
				this.Game.GameCore.FireEvent("TradeResourceAdded", new object[] { resourceData2 });
				return;
			}
			case Tradecategories.Province:
				this.Provinces.Add(Data.ID);
				return;
			case Tradecategories.Treaty:
				this.Treaty = (TreatyTypes)Data.ID;
				return;
			case Tradecategories.Prisoner:
				this.Prisoners.Add(Data.ID);
				return;
			default:
				return;
			}
		}

		public void RemoveItem(TradeItemData Data)
		{
			switch (Data.Category)
			{
			case Tradecategories.GoldLump:
				this.GoldLump = 0;
				return;
			case Tradecategories.GoldPerTurn:
				this.GoldPerTurn = 0;
				return;
			case Tradecategories.ResourceLump:
			{
				ResourceData resourceData = this.Game.GameCore.Data.Resources.Values.ElementAt(Data.ID);
				this.ResourceLump[resourceData] = 0;
				return;
			}
			case Tradecategories.ResourcePerTurn:
			{
				ResourceData resourceData2 = this.Game.GameCore.Data.Resources.Values.ElementAt(Data.ID);
				this.ResourceTurn[resourceData2] = 0;
				return;
			}
			case Tradecategories.Province:
				this.Provinces.Remove(Data.ID);
				return;
			case Tradecategories.Treaty:
				this.Treaty = TreatyTypes.None;
				return;
			case Tradecategories.Prisoner:
				this.Prisoners.Remove(Data.ID);
				return;
			default:
				return;
			}
		}

		public void SetQuantity(TradeItemData Data)
		{
			switch (Data.Category)
			{
			case Tradecategories.GoldLump:
				this.GoldLump = Data.Value;
				return;
			case Tradecategories.GoldPerTurn:
				this.GoldPerTurn = Data.Value;
				return;
			case Tradecategories.ResourceLump:
			{
				ResourceData resourceData = this.Game.GameCore.Data.Resources.Values.ElementAt(Data.ID);
				this.ResourceLump[resourceData] = Data.Value;
				return;
			}
			case Tradecategories.ResourcePerTurn:
			{
				ResourceData resourceData2 = this.Game.GameCore.Data.Resources.Values.ElementAt(Data.ID);
				this.ResourceTurn[resourceData2] = Data.Value;
				return;
			}
			default:
				return;
			}
		}

		internal void Cleanup()
		{
			List<ResourceData> list = new List<ResourceData>();
			foreach (KeyValuePair<ResourceData, int> keyValuePair in this.ResourceLump)
			{
				if (keyValuePair.Value == 0)
				{
					list.Add(keyValuePair.Key);
				}
			}
			foreach (ResourceData resourceData in list)
			{
				this.ResourceLump.Remove(resourceData);
			}
			list.Clear();
			foreach (KeyValuePair<ResourceData, int> keyValuePair2 in this.ResourceTurn)
			{
				if (keyValuePair2.Value == 0)
				{
					list.Add(keyValuePair2.Key);
				}
			}
			foreach (ResourceData resourceData2 in list)
			{
				this.ResourceTurn.Remove(resourceData2);
			}
		}

		private int m_GoldLump;

		private int m_GoldTurn;

		private TreatyTypes m_Treaty;

		public bool IsOffer;

		private Dictionary<ResourceData, int> ResourceLump;

		private Dictionary<ResourceData, int> ResourceTurn;

		private List<int> Provinces;

		private List<int> Prisoners;

		private TradeOffer Offer;

		private SovereigntyGame Game;
	}
}
