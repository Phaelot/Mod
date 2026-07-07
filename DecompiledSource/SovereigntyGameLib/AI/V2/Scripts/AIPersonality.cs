using System;
using System.IO;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Scripts
{
	public abstract class AIPersonality
	{
		public abstract bool UseForRealm(string RealmName);

		public abstract void InitInternal();

		public abstract void SaveInternal(BinaryWriter w);

		public abstract void LoadInternal(BinaryReader r, int SaveVersion);

		public abstract int GetRawAffinity(string TerrainName);

		public virtual int GetLandmarkAffinity(string LandmarkName)
		{
			return 0;
		}

		public virtual int GetTerrainAffinity(WorkingProvince Province)
		{
			return this.GetRawAffinity(Province.Terrain.BaseType);
		}

		public virtual void Load(BinaryReader r, int SaveVersion)
		{
			this.LoadInternal(r, SaveVersion);
		}

		public virtual void Save(BinaryWriter w)
		{
			this.SaveInternal(w);
		}

		public virtual void Init(AIPlayer AI)
		{
			this.AI = AI;
			this.InitInternal();
		}

		public virtual void Dispose()
		{
		}

		public AIPlayer AI;
	}
}
