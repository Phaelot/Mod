using System;
using System.Drawing;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI.Map
{
	public class MapColour
	{
		public MapColour(Color Colour, GameText Description)
		{
			this.Colour = Colour;
			this.Description = Description;
		}

		public Color Colour;

		public GameText Description;
	}
}
