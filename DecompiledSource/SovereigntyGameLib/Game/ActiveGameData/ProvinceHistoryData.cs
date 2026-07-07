using System;
using System.Collections.Generic;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class ProvinceHistoryData
	{
		public ProvinceHistoryData(int OwnerID)
		{
			this.OriginalOwnerID = OwnerID;
			this.PreviousOwnerIDs = new List<int>();
			for (int i = 0; i < 30; i++)
			{
				this.PreviousOwnerIDs.Add(OwnerID);
			}
		}

		public void Load(BinaryReader r, int SaveVersion)
		{
			this.OriginalOwnerID = r.ReadInt32();
			this.PreviousOwnerIDs = new List<int>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.PreviousOwnerIDs.Add(r.ReadInt32());
			}
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.OriginalOwnerID);
			w.Write(this.PreviousOwnerIDs.Count);
			foreach (int num in this.PreviousOwnerIDs)
			{
				w.Write(num);
			}
		}

		public bool RealmHasClaim(int RealmID)
		{
			return this.GetRealmClaimAge(RealmID) >= 0;
		}

		public int GetRealmClaimAge(int RealmID)
		{
			if (RealmID == this.OriginalOwnerID)
			{
				return 0;
			}
			return this.PreviousOwnerIDs.IndexOf(RealmID);
		}

		public int OriginalOwnerID;

		public List<int> PreviousOwnerIDs;
	}
}
