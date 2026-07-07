using System;
using System.Drawing;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class GameColorConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			string[] array = Data.Split(new char[] { ',' });
			if (array.Length != 3)
			{
				throw new Exception("Error in Colour format");
			}
			int num = int.Parse(array[0]);
			int num2 = int.Parse(array[1]);
			int num3 = int.Parse(array[2]);
			return Color.FromArgb((int)((byte)num), (int)((byte)num2), (int)((byte)num3));
		}

		public override string ConvertToString(object Data)
		{
			Color color = (Color)Data;
			return string.Concat(new string[]
			{
				color.R.ToString(CultureInfo.InvariantCulture),
				",",
				color.G.ToString(CultureInfo.InvariantCulture),
				",",
				color.B.ToString(CultureInfo.InvariantCulture)
			});
		}
	}
}
