using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using SovereigntyTK.UI.Controls;

namespace SovereigntyTK.UI.Text
{
	public class TextRenderer
	{
		public TextRenderer(GameBase Game)
		{
			this.Game = Game;
		}

		public Rectangle GetWordBounds(CharacterData Character)
		{
			int num = 0;
			while (num < this.Characters.Length && this.Characters[num] != Character)
			{
				num++;
			}
			int num2 = num;
			while (num2 > 0 && this.Characters[num2].Character != ' ')
			{
				num2--;
			}
			int num3 = num;
			while (num3 < this.Characters.Length - 1 && this.Characters[num3].Character != ' ')
			{
				num3++;
			}
			if (this.Characters[num2].Character == ' ')
			{
				num2++;
			}
			if (this.Characters[num3].Character == ' ')
			{
				num3--;
			}
			List<Rectangle> list = new List<Rectangle>();
			for (int i = num2; i <= num3; i++)
			{
				list.Add(this.Characters[i].GetBounds());
			}
			Rectangle rectangle = default(Rectangle);
			rectangle.X = list[0].X;
			rectangle.Y = list.Min((Rectangle x) => x.Y);
			rectangle.Width = list.Sum((Rectangle x) => x.Width);
			rectangle.Height = list.Max((Rectangle x) => x.Bottom) - rectangle.Y;
			return rectangle;
		}

		public CharacterData GetCharacterAtPoint(int X, int Y)
		{
			foreach (CharacterData characterData in this.Characters)
			{
				if (characterData.ContainsPoint(X, Y))
				{
					return characterData;
				}
			}
			return null;
		}

		public void Dispose()
		{
		}

		public unsafe void RenderOverImage(Bitmap Output, Color Col)
		{
			BitmapData bitmapData = Output.LockBits(new Rectangle(0, 0, Output.Width, Output.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			for (int i = 0; i < this.Characters.Length; i++)
			{
				if (this.Characters[i] != null && this.Characters[i].CharImage != null && this.Characters[i].Character != '\n')
				{
					BitmapData bitmapData2 = this.Characters[i].CharImage.LockBits(new Rectangle(0, 0, this.Characters[i].CharImage.Width, this.Characters[i].CharImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					for (int j = 0; j < this.Characters[i].CharImage.Width; j++)
					{
						for (int k = 0; k < this.Characters[i].CharImage.Height; k++)
						{
							int num = j + this.Characters[i].RenderX;
							int num2 = k + this.Characters[i].RenderY;
							int num3 = j * 4 + k * bitmapData2.Stride;
							int num4 = num * 4 + num2 * bitmapData.Stride;
							byte* ptr = (byte*)(void*)bitmapData.Scan0;
							byte* ptr2 = (byte*)(void*)bitmapData2.Scan0;
							ptr[num4] = Col.B;
							ptr[num4 + 1] = Col.G;
							ptr[num4 + 2] = Col.R;
							ptr[num4 + 3] = ptr2[num3 + 3];
						}
					}
					this.Characters[i].CharImage.UnlockBits(bitmapData2);
				}
			}
			Output.UnlockBits(bitmapData);
		}

		public void Render(Bitmap Output)
		{
			Graphics graphics = Graphics.FromImage(Output);
			graphics.Clear(Color.Transparent);
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			for (int i = 0; i < this.Characters.Length; i++)
			{
				if (this.Characters[i] != null && this.Characters[i].CharImage != null && this.Characters[i].Character != '\n')
				{
					graphics.DrawImage(this.Characters[i].CharImage, this.Characters[i].RenderX, this.Characters[i].RenderY);
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write(string.Concat(new object[]
						{
							"Text Renderer: character ",
							this.Characters[i].Character,
							" drawn at ",
							this.Characters[i].RenderX,
							",",
							this.Characters[i].RenderY
						}));
					}
					if (this.Characters[i].UnderlinePos != -1)
					{
						Point point = new Point(this.Characters[i].RenderX, this.Characters[i].UnderlinePos);
						Point point2 = point;
						point2.X += this.Characters[i].RealWidth;
						graphics.DrawLine(Pens.White, point, point2);
					}
				}
			}
			graphics.Dispose();
		}

		private float GetTextBottom()
		{
			if (this.Characters.Length == 0)
			{
				return 0f;
			}
			float num;
			try
			{
				num = (float)this.Characters.Where((CharacterData x) => x.LineNumber == this.LineCount - 1).Max((CharacterData x) => x.RenderY + x.Height);
			}
			catch
			{
				num = 0f;
			}
			return num;
		}

		private float GetTextTop()
		{
			if (this.Characters.Length == 0)
			{
				return 0f;
			}
			float num;
			try
			{
				int TargetLine = this.Characters.Min((CharacterData x) => x.LineNumber);
				num = (float)this.Characters.Where((CharacterData x) => x.LineNumber == TargetLine).Min((CharacterData x) => x.RenderY);
			}
			catch
			{
				num = 0f;
			}
			return num;
		}

		public void SetText(string Text, GameFont Font, Color Col, float MaxWidth, float MaxHeight)
		{
			this.MaxWidth = MaxWidth;
			this.MaxHeight = MaxHeight;
			this.LineHeight = (float)Font.LineHeight;
			if (GlobalData.LoggingActive)
			{
				this.Game.Utilities.Logger.Write("Text Renderer: line height set to " + this.LineHeight);
			}
			this.Characters = new CharacterData[Text.Length];
			int i = 0;
			int num = 0;
			bool flag = false;
			while (i < Text.Length)
			{
				if (Text[i] == '[' && !this.IgnoreFormatting)
				{
					string text = "";
					i++;
					while (Text[i] != ']')
					{
						text += Text[i++];
					}
					i++;
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write("Text Renderer: tag " + text + " found");
					}
					if (text == "b")
					{
						string font = this.Game.Utilities.FontConvertor.GetFont(Font.FontName, true);
						Font = GameFont.GetFont(this.Game, Font.FontName, "Data\\Fonts\\" + font, Font.FontSize);
					}
					if (text == "/b")
					{
						string font2 = this.Game.Utilities.FontConvertor.GetFont(Font.FontName, false);
						Font = GameFont.GetFont(this.Game, Font.FontName, "Data\\Fonts\\" + font2, Font.FontSize);
					}
					if (text == "u")
					{
						flag = true;
					}
					if (text == "/u")
					{
						flag = false;
					}
				}
				else
				{
					FontGlyphData glyph = Font.GetGlyph(Text[i]);
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write("Text Renderer: obtaining data for character " + Text[i]);
					}
					int num2 = 0;
					if (i < Text.Length - 1)
					{
						num2 = Font.GetKerning(Text[i], Text[i + 1]);
					}
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write("Text Renderer: kerning adjustment " + num2);
					}
					CharacterData characterData = new CharacterData();
					characterData.CharImage = glyph.GlyphImage;
					characterData.OffsetX = glyph.OffsetX + num2;
					characterData.OffsetY = glyph.OffsetY;
					characterData.Width = glyph.AdvanceX;
					characterData.RealWidth = glyph.AdvanceX;
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write(string.Concat(new object[] { "Text Renderer: character data: offsetx: ", characterData.OffsetX, ", offsety: ", characterData.OffsetY, ", width: ", characterData.Width }));
					}
					if (flag)
					{
						characterData.UnderlinePos = Font.UnderlinePosition;
					}
					else
					{
						characterData.UnderlinePos = -1;
					}
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write("Text Renderer: underline: " + characterData.UnderlinePos);
					}
					characterData.Height = glyph.Height;
					characterData.Character = Text[i];
					characterData.CharColour = Col;
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write("Text Renderer: height: " + characterData.Height);
					}
					if (characterData.Character == '\n')
					{
						characterData.Width = 0;
						characterData.Height = 0;
						if (GlobalData.LoggingActive)
						{
							this.Game.Utilities.Logger.Write("Text Renderer: newline character, width and height set to 0");
						}
					}
					this.Characters[num++] = characterData;
					i++;
				}
			}
			Array.Resize<CharacterData>(ref this.Characters, num);
			int num3 = 0;
			int num4 = Font.LineHeight;
			int num5 = 0;
			int j = 0;
			int num6 = 0;
			int num7 = 0;
			if (GlobalData.LoggingActive)
			{
				this.Game.Utilities.Logger.Write("Text Renderer: generating render coords for each character");
			}
			while (j < this.Characters.Length)
			{
				if (this.Characters[j] == null)
				{
					if (GlobalData.LoggingActive)
					{
						this.Game.Utilities.Logger.Write("Text Renderer: blank caharcter, skipping");
					}
					j++;
				}
				else
				{
					if (this.Characters[j].Character == ' ' || this.Characters[j].Character == '\n')
					{
						if (GlobalData.LoggingActive)
						{
							this.Game.Utilities.Logger.Write("Text Renderer: white space character, starting new word (" + num7 + ")");
						}
						num5 = j;
						num7++;
					}
					if ((float)(num3 + this.Characters[j].Width) > MaxWidth || this.Characters[j].Character == '\n')
					{
						if (GlobalData.LoggingActive)
						{
							this.Game.Utilities.Logger.Write("Text Renderer: word passes end of line");
						}
						num3 = 0;
						num4 += Font.LineHeight;
						if (num7 > 0)
						{
							j = num5;
						}
						num6++;
						if (GlobalData.LoggingActive)
						{
							this.Game.Utilities.Logger.Write(string.Concat(new object[] { "Text Renderer: new line (", num6, ") at ypos ", num4 }));
						}
						if (this.Characters[num5].Character == ' ')
						{
							this.Characters[j].RenderX = num3 + this.Characters[j].OffsetX;
							this.Characters[j].RenderY = num4 - this.Characters[j].OffsetY;
							if (this.Characters[j].UnderlinePos != -1)
							{
								this.Characters[j].UnderlinePos = num4 + 1;
							}
							this.Characters[j].LineNumber = num6;
							j++;
						}
						num7 = 0;
					}
					if (j < this.Characters.Length)
					{
						this.Characters[j].RenderX = num3 + this.Characters[j].OffsetX;
						this.Characters[j].RenderY = num4 - this.Characters[j].OffsetY;
						if (GlobalData.LoggingActive)
						{
							this.Game.Utilities.Logger.Write(string.Concat(new object[]
							{
								"Text Renderer: character ",
								j,
								" set to X: ",
								this.Characters[j].RenderX,
								", Y: ",
								this.Characters[j].RenderY
							}));
						}
						if (this.Characters[j].UnderlinePos != -1)
						{
							this.Characters[j].UnderlinePos = num4 + 1;
						}
						this.Characters[j].LineNumber = num6;
						num3 += this.Characters[j].Width;
						if (GlobalData.LoggingActive)
						{
							this.Game.Utilities.Logger.Write("Text Renderer: new x position: " + num3);
						}
						j++;
					}
				}
			}
			this.LineCount = num6 + 1;
			switch (this.TextAnchor)
			{
			case AnchorPoints.TopLeft:
				break;
			case AnchorPoints.TopMiddle:
				this.CenterHorizontal();
				return;
			case AnchorPoints.TopRight:
				this.RightAlign();
				return;
			case AnchorPoints.Left:
				this.CenterVertical();
				return;
			case AnchorPoints.Middle:
				this.CenterVertical();
				this.CenterHorizontal();
				return;
			case AnchorPoints.Right:
				this.CenterVertical();
				this.RightAlign();
				return;
			case AnchorPoints.BottomLeft:
				this.BottomAlign();
				return;
			case AnchorPoints.BottomMiddle:
				this.BottomAlign();
				this.CenterHorizontal();
				return;
			case AnchorPoints.BottomRight:
				this.BottomAlign();
				this.RightAlign();
				break;
			default:
				return;
			}
		}

		public float GetTextWidth()
		{
			return (float)this.Characters.Max((CharacterData x) => x.GetBounds().Right);
		}

		public float GetTextMaxY()
		{
			if (this.TextAnchor == AnchorPoints.BottomLeft)
			{
				return this.GetTextHeight();
			}
			if (this.Characters.Length == 0)
			{
				return 0f;
			}
			return (float)this.Characters.Max((CharacterData x) => x.GetBounds().Bottom);
		}

		public float GetTextHeight()
		{
			return this.GetTextBottom() - this.GetTextTop();
		}

		public float GetLineWidth(int StartIndex, int EndIndex)
		{
			List<Rectangle> list = new List<Rectangle>();
			for (int i = StartIndex; i <= EndIndex; i++)
			{
				list.Add(this.Characters[i].GetBounds());
			}
			return (float)list.Max((Rectangle x) => x.Right);
		}

		private void CenterVertical()
		{
			float textHeight = this.GetTextHeight();
			float num = (this.MaxHeight - textHeight) / 2f;
			num -= this.GetTextTop();
			for (int i = 0; i < this.Characters.Length; i++)
			{
				this.Characters[i].RenderY += (int)num;
			}
		}

		private void BottomAlign()
		{
			float textHeight = this.GetTextHeight();
			float num = this.MaxHeight - textHeight;
			num -= this.GetTextTop();
			for (int i = 0; i < this.Characters.Length; i++)
			{
				this.Characters[i].RenderY += (int)num;
			}
		}

		private void CenterHorizontal()
		{
			int LineNumber = 0;
			bool flag = true;
			while (flag)
			{
				flag = false;
				List<CharacterData> list = this.Characters.Where((CharacterData x) => x.LineNumber == LineNumber).ToList<CharacterData>();
				if (list.Count > 0)
				{
					flag = true;
					float num = (float)list.Max((CharacterData x) => x.GetBounds().Right);
					float num2 = (this.MaxWidth - num) / 2f;
					for (int i = 0; i < list.Count; i++)
					{
						list[i].RenderX += (int)num2;
					}
				}
				LineNumber++;
			}
		}

		private void RightAlign()
		{
			int LineNumber = 0;
			bool flag = true;
			while (flag)
			{
				flag = false;
				List<CharacterData> list = this.Characters.Where((CharacterData x) => x.LineNumber == LineNumber).ToList<CharacterData>();
				if (list.Count > 0)
				{
					flag = true;
					float num = (float)list.Max((CharacterData x) => x.GetBounds().Right);
					float num2 = this.MaxWidth - num;
					for (int i = 0; i < list.Count; i++)
					{
						list[i].RenderX += (int)num2;
					}
				}
				LineNumber++;
			}
		}

		internal bool IsEmpty()
		{
			return this.Characters == null || this.Characters.Length == 0;
		}

		private CharacterData[] Characters;

		public float MaxWidth;

		public float MaxHeight;

		public Color DefaultColour = Color.FromArgb(255, 255, 255, 255);

		public GameFont DefaultFont;

		public AnchorPoints TextAnchor;

		private GameBase Game;

		private int LineCount;

		private float LineHeight;

		public bool IgnoreFormatting;
	}
}
