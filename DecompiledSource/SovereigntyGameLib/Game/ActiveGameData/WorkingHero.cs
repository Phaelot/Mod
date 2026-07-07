using System;
using System.IO;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WorkingHero : WorkingUnit
	{
		public bool Legendary { get; set; }

		public WorkingHero(SovereigntyGame Game, HeroClassData HeroType, int ID)
			: base(Game, ID, null)
		{
			this.NewHero = true;
			this.HeroArt1 = HeroType.ArtName1;
			this.HeroArt2 = HeroType.ArtName2;
			this.BaseName = HeroType.ClassName;
			this.DisplayName = HeroType.DisplayName;
			this.BaseAbility = HeroType.AbilityName;
			this.AbilityOption1 = HeroType.UpgradeName1;
			this.AbilityOption2 = HeroType.UpgradeName2;
			this.Attack = new UnitStat(Game, ID, UnitStatNames.Attack, 0);
			this.Defence = new UnitStat(Game, ID, UnitStatNames.Defence, 0);
			this.RangedAttack = new UnitStat(Game, ID, UnitStatNames.Rangedattack, 0);
			this.Range = new UnitStat(Game, ID, UnitStatNames.Range, 0);
			this.HealRate = new UnitStat(Game, ID, UnitStatNames.Heal, 0);
			this.Initiative = new UnitStat(Game, ID, UnitStatNames.Initiative, 0);
			this.Discipline = new UnitStat(Game, ID, UnitStatNames.Discipline, 0);
			this.MaxCombatMoves = new UnitStat(Game, ID, UnitStatNames.MaxCombatMoves, 0);
			this.Upkeep = new UnitStat(Game, ID, UnitStatNames.Upkeep, 0);
			this.Suppression = new UnitStat(Game, ID, UnitStatNames.Suppression, 0);
			this.CombatMoves = new ActiveStat<float>(0f);
		}

		public WorkingHero(SovereigntyGame Game, BinaryReader r, int SaveVersion)
			: base(Game, r, SaveVersion)
		{
			this.NewHero = r.ReadBoolean();
			this.HeroArt1 = r.ReadString();
			this.HeroArt2 = r.ReadString();
			r.ReadString();
			this.Legendary = r.ReadBoolean();
			this.XP = r.ReadInt32();
			this.LegendaryAbilityName = r.ReadString();
			this.AbilityOption1 = r.ReadString();
			this.AbilityOption2 = r.ReadString();
			this.BaseAbility = r.ReadString();
		}

		internal override void Save(BinaryWriter w)
		{
			base.Save(w);
			w.Write(this.NewHero);
			w.Write(this.HeroArt1);
			w.Write(this.HeroArt2);
			w.Write("");
			w.Write(this.Legendary);
			w.Write(this.XP);
			w.Write(this.LegendaryAbilityName);
			w.Write(this.AbilityOption1);
			w.Write(this.AbilityOption2);
			w.Write(this.BaseAbility);
		}

		public string GetArt(WorkingRealm Realm)
		{
			if (Realm.HeroTypeID == 1)
			{
				return this.HeroArt1;
			}
			return this.HeroArt2;
		}

		internal void RemoveModifiers(WorkingStack Stack)
		{
			Stack.RemoveModifier("HeroDiscipline");
		}

		internal void ApplyModifiers(WorkingStack Stack)
		{
			int num = base.OwnerRealm.HeroDisciplineModifier;
			if (num != 0)
			{
				Stack.ApplyModifier("HeroDiscipline", false, UnitStatNames.Discipline, num);
			}
		}

		public bool NewHero;

		public string HeroArt1;

		public string HeroArt2;

		public string BaseAbility;

		public string AbilityOption1;

		public string AbilityOption2;

		public SpellSchools Panel;

		public string SoundName;

		public string LegendaryAbilityName = "";

		public new int XP;
	}
}
