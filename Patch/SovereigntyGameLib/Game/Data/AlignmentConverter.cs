using System;

namespace SovereigntyTK.Game.Data
{
	public class AlignmentConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			return DataConverters.ParseAlignmentString(Data);
		}

		public override string ConvertToString(object Data)
		{
			return ((RealmAlignments)Data).ToString();
		}
	}
}
