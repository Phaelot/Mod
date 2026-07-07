using System;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class GeneralIntConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			int num = 0;
			int.TryParse(Data, out num);
			return num;
		}

		public override string ConvertToString(object Data)
		{
			return ((int)Data).ToString(CultureInfo.InvariantCulture);
		}
	}
}
