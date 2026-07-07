using System;

namespace SovereigntyTK.Game.Data
{
	public class ClassConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.GetClassByName(Data);
		}

		public override string ConvertToString(object Data)
		{
			return DataConverters.GetClassName((UnitClasses)Data);
		}
	}
}
