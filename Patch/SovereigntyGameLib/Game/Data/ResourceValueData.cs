using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public class ResourceValueData : BaseData
	{
		public ResourceValueData()
		{
			this.ValuationTable = new Dictionary<string, int>();
		}

		public override string LoadData(XElement Element)
		{
			this.Realm = Element.Attribute("realm").Value;
			foreach (XAttribute xattribute in Element.Attributes())
			{
				int num;
				int.TryParse(xattribute.Value, out num);
				this.ValuationTable.Add(base.FixName(xattribute.Name.ToString()), num);
			}
			return this.Realm;
		}

		public string Realm;

		public Dictionary<string, int> ValuationTable;
	}
}
