using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Game
{
	public class EnumeratedSetting : Setting
	{
		public EnumeratedSetting(string SettingName, string SettingDisplayName, string TooltipDisplayName, int DefaultValue, int MinValue, int MaxValue)
			: base(SettingName, SettingDisplayName, TooltipDisplayName)
		{
			this.Value = DefaultValue;
			this.DefaultValue = DefaultValue;
			this.MinValue = MinValue;
			this.MaxValue = MaxValue;
			this.DisplayValues = new Dictionary<int, string>();
		}

		public override void Reset()
		{
			this.Value = this.DefaultValue;
		}

		public override void CopyValue(Setting OtherSetting)
		{
			if (OtherSetting is EnumeratedSetting)
			{
				this.Value = (OtherSetting as EnumeratedSetting).Value;
			}
		}

		public override void Load(XElement Element)
		{
			this.SetValue(int.Parse(Element.Value));
		}

		public override void Save(XElement Element)
		{
			Element.Value = this.Value.ToString();
		}

		public void SetValue(int NewValue)
		{
			this.Value = NewValue;
			if (this.Value < this.MinValue)
			{
				this.Value = this.MinValue;
			}
			if (this.Value > this.MaxValue)
			{
				this.Value = this.MaxValue;
			}
		}

		public void Increase(int Amount)
		{
			this.SetValue(this.Value + Amount);
		}

		public void Decrease(int Amount)
		{
			this.SetValue(this.Value - Amount);
		}

		public int DefaultValue;

		public int Value;

		public int MinValue;

		public int MaxValue;

		public Dictionary<int, string> DisplayValues;
	}
}
