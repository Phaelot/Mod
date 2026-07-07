using System;
using System.Drawing;

namespace SovereigntyTK.UI.Text
{
	public class CharacterData
	{
		public bool ContainsPoint(int X, int Y)
		{
			return new Rectangle(this.RenderX, this.RenderY, this.Width, this.Height).Contains(X, Y);
		}

		public Rectangle GetBounds()
		{
			return new Rectangle(this.RenderX, this.RenderY, this.Width, this.Height);
		}

		public char Character;

		public Bitmap CharImage;

		public int OffsetX;

		public int OffsetY;

		public int Width;

		public int Height;

		public int RenderX;

		public int RenderY;

		public int LineNumber;

		public Color CharColour;

		public int UnderlinePos;

		public int RealWidth;
	}
}
