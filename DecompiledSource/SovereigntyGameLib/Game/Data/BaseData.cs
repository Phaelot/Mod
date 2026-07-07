using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace SovereigntyTK.Game.Data
{
	public abstract class BaseData
	{
		public BaseData()
		{
		}

		protected string FixName(string DamagedName)
		{
			string[] array = DamagedName.Split(new char[] { '_' });
			string text = "";
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Substring(0, 1);
				string text3 = array[i].Substring(1, array[i].Length - 1);
				text = text + text2.ToUpperInvariant() + text3;
				if (i < array.Length - 1)
				{
					text += " ";
				}
			}
			return text;
		}

		public void SaveChangedData(XElement Element, object o)
		{
			Dictionary<string, DataFieldInfo> dictionary = BaseData.FieldData[base.GetType()];
			Element.Add(new XElement("Key")
			{
				Value = this.ToString()
			});
			foreach (KeyValuePair<string, DataFieldInfo> keyValuePair in dictionary)
			{
				object rawData = keyValuePair.Value.GetRawData(this);
				object rawData2 = keyValuePair.Value.GetRawData(o);
				if (!rawData.Equals(rawData2))
				{
					Element.Add(new XElement(keyValuePair.Key)
					{
						Value = keyValuePair.Value.GetData(this)
					});
				}
			}
		}

		public void SaveData(XElement Element)
		{
			Dictionary<string, DataFieldInfo> dictionary = BaseData.FieldData[base.GetType()];
			foreach (KeyValuePair<string, DataFieldInfo> keyValuePair in dictionary)
			{
				Element.Add(new XElement(keyValuePair.Key)
				{
					Value = keyValuePair.Value.GetData(this)
				});
			}
		}

		public virtual string LoadData(XElement Element)
		{
			if (!BaseData.FieldData.ContainsKey(base.GetType()))
			{
				this.CreateFieldData();
			}
			Dictionary<string, DataFieldInfo> dictionary = BaseData.FieldData[base.GetType()];
			foreach (XAttribute xattribute in Element.Attributes())
			{
				DataFieldInfo dataFieldInfo = null;
				dictionary.TryGetValue(xattribute.Name.LocalName, out dataFieldInfo);
				if (dataFieldInfo != null)
				{
					dataFieldInfo.SetData(this, xattribute.Value);
				}
			}
			return this.ToString();
		}

		public string LoadModData(XElement Element)
		{
			if (!BaseData.FieldData.ContainsKey(base.GetType()))
			{
				this.CreateFieldData();
			}
			Dictionary<string, DataFieldInfo> dictionary = BaseData.FieldData[base.GetType()];
			foreach (XElement xelement in Element.Elements())
			{
				DataFieldInfo dataFieldInfo = null;
				dictionary.TryGetValue(xelement.Name.LocalName, out dataFieldInfo);
				if (dataFieldInfo != null)
				{
					dataFieldInfo.SetData(this, xelement.Value);
				}
			}
			return this.ToString();
		}

		private void CreateFieldData()
		{
			Dictionary<string, DataFieldInfo> dictionary = new Dictionary<string, DataFieldInfo>();
			foreach (FieldInfo fieldInfo in base.GetType().GetFields())
			{
				DataNameAttribute dataNameAttribute = fieldInfo.GetCustomAttribute(typeof(DataNameAttribute)) as DataNameAttribute;
				DataConverterAttribute dataConverterAttribute = fieldInfo.GetCustomAttribute(typeof(DataConverterAttribute)) as DataConverterAttribute;
				if (dataNameAttribute != null && dataConverterAttribute != null)
				{
					DataFieldInfo dataFieldInfo = new DataFieldInfo();
					dataFieldInfo.AttributeName = dataNameAttribute.DataName;
					dataFieldInfo.Converter = Activator.CreateInstance(dataConverterAttribute.ConverterType) as GameDataConverter;
					dataFieldInfo.Field = fieldInfo;
					dictionary.Add(dataFieldInfo.AttributeName, dataFieldInfo);
				}
			}
			foreach (PropertyInfo propertyInfo in base.GetType().GetProperties())
			{
				DataNameAttribute dataNameAttribute2 = propertyInfo.GetCustomAttribute(typeof(DataNameAttribute)) as DataNameAttribute;
				DataConverterAttribute dataConverterAttribute2 = propertyInfo.GetCustomAttribute(typeof(DataConverterAttribute)) as DataConverterAttribute;
				if (dataNameAttribute2 != null && dataConverterAttribute2 != null)
				{
					DataFieldInfo dataFieldInfo2 = new DataFieldInfo();
					dataFieldInfo2.AttributeName = dataNameAttribute2.DataName;
					dataFieldInfo2.Converter = Activator.CreateInstance(dataConverterAttribute2.ConverterType) as GameDataConverter;
					dataFieldInfo2.Property = propertyInfo;
					dictionary.Add(dataFieldInfo2.AttributeName, dataFieldInfo2);
				}
			}
			BaseData.FieldData.Add(base.GetType(), dictionary);
		}

		private static Dictionary<Type, Dictionary<string, DataFieldInfo>> FieldData = new Dictionary<Type, Dictionary<string, DataFieldInfo>>();
	}
}
