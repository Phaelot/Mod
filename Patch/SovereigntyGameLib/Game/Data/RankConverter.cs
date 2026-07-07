using System;

namespace SovereigntyTK.Game.Data
{
	public class RankConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.GetRankByName(Data);
		}

		public override string ConvertToString(object Data)
		{
			return DataConverters.GetRankName((UnitRanks)Data);
		}
	}
}
