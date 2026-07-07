using System;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WarTrackingData
	{
		public WarTrackingData(string Enemy, bool Invasion, bool Aggressor)
		{
			this.Enemy = Enemy;
			this.Invasion = Invasion;
			this.WarOfAggression = Aggressor;
		}

		public WarTrackingData(BinaryReader r, int SaveVersion)
		{
			this.Enemy = r.ReadString();
			this.ProvincesGained = r.ReadInt32();
			this.ProvincesLost = r.ReadInt32();
			this.UnitsKilled = r.ReadInt32();
			this.UnitsLost = r.ReadInt32();
			if (SaveVersion >= 7)
			{
				this.UnitValueKilled = r.ReadSingle();
				this.UnitValueLost = r.ReadSingle();
			}
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.Enemy);
			w.Write(this.ProvincesGained);
			w.Write(this.ProvincesLost);
			w.Write(this.UnitsKilled);
			w.Write(this.UnitsLost);
			w.Write(this.UnitValueKilled);
			w.Write(this.UnitValueLost);
		}

		public float GetWarValue()
		{
			float num = (float)this.ProvincesLost - (float)this.ProvincesGained * 0.5f;
			float num2 = this.UnitValueLost / 500f - this.UnitValueKilled / 1500f;
			if (num < -20f)
			{
				num = -20f;
			}
			if (num > 20f)
			{
				num = 20f;
			}
			if (num2 < -10f)
			{
				num2 = -10f;
			}
			if (num2 > 10f)
			{
				num2 = 10f;
			}
			return num + num2;
		}

		public string Enemy;

		public int ProvincesGained;

		public int ProvincesLost;

		public int UnitsKilled;

		public int UnitsLost;

		public float UnitValueLost;

		public float UnitValueKilled;

		public bool Invasion;

		public bool WarOfAggression;
	}
}
