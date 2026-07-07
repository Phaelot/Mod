using System;
using System.Drawing;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	public class SpriteButton
	{
		public event SpriteButtonDelegate OnClick;

		public SpriteButton(Sovereignty Game, string Img, PointF Location, WorkingRealm Target, GameText Tooltip)
		{
			this.Game = Game;
			this.Img1 = Img + "_normal.png";
			this.Img2 = Img + "_mouseover.png";
			this.Img3 = Img + "_pressed.png";
			this.Location = Location;
			this.Target = Target;
			this.Sprite = Game.Utilities.SpriteManager.CreateSprite(this.Img1, true);
			this.Sprite.SetSize(45f, 45f);
			this.Sprite.SetPosition(Location.X, Location.Y);
			this.Sprite.OnMouseEnter += this.Sprite_OnMouseEnter;
			this.Sprite.OnMouseLeave += this.Sprite_OnMouseLeave;
			this.Sprite.OnMouseDown += this.Sprite_OnMouseDown;
			this.Sprite.OnMouseUp += this.Sprite_OnMouseUp;
			this.Sprite.OnClick += this.Sprite_OnClick;
			this.Sprite.Tooltip = Tooltip;
			this.Sprite.RenderOnTop = true;
		}

		public void Dispose()
		{
			this.Sprite.Dispose(false);
			this.OnClick = null;
			this.Game = null;
			this.Target = null;
		}

		private void Sprite_OnClick(GLBaseSprite Sprite)
		{
			if (this.OnClick != null)
			{
				this.OnClick(this);
			}
		}

		private void Sprite_OnMouseUp(GLBaseSprite Sprite)
		{
			Sprite.SetImage(this.Img1);
		}

		private void Sprite_OnMouseDown(GLBaseSprite Sprite)
		{
			Sprite.SetImage(this.Img3);
		}

		private void Sprite_OnMouseLeave(GLBaseSprite Sprite)
		{
			Sprite.SetImage(this.Img1);
		}

		private void Sprite_OnMouseEnter(GLBaseSprite Sprite)
		{
			Sprite.SetImage(this.Img2);
		}

		private GLSprite Sprite;

		private PointF Location;

		private string Img1;

		private string Img2;

		private string Img3;

		private Sovereignty Game;

		public WorkingRealm Target;
	}
}
