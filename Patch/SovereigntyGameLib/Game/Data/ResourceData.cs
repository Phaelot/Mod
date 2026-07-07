using System;

namespace SovereigntyTK.Game.Data
{
	public class ResourceData : BaseData
	{
		[DataName("resource")]
		[EditorData("Name", EditorTypes.Text)]
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string ResourceName { get; set; }

		[EditorData("Localised Name", EditorTypes.Text)]
		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displayresource")]
		public string DisplayName { get; set; }

		[DataConverter(typeof(TextIndexConverter))]
		[EditorData("Localised Description", EditorTypes.Text)]
		[DataName("displayrsceffect")]
		public string DisplayEffect { get; set; }

		public override string ToString()
		{
			return this.ResourceName;
		}
	}
}
