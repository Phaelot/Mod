using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OpenTK;

namespace SovereigntyTK.Game
{
	public class DisplayModeSetting : Setting
	{
		public DisplayModeSetting(string SettingName, string SettingDisplayName, string TooltipDisplayName, DisplayResolution DefaultValue)
			: base(SettingName, SettingDisplayName, TooltipDisplayName)
		{
			this.Value = DefaultValue;
			this.DefaultValue = DefaultValue;
			this.DisplayValues = new List<DisplayResolution>();
		}

		public override void Reset()
		{
			this.Value = this.DefaultValue;
		}

		public override void CopyValue(Setting OtherSetting)
		{
			if (OtherSetting is DisplayModeSetting)
			{
				this.Value = (OtherSetting as DisplayModeSetting).Value;
			}
		}

		public override void Load(XElement Element)
		{
			int w = int.Parse(Element.Element("Width").Value);
			int h = int.Parse(Element.Element("Height").Value);
			int d = int.Parse(Element.Element("Depth").Value);
			int r = int.Parse(Element.Element("Refresh").Value);
			this.Value = this.DisplayValues.FirstOrDefault((DisplayResolution x) => x.Width == w && x.Height == h && x.BitsPerPixel == d && x.RefreshRate == (float)r);
			if (this.Value == null)
			{
				this.Value = this.DefaultValue;
			}
		}

		public override void Save(XElement Element)
		{
			Element.Add(new XElement("Width")
			{
				Value = this.Value.Width.ToString()
			});
			Element.Add(new XElement("Height")
			{
				Value = this.Value.Height.ToString()
			});
			Element.Add(new XElement("Depth")
			{
				Value = this.Value.BitsPerPixel.ToString()
			});
			Element.Add(new XElement("Refresh")
			{
				Value = this.Value.RefreshRate.ToString()
			});
		}

		public void SetValue(DisplayResolution NewValue)
		{
			this.Value = this.DisplayValues.FirstOrDefault((DisplayResolution x) => x.Width == NewValue.Width && x.Height == NewValue.Height && x.BitsPerPixel == NewValue.BitsPerPixel && x.RefreshRate == NewValue.RefreshRate);
			if (this.Value == null)
			{
				this.Value = this.DefaultValue;
			}
		}

		public void Increase(int Clicks)
		{
			int num = this.DisplayValues.IndexOf(this.Value);
			if (num < this.DisplayValues.Count - 1)
			{
				num++;
				this.SetValue(this.DisplayValues[num]);
			}
		}

		public void Decrease(int Clicks)
		{
			int num = this.DisplayValues.IndexOf(this.Value);
			if (num > 0)
			{
				num--;
				this.SetValue(this.DisplayValues[num]);
			}
		}

		public DisplayResolution DefaultValue;

		public DisplayResolution Value;

		public List<DisplayResolution> DisplayValues;
	}
}
