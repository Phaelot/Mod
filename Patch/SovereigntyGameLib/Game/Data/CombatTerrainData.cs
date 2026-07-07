using System;
using System.IO;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public class CombatTerrainData
	{
		public TerrainData BaseType
		{
			get
			{
				return this.m_BaseType;
			}
			set
			{
				this.m_BaseType = value;
			}
		}

		public int FeatureHeight
		{
			get
			{
				return this.m_Height;
			}
			set
			{
				this.m_Height = value;
			}
		}

		public bool Hascoast
		{
			get
			{
				return this.m_HasCoast;
			}
			set
			{
				this.m_HasCoast = value;
			}
		}

		public string DisplayFilename
		{
			get
			{
				return this.m_DisplayFilename;
			}
			set
			{
				this.m_DisplayFilename = value;
			}
		}

		public int MinIndexValue
		{
			get
			{
				return this.m_MinIndexValue;
			}
			set
			{
				this.m_MinIndexValue = value;
			}
		}

		public int MaxIndexValue
		{
			get
			{
				return this.m_MaxIndexValue;
			}
			set
			{
				this.m_MaxIndexValue = value;
			}
		}

		public string FeatureFilename
		{
			get
			{
				return this.m_FeatureFilename;
			}
			set
			{
				this.m_FeatureFilename = value;
			}
		}

		public int FeatureMinIndex
		{
			get
			{
				return this.m_FeatureMinValue;
			}
			set
			{
				this.m_FeatureMinValue = value;
			}
		}

		public int FeatureMaxIndex
		{
			get
			{
				return this.m_FeatureMaxValue;
			}
			set
			{
				this.m_FeatureMaxValue = value;
			}
		}

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

		public CombatTerrainData(SovereigntyData Data)
		{
			this.Custom = true;
			this.Name = "Unknown Terrain";
			this.Filename = "openplainhex.png";
			this.DisplayName = "TERRAIN_DEFAULT";
			this.m_DisplayFilename = "layerbasedry.png";
			this.m_MinIndexValue = -1;
			this.m_MaxIndexValue = -1;
			this.m_FeatureFilename = "";
			this.m_FeatureMinValue = 0;
			this.m_FeatureMaxValue = 0;
			this.m_HasCoast = false;
			this.m_BaseType = Data.Terrains["Plains"];
			this.m_Height = 128;
			this.m_ID = CombatTerrainData.NextID++;
		}

		public int GetRandomIndex(Random RNG)
		{
			return RNG.Next(this.m_MaxIndexValue - this.m_MinIndexValue) + this.m_MinIndexValue;
		}

		public int GetRandomFeatureIndex(Random RNG)
		{
			return RNG.Next(this.m_FeatureMaxValue - this.m_FeatureMinValue) + this.m_FeatureMinValue;
		}

		public CombatTerrainData(BinaryReader r, int Version, SovereigntyData Data)
		{
			this.Custom = true;
			this.m_Name = r.ReadString();
			this.m_DisplayName = r.ReadString();
			this.m_Filename = r.ReadString();
			this.m_ID = r.ReadInt32();
			if (Version >= 5)
			{
				this.m_DisplayFilename = r.ReadString();
				this.m_MinIndexValue = r.ReadInt32();
				this.m_MaxIndexValue = r.ReadInt32();
				this.m_FeatureFilename = r.ReadString();
				this.m_FeatureMinValue = r.ReadInt32();
				this.m_FeatureMaxValue = r.ReadInt32();
				this.m_HasCoast = r.ReadBoolean();
			}
			else
			{
				this.m_DisplayFilename = "layerbasedry.png";
				this.m_MinIndexValue = -1;
				this.m_MaxIndexValue = -1;
				this.m_FeatureFilename = "";
				this.m_FeatureMinValue = 0;
				this.m_FeatureMaxValue = 0;
				this.m_HasCoast = false;
			}
			if (Version > 6)
			{
				string text = r.ReadString();
				this.m_BaseType = Data.Terrains[text];
			}
			else if (Version == 6)
			{
				r.ReadSingle();
				r.ReadBoolean();
				string text2 = r.ReadString();
				this.m_BaseType = Data.Terrains[text2];
			}
			else
			{
				this.m_BaseType = Data.Terrains["Plains"];
			}
			if (Version > 7)
			{
				this.m_Height = r.ReadInt32();
			}
			if (this.m_ID < 100)
			{
				this.m_ID += 100;
			}
			if (this.ID >= CombatTerrainData.NextID)
			{
				CombatTerrainData.NextID = this.ID + 1;
			}
		}

		public CombatTerrainData(CombatTerrainData Data)
		{
			this.Custom = true;
			this.m_Name = Data.m_Name;
			this.m_Filename = Data.m_Filename;
			this.m_DisplayName = Data.DisplayName;
			this.m_ID = CombatTerrainData.NextID++;
			this.m_DisplayFilename = Data.DisplayFilename;
			this.m_MinIndexValue = Data.MinIndexValue;
			this.m_MaxIndexValue = Data.MaxIndexValue;
			this.m_FeatureFilename = Data.FeatureFilename;
			this.m_FeatureMaxValue = Data.FeatureMaxIndex;
			this.m_FeatureMinValue = Data.FeatureMinIndex;
			this.m_HasCoast = Data.Hascoast;
			this.m_BaseType = Data.m_BaseType;
			this.m_Height = Data.m_Height;
		}

		public CombatTerrainData(XElement Element, SovereigntyData Data)
		{
			this.Name = Element.Attribute("name").Value;
			this.Filename = Element.Element("filename").Value;
			this.DisplayName = Element.Element("displayname").Value;
			this.DisplayFilename = Element.Element("displayfilename").Value;
			this.FeatureFilename = Element.Element("featurefilename").Value;
			this.BaseType = Data.Terrains[Element.Element("basetype").Value];
			int.TryParse(Element.Element("id").Value, out this.m_ID);
			int.TryParse(Element.Element("displaymin").Value, out this.m_MinIndexValue);
			int.TryParse(Element.Element("displaymax").Value, out this.m_MaxIndexValue);
			int.TryParse(Element.Element("featuremin").Value, out this.m_FeatureMinValue);
			int.TryParse(Element.Element("featuremax").Value, out this.m_FeatureMaxValue);
			if (Element.Element("height") != null)
			{
				int.TryParse(Element.Element("height").Value, out this.m_Height);
			}
			bool.TryParse(Element.Element("hascoast").Value, out this.m_HasCoast);
			if (this.ID >= CombatTerrainData.NextID)
			{
				CombatTerrainData.NextID = this.ID + 1;
			}
			this.Custom = false;
		}

		public override string ToString()
		{
			return this.Name;
		}

		public void Save(BinaryWriter w)
		{
			if (this.m_DisplayFilename == null)
			{
				this.m_DisplayFilename = "";
			}
			if (this.m_FeatureFilename == null)
			{
				this.m_FeatureFilename = "";
			}
			string text = "Plains";
			if (this.m_BaseType != null)
			{
				text = this.m_BaseType.ToString();
			}
			w.Write(this.m_Name);
			w.Write(this.m_DisplayName);
			w.Write(this.m_Filename);
			w.Write(this.m_ID);
			w.Write(this.m_DisplayFilename);
			w.Write(this.m_MinIndexValue);
			w.Write(this.m_MaxIndexValue);
			w.Write(this.m_FeatureFilename);
			w.Write(this.m_FeatureMinValue);
			w.Write(this.m_FeatureMaxValue);
			w.Write(this.m_HasCoast);
			w.Write(text);
			w.Write(this.m_Height);
		}

		public bool IsNaval
		{
			get
			{
				return this.BaseType.IsAnyType(new string[] { "river", "sea" });
			}
		}

		private string m_Name;

		private string m_DisplayName;

		private string m_Filename;

		private int m_ID;

		private string m_DisplayFilename;

		private int m_MinIndexValue;

		private int m_MaxIndexValue;

		private string m_FeatureFilename;

		private int m_FeatureMinValue;

		private int m_FeatureMaxValue;

		private bool m_HasCoast;

		private TerrainData m_BaseType;

		private int m_Height;

		private static int NextID;

		public bool Custom;
	}
}
