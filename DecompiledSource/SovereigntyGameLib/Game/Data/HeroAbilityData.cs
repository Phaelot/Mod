using System;

namespace SovereigntyTK.Game.Data
{
	public class HeroAbilityData : BaseData
	{
		[EditorData("Name", EditorTypes.Text)]
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("ability")]
		public string AbilityName { get; set; }

		[DataName("displayname")]
		[EditorData("Localised Name", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string DisplayName { get; set; }

		[EditorData("Localised Description", EditorTypes.Text)]
		[DataName("cardeffname")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string DisplayDesc { get; set; }

		[DataName("color")]
		[EditorData("Magic School", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(MagicRealmConverter))]
		public SpellSchools School { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Sound Effect", EditorTypes.Text)]
		[DataName("sound")]
		public string SoundFile { get; set; }

		public override string ToString()
		{
			return this.AbilityName;
		}
	}
}
