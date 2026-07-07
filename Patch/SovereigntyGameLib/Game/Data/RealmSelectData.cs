using System;

namespace SovereigntyTK.Game.Data
{
	public class RealmSelectData : BaseData
	{
		[EditorData("Realm", EditorTypes.DropDown)]
		[DataBinding("RlmStats", "Name", false)]
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("realm")]
		public string Name { get; set; }

		[DataName("displaytext")]
		[DataConverter(typeof(TextIndexConverter))]
		[EditorData("Localised Name", EditorTypes.Text)]
		public string Description { get; set; }

		[DataName("player")]
		[EditorData("Playable", EditorTypes.Boolean)]
		[DataConverter(typeof(YesNoConverter))]
		public bool Playable { get; set; }

		[EditorData("Image File 1", EditorTypes.Text)]
		[DataName("portrait1")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string ProvinceImageFile { get; set; }

		[EditorData("Image File 2", EditorTypes.Text)]
		[DataName("portrait2")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string UnitImageFile { get; set; }

		public override string ToString()
		{
			return this.Name;
		}
	}
}
