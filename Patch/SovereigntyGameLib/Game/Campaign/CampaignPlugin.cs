using System;
using System.IO;

namespace SovereigntyTK.Game.Campaign
{
	public abstract class CampaignPlugin
	{
		protected abstract int SaveData(BinaryWriter w);

		protected abstract void LoadData(BinaryReader r);

		public CampaignPlugin(Sovereignty Game, bool NewGame)
		{
			this.Game = Game;
		}

		public virtual void Dispose()
		{
		}

		public int Save(BinaryWriter w)
		{
			return this.SaveData(w);
		}

		public void Load(BinaryReader r)
		{
			this.LoadData(r);
		}

		public Sovereignty Game;
	}
}
