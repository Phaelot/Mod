// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Controls.ControlInput
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class ControlInput : UIControl
	{
		private ControlText Text;

		private ControlImage CursorImage;

		public string CurrentText;

		public List<char> BlockedKeys;

		private float Timer;

		public bool Filename;

		public string FontName
		{
			get
			{
				return Text.FontName;
			}
			set
			{
				Text.FontName = value;
			}
		}

		public bool Bold
		{
			get
			{
				return Text.Bold;
			}
			set
			{
				Text.Bold = value;
			}
		}

		public AnchorPoints TextAnchor
		{
			get
			{
				return Text.TextAnchor;
			}
			set
			{
				Text.TextAnchor = value;
			}
		}

		public event ControlDelegate OnReturn;

		public event EventHandler<KeyboardKeyEventArgs> OnFunctionKey;

		public event EventHandler<KeyboardKeyEventArgs> OnArrowKey;

		public ControlInput(GameBase Game)
			: base(Game)
		{
			Text = new ControlText(Game);
			Text.IgnoreFormatting = true;
			Text.OnTextUpdated += Text_OnTextUpdated;
			AddChild(Text);
			CursorImage = new ControlImage(Game);
			AddChild(CursorImage);
			CursorImage.SetImageFile("data\\images\\HUD\\text.png");
			AcceptsText = true;
			BlockedKeys = new List<char>();
		}

		private void Text_OnTextUpdated()
		{
			CursorImage.SetPositionX(Text.GetTextWidth() - CursorImage.Sprite.Bounds.Width * 0.4f, UIUnits.PixelScaled);
		}

		protected override void ParseElement(XElement Element)
		{
			switch (Element.Name.LocalName)
			{
				case "fontname":
					Text.FontName = Element.Value;
					break;
				case "fontsize":
					Text.SetFontSize(Element.Value);
					break;
				case "fontcolour":
					Text.Colour = ParseColour(Element.Value);
					break;
				case "bold":
					Text.Bold = bool.Parse(Element.Value);
					break;
				case "onreturn":
					OnReturn += GetEventHandler(Element.Value);
					break;
				default:
					base.ParseElement(Element);
					break;
			}
		}

		protected override void ReclaculateBounds()
		{
			base.ReclaculateBounds();
			if (Text != null)
			{
				Text.SetSize(Sprite.Bounds.Width, Sprite.Bounds.Height, UIUnits.PixelScaled);
			}
			if (CursorImage != null)
			{
				CursorImage.SetSize(Sprite.Bounds.Height * 0.5f, Sprite.Bounds.Height, UIUnits.PixelScaled);
			}
		}

		internal void SetFontSize(int Size, UIUnits UnitType)
		{
			Text.SetFontSize(Size, UnitType);
		}

		public void SetText(string NewText)
		{
			CurrentText = NewText;
			UpdateText();
		}

		private void UpdateText()
		{
			Text.SetLiteralText(CurrentText);
		}

		private bool KeyIsValidInput(char KeyChar)
		{
			if (Filename && Path.GetInvalidFileNameChars().Any((char x) => x == KeyChar))
			{
				return false;
			}
			if (BlockedKeys.Contains(KeyChar))
			{
				return false;
			}
			if (char.IsLetterOrDigit(KeyChar))
			{
				return true;
			}
			if (char.IsPunctuation(KeyChar))
			{
				return true;
			}
			if (char.IsSymbol(KeyChar))
			{
				return true;
			}
			if (KeyChar == ' ')
			{
				return true;
			}
			return false;
		}

		public override void HandleKeyPress(KeyPressEventArgs e)
		{
			base.HandleKeyPress(e);
			if (KeyIsValidInput(e.KeyChar))
			{
				CurrentText += e.KeyChar;
				UpdateText();
			}
		}

		public override void HandleKeyDown(KeyboardKeyEventArgs e)
		{
			base.HandleKeyDown(e);
			if ((e.Key == Key.Enter || e.Key == Key.KeypadEnter) && this.OnReturn != null)
			{
				this.OnReturn(this);
			}
			if (e.Key >= Key.F1 && e.Key <= Key.F35 && this.OnFunctionKey != null)
			{
				this.OnFunctionKey(this, e);
			}
			if (e.Key == Key.Up && this.OnArrowKey != null)
			{
				this.OnArrowKey(this, e);
			}
			if (e.Key == Key.Down && this.OnArrowKey != null)
			{
				this.OnArrowKey(this, e);
			}
			if (e.Key == Key.Left && this.OnArrowKey != null)
			{
				this.OnArrowKey(this, e);
			}
			if (e.Key == Key.Right && this.OnArrowKey != null)
			{
				this.OnArrowKey(this, e);
			}
			if (e.Key == Key.BackSpace && CurrentText != null && CurrentText.Length > 0)
			{
				CurrentText = CurrentText.Substring(0, CurrentText.Length - 1);
				UpdateText();
			}
		}

		public override void Render(GLShader Shader, float ElapsedTime)
		{
			base.Render(Shader, ElapsedTime);
			if (Manager.ActiveTextControl != this)
			{
				CursorImage.Visible = false;
				return;
			}
			Timer += ElapsedTime;
			if (Timer > 0.25f)
			{
				CursorImage.Visible = !CursorImage.Visible;
				Timer -= 0.25f;
			}
		}

		internal void Clear()
		{
			CurrentText = "";
			UpdateText();
		}
	}
}