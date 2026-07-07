using System;

namespace SovereigntyTK.Game.Data
{
	public class YesNoConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			Data = Data.ToLowerInvariant();
			if (Data == "yes")
			{
				return true;
			}
			if (Data == "no")
			{
				return false;
			}
			if (Data == "")
			{
				return false;
			}
			throw new Exception("Invalid data in Yes/No field: " + Data);
		}

		public override string ConvertToString(object Data)
		{
			if ((bool)Data)
			{
				return "yes";
			}
			return "no";
		}
	}
}
