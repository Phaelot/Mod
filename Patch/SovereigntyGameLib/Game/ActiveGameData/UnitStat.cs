using System;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class UnitStat
	{
		public event GenericStatModifierDelegate OnRequestGenericModifier;

		public event StatModifierDelegate OnRequestModifier;

		public event StatEffectDelegate OnRequestEffect;

		public WorkingUnit Unit
		{
			get
			{
				WorkingUnit workingUnit = null;
				this.Game.AllUnits.TryGetValue(this.UnitID, out workingUnit);
				return workingUnit;
			}
		}

		public UnitStat(SovereigntyGame Game, int UnitID, UnitStatNames StatName, int InitialValue)
		{
			this.Game = Game;
			this.UnitID = UnitID;
			this.StatName = StatName;
			this.BaseValue = InitialValue;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.UnitID);
			w.Write((short)this.StatName);
			w.Write(this.BaseValue);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.UnitID = r.ReadInt32();
			this.StatName = (UnitStatNames)r.ReadInt16();
			this.BaseValue = r.ReadInt32();
		}

		public void Dispose()
		{
			this.UnitID = -1;
			this.OnRequestModifier = null;
			this.OnRequestGenericModifier = null;
		}

		public static implicit operator int(UnitStat Stat)
		{
			return Stat.GetValue();
		}

		public int GetValue()
		{
			return this.GetValue(null);
		}

		public int GetValue(WorkingUnit OpposingUnit)
		{
			int baseValue = this.BaseValue;
			if (this.OnRequestModifier != null)
			{
				this.OnRequestModifier(this.Unit, OpposingUnit, ref baseValue);
			}
			if (this.OnRequestGenericModifier != null)
			{
				this.OnRequestGenericModifier(this.Unit, OpposingUnit, this.StatName, ref baseValue);
			}
			return Math.Max(0, baseValue);
		}

		internal int CompareTo(UnitStat unitStat)
		{
			return this.GetValue().CompareTo(unitStat.GetValue());
		}

		private SovereigntyGame Game;

		public int UnitID;

		public UnitStatNames StatName;

		public int BaseValue;
	}
}
