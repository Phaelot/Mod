using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenTK;
using SovereigntyTK.Data;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.Data
{
	public class SovereigntyData
	{
		public SovereigntyData(Sovereignty Game)
		{
			this.Game = Game;
		}

		public void LoadCombatTerrains(string fname)
		{
			this.CombatTerrainList = new Dictionary<int, CombatTerrainData>();
			XElement xelement = XElement.Load(File.OpenRead(fname));
			foreach (XElement xelement2 in xelement.Elements())
			{
				CombatTerrainData combatTerrainData = new CombatTerrainData(xelement2, this);
				this.CombatTerrainList.Add(combatTerrainData.ID, combatTerrainData);
			}
		}

		public void LoadCombatRoads(string fname)
		{
			this.CombatRoadList = new Dictionary<int, CombatRoadData>();
			XElement xelement = XElement.Load(File.OpenRead(fname));
			foreach (XElement xelement2 in xelement.Elements())
			{
				CombatRoadData combatRoadData = new CombatRoadData(xelement2);
				this.CombatRoadList.Add(combatRoadData.ID, combatRoadData);
			}
		}

		public void LoadCombatFeatures(string fname)
		{
			this.CombatFeatureList = new Dictionary<int, CombatFeatureData>();
			XElement xelement = XElement.Load(File.OpenRead(fname));
			foreach (XElement xelement2 in xelement.Elements())
			{
				CombatFeatureData combatFeatureData = new CombatFeatureData(xelement2);
				this.CombatFeatureList.Add(combatFeatureData.ID, combatFeatureData);
			}
		}

		public void LoadCombatMap(string Filename)
		{
			this.CombatMap = new CombatMapData(this.Game, Filename);
		}

		public void LoadNodes(string fname)
		{
			XElement xelement = XElement.Load(fname);
			this.Nodes = new List<PathingNodeData>();
			foreach (XElement xelement2 in xelement.Elements("Node"))
			{
				PathingNodeData pathingNodeData = new PathingNodeData(xelement2);
				this.Nodes.Add(pathingNodeData);
			}
			foreach (PathingNodeData pathingNodeData2 in this.Nodes)
			{
				pathingNodeData2.FixConnections(this.Nodes);
			}
			foreach (ProvinceData provinceData in this.Provinces.Values)
			{
				provinceData.SetupLinks(this.Nodes);
			}
		}

		public void LoadHitmapMeshes(string Filename)
		{
			this.ProvinceOutlines = new Dictionary<int, ProvinceOutlineData>();
			BinaryReader binaryReader = new BinaryReader(File.OpenRead(Filename));
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ProvincePolygonData provincePolygonData = new ProvincePolygonData();
				provincePolygonData.Load(binaryReader);
			}
			for (int j = 0; j < num; j++)
			{
				ProvinceOutlineData Data = new ProvinceOutlineData(binaryReader);
				Data.LoadData(binaryReader);
				Data.Province = this.Provinces.Values.SingleOrDefault((ProvinceData x) => x.Name == Data.RegionName);
				Data.Zone = this.SeaZones.Values.SingleOrDefault((SeaZoneData x) => x.Name == Data.RegionName);
				if (Data.Province != null)
				{
					Data.Province.Outline = Data;
				}
				if (Data.Zone != null)
				{
					Data.Zone.Outline = Data;
				}
				this.ProvinceOutlines.Add(Data.RegionID, Data);
			}
			binaryReader.Close();
		}

		public void LoadSpellTree(string FileName)
		{
			XElement xelement = XElement.Load(this.Game.Utilities.FileSystem.OpenFile(FileName, FileTypes.Application, FileModes.ReadOnly, true));
			List<string> list = new List<string>();
			foreach (XElement xelement2 in xelement.Elements())
			{
				string value = xelement2.Attribute("name").Value;
				if (!this.Spells.ContainsKey(value))
				{
					list.Add(value);
				}
				else
				{
					this.Spells[value].LoadTreeData(xelement2);
				}
			}
			if (list.Count > 0)
			{
				string text = "missing spells:\n";
				foreach (string text2 in list)
				{
					text = text + text2 + "\n";
				}
			}
		}

		public void LoadTables(string Filename)
		{
			if (this.ResourceValuations != null)
			{
				this.ResourceValuations.Clear();
			}
			this.ResourceValuations = null;
			if (this.DiplomaticOffsets != null)
			{
				this.DiplomaticOffsets.Clear();
			}
			this.DiplomaticOffsets = null;
			if (this.BuildingAffinities != null)
			{
				this.BuildingAffinities.Clear();
			}
			this.BuildingAffinities = null;
			XElement xelement = XElement.Load(this.Game.Utilities.FileSystem.OpenFile(Filename, FileTypes.Application, FileModes.ReadOnly, true));
			this.LoadDataTable<ResourceValueData>("RsrcValue", xelement, ref this.ResourceValuations);
			this.LoadDataTable<DiplomacyOffsetData>("Diplom Offset", xelement, ref this.DiplomaticOffsets);
			this.LoadDataTable<BuildingAffinityData>("BuildRealm", xelement, ref this.BuildingAffinities);
		}

		public void LoadData(string Filename)
		{
			XElement xelement = XElement.Load(this.Game.Utilities.FileSystem.OpenFile(Filename, FileTypes.Application, FileModes.ReadOnly, true));
			this.LoadDataTable<TerrainData>("Terrain", xelement, ref this.Terrains);
			this.LoadDataTable<UnitData>("Unit", xelement, ref this.Units);
			this.LoadDataTable<SharedUnitData>("SharedUnit", xelement, ref this.SharedUnits);
			this.LoadDataTable<RealmData>("RlmStats", xelement, ref this.Realms);
			this.LoadDataTable<ProvinceData>("ProvCoord", xelement, ref this.Provinces);
			this.LoadDataTable<RealmSelectData>("RlmSelect", xelement, ref this.SelectRealms);
			this.LoadDataTable<HeroAbilityData>("HeroPower", xelement, ref this.HeroAbilities);
			this.LoadDataTable<HeroClassData>("HeroClass", xelement, ref this.HeroClasses);
			this.LoadDataTable<ResourceData>("Resources", xelement, ref this.Resources);
			this.LoadDataTable<SeaZoneData>("SeaZone", xelement, ref this.SeaZones);
			this.LoadDataTable<RealmBulletData>("RlmBullet", xelement, ref this.RealmBullets);
			this.LoadDataTable<LustModData>("LustMod", xelement, ref this.LustMods);
			this.LoadDataTable<DiplomaticEventData>("DiplomaticEvents", xelement, ref this.DiplomaticEvents);
			this.LoadDataTable<DiplomaticConditionData>("DiplomaticConditions", xelement, ref this.DiplomaticConditions);
			this.LoadDataTable<AITraitData>("AI", xelement, ref this.AITraits);
			this.LoadDataTable<MagicLevelData>("XP", xelement, ref this.MagicLevels);
			this.LoadDataTable<AnimationData>("Animations", xelement, ref this.Animations);
			this.LoadDataTable<RealmMagicData>("Realm Magic", xelement, ref this.Spells);
			this.LoadDataTable<BuildingData>("Buildings", xelement, ref this.Buildings);
			this.LoadDataTable<UnitXPData>("UnitXP", xelement, ref this.UnitXP);
			this.ProvincesByID = new Dictionary<int, object>();
			foreach (ProvinceData provinceData in this.Provinces.Values)
			{
				this.ProvincesByID.Add(provinceData.ID, provinceData);
			}
			foreach (SeaZoneData seaZoneData in this.SeaZones.Values)
			{
				this.ProvincesByID.Add(seaZoneData.ID, seaZoneData);
			}
			this.CreateSharedUnits();
		}

		private void CreateSharedUnits()
		{
			List<UnitData> list = new List<UnitData>();
			foreach (SharedUnitData sharedUnitData in this.SharedUnits.Values)
			{
				foreach (RealmData realmData in this.Realms.Values)
				{
					bool flag = sharedUnitData.RealmRace == Races.None || realmData.Race == sharedUnitData.RealmRace;
					bool flag2 = sharedUnitData.RealmType == NavalType.All || realmData.RealmType == sharedUnitData.RealmType;
					if (flag && flag2)
					{
						UnitData unitData = new UnitData(sharedUnitData, realmData.Name);
						list.Add(unitData);
					}
				}
			}
			foreach (UnitData unitData2 in list)
			{
				if (!this.Units.ContainsKey(unitData2.ToString()))
				{
					this.Units.Add(unitData2.ToString(), unitData2);
				}
			}
		}

		public void LoadDataTable<T>(string TableName, XElement rootElement, ref Dictionary<string, T> Dict)
		{
			Type typeFromHandle = typeof(T);
			Dict = new Dictionary<string, T>();
			XElement xelement = rootElement.Elements().FirstOrDefault((XElement x) => x.Attribute("name").Value == TableName);
			if (xelement == null)
			{
				return;
			}
			foreach (XElement xelement2 in xelement.Elements())
			{
				BaseData baseData = (BaseData)Activator.CreateInstance(typeFromHandle);
				string text = baseData.LoadData(xelement2);
				if (!Dict.ContainsKey(text))
				{
					Dict.Add(text, (T)((object)Convert.ChangeType(baseData, typeof(T))));
				}
			}
		}

		internal string GetProvinceAtPoint(Vector3 MapPoint)
		{
			if (this.ProvinceOutlines == null)
			{
				return "";
			}
			foreach (ProvinceOutlineData provinceOutlineData in this.ProvinceOutlines.Values)
			{
				if (provinceOutlineData.PointInside(new PointF(MapPoint.X, MapPoint.Z)))
				{
					return provinceOutlineData.RegionName;
				}
			}
			return "";
		}

		public int GetXPForLevel(int Level)
		{
			if (Level == 0)
			{
				return 0;
			}
			if (!this.MagicLevels.ContainsKey(Level.ToString(CultureInfo.InvariantCulture)))
			{
				return 100000000;
			}
			return this.MagicLevels[Level.ToString(CultureInfo.InvariantCulture)].XP;
		}

		internal void LoadMods(List<ModData> ActiveMods)
		{
			foreach (ModData modData in ActiveMods)
			{
				string text = string.Concat(new object[]
				{
					"Mods",
					System.IO.Path.DirectorySeparatorChar,
					modData.ID,
					System.IO.Path.DirectorySeparatorChar,
					"data.xml"
				});
				Stream stream = this.Game.Utilities.FileSystem.OpenFile(text, FileTypes.User, FileModes.ReadOnly, true);
				XElement xelement = XElement.Load(stream);
				this.LoadModData(xelement.Element("Data"));
				this.LoadModText(xelement.Element("Text"));
				string text2 = System.IO.Path.GetDirectoryName(text);
				text2 = this.Game.Utilities.FileSystem.ConvertFoldername(text2, FileTypes.User, true);
				this.Game.Utilities.FileSystem.ProcessMappings(text2 + System.IO.Path.DirectorySeparatorChar + "data", text2);
			}
		}

		private void LoadModText(XElement Element)
		{
			this.Game.Utilities.TextManager.LoadChanges(Element);
		}

		private void LoadModData(XElement Element)
		{
			foreach (XElement xelement in Element.Elements("Table"))
			{
				string value;
				switch (value = xelement.Attribute("TableName").Value)
				{
				case "Terrain":
					this.LoadModChanges<TerrainData>(xelement, this.Terrains);
					break;
				case "Unit":
					this.LoadModChanges<UnitData>(xelement, this.Units);
					break;
				case "SharedUnit":
					this.LoadModChanges<SharedUnitData>(xelement, this.SharedUnits);
					break;
				case "RlmStats":
					this.LoadModChanges<RealmData>(xelement, this.Realms);
					break;
				case "ProvCoord":
					this.LoadModChanges<ProvinceData>(xelement, this.Provinces);
					break;
				case "RlmSelect":
					this.LoadModChanges<RealmSelectData>(xelement, this.SelectRealms);
					break;
				case "HeroPower":
					this.LoadModChanges<HeroAbilityData>(xelement, this.HeroAbilities);
					break;
				case "HeroClass":
					this.LoadModChanges<HeroClassData>(xelement, this.HeroClasses);
					break;
				case "Resources":
					this.LoadModChanges<ResourceData>(xelement, this.Resources);
					break;
				case "SeaZone":
					this.LoadModChanges<SeaZoneData>(xelement, this.SeaZones);
					break;
				case "RlmBullet":
					this.LoadModChanges<RealmBulletData>(xelement, this.RealmBullets);
					break;
				case "LustMod":
					this.LoadModChanges<LustModData>(xelement, this.LustMods);
					break;
				case "DiplomaticEvents":
					this.LoadModChanges<DiplomaticEventData>(xelement, this.DiplomaticEvents);
					break;
				case "DiplomaticConditions":
					this.LoadModChanges<DiplomaticConditionData>(xelement, this.DiplomaticConditions);
					break;
				case "AI":
					this.LoadModChanges<AITraitData>(xelement, this.AITraits);
					break;
				case "XP":
					this.LoadModChanges<MagicLevelData>(xelement, this.MagicLevels);
					break;
				case "Animations":
					this.LoadModChanges<AnimationData>(xelement, this.Animations);
					break;
				case "Realm Magic":
					this.LoadModChanges<RealmMagicData>(xelement, this.Spells);
					break;
				case "Buildings":
					this.LoadModChanges<BuildingData>(xelement, this.Buildings);
					break;
				case "UnitXP":
					this.LoadModChanges<UnitXPData>(xelement, this.UnitXP);
					break;
				}
			}
		}

		private void LoadModChanges<T>(XElement Table, Dictionary<string, T> Dict) where T : BaseData
		{
			Type typeFromHandle = typeof(T);
			XElement xelement = Table.Element("Deletions");
			XElement xelement2 = Table.Element("Additions");
			XElement xelement3 = Table.Element("Changes");
			foreach (XElement xelement4 in xelement.Elements("Row"))
			{
				Dict.Remove(xelement4.Value);
			}
			foreach (XElement xelement5 in xelement2.Elements("Row"))
			{
				BaseData baseData = Activator.CreateInstance(typeFromHandle) as BaseData;
				baseData.LoadModData(xelement5);
				Dict.Add(baseData.ToString(), (T)((object)baseData));
			}
			foreach (XElement xelement6 in xelement3.Elements("Row"))
			{
				string value = xelement6.Element("Key").Value;
				BaseData baseData2 = Dict[value];
				baseData2.LoadModData(xelement6);
			}
		}

		internal void LoadWorlds(FileManager FileSystem, List<ModData> ActiveMods)
		{
			string[] folderNames = FileSystem.GetFolderNames("Maps", ActiveMods);
			List<string> list = new List<string>();
			foreach (string text in folderNames)
			{
				string text2 = text + System.IO.Path.DirectorySeparatorChar + "map.xml";
				if (File.Exists(text2))
				{
					list.Add(text2);
				}
			}
			this.Worlds = new List<NewWorldData>();
			foreach (string text3 in list)
			{
				XElement xelement = XElement.Load(text3);
				NewWorldData newWorldData = new NewWorldData();
				newWorldData.InternalName = xelement.Element("Name").Value;
				newWorldData.LocalisedName = xelement.Element("DisplayName").Value;
				newWorldData.MapWidth = int.Parse(xelement.Element("Width").Value);
				newWorldData.MapHeight = int.Parse(xelement.Element("Height").Value);
				newWorldData.TileWidth = int.Parse(xelement.Element("TilesX").Value);
				newWorldData.TileHeight = int.Parse(xelement.Element("TilesY").Value);
				newWorldData.FullFilename = text3;
				this.Worlds.Add(newWorldData);
			}
		}

		public void LoadWorld(NewWorldData WorldData)
		{
			this.CurrentWorld = WorldData;
			string directoryName = System.IO.Path.GetDirectoryName(WorldData.FullFilename);
			this.LoadNodes(directoryName + System.IO.Path.DirectorySeparatorChar + "nodes.xml");
			this.LoadHitmapMeshes(directoryName + System.IO.Path.DirectorySeparatorChar + "meshdata.dat");
			this.LoadCombatTerrains(directoryName + System.IO.Path.DirectorySeparatorChar + "terrains.xml");
			this.LoadCombatRoads(directoryName + System.IO.Path.DirectorySeparatorChar + "roads.xml");
			this.LoadCombatFeatures(directoryName + System.IO.Path.DirectorySeparatorChar + "features.xml");
			this.LoadCombatMap(directoryName + System.IO.Path.DirectorySeparatorChar + "combat.sovmap");
			this.RefreshActiveProvinces();
			this.RefreshActiveZones();
			this.RefreshActiveRealms();
			this.Game.Map.ChangeMap(directoryName + System.IO.Path.DirectorySeparatorChar + "map", WorldData.MapWidth, WorldData.MapHeight);
		}

		private void RefreshActiveRealms()
		{
			this.ActiveRealms = new Dictionary<string, RealmData>();
			foreach (ProvinceData provinceData in this.ActiveProvinces.Values)
			{
				if (!this.ActiveRealms.ContainsKey(provinceData.Owner))
				{
					this.ActiveRealms.Add(provinceData.Owner, this.Realms[provinceData.Owner]);
				}
			}
			if (!this.ActiveRealms.ContainsKey("Rebels"))
			{
				this.ActiveRealms.Add("Rebels", this.Realms["Rebels"]);
			}
		}

		private void RefreshActiveProvinces()
		{
			this.ActiveProvinces = new Dictionary<string, ProvinceData>();
			foreach (ProvinceOutlineData provinceOutlineData in this.ProvinceOutlines.Values)
			{
				if (provinceOutlineData.Province != null && !this.ActiveProvinces.ContainsKey(provinceOutlineData.Province.Name))
				{
					this.ActiveProvinces.Add(provinceOutlineData.Province.Name, provinceOutlineData.Province);
				}
			}
		}

		private void RefreshActiveZones()
		{
			this.ActiveSeaZones = new Dictionary<string, SeaZoneData>();
			foreach (ProvinceOutlineData provinceOutlineData in this.ProvinceOutlines.Values)
			{
				if (provinceOutlineData.Zone != null && !this.ActiveSeaZones.ContainsKey(provinceOutlineData.Zone.Name))
				{
					this.ActiveSeaZones.Add(provinceOutlineData.Zone.Name, provinceOutlineData.Zone);
				}
			}
		}

		public Sovereignty Game;

		public Dictionary<string, TerrainData> Terrains;

		public Dictionary<string, UnitData> Units;

		public Dictionary<string, SharedUnitData> SharedUnits;

		public Dictionary<string, RealmMagicData> Spells;

		public Dictionary<string, BuildingData> Buildings;

		public Dictionary<string, BuildingAffinityData> BuildingAffinities;

		private Dictionary<string, RealmData> Realms;

		public Dictionary<string, RealmData> ActiveRealms;

		private Dictionary<string, ProvinceData> Provinces;

		public Dictionary<string, ProvinceData> ActiveProvinces;

		public Dictionary<int, object> ProvincesByID;

		public Dictionary<string, RealmSelectData> SelectRealms;

		public Dictionary<string, HeroAbilityData> HeroAbilities;

		public Dictionary<string, HeroClassData> HeroClasses;

		public Dictionary<string, ResourceData> Resources;

		private Dictionary<string, SeaZoneData> SeaZones;

		public Dictionary<string, SeaZoneData> ActiveSeaZones;

		public Dictionary<string, RealmBulletData> RealmBullets;

		public Dictionary<string, DiplomacyOffsetData> DiplomaticOffsets;

		public Dictionary<string, ResourceValueData> ResourceValuations;

		public Dictionary<string, LustModData> LustMods;

		public Dictionary<string, DiplomaticEventData> DiplomaticEvents;

		public Dictionary<string, DiplomaticConditionData> DiplomaticConditions;

		public Dictionary<string, AITraitData> AITraits;

		public Dictionary<string, MagicLevelData> MagicLevels;

		public Dictionary<string, AnimationData> Animations;

		public Dictionary<string, UnitXPData> UnitXP;

		public Dictionary<string, Bitmap> NavalStackCountersN;

		public Dictionary<string, Bitmap> NavalStackCountersH;

		public Dictionary<string, Bitmap> NavalStackCountersL;

		public Dictionary<int, CombatTerrainData> CombatTerrainList;

		public Dictionary<int, CombatFeatureData> CombatFeatureList;

		public Dictionary<int, CombatRoadData> CombatRoadList;

		public Dictionary<int, ProvinceOutlineData> ProvinceOutlines;

		public List<PathingNodeData> Nodes;

		public CombatMapData CombatMap;

		public List<NewWorldData> Worlds;

		public NewWorldData CurrentWorld;
	}
}
