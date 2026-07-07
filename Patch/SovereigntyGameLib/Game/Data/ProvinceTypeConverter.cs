using System;

namespace SovereigntyTK.Game.Data
{
	public class ProvinceTypeConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.GetProvinceTypeByName(Data);
		}

		public override string ConvertToString(object Data)
		{
			return DataConverters.GetProvinceTypeName((ProvinceTypes)Data);
		}
	}
}
