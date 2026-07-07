using System;
using System.Xml.Linq;

namespace SovereigntyTK.Game
{
	public class BooleanSetting : Setting
	{
		public BooleanSetting(string SettingName, string SettingDisplayName, string TooltipDisplayName, bool DefaultValue)
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
			if (OtherSetting is BooleanSetting)
			{
				this.Value = (OtherSetting as BooleanSetting).Value;
			}
		}

		public override void Load(XElement Element)
		{
			this.Value = bool.Parse(Element.Value);
		}

		public override void Save(XElement Element)
		{
			Element.Value = this.Value.ToString();
		}

		public void SetValue(bool NewValue)
		{
			this.Value = NewValue;
		}

		public bool Value;

		public bool DefaultValue;
	}
}
