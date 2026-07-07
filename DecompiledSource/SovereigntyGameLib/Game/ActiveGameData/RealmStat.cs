using System;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmStat
	{
		public event RealmStatModifierDelegate OnRequestModifier;

		public WorkingRealm Realm
		{
			get
			{
				WorkingRealm workingRealm = null;
				this.Game.AllRealms.TryGetValue(this.RealmID, out workingRealm);
				return workingRealm;
			}
		}

		public RealmStat(SovereigntyGame Game, int RealmID, int InitialValue)
		{
			this.Game = Game;
			this.RealmID = RealmID;
			this.BaseValue = InitialValue;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.RealmID);
			w.Write(this.BaseValue);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.RealmID = r.ReadInt32();
			this.BaseValue = r.ReadInt32();
		}

		public void Dispose()
		{
			this.RealmID = -1;
			this.OnRequestModifier = null;
		}

		public static implicit operator int(RealmStat Stat)
		{
			return Stat.GetValue();
		}

		public int GetValue()
		{
			int baseValue = this.BaseValue;
			if (this.OnRequestModifier != null)
			{
				this.OnRequestModifier(this.Realm, ref baseValue);
			}
			return Math.Max(0, baseValue);
		}

		internal int CompareTo(UnitStat unitStat)
		{
			return this.GetValue().CompareTo(unitStat.GetValue());
		}

		private SovereigntyGame Game;

		public int RealmID;

		public int BaseValue;
	}
}
