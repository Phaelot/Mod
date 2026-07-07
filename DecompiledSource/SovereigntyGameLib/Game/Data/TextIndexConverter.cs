using System;

namespace SovereigntyTK.Game.Data
{
	public class TextIndexConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return Data.ToUpperInvariant();
		}

		public override string ConvertToString(object Data)
		{
			return (string)Data;
		}
	}
}
