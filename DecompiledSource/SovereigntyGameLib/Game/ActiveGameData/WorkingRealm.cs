using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WorkingRealm
	{
		public event SpellRechargeDelegate OnRechargeModRequested;

		public event RealmstatusDelegate OnStatusRequested;

		public event SpellDataDelegate OnSpellLearned;

		public event StatModifierDelegate OnUnitXPBonusRequested;

		public IList<WorkingProvince> Provinces
		{
			get
			{
				if (this.m_Provinces == null)
				{
					this.m_Provinces = this.Game.AllProvinces.Values.Where((WorkingProvince x) => x.OwnerID == this.ID).ToList<WorkingProvince>();
				}
				return this.m_Provinces.AsReadOnly();
			}
		}

		public IList<WorkingProvince> OccupiedProvinces
		{
			get
			{
				return this.Game.AllProvinces.Values.Where((WorkingProvince x) => x.OwnerID != this.ID && x.OccupierRealm == this).ToList<WorkingProvince>().AsReadOnly();
			}
		}

		public IList<WorkingStack> Stacks
		{
			get
			{
				if (this.m_Stacks == null)
				{
					this.m_Stacks = this.Game.AllStacks.Values.Where((WorkingStack x) => x.OwnerID == this.ID).ToList<WorkingStack>();
				}
				return this.m_Stacks.AsReadOnly();
			}
		}

		public IList<WorkingUnit> Units
		{
			get
			{
				if (this.m_UnitList == null)
				{
					this.m_UnitList = this.Game.AllUnits.Values.Where((WorkingUnit x) => x.OwnerRealmID == this.ID).ToList<WorkingUnit>();
				}
				return this.m_UnitList.AsReadOnly();
			}
		}

		public IList<WorkingRealm> Enemies
		{
			get
			{
				return this.Game.AllRealms.Values.Where((WorkingRealm x) => this.DiplomacyManager.GetRelation(x) == RelationStates.War).ToList<WorkingRealm>().AsReadOnly();
			}
		}

		public IList<WorkingRealm> Allies
		{
			get
			{
				return this.Game.AllRealms.Values.Where((WorkingRealm x) => this.DiplomacyManager.GetRelation(x) == RelationStates.Alliance).ToList<WorkingRealm>().AsReadOnly();
			}
		}

		public IList<WorkingRealm> PeaceRealms
		{
			get
			{
				return this.Game.AllRealms.Values.Where((WorkingRealm x) => this.DiplomacyManager.GetRelation(x) == RelationStates.Peace || this.DiplomacyManager.GetRelation(x) == RelationStates.ForcedPeace).ToList<WorkingRealm>().AsReadOnly();
			}
		}

		public IList<WorkingRealm> DefenceRealms
		{
			get
			{
				return this.Game.AllRealms.Values.Where((WorkingRealm x) => this.DiplomacyManager.GetRelation(x) == RelationStates.Defence).ToList<WorkingRealm>().AsReadOnly();
			}
		}

		public IList<WorkingAgent> Agents
		{
			get
			{
				return this.Game.AllAgents.Values.Where((WorkingAgent x) => x.OwnerID == this.ID).ToList<WorkingAgent>().AsReadOnly();
			}
		}

		public IList<int> StoredHeroIDs
		{
			get
			{
				return this.StoredHeroList.AsReadOnly();
			}
		}

		public IList<WorkingHero> Heroes
		{
			get
			{
				return this.Game.AllHeroes.Values.Where((WorkingHero x) => x.OwnerRealmID == this.ID).ToList<WorkingHero>().AsReadOnly();
			}
		}

		public AIPlayer AIPlayer
		{
			get
			{
				return this.Game.AllAIPlayers.Values.FirstOrDefault((AIPlayer x) => x.Realm.ID == this.ID);
			}
		}

		public bool RealmIsDead
		{
			get
			{
				return this.m_Dead;
			}
		}

		public WorkingProvince CapitolProvince
		{
			get
			{
				return this.Game.AllProvinces.Values.FirstOrDefault((WorkingProvince x) => x.OwnerID == this.ID && x.IsCapitol);
			}
		}

		public bool HasHarbour
		{
			get
			{
				return this.Game.AllProvinces.Values.Count((WorkingProvince x) => x.OwnerID == this.ID && x.HasHarbour) > 0;
			}
		}

		public WorkingRealm(SovereigntyGame Game, int ID, RealmData BaseRealm)
		{
			this.ID = ID;
			this.Game = Game;
			this.Name = BaseRealm.Name;
			this.DisplayName = BaseRealm.DisplayName;
			this.CounterFilename = BaseRealm.CounterFilename;
			this.FlagFilename = BaseRealm.FlagFilename;
			this.CodeOfWar = BaseRealm.CodeOfWar;
			this.Alignment = BaseRealm.Alignment;
			this.Race = BaseRealm.Race;
			this.RealmType = BaseRealm.RealmType;
			this.IsPirate = false;
			this.EconomyRace = BaseRealm.EconomyRace;
			this.ScienceValue = BaseRealm.ScienceValue;
			this.Science_SiegecraftValue = BaseRealm.Science_SiegecraftValue;
			this.Science_MetallurgyValue = BaseRealm.Science_MetallurgyValue;
			this.Science_EngineeringValue = BaseRealm.Science_EngineeringValue;
			this.Science_AlchemyValue = BaseRealm.Science_AlchemyValue;
			this.ArtsValue = BaseRealm.ArtsValue;
			this.Arts_StatecraftValue = BaseRealm.Arts_StatecraftValue;
			this.Arts_PublicValue = BaseRealm.Arts_PublicValue;
			this.Arts_MedicineValue = BaseRealm.Arts_MedicineValue;
			this.Panel = BaseRealm.Panel;
			this.AllyValue = BaseRealm.AllyValue;
			this.AllowUndead = BaseRealm.AllowUndead;
			this.Gold = new ActiveStat<int>(BaseRealm.StartingGold);
			this.Gold.OnStatChanged += this.Gold_OnStatChanged;
			this.MinimapColour = BaseRealm.MinimapColour;
			this.Strength = BaseRealm.Strength;
			this.HeroTypeID = BaseRealm.HeroTypeID;
			this.AgentCount = BaseRealm.SpyCount;
			this.MusicRealm = BaseRealm.MusicRealm;
			this.MagicValue = BaseRealm.MagicValue;
			this.HeroClasses = new List<HeroClassData>();
			foreach (string text in BaseRealm.HeroClasses)
			{
				HeroClassData heroClassData = null;
				Game.Data.HeroClasses.TryGetValue(text, out heroClassData);
				if (heroClassData != null)
				{
					this.HeroClasses.Add(heroClassData);
				}
			}
			this.OwnedProvinces = new List<int>();
			this.OwnedStacks = new List<int>();
			this.StoredHeroList = new List<int>();
			this.QueuedUnits = new List<UnitQueueItem>();
			this.SpecialIncomes = new List<int>();
			this.MagicData = new RealmMagic(Game, this);
			this.Restrictions = new RealmRestrictions(Game, ID);
			this.DiplomacyManager = new RealmDiplomacyManager(this.Name, Game);
			this.WarTracker = new RealmWarTracker();
			this.UnitPurchaseManager = new UnitPurchaseManager(Game, ID);
			this.Prison = new PrisonData(Game, ID);
			this.Resources = new Dictionary<ResourceData, int>();
			this.SpellEffects = new SpellTargetData(Game);
			this.TradeManager = new RealmTradeManager(Game, ID);
			this.ProvinceIncomeMultiplier = new RealmStat(Game, ID, 100);
			this.AgentSpeedModifier = new RealmStat(Game, ID, 0);
			this.AgentEffectModifier = new RealmStat(Game, ID, 100);
			this.InterestRate = new FloatRealmStat(Game, ID, 0);
			this.EliteUnitLimit = new RealmStat(Game, ID, 4);
			this.HeroLimit = new RealmStat(Game, ID, BaseRealm.HeroValue);
			this.HeroDisciplineModifier = new RealmStat(Game, ID, 0);
			this.Traits = Game.Data.AITraits[this.Name].CreateDictionary();
			for (int i = 0; i < BaseRealm.SpyCount; i++)
			{
				Game.CreateAgent(this.ID);
			}
		}

		public void ProvincesChanged()
		{
			this.m_Provinces = null;
		}

		public void StacksChanged()
		{
			this.m_Stacks = null;
		}

		public void UnitsChanged()
		{
			this.m_UnitList = null;
		}

		private void Gold_OnStatChanged()
		{
			if (this.Gold.Value < 0)
			{
				throw new Exception("Negative gold");
			}
		}

		public WorkingRealm(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.ID = r.ReadInt32();
			this.Name = r.ReadString();
			this.DisplayName = r.ReadString();
			this.CounterFilename = r.ReadString();
			this.FlagFilename = r.ReadString();
			this.CodeOfWar = r.ReadBoolean();
			this.Alignment = (RealmAlignments)r.ReadInt16();
			this.Race = (Races)r.ReadInt16();
			this.RealmType = (NavalType)r.ReadInt16();
			this.IsPirate = r.ReadBoolean();
			this.EconomyRace = (Races)r.ReadInt16();
			this.ScienceValue = r.ReadInt32();
			this.Science_SiegecraftValue = r.ReadInt32();
			this.Science_MetallurgyValue = r.ReadInt32();
			this.Science_EngineeringValue = r.ReadInt32();
			this.Science_AlchemyValue = r.ReadInt32();
			this.ArtsValue = r.ReadInt32();
			this.Arts_StatecraftValue = r.ReadInt32();
			this.Arts_PublicValue = r.ReadInt32();
			this.Arts_MedicineValue = r.ReadInt32();
			this.Panel = (SpellSchools)r.ReadInt16();
			if (SaveVersion < 42)
			{
				r.ReadInt32();
				r.ReadInt32();
			}
			if (SaveVersion < GlobalData.SAVEVERSION_EA3)
			{
				r.ReadInt32();
			}
			this.AllyValue = r.ReadInt32();
			if (SaveVersion < 42)
			{
				r.ReadInt32();
				r.ReadInt16();
				r.ReadString();
			}
			this.MinimapColour = Color.FromArgb((int)r.ReadByte(), (int)r.ReadByte(), (int)r.ReadByte());
			int num = r.ReadInt32();
			this.HeroClasses = new List<HeroClassData>();
			for (int i = 0; i < num; i++)
			{
				HeroClassData heroClassData = null;
				Game.Data.HeroClasses.TryGetValue(r.ReadString(), out heroClassData);
				if (heroClassData != null)
				{
					this.HeroClasses.Add(heroClassData);
				}
			}
			this.Strength = r.ReadInt32();
			this.HeroTypeID = r.ReadInt32();
			this.AgentCount = r.ReadInt32();
			num = r.ReadInt32();
			this.Traits = new Dictionary<AITraits, int>();
			for (int j = 0; j < num; j++)
			{
				this.Traits.Add((AITraits)r.ReadInt16(), r.ReadInt32());
			}
			if (SaveVersion < 42)
			{
				r.ReadInt32();
			}
			this.MusicRealm = r.ReadString();
			this.m_Dead = r.ReadBoolean();
			num = r.ReadInt32();
			this.OwnedProvinces = new List<int>();
			for (int k = 0; k < num; k++)
			{
				this.OwnedProvinces.Add(r.ReadInt32());
			}
			num = r.ReadInt32();
			this.OwnedStacks = new List<int>();
			for (int l = 0; l < num; l++)
			{
				this.OwnedStacks.Add(r.ReadInt32());
			}
			num = r.ReadInt32();
			this.StoredHeroList = new List<int>();
			for (int m = 0; m < num; m++)
			{
				this.StoredHeroList.Add(r.ReadInt32());
			}
			num = r.ReadInt32();
			this.QueuedUnits = new List<UnitQueueItem>();
			for (int n = 0; n < num; n++)
			{
				UnitQueueItem unitQueueItem = new UnitQueueItem(Game);
				unitQueueItem.LoadState(r, SaveVersion);
				this.QueuedUnits.Add(unitQueueItem);
			}
			num = r.ReadInt32();
			this.Resources = new Dictionary<ResourceData, int>();
			for (int num2 = 0; num2 < num; num2++)
			{
				ResourceData resourceData = null;
				Game.Data.Resources.TryGetValue(r.ReadString(), out resourceData);
				int num3 = r.ReadInt32();
				if (resourceData != null)
				{
					this.Resources.Add(resourceData, num3);
				}
			}
			this.MagicData = new RealmMagic(Game, this);
			this.MagicData.Load(r, SaveVersion);
			this.Restrictions = new RealmRestrictions(Game, this.ID);
			this.Restrictions.Load(r, SaveVersion);
			this.DiplomacyManager = new RealmDiplomacyManager(this.Name, Game);
			this.DiplomacyManager.Load(r, SaveVersion);
			this.WarTracker = new RealmWarTracker();
			this.WarTracker.Load(r, SaveVersion);
			this.PowerGroup = (PowerGroup)r.ReadInt16();
			this.Prison = new PrisonData(Game, this.ID);
			this.Prison.Load(r, SaveVersion);
			this.SpellEffects = new SpellTargetData(Game);
			this.TradeManager = new RealmTradeManager(Game, this.ID);
			this.TradeManager.Load(r, SaveVersion);
			this.Gold = new ActiveStat<int>(0);
			this.Gold.OnStatChanged += this.Gold_OnStatChanged;
			this.Gold.Value = r.ReadInt32();
			this.UnitPurchaseManager = new UnitPurchaseManager(Game, this.ID);
			this.SpecialIncomes = new List<int>();
			num = r.ReadInt32();
			for (int num4 = 0; num4 < num; num4++)
			{
				this.SpecialIncomes.Add(r.ReadInt32());
			}
			this.Lucky = r.ReadBoolean();
			this.LuckTurns = r.ReadInt32();
			this.ProvinceIncomeMultiplier = new RealmStat(Game, this.ID, 100);
			this.AgentSpeedModifier = new RealmStat(Game, this.ID, 0);
			this.AgentEffectModifier = new RealmStat(Game, this.ID, 100);
			this.InterestRate = new FloatRealmStat(Game, this.ID, 0);
			this.HeroLimit = new RealmStat(Game, this.ID, 0);
			this.EliteUnitLimit = new RealmStat(Game, this.ID, 4);
			this.HeroDisciplineModifier = new RealmStat(Game, this.ID, 0);
			if (SaveVersion >= GlobalData.SAVEVERSION_EA3)
			{
				this.ProvinceIncomeMultiplier.Load(r, SaveVersion);
				this.AgentSpeedModifier.Load(r, SaveVersion);
				this.AgentEffectModifier.Load(r, SaveVersion);
				this.InterestRate.Load(r, SaveVersion);
				this.HeroLimit.Load(r, SaveVersion);
			}
			if (SaveVersion >= 50)
			{
				this.EliteUnitLimit.Load(r, SaveVersion);
				this.HeroDisciplineModifier.Load(r, SaveVersion);
			}
			if (SaveVersion >= 57)
			{
				this.AllowUndead = r.ReadBoolean();
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ID);
			w.Write(this.Name);
			w.Write(this.DisplayName);
			w.Write(this.CounterFilename);
			w.Write(this.FlagFilename);
			w.Write(this.CodeOfWar);
			w.Write((short)this.Alignment);
			w.Write((short)this.Race);
			w.Write((short)this.RealmType);
			w.Write(this.IsPirate);
			w.Write((short)this.EconomyRace);
			w.Write(this.ScienceValue);
			w.Write(this.Science_SiegecraftValue);
			w.Write(this.Science_MetallurgyValue);
			w.Write(this.Science_EngineeringValue);
			w.Write(this.Science_AlchemyValue);
			w.Write(this.ArtsValue);
			w.Write(this.Arts_StatecraftValue);
			w.Write(this.Arts_PublicValue);
			w.Write(this.Arts_MedicineValue);
			w.Write((short)this.Panel);
			w.Write(this.AllyValue);
			w.Write(this.MinimapColour.R);
			w.Write(this.MinimapColour.G);
			w.Write(this.MinimapColour.B);
			w.Write(this.HeroClasses.Count);
			foreach (HeroClassData heroClassData in this.HeroClasses)
			{
				w.Write(heroClassData.ClassName);
			}
			w.Write(this.Strength);
			w.Write(this.HeroTypeID);
			w.Write(this.AgentCount);
			w.Write(this.Traits.Count);
			foreach (KeyValuePair<AITraits, int> keyValuePair in this.Traits)
			{
				w.Write((short)keyValuePair.Key);
				w.Write(keyValuePair.Value);
			}
			w.Write(this.MusicRealm);
			w.Write(this.m_Dead);
			w.Write(this.OwnedProvinces.Count);
			foreach (int num in this.OwnedProvinces)
			{
				w.Write(num);
			}
			w.Write(this.OwnedStacks.Count);
			foreach (int num2 in this.OwnedStacks)
			{
				w.Write(num2);
			}
			w.Write(this.StoredHeroList.Count);
			foreach (int num3 in this.StoredHeroList)
			{
				w.Write(num3);
			}
			w.Write(this.QueuedUnits.Count);
			foreach (UnitQueueItem unitQueueItem in this.QueuedUnits)
			{
				unitQueueItem.SaveCurrentState(w);
			}
			w.Write(this.Resources.Count);
			foreach (KeyValuePair<ResourceData, int> keyValuePair2 in this.Resources)
			{
				w.Write(keyValuePair2.Key.ResourceName);
				w.Write(keyValuePair2.Value);
			}
			this.MagicData.Save(w);
			this.Restrictions.Save(w);
			this.DiplomacyManager.Save(w);
			this.WarTracker.Save(w);
			w.Write((short)this.PowerGroup);
			this.Prison.Save(w);
			this.TradeManager.Save(w);
			w.Write(this.Gold.Value);
			w.Write(this.SpecialIncomes.Count);
			foreach (int num4 in this.SpecialIncomes)
			{
				w.Write(num4);
			}
			w.Write(this.Lucky);
			w.Write(this.LuckTurns);
			this.ProvinceIncomeMultiplier.Save(w);
			this.AgentSpeedModifier.Save(w);
			this.AgentEffectModifier.Save(w);
			this.InterestRate.Save(w);
			this.HeroLimit.Save(w);
			this.EliteUnitLimit.Save(w);
			this.HeroDisciplineModifier.Save(w);
			w.Write(this.AllowUndead);
		}

		public int GetRechargeTime(SpellEffect Spell)
		{
			int level = Spell.SpellData.Level;
			if (this.OnRechargeModRequested != null)
			{
				this.OnRechargeModRequested(Spell, ref level);
			}
			return Math.Max(level, 1);
		}

		public void Dispose()
		{
			if (this.AIPlayer != null)
			{
				this.AIPlayer.Dispose();
			}
			this.Restrictions.Dispose();
			this.DiplomacyManager.Dispose();
			this.WarTracker.Dispose();
			this.OnRechargeModRequested = null;
			this.OnSpellLearned = null;
			this.OnStatusRequested = null;
			this.OnUnitXPBonusRequested = null;
			this.Gold.Dispose();
		}

		public void StartBattle(TacticalBattleController Battle)
		{
			this.BattleData = new RealmBattleData(this.Game, Battle, this.ID);
		}

		public void EndBattle()
		{
			if (this.BattleData != null)
			{
				this.BattleData.Dispose();
				this.BattleData = null;
			}
		}

		internal void DisbandAuxiliaries(WorkingRealm Realm, SovereigntyGame Game)
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (WorkingUnit workingUnit in this.Units)
			{
				if (workingUnit.IsAuxiliary(Realm))
				{
					this.DisbandUnit(workingUnit);
					list.Add(workingUnit);
				}
			}
			if (list.Count > 0)
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.CaptionText = GameText.CreateLocalised("MSG_AUX_LOST_TITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("MSG_AUXILIARY_DISBAND", new object[] { list.Count });
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(list[0].DisplayName, new object[0]));
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.MsgType = MessageType.GenericInfo;
				Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
		}

		private void DisbandUnit(WorkingUnit Unit)
		{
			Unit.OwnerStack.RemoveUnit(Unit);
		}

		public void UpdateUnitQueue()
		{
			foreach (UnitQueueItem unitQueueItem in this.QueuedUnits)
			{
				if (unitQueueItem.TurnsLeft > 0)
				{
					unitQueueItem.TurnsLeft--;
				}
			}
		}

		public List<WorkingUnit> GetAuxiliaryUnits(WorkingRealm Target)
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (WorkingUnit workingUnit in this.Units)
			{
				if (workingUnit.IsAuxiliary(Target))
				{
					list.Add(workingUnit);
				}
			}
			foreach (UnitQueueItem unitQueueItem in this.QueuedUnits)
			{
				if (unitQueueItem.Unit.IsAuxiliary(Target))
				{
					list.Add(unitQueueItem.Unit);
				}
			}
			return list;
		}

		public bool HasLandmark(string Landmark)
		{
			return this.Provinces.Count((WorkingProvince x) => x.Landmark == Landmark) > 0;
		}

		public void QueueUnitWithoutCharge(WorkingUnit NewUnit)
		{
			UnitQueueItem unitQueueItem = new UnitQueueItem(this.Game);
			unitQueueItem.UnitID = NewUnit.ID;
			unitQueueItem.UnitCost = this.UnitPurchaseManager.GetUnitCost(NewUnit);
			unitQueueItem.TotalTurns = this.UnitPurchaseManager.GetUnitTime(NewUnit.BaseType);
			unitQueueItem.TurnsLeft = unitQueueItem.TotalTurns;
		}

		public void QueueUnit(WorkingUnit NewUnit, bool Instant = false, bool IgnoreMissingResources = false)
		{
			UnitQueueItem unitQueueItem = new UnitQueueItem(this.Game);
			unitQueueItem.UnitID = NewUnit.ID;
			if (Instant)
			{
				unitQueueItem.UnitCost = 0;
				unitQueueItem.TotalTurns = 0;
				unitQueueItem.TurnsLeft = 0;
			}
			else
			{
				List<Tuple<ResourceData, int>> unitResourceCost = this.UnitPurchaseManager.GetUnitResourceCost(NewUnit);
				foreach (Tuple<ResourceData, int> tuple in unitResourceCost)
				{
					this.RemoveResource(tuple.Item1, tuple.Item2, IgnoreMissingResources);
				}
				unitQueueItem.UnitCost = this.UnitPurchaseManager.GetUnitCost(NewUnit);
				unitQueueItem.TotalTurns = this.UnitPurchaseManager.GetUnitTime(NewUnit.BaseType);
				unitQueueItem.TurnsLeft = unitQueueItem.TotalTurns;
				this.SpendUnitsGold(unitQueueItem.UnitCost);
			}
			this.QueuedUnits.Add(unitQueueItem);
			this.Game.GameCore.FireEvent("TrainingQueueChanged", new object[] { this });
		}

		public void SpendUnitsGold(int Value)
		{
			if (this.AIPlayer == null)
			{
				this.Gold.Value -= Value;
				return;
			}
			this.AIPlayer.UnitsManager.Funds.CurrentGold -= Value;
		}

		public IList<UnitQueueItem> GetCurrentUnitQueue()
		{
			this.QueuedUnits.RemoveAll((UnitQueueItem x) => x.Unit == null);
			return this.QueuedUnits.AsReadOnly();
		}

		public void CancelUnitTraining(UnitQueueItem Item)
		{
			this.QueuedUnits.Remove(Item);
			if (Item.TurnsLeft == Item.TotalTurns)
			{
				UnitData baseType = Item.Unit.BaseType;
				if (baseType != null)
				{
					foreach (KeyValuePair<string, int> keyValuePair in baseType.GetRequiredResources())
					{
						this.GrantResource(this.Game.Data.Resources[keyValuePair.Key], keyValuePair.Value);
					}
				}
			}
			this.SpendUnitsGold(-Item.GetRefundValue());
			this.Game.DestroyUnit(Item.Unit);
			this.Game.GameCore.FireEvent("TrainingQueueChanged", new object[] { this });
		}

		public Dictionary<ResourceData, int> GetAllResources()
		{
			return new Dictionary<ResourceData, int>(this.Resources);
		}

		public int GetStockpiledResource(ResourceData Resource)
		{
			if (!this.Resources.ContainsKey(Resource))
			{
				return 0;
			}
			return this.Resources[Resource];
		}

		public int GetUnitTypeCount(UnitData Data)
		{
			return this.Units.Count((WorkingUnit x) => x.BaseName == Data.ToString());
		}

		public void GrantResource(ResourceData Resource, int Quantity)
		{
			if (Quantity <= 0)
			{
				return;
			}
			if (!this.Resources.ContainsKey(Resource))
			{
				this.Resources.Add(Resource, Quantity);
				return;
			}
			Dictionary<ResourceData, int> resources;
			(resources = this.Resources)[Resource] = resources[Resource] + Quantity;
		}

		public void RemoveResource(ResourceData Resource, int Quantity, bool IgnoreMissing = false)
		{
			if (Quantity <= 0)
			{
				return;
			}
			if (!this.Resources.ContainsKey(Resource))
			{
				return;
			}
			Dictionary<ResourceData, int> resources;
			(resources = this.Resources)[Resource] = resources[Resource] - Quantity;
			if (this.Resources[Resource] >= 0)
			{
				return;
			}
			if (IgnoreMissing)
			{
				this.Resources[Resource] = 0;
				return;
			}
			throw new Exception("Resource " + Resource.ResourceName + " reduced to less than 0 in stock");
		}

		internal void EndUnitTraining(UnitQueueItem Item)
		{
			this.QueuedUnits.Remove(Item);
			this.Game.GameCore.FireEvent("TrainingQueueChanged", new object[] { this });
		}

		internal void ResetUnitMoves()
		{
			foreach (WorkingUnit workingUnit in this.Units)
			{
				workingUnit.ResetCampaignMoves();
				if (!workingUnit.HasBattled)
				{
					workingUnit.ApplyHealing(workingUnit.HealRate * 10, false, null);
				}
				workingUnit.HasBattled = false;
			}
			foreach (WorkingHero workingHero in this.Heroes)
			{
				workingHero.ResetCampaignMoves();
			}
			foreach (WorkingStack workingStack in this.Stacks)
			{
				workingStack.ForceScout = false;
			}
		}

		public List<ResourceData> GetResourcesInRealm()
		{
			List<ResourceData> list = new List<ResourceData>();
			foreach (ResourceData resourceData in this.Game.Data.Resources.Values)
			{
				int resourceIncome = this.GetResourceIncome(resourceData, false);
				if (resourceIncome > 0)
				{
					list.Add(resourceData);
				}
			}
			return list;
		}

		public void DoResourceIncome()
		{
			foreach (ResourceData resourceData in this.Game.Data.Resources.Values)
			{
				int resourceIncome = this.GetResourceIncome(resourceData, false);
				if (resourceIncome > 0)
				{
					this.GrantResource(resourceData, resourceIncome);
				}
			}
		}

		public int GetResourceExpenses(ResourceData Resource)
		{
			int num = 0;
			foreach (OngoingTrade ongoingTrade in from x in this.Game.GetOngoingTrades(this)
				where x.Realm == this
				select x)
			{
				for (int i = 0; i < ongoingTrade.Resources.Count; i++)
				{
					if (ongoingTrade.Resources[i] == Resource)
					{
						num += ongoingTrade.ResourceQuantities[i];
					}
				}
			}
			return num;
		}

		public int GetResourceIncome(ResourceData Resource, bool IncludeTrade = false)
		{
			int num = this.Provinces.Sum((WorkingProvince x) => x.GetResourceIncome(Resource));
			if (IncludeTrade)
			{
				foreach (OngoingTrade ongoingTrade in from x in this.Game.GetOngoingTrades(this)
					where x.TargetRealm == this
					select x)
				{
					for (int i = 0; i < ongoingTrade.Resources.Count; i++)
					{
						if (ongoingTrade.Resources[i] == Resource)
						{
							num += ongoingTrade.ResourceQuantities[i];
						}
					}
				}
			}
			return num;
		}

		internal void UpdateAgents()
		{
			foreach (WorkingAgent workingAgent in this.Agents)
			{
				workingAgent.Update();
			}
			if (this.LuckTurns > 0)
			{
				this.LuckTurns--;
				if (this.LuckTurns == 0)
				{
					this.RemoveLuck();
				}
			}
		}

		public bool HeroSlotAvailable()
		{
			return this.Stacks.Count((WorkingStack x) => x.Hero == null) > 0;
		}

		internal WorkingHero GetFirstStoredHero()
		{
			if (this.StoredHeroList.Count == 0)
			{
				return null;
			}
			int num = this.StoredHeroList[0];
			this.StoredHeroList.RemoveAt(0);
			return this.Game.AllHeroes[num];
		}

		public int GetLandmarkCount()
		{
			return this.Provinces.Count((WorkingProvince x) => x.Landmark != null && x.Landmark != "");
		}

		internal void UpdatePlague()
		{
			foreach (WorkingProvince workingProvince in this.Provinces)
			{
				workingProvince.UpdatePlague();
			}
		}

		public bool HasStatus(string StatusName, params object[] Args)
		{
			bool flag = false;
			if (this.OnStatusRequested != null)
			{
				this.OnStatusRequested(StatusName, ref flag, this, Args);
			}
			return flag;
		}

		internal void HandleSpellLearned(RealmMagicData Spell)
		{
			if (this.OnSpellLearned != null)
			{
				this.OnSpellLearned(Spell);
			}
		}

		internal void KillRealm()
		{
			this.m_Dead = true;
		}

		internal Dictionary<string, int> GetResources()
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (KeyValuePair<ResourceData, int> keyValuePair in this.Resources)
			{
				dictionary.Add(keyValuePair.Key.ResourceName, keyValuePair.Value);
			}
			return dictionary;
		}

		public void RemoveLuck()
		{
			this.Lucky = false;
			foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
			{
				workingRealm.DiplomacyManager.AdjustBaseValue(this, -5f);
			}
			this.Game.GameCore.FireEvent("LuckEnded", new object[] { this });
		}

		public void GrantLuck(int TurnCount, bool Notify = true)
		{
			this.Lucky = true;
			this.LuckTurns = TurnCount;
			foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
			{
				workingRealm.DiplomacyManager.AdjustBaseValue(this, 5f);
			}
			if (Notify)
			{
				this.Game.GameCore.FireEvent("LuckStarted", new object[] { this });
			}
		}

		internal void CheckCradleEffects()
		{
			foreach (UnitQueueItem unitQueueItem in this.QueuedUnits)
			{
				WorkingUnit unit = unitQueueItem.Unit;
				if (this.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Metallurgy) > 0 && (unit.Race == Races.Human || unit.Race == Races.Elf || unit.Race == Races.Dwarf || unit.Race == Races.Orc))
				{
					unit.DefaultDamageType = DamageTypes.War;
				}
				if (this.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Siegecraft) > 0 && unit.Class == UnitClasses.Siege)
				{
					unit.Range.BaseValue++;
				}
				if (this.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Engineering) > 0 && unit.Class == UnitClasses.Infantry && unit.HasAnyNamedFlags(new string[] { "Besiege", "Saboteur", "Rebels" }) && !unit.HasAnyNamedFlag("Bridging"))
				{
					unit.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Bridging"));
				}
				if (this.Provinces.Count((WorkingProvince x) => x.Cradle == ArtScienceTypes.Medicine) > 0 && unit.HasAnyNamedFlags(new string[] { "Healer", "MassHeal" }))
				{
					unit.HealRate.BaseValue++;
				}
			}
		}

		public WorkingAgent GetTradeAgent()
		{
			if (this.AIPlayer == null)
			{
				return this.GetFreeAgent();
			}
			return this.AIPlayer.TradeManager.GetFreeAgent();
		}

		public WorkingAgent GetCovertAgent()
		{
			if (this.AIPlayer == null)
			{
				return this.GetFreeAgent();
			}
			return this.AIPlayer.EspionageManager.GetFreeAgent();
		}

		private WorkingAgent GetFreeAgent()
		{
			return this.Agents.FirstOrDefault((WorkingAgent x) => x.CurrentMode == AgentModes.Idle && x.HostRealm == x.OwnerRealm && x.TurnsLeft == 0);
		}

		public bool IsDefensiveInvasion(WorkingRealm Realm)
		{
			return this.WarTracker.IsDefensiveInvasion(Realm);
		}

		public void StoreHero(WorkingHero Hero)
		{
			this.StoredHeroList.Add(Hero.ID);
		}

		public int GetPrisonGold()
		{
			if (this.AIPlayer == null)
			{
				return this.Gold;
			}
			return this.AIPlayer.PrisonManager.Funds.CurrentGold;
		}

		public int GetMagicGold()
		{
			if (this.AIPlayer == null)
			{
				return this.Gold;
			}
			return this.AIPlayer.MagicManager.Funds.CurrentGold;
		}

		public int GetTradeGold()
		{
			if (this.AIPlayer == null)
			{
				return this.Gold;
			}
			return this.AIPlayer.TradeManager.Funds.CurrentGold;
		}

		public void SpendTradeGold(int Value)
		{
			if (this.AIPlayer == null)
			{
				this.Gold.Value -= Value;
				return;
			}
			this.AIPlayer.TradeManager.Funds.CurrentGold -= Value;
		}

		public int GetUnitGold()
		{
			if (this.AIPlayer == null)
			{
				return this.Gold;
			}
			return this.AIPlayer.UnitsManager.Funds.CurrentGold;
		}

		public int GetConstructionGold()
		{
			if (this.AIPlayer == null)
			{
				return this.Gold;
			}
			return this.AIPlayer.ConstructionManager.Funds.CurrentGold;
		}

		public int GetRelationsGold()
		{
			if (this.AIPlayer == null)
			{
				return this.Gold;
			}
			return this.AIPlayer.RelationsManager.Funds.CurrentGold;
		}

		public void SpendPrisonGold(int Cost)
		{
			if (this.AIPlayer == null)
			{
				this.Gold.Value -= Cost;
				return;
			}
			this.AIPlayer.PrisonManager.Funds.CurrentGold -= Cost;
		}

		public float GetTotalGold()
		{
			if (this.AIPlayer == null)
			{
				return (float)this.Gold;
			}
			return (float)this.AIPlayer.BudgetManager.GetTotalGold();
		}

		public int GetMarketGold()
		{
			if (this.AIPlayer == null)
			{
				return this.Gold;
			}
			return this.AIPlayer.ResourcesManager.Funds.CurrentGold;
		}

		public void SpendMarketGold(int TotalPrice)
		{
			if (this.AIPlayer == null)
			{
				this.Gold.Value -= TotalPrice;
				return;
			}
			this.AIPlayer.ResourcesManager.Funds.CurrentGold -= TotalPrice;
		}

		public int ID;

		public int AIPlayerID;

		public string Name;

		public string DisplayName;

		public string CounterFilename;

		public string FlagFilename;

		public bool CodeOfWar;

		public RealmAlignments Alignment;

		public Races Race;

		public NavalType RealmType;

		public bool IsPirate;

		public bool AllowUndead;

		public Races EconomyRace;

		public int ScienceValue;

		public int Science_SiegecraftValue;

		public int Science_EngineeringValue;

		public int Science_MetallurgyValue;

		public int Science_AlchemyValue;

		public int ArtsValue;

		public int Arts_PublicValue;

		public int Arts_MedicineValue;

		public int Arts_StatecraftValue;

		public SpellSchools Panel;

		public int MagicValue;

		public int AllyValue;

		public int AggressionValue;

		public Color MinimapColour;

		public List<int> SpecialIncomes;

		public List<HeroClassData> HeroClasses;

		public int Strength;

		public int HeroTypeID;

		public int AgentCount;

		public Dictionary<AITraits, int> Traits;

		public string MusicRealm;

		private bool m_Dead;

		private List<int> OwnedProvinces;

		private List<int> OwnedStacks;

		private List<int> StoredHeroList;

		private List<UnitQueueItem> QueuedUnits;

		private Dictionary<ResourceData, int> Resources;

		public RealmMagic MagicData;

		public RealmRestrictions Restrictions;

		public RealmDiplomacyManager DiplomacyManager;

		public RealmWarTracker WarTracker;

		public PowerGroup PowerGroup;

		public UnitPurchaseManager UnitPurchaseManager;

		public PrisonData Prison;

		public RealmBattleData BattleData;

		public SpellTargetData SpellEffects;

		public RealmTradeManager TradeManager;

		public RealmStat ProvinceIncomeMultiplier;

		public RealmStat AgentSpeedModifier;

		public RealmStat AgentEffectModifier;

		public RealmStat EliteUnitLimit;

		public FloatRealmStat InterestRate;

		public RealmStat HeroLimit;

		public RealmStat HeroDisciplineModifier;

		public SovereigntyGame Game;

		public ActiveStat<int> Gold;

		public bool Lucky;

		private int LuckTurns;

		private List<WorkingProvince> m_Provinces;

		private List<WorkingStack> m_Stacks;

		private List<WorkingUnit> m_UnitList;
	}
}
