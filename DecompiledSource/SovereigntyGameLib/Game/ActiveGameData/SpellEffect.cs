// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Game.ActiveGameData.SpellEffect
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public abstract class SpellEffect
	{
		private static List<SpellEffect> AllEffects;

		public string SpellName;

		public RealmMagicData SpellData;

		public SovereigntyGame Game;

		public int CasterID;

		public int Timer;

		public int ID;

		private List<WorkingProvince> ProvincesInRange;

		private List<WorkingZone> ZonesInRange;

		private List<ActivePathNode> NodesInRange;

		internal bool LoadOK;

		public WorkingRealm Caster => Game.AllRealms[CasterID];

		private static void CreateEffects(SovereigntyGame Game)
		{
			AllEffects = new List<SpellEffect>();
			List<Type> list = (from t in Game.GameCore.Utilities.ScriptManager.SpellAssembly.GetTypes()
							   where t.IsSubclassOf(typeof(SpellEffect))
							   select t).ToList();
			foreach (Type item2 in list)
			{
				if (!(item2.GetConstructor(Type.EmptyTypes) == null))
				{
					SpellEffect item = (SpellEffect)Activator.CreateInstance(item2);
					AllEffects.Add(item);
				}
			}
		}

		public static SpellEffect CreateEffect(SovereigntyGame Game, RealmMagicData SpellData, WorkingRealm Caster)
		{
			if (AllEffects == null)
			{
				CreateEffects(Game);
			}
			SpellEffect spellEffect = AllEffects.SingleOrDefault((SpellEffect x) => x.SpellName == SpellData.Name);
			if (spellEffect == null)
			{
				throw new Exception("Spell effect for " + SpellData.Name + " does not exist.");
			}
			Type type = spellEffect.GetType();
			SpellEffect spellEffect2 = (SpellEffect)Activator.CreateInstance(type);
			spellEffect2.SpellData = SpellData;
			spellEffect2.Game = Game;
			spellEffect2.CasterID = Caster.ID;
			return spellEffect2;
		}

		public static SpellEffect LoadEffect(SovereigntyGame Game, RealmMagicData SpellData, BinaryReader r, int SaveVersion)
		{
			if (AllEffects == null)
			{
				CreateEffects(Game);
			}
			SpellEffect spellEffect = AllEffects.SingleOrDefault((SpellEffect x) => x.SpellName == SpellData.Name);
			if (spellEffect == null)
			{
				throw new Exception("Spell effect for " + SpellData.Name + " does not exist.");
			}
			Type type = spellEffect.GetType();
			SpellEffect spellEffect2 = (SpellEffect)Activator.CreateInstance(type);
			spellEffect2.SpellData = SpellData;
			spellEffect2.Game = Game;
			spellEffect2.LoadOK = spellEffect2.Load(r, SaveVersion);
			return spellEffect2;
		}

		public SpellEffect()
		{
		}

		protected bool RealmInRange(WorkingRealm Target)
		{
			if (ProvincesInRange == null)
			{
				ProvincesInRange = Game.PathManager.GetSpellTargetProvinces(Game.AllRealms[CasterID], SpellData.Range);
			}
			return ProvincesInRange.FirstOrDefault((WorkingProvince x) => x.OwnerID == Target.ID) != null;
		}

		protected bool StackInRange(WorkingStack Target)
		{
			if (NodesInRange == null)
			{
				NodesInRange = Game.PathManager.GetSpellTargetNodes(Game.AllRealms[CasterID], SpellData.Range);
			}
			return NodesInRange.Contains(Target.Node);
		}

		protected bool ProvinceInRange(WorkingProvince Target)
		{
			if (ProvincesInRange == null)
			{
				ProvincesInRange = Game.PathManager.GetSpellTargetProvinces(Game.AllRealms[CasterID], SpellData.Range);
			}
			return ProvincesInRange.Contains(Target);
		}

		protected bool ZoneInRange(WorkingZone Target)
		{
			if (ZonesInRange == null)
			{
				ZonesInRange = Game.PathManager.GetSpellTargetZones(Game.AllRealms[CasterID], SpellData.Range);
			}
			return ZonesInRange.Contains(Target);
		}

		public bool TargetNeeded()
		{
			if (SpellData.TargetType == SpellTargets.None)
			{
				return false;
			}
			if (SpellData.TargetType == SpellTargets.Realm && SpellData.Range == 0)
			{
				return false;
			}
			return true;
		}

		public bool TargetIsValid(object Target)
		{
			switch (SpellData.TargetType)
			{
				case SpellTargets.Province:
					{
						if (!(Target is WorkingProvince))
						{
							return false;
						}
						WorkingProvince workingProvince = Target as WorkingProvince;
						if (SpellData.Type == SpellTypes.Negative && workingProvince.HasStatus("BlockNegativeSpells"))
						{
							return false;
						}
						if (workingProvince.SpellEffects.IgnoreSpellType(SpellData.Type))
						{
							return false;
						}
						if (workingProvince.SpellEffects.IgnoreSpell(SpellData.Name))
						{
							return false;
						}
						break;
					}
				case SpellTargets.Realm:
					if (!(Target is WorkingRealm))
					{
						return false;
					}
					break;
				case SpellTargets.Stack:
					{
						if (!(Target is WorkingStack))
						{
							return false;
						}
						WorkingStack workingStack = Target as WorkingStack;
						if (workingStack.Node != null && workingStack.Node.Province != null)
						{
							if (workingStack.Node.Province.SpellEffects.IgnoreSpellType(SpellData.Type))
							{
								return false;
							}
							if (workingStack.Node.Province.SpellEffects.IgnoreSpell(SpellData.Name))
							{
								return false;
							}
						}
						break;
					}
				case SpellTargets.Unit:
					{
						if (!(Target is WorkingUnit))
						{
							return false;
						}
						WorkingUnit workingUnit = Target as WorkingUnit;
						if (workingUnit.SpellEffects.IgnoreSpellType(SpellData.Type))
						{
							return false;
						}
						if (workingUnit.SpellEffects.IgnoreSpell(SpellData.Name))
						{
							return false;
						}
						if (workingUnit.OwnerStack != null && workingUnit.OwnerStack.Node != null && workingUnit.OwnerStack.Node.Province != null)
						{
							if (workingUnit.OwnerStack.Node.Province.SpellEffects.IgnoreSpellType(SpellData.Type))
							{
								return false;
							}
							if (workingUnit.OwnerStack.Node.Province.SpellEffects.IgnoreSpell(SpellData.Name))
							{
								return false;
							}
						}
						break;
					}
				case SpellTargets.SeaZone:
					if (!(Target is WorkingZone))
					{
						return false;
					}
					break;
			}
			return CanCastOnTarget(Target);
		}

		public void Save(BinaryWriter w)
		{
			w.Write(ID);
			w.Write(SpellName);
			w.Write(CasterID);
			w.Write(Timer);
			SaveData(w);
		}

		public bool Load(BinaryReader r, int SaveVersion)
		{
			ID = r.ReadInt32();
			SpellName = r.ReadString();
			CasterID = r.ReadInt32();
			Timer = r.ReadInt32();
			return LoadData(r, SaveVersion);
		}

		protected abstract void SaveData(BinaryWriter w);

		protected abstract bool LoadData(BinaryReader r, int SaveVersion);

		protected abstract bool CanCastOnTarget(object Target);

		protected abstract void ApplyEffect(object Target);

		protected abstract void RemoveEffect();

		protected abstract int AICastChance(List<object> TargetList);

		public abstract int AITargetWeight(object Target);

		public int GetAICastChance(List<object> TargetList)
		{
			return AICastChance(TargetList);
		}

		public SpellTargetData GetTargetData(object Target)
		{
			if (Target == null)
			{
				return Game.GlobalSpellEffects;
			}
			if (Target is WorkingRealm)
			{
				return (Target as WorkingRealm).SpellEffects;
			}
			if (Target is WorkingProvince)
			{
				return (Target as WorkingProvince).SpellEffects;
			}
			if (Target is WorkingZone)
			{
				return (Target as WorkingZone).SpellEffects;
			}
			if (Target is WorkingUnit)
			{
				return (Target as WorkingUnit).SpellEffects;
			}
			if (Target is WorkingStack)
			{
				throw new Exception("Stacks are not valid targets for persistent spells, apply them to the units in the stack instead");
			}
			throw new Exception("Target is not a valid spell target type: " + Target.GetType().Name);
		}

		public bool Dispel(bool Force)
		{
			if (SpellData.NoDispell && !Force)
			{
				return false;
			}
			Game.AllRealms[CasterID].MagicData.RemoveCastSpell(ID);
			RemoveEffect();
			Game.GameCore.FireEvent("SpellsChanged");
			Game.RemoveSpell(this);
			return true;
		}

		public bool CastOnTarget(object Target)
		{
			if (!(Target is WorkingStack))
			{
				SpellEffect spellOfType = GetTargetData(Target).GetSpellOfType(GetType());
				if (spellOfType != null && !spellOfType.Dispel(Force: false))
				{
					return false;
				}
			}
			Vector2 targetCoords = GetTargetCoords(Target);
			Game.AllRealms[CasterID].MagicData.SpellCoolDown = Caster.GetRechargeTime(this);
			if (SpellData.Duration > 0 || SpellData.Duration == -1)
			{
				Timer = SpellData.Duration;
				ID = Game.AddSpell(this);
				Game.AllRealms[CasterID].MagicData.AddCastSpell(ID);
			}
			ApplyEffect(Target);
			Game.GameCore.FireEvent("SpellsChanged");
			Game.GameCore.FireEvent("SpellCast", this);
			if (ProvincesInRange != null)
			{
				ProvincesInRange.Clear();
			}
			if (ZonesInRange != null)
			{
				ZonesInRange.Clear();
			}
			if (NodesInRange != null)
			{
				NodesInRange.Clear();
			}
			ProvincesInRange = null;
			ZonesInRange = null;
			NodesInRange = null;
			Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + SpellData.SoundFileName + ".wav");
			Vector2 size = new Vector2(100f, 100f);
			string key = "";
			switch (SpellData.School)
			{
				case SpellSchools.Death:
					key = "attack_death";
					break;
				case SpellSchools.Illusion:
					key = "attack_illusion";
					break;
				case SpellSchools.Nature:
					key = "attack_nature";
					break;
				case SpellSchools.War:
					key = "attack_war";
					break;
			}
			AnimationData animation = Game.GameCore.Data.Animations[key];
			Game.AddSpellAnimation(animation, targetCoords, size);
			return true;
		}

		public virtual int GetGoldIncome(WorkingProvince Province)
		{
			return 0;
		}

		private Vector2 GetTargetCoords(object Target)
		{
			if (Target is WorkingUnit)
			{
				WorkingUnit workingUnit = Target as WorkingUnit;
				if (workingUnit.OwnerStack == null)
				{
					throw new Exception("Spell cast on unit which is not part of a stack");
				}
				return new Vector2(workingUnit.OwnerStack.Node.MapCoords.X, workingUnit.OwnerStack.Node.MapCoords.Y);
			}
			if (Target is WorkingStack)
			{
				WorkingStack workingStack = Target as WorkingStack;
				if (workingStack.Node == null)
				{
					throw new Exception("Spell cast on stack which is not on map");
				}
				return new Vector2(workingStack.Node.MapCoords.X, workingStack.Node.MapCoords.Y);
			}
			if (Target is WorkingProvince)
			{
				WorkingProvince workingProvince = Target as WorkingProvince;
				return new Vector2(workingProvince.CapitolCoords.X, workingProvince.CapitolCoords.Y);
			}
			if (Target is WorkingZone)
			{
				WorkingZone workingZone = Target as WorkingZone;
				return new Vector2(workingZone.Nodes[0].MapCoords.X, workingZone.Nodes[0].MapCoords.Y);
			}
			if (Target is WorkingRealm)
			{
				WorkingRealm workingRealm = Target as WorkingRealm;
				if (workingRealm.CapitolProvince == null)
				{
					throw new Exception("Spell cast on dead realm");
				}
				return new Vector2(workingRealm.CapitolProvince.CapitolCoords.X, workingRealm.CapitolProvince.CapitolCoords.Y);
			}
			return new Vector2(-1000f, -1000f);
		}

		internal void UpdateTimer()
		{
			if (Timer != -1)
			{
				if (Timer < -1)
				{
					Timer = 1;
				}
				Timer--;
				if (Timer == 0)
				{
					Dispel(Force: true);
				}
			}
		}
	}
}