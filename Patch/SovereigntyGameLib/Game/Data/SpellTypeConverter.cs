using System;

namespace SovereigntyTK.Game.Data
{
	public class SpellTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.ParseSpellType(Data);
		}

		public override string ConvertToString(object Data)
		{
			return DataConverters.GetSpellTypeName((SpellTypes)Data);
		}
	}
}
