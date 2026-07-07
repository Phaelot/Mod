using System;
using System.Collections.Generic;

namespace SovereigntyTK.Game.Data
{
	public class BuildingData : BaseData
	{
		[DataName("name")]
		[PrimaryKey(1)]
		[EditorData("Name", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Name { get; set; }

		[EditorData("Localised Name", EditorTypes.Text)]
		[DataName("displayname")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string DisplayName { get; set; }

		[DataName("displayeffect")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Localised Description", EditorTypes.Text)]
		public string DisplayDesc { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Gold Cost", EditorTypes.Text)]
		[DataName("gold")]
		public int GoldCost { get; set; }

		[DataName("res1")]
		[DataConverter(typeof(GeneralStringConverter))]
		[EditorData("Resource 1", EditorTypes.DropDown)]
		[DataBinding("Resources", "ResourceName", true)]
		public string ResourceName1 { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("res1cost")]
		[EditorData("Resource 1 Cost", EditorTypes.Text)]
		public int ResourceCost1 { get; set; }

		[DataBinding("Resources", "ResourceName", true)]
		[EditorData("Resource 2", EditorTypes.DropDown)]
		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("res2")]
		public string ResourceName2 { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("res2cost")]
		[EditorData("Resource 2 Cost", EditorTypes.Text)]
		public int ResourceCost2 { get; set; }

		[DataName("tier")]
		[EditorData("Tier", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Tier { get; set; }

		[DataBinding("Buildings", "Name", true)]
		[DataName("prereq")]
		[DataConverter(typeof(StringListConverter))]
		[EditorData("Required Buildings", EditorTypes.List)]
		public List<string> RequiredBuildings { get; set; }

		[EditorData("Build Limit", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("number")]
		public int MaxNumber { get; set; }

		[DataName("art")]
		[EditorData("Image File", EditorTypes.Text)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string ArtName { get; set; }

		public Dictionary<string, int> ResourceCosts
		{
			get
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				if (this.ResourceName1 != null && this.ResourceName1.Length > 0)
				{
					dictionary.Add(this.ResourceName1, this.ResourceCost1);
				}
				if (this.ResourceName2 != null && this.ResourceName2.Length > 0)
				{
					dictionary.Add(this.ResourceName2, this.ResourceCost2);
				}
				return dictionary;
			}
		}

		public override string ToString()
		{
			return this.Name;
		}
	}
}
