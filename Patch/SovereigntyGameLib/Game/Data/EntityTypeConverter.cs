using System;

namespace SovereigntyTK.Game.Data
{
	public class EntityTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			if (Data.ToLowerInvariant() == "single")
			{
				return EntityType.Single;
			}
			if (Data.ToLowerInvariant() == "unit")
			{
				return EntityType.Group;
			}
			throw new Exception("Invalid data in Entity Type field: " + Data);
		}

		public override string ConvertToString(object Data)
		{
			switch ((EntityType)Data)
			{
			case EntityType.Single:
				return "single";
			case EntityType.Group:
				return "unit";
			default:
				return "";
			}
		}
	}
}
