using System;

namespace SovereigntyTK.Game.Data
{
	public class DataConverters
	{
		public static SpellTypes ParseSpellType(string TypeName)
		{
			string text;
			if ((text = TypeName.ToLowerInvariant()) != null)
			{
				if (text == "pos")
				{
					return SpellTypes.Positive;
				}
				if (text == "neg")
				{
					return SpellTypes.Negative;
				}
				if (text == "neut")
				{
					return SpellTypes.Neutral;
				}
			}
			throw new Exception("Invalid data in Spell Type field: " + TypeName);
		}

		public static string GetSpellTypeName(SpellTypes Type)
		{
			switch (Type)
			{
			case SpellTypes.Positive:
				return "pos";
			case SpellTypes.Negative:
				return "neg";
			case SpellTypes.Neutral:
				return "neut";
			default:
				return "";
			}
		}

		public static SpellTargets ParseTargetType(string Target)
		{
			string text;
			switch (text = Target.ToLowerInvariant())
			{
			case "unit":
				return SpellTargets.Unit;
			case "stack":
				return SpellTargets.Stack;
			case "sea zone":
				return SpellTargets.SeaZone;
			case "realm":
				return SpellTargets.Realm;
			case "province":
				return SpellTargets.Province;
			case "none":
				return SpellTargets.None;
			}
			throw new Exception("Invalid data in Spell Target field: " + Target);
		}

		public static string GetTargetTypeName(SpellTargets Target)
		{
			switch (Target)
			{
			case SpellTargets.Unit:
				return "unit";
			case SpellTargets.Province:
				return "province";
			case SpellTargets.Realm:
				return "realm";
			case SpellTargets.SeaZone:
				return "sea zone";
			case SpellTargets.None:
				return "none";
			case SpellTargets.Stack:
				return "stack";
			default:
				return "";
			}
		}

		public static NavalType GetNavalClass(string NavalString)
		{
			string text;
			switch (text = NavalString.ToLowerInvariant())
			{
			case "civilized":
				return NavalType.Civilized;
			case "pirate":
				return NavalType.Pirate;
			case "elf":
				return NavalType.Elf;
			case "orc":
				return NavalType.Orc;
			case "dwarf":
				return NavalType.Dwarf;
			case "undead":
				return NavalType.Undead;
			case "barbarian":
				return NavalType.Barbarian;
			case "giant":
				return NavalType.Giant;
			case "all":
				return NavalType.All;
			}
			throw new Exception("Invalid data in Naval Type field: " + NavalString);
		}

		public static RealmAlignments ParseAlignmentString(string AlignString)
		{
			string text;
			if ((text = AlignString.ToLowerInvariant()) != null)
			{
				if (text == "good")
				{
					return RealmAlignments.Good;
				}
				if (text == "neutral")
				{
					return RealmAlignments.Neutral;
				}
				if (text == "evil")
				{
					return RealmAlignments.Evil;
				}
			}
			throw new Exception("Invalid data in Alignment field: " + AlignString);
		}

		public static SpellSchools ParseSchool(string SchoolName)
		{
			string text;
			if ((text = SchoolName.ToLowerInvariant()) != null)
			{
				if (text == "death")
				{
					return SpellSchools.Death;
				}
				if (text == "illusion")
				{
					return SpellSchools.Illusion;
				}
				if (text == "nature")
				{
					return SpellSchools.Nature;
				}
				if (text == "war")
				{
					return SpellSchools.War;
				}
			}
			throw new Exception("Invalid data in Magic Realm field: " + SchoolName);
		}

		public static ProvinceTypes GetProvinceTypeByName(string TypeName)
		{
			string text;
			if ((text = TypeName.ToLowerInvariant()) != null)
			{
				if (text == "standard")
				{
					return ProvinceTypes.Normal;
				}
				if (text == "capitol")
				{
					return ProvinceTypes.Capitol;
				}
			}
			throw new Exception("Invalid data in Province Type field: " + TypeName);
		}

		public static string GetProvinceTypeName(ProvinceTypes Type)
		{
			switch (Type)
			{
			case ProvinceTypes.Normal:
				return "normal";
			case ProvinceTypes.Capitol:
				return "capitol";
			default:
				return "";
			}
		}

		public static DiplomaticStackModes GetStackModeByName(string Mode)
		{
			string text;
			if ((text = Mode.ToLowerInvariant()) != null)
			{
				if (text == "ignore")
				{
					return DiplomaticStackModes.Ignore;
				}
				if (text == "refresh")
				{
					return DiplomaticStackModes.Refresh;
				}
				if (text == "stack")
				{
					return DiplomaticStackModes.Stack;
				}
			}
			throw new Exception("Invalid data in Stack Mode field: " + Mode);
		}

		public static DamageTypes GetDamageTypeByName(string DamageTypeRaw)
		{
			string text;
			if ((text = DamageTypeRaw.ToLowerInvariant()) != null)
			{
				if (text == "death")
				{
					return DamageTypes.Death;
				}
				if (text == "war")
				{
					return DamageTypes.War;
				}
				if (text == "nature")
				{
					return DamageTypes.Nature;
				}
				if (text == "illusion")
				{
					return DamageTypes.Illusion;
				}
				if (text == "physical")
				{
					return DamageTypes.Physical;
				}
			}
			throw new Exception("Invalid data in Damage Type field: " + DamageTypeRaw);
		}

		public static Races GetRaceByName(string RaceRaw)
		{
			string text;
			switch (text = RaceRaw.ToLowerInvariant())
			{
			case "human":
				return Races.Human;
			case "outcast":
				return Races.Outcast;
			case "elf":
				return Races.Elf;
			case "orc":
				return Races.Orc;
			case "dwarf":
				return Races.Dwarf;
			case "undead":
				return Races.Undead;
			case "monster":
				return Races.Monster;
			case "dragon":
				return Races.Dragon;
			case "giant":
				return Races.Giant;
			case "none":
				return Races.None;
			}
			throw new Exception("Invalid data in Race field: " + RaceRaw);
		}

		public static UnitRanks GetRankByName(string RankRaw)
		{
			string text;
			if ((text = RankRaw.ToLowerInvariant()) != null)
			{
				if (text == "elite")
				{
					return UnitRanks.Elite;
				}
				if (text == "merc")
				{
					return UnitRanks.Mercenary;
				}
				if (text == "standard")
				{
					return UnitRanks.Standard;
				}
				if (text == "unique")
				{
					return UnitRanks.Unique;
				}
			}
			throw new Exception("Invalid data in Rank field: " + RankRaw);
		}

		public static string GetRankName(UnitRanks Rank)
		{
			switch (Rank)
			{
			case UnitRanks.Standard:
				return "standard";
			case UnitRanks.Elite:
				return "elite";
			case UnitRanks.Unique:
				return "unique";
			case UnitRanks.Mercenary:
				return "merc";
			default:
				return "";
			}
		}

		public static UnitClasses GetClassByName(string ClassRaw)
		{
			string text;
			switch (text = ClassRaw.ToLowerInvariant())
			{
			case "infantry":
				return UnitClasses.Infantry;
			case "archer":
				return UnitClasses.Archer;
			case "cavalry":
				return UnitClasses.Cavalry;
			case "siege":
				return UnitClasses.Siege;
			case "naval":
				return UnitClasses.Naval;
			case "fort":
				return UnitClasses.Fort;
			case "transport":
				return UnitClasses.Wagon;
			case "irregular":
				return UnitClasses.Irregular;
			}
			throw new Exception("Invalid data in Class field: " + ClassRaw);
		}

		public static string GetClassName(UnitClasses Class)
		{
			switch (Class)
			{
			case UnitClasses.Infantry:
				return "infantry";
			case UnitClasses.Cavalry:
				return "cavalry";
			case UnitClasses.Archer:
				return "archer";
			case UnitClasses.Siege:
				return "siege";
			case UnitClasses.Naval:
				return "naval";
			case UnitClasses.Fort:
				return "fort";
			case UnitClasses.Wagon:
				return "transport";
			case UnitClasses.Irregular:
				return "irregular";
			default:
				return "";
			}
		}
	}
}
