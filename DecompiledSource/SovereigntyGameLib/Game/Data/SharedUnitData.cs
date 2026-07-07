using System;

namespace SovereigntyTK.Game.Data
{
	public class SharedUnitData : BaseData
	{
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Unit Name", EditorTypes.Text)]
		[DataName("name")]
		[PrimaryKey(1)]
		public string Name { get; set; }

		[PrimaryKey(2)]
		[DataName("realmtype")]
		[EditorData("Realm Typr", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(NavalTypeConverter))]
		public NavalType RealmType { get; set; }

		[EditorData("Realm Race", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(RaceConverter))]
		[PrimaryKey(3)]
		[DataName("realmrace")]
		public Races RealmRace { get; set; }

		[EditorData("Localised Name")]
		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displayname")]
		public string DisplayName { get; set; }

		[EditorData("Art Filename")]
		[DataName("art")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string ImageFile { get; set; }

		[DataName("pce")]
		[EditorData("Peace Weight")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int PeaceWeight { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("War Weight")]
		[DataName("war")]
		public int WarWeight { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("sound")]
		[EditorData("Attack Sound")]
		public string SoundFile { get; set; }

		[EditorData("Move Sound")]
		[DataName("movesound")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string MoveSound { get; set; }

		[DataConverter(typeof(ClassConverter))]
		[EditorData("Class", EditorTypes.DropDownEnum)]
		[DataName("class")]
		public UnitClasses Class { get; set; }

		[DataConverter(typeof(RaceConverter))]
		[DataName("race")]
		[EditorData("Race", EditorTypes.DropDownEnum)]
		public Races Race { get; set; }

		[EditorData("Type", EditorTypes.DropDownEnum)]
		[DataName("entity")]
		[DataConverter(typeof(EntityTypeConverter))]
		public EntityType IsSingleEntity { get; set; }

		[DataName("cwork")]
		[EditorData("Value")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Value { get; set; }

		[EditorData("Gold Cost")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("total_cost")]
		public int Cost { get; set; }

		[EditorData("Upkeep Cost")]
		[DataName("total_upkeep")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Upkeep { get; set; }

		[EditorData("Can Purchase", EditorTypes.Boolean)]
		[DataConverter(typeof(YesNoConverter))]
		[DataName("purch")]
		public bool AllowPurchase { get; set; }

		[DataName("trans")]
		[EditorData("Is Transport", EditorTypes.Boolean)]
		[DataConverter(typeof(YesNoConverter))]
		public bool AllowTransport { get; set; }

		[DataName("time")]
		[EditorData("Train Time")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int TrainTime { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("contact")]
		[EditorData("Naval Contact Value")]
		public int ContactValue { get; set; }

		[EditorData("Starting Count")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("purc")]
		public int InitialPurchaseCount { get; set; }

		[DataName("pack")]
		[DataConverter(typeof(YesNoConverter))]
		[EditorData("Can Pack", EditorTypes.Boolean)]
		public bool CanPack { get; set; }

		[DataName("type")]
		[EditorData("Rank", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(RankConverter))]
		public UnitRanks Rank { get; set; }

		[EditorData("Damage Type", EditorTypes.DropDownEnum)]
		[DataName("atttype")]
		[DataConverter(typeof(DamageTypeConverter))]
		public DamageTypes DamageType { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Melee Attack")]
		[DataName("attack")]
		public int Attack { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("defense")]
		[EditorData("Defence")]
		public int Defence { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("move")]
		[EditorData("Combat Moves")]
		public int Move { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("rng_att")]
		[EditorData("Ranged attack")]
		public int RangedAttack { get; set; }

		[EditorData("Range")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("range")]
		public int Range { get; set; }

		[EditorData("Initiative")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("speed")]
		public int Speed { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("morale")]
		[EditorData("Discipline")]
		public int Discipline { get; set; }

		[EditorData("Heal Rate")]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("heal")]
		public int HealRate { get; set; }

		[EditorData("Ability 1")]
		[DataName("special1")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Ability1 { get; set; }

		[DataName("special2")]
		[EditorData("Ability 2")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Ability2 { get; set; }

		[DataName("special3")]
		[EditorData("Ability 3")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Ability3 { get; set; }

		[EditorData("Medal 1 Option 1")]
		[DataName("medal1a")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Medal1A { get; set; }

		[DataName("medal1b")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Medal 1 Option 2")]
		public string Medal1B { get; set; }

		[EditorData("Medal 2 Option 1")]
		[DataName("medal2a")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Medal2A { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Medal 2 Option 2")]
		[DataName("medal2b")]
		public string Medal2B { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Medal 3 Option 1")]
		[DataName("medal3a")]
		public string Medal3A { get; set; }

		[EditorData("Medal 3 Option 2")]
		[DataName("medal3b")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Medal3B { get; set; }

		[EditorData("Medal 4 Option 1")]
		[DataName("medal4a")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Medal4A { get; set; }

		[DataName("medal4b")]
		[EditorData("Medal 4 Option 2")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Medal4B { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Ranged Attack Sound")]
		[DataName("soundranged")]
		public string RangedSoundFile { get; set; }

		[DataName("attackanim")]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataBinding("Animations", "Name", false)]
		[EditorData("Attack Animation", EditorTypes.DropDown)]
		public string AttackAnimation { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataBinding("Resources", "ResourceName", true)]
		[DataName("resource1")]
		[EditorData("Resource 1", EditorTypes.DropDown)]
		public string Resource1Name { get; set; }

		[EditorData("Resource 1 Quantity")]
		[DataName("res1")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Resource1Quantity { get; set; }

		[DataBinding("Resources", "ResourceName", true)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("resource2")]
		[EditorData("Resource 2", EditorTypes.DropDown)]
		public string Resource2Name { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("res2")]
		[EditorData("Resource 2 Quantity")]
		public int Resource2Quantity { get; set; }

		[DataName("unitdesc")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Localised Description", EditorTypes.Text)]
		public string DisplayDesc { get; set; }

		public override string ToString()
		{
			return string.Concat(new object[] { this.RealmType, ".", this.RealmRace, ".", this.Name });
		}
	}
}
