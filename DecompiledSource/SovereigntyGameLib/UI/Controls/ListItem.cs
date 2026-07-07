using System;
using System.Drawing;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI.Controls
{
	public class ListItem<T>
	{
		public ListItem(ControlList<T> Parent)
		{
			this.Parent = Parent;
		}

		public void SetFontDetails(string FontName, PositionData FontSize, Color FontColour)
		{
			this.FontName = FontName;
			this.FontColour = FontColour;
			this.FontSize = FontSize;
			if (this.InternalText != null)
			{
				this.InternalText.FontName = FontName;
				this.InternalText.SetFontSize((int)FontSize.Value, FontSize.UnitType);
				this.InternalText.Colour = FontColour;
			}
		}

		internal void RecreateContainer(UIControl Owner, int Width, int Height)
		{
			this.DestroyContainer();
			this.Container = new ControlContainer(Owner.Game);
			Owner.AddChild(this.Container);
			this.Container.SetBounds(0f, 0f, (float)Width, (float)Height, UIUnits.PixelScaled);
			this.Container.MouseInputType = MouseInputTypes.Forced;
		}

		public void UpdateData(T Data, bool CreateText)
		{
			this.Data = Data;
			if (CreateText)
			{
				this.InternalText = new ControlText(this.Container.Game);
				this.Container.AddChild(this.InternalText);
				this.InternalText.SetBounds(0f, 0f, this.Container.Sprite.Bounds.Width, this.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
				this.InternalText.FontName = this.FontName;
				this.InternalText.SetFontSize((int)this.FontSize.Value, this.FontSize.UnitType);
				this.InternalText.Colour = this.FontColour;
				GameText gameText;
				if (Data is GameText)
				{
					gameText = Data as GameText;
				}
				else
				{
					gameText = GameText.CreateFromLiteral(Data.ToString());
				}
				this.InternalText.SetTextData(gameText);
			}
		}

		public void Dispose()
		{
			this.DestroyContainer();
		}

		private void DestroyContainer()
		{
			if (this.Container != null)
			{
				this.Container.ParentControl.RemoveChild(this.Container);
				this.Container.Dispose();
				this.Container = null;
			}
		}

		public T Data;

		public ControlContainer Container;

		public ControlList<T> Parent;

		private string FontName;

		private PositionData FontSize;

		private Color FontColour;

		private ControlText InternalText;
	}
}
