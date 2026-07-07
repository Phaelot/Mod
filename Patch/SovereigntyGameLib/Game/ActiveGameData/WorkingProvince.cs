// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Game.ActiveGameData.WorkingProvince
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using SovereigntyTK;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WorkingProvince : GameRegion
	{
		public int ID;

		private int m_OwnerID;

		public string Landmark;

		private bool m_IsCapitol;

		public bool HasWater;

		public bool HasHarbour;

		public string ImageFile;

		public int Value;

		public string Description;

		public Point FortCoords;

		public Point CapitolCoords;

		public Point EconCoords;

		public Point LandmarkCoords;

		public Point CradleCoords;

		public Point ResourceCoords;

		public ProvinceOutlineData Outline;

		public string ResourceName;

		public int OwnerTurnCount;

		private static Random RNG;

		public ArtScienceTypes Cradle;

		private int m_BaseEconomy;

		private int m_CurrentEconomy;

		private int m_BonusEconomy;

		private bool EconomyImproved;

		public List<int> FortIDs;

		public int LandNodeID;

		public int HarbourNodeID;

		public ProvinceStat ResearchPoints;

		public ProvinceStat RevoltChance;

		public ProvinceStat Resistance;

		public ProvinceStat ResourceIncome;

		public ProvinceStat FortLevel;

		public ProvinceStat CapitolDistanceModifier;

		public ProvinceStat IncomeMultiplier;

		public ProvinceStat BaseIncome;

		public int CurrentLoot;

		public ProvinceHistoryData OwnerHistory;

		public int PlagueTurns;

		public int PlagueImmuneTurns;

		public int RepairTurns;

		public int ActiveResistance;

		public string ResistingRealmName = "";

		public int InciteCount;

		public float InciteEffect;

		private List<int> SlaveBonuses;

		private ConstructionStates m_ConstructionState = ConstructionStates.Free;

		public List<string> AdjacentZones;

		public SpellTargetData SpellEffects;

		private GLSprite CapitolSprite;

		private GLSprite EconomySprite;

		private GLSprite FortSprite;

		private GLSprite ResourceSprite;

		private GLSprite CradleSprite;

		private GLSprite ResistSprite;

		private GLSprite LandmarkSprite;

		private GLSprite InciteSprite;

		public bool Floating;

		public UnitMoveResult DeploymentStatus;

		public BattleFieldData BattleField;

		public int AILust;

		public int SpecialEconomyBonus;

		public string NaturalOwner;

		private string NaturalOwnerAlt;

		public int RebelChanceModifier;

		public int BaseFortLevel;

		public bool IsCapitol
		{
			get
			{
				return m_IsCapitol;
			}
			set
			{
				m_IsCapitol = value;
				if (!m_IsCapitol)
				{
					m_BaseEconomy = GetMaxEconomy();
				}
			}
		}

		public ConstructionStates ConstructionState
		{
			get
			{
				return m_ConstructionState;
			}
			set
			{
				if (m_ConstructionState != value)
				{
					m_ConstructionState = value;
					if (this.OnConstructionStateChanged != null)
					{
						this.OnConstructionStateChanged(this);
					}
				}
			}
		}

		public int OwnerID
		{
			get
			{
				return m_OwnerID;
			}
			set
			{
				if (m_OwnerID != value)
				{
					OwnerTurnCount = 0;
					WorkingRealm ownerRealm = OwnerRealm;
					m_OwnerID = value;
					ownerRealm?.ProvincesChanged();
					if (m_OwnerID > 0)
					{
						Game.AllRealms[m_OwnerID].ProvincesChanged();
					}
					CheckBuildings();
					if (SlaveBonuses != null)
					{
						SlaveBonuses.Clear();
					}
					if (this.OnOwnerChanged != null)
					{
						this.OnOwnerChanged(this, ownerRealm, OwnerRealm);
					}
				}
			}
		}

		public int CurrentEconomy
		{
			get
			{
				int num = m_CurrentEconomy + m_BonusEconomy;
				if (SlaveBonuses != null)
				{
					num += SlaveBonuses.Count;
				}
				return Math.Max(0, num);
			}
		}

		public int BaseEconomy
		{
			get
			{
				return m_BaseEconomy;
			}
			set
			{
				m_BaseEconomy = value;
				Game.GameCore.FireEvent("EconomyChanged", this);
				UpdateEconSprite();
			}
		}

		public int BonusEconomy
		{
			get
			{
				int num = m_BonusEconomy;
				if (SlaveBonuses != null)
				{
					num += SlaveBonuses.Count;
				}
				return num;
			}
			set
			{
				m_BonusEconomy = value;
				Game.GameCore.FireEvent("EconomyChanged", this);
				UpdateEconSprite();
			}
		}

		public bool HasRiver => LandNode.ConnectedNodes.Count((ActiveNodeConnection x) => x.ConnectionType == ConnectionTypes.Bridge || x.ConnectionType == ConnectionTypes.River) > 0;

		public bool EconomyDamaged => m_CurrentEconomy < m_BaseEconomy;

		public bool Occupied => OccupierRealm != OwnerRealm;

		public WorkingRealm OwnerRealm
		{
			get
			{
				WorkingRealm value = null;
				Game.AllRealms.TryGetValue(OwnerID, out value);
				return value;
			}
		}

		public WorkingRealm OccupierRealm
		{
			get
			{
				if (LandNode.CurrentStack == null)
				{
					return OwnerRealm;
				}
				return LandNode.CurrentStack.Owner;
			}
		}

		public ResourceData Resource
		{
			get
			{
				if (ResourceName == null)
				{
					return null;
				}
				ResourceData value = null;
				Game.GameCore.Data.Resources.TryGetValue(ResourceName, out value);
				return value;
			}
		}

		public ActivePathNode LandNode
		{
			get
			{
				ActivePathNode value = null;
				Game.AllNodes.TryGetValue(LandNodeID, out value);
				return value;
			}
		}

		public ActivePathNode HarbourNode
		{
			get
			{
				ActivePathNode value = null;
				Game.AllNodes.TryGetValue(HarbourNodeID, out value);
				return value;
			}
		}

		public IList<WorkingUnit> Forts
		{
			get
			{
				List<WorkingUnit> list = new List<WorkingUnit>();
				foreach (int item in FortIDs.ToList())
				{
					WorkingUnit value = null;
					Game.AllUnits.TryGetValue(item, out value);
					if (value == null)
					{
						UnitData unitByClass = OwnerRealm.UnitPurchaseManager.GetUnitByClass(UnitClasses.Fort);
						if (unitByClass == null)
						{
							throw new Exception("Unable to create forts, " + OwnerRealm.Name + " has no fort type defined");
						}
						value = Game.CreateUnit(OwnerID, unitByClass);
						FortIDs.Remove(item);
						FortIDs.Add(value.ID);
					}
					list.Add(value);
				}
				return list.AsReadOnly();
			}
		}

		public IList<BuildingEffect> Buildings => Game.AllBuildings.Values.Where((BuildingEffect x) => x.ProvinceID == ID).ToList().AsReadOnly();

		public event ProvinceRealmDelegate OnOwnerChanged;

		public event ProvincestatusDelegate OnStatusRequested;

		public event ProvinceValueDelegate OnBattleTurnModifierRequested;

		public event UnitValueDelegate OnWanderChanceRequested;

		public event ProvinceDelegate OnConstructionStateChanged;

		public WorkingProvince(int ID, int OwnerID, SovereigntyGame Game, ProvinceData Data)
		{
			if (RNG == null)
			{
				RNG = new Random();
			}
			this.ID = ID;
			base.Game = Game;
			this.OwnerID = OwnerID;
			RegionID = Data.ID;
			OwnerTurnCount = 100;
			AdjacentZones = new List<string>();
			foreach (ProvinceLink Link in Data.AdjacentZones)
			{
				Dictionary<string, SeaZoneData>.ValueCollection values = Game.Data.ActiveSeaZones.Values;
				Func<SeaZoneData, bool> predicate = (SeaZoneData x) => x.ID == Link.LinkedProvinceID;
				SeaZoneData seaZoneData = values.FirstOrDefault(predicate);
				AdjacentZones.Add(seaZoneData.Name);
			}
			NaturalOwner = Data.NaturalOwner;
			if (Data.AltOwner != null && Data.AltOwner != "None")
			{
				NaturalOwnerAlt = Data.AltOwner;
			}
			else
			{
				NaturalOwnerAlt = "";
			}
			if (Data.Landmark != null)
			{
				Landmark = Data.Landmark;
			}
			else
			{
				Landmark = "";
			}
			Name = Data.Name;
			DisplayName = Data.DisplayName;
			HasWater = Data.HasWater;
			HasHarbour = Data.HasHarbour;
			IsCapitol = Data.IsCapitol;
			TerrainString = Data.Terrain;
			ImageFile = Data.ImageFile;
			m_BaseEconomy = Data.EconomyValue;
			m_CurrentEconomy = m_BaseEconomy;
			Value = Data.Value;
			Description = Data.Description;
			FortCoords = Data.FortCoords;
			CapitolCoords = Data.CapitolCoords;
			EconCoords = Data.EconCoords;
			LandmarkCoords = Data.LandmarkCoords;
			CradleCoords = Data.CradleCoords;
			ResourceCoords = Data.ResourceCoords;
			BaseIncome = new ProvinceStat(Game, ID, 0);
			BaseIncome.OnRequestModifier += BaseIncome_OnRequestModifier;
			ResearchPoints = new ProvinceStat(Game, ID, Data.XP);
			RevoltChance = new ProvinceStat(Game, ID, 0);
			RevoltChance.OnRequestModifier += RevoltChance_OnRequestModifier;
			RevoltChance.OnRequestModifierList += RevoltChance_OnRequestModifierList;
			Resistance = new ProvinceStat(Game, ID, 2);
			Resistance.OnRequestModifier += Resistance_OnRequestModifier;
			ResourceIncome = new ProvinceStat(Game, ID, 1);
			CapitolDistanceModifier = new ProvinceStat(Game, ID, 0, AllowNegativeNumbers: true);
			FortLevel = new ProvinceStat(Game, ID, 0);
			BaseFortLevel = Data.FortLevel;
			IncomeMultiplier = new ProvinceStat(Game, ID, 100);
			Outline = Data.Outline;
			if (Data.Resource != null)
			{
				ResourceName = Data.Resource;
			}
			Game.GameCore.Utilities.SpriteManager.GetBatch("Data\\Images\\buttons\\button_round_normal.png");
			Game.GameCore.Utilities.SpriteManager.GetBatch("Data\\Images\\buttons\\button_round_mouseover.png");
			Game.GameCore.Utilities.SpriteManager.GetBatch("Data\\Images\\buttons\\button_round_pressed.png");
			if (IsCapitol)
			{
				CreateCapitolSprite();
			}
			if (HasLandmark())
			{
				CreateLandmarkSprite();
			}
			UpdateFortSprite();
			UpdateResourceSprite();
			FortIDs = new List<int>();
			UnitData unitByClass = OwnerRealm.UnitPurchaseManager.GetUnitByClass(UnitClasses.Fort);
			if (unitByClass == null)
			{
				throw new Exception("Unable to create forts, " + OwnerRealm.Name + " has no fort type defined");
			}
			for (int num = 0; num < 5; num++)
			{
				WorkingUnit workingUnit = Game.CreateUnit(OwnerID, unitByClass);
				FortIDs.Add(workingUnit.ID);
			}
			SpellEffects = new SpellTargetData(Game);
			SlaveBonuses = new List<int>();
			OwnerHistory = new ProvinceHistoryData(OwnerID);
		}

		public void CreateForts()
		{
			for (int i = 0; i < BaseFortLevel; i++)
			{
				BuildingEffect buildingEffect = BuildingEffect.CreateEffect(Game, Game.GameCore.Data.Buildings["Fort"], this);
				buildingEffect.Construct(OwnerRealm, this, Charge: false);
			}
			UpdateFortSprite();
		}

		private void BaseIncome_OnRequestModifier(WorkingProvince Province, ref int Value)
		{
			Value += 150 * CurrentEconomy;
		}

		private void Resistance_OnRequestModifier(WorkingProvince Province, ref int Value)
		{
			Value += (int)FortLevel + 1;
		}

		private void RevoltChance_OnRequestModifier(WorkingProvince Province, ref int Value)
		{
			Value += (int)GetBaseRevoltChance();
			Value += (int)InciteEffect;
		}

		public WorkingProvince(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			if (RNG == null)
			{
				RNG = new Random();
			}
			base.Game = Game;
			ID = r.ReadInt32();
			Name = r.ReadString();
			DisplayName = r.ReadString();
			TerrainString = r.ReadString();
			RegionID = r.ReadInt32();
			m_OwnerID = r.ReadInt32();
			r.ReadInt32();
			Landmark = r.ReadString();
			IsCapitol = r.ReadBoolean();
			HasWater = r.ReadBoolean();
			HasHarbour = r.ReadBoolean();
			ImageFile = r.ReadString();
			Value = r.ReadInt32();
			Description = r.ReadString();
			LoadCoords(r, ref FortCoords);
			LoadCoords(r, ref CapitolCoords);
			LoadCoords(r, ref EconCoords);
			LoadCoords(r, ref LandmarkCoords);
			LoadCoords(r, ref CradleCoords);
			LoadCoords(r, ref ResourceCoords);
			ResourceName = r.ReadString();
			if (ResourceName == "")
			{
				ResourceName = null;
			}
			if (SaveVersion < GlobalData.SAVEVERSION_EA3)
			{
				ResearchPoints = new ProvinceStat(Game, ID, r.ReadInt32());
			}
			else
			{
				ResearchPoints = new ProvinceStat(Game, ID, 0);
				ResearchPoints.Load(r, SaveVersion);
			}
			Cradle = (ArtScienceTypes)r.ReadInt16();
			m_BaseEconomy = r.ReadInt32();
			m_CurrentEconomy = r.ReadInt32();
			int initialValue = 0;
			if (SaveVersion < GlobalData.SAVEVERSION_EA3)
			{
				initialValue = r.ReadInt32();
				r.ReadInt32();
			}
			int num = r.ReadInt32();
			FortIDs = new List<int>();
			for (int i = 0; i < num; i++)
			{
				FortIDs.Add(r.ReadInt32());
			}
			LandNodeID = r.ReadInt32();
			HarbourNodeID = r.ReadInt32();
			PlagueTurns = r.ReadInt32();
			PlagueImmuneTurns = r.ReadInt32();
			RepairTurns = r.ReadInt32();
			num = r.ReadInt32();
			AdjacentZones = new List<string>();
			for (int j = 0; j < num; j++)
			{
				AdjacentZones.Add(r.ReadString());
			}
			SpellEffects = new SpellTargetData(Game);
			Floating = r.ReadBoolean();
			if (r.ReadBoolean())
			{
				BattleField = new BattleFieldData(this, Game);
				BattleField.Load(r, SaveVersion);
			}
			AILust = r.ReadInt32();
			SpecialEconomyBonus = r.ReadInt32();
			OwnerTurnCount = r.ReadInt32();
			NaturalOwner = r.ReadString();
			NaturalOwnerAlt = r.ReadString();
			RebelChanceModifier = r.ReadInt32();
			SlaveBonuses = new List<int>();
			if (SaveVersion >= GlobalData.SAVEVERSION_EA3)
			{
				num = r.ReadInt32();
				for (int k = 0; k < num; k++)
				{
					SlaveBonuses.Add(r.ReadInt32());
				}
			}
			Game.GameCore.Utilities.SpriteManager.GetBatch("Data\\Images\\buttons\\button_round_normal.png");
			Game.GameCore.Utilities.SpriteManager.GetBatch("Data\\Images\\buttons\\button_round_mouseover.png");
			Game.GameCore.Utilities.SpriteManager.GetBatch("Data\\Images\\buttons\\button_round_pressed.png");
			BaseIncome = new ProvinceStat(Game, ID, 0);
			BaseIncome.OnRequestModifier += BaseIncome_OnRequestModifier;
			Resistance = new ProvinceStat(Game, ID, 2);
			RevoltChance = new ProvinceStat(Game, ID, 0);
			RevoltChance.OnRequestModifier += RevoltChance_OnRequestModifier;
			RevoltChance.OnRequestModifierList += RevoltChance_OnRequestModifierList;
			ResourceIncome = new ProvinceStat(Game, ID, 0);
			FortLevel = new ProvinceStat(Game, ID, initialValue);
			CapitolDistanceModifier = new ProvinceStat(Game, ID, 0, AllowNegativeNumbers: true);
			IncomeMultiplier = new ProvinceStat(Game, ID, 100);
			if (SaveVersion >= GlobalData.SAVEVERSION_EA3)
			{
				BaseIncome.Load(r, SaveVersion);
				Resistance.Load(r, SaveVersion);
				RevoltChance.Load(r, SaveVersion);
				ResourceIncome.Load(r, SaveVersion);
				FortLevel.Load(r, SaveVersion);
				CapitolDistanceModifier.Load(r, SaveVersion);
				IncomeMultiplier.Load(r, SaveVersion);
				ConstructionState = (ConstructionStates)r.ReadInt16();
				ActiveResistance = r.ReadInt32();
				ResistingRealmName = r.ReadString();
				BaseFortLevel = r.ReadInt32();
			}
			OwnerHistory = new ProvinceHistoryData(OwnerID);
			if (SaveVersion >= 58)
			{
				InciteCount = r.ReadInt32();
				InciteEffect = r.ReadSingle();
				OwnerHistory.Load(r, SaveVersion);
				CurrentLoot = r.ReadInt32();
			}
			if (IsCapitol)
			{
				CreateCapitolSprite();
			}
			if (HasLandmark())
			{
				CreateLandmarkSprite();
			}
			UpdateFortSprite();
			UpdateResourceSprite();
			UpdateCradleSprite();
			UpdateInciteIcon();
		}

		public int GetWanderChance(WorkingUnit Unit)
		{
			int Value = 0;
			if (this.OnWanderChanceRequested != null)
			{
				this.OnWanderChanceRequested(Unit, ref Value);
			}
			return Value;
		}

		private void RevoltChance_OnRequestModifierList(WorkingProvince Province, List<GameText> ModifierList)
		{
			if (IsCapitol)
			{
				ModifierList.Add(GameText.CreateLocalised("REVOLT_CAPITOL"));
				return;
			}
			if (OccupierRealm == Game.RebelRealm)
			{
				ModifierList.Add(GameText.CreateLocalised("REVOLT_REBEL"));
				return;
			}
			GameText gameText = GameText.CreateLocalised("FORMAT_PRISONERS", 3);
			gameText.AddChildText(GameText.CreateLocalised("REVOLT_BASE"));
			ModifierList.Add(gameText);
			int num = Math.Min(OwnerTurnCount, 100);
			int num2 = 0;
			while (num > 0)
			{
				num -= 5;
				num2--;
			}
			gameText = GameText.CreateLocalised("FORMAT_PRISONERS", num2);
			gameText.AddChildText(GameText.CreateLocalised("REVOLT_OWNERSHIP"));
			ModifierList.Add(gameText);
			gameText = GameText.CreateLocalised("FORMAT_PRISONERS", SpellEffects.ActiveSpells.Count((SpellEffect x) => x.SpellData.Type == SpellTypes.Negative));
			gameText.AddChildText(GameText.CreateLocalised("REVOLT_SPELLNEG"));
			ModifierList.Add(gameText);
			gameText = GameText.CreateLocalised("FORMAT_PRISONERS", SpellEffects.ActiveSpells.Count((SpellEffect x) => x.SpellData.Type == SpellTypes.Positive));
			gameText.AddChildText(GameText.CreateLocalised("REVOLT_SPELLPOS"));
			ModifierList.Add(gameText);
			if (PlagueTurns > 0)
			{
				gameText = GameText.CreateLocalised("FORMAT_PRISONERS", 3);
				gameText.AddChildText(GameText.CreateLocalised("REVOLT_PLAGUE"));
				ModifierList.Add(gameText);
			}
			if (LandNode.CurrentStack != null)
			{
				num2 = -1 * LandNode.CurrentStack.Units.Sum((WorkingUnit x) => x.GetRevoltReduction());
				if (LandNode.CurrentStack.Hero != null)
				{
					num2 -= 3;
				}
				gameText = GameText.CreateLocalised("FORMAT_PRISONERS", num2);
				gameText.AddChildText(GameText.CreateLocalised("REVOLT_UNITS"));
				ModifierList.Add(gameText);
			}
			if ((int)FortLevel > 0)
			{
				gameText = GameText.CreateLocalised("FORMAT_PRISONERS", -(int)FortLevel);
				gameText.AddChildText(GameText.CreateLocalised("REVOLT_FORTS"));
				ModifierList.Add(gameText);
			}
			if (NaturalOwner == OccupierRealm.Name)
			{
				gameText = GameText.CreateLocalised("FORMAT_PRISONERS", -3);
				gameText.AddChildText(GameText.CreateLocalised("REVOLT_NATURAL"));
				ModifierList.Add(gameText);
			}
			else if (NaturalOwnerAlt == OccupierRealm.Name)
			{
				gameText = GameText.CreateLocalised("FORMAT_PRISONERS", -2);
				gameText.AddChildText(GameText.CreateLocalised("REVOLT_NATURAL"));
				ModifierList.Add(gameText);
			}
			else
			{
				gameText = GameText.CreateLocalised("FORMAT_PRISONERS", CurrentEconomy);
				gameText.AddChildText(GameText.CreateLocalised("REVOLT_TAKEN"));
				ModifierList.Add(gameText);
			}
			int num3 = (int)InciteEffect;
			if (num3 > 0)
			{
				gameText = GameText.CreateLocalised("FORMAT_PRISONERS", num3);
				gameText.AddChildText(GameText.CreateLocalised("REVOLT_INCITE"));
				ModifierList.Add(gameText);
			}
		}

		public float GetBaseRevoltChance()
		{
			if (IsCapitol)
			{
				return 0f;
			}
			if (OccupierRealm == Game.RebelRealm)
			{
				return 0f;
			}
			float num = 3f;
			int num2 = Math.Min(OwnerTurnCount, 100);
			while (num2 > 0)
			{
				num2 -= 5;
				num -= 1f;
			}
			num += (float)SpellEffects.ActiveSpells.Count((SpellEffect x) => x.SpellData.Type == SpellTypes.Negative);
			if (PlagueTurns > 0)
			{
				num += 3f;
			}
			num -= (float)SpellEffects.ActiveSpells.Count((SpellEffect x) => x.SpellData.Type == SpellTypes.Positive);
			if (LandNode.CurrentStack != null)
			{
				num -= (float)LandNode.CurrentStack.Units.Sum((WorkingUnit x) => x.GetRevoltReduction());
				if (LandNode.CurrentStack.Hero != null)
				{
					num -= 3f;
				}
			}
			num -= (float)(int)FortLevel;
			num = ((NaturalOwner == OccupierRealm.Name) ? (num - 3f) : ((!(NaturalOwnerAlt == OccupierRealm.Name)) ? (num + (float)CurrentEconomy) : (num - 2f)));
			return num + (float)RebelChanceModifier;
		}

		public void UpdateCradleSprite()
		{
			if (CradleSprite != null)
			{
				CradleSprite.Dispose();
			}
			string text = "";
			string textName = "";
			string textName2 = "";
			switch (Cradle)
			{
				case ArtScienceTypes.Alchemy:
					text = "alchemy_icon.png";
					textName = "CRADLE_ALCHEMY_DESC";
					textName2 = "MAPTOOLTIPCRADLE";
					break;
				case ArtScienceTypes.Engineering:
					text = "Engineering_icon.png";
					textName = "CRADLE_ENGINE_DESC";
					textName2 = "MAPTOOLTIPCRADLE";
					break;
				case ArtScienceTypes.Medicine:
					text = "Medicine_icon.png";
					textName = "PATRON_MEDICINE_DESC";
					textName2 = "MAPTOOLTIPPATRON";
					break;
				case ArtScienceTypes.Metallurgy:
					text = "Metallurgy_icon.png";
					textName = "CRADLE_METAL_DESC";
					textName2 = "MAPTOOLTIPCRADLE";
					break;
				case ArtScienceTypes.PublicArt:
					text = "PublicArt_icon.png";
					textName = "PATRON_PUBLIC_DESC";
					textName2 = "MAPTOOLTIPPATRON";
					break;
				case ArtScienceTypes.Siegecraft:
					text = "Siegecraft_icon.png";
					textName = "CRADLE_SIEGE_DESC";
					textName2 = "MAPTOOLTIPCRADLE";
					break;
				case ArtScienceTypes.Statecraft:
					text = "Statecraft_icon.png";
					textName = "PATRON_STATE_DESC";
					textName2 = "MAPTOOLTIPPATRON";
					break;
			}
			if (text != "")
			{
				GameText gameText = GameText.CreateLocalised(textName2);
				gameText.AddChildText(GameText.CreateLocalised(textName));
				CreateSprite(ref CradleSprite, text, CradleCoords, gameText);
			}
			UpdateMapVisibilityState();
		}

		private void LoadCoords(BinaryReader r, ref Point Coords)
		{
			Coords.X = r.ReadInt32();
			Coords.Y = r.ReadInt32();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(ID);
			w.Write(Name);
			w.Write(DisplayName);
			w.Write(TerrainString);
			w.Write(RegionID);
			w.Write(m_OwnerID);
			w.Write(0);
			w.Write(Landmark);
			w.Write(IsCapitol);
			w.Write(HasWater);
			w.Write(HasHarbour);
			w.Write(ImageFile);
			w.Write(Value);
			w.Write(Description);
			SaveCoords(w, FortCoords);
			SaveCoords(w, CapitolCoords);
			SaveCoords(w, EconCoords);
			SaveCoords(w, LandmarkCoords);
			SaveCoords(w, CradleCoords);
			SaveCoords(w, ResourceCoords);
			if (ResourceName == null)
			{
				w.Write("");
			}
			else
			{
				w.Write(ResourceName);
			}
			ResearchPoints.Save(w);
			w.Write((short)Cradle);
			w.Write(m_BaseEconomy);
			w.Write(m_CurrentEconomy);
			w.Write(FortIDs.Count);
			foreach (int fortID in FortIDs)
			{
				w.Write(fortID);
			}
			w.Write(LandNodeID);
			w.Write(HarbourNodeID);
			w.Write(PlagueTurns);
			w.Write(PlagueImmuneTurns);
			w.Write(RepairTurns);
			w.Write(AdjacentZones.Count);
			foreach (string adjacentZone in AdjacentZones)
			{
				w.Write(adjacentZone);
			}
			w.Write(Floating);
			if (BattleField == null)
			{
				w.Write(value: false);
			}
			else
			{
				w.Write(value: true);
				BattleField.Save(w);
			}
			w.Write(AILust);
			w.Write(SpecialEconomyBonus);
			w.Write(OwnerTurnCount);
			w.Write(NaturalOwner);
			w.Write(NaturalOwnerAlt);
			w.Write(RebelChanceModifier);
			w.Write(SlaveBonuses.Count);
			foreach (int slaveBonuse in SlaveBonuses)
			{
				w.Write(slaveBonuse);
			}
			BaseIncome.Save(w);
			Resistance.Save(w);
			RevoltChance.Save(w);
			ResourceIncome.Save(w);
			FortLevel.Save(w);
			CapitolDistanceModifier.Save(w);
			IncomeMultiplier.Save(w);
			w.Write((short)ConstructionState);
			w.Write(ActiveResistance);
			w.Write(ResistingRealmName);
			w.Write(BaseFortLevel);
			w.Write(InciteCount);
			w.Write(InciteEffect);
			OwnerHistory.Save(w);
			w.Write(CurrentLoot);
		}

		private void SaveCoords(BinaryWriter w, Point Coords)
		{
			w.Write(Coords.X);
			w.Write(Coords.Y);
		}

		public void AddCapitol()
		{
			if (!IsCapitol)
			{
				CreateCapitolSprite();
				IsCapitol = true;
			}
		}

		private void CreateCapitolSprite()
		{
			CreateSprite(ref CapitolSprite, "plainscapital.png", CapitolCoords, GameText.CreateLocalised("MAPTOOLTIPCAPITOL"));
			UpdateMapVisibilityState();
		}

		private void CreateLandmarkSprite()
		{
			CreateSprite(ref LandmarkSprite, "landmark.png", LandmarkCoords, null);
			UpdateMapVisibilityState();
		}

		public void RemoveCapitol()
		{
			if (IsCapitol)
			{
				CapitolSprite.Dispose();
				IsCapitol = false;
				if (m_CurrentEconomy > GetMaxEconomy())
				{
					m_CurrentEconomy = GetMaxEconomy();
				}
			}
		}

		public bool EconomyIsMaximum()
		{
			return m_CurrentEconomy >= GetMaxEconomy();
		}

		public void ChangeResource(string NewResource)
		{
			ResourceName = NewResource;
			UpdateResourceSprite();
		}

		public void UpdatePlague()
		{
			OwnerTurnCount++;
			if (PlagueTurns > 0)
			{
				PlagueTurns--;
				_ = PlagueTurns;
			}
			if (PlagueImmuneTurns > 0)
			{
				PlagueImmuneTurns--;
			}
			if (m_BaseEconomy > m_CurrentEconomy)
			{
				RepairTurns++;
			}
			EconomyImproved = false;
			for (int i = 0; i < SlaveBonuses.Count; i++)
			{
				SlaveBonuses[i]--;
			}
			if (SlaveBonuses.RemoveAll((int x) => x <= 0) > 0)
			{
				Game.GameCore.FireEvent("EconomyChanged", this);
				UpdateEconSprite();
			}
			ConstructionState = ConstructionStates.Free;
			if (InciteEffect > 0f)
			{
				InciteEffect -= 0.5f;
			}
			float num = CurrentEconomy * 200;
			float num2 = CurrentLoot;
			num2 = ((!Occupied) ? (num2 + num * 0.1f) : (num2 + num * 0.04f));
			if (num2 > num)
			{
				num2 = num;
			}
			CurrentLoot = (int)num2;
		}

		private void UpdateResourceSprite()
		{
			if (ResourceSprite != null)
			{
				ResourceSprite.Dispose();
			}
			if (Resource != null)
			{
				ResourceSprite = Game.GameCore.Utilities.SpriteManager.CreateSprite("Data\\Images\\HUD\\Tradeicons\\" + Resource.ResourceName + ".png", Interactive: true);
				ResourceSprite.SetSize(24f, 24f);
				ResourceSprite.SetPosition(ResourceCoords.X, ResourceCoords.Y);
				ResourceSprite.IgnoreMouseClicks = true;
				ResourceSprite.Tooltip = GameText.CreateLocalised("MAPTOOLTIPRESOURCE");
				ResourceSprite.Tooltip.AddChildText(GameText.CreateLocalised(Resource.DisplayName));
			}
			UpdateMapVisibilityState();
		}

		public void UpdateFortSprite()
		{
			if (FortSprite != null)
			{
				FortSprite.Dispose();
			}
			if (FortLevel.GetValue() != 0)
			{
				string iconName = "plains0" + FortLevel.GetValue() + ".png";
				CreateSprite(ref FortSprite, iconName, FortCoords, GameText.CreateLocalised("MAPTOOLTIPFORTS", FortLevel.GetValue()));
			}
			UpdateMapVisibilityState();
		}

		public void UpdateEconSprite()
		{
			if (EconomySprite != null)
			{
				EconomySprite.Dispose();
			}
			if (ResistSprite != null)
			{
				ResistSprite.Dispose();
			}
			if (ResistingRealmName == "")
			{
				int provinceIncome = Game.EconomyController.GetProvinceIncome(this, OwnerRealm.CapitolProvince, 1f);
				string iconName = CurrentEconomy + ".png";
				GameText gameText = GameText.CreateLocalised("MAPTOOLTIPECONOMY");
				gameText.AddChildText(GameText.CreateLocalised("FORMAT_PROVINCEINCOME", provinceIncome, BaseIncome.GetValue()));
				CreateSprite(ref EconomySprite, iconName, EconCoords, gameText);
				EconomySprite.OnMouseEnter += EconomySprite_OnMouseEnter;
			}
			else
			{
				string iconName2 = "d" + ActiveResistance + ".png";
				CreateSprite(ref EconomySprite, iconName2, EconCoords, GameText.CreateLocalised("MAPTOOLTIPRESIST"));
				ResistSprite = Game.GameCore.Utilities.SpriteManager.CreateSprite("Data\\Images\\HUD\\Info\\resistanceicon.png");
				ResistSprite.SetSize(32f, 32f);
				ResistSprite.SetPosition(EconCoords.X + 16, EconCoords.Y - 16);
				ResistSprite.BringToFront();
			}
			UpdateMapVisibilityState();
		}

		private void EconomySprite_OnMouseEnter(GLBaseSprite Sprite)
		{
			int provinceIncome = Game.EconomyController.GetProvinceIncome(this, OwnerRealm.CapitolProvince, 1f);
			_ = CurrentEconomy + ".png";
			GameText gameText = GameText.CreateLocalised("MAPTOOLTIPECONOMY");
			gameText.AddChildText(GameText.CreateLocalised("FORMAT_PROVINCEINCOME", provinceIncome, BaseIncome.GetValue()));
			EconomySprite.Tooltip = gameText;
		}

		private void CreateSprite(ref GLSprite Sprite, string IconName, Point Coords, GameText Tooltip)
		{
			Sprite = Game.GameCore.Utilities.SpriteManager.CreateIndexedSprite("Data\\Images\\Map\\mapicons.png", IconName, Interactive: true);
			Sprite.SetSize(32f, 32f);
			Sprite.SetPosition(Coords.X, Coords.Y);
			Sprite.Tooltip = Tooltip;
			Sprite.IgnoreMouseClicks = true;
		}

		private bool IsVisibleOnCampaignMap()
		{
			if (this.Game == null || this.Game.GameCore == null || this.Game.GameCore.Map == null)
			{
				return true;
			}
			return this.Game.GameCore.Map.IsRegionVisible(this);
		}

		private void ApplySpriteVisibility(GLSprite sprite, bool visible)
		{
			if (sprite == null)
			{
				return;
			}
			sprite.SetAlpha(visible ? 1f : 0f);
			sprite.IgnoreMouse = !visible;
			sprite.IgnoreMouseClicks = !visible;
		}

		public void UpdateMapVisibilityState()
		{
			bool visible = IsVisibleOnCampaignMap();
			if (ResistingRealmName != "")
			{
				ApplySpriteVisibility(EconomySprite, visible);
				ApplySpriteVisibility(ResistSprite, visible);
			}
			else
			{
				ApplySpriteVisibility(EconomySprite, visible: true);
			}
		}

		public void CreateLinks(ProvinceData Data)
		{
		}

		public void Dispose()
		{
			this.OnOwnerChanged = null;
			this.OnStatusRequested = null;
			this.OnWanderChanceRequested = null;
			this.OnBattleTurnModifierRequested = null;
			if (EconomySprite != null)
			{
				EconomySprite.Dispose();
			}
			if (CapitolSprite != null)
			{
				CapitolSprite.Dispose();
			}
			if (FortSprite != null)
			{
				FortSprite.Dispose();
			}
			if (ResourceSprite != null)
			{
				ResourceSprite.Dispose();
			}
			if (CradleSprite != null)
			{
				CradleSprite.Dispose();
			}
			if (BattleField != null)
			{
				BattleField.Dispose();
			}
			if (ResistSprite != null)
			{
				ResistSprite.Dispose();
			}
			if (LandmarkSprite != null)
			{
				LandmarkSprite.Dispose();
			}
			if (InciteSprite != null)
			{
				InciteSprite.Dispose();
			}
			EconomySprite = null;
			CapitolSprite = null;
		}

		public int GetIncomeIncrease()
		{
			float num = 150f;
			float provinceDistancePenalty = Game.EconomyController.GetProvinceDistancePenalty(this, OwnerRealm.CapitolProvince);
			return (int)(num * provinceDistancePenalty);
		}

		private int GetMaxEconomy()
		{
			int num = 5;
			if (IsCapitol)
			{
				num = 9;
			}
			else if (HarbourNode != null)
			{
				num = 7;
			}
			return num + SpecialEconomyBonus;
		}

		public int GetUnmodifiedEconomy()
		{
			return m_CurrentEconomy;
		}

		public void DamageEconomy(int Amount)
		{
			m_CurrentEconomy -= Amount;
			if (m_CurrentEconomy < 0)
			{
				m_CurrentEconomy = 0;
			}
			Game.GameCore.FireEvent("EconomyChanged", this);
			UpdateEconSprite();
		}

		public void ImproveEconomy(int Amount)
		{
			RepairTurns = 0;
			EconomyImproved = true;
			m_CurrentEconomy += Amount;
			if (m_CurrentEconomy > GetMaxEconomy())
			{
				m_CurrentEconomy = GetMaxEconomy();
			}
			if (m_CurrentEconomy > m_BaseEconomy)
			{
				m_BaseEconomy = m_CurrentEconomy;
			}
			Game.GameCore.FireEvent("EconomyChanged", this);
			UpdateEconSprite();
		}

		public bool HasStatus(string StatusName)
		{
			bool Value = false;
			if (this.OnStatusRequested != null)
			{
				this.OnStatusRequested(StatusName, ref Value, this);
			}
			return Value;
		}

		public void RestoreDamage()
		{
			m_CurrentEconomy = m_BaseEconomy;
			Game.GameCore.FireEvent("EconomyChanged", this);
			UpdateEconSprite();
		}

		public void ForceEconomyBoost(int Amount)
		{
			m_BaseEconomy += Amount;
			m_CurrentEconomy += Amount;
			Game.GameCore.FireEvent("EconomyChanged", this);
			UpdateEconSprite();
		}

		public void ForceEconomyDamage(int Amount)
		{
			m_BaseEconomy -= Amount;
			m_CurrentEconomy -= Amount;
			if (m_BaseEconomy < 0)
			{
				m_BaseEconomy = 0;
			}
			if (m_CurrentEconomy < 0)
			{
				m_CurrentEconomy = 0;
			}
			Game.GameCore.FireEvent("EconomyChanged", this);
			UpdateEconSprite();
		}

		internal bool PlaguePossible()
		{
			if (PlagueImmuneTurns > 0)
			{
				return false;
			}
			if (PlagueTurns > 0)
			{
				return false;
			}
			if (RepairTurns < 3)
			{
				return false;
			}
			if (!EconomyDamaged)
			{
				return false;
			}
			if (Cradle == ArtScienceTypes.Medicine)
			{
				return false;
			}
			return true;
		}

		public int GetImproveCost(WorkingRealm Realm)
		{
			if (m_CurrentEconomy < m_BaseEconomy)
			{
				return GetRepairCost(Realm);
			}
			return GetUpgradeCost(Realm);
		}

		private int GetRepairCost(WorkingRealm Realm)
		{
			return GetUpgradeCost(Realm) / 2;
		}

		private int GetUpgradeCost(WorkingRealm Realm)
		{
			float num = 4500f;
			num *= base.Terrain.GetEconomyMultiplier(Realm.EconomyRace);
			if (OwnerRealm.AIPlayer != null)
			{
				switch (Game.GameCore.Settings.GetEnumeratedSetting("Difficulty"))
				{
					case 2:
						num *= 0.5f;
						break;
					case 3:
						num *= 0.25f;
						break;
					case 4:
						num *= 0.125f;
						break;
					case 5:
						num *= 0.0625f;
						break;
				}
			}
			return (int)num;
		}

		internal int GetImprovementLevel()
		{
			return m_BaseEconomy;
		}

		internal void ModifyBattleTurns(ref int Turns)
		{
			if (this.OnBattleTurnModifierRequested != null)
			{
				this.OnBattleTurnModifierRequested(this, ref Turns);
			}
		}

		internal void SetLustValue(int LustValue)
		{
			AILust = LustValue;
		}

		public bool HasScience()
		{
			if (Cradle == ArtScienceTypes.Alchemy)
			{
				return true;
			}
			if (Cradle == ArtScienceTypes.Engineering)
			{
				return true;
			}
			if (Cradle == ArtScienceTypes.Metallurgy)
			{
				return true;
			}
			if (Cradle == ArtScienceTypes.Siegecraft)
			{
				return true;
			}
			return false;
		}

		public bool HasArts()
		{
			if (Cradle == ArtScienceTypes.Medicine)
			{
				return true;
			}
			if (Cradle == ArtScienceTypes.PublicArt)
			{
				return true;
			}
			if (Cradle == ArtScienceTypes.Statecraft)
			{
				return true;
			}
			return false;
		}

		public bool HasLandmark()
		{
			if (Landmark != null)
			{
				return Landmark != "";
			}
			return false;
		}

		internal void SetRelationsMode(bool State)
		{
			if (CapitolSprite != null)
			{
				CapitolSprite.IgnoreMouse = State;
			}
			if (EconomySprite != null)
			{
				EconomySprite.IgnoreMouse = State;
			}
			if (LandmarkSprite != null)
			{
				LandmarkSprite.IgnoreMouse = State;
			}
		}

		internal void SetMoveMode(bool State)
		{
			if (CapitolSprite != null)
			{
				CapitolSprite.IgnoreMouse = State;
			}
			if (EconomySprite != null)
			{
				EconomySprite.IgnoreMouse = State;
			}
			if (ResourceSprite != null)
			{
				ResourceSprite.IgnoreMouse = State;
			}
			if (CradleSprite != null)
			{
				CradleSprite.IgnoreMouse = State;
			}
			if (FortSprite != null)
			{
				FortSprite.IgnoreMouse = State;
			}
			if (LandmarkSprite != null)
			{
				LandmarkSprite.IgnoreMouse = State;
			}
		}

		public int GetSlaverySlots()
		{
			return Math.Max(0, 10 - CurrentEconomy);
		}

		public void AddSlaveBonus(int Turns)
		{
			SlaveBonuses.Add(Turns);
			Game.GameCore.FireEvent("EconomyChanged", this);
			UpdateEconSprite();
		}

		public List<BuildingEffect> GetSupportedBuildings(BuildingEffect Building)
		{
			List<BuildingEffect> Result = new List<BuildingEffect>();
			Result.Add(Building);
			bool flag = true;
			while (flag)
			{
				flag = false;
				foreach (BuildingEffect TestBuilding in Buildings)
				{
					if (Result.Contains(TestBuilding))
					{
						continue;
					}
					if (TestBuilding.Data.Tier > 1 && Buildings.Count((BuildingEffect x) => !Result.Contains(x) && x.Data.Tier == TestBuilding.Data.Tier - 1) <= 0)
					{
						Result.Add(TestBuilding);
						flag = true;
						continue;
					}
					bool flag2 = true;
					foreach (string PreReq in TestBuilding.Data.RequiredBuildings)
					{
						IList<BuildingEffect> buildings = Buildings;
						Func<BuildingEffect, bool> predicate = (BuildingEffect x) => !Result.Contains(x) && x.BuildingName == PreReq;
						if (buildings.Count(predicate) == 0)
						{
							flag2 = false;
							break;
						}
					}
					if (!flag2)
					{
						Result.Add(TestBuilding);
						flag = true;
					}
				}
			}
			Result.Remove(Building);
			return Result;
		}

		public bool CanBuild(BuildingEffect Building)
		{
			if (ConstructionState != ConstructionStates.Free)
			{
				return false;
			}
			if (Occupied)
			{
				return false;
			}
			if (OwnerRealm.GetConstructionGold() < GetBuildingGoldCost(Building))
			{
				return false;
			}
			foreach (KeyValuePair<string, int> buildingResourceCost in GetBuildingResourceCosts(Building))
			{
				ResourceData resource = Game.GameCore.Data.Resources[buildingResourceCost.Key];
				if (OwnerRealm.GetStockpiledResource(resource) < buildingResourceCost.Value)
				{
					return false;
				}
			}
			if (Building.Data.Tier > MaxBuildableTier())
			{
				return false;
			}
			foreach (string RequiredBuilding in Building.Data.RequiredBuildings)
			{
				IList<BuildingEffect> buildings = Buildings;
				Func<BuildingEffect, bool> predicate = (BuildingEffect x) => x.BuildingName == RequiredBuilding;
				if (buildings.Count(predicate) == 0)
				{
					return false;
				}
			}
			if (Buildings.Count >= CurrentEconomy)
			{
				return false;
			}
			if (!Building.IsAvailable(this))
			{
				return false;
			}
			return true;
		}

		public Dictionary<string, int> GetBuildingResourceCosts(BuildingEffect Building)
		{
			if (HasStatus("FreeBuild"))
			{
				return new Dictionary<string, int>();
			}
			return Building.Data.ResourceCosts;
		}

		public int GetBuildingGoldCost(BuildingEffect Building)
		{
			if (HasStatus("FreeBuild"))
			{
				return 0;
			}
			return Building.Data.GoldCost;
		}

		public int MaxBuildableTier()
		{
			int result = 1;
			if (Buildings.Count > 0)
			{
				result = Buildings.Max((BuildingEffect x) => x.Data.Tier) + 1;
			}
			return result;
		}

		public int GetResourceIncome(ResourceData Resource)
		{
			int num = 0;
			if (Resource == this.Resource)
			{
				num += (int)ResourceIncome;
			}
			return num + Buildings.Sum((BuildingEffect x) => x.GetResourceIncome(Resource));
		}

		public void Resist(int Amount, bool DestroyBuilding)
		{
			ActiveResistance -= Amount;
			if (ActiveResistance < 0)
			{
				ActiveResistance = 0;
			}
			if (DestroyBuilding)
			{
				DestroyRandomBuilding();
			}
			UpdateEconSprite();
		}

		public void DestroyRandomBuilding()
		{
			if (Buildings.Count > 0)
			{
				List<BuildingEffect> list = Buildings.Where((BuildingEffect x) => GetSupportedBuildings(x).Count == 0).ToList();
				if (list.Count > 0)
				{
					BuildingEffect buildingEffect = list[RNG.Next(list.Count)];
					buildingEffect.Demolish();
				}
			}
		}

		public void EndResistance()
		{
			ResistingRealmName = "";
			UpdateEconSprite();
		}

		public void StartResistance()
		{
			if (!(ResistingRealmName == OccupierRealm.Name))
			{
				ResistingRealmName = OccupierRealm.Name;
				ActiveResistance = Resistance.GetValue();
				if (OccupierRealm.Alignment != OwnerRealm.Alignment)
				{
					ActiveResistance++;
				}
				if (OccupierRealm.Race != OwnerRealm.Race)
				{
					ActiveResistance++;
				}
				if (OccupierRealm.CodeOfWar != OwnerRealm.CodeOfWar)
				{
					ActiveResistance++;
				}
				UpdateEconSprite();
			}
		}

		public void CheckBuildings()
		{
			bool flag = true;
			while (flag)
			{
				flag = false;
				foreach (BuildingEffect building in Buildings)
				{
					if (Game.GameCore.Data.BuildingAffinities[building.BuildingName].GetAffinity(OwnerRealm.Name) > 0)
					{
						continue;
					}
					List<BuildingEffect> supportedBuildings = GetSupportedBuildings(building);
					building.Demolish();
					foreach (BuildingEffect item in supportedBuildings)
					{
						item.Demolish();
					}
					flag = true;
					break;
				}
			}
		}

		internal void RemoveFort()
		{
			Buildings.FirstOrDefault((BuildingEffect x) => x.BuildingName == "Fort")?.Demolish();
		}

		internal void ReduceInciteCount()
		{
			InciteCount--;
			if (InciteCount < 0)
			{
				InciteCount = 0;
			}
			UpdateInciteIcon();
		}

		internal void IncreaseInciteCount()
		{
			InciteCount++;
			UpdateInciteIcon();
		}

		private void UpdateInciteIcon()
		{
			if (InciteSprite != null)
			{
				InciteSprite.Dispose();
				InciteSprite = null;
			}
			if (InciteCount > 0)
			{
				InciteSprite = Game.GameCore.Utilities.SpriteManager.CreateSprite("Data\\Images\\HUD\\Info\\button_provocateur_mouseover.png", Interactive: true);
				InciteSprite.SetSize(32f, 32f);
				InciteSprite.SetPosition(CapitolCoords.X + 32, CapitolCoords.Y);
				InciteSprite.Tooltip = GameText.CreateLocalised("TT_INCITE");
				InciteSprite.IgnoreMouseClicks = true;
			}
		}

		internal void IncreaseInciteAmount()
		{
			InciteEffect += 2f;
		}
	}
}