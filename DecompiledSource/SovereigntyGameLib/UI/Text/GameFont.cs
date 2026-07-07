// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Text.GameFont
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SharpFont;
using SovereigntyTK;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Text
{
	public class GameFont
	{
		private static Library FontLibrary;

		private static List<FontData> ActiveFonts;

		private static List<GameFont> FontList;

		private Face FontFace;

		public Dictionary<char, FontGlyphData> Glyphs;

		public string FontName;

		public int FontSize;

		private string Filename;

		public int UnderlinePosition => FontFace.UnderlinePosition >> 6;

		public int LineHeight => FontFace.Size.Metrics.Height.Value >> 6;

		public static GameFont GetFont(GameBase Game, string FontName, string Filename, int PixelSize)
		{
			if (FontList == null)
			{
				FontList = new List<GameFont>();
			}
			GameFont gameFont = FontList.FirstOrDefault((GameFont x) => x.FontName == FontName && x.Filename == Filename && x.FontSize == PixelSize);
			if (gameFont == null)
			{
				gameFont = new GameFont(Game, FontName, Filename, PixelSize);
				FontList.Add(gameFont);
			}
			return gameFont;
		}

		private GameFont(GameBase Game, string FontName, string Filename, int PixelSize)
		{
			this.FontName = FontName;
			FontSize = PixelSize;
			this.Filename = Filename;
			if (FontLibrary == null)
			{
				FontLibrary = new Library();
			}
			if (ActiveFonts == null)
			{
				ActiveFonts = new List<FontData>();
			}
			string FullFilename = Game.Utilities.FileSystem.ConvertFilename(Filename, FileTypes.Application);
			FontData fontData = ActiveFonts.FirstOrDefault((FontData x) => x.Filename == FullFilename && x.PixelSize == PixelSize);
			if (fontData != null)
			{
				FontFace = fontData.FontFace;
			}
			else
			{
				FontFace = new Face(FontLibrary, FullFilename);
				FontFace.SetPixelSizes(0u, (uint)PixelSize);
				fontData = new FontData
				{
					Filename = FullFilename,
					PixelSize = PixelSize,
					FontFace = FontFace
				};
				ActiveFonts.Add(fontData);
			}
			Glyphs = new Dictionary<char, FontGlyphData>();
		}

		public Size MeasureString(string Text)
		{
			int num = 0;
			for (int i = 0; i < Text.Length; i++)
			{
				FontGlyphData glyph = GetGlyph(Text[i]);
				int num2 = 0;
				if (i < Text.Length - 1)
				{
					num2 = GetKerning(Text[i], Text[i + 1]);
				}
				num += glyph.AdvanceX + num2;
			}
			return new Size(num, FontFace.Height);
		}

		public int GetKerning(char Left, char Right)
		{
			FontGlyphData glyph = GetGlyph(Left);
			FontGlyphData glyph2 = GetGlyph(Right);
			if (glyph.Kerning.ContainsKey(glyph2))
			{
				return glyph.Kerning[glyph2];
			}
			int num = FontFace.GetKerning(glyph.GlyphIndex, glyph2.GlyphIndex, KerningMode.Default).X.Value >> 6;
			glyph.Kerning.Add(glyph2, num);
			return num;
		}

		public FontGlyphData GetGlyph(char c)
		{
			FontGlyphData value = null;
			Glyphs.TryGetValue(c, out value);
			if (value != null)
			{
				return value;
			}
			uint charIndex = FontFace.GetCharIndex(c);
			FontFace.LoadGlyph(charIndex, LoadFlags.Render, LoadTarget.Light);
			value = new FontGlyphData();
			value.GlyphIndex = charIndex;
			if (char.IsWhiteSpace(c))
			{
				value.OffsetX = 0;
				value.OffsetY = 0;
				value.AdvanceX = FontFace.Glyph.Metrics.HorizontalAdvance.Value >> 6;
				value.Height = FontFace.Glyph.Metrics.Height.Value >> 6;
			}
			else
			{
				value.OffsetX = FontFace.Glyph.BitmapLeft;
				value.OffsetY = FontFace.Glyph.BitmapTop;
				value.AdvanceX = FontFace.Glyph.Metrics.HorizontalAdvance.Value >> 6;
				value.GlyphImage = FontFace.Glyph.Bitmap.ToGdipBitmap(Color.FromArgb(255, 0, 0, 0));
				value.Height = FontFace.Glyph.Metrics.Height.Value >> 6;
			}
			Glyphs.Add(c, value);
			return value;
		}
	}
}