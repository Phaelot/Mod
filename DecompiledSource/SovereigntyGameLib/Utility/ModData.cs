using System;
using System.Xml.Linq;

namespace SovereigntyTK.Utility
{
	public class ModData
	{
		public ModData(string Filename)
		{
			XElement xelement = XElement.Load(Filename);
			this.ID = xelement.Element("ID").Value;
			this.Name = xelement.Element("Name").Value;
			this.ShortDescription = xelement.Element("ShortDesc").Value;
		}

		public string ID;

		public string Name;

		public string ShortDescription;
	}
}
