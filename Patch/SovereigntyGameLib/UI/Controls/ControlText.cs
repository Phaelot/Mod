// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Controls.ControlText
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class ControlText : UIControl
	{
		private TextRenderer Renderer;

		private string m_Text;

		private Color m_Colour = Color.White;

		private Bitmap Canvas;

		private AnchorPoints m_TextAnchor;

		private bool m_Bold;

		private string m_FontName;

		private bool SuperSample;

		private bool m_ScrollingEnabled;

		private bool ScrollPending;

		public bool AdjustTextureCoords = true;

		private ControlButton ScrollUpButton;

		private ControlButton ScrollDownButton;

		private ControlImage ScrollBarImage;

		private ControlImage ScrollCurrentImage;

		private PositionData OriginalFontSize;

		private int FontSize;

		private GameFont DefaultFont;

		public bool IgnoreFormatting;

		private float CurrentScrollValue;

		private GameText Text;

		private List<GameText> TextList;

		private string LiteralText;

		private bool UseLiteral;

		private bool TextModified;

		public bool OutputDebugRender;

		public bool ScrollingEnabled
		{
			get
			{
				return m_ScrollingEnabled;
			}
			set
			{
				if (m_ScrollingEnabled != value)
				{
					m_ScrollingEnabled = value;
					UpdateScrollingStatus();
					TextModified = true;
				}
			}
		}

		public string FontName
		{
			get
			{
				return m_FontName;
			}
			set
			{
				if (!(m_FontName == value))
				{
					m_FontName = value;
					TextModified = true;
				}
			}
		}

		public Color Colour
		{
			get
			{
				return m_Colour;
			}
			set
			{
				if (!(m_Colour == value))
				{
					m_Colour = value;
					SetBlendColour(m_Colour);
					TextModified = true;
				}
			}
		}

		public AnchorPoints TextAnchor
		{
			get
			{
				return m_TextAnchor;
			}
			set
			{
				if (m_TextAnchor != value)
				{
					m_TextAnchor = value;
					TextModified = true;
				}
			}
		}

		public bool Bold
		{
			get
			{
				return m_Bold;
			}
			set
			{
				if (m_Bold != value)
				{
					m_Bold = value;
					TextModified = true;
				}
			}
		}

		public event Action OnTextUpdated;

		public void SetFontSize(int Value, UIUnits Unit)
		{
			OriginalFontSize = new PositionData(Value, X: false);
			OriginalFontSize.UnitType = Unit;
			FontSize = ConvertSinglePositionData(OriginalFontSize);
			TextModified = true;
		}

		public void SetFontSize(string Value)
		{
			OriginalFontSize = new PositionData(Value, X: false);
			FontSize = ConvertSinglePositionData(OriginalFontSize);
			TextModified = true;
		}

		public ControlText(GameBase Game)
			: base(Game)
		{
			MouseInputType = MouseInputTypes.None;
			SetBlendColour(m_Colour);
		}

		private void UpdateScrollingStatus()
		{
			if (m_ScrollingEnabled)
			{
				AcceptMouseWheel = true;
				MouseInputType = MouseInputTypes.Forced;
				ScrollUpButton = new ControlButton(Game);
				AddChild(ScrollUpButton);
				ScrollUpButton.SetBounds(0f, 0f, 20f, 20f, UIUnits.Pixel);
				ScrollUpButton.SetAnchor(AnchorPoints.TopRight);
				ScrollUpButton.SetImageFiles("Data\\Images\\HUD\\Economy\\arrow_up_normal.png", "Data\\Images\\HUD\\Economy\\arrow_up_mouseover.png", "Data\\Images\\HUD\\Economy\\arrow_up_pressed.png");
				ScrollUpButton.AutoClick = true;
				ScrollUpButton.OnClick += ScrollUpButton_OnClick;
				ScrollDownButton = new ControlButton(Game);
				AddChild(ScrollDownButton);
				ScrollDownButton.SetBounds(0f, 0f, 20f, 20f, UIUnits.Pixel);
				ScrollDownButton.SetAnchor(AnchorPoints.BottomRight);
				ScrollDownButton.SetImageFiles("Data\\Images\\HUD\\Economy\\arrow_down_normal.png", "Data\\Images\\HUD\\Economy\\arrow_down_mouseover.png", "Data\\Images\\HUD\\Economy\\arrow_down_pressed.png");
				ScrollDownButton.AutoClick = true;
				ScrollDownButton.OnClick += ScrollDownButton_OnClick;
				ScrollBarImage = new ControlImage(Game);
				AddChild(ScrollBarImage);
				ScrollBarImage.SetImageFile("Data\\Images\\HUD\\Economy\\arrow_line_v.png");
				ScrollCurrentImage = new ControlImage(Game);
				AddChild(ScrollCurrentImage);
				ScrollCurrentImage.SetImageFile("Data\\Images\\HUD\\Realmselect\\check_normal.png");
				ScrollCurrentImage.SetSize(10f, 10f, UIUnits.Pixel);
			}
			else
			{
				if (ScrollBarImage != null)
				{
					RemoveChild(ScrollBarImage);
					RemoveChild(ScrollCurrentImage);
					RemoveChild(ScrollUpButton);
					RemoveChild(ScrollDownButton);
					ScrollBarImage.Dispose();
					ScrollCurrentImage.Dispose();
					ScrollUpButton.Dispose();
					ScrollDownButton.Dispose();
					ScrollBarImage = null;
					ScrollCurrentImage = null;
					ScrollUpButton = null;
					ScrollDownButton = null;
				}
				MouseInputType = MouseInputTypes.None;
				AcceptMouseWheel = false;
			}
		}

		private void ScrollDownButton_OnClick(UIControl Control)
		{
			ScrollText(20);
		}

		private void ScrollUpButton_OnClick(UIControl Control)
		{
			ScrollText(-20);
		}

		public override void HandleMousewheelUp(float LocalX, float LocalY)
		{
			base.HandleMousewheelUp(LocalX, LocalY);
			ScrollText(-20);
		}

		public override void HandleMousewheelDown(float LocalX, float LocalY)
		{
			base.HandleMousewheelDown(LocalX, LocalY);
			ScrollText(20);
		}

		public float GetTextWidth()
		{
			if (Renderer == null)
			{
				return 0f;
			}
			return Renderer.GetTextWidth();
		}

		public float GetTextHeight()
		{
			if (TextModified)
			{
				RedrawText();
			}
			if (Renderer == null)
			{
				return 0f;
			}
			return Renderer.GetTextHeight();
		}

		public override void Dispose()
		{
			if (Canvas != null)
			{
				Canvas.Dispose();
				Sprite.CurrentTexture.Dispose();
				Sprite.CurrentTexture = null;
				Canvas = null;
			}
			if (Renderer != null)
			{
				Renderer.Dispose();
			}
		}

		protected override void ReclaculateBounds()
		{
			SizeF size = Sprite.Bounds.Size;
			base.ReclaculateBounds();
			if (size != Sprite.Bounds.Size)
			{
				FontSize = ConvertSinglePositionData(OriginalFontSize);
				TextModified = true;
			}
		}

		public string GetCurrentText()
		{
			if (UseLiteral)
			{
				if (LiteralText == null || LiteralText == "")
				{
					return null;
				}
				return LiteralText;
			}
			if (Text != null)
			{
				return Text.GetActualText(Game);
			}
			if (TextList != null)
			{
				string text = "";
				{
					foreach (GameText text2 in TextList)
					{
						text += text2.GetActualText(Game);
					}
					return text;
				}
			}
			return null;
		}

		protected override void ParseElement(XElement Element)
		{
			switch (Element.Name.LocalName)
			{
				case "fontname":
					FontName = Element.Value;
					break;
				case "fontsize":
					SetFontSize(Element.Value);
					break;
				case "fontcolour":
					Colour = ParseColour(Element.Value);
					break;
				case "textanchor":
					TextAnchor = ParseAnchorPoint(Element.Value);
					break;
				case "text":
					SetTextData(GameText.CreateLocalised(Element.Value));
					break;
				case "bold":
					Bold = bool.Parse(Element.Value);
					break;
				case "scroll":
					ScrollingEnabled = bool.Parse(Element.Value);
					break;
				default:
					base.ParseElement(Element);
					break;
				case "wordwrap":
				case "rightalign":
				case "fontmult":
					break;
			}
		}

		public void SetLiteralText(string NewText)
		{
			UseLiteral = true;
			Text = null;
			TextList = null;
			if (LiteralText != NewText)
			{
				LiteralText = NewText;
				CurrentScrollValue = 0f;
				TextModified = true;
			}
		}

		public void SetTextData(List<GameText> GameTextList)
		{
			Text = null;
			LiteralText = null;
			TextList = GameTextList;
			CurrentScrollValue = 0f;
			TextModified = true;
		}

		public void SetTextData(GameText NewText)
		{
			TextList = null;
			LiteralText = null;
			Text = NewText;
			CurrentScrollValue = 0f;
			TextModified = true;
		}

		public void ResetScroll()
		{
			CurrentScrollValue = 0f;
			TextModified = true;
		}

		public override void Render(GLShader Shader, float ElapsedTime)
		{
			if (TextModified)
			{
				RedrawText();
			}
			base.Render(Shader, ElapsedTime);
		}

		private void RedrawText()
		{
			TextModified = false;
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " modified, redrawing");
			}
			if (GetCurrentText() == null)
			{
				if (Canvas != null)
				{
					if (GlobalData.LoggingActive)
					{
						Game.Utilities.Logger.Write("Text control " + ID + " text is null, drawing blank image");
					}
					Graphics graphics = Graphics.FromImage(Canvas);
					graphics.Clear(Color.FromArgb(0, 0, 0, 0));
					graphics.Dispose();
					Sprite.CurrentTexture.BindBitmap(Canvas);
				}
				return;
			}
			if (Renderer == null)
			{
				if (GlobalData.LoggingActive)
				{
					Game.Utilities.Logger.Write("Text control " + ID + " creating new text renderer");
				}
				Renderer = new TextRenderer(Game);
				Renderer.IgnoreFormatting = IgnoreFormatting;
			}
			string currentText = GetCurrentText();
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " text: " + currentText);
			}
			int num = FontSize;
			if (SuperSample)
			{
				num *= 2;
			}
			if (DefaultFont == null || DefaultFont.FontSize != num)
			{
				string font = Game.Utilities.FontConvertor.GetFont(m_FontName, Bold);
				DefaultFont = GameFont.GetFont(Game, m_FontName, "Data\\Fonts\\" + font, num);
				if (GlobalData.LoggingActive)
				{
					Game.Utilities.Logger.Write("Text control " + ID + " font modified, creating new font. Name: " + font + ", Size: " + FontSize);
				}
			}
			Renderer.MaxWidth = Sprite.Bounds.Width;
			if (SuperSample)
			{
				Renderer.MaxWidth *= 2f;
			}
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " renderer max width set to " + Renderer.MaxWidth);
			}
			if (m_ScrollingEnabled)
			{
				Renderer.MaxWidth -= ScrollUpButton.Sprite.Bounds.Width * 2f;
			}
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " renderer max width set to " + Renderer.MaxWidth);
			}
			Renderer.MaxHeight = Sprite.Bounds.Height;
			if (SuperSample)
			{
				Renderer.MaxHeight *= 2f;
			}
			Renderer.DefaultColour = m_Colour;
			Renderer.DefaultFont = DefaultFont;
			Renderer.TextAnchor = m_TextAnchor;
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " renderer max height set to " + Renderer.MaxHeight);
			}
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " passsing text to renderer");
			}
			Renderer.SetText(currentText, Renderer.DefaultFont, m_Colour, Renderer.MaxWidth, Renderer.MaxHeight);
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " checking canvas");
			}
			CheckCanvas(Renderer.MaxWidth, Renderer.GetTextHeight());
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " starting render");
			}
			Renderer.Render(Canvas);
			if (OutputDebugRender)
			{
				Bitmap bitmap = new Bitmap(Canvas.Width, Canvas.Height);
				Graphics graphics2 = Graphics.FromImage(bitmap);
				graphics2.Clear(Color.Black);
				graphics2.DrawImage(Canvas, 0, 0);
				graphics2.Dispose();
				bitmap.Save(Game.Utilities.FileSystem.OpenFile("TestOutput.png", FileTypes.User, FileModes.ReadWrite), ImageFormat.Png);
				bitmap.Dispose();
			}
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " updating visible area");
			}
			Sprite.CurrentTexture.BindBitmap(Canvas, GenerateMipMap: true);
			Sprite.CurrentTexture.SetFilter(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
			Sprite.CurrentTexture.SetWrapMode(TextureWrapMode.ClampToBorder);
			UpdateVisibleArea();
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " scrolling text to bottom");
			}
			if (ScrollPending)
			{
				ScrollToBottom();
			}
			if (this.OnTextUpdated != null)
			{
				this.OnTextUpdated();
			}
		}

		private float GetMaxScroll()
		{
			if (Renderer == null)
			{
				return 0f;
			}
			float textMaxY = Renderer.GetTextMaxY();
			textMaxY -= Sprite.Bounds.Height;
			if (SuperSample)
			{
				textMaxY -= Sprite.Bounds.Height;
			}
			if (textMaxY < 0f)
			{
				textMaxY = 0f;
			}
			return textMaxY;
		}

		public void ScrollText(int Value)
		{
			CurrentScrollValue += Value;
			float maxScroll = GetMaxScroll();
			if (CurrentScrollValue < 0f)
			{
				CurrentScrollValue = 0f;
			}
			if (CurrentScrollValue > maxScroll)
			{
				CurrentScrollValue = maxScroll;
			}
			UpdateVisibleArea();
		}

		private void UpdateVisibleArea()
		{
			if (Canvas == null || Renderer.IsEmpty())
			{
				return;
			}
			float maxScroll = GetMaxScroll();
			if (CurrentScrollValue > maxScroll)
			{
				CurrentScrollValue = maxScroll;
			}
			float num = 0f;
			float num2 = Sprite.Bounds.Width;
			if (SuperSample)
			{
				num2 += Sprite.Bounds.Width;
			}
			float currentScrollValue = CurrentScrollValue;
			float num3 = currentScrollValue + Sprite.Bounds.Height;
			if (SuperSample)
			{
				num3 += Sprite.Bounds.Height;
			}
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " new visible area: L: " + num + ", R: " + num2 + ", T: " + currentScrollValue + ", B: " + num3);
			}
			num /= (float)Canvas.Width;
			num2 /= (float)Canvas.Width;
			currentScrollValue /= (float)Canvas.Height;
			num3 /= (float)Canvas.Height;
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " normalised coords: L: " + num + ", R: " + num2 + ", T: " + currentScrollValue + ", B: " + num3);
			}
			Sprite.Vertices[0].TexCoords = new Vector2(num, num3);
			Sprite.Vertices[1].TexCoords = new Vector2(num2, currentScrollValue);
			Sprite.Vertices[2].TexCoords = new Vector2(num, currentScrollValue);
			Sprite.Vertices[3].TexCoords = new Vector2(num, num3);
			Sprite.Vertices[4].TexCoords = new Vector2(num2, num3);
			Sprite.Vertices[5].TexCoords = new Vector2(num2, currentScrollValue);
			Sprite.UpdateVertices();
			if (ScrollBarImage != null)
			{
				ScrollBarImage.Visible = maxScroll > 0f;
				ScrollUpButton.Visible = maxScroll > 0f;
				ScrollDownButton.Visible = maxScroll > 0f;
				ScrollCurrentImage.Visible = maxScroll > 0f;
				if (maxScroll > 0f)
				{
					ScrollBarImage.SetPositionX(7.5f, UIUnits.Pixel);
					ScrollBarImage.SetWidth(5f, UIUnits.Pixel);
					ScrollBarImage.SetHeight(Sprite.Bounds.Height - ScrollUpButton.Sprite.Bounds.Height, UIUnits.PixelScaled);
					ScrollBarImage.SetAnchor(AnchorPoints.Right);
					float num4 = CurrentScrollValue / maxScroll;
					float height = ScrollCurrentImage.Sprite.Bounds.Height;
					float num5 = ScrollBarImage.Sprite.Bounds.Height - height;
					float num6 = (num5 - height) * num4 + height;
					num6 -= ScrollBarImage.Sprite.Bounds.Height / 2f;
					ScrollCurrentImage.SetPosition(5f, num6, UIUnits.PixelScaled);
					ScrollCurrentImage.SetAnchor(AnchorPoints.Right);
				}
			}
		}

		private void CheckCanvas(float Width, float Height)
		{
			if (Width < Sprite.Bounds.Width)
			{
				Width = Sprite.Bounds.Width;
			}
			if (Height < Sprite.Bounds.Height)
			{
				Height = Sprite.Bounds.Height;
			}
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " canvas requires width: " + Width + ", height: " + Height);
			}
			if (Canvas != null && (float)Canvas.Width >= Width && (float)Canvas.Height >= Height)
			{
				if (GlobalData.LoggingActive)
				{
					Game.Utilities.Logger.Write("Text control " + ID + " canavas is already large enough (width: " + Canvas.Width + ", height: " + Canvas.Height + ")");
				}
				return;
			}
			if (Canvas != null)
			{
				if (GlobalData.LoggingActive)
				{
					Game.Utilities.Logger.Write("Text control " + ID + " disposing canvas");
				}
				Canvas.Dispose();
				Sprite.CurrentTexture.Dispose();
				Sprite.CurrentTexture = null;
				Canvas = null;
			}
			int num = 2;
			int num2 = 2;
			while ((float)num < Width)
			{
				num *= 2;
			}
			while ((float)num2 < Height)
			{
				num2 *= 2;
			}
			if (GlobalData.LoggingActive)
			{
				Game.Utilities.Logger.Write("Text control " + ID + " creating new canavas, width: " + num + ", height: " + num2);
			}
			Canvas = new Bitmap(num, num2);
			Sprite.CurrentTexture = new GLTexture();
		}

		internal void ForceTextUpdate()
		{
			TextModified = true;
		}

		public void ScrollToBottom()
		{
			if (Renderer == null)
			{
				ScrollPending = true;
				return;
			}
			ScrollPending = false;
			CurrentScrollValue = GetMaxScroll();
			UpdateVisibleArea();
		}
	}
}