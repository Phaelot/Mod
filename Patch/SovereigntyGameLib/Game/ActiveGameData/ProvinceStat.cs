using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class ProvinceStat
	{
		public event ProvinceStatModifierDelegate OnRequestModifier;

		public event ProvinceStatModifierListDelegate OnRequestModifierList;

		public WorkingProvince Province
		{
			get
			{
				WorkingProvince workingProvince = null;
				this.Game.AllProvinces.TryGetValue(this.ProvinceID, out workingProvince);
				return workingProvince;
			}
		}

		public ProvinceStat(SovereigntyGame Game, int ProvinceID, int InitialValue, bool AllowNegativeNumbers = false)
		{
			this.AllowNegativeNumbers = AllowNegativeNumbers;
			this.Game = Game;
			this.ProvinceID = ProvinceID;
			this.BaseValue = InitialValue;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ProvinceID);
			w.Write(this.BaseValue);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.ProvinceID = r.ReadInt32();
			this.BaseValue = r.ReadInt32();
		}

		public void Dispose()
		{
			this.ProvinceID = -1;
			this.OnRequestModifier = null;
		}

		public static implicit operator int(ProvinceStat Stat)
		{
			return Stat.GetValue();
		}

		public List<GameText> GetBreakdown()
		{
			List<GameText> list = new List<GameText>();
			if (this.OnRequestModifierList != null)
			{
				this.OnRequestModifierList(this.Province, list);
			}
			return list;
		}

		public int GetValue()
		{
			int num = this.BaseValue;
			if (this.OnRequestModifier != null)
			{
				this.OnRequestModifier(this.Province, ref num);
			}
			if (!this.AllowNegativeNumbers)
			{
				num = Math.Max(0, num);
			}
			return num;
		}

		internal int CompareTo(UnitStat unitStat)
		{
			return this.GetValue().CompareTo(unitStat.GetValue());
		}

		private SovereigntyGame Game;

		public int ProvinceID;

		public int BaseValue;

		public bool AllowNegativeNumbers;
	}
}
