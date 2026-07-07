using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public class DiplomacyOffsetData : BaseData
	{
		public DiplomacyOffsetData()
		{
			this.NaturalOffsets = new Dictionary<string, float>();
		}

		public override string LoadData(XElement Element)
		{
			this.Realm = Element.Attribute("realm").Value;
			foreach (XAttribute xattribute in Element.Attributes())
			{
				float num;
				float.TryParse(xattribute.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
				this.NaturalOffsets.Add(base.FixName(xattribute.Name.ToString()), num);
			}
			return this.Realm;
		}

		public string Realm;

		public Dictionary<string, float> NaturalOffsets;
	}
}
