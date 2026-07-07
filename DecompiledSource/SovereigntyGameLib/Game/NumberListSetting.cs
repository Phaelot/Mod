using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Game
{
	public class NumberListSetting : Setting
	{
		public NumberListSetting(string SettingName, string SettingDisplayName, string TooltipDisplayName, int DefaultValue)
			: base(SettingName, SettingDisplayName, TooltipDisplayName)
		{
			this.Value = DefaultValue;
			this.DefaultValue = DefaultValue;
			this.DisplayValues = new List<int>();
		}

		public override void Reset()
		{
			this.Value = this.DefaultValue;
		}

		public override void CopyValue(Setting OtherSetting)
		{
			if (OtherSetting is NumberListSetting)
			{
				this.Value = (OtherSetting as NumberListSetting).Value;
			}
		}

		public override void Load(XElement Element)
		{
			this.Value = int.Parse(Element.Value);
			if (!this.DisplayValues.Contains(this.Value))
			{
				this.Value = this.DefaultValue;
			}
		}

		public override void Save(XElement Element)
		{
			Element.Value = this.Value.ToString();
		}

		public void SetValue(int NewValue)
		{
			this.Value = NewValue;
			if (!this.DisplayValues.Contains(this.Value))
			{
				this.Value = this.DefaultValue;
			}
		}

		public void Increase(int Amount)
		{
			int num = this.DisplayValues.IndexOf(this.Value);
			if (num < this.DisplayValues.Count - 1)
			{
				num++;
				this.SetValue(this.DisplayValues[num]);
			}
		}

		public void Decrease(int Amount)
		{
			int num = this.DisplayValues.IndexOf(this.Value);
			if (num > 0)
			{
				num--;
				this.SetValue(this.DisplayValues[num]);
			}
		}

		public int DefaultValue;

		public int Value;

		public List<int> DisplayValues;
	}
}
