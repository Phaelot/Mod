using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI
{
	public class UIManager
	{
		public UIManager(GameBase Game)
		{
			this.Game = Game;
			this.AllControls = new List<UIControl>();
			this.PendingRemovals = new List<UIControl>();
			this.PendingAdditions = new List<UIControl>();
			Rectangle viewport = Game.GetViewport();
			Matrix4 matrix = Matrix4.CreateOrthographicOffCenter(0f, (float)viewport.Width, (float)viewport.Height, 0f, -1f, 1f);
			this.UIShader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\UI.vert", "Data\\Shaders\\UI.frag", false);
			this.UIShader.SetMatrix("Projection", ref matrix);
			this.UIShader.SetTexture("Texture", 0);
			UISprite.Shader = this.UIShader;
			Game.Window.MouseDown += this.Window_MouseDown;
			Game.Window.MouseUp += this.Window_MouseUp;
			Game.Window.MouseMove += this.Window_MouseMove;
			Game.Window.MouseWheel += this.Window_MouseWheel;
			Game.Window.KeyUp += this.Window_KeyUp;
			Game.Window.KeyDown += this.Window_KeyDown;
			Game.Window.KeyPress += this.Window_KeyPress;
			Game.Window.MouseLeave += this.Window_MouseLeave;
		}

		private void Window_MouseLeave(object sender, EventArgs e)
		{
			this.LastMousePos = new Point(-1, -1);
		}

		private void Window_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (this.ActiveTextControl != null)
			{
				this.ActiveTextControl.HandleKeyPress(e);
				return;
			}
			this.Game.HandleKeyPress(e);
		}

		private void Window_KeyDown(object sender, KeyboardKeyEventArgs e)
		{
			if (this.ActiveTextControl != null)
			{
				this.ActiveTextControl.HandleKeyDown(e);
				return;
			}
			this.Game.HandleKeyDown(e);
		}

		private void Window_KeyUp(object sender, KeyboardKeyEventArgs e)
		{
			if (this.ActiveTextControl != null)
			{
				this.ActiveTextControl.HandleKeyUp(e);
				return;
			}
			this.Game.HandleKeyUp(e);
		}

		private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			UIControl uicontrol = this.ControlAtPoint(e.X, e.Y, true);
			if (uicontrol != null)
			{
				if (e.Delta > 0)
				{
					uicontrol.HandleMousewheelUp((float)e.X - uicontrol.Sprite.Bounds.X, (float)e.Y - uicontrol.Sprite.Bounds.Y);
					return;
				}
				uicontrol.HandleMousewheelDown((float)e.X - uicontrol.Sprite.Bounds.X, (float)e.Y - uicontrol.Sprite.Bounds.Y);
				return;
			}
			else
			{
				if (e.Delta > 0)
				{
					this.Game.HandleWheelUp(e);
					return;
				}
				this.Game.HandleWheelDown(e);
				return;
			}
		}

		private void Window_MouseMove(object sender, MouseMoveEventArgs e)
		{
			float num = (float)Math.Abs(this.LastMousePos.X - e.X);
			float num2 = (float)Math.Abs(this.LastMousePos.Y - e.Y);
			this.MouseMoveDist = (int)Math.Sqrt((double)(num * num + num2 * num2));
			this.LastMousePos = new Point(e.X, e.Y);
			UIControl uicontrol = this.ControlAtPoint(e.X, e.Y, false);
			if (uicontrol != this.ActiveMouseControl)
			{
				if (this.ActiveMouseControl == null && uicontrol != null)
				{
					this.Game.FireEvent("MouseLeftMap", new object[0]);
				}
				if (this.ActiveMouseControl != null)
				{
					this.ActiveMouseControl.HandleMouseLeave();
				}
				this.ActiveMouseControl = uicontrol;
				if (this.ActiveMouseControl != null)
				{
					this.ActiveMouseControl.HandleMouseEnter();
				}
			}
			if (uicontrol != null)
			{
				uicontrol.HandleMouseMove((float)e.X - uicontrol.Sprite.Bounds.X, (float)e.Y - uicontrol.Sprite.Bounds.Y);
			}
			else
			{
				this.Game.HandleMouseMove(e);
			}
			this.Game.HandleMousePositionChanged(e.X, e.Y);
		}

		private void Window_MouseUp(object sender, MouseButtonEventArgs e)
		{
			UIControl uicontrol = this.ControlAtPoint(e.X, e.Y, false);
			if (uicontrol != null)
			{
				uicontrol.HandleMouseUp((float)e.X - uicontrol.Sprite.Bounds.X, (float)e.Y - uicontrol.Sprite.Bounds.Y, e.Button);
			}
			else
			{
				this.Game.HandleMouseUp(e);
			}
			this.Game.HandleGeneralMouseUp(e);
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			UIControl uicontrol = this.ControlAtPoint(e.X, e.Y, false);
			if (uicontrol != null)
			{
				if (uicontrol.AcceptsText)
				{
					this.ActiveTextControl = uicontrol;
				}
				else
				{
					this.ActiveTextControl = null;
				}
				uicontrol.HandleMouseDown((float)e.X - uicontrol.Sprite.Bounds.X, (float)e.Y - uicontrol.Sprite.Bounds.Y, e.Button);
				return;
			}
			this.Game.HandleMouseDown(e);
		}

		public UIControl GetNamedControl(string Name)
		{
			return this.AllControls.FirstOrDefault((UIControl x) => x.ControlName == Name);
		}

		public void SetTextFocus(UIControl Control)
		{
			if (Control != null && !Control.AcceptsText)
			{
				return;
			}
			this.ActiveTextControl = Control;
		}

		private UIControl ControlAtPoint(int X, int Y, bool MouseWheel = false)
		{
			Point point = new Point(X, Y);
			int i = this.AllControls.Count - 1;
			while (i >= 0)
			{
				UIControl uicontrol = this.AllControls[i--];
				if (uicontrol.Sprite != null && uicontrol.Visible && uicontrol.MouseInputType != MouseInputTypes.None && (!MouseWheel || uicontrol.AcceptMouseWheel) && uicontrol.Sprite.Bounds.Contains(point) && (uicontrol.MouseInputType != MouseInputTypes.HitTest || uicontrol.Sprite.HitTest((float)X - uicontrol.Sprite.Bounds.X, (float)Y - uicontrol.Sprite.Bounds.Y)))
				{
					return uicontrol;
				}
			}
			return null;
		}

		internal void ControlOrderChanged()
		{
			this.ControlUpdateNeeded = true;
		}

		internal void ControlOrderChangedInternal()
		{
			this.ControlUpdateNeeded = false;
			List<UIControl> list = new List<UIControl>(this.AllControls.Count);
			foreach (UIControl uicontrol in from x in this.AllControls
				where x.ParentControl == null
				orderby x.ZOrder
				select x)
			{
				list.Add(uicontrol);
				uicontrol.AddChildrenToList(list);
			}
			this.AllControls = list;
		}

		public T CreateControl<T>() where T : UIControl
		{
			Type typeFromHandle = typeof(T);
			T t = Activator.CreateInstance(typeFromHandle, new object[] { this.Game }) as T;
			t.UniqueID = UIManager.NextID++;
			return t;
		}

		public void AddControl(UIControl Control)
		{
			Control.ZOrder = this.GetHighestZ() + 1;
			this.PendingAdditions.Add(Control);
		}

		private int GetHighestZ()
		{
			int num = 0;
			foreach (UIControl uicontrol in this.AllControls)
			{
				if (uicontrol.ParentControl == null && uicontrol.ZOrder > num)
				{
					num = uicontrol.ZOrder;
				}
			}
			return num;
		}

		public void RemoveControl(UIControl Control)
		{
			this.PendingRemovals.Add(Control);
		}

		internal void Render(float ElapsedTime)
		{
			foreach (UIControl uicontrol in this.PendingRemovals)
			{
				this.AllControls.Remove(uicontrol);
			}
			this.PendingRemovals.Clear();
			foreach (UIControl uicontrol2 in this.PendingAdditions)
			{
				if (uicontrol2 != null && !uicontrol2.Disposed)
				{
					this.AllControls.Add(uicontrol2);
					this.ControlUpdateNeeded = true;
				}
			}
			this.PendingAdditions.Clear();
			if (this.ControlUpdateNeeded)
			{
				this.ControlOrderChangedInternal();
			}
			foreach (UIControl uicontrol3 in this.AllControls)
			{
				uicontrol3.Render(this.UIShader, ElapsedTime);
			}
		}

		internal void AddToControlList(UIControl Child)
		{
			if (this.AllControls.Contains(Child))
			{
				return;
			}
			if (this.PendingAdditions.Contains(Child))
			{
				return;
			}
			this.PendingAdditions.Add(Child);
		}

		internal void MoveControlToFront(UIControl Control)
		{
			Control.ZOrder = this.GetHighestZ() + 1;
			this.ControlOrderChanged();
		}

		internal void Dispose()
		{
		}

		internal void UpdateViewport()
		{
			Rectangle viewport = this.Game.GetViewport();
			Matrix4 matrix = Matrix4.CreateOrthographicOffCenter(0f, (float)viewport.Width, (float)viewport.Height, 0f, -1f, 1f);
			this.UIShader.SetMatrix("Projection", ref matrix);
			this.AllControls.ToList<UIControl>().ForEach(delegate(UIControl x)
			{
				x.ForceUpdate();
			});
		}

		public void LanguageChanged()
		{
			List<UIControl> list = (from x in this.AllControls.ToList<UIControl>()
				where x is ControlText
				select x).ToList<UIControl>();
			foreach (UIControl uicontrol in list)
			{
				(uicontrol as ControlText).ForceTextUpdate();
			}
		}

		public void SetScaleIndex(int Index)
		{
			switch (Index)
			{
			case 1:
				this.UIScaleValue = 0f;
				break;
			case 2:
				this.UIScaleValue = 0.25f;
				break;
			case 3:
				this.UIScaleValue = 0.5f;
				break;
			case 4:
				this.UIScaleValue = 1f;
				break;
			}
			this.AllControls.ToList<UIControl>().ForEach(delegate(UIControl x)
			{
				x.ForceUpdate();
			});
		}

		private List<UIControl> AllControls;

		private List<UIControl> PendingRemovals;

		private List<UIControl> PendingAdditions;

		internal GameBase Game;

		private static int NextID = 1;

		private UIControl ActiveMouseControl;

		internal UIControl ActiveTextControl;

		private GLShader UIShader;

		private bool ControlUpdateNeeded;

		public float UIScaleValue;

		public Point LastMousePos;

		public int MouseMoveDist;
	}
}
