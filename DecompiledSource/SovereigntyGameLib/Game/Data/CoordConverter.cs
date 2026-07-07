using System;
using System.Drawing;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class CoordConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			Point empty = Point.Empty;
			string[] array = Data.Split(new char[] { ',' });
			if (array.Length == 2)
			{
				int num = 0;
				int.TryParse(array[0], out num);
				empty.X = num;
				num = 0;
				int.TryParse(array[1], out num);
				empty.Y = num;
			}
			return empty;
		}

		public override string ConvertToString(object Data)
		{
			Point point = (Point)Data;
			return point.X.ToString(CultureInfo.InvariantCulture) + "," + point.Y.ToString(CultureInfo.InvariantCulture);
		}
	}
}
