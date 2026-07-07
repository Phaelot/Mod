using System;

namespace SovereigntyTK.Game.Data
{
	public class ResourceNameConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			if (Data == "None")
			{
				return "";
			}
			return Data;
		}

		public override string ConvertToString(object Data)
		{
			if ((string)Data == "")
			{
				return "None";
			}
			return (string)Data;
		}
	}
}
