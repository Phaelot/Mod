using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SovereigntyTK.Game
{
	public class StringListSetting : Setting
	{
		public StringListSetting(string SettingName, string SettingDisplayName, string TooltipDisplayName, string DefaultValue)
			: base(SettingName, SettingDisplayName, TooltipDisplayName)
		{
			this.Value = DefaultValue.ToLowerInvariant();
			this.DefaultValue = DefaultValue.ToLowerInvariant();
			this.DisplayValues = new Dictionary<string, string>();
		}

		public override void Reset()
		{
			this.Value = this.DefaultValue;
		}

		public override void CopyValue(Setting OtherSetting)
		{
			if (OtherSetting is StringListSetting)
			{
				this.Value = (OtherSetting as StringListSetting).Value;
			}
		}

		public override void Load(XElement Element)
		{
			this.SetValue(Element.Value.ToLowerInvariant());
		}

		public override void Save(XElement Element)
		{
			Element.Value = this.Value;
		}

		public void SetValue(string NewValue)
		{
			this.Value = NewValue.ToLowerInvariant();
			if (!this.DisplayValues.ContainsKey(this.Value))
			{
				this.Value = this.DefaultValue;
			}
		}

		public void Increase()
		{
			int num = this.DisplayValues.Keys.ToList<string>().IndexOf(this.Value);
			if (num < this.DisplayValues.Count - 1)
			{
				num++;
				this.SetValue(this.DisplayValues.Keys.ElementAt(num));
			}
		}

		public void Decrease()
		{
			int num = this.DisplayValues.Keys.ToList<string>().IndexOf(this.Value);
			if (num > 0)
			{
				num--;
				this.SetValue(this.DisplayValues.Keys.ElementAt(num));
			}
		}

		public string DefaultValue;

		public string Value;

		public Dictionary<string, string> DisplayValues;
	}
}
