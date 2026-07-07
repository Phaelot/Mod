using System;
using System.Collections.Generic;

namespace SovereigntyTK.Game
{
	public class SaveData
	{
		public SaveData()
		{
			this.ActiveModIDs = new List<string>();
			this.ActiveModNames = new List<string>();
		}

		public string ShortFilename;

		public string FullFilename;

		public bool Hardcore;

		public DateTime Date;

		public string RealmDisplayName;

		public int TurnNumber;

		public string CampaignID;

		public int Version;

		public string RealmName;

		public string IronManName;

		public string CurrentGameID;

		public bool Auto;

		public string CampaignDisplayName;

		public List<string> ActiveModIDs;

		public List<string> ActiveModNames;

		public string WorldName;
	}
}
