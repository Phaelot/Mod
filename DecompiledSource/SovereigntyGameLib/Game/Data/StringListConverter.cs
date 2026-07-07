using System;
using System.Collections.Generic;

namespace SovereigntyTK.Game.Data
{
	public class StringListConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			List<string> list = new List<string>();
			if (Data == null || Data.Length == 0)
			{
				return list;
			}
			list.AddRange(Data.Split(new char[] { ',' }));
			return list;
		}

		public override string ConvertToString(object Data)
		{
			string text = "";
			foreach (string text2 in (Data as List<string>))
			{
				text = text + text2 + ",";
			}
			return text.TrimEnd(new char[] { ',' });
		}
	}
}
