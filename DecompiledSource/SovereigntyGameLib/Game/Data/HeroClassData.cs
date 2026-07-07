using System;

namespace SovereigntyTK.Game.Data
{
	public class HeroClassData : BaseData
	{
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("class")]
		[EditorData("Name", EditorTypes.Text)]
		public string ClassName { get; set; }

		[EditorData("Localised Name", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("displayname")]
		public string DisplayName { get; set; }

		[EditorData("Starting Ability", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("ability")]
		public string AbilityName { get; set; }

		[EditorData("Promotion Ability 1", EditorTypes.Text)]
		[DataName("upgrade1")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string UpgradeName1 { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("upgrade2")]
		[EditorData("Promotion Ability 2", EditorTypes.Text)]
		public string UpgradeName2 { get; set; }

		[DataName("heroart")]
		[EditorData("Art File", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string ArtName1 { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Art File (alt)", EditorTypes.Text)]
		[DataName("hero2")]
		public string ArtName2 { get; set; }

		[DataName("terrain")]
		[EditorData("Deploy Terrain", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(HeroDeployTypeConverter))]
		public HeroDeployTypes DeployType { get; set; }

		public override string ToString()
		{
			return this.ClassName;
		}
	}
}
