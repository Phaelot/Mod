using System;
using System.Xml.Linq;
using OpenTK.Input;

namespace SovereigntyTK.Game
{
	public class KeybindSetting : Setting
	{
		public KeybindSetting(string SettingName, string SettingDisplayName, string TooltipDisplayName, Key DefaultValue)
			: base(SettingName, SettingDisplayName, TooltipDisplayName)
		{
			this.Value = DefaultValue;
			this.DefaultValue = DefaultValue;
		}

		public override void Reset()
		{
			this.Value = this.DefaultValue;
		}

		public override void CopyValue(Setting OtherSetting)
		{
			if (OtherSetting is KeybindSetting)
			{
				this.Value = (OtherSetting as KeybindSetting).Value;
			}
		}

		public override void Load(XElement Element)
		{
			this.Value = (Key)Enum.Parse(typeof(Key), Element.Value);
		}

		public override void Save(XElement Element)
		{
			Element.Value = this.Value.ToString();
		}

		public Key DefaultValue;

		public Key Value;
	}
}
