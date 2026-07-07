using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2
{
	public class AIConstructionManager
	{
		public AIConstructionManager(AIPlayer AI)
		{
			this.AI = AI;
			this.Funds = new AIFundData();
		}

		internal void Dispose()
		{
		}

		private bool ShouldDisplay(BuildingEffect Building, WorkingProvince Province)
		{
			if (Province.Game.GameCore.Data.BuildingAffinities[Building.BuildingName].GetAffinity(Province.OwnerRealm.Name) == 0)
			{
				return false;
			}
			if (Building.Data.Tier > Province.CurrentEconomy)
			{
				return false;
			}
			if (!Building.CanBuildInProvince(Province.OwnerRealm, Province))
			{
				return false;
			}
			if (Building.BuildingName == "Fort")
			{
				if (Province.Buildings.Count((BuildingEffect x) => x.BuildingName == "Fort") >= Province.BaseFortLevel)
				{
					return false;
				}
			}
			return true;
		}

		internal void ChooseScience(WorkingProvince Prov)
		{
			Dictionary<ArtScienceTypes, int> dictionary = new Dictionary<ArtScienceTypes, int>();
			if (this.AI.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Alchemy) < 2)
			{
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Alchemy) == 0)
				{
					dictionary.Add(ArtScienceTypes.Alchemy, this.AI.Realm.Science_AlchemyValue);
				}
			}
			if (this.AI.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) < 2)
			{
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) == 0)
				{
					dictionary.Add(ArtScienceTypes.Engineering, this.AI.Realm.Science_EngineeringValue);
				}
			}
			if (this.AI.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) < 2)
			{
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) == 0)
				{
					dictionary.Add(ArtScienceTypes.Metallurgy, this.AI.Realm.Science_MetallurgyValue);
				}
			}
			if (this.AI.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) < 2)
			{
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) == 0)
				{
					dictionary.Add(ArtScienceTypes.Siegecraft, this.AI.Realm.Science_SiegecraftValue);
				}
			}
			int num = this.AI.RNG.Next(dictionary.Sum((KeyValuePair<ArtScienceTypes, int> x) => x.Value));
			int num2 = 0;
			ArtScienceTypes artScienceTypes = ArtScienceTypes.None;
			foreach (KeyValuePair<ArtScienceTypes, int> keyValuePair in dictionary)
			{
				num2 += keyValuePair.Value;
				if (num < num2)
				{
					artScienceTypes = keyValuePair.Key;
					break;
				}
			}
			if (artScienceTypes != ArtScienceTypes.None)
			{
				Prov.Cradle = artScienceTypes;
				Prov.UpdateCradleSprite();
				if ((artScienceTypes == ArtScienceTypes.PublicArt && Prov.IsCapitol) || artScienceTypes == ArtScienceTypes.Statecraft)
				{
					foreach (WorkingRealm workingRealm in this.AI.Game.AllRealms.Values)
					{
						if (workingRealm != this.AI.Realm && workingRealm != this.AI.Game.RebelRealm)
						{
							workingRealm.DiplomacyManager.AdjustBaseValue(this.AI.Realm, 1f);
						}
					}
				}
				if (artScienceTypes == ArtScienceTypes.PublicArt && Prov.IsCapitol)
				{
					Prov.AILust -= 10;
				}
				this.AI.Realm.CheckCradleEffects();
				this.AI.Game.GameCore.FireEvent("CradlePlaced", new object[] { Prov });
			}
		}

		internal void ChooseArts(WorkingProvince Prov)
		{
			Dictionary<ArtScienceTypes, int> dictionary = new Dictionary<ArtScienceTypes, int>();
			if (this.AI.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) < 3)
			{
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) == 0)
				{
					dictionary.Add(ArtScienceTypes.Medicine, this.AI.Realm.Arts_MedicineValue);
				}
			}
			if (this.AI.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.PublicArt) < 3)
			{
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.PublicArt) == 0)
				{
					dictionary.Add(ArtScienceTypes.PublicArt, this.AI.Realm.Arts_PublicValue);
				}
			}
			if (this.AI.Game.AllProvinces.Values.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Statecraft) < 3)
			{
				if (this.AI.Realm.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Statecraft) == 0)
				{
					dictionary.Add(ArtScienceTypes.Statecraft, this.AI.Realm.Arts_StatecraftValue);
				}
			}
			int num = this.AI.RNG.Next(dictionary.Sum((KeyValuePair<ArtScienceTypes, int> x) => x.Value));
			int num2 = 0;
			ArtScienceTypes artScienceTypes = ArtScienceTypes.None;
			foreach (KeyValuePair<ArtScienceTypes, int> keyValuePair in dictionary)
			{
				num2 += keyValuePair.Value;
				if (num < num2)
				{
					artScienceTypes = keyValuePair.Key;
					break;
				}
			}
			if (artScienceTypes != ArtScienceTypes.None)
			{
				Prov.Cradle = artScienceTypes;
				Prov.UpdateCradleSprite();
				this.AI.Game.GameCore.FireEvent("PatronPlaced", new object[] { Prov });
			}
		}

		internal void ConstructBuildings()
		{
			this.AI.Log("");
			this.AI.Log("Construction manager updating");
			this.AI.Log("  Available Funds: " + this.Funds.CurrentGold);
			foreach (WorkingProvince workingProvince in this.AI.Realm.Provinces)
			{
				this.AI.Log("  Considering province " + workingProvince.Name);
				if (workingProvince.Occupied)
				{
					this.AI.Log("    Province occupied, cannot build");
				}
				else
				{
					BuildingEffect nextBuilding = this.GetNextBuilding(workingProvince);
					if (nextBuilding == null)
					{
						this.AI.Log("    No buildings possible in province");
					}
					else
					{
						this.AI.Log("    Attempting to build " + nextBuilding.Data.Name);
						if (!workingProvince.CanBuild(nextBuilding))
						{
							this.AI.Log("    Cannot afford building");
							break;
						}
						AIActionConstructBuilding aiactionConstructBuilding = this.AI.ActionManager.CreateAction<AIActionConstructBuilding>();
						aiactionConstructBuilding.ProvinceName = workingProvince.Name;
						aiactionConstructBuilding.BuildingName = nextBuilding.BuildingName;
						this.AI.ActionManager.AddAction(aiactionConstructBuilding, true);
					}
				}
			}
		}

		internal void Save(BinaryWriter w)
		{
			this.Funds.Save(w);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.Funds.Load(r, SaveVersion);
		}

		internal int GetRequiredResource(ResourceData Resource)
		{
			int num = 0;
			foreach (WorkingProvince workingProvince in this.AI.Realm.Provinces)
			{
				BuildingEffect nextBuilding = this.GetNextBuilding(workingProvince);
				if (nextBuilding != null)
				{
					Dictionary<string, int> resourceCosts = nextBuilding.Data.ResourceCosts;
					foreach (KeyValuePair<string, int> keyValuePair in resourceCosts)
					{
						if (keyValuePair.Key.ToLowerInvariant() == Resource.ResourceName.ToLowerInvariant())
						{
							num += keyValuePair.Value;
						}
					}
				}
			}
			return num;
		}

		private BuildingEffect GetNextBuilding(WorkingProvince Province)
		{
			List<BuildingEffect> list = BuildingEffect.GetAllEffects(Province.Game);
			list = (from x in list
				where this.ShouldDisplay(x, Province)
				orderby this.AI.Game.GameCore.Data.BuildingAffinities[x.BuildingName].GetAffinity(this.AI.Realm.Name)
				select x).Take(10).ToList<BuildingEffect>();
			if (list.Count == 0)
			{
				return null;
			}
			return list.OrderBy((BuildingEffect x) => x.Data.Tier).First<BuildingEffect>();
		}

		public AIPlayer AI;

		public AIFundData Funds;
	}
}
