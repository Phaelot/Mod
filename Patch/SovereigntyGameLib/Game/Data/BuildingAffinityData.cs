using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public class BuildingAffinityData : BaseData
	{
		public override string LoadData(XElement Element)
		{
			this.BuildingName = Element.Attribute("name").Value;
			this.Affinities = new Dictionary<string, int>();
			foreach (XAttribute xattribute in Element.Attributes())
			{
				int num = 0;
				int.TryParse(xattribute.Value, out num);
				this.Affinities.Add(xattribute.Name.LocalName, num);
			}
			return this.BuildingName;
		}

		public int GetAffinity(string RealmName)
		{
			int num = 0;
			string text = RealmName.ToLowerInvariant();
			text = text.Replace(" ", "_");
			this.Affinities.TryGetValue(text, out num);
			return num;
		}

		private string BuildingName;

		private Dictionary<string, int> Affinities;
	}
}
