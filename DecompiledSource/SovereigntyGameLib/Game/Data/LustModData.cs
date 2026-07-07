using System;

namespace SovereigntyTK.Game.Data
{
	public class LustModData : BaseData
	{
		public override string ToString()
		{
			return this.RealmName + "." + this.ProvinceName;
		}

		[DataName("lusting_realm")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string RealmName;

		[DataName("target_province")]
		[DataConverter(typeof(GeneralStringConverter))]
		public string ProvinceName;

		[DataName("mod")]
		[DataConverter(typeof(GeneralIntConverter))]
		public int LustMod;
	}
}
