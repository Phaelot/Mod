using System;

namespace SovereigntyTK.Game.Data
{
	public class GeneralStringConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return Data;
		}

		public override string ConvertToString(object Data)
		{
			return (string)Data;
		}
	}
}
