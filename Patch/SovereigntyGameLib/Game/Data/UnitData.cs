using System;
using System.Collections.Generic;

namespace SovereigntyTK.Game.Data
{
	public class UnitData : BaseData
	{
		[EditorData("Unit Name")]
		[PrimaryKey(2)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("name")]
		public string Name { get; set; }

		[DataName("realm")]
		[DataBinding("RlmStats", "Name", false)]
		[EditorData("Realm", EditorTypes.DropDown)]
		[DataConverter(typeof(GeneralStringConverter))]
		[PrimaryKey(1)]
		public string Realm { get; set; }

		[EditorData("Localised Name")]
		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displayname")]
		public string DisplayName { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Formation Value")]
		[DataName("form")]
		public int Formation { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Art Filename")]
		[DataName("art")]
		public string ImageFile { get; set; }

		[EditorData("Peace Weight")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("pce")]
		public int PeaceWeight { get; set; }

		[EditorData("War Weight")]
		[DataName("war")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int WarWeight { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("sound")]
		[EditorData("Attack Sound")]
		public string SoundFile { get; set; }

		[DataName("movesound")]
		[EditorData("Move Sound")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string MoveSound { get; set; }

		[DataName("class")]
		[DataConverter(typeof(ClassConverter))]
		[EditorData("Class", EditorTypes.DropDownEnum)]
		public UnitClasses Class { get; set; }

		[EditorData("Race", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(RaceConverter))]
		[DataName("race")]
		public Races Race { get; set; }

		[DataName("entity")]
		[DataConverter(typeof(EntityTypeConverter))]
		[EditorData("Type", EditorTypes.DropDownEnum)]
		public EntityType IsSingleEntity { get; set; }

		[EditorData("Value")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("cwork")]
		public int Value { get; set; }

		[DataName("total_cost")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Gold Cost")]
		public int Cost { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Upkeep Cost")]
		[DataName("total_upkeep")]
		public int Upkeep { get; set; }

		[EditorData("Can Purchase", EditorTypes.Boolean)]
		[DataConverter(typeof(YesNoConverter))]
		[DataName("purch")]
		public bool AllowPurchase { get; set; }

		[EditorData("Is Transport", EditorTypes.Boolean)]
		[DataConverter(typeof(YesNoConverter))]
		[DataName("trans")]
		public bool AllowTransport { get; set; }

		[EditorData("Train Time")]
		[DataName("time")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int TrainTime { get; set; }

		[DataName("contact")]
		[EditorData("Naval Contact Value")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int ContactValue { get; set; }

		[EditorData("Starting Count")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("purc")]
		public int InitialPurchaseCount { get; set; }

		[EditorData("Can Pack", EditorTypes.Boolean)]
		[DataName("pack")]
		[DataConverter(typeof(YesNoConverter))]
		public bool CanPack { get; set; }

		[EditorData("Rank", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(RankConverter))]
		[DataName("type")]
		public UnitRanks Rank { get; set; }

		[EditorData("Damage Type", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(DamageTypeConverter))]
		[DataName("atttype")]
		public DamageTypes DamageType { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("attack")]
		[EditorData("Melee Attack")]
		public int Attack { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Defence")]
		[DataName("defense")]
		public int Defence { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Combat Moves")]
		[DataName("move")]
		public int Move { get; set; }

		[EditorData("Ranged attack")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("rng_att")]
		public int RangedAttack { get; set; }

		[EditorData("Range")]
		[DataName("range")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Range { get; set; }

		[EditorData("Initiative")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("speed")]
		public int Speed { get; set; }

		[DataName("morale")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Discipline")]
		public int Discipline { get; set; }

		[DataName("heal")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Heal Rate")]
		public int HealRate { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Ability 1")]
		[DataName("special1")]
		public string Ability1 { get; set; }

		[EditorData("Ability 2")]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("special2")]
		public string Ability2 { get; set; }

		[DataName("special3")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Ability 3")]
		public string Ability3 { get; set; }

		[DataName("medal1a")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Medal 1 Option 1")]
		public string Medal1A { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("medal1b")]
		[EditorData("Medal 1 Option 2")]
		public string Medal1B { get; set; }

		[EditorData("Medal 2 Option 1")]
		[DataName("medal2a")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Medal2A { get; set; }

		[EditorData("Medal 2 Option 2")]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("medal2b")]
		public string Medal2B { get; set; }

		[DataName("medal3a")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Medal 3 Option 1")]
		public string Medal3A { get; set; }

		[DataName("medal3b")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Medal 3 Option 2")]
		public string Medal3B { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Medal 4 Option 1")]
		[DataName("medal4a")]
		public string Medal4A { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("medal4b")]
		[EditorData("Medal 4 Option 2")]
		public string Medal4B { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("soundranged")]
		[EditorData("Ranged Attack Sound")]
		public string RangedSoundFile { get; set; }

		[DataName("attackanim")]
		[EditorData("Attack Animation", EditorTypes.DropDown)]
		[DataBinding("Animations", "Name", false)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string AttackAnimation { get; set; }

		[DataName("resource1")]
		[DataBinding("Resources", "ResourceName", true)]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Resource 1", EditorTypes.DropDown)]
		public string Resource1Name { get; set; }

		[EditorData("Resource 1 Quantity")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("res1")]
		public int Resource1Quantity { get; set; }

		[DataName("resource2")]
		[EditorData("Resource 2", EditorTypes.DropDown)]
		[DataBinding("Resources", "ResourceName", true)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Resource2Name { get; set; }

		[DataName("res2")]
		[EditorData("Resource 2 Quantity")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Resource2Quantity { get; set; }

		[DataName("unitdesc")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Localised Description", EditorTypes.Text)]
		public string DisplayDesc { get; set; }

		public Dictionary<string, int> GetRequiredResources()
		{
			if (this.m_RequiredResources == null)
			{
				this.m_RequiredResources = new Dictionary<string, int>();
				if (this.Resource1Quantity > 0 && this.Resource1Name.ToLowerInvariant() != "none")
				{
					this.m_RequiredResources.Add(this.Resource1Name, this.Resource1Quantity);
				}
				if (this.Resource2Quantity > 0 && this.Resource2Name.ToLowerInvariant() != "none")
				{
					this.m_RequiredResources.Add(this.Resource2Name, this.Resource2Quantity);
				}
			}
			return this.m_RequiredResources;
		}

		public List<string> FirstMedals
		{
			get
			{
				List<string> list = new List<string>();
				if (this.Medal1A.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal1A);
				}
				if (this.Medal2A.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal2A);
				}
				if (this.Medal3A.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal3A);
				}
				if (this.Medal4A.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal4A);
				}
				return list;
			}
		}

		public List<string> SecondMedals
		{
			get
			{
				List<string> list = new List<string>();
				if (this.Medal1B.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal1B);
				}
				if (this.Medal2B.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal2B);
				}
				if (this.Medal3B.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal3B);
				}
				if (this.Medal4B.ToLowerInvariant() != "none")
				{
					list.Add(this.Medal4B);
				}
				return list;
			}
		}

		public event FlagsDelegate OnRequestDeployPoolFlags;

		public UnitData(SharedUnitData CloneData, string Realm)
		{
			this.Name = CloneData.Name;
			this.DisplayName = CloneData.DisplayName;
			this.ImageFile = CloneData.ImageFile;
			this.Realm = Realm;
			this.PeaceWeight = CloneData.PeaceWeight;
			this.WarWeight = CloneData.WarWeight;
			this.SoundFile = CloneData.SoundFile;
			this.RangedSoundFile = CloneData.RangedSoundFile;
			this.Class = CloneData.Class;
			this.Race = CloneData.Race;
			this.IsSingleEntity = CloneData.IsSingleEntity;
			this.Cost = CloneData.Cost;
			this.Upkeep = CloneData.Upkeep;
			this.TrainTime = CloneData.TrainTime;
			this.Rank = CloneData.Rank;
			this.DamageType = CloneData.DamageType;
			this.Attack = CloneData.Attack;
			this.Defence = CloneData.Defence;
			this.RangedAttack = CloneData.RangedAttack;
			this.Speed = CloneData.Speed;
			this.Discipline = CloneData.Discipline;
			this.HealRate = CloneData.HealRate;
			this.Move = CloneData.Move;
			this.Range = CloneData.Range;
			this.MoveSound = CloneData.MoveSound;
			this.AllowPurchase = CloneData.AllowPurchase;
			this.ContactValue = CloneData.ContactValue;
			this.AttackAnimation = CloneData.AttackAnimation;
			this.Value = CloneData.Value;
			this.InitialPurchaseCount = CloneData.InitialPurchaseCount;
			this.CanPack = CloneData.CanPack;
			this.AllowTransport = CloneData.AllowTransport;
			this.DisplayDesc = CloneData.DisplayDesc;
			this.Medal1A = CloneData.Medal1A;
			this.Medal1B = CloneData.Medal1B;
			this.Medal2A = CloneData.Medal2A;
			this.Medal2B = CloneData.Medal2B;
			this.Medal3A = CloneData.Medal3A;
			this.Medal3B = CloneData.Medal3B;
			this.Medal4A = CloneData.Medal4A;
			this.Medal4B = CloneData.Medal4B;
			this.Ability1 = CloneData.Ability1;
			this.Ability2 = CloneData.Ability2;
			this.Ability3 = CloneData.Ability3;
			this.Resource1Name = CloneData.Resource1Name;
			this.Resource1Quantity = CloneData.Resource1Quantity;
			this.Resource2Name = CloneData.Resource2Name;
			this.Resource2Quantity = CloneData.Resource2Quantity;
		}

		public UnitData()
		{
		}

		public override string ToString()
		{
			return this.Realm + "." + this.Name;
		}

		public virtual List<string> Abilities
		{
			get
			{
				List<string> list = new List<string>();
				if (this.Ability1 != null && this.Ability1.Length > 0 && this.Ability1.ToLowerInvariant() != "none")
				{
					list.Add(this.Ability1);
				}
				if (this.Ability2 != null && this.Ability2.Length > 0 && this.Ability2.ToLowerInvariant() != "none")
				{
					list.Add(this.Ability2);
				}
				if (this.Ability3 != null && this.Ability3.Length > 0 && this.Ability3.ToLowerInvariant() != "none")
				{
					list.Add(this.Ability3);
				}
				if (this.Class == UnitClasses.Irregular)
				{
					list.Add("HitAndRun");
					list.Add("SkirmishersScreen");
				}
				if (this.OnRequestDeployPoolFlags != null)
				{
					list.AddRange(this.OnRequestDeployPoolFlags(this));
				}
				return list;
			}
		}

		public static string GetRankName(UnitRanks Rank)
		{
			switch (Rank)
			{
			case UnitRanks.Standard:
				return "Standard";
			case UnitRanks.Elite:
				return "Elite";
			case UnitRanks.Unique:
				return "Unique";
			case UnitRanks.Mercenary:
				return "Mercenary";
			default:
				return "Standard";
			}
		}

		public int GetUnitWeight(WarMode EconState)
		{
			switch (EconState)
			{
			case WarMode.Peace:
				return this.PeaceWeight;
			case WarMode.War:
				return this.WarWeight;
			}
			return 0;
		}

		public static string GetClassNameKey(UnitClasses Class)
		{
			switch (Class)
			{
			case UnitClasses.Infantry:
				return "CLASS_INFANTRY";
			case UnitClasses.Cavalry:
				return "CLASS_CAVALRY";
			case UnitClasses.Archer:
				return "CLASS_ARCHER";
			case UnitClasses.Siege:
				return "CLASS_SIEGE";
			case UnitClasses.Naval:
				return "CLASS_NAVAL";
			case UnitClasses.Fort:
				return "CLASS_FORT";
			case UnitClasses.Wagon:
				return "CLASS_WAGON";
			default:
				return "CLASS_MISSING";
			}
		}

		public static string GetClassNameValue(UnitClasses Class)
		{
			switch (Class)
			{
			case UnitClasses.Infantry:
				return "Infantry";
			case UnitClasses.Cavalry:
				return "Cavalry";
			case UnitClasses.Archer:
				return "Archer";
			case UnitClasses.Siege:
				return "Siege";
			case UnitClasses.Naval:
				return "Naval";
			case UnitClasses.Fort:
				return "Fort";
			case UnitClasses.Wagon:
				return "Transport";
			default:
				return "CLASS_MISSING";
			}
		}

		internal int GetRequiredResourceCount(ResourceData Resource)
		{
			if (this.Resource1Quantity > 0 && this.Resource1Name.ToLowerInvariant() == Resource.ResourceName.ToLowerInvariant())
			{
				return this.Resource1Quantity;
			}
			if (this.Resource2Quantity > 0 && this.Resource2Name.ToLowerInvariant() == Resource.ResourceName.ToLowerInvariant())
			{
				return this.Resource2Quantity;
			}
			return 0;
		}

		private Dictionary<string, int> m_RequiredResources;
	}
}
