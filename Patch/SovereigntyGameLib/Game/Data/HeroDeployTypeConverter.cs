using System;

namespace SovereigntyTK.Game.Data
{
	public class HeroDeployTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			if (Data.ToLowerInvariant() == "land")
			{
				return HeroDeployTypes.Land;
			}
			if (Data.ToLowerInvariant() == "naval")
			{
				return HeroDeployTypes.Naval;
			}
			throw new Exception("Invalid data in Hero Deploy Type field: " + Data);
		}

		public override string ConvertToString(object Data)
		{
			switch ((HeroDeployTypes)Data)
			{
			case HeroDeployTypes.Land:
				return "land";
			case HeroDeployTypes.Naval:
				return "naval";
			default:
				return "";
			}
		}
	}
}
