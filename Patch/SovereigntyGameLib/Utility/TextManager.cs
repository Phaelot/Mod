using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Utility
{
	public class TextManager
	{
		public TextManager()
		{
			this.Texts = new Dictionary<string, Dictionary<string, string>>();
			this.LanguageNames = new List<string>();
			this.FirstTable = true;
		}

		private void LoadLanguageNames(XElement Element)
		{
			XAttribute xattribute = Element.FirstAttribute;
			while (xattribute != null)
			{
				xattribute = xattribute.NextAttribute;
				if (xattribute != null)
				{
					this.AddLanguage(xattribute.Name.ToString().ToLowerInvariant());
				}
			}
		}

		public void LoadChanges(XElement Element)
		{
			foreach (XElement xelement in Element.Elements())
			{
				if (xelement.Name.LocalName == "Delete")
				{
					string value = xelement.Attribute("Key").Value;
					this.Texts.Remove(value);
				}
				if (xelement.Name.LocalName == "Update")
				{
					string value2 = xelement.Attribute("Key").Value;
					if (!this.Texts.ContainsKey(value2))
					{
						this.Texts.Add(value2, new Dictionary<string, string>());
					}
					foreach (XAttribute xattribute in xelement.Attributes())
					{
						string localName = xattribute.Name.LocalName;
						if (!(localName == "Key") && !(localName == "TextKey"))
						{
							string value3 = xattribute.Value;
							if (!this.LanguageNames.Contains(localName))
							{
								this.AddLanguage(localName);
							}
							this.Texts[value2][localName] = value3;
						}
					}
				}
			}
		}

		private void AddLanguage(string Language)
		{
			this.LanguageNames.Add(Language);
			foreach (KeyValuePair<string, Dictionary<string, string>> keyValuePair in this.Texts)
			{
				keyValuePair.Value.Add(Language, "");
			}
		}

		public void LoadTextData(XElement TableRootElement)
		{
			bool flag = true;
			foreach (XElement xelement in TableRootElement.Elements())
			{
				if (flag)
				{
					if (this.FirstTable)
					{
						this.LoadLanguageNames(xelement);
					}
					flag = false;
				}
				string value = xelement.Attribute("textid").Value;
				if (!this.Texts.ContainsKey(value))
				{
					this.Texts.Add(value, new Dictionary<string, string>());
				}
				foreach (XAttribute xattribute in xelement.Attributes())
				{
					if (!(xattribute.Name.LocalName == "textid"))
					{
						string localName = xattribute.Name.LocalName;
						string value2 = xattribute.Value;
						if (!this.LanguageNames.Contains(localName))
						{
							this.AddLanguage(localName);
						}
						this.Texts[value][localName] = value2;
					}
				}
			}
			this.FirstTable = false;
		}

		public virtual string GetText(string TextName, params object[] Args)
		{
			Dictionary<string, string> dictionary = null;
			if (TextName == null)
			{
				return "ERROR";
			}
			if (TextName.StartsWith("LITERAL:", StringComparison.OrdinalIgnoreCase))
			{
				return TextName.Substring(8);
			}
			if (!this.Texts.TryGetValue(TextName, out dictionary))
			{
				if (this.AllowMissingText)
				{
					return TextName;
				}
				return "TEXT_MISSING";
			}
			else
			{
				string text = dictionary[this.CurrentLanguageName].Replace("\\n", "\n");
				if (text == null || text == "")
				{
					return "TEXT_MISSING";
				}
				string text2;
				try
				{
					text2 = string.Format(text, Args);
				}
				catch
				{
					text2 = "FORMAT_ERROR";
				}
				return text2;
			}
		}

		public void SetLanguage(string Name)
		{
			int num = this.LanguageNames.IndexOf(Name.ToLowerInvariant());
			if (num != -1)
			{
				this.CurrentLanguageName = Name.ToLowerInvariant();
				this.CurrentLanguageIndex = num;
				return;
			}
			this.CurrentLanguageName = this.LanguageNames[0];
			this.CurrentLanguageIndex = 0;
		}

		public void SetLanguage(int Index)
		{
			if (Index < 0)
			{
				return;
			}
			if (Index >= this.LanguageNames.Count)
			{
				return;
			}
			this.CurrentLanguageName = this.LanguageNames[Index];
			this.CurrentLanguageIndex = Index;
		}

		public void Clear()
		{
			this.Texts = new Dictionary<string, Dictionary<string, string>>();
			this.LanguageNames = new List<string>();
			this.FirstTable = true;
		}

		public int GetMaxLanguageIndex()
		{
			return this.LanguageNames.Count - 1;
		}

		public string GetLanguageName(int Index)
		{
			return this.LanguageNames[Index];
		}

		private Dictionary<string, Dictionary<string, string>> Texts;

		public List<string> LanguageNames;

		private bool FirstTable;

		public string CurrentLanguageName;

		public int CurrentLanguageIndex;

		public bool AllowMissingText;
	}
}
