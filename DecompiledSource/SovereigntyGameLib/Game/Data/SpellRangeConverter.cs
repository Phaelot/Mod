using System;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class SpellRangeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			if (Data.ToLowerInvariant() == "infinite")
			{
				return 1000;
			}
			int num = 0;
			int.TryParse(Data, out num);
			return num;
		}

		public override string ConvertToString(object Data)
		{
			int num = (int)Data;
			if (num >= 1000)
			{
				return "infinite";
			}
			return num.ToString(CultureInfo.InvariantCulture);
		}
	}
}
