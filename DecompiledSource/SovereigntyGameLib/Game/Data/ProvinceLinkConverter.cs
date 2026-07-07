using System;
using System.Collections.Generic;
using System.Globalization;

namespace SovereigntyTK.Game.Data
{
	public class ProvinceLinkConverter : GameDataConverter
	{
		public override object Convert(string Data)
		{
			List<ProvinceLink> list = new List<ProvinceLink>();
			if (Data != "0")
			{
				string[] array = Data.Split(new char[] { ',' });
				foreach (string text in array)
				{
					ProvinceLink provinceLink = new ProvinceLink();
					char c = text[text.Length - 1];
					ProvinceLinkTypes provinceLinkTypes = ProvinceLinkTypes.Normal;
					if (c == 'a')
					{
						provinceLinkTypes = ProvinceLinkTypes.River;
					}
					else if (c == 'b')
					{
						provinceLinkTypes = ProvinceLinkTypes.Bridge;
					}
					else if (c == 'c')
					{
						provinceLinkTypes = ProvinceLinkTypes.Road;
					}
					else if (c == 'd')
					{
						provinceLinkTypes = ProvinceLinkTypes.Blocked;
					}
					else if (c == 'e')
					{
						provinceLink.IgnoreForBorders = true;
					}
					int num = 0;
					int.TryParse(text.TrimEnd(new char[] { 'a', 'b', 'c', 'd', 'e' }), out num);
					provinceLink.LinkedProvinceID = num;
					provinceLink.LinkType = provinceLinkTypes;
					list.Add(provinceLink);
				}
			}
			return list;
		}

		public override string ConvertToString(object Data)
		{
			string text = "";
			foreach (ProvinceLink provinceLink in (Data as List<ProvinceLink>))
			{
				text += provinceLink.LinkedProvinceID.ToString(CultureInfo.InvariantCulture);
				switch (provinceLink.LinkType)
				{
				case ProvinceLinkTypes.Road:
					text += "c";
					break;
				case ProvinceLinkTypes.Bridge:
					text += "b";
					break;
				case ProvinceLinkTypes.River:
					text += "a";
					break;
				case ProvinceLinkTypes.Blocked:
					text += "d";
					break;
				}
				if (provinceLink.IgnoreForBorders)
				{
					text += "e";
				}
				text += ",";
			}
			return text.Trim(new char[] { ',' });
		}
	}
}
