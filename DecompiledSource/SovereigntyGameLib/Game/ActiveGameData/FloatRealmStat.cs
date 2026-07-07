using System;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class FloatRealmStat
	{
		public event RealmFloatStatModifierDelegate OnRequestModifier;

		public WorkingRealm Realm
		{
			get
			{
				WorkingRealm workingRealm = null;
				this.Game.AllRealms.TryGetValue(this.RealmID, out workingRealm);
				return workingRealm;
			}
		}

		public FloatRealmStat(SovereigntyGame Game, int RealmID, int InitialValue)
		{
			this.Game = Game;
			this.RealmID = RealmID;
			this.BaseValue = (float)InitialValue;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.RealmID);
			w.Write(this.BaseValue);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.RealmID = r.ReadInt32();
			this.BaseValue = (float)r.ReadInt32();
		}

		public void Dispose()
		{
			this.RealmID = -1;
			this.OnRequestModifier = null;
		}

		public static implicit operator float(FloatRealmStat Stat)
		{
			return Stat.GetValue();
		}

		public float GetValue()
		{
			float baseValue = this.BaseValue;
			if (this.OnRequestModifier != null)
			{
				this.OnRequestModifier(this.Realm, ref baseValue);
			}
			return Math.Max(0f, baseValue);
		}

		internal int CompareTo(UnitStat unitStat)
		{
			return this.GetValue().CompareTo((float)unitStat.GetValue());
		}

		private SovereigntyGame Game;

		public int RealmID;

		public float BaseValue;
	}
}
