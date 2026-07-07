using System;

namespace SovereigntyTK.Game.Data
{
	public class WaterTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			if (Data != null)
			{
				if (Data == "0")
				{
					return WaterTypes.NoWater;
				}
				if (Data == "1")
				{
					return WaterTypes.Water;
				}
				if (Data == "3")
				{
					return WaterTypes.Harbour;
				}
			}
			throw new Exception("Invalid data in Water Type field: " + Data);
		}

		public override string ConvertToString(object Data)
		{
			return ((WaterTypes)Data).ToString();
		}
	}
}
