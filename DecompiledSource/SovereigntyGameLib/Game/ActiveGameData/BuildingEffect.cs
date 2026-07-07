using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public abstract class BuildingEffect
	{
		public WorkingProvince Province
		{
			get
			{
				return this.Game.AllProvinces[this.ProvinceID];
			}
		}

		private static void CreateEffects(SovereigntyGame Game)
		{
			BuildingEffect.AllEffects = new List<BuildingEffect>();
			List<Type> list = (from t in Game.GameCore.Utilities.ScriptManager.BuildingAssembly.GetTypes()
				where t.IsSubclassOf(typeof(BuildingEffect))
				select t).ToList<Type>();
			foreach (Type type in list)
			{
				if (!(type.GetConstructor(Type.EmptyTypes) == null))
				{
					BuildingEffect buildingEffect = (BuildingEffect)Activator.CreateInstance(type);
					Game.GameCore.Data.Buildings.TryGetValue(buildingEffect.BuildingName, out buildingEffect.Data);
					if (buildingEffect.Data != null)
					{
						buildingEffect.Game = Game;
						BuildingEffect.AllEffects.Add(buildingEffect);
					}
				}
			}
		}

		public static List<BuildingEffect> GetAllEffects(SovereigntyGame Game)
		{
			if (BuildingEffect.AllEffects == null)
			{
				BuildingEffect.CreateEffects(Game);
			}
			return BuildingEffect.AllEffects.ToList<BuildingEffect>();
		}

		public static List<BuildingEffect> GetAvailableBuildings(WorkingProvince Province)
		{
			if (BuildingEffect.AllEffects == null)
			{
				BuildingEffect.CreateEffects(Province.Game);
			}
			return BuildingEffect.AllEffects.Where((BuildingEffect x) => Province.CanBuild(x)).ToList<BuildingEffect>();
		}

		public static BuildingEffect CreateEffect(SovereigntyGame Game, BuildingData Data, WorkingProvince Province)
		{
			if (BuildingEffect.AllEffects == null)
			{
				BuildingEffect.CreateEffects(Game);
			}
			BuildingEffect buildingEffect = BuildingEffect.AllEffects.SingleOrDefault((BuildingEffect x) => x.BuildingName == Data.Name);
			if (buildingEffect == null)
			{
				throw new Exception("Building effect effect for " + Data.Name + " does not exist.");
			}
			Type type = buildingEffect.GetType();
			return Game.CreateBuilding(type, Data, Province);
		}

		public static BuildingEffect LoadEffect(SovereigntyGame Game, BuildingData Data, BinaryReader r, int SaveVersion)
		{
			if (BuildingEffect.AllEffects == null)
			{
				BuildingEffect.CreateEffects(Game);
			}
			BuildingEffect buildingEffect = BuildingEffect.AllEffects.SingleOrDefault((BuildingEffect x) => x.BuildingName == Data.Name);
			if (buildingEffect == null)
			{
				throw new Exception("building effect for " + Data.Name + " does not exist.");
			}
			Type type = buildingEffect.GetType();
			BuildingEffect buildingEffect2 = (BuildingEffect)Activator.CreateInstance(type);
			buildingEffect2.Data = Data;
			buildingEffect2.Game = Game;
			buildingEffect2.LoadOK = buildingEffect2.Load(r, SaveVersion);
			return buildingEffect2;
		}

		public BuildingEffect()
		{
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.ID);
			w.Write(this.BuildingName);
			w.Write(this.ProvinceID);
			this.SaveData(w);
		}

		public bool Load(BinaryReader r, int SaveVersion)
		{
			this.ID = r.ReadInt32();
			this.BuildingName = r.ReadString();
			this.ProvinceID = r.ReadInt32();
			this.Construct(this.Province.OwnerRealm, this.Province, false);
			return this.LoadData(r);
		}

		protected abstract void SaveData(BinaryWriter w);

		protected abstract bool LoadData(BinaryReader r);

		public abstract bool CanBuildInProvince(WorkingRealm Realm, WorkingProvince Province);

		protected abstract void ApplyEffect(WorkingRealm Realm, WorkingProvince Province);

		protected abstract void RemoveEffect();

		protected abstract void ProvinceOwnerChanged(WorkingRealm OldRealm, WorkingRealm Realm);

		public abstract int GetAIBuildWeight(WorkingRealm AIRealm, WorkingProvince Province);

		public bool IsAvailable(WorkingProvince Province)
		{
			return Province.Buildings.Count((BuildingEffect x) => x.BuildingName == this.BuildingName) < this.Data.MaxNumber && Province.Game.GameCore.Data.BuildingAffinities[this.BuildingName].GetAffinity(Province.OwnerRealm.Name) != 0 && this.CanBuildInProvince(Province.OwnerRealm, Province);
		}

		public bool Demolish()
		{
			this.Province.OnOwnerChanged -= this.Province_OnOwnerChanged;
			this.RemoveEffect();
			this.Game.RemoveBuilding(this);
			return true;
		}

		public bool Construct(WorkingRealm Realm, WorkingProvince Province, bool Charge)
		{
			if (Charge)
			{
				if (Realm.AIPlayer != null)
				{
					Realm.AIPlayer.ConstructionManager.Funds.CurrentGold -= Province.GetBuildingGoldCost(this);
				}
				else
				{
					Realm.Gold.Value -= Province.GetBuildingGoldCost(this);
				}
				foreach (KeyValuePair<string, int> keyValuePair in Province.GetBuildingResourceCosts(this))
				{
					ResourceData resourceData = this.Game.GameCore.Data.Resources[keyValuePair.Key];
					Realm.RemoveResource(resourceData, keyValuePair.Value, false);
				}
			}
			this.ApplyEffect(Realm, Province);
			this.Game.AddBuilding(this);
			Province.OnOwnerChanged += this.Province_OnOwnerChanged;
			if (Charge)
			{
				this.Game.GameCore.FireEvent("BuildingConstructed", new object[] { this });
			}
			return true;
		}

		private void Province_OnOwnerChanged(WorkingProvince Province, WorkingRealm OldRealm, WorkingRealm Realm)
		{
			if (!this.IsAvailable(Province))
			{
				return;
			}
			this.ProvinceOwnerChanged(OldRealm, Realm);
		}

		public virtual int GetGoldIncome()
		{
			return 0;
		}

		public virtual int GetTreasuryIncome()
		{
			return 0;
		}

		public virtual int GetResourceIncome(ResourceData Resource)
		{
			return 0;
		}

		private static List<BuildingEffect> AllEffects;

		public string BuildingName;

		public BuildingData Data;

		public SovereigntyGame Game;

		public int ProvinceID;

		public int ID;

		internal bool LoadOK;
	}
}
