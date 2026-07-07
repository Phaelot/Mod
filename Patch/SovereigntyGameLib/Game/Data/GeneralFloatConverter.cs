using System;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class GeneralFloatConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			float num = 0f;
			float.TryParse(Data, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
			return num;
		}

		public override string ConvertToString(object Data)
		{
			return ((float)Data).ToString(CultureInfo.InvariantCulture);
		}
	}
}
