using System;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI.Controls
{
	public class MenuItem
	{
		public event MenuItemDelegate OnClick;

		public MenuItem(GameBase Game)
		{
			this.Game = Game;
		}

		public void Dispose()
		{
			if (this.SubMenu != null)
			{
				this.SubMenu.Dispose();
			}
			this.OnClick = null;
		}

		public void Update(UIControl Container)
		{
			if (this.IconImageFile != null)
			{
				ControlImage controlImage = new ControlImage(this.Game);
				controlImage.SetBounds(Container.Sprite.Bounds.Height * 0.05f, Container.Sprite.Bounds.Height * 0.05f, Container.Sprite.Bounds.Height * 0.9f, Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
				controlImage.SetImageFile(this.IconImageFile);
				controlImage.BubbleMouseovers = true;
				Container.AddChild(controlImage);
			}
			ControlText controlText = new ControlText(this.Game);
			controlText.SetBounds(Container.Sprite.Bounds.Height, 0f, Container.Sprite.Bounds.Width - Container.Sprite.Bounds.Height, Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
			controlText.SetFontSize((int)(Container.Sprite.Bounds.Height * 0.6f), UIUnits.PixelScaled);
			controlText.FontName = "Trebuchet MS";
			controlText.TextAnchor = AnchorPoints.Left;
			controlText.SetTextData(this.Text);
			Container.AddChild(controlText);
			if (this.SubMenu != null)
			{
				ControlImage controlImage2 = new ControlImage(this.Game);
				controlImage2.SetBounds(Container.Sprite.Bounds.Width - Container.Sprite.Bounds.Height * 0.95f, Container.Sprite.Bounds.Height * 0.05f, Container.Sprite.Bounds.Height * 0.9f, Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
				controlImage2.SetImageFile("Data\\Images\\HUD\\Menu.png");
				controlImage2.BubbleMouseovers = true;
				Container.AddChild(controlImage2);
			}
		}

		public void HandleClicked()
		{
			if (this.OnClick != null)
			{
				this.OnClick(this);
			}
		}

		public GameBase Game;

		public GameText Text;

		public ControlMenu SubMenu;

		public string IconImageFile;

		public object CustomData;
	}
}
