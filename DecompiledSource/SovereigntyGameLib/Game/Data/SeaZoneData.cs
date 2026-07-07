using System;

namespace SovereigntyTK.Game.Data
{
	public class SeaZoneData : BaseData
	{
		[EditorData("Region ID", EditorTypes.Text)]
		[PrimaryKey(1)]
		[DataName("provinceid")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int ID { get; set; }

		[EditorData("Name", EditorTypes.Text)]
		[PrimaryKey(2)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("name")]
		public string Name { get; set; }

		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displayname")]
		[EditorData("Localised Name", EditorTypes.Text)]
		public string DisplayName { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Zone ID", EditorTypes.Text)]
		[DataName("zone")]
		public int ZoneID { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("art")]
		[EditorData("Image File", EditorTypes.Text)]
		public string ArtName { get; set; }

		public override string ToString()
		{
			return this.Name;
		}

		public SeaZoneData()
		{
			this.Terrain = "Sea";
		}

		public string Terrain;

		public ProvinceOutlineData Outline;
	}
}
