using System;
using System.Globalization;

namespace SovereigntyTK.UI.Controls
{
	public class PositionData
	{
		public PositionData(float Value, UIUnits UnitType, bool X)
		{
			this.Value = Value;
			this.UnitType = UnitType;
			this.IsXValue = X;
		}

		public PositionData(float InitialValue, bool X)
		{
			this.Value = InitialValue;
			this.UnitType = UIUnits.Pixel;
			this.IsXValue = X;
		}

		public PositionData(string ValueString, bool X)
		{
			string text;
			if (ValueString.EndsWith("%"))
			{
				this.UnitType = UIUnits.Percent;
				text = ValueString.Substring(0, ValueString.Length - 1);
			}
			else if (ValueString.EndsWith("px"))
			{
				this.UnitType = UIUnits.Pixel;
				text = ValueString.Substring(0, ValueString.Length - 2);
			}
			else
			{
				this.UnitType = UIUnits.Internal;
				text = ValueString;
			}
			float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out this.Value);
			this.IsXValue = X;
		}

		public float Value;

		public UIUnits UnitType;

		public bool IsXValue;
	}
}
