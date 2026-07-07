using System;
using System.IO;

namespace SovereigntyTK.AI.V2
{
	public class AIFundData
	{
		internal void Save(BinaryWriter w)
		{
			w.Write(this.CurrentPercentage);
			w.Write(this.CurrentGold);
			w.Write(this.MaximumGold);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.CurrentPercentage = r.ReadSingle();
			this.CurrentGold = r.ReadInt32();
			this.MaximumGold = r.ReadInt32();
		}

		public float CurrentPercentage;

		public int MaximumGold;

		public int CurrentGold;
	}
}
