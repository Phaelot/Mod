using System;

namespace SovereigntyTK.Game.Data
{
	public class DamageTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.GetDamageTypeByName(Data);
		}

		public override string ConvertToString(object Data)
		{
			return ((DamageTypes)Data).ToString();
		}
	}
}
