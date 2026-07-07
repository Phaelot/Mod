using System;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class MagicLevelData : BaseData
	{
		[DataConverter(typeof(GeneralIntConverter))]
		[PrimaryKey(1)]
		[DataName("lvl")]
		[EditorData("Level", EditorTypes.Text)]
		public int Level { get; set; }

		[EditorData("XP Needed", EditorTypes.Text)]
		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("xp")]
		public int XP { get; set; }

		public override string ToString()
		{
			return this.Level.ToString(CultureInfo.InvariantCulture);
		}
	}
}
