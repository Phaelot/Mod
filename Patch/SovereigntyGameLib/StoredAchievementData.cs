using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK
{
	public class StoredAchievementData
	{
		private StoredAchievementData()
		{
			this.CampaignRealms = new List<string>();
			this.RivalryRealms = new List<string>();
			this.LMSRealms = new List<string>();
			this.ConquestRealms = new List<string>();
			this.PowerGamesRealms = new List<string>();
			this.SpellsLearned = new List<string>();
			this.RacesPlayed = new List<Races>();
		}

		public StoredAchievementData(ulong SteamID)
			: this()
		{
			this.SteamID = SteamID;
		}

		public StoredAchievementData(BinaryReader r)
			: this()
		{
			this.SteamID = r.ReadUInt64();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.CampaignRealms.Add(r.ReadString());
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				this.RivalryRealms.Add(r.ReadString());
			}
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				this.LMSRealms.Add(r.ReadString());
			}
			num = r.ReadInt32();
			for (int l = 0; l < num; l++)
			{
				this.ConquestRealms.Add(r.ReadString());
			}
			num = r.ReadInt32();
			for (int m = 0; m < num; m++)
			{
				this.PowerGamesRealms.Add(r.ReadString());
			}
			num = r.ReadInt32();
			for (int n = 0; n < num; n++)
			{
				this.SpellsLearned.Add(r.ReadString());
			}
			num = r.ReadInt32();
			for (int num2 = 0; num2 < num; num2++)
			{
				this.RacesPlayed.Add((Races)r.ReadInt16());
			}
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.SteamID);
			w.Write(this.CampaignRealms.Count);
			foreach (string text in this.CampaignRealms)
			{
				w.Write(text);
			}
			w.Write(this.RivalryRealms.Count);
			foreach (string text2 in this.RivalryRealms)
			{
				w.Write(text2);
			}
			w.Write(this.LMSRealms.Count);
			foreach (string text3 in this.LMSRealms)
			{
				w.Write(text3);
			}
			w.Write(this.ConquestRealms.Count);
			foreach (string text4 in this.ConquestRealms)
			{
				w.Write(text4);
			}
			w.Write(this.PowerGamesRealms.Count);
			foreach (string text5 in this.PowerGamesRealms)
			{
				w.Write(text5);
			}
			w.Write(this.SpellsLearned.Count);
			foreach (string text6 in this.SpellsLearned)
			{
				w.Write(text6);
			}
			w.Write(this.RacesPlayed.Count);
			foreach (Races races in this.RacesPlayed)
			{
				w.Write((short)races);
			}
		}

		public List<string> CampaignRealms;

		public List<string> RivalryRealms;

		public List<string> LMSRealms;

		public List<string> ConquestRealms;

		public List<string> PowerGamesRealms;

		public List<string> SpellsLearned;

		public List<Races> RacesPlayed;

		public ulong SteamID;
	}
}
