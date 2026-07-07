using System;

namespace SovereigntyTK.Game.Data
{
	public class StackModeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.GetStackModeByName(Data);
		}

		public override string ConvertToString(object Data)
		{
			return ((DiplomaticStackModes)Data).ToString();
		}
	}
}
