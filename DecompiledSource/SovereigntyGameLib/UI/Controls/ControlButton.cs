// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Controls.ControlButton
using System.Xml.Linq;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class ControlButton : UIControl
	{
		private GLTexture NormalImg;

		private GLTexture MouseoverImg;

		private GLTexture PressedImg;

		public bool AutoClick;

		private float TimeSinceClick;

		private float TimeToFirstClick = 0.25f;

		private float TimeBetweenClicks = 0.1f;

		public int AutoClickCount;

		private bool FirstClick;

		private bool PlaySound;

		private bool m_ForceOn;

		private bool m_Disabled;

		public bool Disabled
		{
			get
			{
				return m_Disabled;
			}
			set
			{
				if (m_Disabled != value)
				{
					m_Disabled = value;
					Sprite.CurrentTexture = NormalImg;
				}
			}
		}

		public bool ForceOn
		{
			get
			{
				return m_ForceOn;
			}
			set
			{
				if (m_ForceOn != value)
				{
					m_ForceOn = value;
					if (m_ForceOn)
					{
						Sprite.CurrentTexture = PressedImg;
					}
					else
					{
						Sprite.CurrentTexture = NormalImg;
					}
				}
			}
		}

		public ControlButton(GameBase Game)
			: base(Game)
		{
		}

		protected override void ParseElement(XElement Element)
		{
			switch (Element.Name.LocalName)
			{
				case "imgnormal":
					SetImage1(Element.Value);
					break;
				case "imghigh":
					SetImage2(Element.Value);
					break;
				case "imgpress":
					SetImage3(Element.Value);
					break;
				case "autoclick":
					AutoClick = bool.Parse(Element.Value);
					break;
				case "playsound":
					PlaySound = bool.Parse(Element.Value);
					break;
				default:
					base.ParseElement(Element);
					break;
			}
		}

		public override void HandleMouseClick(float LocalX, float LocalY, MouseButton Button)
		{
			if (Game != null)
			{
				Game.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\smooth_button_click07.wav");
			}
			base.HandleMouseClick(LocalX, LocalY, Button);
		}

		public override void Render(GLShader Shader, float ElapsedTime)
		{
			base.Render(Shader, ElapsedTime);
			if (Disabled || !AutoClick || !ActiveButtons.Contains(MouseButton.Left))
			{
				return;
			}
			TimeSinceClick += ElapsedTime;
			if (FirstClick)
			{
				AutoClickCount = 0;
				if (TimeSinceClick >= TimeToFirstClick)
				{
					TimeSinceClick -= TimeToFirstClick;
					FirstClick = false;
					AutoClickCount++;
					Click();
				}
			}
			else if (TimeSinceClick >= TimeBetweenClicks)
			{
				TimeSinceClick -= TimeBetweenClicks;
				AutoClickCount++;
				Click();
			}
		}

		public override void Update()
		{
		}

		private void SetImage1(string Filename)
		{
			if (NormalImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(NormalImg);
			}
			NormalImg = Game.Utilities.TextureManager.GetTexture(Filename);
			Sprite.CurrentTexture = NormalImg;
		}

		private void SetImage2(string Filename)
		{
			if (MouseoverImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(MouseoverImg);
			}
			MouseoverImg = Game.Utilities.TextureManager.GetTexture(Filename);
			Sprite.CurrentTexture = NormalImg;
		}

		private void SetImage3(string Filename)
		{
			if (PressedImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(PressedImg);
			}
			PressedImg = Game.Utilities.TextureManager.GetTexture(Filename);
			Sprite.CurrentTexture = NormalImg;
		}

		public void SetImageFiles(string Filename1, string Filename2, string Filename3)
		{
			Sprite.CurrentTexture = null;
			if (NormalImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(NormalImg);
			}
			if (MouseoverImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(MouseoverImg);
			}
			if (PressedImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(PressedImg);
			}
			NormalImg = Game.Utilities.TextureManager.GetTexture(Filename1);
			MouseoverImg = Game.Utilities.TextureManager.GetTexture(Filename2);
			PressedImg = Game.Utilities.TextureManager.GetTexture(Filename3);
			NormalImg.SetFilter(TextureMinFilter.Linear, TextureMagFilter.Linear);
			MouseoverImg.SetFilter(TextureMinFilter.Linear, TextureMagFilter.Linear);
			PressedImg.SetFilter(TextureMinFilter.Linear, TextureMagFilter.Linear);
			Sprite.CurrentTexture = NormalImg;
		}

		public override void Dispose()
		{
			if (NormalImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(NormalImg);
			}
			if (MouseoverImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(MouseoverImg);
			}
			if (PressedImg != null)
			{
				Game.Utilities.TextureManager.ReleaseTexture(PressedImg);
			}
			NormalImg = null;
			MouseoverImg = null;
			PressedImg = null;
			base.Dispose();
		}

		public override void HandleMouseEnter()
		{
			base.HandleMouseEnter();
			if (!m_Disabled && !m_ForceOn)
			{
				Sprite.CurrentTexture = MouseoverImg;
			}
		}

		public override void HandleMouseLeave()
		{
			base.HandleMouseLeave();
			if (Sprite != null && !m_Disabled && !m_ForceOn)
			{
				Sprite.CurrentTexture = NormalImg;
			}
		}

		public override void HandleMouseDown(float LocalX, float LocalY, MouseButton Button)
		{
			base.HandleMouseDown(LocalX, LocalY, Button);
			if (!m_Disabled && Button == MouseButton.Left)
			{
				Sprite.CurrentTexture = PressedImg;
				if (AutoClick)
				{
					FirstClick = true;
					TimeSinceClick = 0f;
				}
			}
		}

		public override void HandleMouseUp(float LocalX, float LocalY, MouseButton Button)
		{
			base.HandleMouseUp(LocalX, LocalY, Button);
			if (Sprite != null && !m_Disabled && Button == MouseButton.Left && !m_ForceOn)
			{
				Sprite.CurrentTexture = MouseoverImg;
			}
		}
	}
}