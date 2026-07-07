using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WorkingUnit
	{
		public int OwnerRealmID
		{
			get
			{
				return this.m_OwnerRealmID;
			}
			set
			{
				if (this.m_OwnerRealmID == value)
				{
					return;
				}
				this.Game.GameCore.FireEvent("UnitOwnerChanged", new object[] { this, this.m_OwnerRealmID, value });
				if (this.m_OwnerRealmID > 0)
				{
					this.Game.AllRealms[this.m_OwnerRealmID].UnitsChanged();
				}
				this.m_OwnerRealmID = value;
				if (this.m_OwnerRealmID > 0)
				{
					this.Game.AllRealms[this.m_OwnerRealmID].UnitsChanged();
				}
			}
		}

		public event UnitstatusDelegate OnStatusRequested;

		public event UnitDamageDelegate OnDamageTakenModRequested;

		public event UnitDamageDelegate OnDamageDealtModRequested;

		public event UnitDamageTypeDelegate OnDamageTypeRequested;

		public event UnitDamagedDelegate OnDamageDealt;

		public event UnitDamagedDelegate OnDamageTaken;

		public event UnitDamagedDelegate OnHealed;

		public event UnitStackDelegate OnStackChanged;

		public event Action OnBattleStarted;

		public event UnitMoveTypeDelegate OnMoveTypeRequested;

		public event UnitRegionDelegate OnRegionEntryRequested;

		public event UnitCombatNotifyDelegate OnCombatNotification;

		public event Action OnBattleStatsReset;

		public event UnitBoolDelegate OnCausedRetreat;

		public event UnitMoveCostDelegate OnMoveCostModRequest;

		public event UnitMovePathDelegate OnUnitMoved;

		public event Action OnBattleTurnStarted;

		public event UnitIntDelegate OnMoved;

		public int XP
		{
			get
			{
				return this.m_XP;
			}
			set
			{
				if (!this.CanPromote())
				{
					this.m_XP = 0;
				}
				else
				{
					this.m_XP = value;
				}
				if (this.m_XP < 0)
				{
					throw new Exception("Negative XP not possible");
				}
			}
		}

		public IList<UnitFlag> ActiveFlags
		{
			get
			{
				return this.AllFlags.Where((UnitFlag x) => x.AttachedToUnit).ToList<UnitFlag>().AsReadOnly();
			}
		}

		public int ActiveFlagCount
		{
			get
			{
				return this.AllFlags.Count((UnitFlag x) => x.AttachedToUnit);
			}
		}

		public int OwnerStackID
		{
			get
			{
				return this.m_OwnerStackID;
			}
			set
			{
				int ownerStackID = this.m_OwnerStackID;
				this.m_OwnerStackID = value;
				if (ownerStackID > 0)
				{
					this.Game.AllStacks[ownerStackID].UnitsChanged();
				}
				if (this.m_OwnerStackID > 0)
				{
					this.Game.AllStacks[this.m_OwnerStackID].UnitsChanged();
				}
				if (this.OnStackChanged != null)
				{
					this.OnStackChanged(this, ownerStackID, this.OwnerStack);
				}
			}
		}

		public MoveTypes MoveType
		{
			get
			{
				MoveTypes defaultMoveType = this.DefaultMoveType;
				if (this.OnMoveTypeRequested != null)
				{
					this.OnMoveTypeRequested(ref defaultMoveType);
				}
				return defaultMoveType;
			}
		}

		public bool Disabled
		{
			get
			{
				return this.Health <= 0 || this.IsPrisoner;
			}
		}

		public IList<UnitFlag> Abilities
		{
			get
			{
				List<UnitFlag> list = new List<UnitFlag>();
				using (List<UnitFlag>.Enumerator enumerator = this.AllFlags.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						UnitFlag Flag = enumerator.Current;
						if (list.Count((UnitFlag x) => x.GetType() == Flag.GetType()) < Flag.MaxStack)
						{
							list.Add(Flag);
						}
					}
				}
				return list.AsReadOnly();
			}
		}

		public WorkingUnit CarriedUnit
		{
			get
			{
				WorkingUnit workingUnit = null;
				this.Game.AllUnits.TryGetValue(this.CarriedUnitID, out workingUnit);
				return workingUnit;
			}
		}

		public WorkingStack OwnerStack
		{
			get
			{
				WorkingStack workingStack = null;
				this.Game.AllStacks.TryGetValue(this.OwnerStackID, out workingStack);
				return workingStack;
			}
		}

		public WorkingRealm OwnerRealm
		{
			get
			{
				WorkingRealm workingRealm = null;
				this.Game.AllRealms.TryGetValue(this.OwnerRealmID, out workingRealm);
				return workingRealm;
			}
		}

		public WorkingUnit(SovereigntyGame Game, int ID, UnitData BaseUnit)
		{
			this.Game = Game;
			this.ID = ID;
			this.AllFlags = new List<UnitFlag>();
			this.MedalNames = new List<string>();
			if (BaseUnit != null)
			{
				this.BaseType = BaseUnit;
				this.TrainTime = BaseUnit.TrainTime;
				this.ImageFile = BaseUnit.ImageFile;
				this.DisplayName = BaseUnit.DisplayName;
				this.BaseName = BaseUnit.ToString();
				this.BaseCost = BaseUnit.Cost;
				this.Class = BaseUnit.Class;
				this.Rank = BaseUnit.Rank;
				this.EntityType = BaseUnit.IsSingleEntity;
				this.Race = BaseUnit.Race;
				this.DefaultDamageType = BaseUnit.DamageType;
				this.AttackAnimation = BaseUnit.AttackAnimation;
				this.ContactValue = BaseUnit.ContactValue;
				this.CanPack = BaseUnit.CanPack;
				this.Transport = BaseUnit.AllowTransport;
				this.Formation = BaseUnit.Formation;
				this.MoveSound = BaseUnit.MoveSound;
				this.MeleeSound = BaseUnit.SoundFile;
				this.RangedSound = BaseUnit.RangedSoundFile;
				this.FirstMedals = new List<string>(BaseUnit.FirstMedals);
				this.SecondMedals = new List<string>(BaseUnit.SecondMedals);
				if (this.Class == UnitClasses.Fort)
				{
					this.DefaultMoveType = MoveTypes.None;
				}
				else if (this.Class == UnitClasses.Naval)
				{
					this.DefaultMoveType = MoveTypes.Sea;
				}
				else
				{
					this.DefaultMoveType = MoveTypes.Land;
				}
				int num = 1;
				if (this.Rank == UnitRanks.Elite)
				{
					num = 2;
				}
				if (this.Rank == UnitRanks.Unique)
				{
					num = 3;
				}
				this.Attack = new UnitStat(Game, ID, UnitStatNames.Attack, BaseUnit.Attack);
				this.Defence = new UnitStat(Game, ID, UnitStatNames.Defence, BaseUnit.Defence);
				this.RangedAttack = new UnitStat(Game, ID, UnitStatNames.Rangedattack, BaseUnit.RangedAttack);
				this.Range = new UnitStat(Game, ID, UnitStatNames.Range, Math.Max(BaseUnit.Range, 1));
				this.HealRate = new UnitStat(Game, ID, UnitStatNames.Heal, BaseUnit.HealRate);
				this.Initiative = new UnitStat(Game, ID, UnitStatNames.Initiative, BaseUnit.Speed);
				this.Discipline = new UnitStat(Game, ID, UnitStatNames.Discipline, BaseUnit.Discipline);
				this.MaxCombatMoves = new UnitStat(Game, ID, UnitStatNames.MaxCombatMoves, BaseUnit.Move);
				this.Upkeep = new UnitStat(Game, ID, UnitStatNames.Upkeep, BaseUnit.Upkeep);
				this.Upkeep.OnRequestModifier += this.Upkeep_OnRequestModifier;
				this.RangedAttack.OnRequestModifier += this.RangedAttack_OnRequestModifier;
				this.Range.OnRequestModifier += this.Range_OnRequestModifier;
				this.Attack.OnRequestModifier += this.Attack_OnRequestModifier;
				this.Defence.OnRequestModifier += this.Defence_OnRequestModifier;
				this.Attack.OnRequestGenericModifier += this.StatModifierRequested;
				this.Defence.OnRequestGenericModifier += this.StatModifierRequested;
				this.RangedAttack.OnRequestGenericModifier += this.StatModifierRequested;
				this.Range.OnRequestGenericModifier += this.StatModifierRequested;
				this.HealRate.OnRequestGenericModifier += this.StatModifierRequested;
				this.Initiative.OnRequestGenericModifier += this.StatModifierRequested;
				this.Discipline.OnRequestGenericModifier += this.StatModifierRequested;
				this.MaxCombatMoves.OnRequestGenericModifier += this.StatModifierRequested;
				this.Upkeep.OnRequestGenericModifier += this.StatModifierRequested;
				this.Suppression = new UnitStat(Game, ID, UnitStatNames.Suppression, num);
				this.CombatMoves = new ActiveStat<float>((float)BaseUnit.Move);
				using (List<string>.Enumerator enumerator = this.BaseType.Abilities.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						string text = enumerator.Current;
						UnitFlag unitFlag = UnitFlag.AttemptCreateNamedFlag(Game.GameCore, text);
						if (unitFlag != null)
						{
							this.GrantFlag(unitFlag);
						}
					}
					goto IL_0403;
				}
			}
			if (!(this is WorkingHero))
			{
				throw new Exception("Unit created with no base type");
			}
			IL_0403:
			if (this.MeleeSound == null)
			{
				this.MeleeSound = "";
			}
			if (this.RangedSound == null)
			{
				this.RangedSound = "";
			}
			if (this.MoveSound == null)
			{
				this.MoveSound = "";
			}
			this.SpellEffects = new SpellTargetData(Game);
			this.Health = new ActiveStat<int>(100);
			this.Morale = new ActiveStat<int>(100);
			this.Health.OnStatChanged += this.Health_OnStatChanged;
			this.Morale.OnStatChanged += this.Morale_OnStatChanged;
		}

		private void StatModifierRequested(WorkingUnit Unit, WorkingUnit EnemyUnit, UnitStatNames StatName, ref int Value)
		{
			if (this.OwnerStack != null)
			{
				Value += this.OwnerStack.GetStackModifier(StatName);
			}
		}

		private void Defence_OnRequestModifier(WorkingUnit Unit, WorkingUnit EnemyUnit, ref int Value)
		{
			if (this.Class == UnitClasses.Siege && EnemyUnit != null && EnemyUnit.Class == UnitClasses.Fort)
			{
				Value += 2;
			}
		}

		private void Morale_OnStatChanged()
		{
			if (this.Morale.Value > 100)
			{
				this.Morale.Value = 100;
			}
		}

		private void Attack_OnRequestModifier(WorkingUnit Unit, WorkingUnit EnemyUnit, ref int Value)
		{
			if (this.GetDamageType() != DamageTypes.Physical && EnemyUnit != null && EnemyUnit.HasStatus("MagicVulnerability", new object[0]))
			{
				Value++;
			}
			if (this.Class == UnitClasses.Siege && EnemyUnit != null && EnemyUnit.Class == UnitClasses.Fort)
			{
				if (this.BattleData != null && this.BattleData.Battle == null)
				{
					Value += 3;
					return;
				}
				Value += 5;
			}
		}

		public WorkingUnit(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.ID = r.ReadInt32();
			this.m_OwnerStackID = r.ReadInt32();
			this.OwnerRealmID = r.ReadInt32();
			this.TrainTime = r.ReadInt32();
			this.DisplayName = r.ReadString();
			this.m_MovePoints = r.ReadSingle();
			this.Transport = r.ReadBoolean();
			this.CarriedUnitID = r.ReadInt32();
			this.ImageFile = r.ReadString();
			if (this.ImageFile == "")
			{
				this.ImageFile = null;
			}
			this.BaseName = r.ReadString();
			if (SaveVersion < 43)
			{
				string[] array = this.BaseName.Split(new char[] { '.' });
				if (array.Length == 2)
				{
					if (array[1].ToLowerInvariant() != this.OwnerRealm.Name.ToLowerInvariant())
					{
						array[1] = this.OwnerRealm.Name;
					}
					this.BaseName = array[1] + "." + array[0];
				}
			}
			Game.Data.Units.TryGetValue(this.BaseName, out this.BaseType);
			this.Attack = new UnitStat(Game, this.ID, UnitStatNames.Attack, 0);
			this.Defence = new UnitStat(Game, this.ID, UnitStatNames.Defence, 0);
			this.RangedAttack = new UnitStat(Game, this.ID, UnitStatNames.Rangedattack, 0);
			this.Range = new UnitStat(Game, this.ID, UnitStatNames.Range, 0);
			this.HealRate = new UnitStat(Game, this.ID, UnitStatNames.Heal, 0);
			this.Initiative = new UnitStat(Game, this.ID, UnitStatNames.Initiative, 0);
			this.Discipline = new UnitStat(Game, this.ID, UnitStatNames.Discipline, 0);
			this.MaxCombatMoves = new UnitStat(Game, this.ID, UnitStatNames.MaxCombatMoves, 0);
			this.Upkeep = new UnitStat(Game, this.ID, UnitStatNames.Upkeep, 0);
			this.Suppression = new UnitStat(Game, this.ID, UnitStatNames.Suppression, 0);
			this.Health = new ActiveStat<int>(0);
			this.Morale = new ActiveStat<int>(0);
			this.CombatMoves = new ActiveStat<float>(0f);
			int num = r.ReadInt32();
			this.AllFlags = new List<UnitFlag>();
			for (int i = 0; i < num; i++)
			{
				UnitFlag unitFlag = UnitFlag.CreateNamedFlag(Game.GameCore, r.ReadString());
				unitFlag.LoadSavedState(r, SaveVersion);
				this.GrantFlag(unitFlag);
			}
			this.BaseCost = r.ReadInt32();
			this.Class = (UnitClasses)r.ReadInt16();
			this.Rank = (UnitRanks)r.ReadInt16();
			this.Attack.Load(r, SaveVersion);
			this.Defence.Load(r, SaveVersion);
			this.RangedAttack.Load(r, SaveVersion);
			this.Range.Load(r, SaveVersion);
			this.HealRate.Load(r, SaveVersion);
			this.Initiative.Load(r, SaveVersion);
			this.Discipline.Load(r, SaveVersion);
			this.MaxCombatMoves.Load(r, SaveVersion);
			this.Upkeep.Load(r, SaveVersion);
			this.Suppression.Load(r, SaveVersion);
			this.Health.Value = r.ReadInt32();
			this.Morale.Value = r.ReadInt32();
			this.CombatMoves.Value = r.ReadSingle();
			this.SpellEffects = new SpellTargetData(Game);
			this.Race = (Races)r.ReadInt16();
			this.DefaultDamageType = (DamageTypes)r.ReadInt16();
			this.DefaultMoveType = (MoveTypes)r.ReadInt16();
			this.Medals = r.ReadInt32();
			this.CanPack = r.ReadBoolean();
			this.AttackAnimation = r.ReadString();
			if (this.AttackAnimation == "")
			{
				this.AttackAnimation = null;
			}
			this.IsPrisoner = r.ReadBoolean();
			this.ContactValue = r.ReadInt32();
			this.TeleportActive = r.ReadBoolean();
			this.Health.OnStatChanged += this.Health_OnStatChanged;
			this.Morale.OnStatChanged += this.Morale_OnStatChanged;
			this.Upkeep.OnRequestModifier += this.Upkeep_OnRequestModifier;
			this.RangedAttack.OnRequestModifier += this.RangedAttack_OnRequestModifier;
			this.Range.OnRequestModifier += this.Range_OnRequestModifier;
			this.Attack.OnRequestModifier += this.Attack_OnRequestModifier;
			this.Defence.OnRequestModifier += this.Defence_OnRequestModifier;
			this.Attack.OnRequestGenericModifier += this.StatModifierRequested;
			this.Defence.OnRequestGenericModifier += this.StatModifierRequested;
			this.RangedAttack.OnRequestGenericModifier += this.StatModifierRequested;
			this.Range.OnRequestGenericModifier += this.StatModifierRequested;
			this.HealRate.OnRequestGenericModifier += this.StatModifierRequested;
			this.Initiative.OnRequestGenericModifier += this.StatModifierRequested;
			this.Discipline.OnRequestGenericModifier += this.StatModifierRequested;
			this.MaxCombatMoves.OnRequestGenericModifier += this.StatModifierRequested;
			this.Upkeep.OnRequestGenericModifier += this.StatModifierRequested;
			int num2 = r.ReadInt32();
			this.FirstMedals = new List<string>();
			this.SecondMedals = new List<string>();
			if (r.ReadBoolean())
			{
				num = r.ReadInt32();
				for (int j = 0; j < num; j++)
				{
					this.FirstMedals.Add(r.ReadString());
				}
				num = r.ReadInt32();
				for (int k = 0; k < num; k++)
				{
					this.SecondMedals.Add(r.ReadString());
				}
			}
			this.MedalNames = new List<string>();
			num = r.ReadInt32();
			for (int l = 0; l < num; l++)
			{
				this.MedalNames.Add(r.ReadString());
			}
			this.XP = num2;
			if (SaveVersion >= 30)
			{
				this.TempUnit = r.ReadBoolean();
			}
			if (SaveVersion >= 44)
			{
				this.EntityType = (EntityType)r.ReadInt16();
			}
			else if (this.BaseType != null)
			{
				this.EntityType = this.BaseType.IsSingleEntity;
			}
			if (SaveVersion >= 49)
			{
				this.MeleeSound = r.ReadString();
				this.MoveSound = r.ReadString();
				this.RangedSound = r.ReadString();
			}
			else if (this.BaseType != null)
			{
				this.MeleeSound = this.BaseType.SoundFile;
				this.MoveSound = this.BaseType.MoveSound;
				this.RangedSound = this.BaseType.RangedSoundFile;
			}
			if (SaveVersion >= 53)
			{
				this.Formation = r.ReadInt32();
			}
			else if (this.BaseType != null)
			{
				this.Formation = this.BaseType.Formation;
			}
			if (this.MeleeSound == null)
			{
				this.MeleeSound = "";
			}
			if (this.RangedSound == null)
			{
				this.RangedSound = "";
			}
			if (this.MoveSound == null)
			{
				this.MoveSound = "";
			}
		}

		internal virtual void Save(BinaryWriter w)
		{
			w.Write(this.ID);
			w.Write(this.m_OwnerStackID);
			w.Write(this.OwnerRealmID);
			w.Write(this.TrainTime);
			w.Write(this.DisplayName);
			w.Write(this.m_MovePoints);
			w.Write(this.Transport);
			w.Write(this.CarriedUnitID);
			if (this.ImageFile == null)
			{
				w.Write("");
			}
			else
			{
				w.Write(this.ImageFile);
			}
			w.Write(this.BaseName);
			w.Write(this.AllFlags.Count((UnitFlag x) => !x.DoNotSave));
			foreach (UnitFlag unitFlag in this.AllFlags.Where((UnitFlag x) => !x.DoNotSave))
			{
				w.Write(unitFlag.Name);
				unitFlag.SaveCurrentState(w);
			}
			w.Write(this.BaseCost);
			w.Write((short)this.Class);
			w.Write((short)this.Rank);
			this.Attack.Save(w);
			this.Defence.Save(w);
			this.RangedAttack.Save(w);
			this.Range.Save(w);
			this.HealRate.Save(w);
			this.Initiative.Save(w);
			this.Discipline.Save(w);
			this.MaxCombatMoves.Save(w);
			this.Upkeep.Save(w);
			this.Suppression.Save(w);
			w.Write(this.Health.Value);
			w.Write(this.Morale.Value);
			w.Write(this.CombatMoves.Value);
			w.Write((short)this.Race);
			w.Write((short)this.DefaultDamageType);
			w.Write((short)this.DefaultMoveType);
			w.Write(this.Medals);
			w.Write(this.CanPack);
			if (this.AttackAnimation == null)
			{
				w.Write("");
			}
			else
			{
				w.Write(this.AttackAnimation);
			}
			w.Write(this.IsPrisoner);
			w.Write(this.ContactValue);
			w.Write(this.TeleportActive);
			w.Write(this.XP);
			if (this.FirstMedals != null)
			{
				w.Write(true);
				w.Write(this.FirstMedals.Count);
				foreach (string text in this.FirstMedals)
				{
					w.Write(text);
				}
				w.Write(this.SecondMedals.Count);
				using (List<string>.Enumerator enumerator3 = this.SecondMedals.GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						string text2 = enumerator3.Current;
						w.Write(text2);
					}
					goto IL_0328;
				}
			}
			w.Write(false);
			IL_0328:
			w.Write(this.MedalNames.Count);
			foreach (string text3 in this.MedalNames)
			{
				w.Write(text3);
			}
			w.Write(this.TempUnit);
			w.Write((short)this.EntityType);
			w.Write(this.MeleeSound);
			w.Write(this.MoveSound);
			w.Write(this.RangedSound);
			w.Write(this.Formation);
		}

		public bool CanPromote()
		{
			if (this.FirstMedals == null)
			{
				return false;
			}
			int num = this.FirstMedals.Count((string x) => x.ToLowerInvariant() != "none" && x != "");
			return this.Medals < num;
		}

		private void Range_OnRequestModifier(WorkingUnit Unit, WorkingUnit EnemyUnit, ref int Value)
		{
			if (Unit.BattleData != null && Unit.BattleData.Battle == null && Value > 3)
			{
				Value--;
			}
		}

		private void RangedAttack_OnRequestModifier(WorkingUnit Unit, WorkingUnit EnemyUnit, ref int Value)
		{
			if (Unit.BattleData != null && Unit.BattleData.Battle == null)
			{
				Value--;
			}
			if (this.Class == UnitClasses.Siege && EnemyUnit != null && EnemyUnit.Class == UnitClasses.Fort)
			{
				if (this.BattleData != null && this.BattleData.Battle == null)
				{
					Value += 2;
					return;
				}
				Value += 4;
			}
		}

		private void Upkeep_OnRequestModifier(WorkingUnit Unit, WorkingUnit EnemyUnit, ref int Value)
		{
			if (this.OwnerStack == null)
			{
				return;
			}
			if (this.OwnerStack.Node.Province != null && this.OwnerStack.Node.Province.IsCapitol && this.OwnerStack.Node.Province.OwnerRealm == this.OwnerRealm)
			{
				Value -= 5000;
			}
		}

		private void Health_OnStatChanged()
		{
			if (this.Health.Value > 100)
			{
				throw new Exception("Unit with over 100 health");
			}
		}

		public float GetValue()
		{
			if (this.Disabled)
			{
				return 0f;
			}
			float num = (float)((this.BaseType == null) ? 0 : (this.BaseType.Value + (int)((double)((float)this.Upkeep.GetValue()) * 1.5)));
			if (this.Class == UnitClasses.Fort)
			{
				num = 1000f;
			}
			if (this.OwnerStack != null && this.OwnerStack.Hero != null)
			{
				num += 150f;
				if (this.OwnerStack.Hero.Legendary)
				{
					num += 150f;
				}
			}
			return num * ((float)this.Health.Value * 0.01f);
		}

		public bool CanMoveAndAttack()
		{
			return this.Class != UnitClasses.Siege || this.Range.GetValue() < 3;
		}

		public void CreateBattleData(AutoBattleController Battle)
		{
			if (this.BattleData != null)
			{
				this.BattleData.Dispose(false);
			}
			this.BattleData = new UnitBattleData(this.Game, null, Battle, this);
			this.BattleData.Sprite.OnAnimationCompleted += this.Sprite_OnAnimationCompleted;
		}

		public void CreateBattleData(TacticalBattleController Battle, bool ResetMoves = true)
		{
			if (this.BattleData != null)
			{
				this.BattleData.Dispose(false);
			}
			this.BattleData = new UnitBattleData(this.Game, Battle, null, this);
			this.BattleData.Sprite.OnAnimationCompleted += this.Sprite_OnAnimationCompleted;
			if (ResetMoves)
			{
				this.ResetBattleMoves();
			}
		}

		public void ClearBattleData()
		{
			foreach (UnitFlag unitFlag in this.AllFlags.Where((UnitFlag x) => x.TurnCount > 0).ToList<UnitFlag>())
			{
				this.RemoveFlag(unitFlag);
			}
			if (this.BattleData == null)
			{
				return;
			}
			this.LocationWhenDisabled = this.BattleData.BattleLocation;
			this.BattleData.Sprite.OnAnimationCompleted -= this.Sprite_OnAnimationCompleted;
			this.BattleData.Dispose(true);
			this.BattleData = null;
		}

		public void MoveCombat(float Moves)
		{
			this.BattleData.CanMove = false;
			this.CombatMoves.Value -= Math.Min(Moves, this.CombatMoves);
			this.BattleData.UpdateImage();
		}

		public void ResetBattleData()
		{
			if (this.BattleData == null)
			{
				return;
			}
			this.BattleData.Reset();
			if (this.OnBattleStatsReset != null)
			{
				this.OnBattleStatsReset();
			}
		}

		public bool IsAuxiliary(WorkingRealm Target)
		{
			if (Target == null)
			{
				foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
				{
					if (workingRealm != this.OwnerRealm)
					{
						if (workingRealm.UnitPurchaseManager.GetAuxiliaryUnits().Count((UnitData x) => x.Name == this.BaseName) > 0)
						{
							return true;
						}
					}
				}
				return false;
			}
			return Target.UnitPurchaseManager.GetAuxiliaryUnits().Count((UnitData x) => x.Name == this.BaseName) > 0;
		}

		public int GetNamedFlagCount(string Flagname)
		{
			return this.AllFlags.Count((UnitFlag x) => x.Name == Flagname);
		}

		public bool HasAnyNamedFlag(string Flagname)
		{
			return this.AllFlags.Count((UnitFlag x) => x.Name == Flagname) > 0;
		}

		public bool HasAnyNamedFlags(params string[] Flagnames)
		{
			return this.AllFlags.Count((UnitFlag x) => Flagnames.Contains(x.Name)) > 0;
		}

		public bool HasAllNamedFlags(params string[] Flagnames)
		{
			int num = 0;
			foreach (UnitFlag unitFlag in this.AllFlags)
			{
				if (Flagnames.Contains(unitFlag.Name))
				{
					num++;
				}
			}
			return num == Flagnames.Length;
		}

		internal bool CanEnterTerrain(GameRegion Region)
		{
			bool flag = true;
			if (this.OnRegionEntryRequested != null)
			{
				flag = this.OnRegionEntryRequested(Region);
			}
			return flag;
		}

		public bool TeleportActive { get; set; }

		public float MovePoints
		{
			get
			{
				return this.m_MovePoints;
			}
		}

		public bool HasMoves()
		{
			return this.m_MovePoints > 0f;
		}

		public void ClearMoves()
		{
			this.m_MovePoints = 0f;
		}

		public void Move(float Moves)
		{
			this.m_MovePoints -= Moves;
			if (this.m_MovePoints < 0f)
			{
				this.m_MovePoints = 0f;
			}
			if (this.OnMoved != null)
			{
				this.OnMoved(this, (int)Moves);
			}
			this.TeleportActive = false;
		}

		public void GrantFlag(UnitFlag Flag)
		{
			if (this.AllFlags.Count((UnitFlag x) => x.GetType() == Flag.GetType()) < Flag.MaxStack)
			{
				Flag.Attach(this);
			}
			this.AllFlags.Add(Flag);
			this.Game.GameCore.FireEvent("UnitFlagGained", new object[] { this, Flag });
		}

		public void RemoveNamedFlags(string Flagname, int Count = 0)
		{
			List<UnitFlag> list = this.AllFlags.Where((UnitFlag x) => x.Name == Flagname).ToList<UnitFlag>();
			if (Count > 0)
			{
				list = list.Take(Count).ToList<UnitFlag>();
			}
			foreach (UnitFlag unitFlag in list)
			{
				this.RemoveFlag(unitFlag);
			}
		}

		public void RemoveFlag(UnitFlag Flag)
		{
			if (Flag.AttachedToUnit)
			{
				Flag.Detach(this);
			}
			this.AllFlags.Remove(Flag);
			int maxStack = Flag.MaxStack;
			List<UnitFlag> list = this.AllFlags.Where((UnitFlag x) => x.GetType() == Flag.GetType()).ToList<UnitFlag>();
			List<UnitFlag> list2 = list.Where((UnitFlag x) => !x.AttachedToUnit).ToList<UnitFlag>();
			int num = list.Count((UnitFlag x) => x.AttachedToUnit);
			while (num < maxStack && list2.Count > 0)
			{
				UnitFlag unitFlag = list2[0];
				unitFlag.Attach(this);
				list2.RemoveAt(0);
			}
		}

		public void AttachNamedFlags(string Flagname)
		{
			List<UnitFlag> list = this.AllFlags.Where((UnitFlag x) => x.Name == Flagname).ToList<UnitFlag>();
			if (list.Count == 0)
			{
				return;
			}
			List<UnitFlag> list2 = list.Where((UnitFlag x) => !x.AttachedToUnit).ToList<UnitFlag>();
			if (list2.Count == 0)
			{
				return;
			}
			int maxStack = list[0].MaxStack;
			int num = list.Count - list2.Count;
			while (num < maxStack && list2.Count > 0)
			{
				UnitFlag unitFlag = list2[0];
				unitFlag.Attach(this);
				list2.RemoveAt(0);
			}
		}

		public void DetachNamedFlags(string Flagname)
		{
			foreach (UnitFlag unitFlag in this.AllFlags.Where((UnitFlag x) => x.Name == Flagname))
			{
				unitFlag.Detach(this);
			}
		}

		public void UpdateBattleFlags()
		{
			foreach (UnitFlag unitFlag in this.AllFlags.ToList<UnitFlag>())
			{
				if (unitFlag.TurnCount != -1)
				{
					unitFlag.TurnCount--;
					if (unitFlag.TurnCount <= 0)
					{
						this.RemoveFlag(unitFlag);
					}
				}
			}
		}

		public void ResetCampaignMoves()
		{
			this.m_MovePoints = 4f;
		}

		public int GetModifiedDamage(WorkingUnit Target, int Damage, bool Ranged, bool Retal, DamageTypes DamageType)
		{
			int num = Damage;
			if (this.OnDamageDealtModRequested != null)
			{
				this.OnDamageDealtModRequested(Target, DamageType, Ranged, Retal, ref num);
			}
			return num;
		}

		public int ApplySimulatedDamage(WorkingUnit Damager, int Damage, bool Ranged, bool Retal, DamageTypes DamageType)
		{
			if (this.OnDamageTakenModRequested != null)
			{
				this.OnDamageTakenModRequested(Damager, DamageType, Ranged, Retal, ref Damage);
			}
			this.Health.Value -= Damage;
			return Damage;
		}

		public void RemoveSimulatedDamage(int Damage)
		{
			this.Health.Value += Damage;
		}

		private void Sprite_OnAnimationCompleted(AnimationSpriteData Animation)
		{
			if (Animation.Tag == "Damage" && this.Health <= 0)
			{
				Random random = new Random();
				switch (random.Next(5) + 1)
				{
				case 1:
					this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\club_strike_body_slash_01.wav");
					break;
				case 2:
					this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\club_strike_body_slash_05.wav");
					break;
				case 3:
					this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\hammer_strike_wood_03.wav");
					break;
				case 4:
					this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\box_cardboard_crush_05.wav");
					break;
				case 5:
					this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\box_cardboard_crush_03.wav");
					break;
				}
				this.BattleData.AddAnimation("unitdeath", "Death");
			}
			if (Animation.Tag == "Death")
			{
				this.BattleData.Battle.KillUnit(this);
			}
		}

		public void ApplyRealDamage(int Damage, DamageTypes DamageType, bool Ranged, WorkingUnit Damager, string DamageAnimation)
		{
			this.Health.Value -= Damage;
			if (this.Health < 0)
			{
				this.Health.Value = 0;
			}
			if (this.BattleData != null)
			{
				this.BattleData.AddHealthFloatie(-Damage);
				if (DamageAnimation != null)
				{
					this.BattleData.AddAnimation(DamageAnimation, "Damage");
				}
				if (this.Health == 0 && Damager != null && Damager.BattleData != null)
				{
					Damager.BattleData.RecordKill();
				}
				if (this.Health == 0 && Damager != null)
				{
					this.Game.GameCore.FireEvent("UnitKilled", new object[] { this, Damager });
				}
				if (Damager == null)
				{
					GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_DAMAGE2", new object[] { Damage });
					gameText.AddChildText(GameText.CreateLocalised(this.OwnerRealm.DisplayName, new object[0]));
					gameText.AddChildText(GameText.CreateLocalised(this.DisplayName, new object[0]));
					this.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
				}
				if (this.Health == 0)
				{
					GameText gameText2 = GameText.CreateLocalised("FORMAT_BATTLELOG_DEAD", new object[0]);
					gameText2.AddChildText(GameText.CreateLocalised(this.OwnerRealm.DisplayName, new object[0]));
					gameText2.AddChildText(GameText.CreateLocalised(this.DisplayName, new object[0]));
					this.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText2 });
				}
				if (this.Health > 0 && Damage > 0)
				{
					if (this.Morale <= 0 && this.BattleData.Battle != null && Damager != null)
					{
						this.BattleData.AddStatusFloatie(GameText.CreateLocalised("MORALE_BROKEN", new object[0]));
						this.BattleData.Battle.RetreatUnitHex(this, Damager, true);
						Damager.HandleRetreatCaused(this, false);
					}
					int num = this.Morale;
					float num2 = (float)(Damage + WorkingUnit.RNG.Next(10) - 4) * this.GetDisciplineMultiplier(Damager);
					this.Morale.Value = Math.Max(this.Morale - (int)num2, 0);
					bool flag = false;
					if (num >= 60 && this.Morale < 60)
					{
						flag = true;
					}
					if (num >= 20 && this.Morale < 20)
					{
						flag = true;
					}
					if (num >= 0 && this.Morale == 0)
					{
						flag = true;
					}
					if (num == 0 && this.Morale > 0)
					{
						flag = true;
					}
					if (num < 20 && this.Morale >= 20)
					{
						flag = true;
					}
					if (num < 60 && this.Morale >= 60)
					{
						flag = true;
					}
					if (flag && this.BattleData != null && !this.Disabled)
					{
						this.ShowMoraleChange();
					}
				}
				if (this.BattleData != null)
				{
					this.BattleData.UpdateImage();
				}
			}
			else if (this.Health == 0)
			{
				this.Game.DestroyUnit(this);
			}
			if (this.OnDamageTaken != null)
			{
				this.OnDamageTaken(Damage, DamageType, Ranged, Damager);
			}
			if (Damager != null)
			{
				Damager.HandleDamageDealt(Damage, DamageType, Ranged, this);
			}
		}

		private void ShowMoraleChange()
		{
			string text;
			if (this.Morale >= 60)
			{
				text = "MORALE_STEADY";
			}
			else if (this.Morale >= 20)
			{
				text = "MORALE_UNCERTAIN";
			}
			else if (this.Morale > 0)
			{
				text = "MORALE_SHAKEN";
			}
			else
			{
				text = "MORALE_BROKEN";
			}
			this.BattleData.AddStatusFloatie(GameText.CreateLocalised(text, new object[0]));
		}

		private float GetDisciplineMultiplier(WorkingUnit Unit)
		{
			switch (this.Discipline.GetValue(Unit))
			{
			case 0:
				return 1.75f;
			case 1:
				return 1.5f;
			case 2:
				return 1.25f;
			case 3:
				return 1f;
			case 4:
				return 0.75f;
			case 5:
				return 0.5f;
			default:
				return 0.25f;
			}
		}

		public void HealMorale(int Amount)
		{
			int num = this.Morale;
			int num2 = this.Morale + Amount;
			if (num2 > 100)
			{
				num2 = 100;
			}
			if (num2 < 0)
			{
				num2 = 0;
			}
			this.Morale.Value = num2;
			bool flag = false;
			if (num >= 60 && this.Morale < 60)
			{
				flag = true;
			}
			if (num >= 20 && this.Morale < 20)
			{
				flag = true;
			}
			if (num >= 0 && this.Morale == 0)
			{
				flag = true;
			}
			if (num == 0 && this.Morale > 0)
			{
				flag = true;
			}
			if (num < 20 && this.Morale >= 20)
			{
				flag = true;
			}
			if (num < 60 && this.Morale >= 60)
			{
				flag = true;
			}
			if (flag && this.BattleData != null)
			{
				this.ShowMoraleChange();
			}
		}

		public void ApplyHealing(int Amount, bool Ranged, WorkingUnit Healer)
		{
			int num = this.Health.Value + Amount;
			if (num > 100)
			{
				num = 100;
			}
			this.Health.Value = num;
			this.HealMorale(Amount / 2);
			if (this.BattleData != null)
			{
				this.BattleData.AddHealthFloatie(Amount);
				if (Healer != null)
				{
					Healer.BattleData.RecordHeal();
				}
				this.BattleData.UpdateImage();
			}
			if (this.OnHealed != null)
			{
				this.OnHealed(Amount, DamageTypes.None, Ranged, Healer);
			}
		}

		private void HandleDamageDealt(int Damage, DamageTypes DamageType, bool Ranged, WorkingUnit Target)
		{
			if (this.OnDamageDealt != null)
			{
				this.OnDamageDealt(Damage, DamageType, Ranged, Target);
			}
		}

		internal void Dispose()
		{
			this.Disposed = true;
			if (this.BattleData != null)
			{
				this.BattleData.Dispose(false);
			}
			this.BattleData = null;
			foreach (UnitFlag unitFlag in this.AllFlags)
			{
				if (unitFlag.AttachedToUnit)
				{
					unitFlag.Detach(this);
				}
			}
			this.AllFlags.Clear();
			this.Attack.Dispose();
			this.Defence.Dispose();
			this.RangedAttack.Dispose();
			this.Range.Dispose();
			this.HealRate.Dispose();
			this.Initiative.Dispose();
			this.Discipline.Dispose();
			this.MaxCombatMoves.Dispose();
			this.Health.Dispose();
			this.Morale.Dispose();
			this.OnBattleStarted = null;
			this.OnBattleStatsReset = null;
			this.OnBattleTurnStarted = null;
			this.OnCausedRetreat = null;
			this.OnCombatNotification = null;
			this.OnDamageDealt = null;
			this.OnDamageTakenModRequested = null;
			this.OnDamageDealtModRequested = null;
			this.OnDamageTaken = null;
			this.OnDamageTypeRequested = null;
			this.OnHealed = null;
			this.OnMoveCostModRequest = null;
			this.OnMoveTypeRequested = null;
			this.OnRegionEntryRequested = null;
			this.OnStackChanged = null;
			this.OnStatusRequested = null;
			this.OnUnitMoved = null;
		}

		internal bool TerrainIsBlocking(BattleTile Tile)
		{
			return Tile.Terrain.BaseType.CombatBlocking && !Tile.HasRoad() && this.MoveType != MoveTypes.Air && this.MoveType != MoveTypes.Phantom && !this.HasStatus("IgnoreTerrain", new object[] { Tile.Terrain.BaseType }) && (this.Race != Races.Undead || !Tile.Terrain.BaseType.IsAnyType(new string[] { "swamp" })) && (this.Race != Races.Dwarf || !Tile.Terrain.BaseType.IsAnyType(new string[] { "mountain" })) && (this.Race != Races.Elf || !Tile.Terrain.BaseType.IsAnyType(new string[] { "old forest" }));
		}

		public bool HasStatus(string StatusName, params object[] args)
		{
			bool flag = false;
			if (this.OnStatusRequested != null)
			{
				this.OnStatusRequested(StatusName, ref flag, this, args);
			}
			return flag;
		}

		public DamageTypes GetDamageType()
		{
			DamageTypes defaultDamageType = this.DefaultDamageType;
			if (this.OnDamageTypeRequested != null)
			{
				this.OnDamageTypeRequested(ref defaultDamageType);
			}
			return defaultDamageType;
		}

		internal void BattleStarted()
		{
			this.IsPrisoner = false;
			this.HasBattled = true;
			if (this.OnBattleStarted != null)
			{
				this.OnBattleStarted();
			}
		}

		internal void CombatNotification(string NotificationType, WorkingUnit OtherUnit, bool Ranged)
		{
			if (this.OnCombatNotification != null)
			{
				this.OnCombatNotification(NotificationType, OtherUnit, Ranged);
			}
			if (NotificationType == "AfterAttacking" && this.BattleData != null)
			{
				this.BattleData.RecordAttack();
			}
		}

		public void ResetBattleMoves()
		{
			this.CombatMoves.Value = (float)this.MaxCombatMoves.GetValue();
			if (this.BattleData != null)
			{
				this.BattleData.CanMove = true;
				this.BattleData.UpdateImage();
			}
		}

		public void ModifyMovementCost(BattleTile Tile, ref float Result)
		{
			if (this.OnMoveCostModRequest != null)
			{
				this.OnMoveCostModRequest(Tile, ref Result);
			}
		}

		public void HandleMovePath(List<Point> PathTiles)
		{
			if (this.OnUnitMoved != null)
			{
				this.OnUnitMoved(PathTiles);
			}
		}

		internal void BeginBattleTurn()
		{
			if (this.OnBattleTurnStarted != null)
			{
				this.OnBattleTurnStarted();
			}
		}

		internal void HandleRetreatCaused(WorkingUnit Unit, bool NoCapture)
		{
			if (this.OnCausedRetreat != null)
			{
				this.OnCausedRetreat(Unit, NoCapture);
			}
		}

		internal int GetLoyalty()
		{
			int num = 0;
			if (this.Rank == UnitRanks.Mercenary)
			{
				num -= 4;
			}
			if (this.OwnerStack.Node.Province != null && !this.OwnerStack.Node.Province.Occupied)
			{
				num += 3;
			}
			if (this.OwnerStack.Hero != null)
			{
				num += 5;
			}
			num += this.Medals * 10;
			if (this.Rank == UnitRanks.Elite)
			{
				num += 10;
			}
			return num + this.Discipline;
		}

		public void AutoSelect()
		{
			if (this.HasStatus("Rooted", new object[0]))
			{
				this.Selected = false;
				return;
			}
			this.Selected = this.MovePoints > 0f;
		}

		public void ToggleSelected()
		{
			if (this.HasStatus("Rooted", new object[0]))
			{
				return;
			}
			if (this.MovePoints == 0f)
			{
				return;
			}
			this.Selected = !this.Selected;
		}

		public string GetFirstMedalName()
		{
			return this.FirstMedals[this.Medals];
		}

		public string GetSecondMedalName()
		{
			return this.SecondMedals[this.Medals];
		}

		public int GetXPForMedal(int Medal)
		{
			int count = this.FirstMedals.Count;
			if (Medal > count)
			{
				return 100000;
			}
			if (Medal == 0)
			{
				return 0;
			}
			if (Medal == 1)
			{
				return this.Game.GameCore.Data.UnitXP["MedalXP"].Medal1XP;
			}
			if (Medal == 2)
			{
				return this.Game.GameCore.Data.UnitXP["MedalXP"].Medal2XP;
			}
			if (Medal == 3)
			{
				return this.Game.GameCore.Data.UnitXP["MedalXP"].Medal3XP;
			}
			if (Medal == 4)
			{
				return this.Game.GameCore.Data.UnitXP["MedalXP"].Medal4XP;
			}
			return 100000;
		}

		public bool ReadytoPromote()
		{
			if (this.Disabled)
			{
				return false;
			}
			if (!this.CanPromote())
			{
				return false;
			}
			int count = this.FirstMedals.Count;
			if (this.Medals >= count)
			{
				return false;
			}
			int num;
			if (this.XP >= this.Game.GameCore.Data.UnitXP["MedalXP"].Medal4XP)
			{
				num = 4;
			}
			else if (this.XP >= this.Game.GameCore.Data.UnitXP["MedalXP"].Medal3XP)
			{
				num = 3;
			}
			else if (this.XP >= this.Game.GameCore.Data.UnitXP["MedalXP"].Medal2XP)
			{
				num = 2;
			}
			else if (this.XP >= this.Game.GameCore.Data.UnitXP["MedalXP"].Medal1XP)
			{
				num = 1;
			}
			else
			{
				num = 0;
			}
			return num > this.Medals;
		}

		internal int GetRevoltReduction()
		{
			int num = 1;
			if (this.Rank == UnitRanks.Elite)
			{
				num = 2;
			}
			if (this.Rank == UnitRanks.Unique)
			{
				num = 3;
			}
			if (this.HasStatus("Police", new object[0]))
			{
				num *= 2;
			}
			return num;
		}

		public UnitFlag GetNamedFlag(string FlagName)
		{
			return this.AllFlags.FirstOrDefault((UnitFlag x) => x.Name == FlagName);
		}

		public bool GetRetreatStatus(WorkingProvince Province)
		{
			if (Province == null)
			{
				return true;
			}
			bool flag = true;
			if (this.HasStatus("RetreatBlocked", new object[] { this, Province }))
			{
				flag = false;
			}
			return flag;
		}

		internal List<GameText> GetCombatStatusEffectsOnSelf(bool Attacking, bool Retal, bool Ranged, WorkingUnit OtherUnit)
		{
			List<GameText> list = new List<GameText>();
			foreach (UnitFlag unitFlag in this.ActiveFlags)
			{
				unitFlag.GetStatusOnSelf(Attacking, Retal, Ranged, OtherUnit, list);
			}
			return list;
		}

		internal List<GameText> GetCombatStatusEffectsOnEnemy(bool Attacking, bool Retal, bool Ranged, WorkingUnit OtherUnit)
		{
			List<GameText> list = new List<GameText>();
			foreach (UnitFlag unitFlag in this.ActiveFlags)
			{
				unitFlag.GetStatusOnEnemy(Attacking, Retal, Ranged, OtherUnit, list);
			}
			return list;
		}

		internal void HandleFlagDetached(UnitFlag Flag)
		{
			if (this.BattleData == null)
			{
				return;
			}
			if (this.BattleData.Battle == null)
			{
				return;
			}
			if (Flag.DisplayName == "")
			{
				return;
			}
			if (Flag.NoFloaties)
			{
				return;
			}
			this.BattleData.AddFlagFloatie(Flag, true);
		}

		internal void HandleFlagAttached(UnitFlag Flag)
		{
			if (this.BattleData == null)
			{
				return;
			}
			if (this.BattleData.Battle == null)
			{
				return;
			}
			if (Flag.DisplayName == "")
			{
				return;
			}
			if (Flag.NoFloaties)
			{
				return;
			}
			this.BattleData.AddFlagFloatie(Flag, false);
		}

		internal void SetTempOwnerStackID(int StackID)
		{
			this.m_OwnerStackID = StackID;
		}

		private static Random RNG = new Random();

		public SovereigntyGame Game;

		private int m_OwnerStackID;

		private int m_OwnerRealmID;

		public int ID;

		public int TrainTime;

		public string DisplayName;

		public bool Selected;

		public bool Disposed;

		private float m_MovePoints = 4f;

		public bool Transport;

		internal int CarriedUnitID = -1;

		public string ImageFile;

		public UnitData BaseType;

		public string BaseName;

		private List<UnitFlag> AllFlags;

		public int BaseCost;

		public UnitClasses Class;

		public UnitRanks Rank;

		public UnitStat Attack;

		public UnitStat Defence;

		public UnitStat RangedAttack;

		public UnitStat Range;

		public UnitStat HealRate;

		public UnitStat Initiative;

		public UnitStat Discipline;

		public UnitStat MaxCombatMoves;

		public UnitStat Upkeep;

		public UnitStat Suppression;

		public bool TempUnit;

		public ActiveStat<int> Health;

		public ActiveStat<int> Morale;

		public ActiveStat<float> CombatMoves;

		public List<string> FirstMedals;

		public List<string> SecondMedals;

		public UnitBattleData BattleData;

		public EntityType EntityType;

		public SpellTargetData SpellEffects;

		public Races Race;

		public DamageTypes DefaultDamageType;

		public MoveTypes DefaultMoveType;

		public int Medals;

		public string AttackAnimation;

		public bool IsPrisoner;

		public int ContactValue;

		public bool CanPack;

		public int Formation;

		public string MoveSound;

		public string MeleeSound;

		public string RangedSound;

		public List<string> MedalNames;

		public bool Promoted;

		private int m_XP;

		public Point LocationWhenDisabled;

		public bool HasBattled;
	}
}
