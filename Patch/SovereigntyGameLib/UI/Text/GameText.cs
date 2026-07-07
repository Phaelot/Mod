using System;
using System.Collections.Generic;

namespace SovereigntyTK.UI.Text
{
	public class GameText
	{
		public static GameText CreateFromLiteral(string LiteralText)
		{
			return new GameText
			{
				LiteralText = LiteralText
			};
		}

		public static GameText CreateLocalised(string TextName, params object[] Args)
		{
			return new GameText
			{
				TextName = TextName,
				TextArgs = Args
			};
		}

		private GameText()
		{
			this.Children = new List<GameText>();
		}

		public void AddChildText(GameText Child)
		{
			this.Children.Add(Child);
		}

		public string GetActualText(GameBase Game)
		{
			if (this.LiteralText != null)
			{
				return this.LiteralText;
			}
			string text = Game.Utilities.TextManager.GetText(this.TextName, this.TextArgs);
			int num = 1;
			foreach (GameText gameText in this.Children)
			{
				if (gameText == null)
				{
					num++;
				}
				else
				{
					text = text.Replace("[text" + num + "]", gameText.GetActualText(Game));
					text = text.Replace("[Text" + num + "]", gameText.GetActualText(Game));
					num++;
				}
			}
			return text;
		}

		public string LiteralText;

		public string TextName;

		public object[] TextArgs;

		private List<GameText> Children;
	}
}
