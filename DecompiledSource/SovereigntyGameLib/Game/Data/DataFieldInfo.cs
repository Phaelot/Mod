using System;
using System.Reflection;

namespace SovereigntyTK.Game.Data
{
	public class DataFieldInfo
	{
		public void SetData(object Row, string Data)
		{
			if (this.Field != null)
			{
				this.Field.SetValue(Row, this.Converter.Convert(Data));
			}
			if (this.Property != null)
			{
				this.Property.SetValue(Row, this.Converter.Convert(Data));
			}
		}

		public string GetFieldName()
		{
			if (this.Field != null)
			{
				return this.Field.Name;
			}
			if (this.Property != null)
			{
				return this.Property.Name;
			}
			return "";
		}

		public string GetData(object Row)
		{
			if (this.Field != null)
			{
				return this.Converter.ConvertToString(this.Field.GetValue(Row));
			}
			if (this.Property != null)
			{
				return this.Converter.ConvertToString(this.Property.GetValue(Row));
			}
			return "";
		}

		public object GetRawData(object Row)
		{
			if (this.Field != null)
			{
				return this.Field.GetValue(Row);
			}
			if (this.Property != null)
			{
				return this.Property.GetValue(Row);
			}
			return null;
		}

		public string AttributeName;

		public FieldInfo Field;

		public PropertyInfo Property;

		public GameDataConverter Converter;
	}
}
