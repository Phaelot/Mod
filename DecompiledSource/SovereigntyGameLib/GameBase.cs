// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.GameBase
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.Game;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK
{
	public abstract class GameBase
	{
		private bool m_Running;

		private DateTime StartTime;

		private int CurrentUpdate;

		private int NextUpdate;

		private static float UPS = 20f;

		private static int MAX_UPDATES = 2;

		public GLFrameBuffer FBO;

		public GameWindow Window;

		public UIManager UIManager;

		public GameCamera Camera;

		public UtilityManager Utilities;

		private string GameFolder;

		public Settings Settings;

		private StreamWriter LogWriter;

		private Stopwatch Timer;

		private Dictionary<string, List<GenericDelegate>> EventHandlers;

		public bool Running => m_Running;

		public abstract void Init();

		public abstract void ShutDown();

		public abstract void Render(float ElapsedTime);

		public abstract void Update();

		public GameBase(GameWindow Window, string WindowTitle, string GameFolder)
		{
			this.Window = Window;
			Utilities = new UtilityManager(GameFolder, this);
			Utilities.FileSystem.DeleteFile("EngineLog.txt", FileTypes.User, Relative: true);
			Utilities.Init();
			CreateSettings();
			if (GlobalData.ShowGPU)
			{
				string text = "Vendor Name: " + GL.GetString(StringName.Vendor) + "\n";
				text = text + "Renderer: " + GL.GetString(StringName.Renderer) + "\n";
				text = text + "Version: " + GL.GetString(StringName.Version) + "\n";
				text = text + "Shader Version: " + GL.GetString(StringName.ShadingLanguageVersion);
				MessageBox.Show(text);
			}
			if (GL.GetString(StringName.Vendor).ToLowerInvariant().Contains("intel"))
			{
				GlobalData.IntelHardware = true;
			}
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			Window.Visible = true;
			Window.ClientSize = new Size(1024, 768);
			GL.Viewport(Window.ClientRectangle);
			Window.Title = WindowTitle;
			Window.Closing += Window_Closing;
			Window.Resize += Window_Resize;
			UpdateFBO();
			this.GameFolder = GameFolder;
			Timer = new Stopwatch();
			EventHandlers = new Dictionary<string, List<GenericDelegate>>();
		}

		public void WriteLog(string p)
		{
			if (LogWriter != null)
			{
				LogWriter.WriteLine(p);
				LogWriter.Flush();
			}
		}

		private void UpdateFBO()
		{
			WriteLog("Creating FBO");
			if (FBO != null)
			{
				WriteLog("Removing old FBO");
				FBO.Dispose();
				FBO = null;
			}
			int multiSamples = GetMultiSamples();
			if (multiSamples > 0)
			{
				WriteLog("Multisample mode requested, creating new FBO");
				FBO = new GLFrameBuffer(Window.ClientSize.Width, Window.ClientSize.Height, multiSamples);
			}
			else
			{
				WriteLog("No multisample requested, FBO not created");
			}
		}

		private int GetMultiSamples()
		{
			return Settings.GetNumericListSetting("AA");
		}

		public void CreateSettings()
		{
			if (Settings == null)
			{
				Settings = new Settings(this);
				string text = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + System.IO.Path.DirectorySeparatorChar + "Sovereignty";
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				if (File.Exists(text + System.IO.Path.DirectorySeparatorChar + "settings.cfg"))
				{
					WriteLog("Loading settings: old format found, converting to new format");
					Settings.ConvertFromOldFile(text + System.IO.Path.DirectorySeparatorChar + "settings.cfg");
					Settings.Save(text + System.IO.Path.DirectorySeparatorChar + "settings.xml");
					File.Delete(text + System.IO.Path.DirectorySeparatorChar + "settings.cfg");
				}
				else if (File.Exists(text + System.IO.Path.DirectorySeparatorChar + "settings.xml"))
				{
					WriteLog("Loading settings");
					Settings.Load(text + System.IO.Path.DirectorySeparatorChar + "settings.xml");
				}
				else
				{
					WriteLog("Loading settings: no file found, creating defaults");
					Settings.Save(text + System.IO.Path.DirectorySeparatorChar + "settings.xml");
				}
			}
		}

		public void UnregisterEvent(GenericDelegate Action, string EventName)
		{
			if (!EventHandlers.ContainsKey(EventName))
			{
				EventHandlers.Add(EventName, new List<GenericDelegate>());
			}
			EventHandlers[EventName].Remove(Action);
		}

		public void RegisterEvents(GenericDelegate Action, params string[] EventNames)
		{
			foreach (string eventName in EventNames)
			{
				RegisterEvent(Action, eventName);
			}
		}

		public void UnregisterEvents(GenericDelegate Action, params string[] EventNames)
		{
			foreach (string eventName in EventNames)
			{
				UnregisterEvent(Action, eventName);
			}
		}

		public void RegisterEvent(GenericDelegate Action, string EventName)
		{
			if (!EventHandlers.ContainsKey(EventName))
			{
				EventHandlers.Add(EventName, new List<GenericDelegate>());
			}
			EventHandlers[EventName].Add(Action);
		}

		public void FireEvent(string EventName, params object[] Args)
		{
			List<GenericDelegate> value = null;
			EventHandlers.TryGetValue(EventName, out value);
			if (value == null)
			{
				return;
			}
			foreach (GenericDelegate item in value.ToList())
			{
				item(EventName, Args);
			}
		}

		public List<int> GetMultiSampleModes()
		{
			int data = 0;
			GL.GetInteger(GetPName.MaxSamples, out data);
			List<int> list = new List<int>();
			int num = 0;
			do
			{
				list.Add(num);
				num = ((num != 0) ? (num * 2) : 2);
			}
			while (num <= data);
			return list;
		}

		protected void Window_Resize(object sender, EventArgs e)
		{
			GL.Viewport(Window.ClientRectangle);
			if (UIManager != null)
			{
				UIManager.UpdateViewport();
				ViewportChanged();
			}
		}

		protected virtual void ViewportChanged()
		{
			UpdateFBO();
		}

		private string CheckRegistration()
		{
			return "";
		}

		public void Start()
		{
			m_Running = true;
			WriteLog("Initialising UI system");
			UIManager = new UIManager(this);
			Init();
			string text = CheckRegistration();
			if (text != "")
			{
				GameText gameText = GameText.CreateLocalised(text);
				MessageBox.Show(gameText.GetActualText(this));
				ForceShutdown();
				return;
			}
			WriteLog("Engine initialised, starting game loop");
			Timer.Start();
			StartTime = DateTime.Now;
			CurrentUpdate = GetUpdateFrame();
			NextUpdate = CurrentUpdate;
			while (m_Running)
			{
				UpdateInternal();
				Window.ProcessEvents();
			}
		}

		public void ForceShutdown()
		{
			m_Running = false;
			Window.Close();
			Stop();
		}

		public void Stop()
		{
			m_Running = false;
			ShutDown();
			if (UIManager != null)
			{
				UIManager.Dispose();
			}
			if (Utilities != null)
			{
				Utilities.Dispose();
			}
			if (LogWriter != null)
			{
				LogWriter.Close();
				LogWriter = null;
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			Stop();
		}

		private int GetUpdateFrame()
		{
			int num = (int)(DateTime.Now - StartTime).TotalMilliseconds;
			return (int)((float)num / (1000f / UPS));
		}

		private void RenderInternal()
		{
			if (m_Running)
			{
				Timer.Stop();
				float elapsedTime = (float)Timer.Elapsed.TotalSeconds;
				Timer.Reset();
				Timer.Start();
				GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
				if (FBO != null)
				{
					FBO.Enable();
				}
				Render(elapsedTime);
				Utilities.SpriteManager.RenderSpecialBatches();
				Utilities.BattleSpriteManager.RenderSpecialBatches();
				UIManager.Render(elapsedTime);
				if (FBO != null)
				{
					FBO.Disable();
					GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FBO.BufferID);
					GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
					GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
					GL.DrawBuffer(DrawBufferMode.Back);
					GL.BlitFramebuffer(0, 0, Window.ClientSize.Width, Window.ClientSize.Height, 0, 0, Window.ClientSize.Width, Window.ClientSize.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
				}
				Window.SwapBuffers();
			}
		}

		private void UpdateInternal()
		{
			if (!m_Running)
			{
				return;
			}
			NextUpdate = GetUpdateFrame();
			if (NextUpdate == CurrentUpdate)
			{
				RenderInternal();
				return;
			}
			int val = NextUpdate - CurrentUpdate;
			for (val = Math.Min(MAX_UPDATES, val); val > 0; val--)
			{
				Update();
			}
			Utilities.TextureManager.Update();
			CurrentUpdate = NextUpdate;
			RenderInternal();
		}

		public Rectangle GetViewport()
		{
			int[] array = new int[4];
			GL.GetInteger(GetPName.Viewport, array);
			return new Rectangle(array[0], array[1], array[2], array[3]);
		}

		public virtual void HandleMouseDown(MouseButtonEventArgs e)
		{
		}

		public virtual void HandleMouseUp(MouseButtonEventArgs e)
		{
		}

		public virtual void HandleGeneralMouseUp(MouseButtonEventArgs e)
		{
		}

		public virtual void HandleKeyDown(KeyboardKeyEventArgs e)
		{
		}

		public virtual void HandleKeyUp(KeyboardKeyEventArgs e)
		{
		}

		public virtual void HandleWheelUp(MouseWheelEventArgs e)
		{
		}

		public virtual void HandleWheelDown(MouseWheelEventArgs e)
		{
		}

		public virtual void HandleMouseMove(MouseMoveEventArgs e)
		{
		}

		internal virtual void HandleMousePositionChanged(int x, int y)
		{
		}

		internal virtual void HandleKeyPress(OpenTK.KeyPressEventArgs e)
		{
		}
	}
}