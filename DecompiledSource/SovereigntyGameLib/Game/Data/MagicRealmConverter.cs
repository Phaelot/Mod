using System;

namespace SovereigntyTK.Game.Data
{
	public class MagicRealmConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.ParseSchool(Data);
		}

		public override string ConvertToString(object Data)
		{
			return ((SpellSchools)Data).ToString();
		}
	}
}
