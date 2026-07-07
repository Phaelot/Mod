using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Utility
{
	public class FontConvertor
	{
		public FontConvertor(GameBase Game)
		{
			this.Fonts = new Dictionary<string, FontConversionData>();
			XElement xelement = XElement.Load(Game.Utilities.FileSystem.OpenFile("Data\\Fonts\\fonts.xml", FileTypes.Application, FileModes.ReadOnly, true));
			foreach (XElement xelement2 in xelement.Elements())
			{
				FontConversionData fontConversionData = new FontConversionData(xelement2);
				this.Fonts.Add(fontConversionData.Name, fontConversionData);
			}
		}

		public string GetFont(string Name, bool Bold)
		{
			if (!this.Fonts.ContainsKey(Name))
			{
				throw new Exception("Requested font does not exist: " + Name);
			}
			if (Bold)
			{
				return this.Fonts[Name].BoldFont;
			}
			return this.Fonts[Name].RegularFont;
		}

		private Dictionary<string, FontConversionData> Fonts;
	}
}
