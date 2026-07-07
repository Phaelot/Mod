using System;
using System.Drawing;
using System.Globalization;

namespace SovereigntyTK.Utility
{
	public static class ConversionUtils
	{
		public static float ConvertXMLFloat(string BrokenNumber)
		{
			float num = 0f;
			if (BrokenNumber.EndsWith("e+"))
			{
				BrokenNumber += "0";
			}
			float.TryParse(BrokenNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
			return num;
		}

		public static Color ParseColour(string s)
		{
			string[] array = s.Split(new char[] { ',' });
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int.TryParse(array[0], out num);
			int.TryParse(array[1], out num2);
			int.TryParse(array[2], out num3);
			return Color.FromArgb(num, num2, num3);
		}
	}
}
