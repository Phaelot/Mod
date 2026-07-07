using System;
using System.IO;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public class CombatRoadData
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

		public CombatRoadData(BinaryReader r)
		{
			this.Custom = true;
			this.m_Name = r.ReadString();
			this.m_DisplayName = r.ReadString();
			this.m_Filename = r.ReadString();
			this.m_ID = r.ReadInt32();
			if (this.ID >= CombatRoadData.NextID)
			{
				CombatRoadData.NextID = this.ID + 1;
			}
		}

		public CombatRoadData(CombatRoadData Data)
		{
			this.Custom = true;
			this.m_Name = Data.m_Name;
			this.m_Filename = Data.m_Filename;
			this.m_DisplayName = Data.DisplayName;
			this.m_ID = CombatRoadData.NextID++;
		}

		public CombatRoadData()
		{
			this.Custom = true;
			this.Name = "Unnamed Road";
			this.Filename = "layerroads.png";
			this.DisplayName = "ROAD_DEFAULT";
			this.m_ID = CombatRoadData.NextID++;
		}

		public CombatRoadData(XElement Element)
		{
			this.Name = Element.Attribute("name").Value;
			this.Filename = Element.Element("filename").Value;
			this.DisplayName = Element.Element("displayname").Value;
			int.TryParse(Element.Element("id").Value, out this.m_ID);
			if (this.ID >= CombatRoadData.NextID)
			{
				CombatRoadData.NextID = this.ID + 1;
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

		private string m_Name;

		private string m_DisplayName;

		private string m_Filename;

		private int m_ID;

		public bool Custom;

		private static int NextID;
	}
}
