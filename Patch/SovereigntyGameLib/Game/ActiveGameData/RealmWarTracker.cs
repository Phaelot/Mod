using System;
using System.Collections.Generic;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmWarTracker
	{
		public RealmWarTracker()
		{
			this.CurrentWars = new Dictionary<string, WarTrackingData>();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.CurrentWars.Count);
			foreach (KeyValuePair<string, WarTrackingData> keyValuePair in this.CurrentWars)
			{
				w.Write(keyValuePair.Key);
				keyValuePair.Value.Save(w);
			}
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.CurrentWars = new Dictionary<string, WarTrackingData>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string text = r.ReadString();
				WarTrackingData warTrackingData = new WarTrackingData(r, SaveVersion);
				this.CurrentWars.Add(text, warTrackingData);
			}
		}

		public void BeginTrackingWar(WorkingRealm Enemy, bool Invasion, bool Aggressor)
		{
			if (Enemy.Name == "World")
			{
				return;
			}
			if (!this.CurrentWars.ContainsKey(Enemy.Name))
			{
				this.CurrentWars.Add(Enemy.Name, new WarTrackingData(Enemy.Name, Invasion, Aggressor));
				return;
			}
			this.CurrentWars[Enemy.Name] = new WarTrackingData(Enemy.Name, Invasion, Aggressor);
		}

		public void StopTrackingWar(WorkingRealm Enemy)
		{
			if (this.CurrentWars.ContainsKey(Enemy.Name))
			{
				this.CurrentWars.Remove(Enemy.Name);
			}
		}

		internal void Dispose()
		{
		}

		internal bool IsDefensiveInvasion(WorkingRealm Enemy)
		{
			return this.CurrentWars.ContainsKey(Enemy.Name) && this.CurrentWars[Enemy.Name].Invasion && !this.CurrentWars[Enemy.Name].WarOfAggression;
		}

		private Dictionary<string, WarTrackingData> CurrentWars;
	}
}
