using System;

namespace SovereigntyTK.Game.Data
{
	public class NavalTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.GetNavalClass(Data);
		}

		public override string ConvertToString(object Data)
		{
			return ((NavalType)Data).ToString();
		}
	}
}
