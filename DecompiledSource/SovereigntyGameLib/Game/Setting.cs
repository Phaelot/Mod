using System;
using System.Xml.Linq;

namespace SovereigntyTK.Game
{
	public abstract class Setting
	{
		public Setting(string SettingName, string SettingDisplayName, string TooltipDisplayName)
		{
			this.SettingName = SettingName;
			this.SettingDisplayName = SettingDisplayName;
			this.TooltipDisplayName = TooltipDisplayName;
		}

		public abstract void Reset();

		public abstract void CopyValue(Setting OtherSetting);

		public abstract void Load(XElement Element);

		public abstract void Save(XElement Element);

		public string SettingName;

		public string SettingDisplayName;

		public string TooltipDisplayName;
	}
}
