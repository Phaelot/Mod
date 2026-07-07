using System;
using System.Collections.Generic;
using System.Drawing;

namespace SovereigntyTK.UI.Text
{
	public class FontGlyphData
	{
		public FontGlyphData()
		{
			this.Kerning = new Dictionary<FontGlyphData, int>();
		}

		public Bitmap GlyphImage;

		public int OffsetX;

		public int OffsetY;

		public int AdvanceX;

		public int Height;

		public uint GlyphIndex;

		public Dictionary<FontGlyphData, int> Kerning;
	}
}
