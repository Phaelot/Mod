// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLSprite
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Utility
{
	public class GLSprite : GLBaseSprite
	{
		internal RectangleF Bounds;

		private Vector4 CurrentCoords;

		public bool IgnoreMouseClicks;

		public bool IgnoreMouse;

		private List<MouseButton> ActiveButtons;

		public GameText Tooltip;

		public List<GameText> TooltipList;

		public event SpriteDelegate OnMouseEnter;

		public event SpriteDelegate OnMouseLeave;

		public event SpriteDelegate OnMouseDown;

		public event SpriteDelegate OnMouseUp;

		public event SpriteDelegate OnClick;

		public event SpriteDelegate OnRightClick;

		public GLSprite(GameBase Game, GLSpriteBatch Batch, SpriteManager Manager)
			: base(Game, Batch, Manager)
		{
			CurrentCoords = new Vector4(0f, 0f, 1f, 1f);
			UpdateTextureCoords();
			ActiveButtons = new List<MouseButton>();
			for (int i = 0; i < 4; i++)
			{
				Batch.Vertices[BaseVertex + i].SetAlpha(Alpha);
			}
		}

		public void SetSize(float Width, float Height)
		{
			Bounds.Width = Width;
			Bounds.Height = Height;
			UpdateVertices();
		}

		public void SetPosition(float X, float Y)
		{
			Bounds.X = X;
			Bounds.Y = Y;
			UpdateVertices();
		}

		public void ForceVertices(Vector3 Point1, Vector3 Point2, Vector3 Point3, Vector3 Point4)
		{
			Batch.Vertices[BaseVertex].Position = Point1;
			Batch.Vertices[BaseVertex + 1].Position = Point2;
			Batch.Vertices[BaseVertex + 2].Position = Point3;
			Batch.Vertices[BaseVertex + 3].Position = Point4;
			for (int i = 0; i < 4; i++)
			{
				Batch.Vertices[BaseVertex + i].SetAlpha(Alpha);
			}
			Batch.BufferModified = true;
		}

		public override void UpdateVertices()
		{
			float num = Bounds.X - Bounds.Width * 0.5f;
			float num2 = Bounds.Y - Bounds.Height * 0.5f;
			float x = num + Bounds.Width;
			float z = num2 + Bounds.Height;
			Batch.Vertices[BaseVertex].Position = new Vector3(num, 0f, num2);
			Batch.Vertices[BaseVertex + 1].Position = new Vector3(x, 0f, num2);
			Batch.Vertices[BaseVertex + 2].Position = new Vector3(num, 0f, z);
			Batch.Vertices[BaseVertex + 3].Position = new Vector3(x, 0f, z);
			for (int i = 0; i < 4; i++)
			{
				Batch.Vertices[BaseVertex + i].SetAlpha(Alpha);
			}
			Batch.BufferModified = true;
		}

		public override void Dispose(bool DisposeBatch = false)
		{
			this.OnMouseDown = null;
			this.OnMouseUp = null;
			this.OnMouseEnter = null;
			this.OnMouseLeave = null;
			this.OnClick = null;
			this.OnRightClick = null;
			base.Dispose(DisposeBatch);
		}

		public override void UpdateTextureCoords()
		{
			Batch.Vertices[BaseVertex].TexCoords = new Vector2(CurrentCoords.X, CurrentCoords.Y);
			Batch.Vertices[BaseVertex + 1].TexCoords = new Vector2(CurrentCoords.Z, CurrentCoords.Y);
			Batch.Vertices[BaseVertex + 2].TexCoords = new Vector2(CurrentCoords.X, CurrentCoords.W);
			Batch.Vertices[BaseVertex + 3].TexCoords = new Vector2(CurrentCoords.Z, CurrentCoords.W);
			Batch.BufferModified = true;
		}

		internal void SetTextureCoords(Vector4 Coords)
		{
			CurrentCoords = Coords;
			UpdateTextureCoords();
		}

		internal void SetTextureCoords(string IconName)
		{
			CurrentCoords = Batch.GetCoords(IconName);
			UpdateTextureCoords();
		}

		internal void SetTextureCoords(int Index)
		{
			CurrentCoords = Batch.GetCoords(Index);
			UpdateTextureCoords();
		}

		internal bool PointInside(float x, float z)
		{
			float num = Bounds.X - Bounds.Width * 0.5f;
			float num2 = Bounds.Y - Bounds.Height * 0.5f;
			float num3 = num + Bounds.Width;
			float num4 = num2 + Bounds.Height;
			if (x < num)
			{
				return false;
			}
			if (x > num3)
			{
				return false;
			}
			if (z < num2)
			{
				return false;
			}
			if (z > num4)
			{
				return false;
			}
			return true;
		}

		public override void HandleMouseLeave()
		{
			if (this.OnMouseLeave != null)
			{
				this.OnMouseLeave(this);
			}
			if (this.OnMouseUp != null)
			{
				this.OnMouseUp(this);
			}
			ActiveButtons.Clear();
			GameBase game = Game;
			object[] args = new object[1];
			game.FireEvent("TooltipChanged", args);
		}

		public override void HandleMouseEnter()
		{
			if (this.OnMouseEnter != null)
			{
				this.OnMouseEnter(this);
			}
			if (Tooltip != null)
			{
				Game.FireEvent("TooltipChanged", Tooltip);
			}
			else
			{
				Game.FireEvent("TooltipChanged", TooltipList);
			}
		}

		public override void HandleMouseDown(MouseButton Button)
		{
			if (!ActiveButtons.Contains(Button))
			{
				ActiveButtons.Add(Button);
				if (Button == MouseButton.Left && this.OnMouseDown != null)
				{
					this.OnMouseDown(this);
				}
			}
		}

		public override void HandleMouseUp(MouseButton Button)
		{
			if (!ActiveButtons.Contains(Button))
			{
				return;
			}
			ActiveButtons.Remove(Button);
			if (Button == MouseButton.Left)
			{
				if (this.OnMouseUp != null)
				{
					this.OnMouseUp(this);
				}
				if (this.OnClick != null)
				{
					this.OnClick(this);
				}
			}
			if (Button == MouseButton.Right && this.OnRightClick != null)
			{
				this.OnRightClick(this);
			}
		}
	}
}