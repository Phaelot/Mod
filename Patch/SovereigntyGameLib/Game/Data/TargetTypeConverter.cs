using System;

namespace SovereigntyTK.Game.Data
{
	public class TargetTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.ParseTargetType(Data);
		}

		public override string ConvertToString(object Data)
		{
			return DataConverters.GetTargetTypeName((SpellTargets)Data);
		}
	}
}
