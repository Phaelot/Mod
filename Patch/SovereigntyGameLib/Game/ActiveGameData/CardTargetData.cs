using System;
using System.Drawing;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class CardTargetData
	{
		public CardTargetTypes TargetType;

		public GameText TargetDescription;

		public Point Tile;

		public WorkingUnit Unit;

		public CardEffect Card;
	}
}
