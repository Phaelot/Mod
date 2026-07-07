using System;
using System.IO;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public class CombatFeatureData
	{
		public string Name
		{
			get
			{
				return this.m_Name;
			}
			set
			{
				this.m_Name = value;
			}
		}

		public string DisplayName
		{
			get
			{
				return this.m_DisplayName;
			}
			set
			{
				this.m_DisplayName = value;
			}
		}

		public string Filename
		{
			get
			{
				return this.m_Filename;
			}
			set
			{
				this.m_Filename = value;
			}
		}

		public int ID
		{
			get
			{
				return this.m_ID;
			}
		}

		public CombatFeatureData(BinaryReader r)
		{
			this.Custom = true;
			this.m_Name = r.ReadString();
			this.m_DisplayName = r.ReadString();
			this.m_Filename = r.ReadString();
			this.m_ID = r.ReadInt32();
			if (this.ID >= CombatFeatureData.NextID)
			{
				CombatFeatureData.NextID = this.ID + 1;
			}
		}

		public CombatFeatureData(CombatFeatureData Data)
		{
			this.Custom = true;
			this.m_Name = Data.m_Name;
			this.m_Filename = Data.m_Filename;
			this.m_DisplayName = Data.DisplayName;
			this.m_ID = CombatFeatureData.NextID++;
		}

		public CombatFeatureData()
		{
			this.Custom = true;
			this.Name = "Unnamed Feature";
			this.Filename = "townhex.png";
			this.DisplayName = "FEATURE_DEFAULT";
			this.m_ID = CombatFeatureData.NextID++;
		}

		public CombatFeatureData(XElement Element)
		{
			this.Name = Element.Attribute("name").Value;
			this.Filename = Element.Element("filename").Value;
			this.DisplayName = Element.Element("displayname").Value;
			int.TryParse(Element.Element("id").Value, out this.m_ID);
			if (this.ID >= CombatFeatureData.NextID)
			{
				CombatFeatureData.NextID = this.ID + 1;
			}
			this.Custom = false;
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.m_Name);
			w.Write(this.m_DisplayName);
			w.Write(this.m_Filename);
			w.Write(this.m_ID);
		}

		public int GetFortID()
		{
			if (this.m_Name.ToLowerInvariant().Contains("fort 1"))
			{
				return 1;
			}
			if (this.m_Name.ToLowerInvariant().Contains("fort 2"))
			{
				return 2;
			}
			if (this.m_Name.ToLowerInvariant().Contains("fort 3"))
			{
				return 3;
			}
			if (this.m_Name.ToLowerInvariant().Contains("fort 4"))
			{
				return 4;
			}
			if (this.m_Name.ToLowerInvariant().Contains("fort 5"))
			{
				return 5;
			}
			return 0;
		}

		private string m_Name;

		private string m_DisplayName;

		private string m_Filename;

		private int m_ID;

		public bool Custom;

		private static int NextID;
	}
}
