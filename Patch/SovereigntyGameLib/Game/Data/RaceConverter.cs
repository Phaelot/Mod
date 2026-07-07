using System;

namespace SovereigntyTK.Game.Data
{
	public class RaceConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.GetRaceByName(Data);
		}

		public override string ConvertToString(object Data)
		{
			return ((Races)Data).ToString();
		}
	}
}
