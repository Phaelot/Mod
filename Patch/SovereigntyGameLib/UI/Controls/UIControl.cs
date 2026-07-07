// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.Controls.UIControl
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class UIControl
	{
		public int ID;

		private static int NextID = 1;

		public UISprite Sprite;

		public UIManager Manager;

		public GameBase Game;

		private bool FadingIn;

		private bool FadingOut;

		private bool FadedOut;

		private float TotalFadeTime;

		private float FadeTimer;

		private PositionData OriginalPositionX;

		private PositionData OriginalPositionY;

		private PositionData OriginalWidth;

		private PositionData OriginalHeight;

		private AnchorPoints AnchorPoint;

		private bool m_Visible = true;

		public UIControl ParentControl;

		private List<UIControl> ChildControls;

		internal int UniqueID;

		internal int ZOrder;

		public bool BubbleMouseovers;

		protected List<MouseButton> ActiveButtons;

		public MouseInputTypes MouseInputType;

		public string ControlName;

		public object CustomData;

		public bool AcceptsText;

		public bool AcceptMouseWheel;

		private bool FullScreen;

		public bool Disposed;

		public float LastMouseX;

		public float LastMouseY;

		private GameText Tooltip;

		private List<GameText> TooltipList;

		private DateTime LastClickTime;

		public float PositionX => OriginalPositionX.Value;

		public bool Visible
		{
			get
			{
				if (GetHiddenStatus())
				{
					return false;
				}
				return true;
			}
			set
			{
				m_Visible = value;
				if (this.OnVisibleChanged != null)
				{
					this.OnVisibleChanged(this);
				}
			}
		}

		public event ControlDelegate OnMouseEnter;

		public event ControlDelegate OnMouseLeave;

		public event ControlDelegate OnClick;

		public event ControlDelegate OnRightClick;

		public event ControlDelegate OnMiddleClick;

		public event ControlDelegate OnDoubleClick;

		public event ControlMouseDelegate OnMouseDown;

		public event ControlMouseDelegate OnMouseMove;

		public event ControlDelegate OnMouseWheelUp;

		public event ControlDelegate OnMouseWheelDown;

		public event ControlDelegate OnVisibleChanged;

		public event ControlDelegate OnFadeoutComplete;

		public event ControlDelegate OnFadeinComplete;

		public bool GetHiddenStatus()
		{
			if (!m_Visible)
			{
				return true;
			}
			if (ParentControl == null)
			{
				return false;
			}
			return ParentControl.GetHiddenStatus();
		}

		public override string ToString()
		{
			return ControlName;
		}

		public UIControl(GameBase Game)
		{
			ID = NextID++;
			this.Game = Game;
			Manager = Game.UIManager;
			Sprite = new UISprite();
			OriginalPositionX = new PositionData(0f, X: true);
			OriginalPositionY = new PositionData(0f, X: false);
			OriginalWidth = new PositionData(50f, X: true);
			OriginalHeight = new PositionData(50f, X: false);
			ChildControls = new List<UIControl>();
			ActiveButtons = new List<MouseButton>();
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
			Game.WriteLog("Creating New control (" + GetType().Name + ")");
		}

		protected void Click()
		{
			if (this.OnClick != null)
			{
				this.OnClick(this);
			}
		}

		public void LoadFromXML(XElement RootElement)
		{
			string text = "Unnamed";
			XElement xElement = RootElement.Element("name");
			if (xElement != null)
			{
				text = xElement.Value;
			}
			ControlName = text;
			Game.WriteLog("Loading control data (" + text + ")");
			foreach (XElement item in RootElement.Elements())
			{
				ParseElement(item);
			}
			Game.WriteLog("Control data loaded (" + text + ") - calculating position");
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
			SetLoaded();
		}

		public virtual void SetLoaded()
		{
		}

		protected virtual void ParseElement(XElement Element)
		{
			Game.WriteLog("Parsing element: " + Element.Name.LocalName);
			switch (Element.Name.LocalName)
			{
				case "fullscreen":
					FullScreen = true;
					OriginalPositionX.UnitType = UIUnits.PixelScaled;
					OriginalPositionX.Value = 0f;
					OriginalPositionY.UnitType = UIUnits.PixelScaled;
					OriginalPositionY.Value = 0f;
					OriginalHeight.UnitType = UIUnits.PixelScaled;
					OriginalHeight.Value = Game.GetViewport().Height;
					OriginalWidth.UnitType = UIUnits.PixelScaled;
					OriginalWidth.Value = Game.GetViewport().Width;
					break;
				case "control":
					CreateControlFromXML(Element);
					break;
				case "name":
					ControlName = Element.Value;
					break;
				case "width":
					OriginalWidth = new PositionData(Element.Value, X: true);
					ReclaculateBounds();
					break;
				case "height":
					OriginalHeight = new PositionData(Element.Value, X: false);
					ReclaculateBounds();
					break;
				case "offsetx":
					OriginalPositionX = new PositionData(Element.Value, X: true);
					ReclaculateBounds();
					break;
				case "offsety":
					OriginalPositionY = new PositionData(Element.Value, X: false);
					ReclaculateBounds();
					break;
				case "ignoremouse":
					if (bool.Parse(Element.Value))
					{
						MouseInputType = MouseInputTypes.None;
					}
					break;
				case "forcemouse":
					if (bool.Parse(Element.Value))
					{
						MouseInputType = MouseInputTypes.Forced;
					}
					break;
				case "visible":
					Visible = bool.Parse(Element.Value);
					break;
				case "anchor":
					AnchorPoint = ParseAnchorPoint(Element.Value);
					ReclaculateBounds();
					break;
				case "onclick":
					OnClick += GetEventHandler(Element.Value);
					break;
				case "onmiddleclick":
					OnMiddleClick += GetEventHandler(Element.Value);
					break;
				case "ondoubleclick":
					OnDoubleClick += GetEventHandler(Element.Value);
					break;
				case "onmouseenter":
					OnMouseEnter += GetEventHandler(Element.Value);
					break;
				case "onmouseleave":
					OnMouseLeave += GetEventHandler(Element.Value);
					break;
				case "onmousewheelup":
					OnMouseWheelUp += GetEventHandler(Element.Value);
					break;
				case "onmousewheeldown":
					OnMouseWheelDown += GetEventHandler(Element.Value);
					break;
				case "acceptmousewheel":
					AcceptMouseWheel = bool.Parse(Element.Value);
					break;
				case "onvisiblechanged":
					OnVisibleChanged += GetEventHandler(Element.Value);
					break;
				case "tooltip":
					Tooltip = GameText.CreateLocalised(Element.Value);
					break;
				case "onfadeoutcomplete":
					OnFadeoutComplete += GetEventHandler(Element.Value);
					break;
				case "onfadeincomplete":
					OnFadeinComplete += GetEventHandler(Element.Value);
					break;
				case "bubblemouseovers":
					BubbleMouseovers = bool.Parse(Element.Value);
					break;
				default:
					throw new Exception("Unknown UI Element: " + Element.Name.LocalName);
				case "onrightclick":
				case "ignoremousemove":
					break;
			}
		}

		public void SetTooltip(GameText Tooltip)
		{
			TooltipList = null;
			this.Tooltip = Tooltip;
		}

		public void SetTooltip(List<GameText> Tooltip)
		{
			this.Tooltip = null;
			TooltipList = Tooltip;
		}

		public virtual void Update()
		{
		}

		public Color ParseColour(string s)
		{
			string[] array = s.Split(',');
			int result = 0;
			int result2 = 0;
			int result3 = 0;
			int.TryParse(array[0], out result);
			int.TryParse(array[1], out result2);
			int.TryParse(array[2], out result3);
			return Color.FromArgb(result, result2, result3);
		}

		protected AnchorPoints ParseAnchorPoint(string Value)
		{
			switch (Value)
			{
				case "TopLeft":
					return AnchorPoints.TopLeft;
				case "TopMiddle":
					return AnchorPoints.TopMiddle;
				case "TopRight":
					return AnchorPoints.TopRight;
				case "MiddleLeft":
					return AnchorPoints.Left;
				case "Middle":
					return AnchorPoints.Middle;
				case "MiddleRight":
					return AnchorPoints.Right;
				case "BottomLeft":
					return AnchorPoints.BottomLeft;
				case "BottomMiddle":
					return AnchorPoints.BottomMiddle;
				case "BottomRight":
					return AnchorPoints.BottomRight;
			}
			return AnchorPoints.TopLeft;
		}

		private void CreateControlFromXML(XElement Element)
		{
			UIControl uIControl = null;
			switch (Element.Attribute("type").Value)
			{
				case "form":
					CreateFormFromXML(Element);
					return;
				case "image":
					uIControl = new ControlImage(Game);
					break;
				case "button":
					uIControl = new ControlButton(Game);
					break;
				case "text":
					uIControl = new ControlText(Game);
					break;
				case "container":
					uIControl = new ControlContainer(Game);
					break;
				case "list":
					{
						string value = Element.Attribute("itemtype").Value;
						Type type = Type.GetType(value);
						if (type == null)
						{
							throw new Exception("List type " + value + " does not exist. Please make sure to include a fully qualified type name");
						}
						Type type2 = typeof(ControlList<>).MakeGenericType(type);
						uIControl = (UIControl)Activator.CreateInstance(type2, Game);
						break;
					}
				case "menu":
					uIControl = new ControlMenu(Game);
					break;
				case "input":
					uIControl = new ControlInput(Game);
					break;
				case "ticker":
					return;
				case "graph":
					uIControl = new ControlGraph(Game);
					break;
				case "video":
					uIControl = new ControlVideo(Game);
					break;
				default:
					throw new Exception("Unknown control type: " + Element.Attribute("type").Value);
			}
			AddChild(uIControl);
			uIControl.LoadFromXML(Element);
		}

		private void CreateFormFromXML(XElement Element)
		{
			ControlForm controlForm = new ControlForm(Game);
			AddChild(controlForm);
			XElement rootElement = XElement.Load(Game.Utilities.FileSystem.OpenFile(Element.Attribute("filename").Value, FileTypes.Application));
			controlForm.LoadFromXML(rootElement);
		}

		protected virtual ControlDelegate GetEventHandler(string FunctionName)
		{
			if (ParentControl != null)
			{
				return ParentControl.GetEventHandler(FunctionName);
			}
			return null;
		}

		public virtual void Dispose()
		{
			if (Disposed || Sprite == null)
			{
				return;
			}
			Game.WriteLog("Disposing control (" + ControlName + ")");
			Disposed = true;
			foreach (UIControl childControl in ChildControls)
			{
				childControl.Dispose();
			}
			this.OnClick = null;
			this.OnMouseEnter = null;
			this.OnMouseLeave = null;
			this.OnMouseDown = null;
			this.OnMouseMove = null;
			this.OnMouseWheelDown = null;
			this.OnMouseWheelUp = null;
			this.OnVisibleChanged = null;
			this.OnFadeinComplete = null;
			this.OnFadeoutComplete = null;
			Manager.RemoveControl(this);
			Game = null;
			Manager = null;
			Sprite.Dispose();
			Sprite = null;
		}

		public float GetOriginalX()
		{
			return OriginalPositionX.Value;
		}

		public float GetOriginalY()
		{
			return OriginalPositionY.Value;
		}

		public float getOriginalWidth()
		{
			return OriginalWidth.Value;
		}

		public float GetOriginalHeight()
		{
			return OriginalHeight.Value;
		}

		public RectangleF GetOriginalBounds()
		{
			return new RectangleF(GetOriginalX(), GetOriginalY(), getOriginalWidth(), GetOriginalHeight());
		}

		public void SetPositionX(float X, UIUnits UnitType)
		{
			OriginalPositionX.Value = X;
			OriginalPositionX.UnitType = UnitType;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetPositionY(float Y, UIUnits UnitType)
		{
			OriginalPositionY.Value = Y;
			OriginalPositionY.UnitType = UnitType;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetWidth(float Width, UIUnits UnitType)
		{
			OriginalWidth.Value = Width;
			OriginalWidth.UnitType = UnitType;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetHeight(float Height, UIUnits UnitType)
		{
			OriginalHeight.Value = Height;
			OriginalHeight.UnitType = UnitType;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetPosition(float X, float Y, UIUnits UnitType)
		{
			OriginalPositionX.Value = X;
			OriginalPositionX.UnitType = UnitType;
			OriginalPositionY.Value = Y;
			OriginalPositionY.UnitType = UnitType;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetSize(float Width, float Height, UIUnits UnitType)
		{
			OriginalWidth.Value = Width;
			OriginalWidth.UnitType = UnitType;
			OriginalHeight.Value = Height;
			OriginalHeight.UnitType = UnitType;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetBounds(RectangleF Bounds, UIUnits UnitType)
		{
			SetBounds(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, UnitType);
		}

		public void SetBounds(float X, float Y, float Width, float Height, UIUnits UnitType)
		{
			OriginalPositionX.Value = X;
			OriginalPositionX.UnitType = UnitType;
			OriginalPositionY.Value = Y;
			OriginalPositionY.UnitType = UnitType;
			OriginalWidth.Value = Width;
			OriginalWidth.UnitType = UnitType;
			OriginalHeight.Value = Height;
			OriginalHeight.UnitType = UnitType;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetUV(float U1, float V1, float U2, float V2)
		{
			Sprite.WriteVertices();
		}

		public void SetAnchor(AnchorPoints Anchor)
		{
			AnchorPoint = Anchor;
			if (Sprite != null)
			{
				ReclaculateBounds();
			}
		}

		public void SetBlendColour(Color Col)
		{
			Sprite.SetBlendColour(Col);
		}

		public void RemoveChild(UIControl Child)
		{
			if (ChildControls.Contains(Child))
			{
				ChildControls.Remove(Child);
				Child.ParentControl = null;
				Manager.RemoveControl(Child);
				Manager.ControlOrderChanged();
			}
		}

		public void AddChild(UIControl Child)
		{
			int zOrder = GetMaxZOrder() + 1;
			ChildControls.Add(Child);
			Child.SetZOrder(zOrder);
			Child.ParentControl = this;
			Child.ReclaculateBounds();
			Manager.AddToControlList(Child);
			Manager.ControlOrderChanged();
		}

		private int GetMaxZOrder()
		{
			int num = 0;
			foreach (UIControl childControl in ChildControls)
			{
				if (childControl.ZOrder > num)
				{
					num = childControl.ZOrder;
				}
			}
			return num;
		}

		public virtual void HandleMouseEnter()
		{
			if (Game != null)
			{
				Game.FireEvent("TooltipChanged", GetTooltip());
			}
			if (this.OnMouseEnter != null)
			{
				this.OnMouseEnter(this);
			}
			if (BubbleMouseovers && ParentControl != null)
			{
				ParentControl.HandleMouseEnter();
			}
		}

		private object GetTooltip()
		{
			if (Tooltip != null)
			{
				return Tooltip;
			}
			if (TooltipList != null)
			{
				return TooltipList;
			}
			return null;
		}

		public virtual void HandleMouseLeave()
		{
			ActiveButtons.Clear();
			if (this.OnMouseLeave != null)
			{
				this.OnMouseLeave(this);
			}
			if (BubbleMouseovers && ParentControl != null)
			{
				ParentControl.HandleMouseLeave();
			}
		}

		public virtual void HandleMouseMove(float LocalX, float LocalY)
		{
			LastMouseX = LocalX;
			LastMouseY = LocalY;
			if (this.OnMouseMove != null)
			{
				this.OnMouseMove(this, LocalX, LocalY, MouseButton.Left);
			}
		}

		public virtual void HandleMouseUp(float LocalX, float LocalY, MouseButton Button)
		{
			if (ActiveButtons.Contains(Button))
			{
				ActiveButtons.Remove(Button);
				DateTime now = DateTime.Now;
				if ((now - LastClickTime).TotalMilliseconds < 250.0 && this.OnDoubleClick != null)
				{
					HandleMouseDoubleClick(LocalX, LocalY, Button);
					return;
				}
				LastClickTime = now;
				HandleMouseClick(LocalX, LocalY, Button);
			}
		}

		public virtual void HandleMouseDoubleClick(float LocalX, float LocalY, MouseButton Button)
		{
			if (Button == MouseButton.Left && this.OnDoubleClick != null)
			{
				this.OnDoubleClick(this);
			}
		}

		public virtual void HandleMouseClick(float LocalX, float LocalY, MouseButton Button)
		{
			if (Button == MouseButton.Left && this.OnClick != null)
			{
				this.OnClick(this);
			}
			if (Button == MouseButton.Right && this.OnRightClick != null)
			{
				this.OnRightClick(this);
			}
			if (Button == MouseButton.Middle && this.OnMiddleClick != null)
			{
				this.OnMiddleClick(this);
			}
		}

		public virtual void HandleMouseDown(float LocalX, float LocalY, MouseButton Button)
		{
			if (!ActiveButtons.Contains(Button))
			{
				ActiveButtons.Add(Button);
			}
			if (this.OnMouseDown != null)
			{
				this.OnMouseDown(this, LocalX, LocalY, Button);
			}
		}

		public virtual void HandleMousewheelUp(float LocalX, float LocalY)
		{
		}

		public virtual void HandleMousewheelDown(float LocalX, float LocalY)
		{
		}

		public virtual void HandleKeyUp(KeyboardKeyEventArgs e)
		{
		}

		public virtual void HandleKeyDown(KeyboardKeyEventArgs e)
		{
		}

		public virtual void HandleKeyPress(KeyPressEventArgs e)
		{
		}

		private void SetZOrder(int Order)
		{
			ZOrder = Order;
			foreach (UIControl childControl in ChildControls)
			{
				childControl.SetZOrder(Order + 1);
			}
		}

		public Point GetScreenCoords()
		{
			return new Point((int)Sprite.Bounds.X, (int)Sprite.Bounds.Y);
		}

		private float GetScreenX()
		{
			if (ParentControl != null)
			{
				return ParentControl.Sprite.Bounds.X;
			}
			return Sprite.Bounds.X;
		}

		private float GetScreenY()
		{
			if (ParentControl != null)
			{
				return ParentControl.Sprite.Bounds.Y;
			}
			return Sprite.Bounds.Y;
		}

		private float GetParentY()
		{
			if (ParentControl != null)
			{
				return ParentControl.GetScreenY();
			}
			return 0f;
		}

		private float GetParentX()
		{
			if (ParentControl != null)
			{
				return ParentControl.GetScreenX();
			}
			return 0f;
		}

		private float GetParentScreenX()
		{
			if (ParentControl != null && ParentControl.Sprite != null)
			{
				return ParentControl.Sprite.Bounds.X;
			}
			return 0f;
		}

		private float GetParentScreenY()
		{
			if (ParentControl != null && ParentControl.Sprite != null)
			{
				return ParentControl.Sprite.Bounds.Y;
			}
			return 0f;
		}

		private float GetParentWidth()
		{
			if (ParentControl != null)
			{
				return ParentControl.GetSpriteWidth();
			}
			int[] array = new int[4];
			GL.GetInteger(GetPName.Viewport, array);
			return array[2];
		}

		private float GetSpriteWidth()
		{
			if (Sprite == null)
			{
				return 0f;
			}
			return Sprite.Bounds.Width;
		}

		private float GetParentHeight()
		{
			if (ParentControl != null)
			{
				return ParentControl.GetSpriteHeight();
			}
			int[] array = new int[4];
			GL.GetInteger(GetPName.Viewport, array);
			return array[3];
		}

		private float GetSpriteHeight()
		{
			if (Sprite == null)
			{
				return 0f;
			}
			return Sprite.Bounds.Height;
		}

		public void ForceUpdate()
		{
			if (!Disposed)
			{
				if (FullScreen)
				{
					OriginalPositionX.UnitType = UIUnits.PixelScaled;
					OriginalPositionX.Value = 0f;
					OriginalPositionY.UnitType = UIUnits.PixelScaled;
					OriginalPositionY.Value = 0f;
					OriginalHeight.UnitType = UIUnits.PixelScaled;
					OriginalHeight.Value = Game.GetViewport().Height;
					OriginalWidth.UnitType = UIUnits.PixelScaled;
					OriginalWidth.Value = Game.GetViewport().Width;
				}
				if (Sprite != null)
				{
					ReclaculateBounds();
				}
			}
		}

		protected virtual void ReclaculateBounds()
		{
			Game.WriteLog("Control " + ControlName + " calculating bounds");
			float num = ConvertSinglePositionData(OriginalPositionX);
			Game.WriteLog("X: " + num);
			float num2 = ConvertSinglePositionData(OriginalPositionY);
			Game.WriteLog("Y: " + num2);
			float num3 = ConvertSinglePositionData(OriginalWidth);
			Game.WriteLog("W: " + num3);
			float num4 = ConvertSinglePositionData(OriginalHeight);
			Game.WriteLog("H: " + num4);
			switch (AnchorPoint)
			{
				case AnchorPoints.TopLeft:
					num += GetParentScreenX();
					num2 += GetParentScreenY();
					break;
				case AnchorPoints.TopMiddle:
					{
						float num10 = GetParentScreenX() + GetParentWidth() / 2f;
						num += (float)(int)(num10 - num3 / 2f);
						num2 += GetParentScreenY();
						break;
					}
				case AnchorPoints.TopRight:
					num = GetParentScreenX() + GetParentWidth() - num3 - num;
					num2 += GetParentScreenY();
					break;
				case AnchorPoints.Left:
					{
						num += GetParentScreenX();
						float num9 = GetParentScreenY() + GetParentHeight() / 2f;
						num2 += (float)(int)(num9 - num4 / 2f);
						break;
					}
				case AnchorPoints.Middle:
					{
						float num7 = GetParentScreenX() + GetParentWidth() / 2f;
						num += (float)(int)(num7 - num3 / 2f);
						float num8 = GetParentScreenY() + GetParentHeight() / 2f;
						num2 += (float)(int)(num8 - num4 / 2f);
						break;
					}
				case AnchorPoints.Right:
					{
						num = GetParentScreenX() + GetParentWidth() - num3 - num;
						float num6 = GetParentScreenY() + GetParentHeight() / 2f;
						num2 += (float)(int)(num6 - num4 / 2f);
						break;
					}
				case AnchorPoints.BottomLeft:
					num += GetParentScreenX();
					num2 = GetParentScreenY() + GetParentHeight() - num4 - num2;
					break;
				case AnchorPoints.BottomMiddle:
					{
						float num5 = GetParentScreenX() + GetParentWidth() / 2f;
						num += (float)(int)(num5 - num3 / 2f);
						num2 = GetParentScreenY() + GetParentHeight() - num4 - num2;
						break;
					}
				case AnchorPoints.BottomRight:
					num = GetParentScreenX() + GetParentWidth() - num3 - num;
					num2 = GetParentScreenY() + GetParentHeight() - num4 - num2;
					break;
			}
			Game.WriteLog("Adjusted bounds: X: " + num + ", Y: " + num2 + ", W: " + num3 + ", H: " + num4);
			Sprite.Bounds = new RectangleF(num, num2, num3, num4);
			Sprite.UpdateVertices();
			foreach (UIControl childControl in ChildControls)
			{
				childControl.ReclaculateBounds();
			}
		}

		protected int ConvertSinglePositionData(PositionData Data)
		{
			int result = 0;
			if (Data == null)
			{
				return result;
			}
			switch (Data.UnitType)
			{
				case UIUnits.Internal:
					{
						int[] array2 = new int[4];
						GL.GetInteger(GetPName.Viewport, array2);
						result = ((!Data.IsXValue) ? ((int)(Data.Value * 0.01f * (float)array2[3])) : ((int)(Data.Value * 0.01f * (float)array2[3])));
						break;
					}
				case UIUnits.Percent:
					{
						float num3 = 0f;
						num3 = ((!Data.IsXValue) ? (Data.Value * 0.01f * GetParentHeight()) : (Data.Value * 0.01f * GetParentWidth()));
						result = ((!float.IsInfinity(num3) && !float.IsNaN(num3)) ? ((int)num3) : 0);
						break;
					}
				case UIUnits.Pixel:
					{
						int[] array = new int[4];
						GL.GetInteger(GetPName.Viewport, array);
						PointF pointF;
						if (Data.IsXValue)
						{
							float num = (float)array[3] / 768f;
							num -= 1f;
							num *= Manager.UIScaleValue;
							num += 1f;
							pointF = new PointF(Data.Value * num, 0f);
						}
						else
						{
							float num2 = (float)array[3] / 768f;
							num2 -= 1f;
							num2 *= Manager.UIScaleValue;
							num2 += 1f;
							pointF = new PointF(0f, Data.Value * num2);
						}
						result = ((!Data.IsXValue) ? ((int)pointF.Y) : ((int)pointF.X));
						break;
					}
				case UIUnits.PixelScaled:
					result = (int)Data.Value;
					break;
			}
			return result;
		}

		internal bool ControlIsAbove(UIControl Control)
		{
			if (ParentControl == Control)
			{
				return true;
			}
			if (ParentControl == null)
			{
				return false;
			}
			return ParentControl.ControlIsAbove(Control);
		}

		internal void AddChildrenToList(List<UIControl> NewList)
		{
			foreach (UIControl item in ChildControls.OrderBy((UIControl x) => x.ZOrder))
			{
				NewList.Add(item);
				item.AddChildrenToList(NewList);
			}
		}

		public void BringToFront()
		{
			if (ParentControl != null)
			{
				ParentControl.MoveControlToFront(this);
			}
			else if (Manager != null)
			{
				Manager.MoveControlToFront(this);
			}
		}

		private void MoveControlToFront(UIControl Control)
		{
			Control.ZOrder = GetMaxZOrder() + 1;
			Manager.ControlOrderChanged();
		}

		public void BeginFadeIn(float FadeTime)
		{
			FadingIn = true;
			FadingOut = false;
			FadedOut = false;
			TotalFadeTime = FadeTime;
			FadeTimer = 0f;
			Sprite.SetAlpha(0f);
		}

		public void BeginFadeOut(float FadeTime)
		{
			FadingOut = true;
			FadingIn = false;
			TotalFadeTime = FadeTime;
			FadeTimer = 0f;
			Sprite.SetAlpha(1f);
		}

		public virtual void Render(GLShader Shader, float ElapsedTime)
		{
			if (!Visible || Disposed)
			{
				return;
			}
			if (FadingIn)
			{
				FadeTimer += ElapsedTime;
				float num = FadeTimer / TotalFadeTime;
				Sprite.SetAlpha(num);
				if (num >= 1f)
				{
					FadingIn = false;
					if (this.OnFadeinComplete != null)
					{
						this.OnFadeinComplete(this);
					}
				}
			}
			if (FadingOut)
			{
				FadeTimer += ElapsedTime;
				float num2 = 1f - FadeTimer / TotalFadeTime;
				Sprite.SetAlpha(num2);
				if (num2 <= 0f)
				{
					FadingOut = false;
					m_Visible = false;
					if (this.OnFadeoutComplete != null)
					{
						this.OnFadeoutComplete(this);
					}
				}
			}
			Sprite.Render(Shader);
		}

		public T GetControlByName<T>(string Name) where T : UIControl
		{
			if (Name == ControlName)
			{
				return (T)this;
			}
			foreach (UIControl childControl in ChildControls)
			{
				T controlByName = childControl.GetControlByName<T>(Name);
				if (controlByName != null)
				{
					return controlByName;
				}
			}
			return null;
		}

		public IList<UIControl> GetChildren()
		{
			return ChildControls.AsReadOnly();
		}
	}
}