using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2
{
	public class RealmWarStatus
	{
		public RealmWarStatus(SovereigntyGame Game, WorkingRealm Realm)
		{
			if (Realm != null)
			{
				this.RealmID = Realm.ID;
			}
			this.Game = Game;
			this.Status = new Dictionary<WarReasons, WarReasonData>();
			this.ResourceStatus = new Dictionary<ResourceData, float>();
			this.Status.Add(WarReasons.Hatred, new WarReasonData(WarReasons.Hatred));
			this.Status.Add(WarReasons.LandExpansion, new WarReasonData(WarReasons.LandExpansion));
			this.Status.Add(WarReasons.LandReclaim, new WarReasonData(WarReasons.LandReclaim));
			this.Status.Add(WarReasons.Resources, new WarReasonData(WarReasons.Resources));
			this.Status.Add(WarReasons.Spying, new WarReasonData(WarReasons.Spying));
			this.Status.Add(WarReasons.Looting, new WarReasonData(WarReasons.Looting));
			this.Status.Add(WarReasons.StealLandmark, new WarReasonData(WarReasons.StealLandmark));
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.RealmID);
			w.Write(this.Status.Count);
			foreach (KeyValuePair<WarReasons, WarReasonData> keyValuePair in this.Status)
			{
				w.Write(keyValuePair.Key.ToString());
				keyValuePair.Value.Save(w);
			}
			w.Write(this.ResourceStatus.Count);
			foreach (KeyValuePair<ResourceData, float> keyValuePair2 in this.ResourceStatus)
			{
				w.Write(keyValuePair2.Key.ResourceName);
				w.Write(keyValuePair2.Value);
			}
		}

		public void Load(BinaryReader r, int SaveVersion)
		{
			this.RealmID = r.ReadInt32();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string text = r.ReadString();
				try
				{
					WarReasons warReasons = (WarReasons)Enum.Parse(typeof(WarReasons), text);
					this.Status[warReasons] = new WarReasonData(warReasons);
					this.Status[warReasons].Load(r, SaveVersion);
				}
				catch (ArgumentException)
				{
				}
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				string text2 = r.ReadString();
				float num2 = r.ReadSingle();
				ResourceData resourceData = null;
				this.Game.Data.Resources.TryGetValue(text2, out resourceData);
				if (resourceData != null)
				{
					this.ResourceStatus.Add(resourceData, num2);
				}
			}
		}

		internal void ModifyResourceStatus(ResourceData Resource, float Value)
		{
			if (!this.ResourceStatus.ContainsKey(Resource))
			{
				this.ResourceStatus.Add(Resource, 0f);
			}
			this.ResourceStatus[Resource] = Value;
		}

		internal void SetResourceStatus(ResourceData Resource, float Value)
		{
			if (!this.ResourceStatus.ContainsKey(Resource))
			{
				this.ResourceStatus.Add(Resource, 0f);
			}
			float num = this.ResourceStatus[Resource];
			num += Value;
			if (num < 0f)
			{
				num = 0f;
			}
			this.ResourceStatus[Resource] = num;
		}

		public int RealmID;

		public Dictionary<WarReasons, WarReasonData> Status;

		public Dictionary<ResourceData, float> ResourceStatus;

		public SovereigntyGame Game;
	}
}
