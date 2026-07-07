using System;
using System.Collections.Generic;
using System.Drawing;

namespace SovereigntyTK.Game.Data
{
	public class RealmData : BaseData
	{
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Name", EditorTypes.Text)]
		[DataName("realm")]
		public string Name { get; set; }

		[DataName("displayrealm")]
		[EditorData("Localised Name", EditorTypes.Text)]
		[DataConverter(typeof(TextIndexConverter))]
		public string DisplayName { get; set; }

		[DataName("counter")]
		[EditorData("Army Counter", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string CounterFilename { get; set; }

		[DataName("flag")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Flag", EditorTypes.Text)]
		public string FlagFilename { get; set; }

		[DataConverter(typeof(YesNoConverter))]
		[EditorData("Civilized", EditorTypes.Boolean)]
		[DataName("code")]
		public bool CodeOfWar { get; set; }

		[DataConverter(typeof(AlignmentConverter))]
		[DataName("align")]
		[EditorData("Alignment", EditorTypes.DropDownEnum)]
		public RealmAlignments Alignment { get; set; }

		[EditorData("Race", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(RaceConverter))]
		[DataName("race")]
		public Races Race { get; set; }

		[DataName("realmtype")]
		[DataConverter(typeof(NavalTypeConverter))]
		[EditorData("Realm Type", EditorTypes.DropDownEnum)]
		public NavalType RealmType { get; set; }

		[DataName("economy")]
		[EditorData("Economy Type", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(RaceConverter))]
		public Races EconomyRace { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("science")]
		[EditorData("Cradle of Science Value", EditorTypes.Text)]
		public int ScienceValue { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Siegecraft Value", EditorTypes.Text)]
		[DataName("siege")]
		public int Science_SiegecraftValue { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("engine")]
		[EditorData("Engineering Value", EditorTypes.Text)]
		public int Science_EngineeringValue { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("metal")]
		[EditorData("Metallurgy Value", EditorTypes.Text)]
		public int Science_MetallurgyValue { get; set; }

		[DataName("alch")]
		[EditorData("Alchemy Value", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Science_AlchemyValue { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Patron of Arts Value", EditorTypes.Text)]
		[DataName("arts")]
		public int ArtsValue { get; set; }

		[EditorData("Public Art Value", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("public")]
		public int Arts_PublicValue { get; set; }

		[EditorData("Medicine Value", EditorTypes.Text)]
		[DataName("medic")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Arts_MedicineValue { get; set; }

		[EditorData("Statecraft Value", EditorTypes.Text)]
		[DataName("state")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Arts_StatecraftValue { get; set; }

		[DataName("panel")]
		[DataConverter(typeof(MagicRealmConverter))]
		[EditorData("Magic School", EditorTypes.DropDownEnum)]
		public SpellSchools Panel { get; set; }

		[DataName("magic")]
		[EditorData("Initial Magic Points", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int MagicValue { get; set; }

		[DataName("hero")]
		[EditorData("Maximum Heroes", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int HeroValue { get; set; }

		[DataName("gold")]
		[EditorData("Initial Gold", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int StartingGold { get; set; }

		[DataName("minimap")]
		[DataConverter(typeof(GameColorConverter))]
		[EditorData("Political Map Colour", EditorTypes.Colour)]
		public Color MinimapColour { get; set; }

		[DataBinding("HeroClass", "ClassName", false)]
		[DataName("hero1")]
		[EditorData("Hero Type 1", EditorTypes.DropDown)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string HeroType1 { get; set; }

		[DataBinding("HeroClass", "ClassName", false)]
		[DataName("hero2")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Hero Type 2", EditorTypes.DropDown)]
		public string HeroType2 { get; set; }

		[DataName("hero3")]
		[EditorData("Hero Type 3", EditorTypes.DropDown)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataBinding("HeroClass", "ClassName", false)]
		public string HeroType3 { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("str")]
		[EditorData("Strength", EditorTypes.Text)]
		public int Strength { get; set; }

		[DataName("heroart")]
		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Hero Art Number", EditorTypes.Text)]
		public int HeroTypeID { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Maximum Agents", EditorTypes.Text)]
		[DataName("spy")]
		public int SpyCount { get; set; }

		[EditorData("Music Type", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("music")]
		public string MusicRealm { get; set; }

		[DataName("ally")]
		[EditorData("Maximum Allies", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int AllyValue { get; set; }

		[DataName("allowundead")]
		[DataConverter(typeof(YesNoConverter))]
		[EditorData("Allow Undead Prisoners", EditorTypes.Boolean)]
		public bool AllowUndead { get; set; }

		public override string ToString()
		{
			return this.Name;
		}

		public List<string> HeroClasses
		{
			get
			{
				return new List<string> { this.HeroType1, this.HeroType2, this.HeroType3 };
			}
		}
	}
}
