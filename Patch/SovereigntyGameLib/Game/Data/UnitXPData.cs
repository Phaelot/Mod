using System;

namespace SovereigntyTK.Game.Data
{
	public class UnitXPData : BaseData
	{
		[EditorData("Promotion 1", EditorTypes.Text)]
		[DataName("medal1")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Medal1XP { get; set; }

		[DataName("medal2")]
		[EditorData("Promotion 2", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Medal2XP { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("medal3")]
		[EditorData("Promotion 3", EditorTypes.Text)]
		public int Medal3XP { get; set; }

		[DataName("medal4")]
		[EditorData("Promotion 4", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		public int Medal4XP { get; set; }

		public override string ToString()
		{
			return "MedalXP";
		}
	}
}
