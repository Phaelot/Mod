using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SovereigntyTK.UI
{
	internal class ConsoleCommand
	{
		public ConsoleCommand(string CommandLine)
		{
			List<string> list = this.SliceText(CommandLine);
			if (list.Count == 0)
			{
				this.CommandName = "";
			}
			else
			{
				this.CommandName = list[0];
			}
			this.Parameters = new List<string>();
			for (int i = 1; i < list.Count; i++)
			{
				this.Parameters.Add(list[i]);
			}
			this.CommandName = this.StripQuotes(this.CommandName);
			for (int j = 0; j < this.Parameters.Count; j++)
			{
				this.Parameters[j] = this.StripQuotes(this.Parameters[j]);
			}
		}

		private string StripQuotes(string Text)
		{
			string text = Text;
			if (text[0] == '"')
			{
				if (text.Length == 1)
				{
					return "";
				}
				text = text.Substring(1);
			}
			if (text[text.Length - 1] == '"')
			{
				if (text.Length == 1)
				{
					return "";
				}
				text = text.Substring(0, text.Length - 1);
			}
			return text;
		}

		private List<string> SliceText(string Text)
		{
			return (from Match m in Regex.Matches(Text, "[\\\"].+?[\\\"]|[^ ]+")
				select m.Value).ToList<string>();
		}

		public string CommandName;

		public List<string> Parameters;
	}
}
