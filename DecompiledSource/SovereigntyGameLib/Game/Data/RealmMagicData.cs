using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public class RealmMagicData : BaseData
	{
		[DataName("name")]
		[EditorData("Name", EditorTypes.Text)]
		[PrimaryKey(1)]
		[DataConverter(typeof(GeneralStringConverter))]
		public string Name { get; set; }

		[EditorData("Localised Name", EditorTypes.Text)]
		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displayname")]
		public string DisplayName { get; set; }

		[DataConverter(typeof(TextIndexConverter))]
		[DataName("displaytext")]
		[EditorData("Localised Description", EditorTypes.Text)]
		public string DisplayText { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("art")]
		[EditorData("Image File", EditorTypes.Text)]
		public string ImageFileName { get; set; }

		[DataConverter(typeof(GeneralStringConverter))]
		[DataName("sound")]
		[EditorData("Sound File", EditorTypes.Text)]
		public string SoundFileName { get; set; }

		[EditorData("Target Type", EditorTypes.DropDownEnum)]
		[DataConverter(typeof(TargetTypeConverter))]
		[DataName("target")]
		public SpellTargets TargetType { get; set; }

		[DataName("range")]
		[DataConverter(typeof(SpellRangeConverter))]
		[EditorData("Range", EditorTypes.Text)]
		public int Range { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[DataName("area")]
		[EditorData("Area", EditorTypes.Text)]
		public int Area { get; set; }

		[DataConverter(typeof(SpellDurationConverter))]
		[EditorData("Duration", EditorTypes.Text)]
		[DataName("dur")]
		public int Duration { get; set; }

		[DataConverter(typeof(MagicRealmConverter))]
		[DataName("school")]
		[EditorData("Magic School", EditorTypes.DropDownEnum)]
		public SpellSchools School { get; set; }

		[DataConverter(typeof(GeneralIntConverter))]
		[EditorData("Level", EditorTypes.Text)]
		[DataName("lvl")]
		public int Level { get; set; }

		[DataConverter(typeof(SpellTypeConverter))]
		[DataName("typ")]
		[EditorData("Type", EditorTypes.DropDownEnum)]
		public SpellTypes Type { get; set; }

		[DataName("blockdispel")]
		[DataConverter(typeof(YesNoConverter))]
		[EditorData("Prevent Dispel", EditorTypes.Boolean)]
		public bool NoDispell { get; set; }

		public override string ToString()
		{
			return this.Name;
		}

		private void ParseRealm(XElement Element)
		{
			string value = Element.Attribute("name").Value;
			int num = 0;
			int.TryParse(Element.Attribute("Column").Value, out num);
			List<string> list = new List<string>();
			foreach (XElement xelement in Element.Elements("requires"))
			{
				list.Add(xelement.Value);
			}
			SpellOwnerData spellOwnerData = new SpellOwnerData(value, num, list);
			this.OwningRealms.Add(spellOwnerData);
		}

		public SpellOwnerData GetOwnerData(string RealmName)
		{
			if (this.OwningRealms == null)
			{
				return null;
			}
			foreach (SpellOwnerData spellOwnerData in this.OwningRealms)
			{
				if (spellOwnerData.RealmName == RealmName)
				{
					return spellOwnerData;
				}
			}
			return null;
		}

		public string SchoolName
		{
			get
			{
				switch (this.School)
				{
				case SpellSchools.Death:
					return "Death";
				case SpellSchools.Illusion:
					return "Illusion";
				case SpellSchools.Nature:
					return "Nature";
				case SpellSchools.War:
					return "War";
				default:
					return "";
				}
			}
		}

		public static string GetSchoolTextName(SpellSchools School)
		{
			switch (School)
			{
			case SpellSchools.Death:
				return "MAGIC_DEATH";
			case SpellSchools.Illusion:
				return "MAGIC_ILLUSION";
			case SpellSchools.Nature:
				return "MAGIC_NATURE";
			case SpellSchools.War:
				return "MAGIC_WAR";
			default:
				return "";
			}
		}

		public string TargetName
		{
			get
			{
				switch (this.TargetType)
				{
				case SpellTargets.Unit:
					return "Unit";
				case SpellTargets.Province:
					return "Province";
				case SpellTargets.Realm:
					return "Realm";
				case SpellTargets.SeaZone:
					return "Sea Zone";
				case SpellTargets.None:
					return "None";
				case SpellTargets.Stack:
					return "Stack";
				default:
					return "";
				}
			}
		}

		internal void LoadTreeData(XElement SpellRoot)
		{
			this.OwningRealms = new List<SpellOwnerData>();
			foreach (XElement xelement in SpellRoot.Elements())
			{
				string localName;
				if ((localName = xelement.Name.LocalName) != null && localName == "realm")
				{
					this.ParseRealm(xelement);
				}
			}
		}

		private List<SpellOwnerData> OwningRealms;
	}
}
