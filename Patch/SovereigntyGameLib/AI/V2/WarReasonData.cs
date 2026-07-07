using System;
using System.Collections.Generic;
using System.IO;

namespace SovereigntyTK.AI.V2
{
	public class WarReasonData
	{
		public WarReasonData(WarReasons Reason)
		{
			this.ReasonType = Reason;
			this.CurrentValue = 0f;
			this.ProvinceIDs = new List<int>();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write((short)this.ReasonType);
			w.Write(this.CurrentValue);
			w.Write(this.ProvinceIDs.Count);
			foreach (int num in this.ProvinceIDs)
			{
				w.Write(num);
			}
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.ReasonType = (WarReasons)r.ReadInt16();
			this.CurrentValue = r.ReadSingle();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.ProvinceIDs.Add(r.ReadInt32());
			}
		}

		public float CurrentValue;

		public List<int> ProvinceIDs;

		public int QuantityValue;

		public WarReasons ReasonType;
	}
}
