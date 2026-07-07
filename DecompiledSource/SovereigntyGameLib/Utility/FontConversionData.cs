using System;
using System.Xml.Linq;

namespace SovereigntyTK.Utility
{
	public class FontConversionData
	{
		public FontConversionData(XElement Element)
		{
			this.Name = Element.Attribute("name").Value;
			this.RegularFont = Element.Element("regular").Value;
			this.BoldFont = this.RegularFont;
			this.ItalicFont = this.RegularFont;
			if (Element.Element("bold") != null)
			{
				this.BoldFont = Element.Element("bold").Value;
			}
			if (Element.Element("italic") != null)
			{
				this.ItalicFont = Element.Element("italic").Value;
			}
		}

		public string Name;

		public string RegularFont;

		public string BoldFont;

		public string ItalicFont;
	}
}
