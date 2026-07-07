using System;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class SpellDurationConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			if (Data.ToLowerInvariant() == "perm")
			{
				return -1;
			}
			int num = 0;
			int.TryParse(Data, out num);
			return num;
		}

		public override string ConvertToString(object Data)
		{
			int num = (int)Data;
			if (num == -1)
			{
				return "perm";
			}
			return num.ToString(CultureInfo.InvariantCulture);
		}
	}
}
