// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.ThreadedGLSLVideoPlayer
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Gst;
using Gst.App;
using Gst.BasePlugins;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Utility
{
	public class ThreadedGLSLVideoPlayer
	{
		public enum VideoPlayerState
		{
			STOPPED,
			LOADING,
			PLAYING,
			PAUSED
		}

		private static bool _gstInited;

		private PlayBin2 playBin;

		private AppSink appSink;

		private int width;

		private int height;

		private byte[] bufferY;

		private byte[] bufferU;

		private byte[] bufferV;

		private bool texturesOK;

		private bool isFrameNew;

		private object lockFrameBuf = new object();

		private bool isrunning = true;

		private Thread gstThread;

		private VideoPlayerState playerState;

		private GLShader Shader;

		private GLVertexBuffer VB;

		private VideoVertex[] Vertices;

		private GLTexture YTexture;

		private GLTexture UTexture;

		private GLTexture VTexture;

		private GameBase Game;

		private int YLoc;

		private int ULoc;

		private int VLoc;

		internal VideoPlayerState PlayerState
		{
			get
			{
				return playerState;
			}
			private set
			{
				playerState = value;
			}
		}

		public static void Init()
		{
			if (!_gstInited)
			{
				string[] args = new string[0];
				Application.InitCheck("", ref args);
			}
		}

		[DllImport("libgstreamer-0.10.so", CallingConvention = CallingConvention.Cdecl)]
		private static extern void gst_mini_object_unref(IntPtr raw);

		public ThreadedGLSLVideoPlayer(GameBase Game, GLShader Shader)
		{
			string text = GL.GetString(StringName.Version);
			if (text.Length < 1)
			{
				Console.WriteLine("OpenGL 2.0 not available. GLSL not supported.\nVersion: " + text);
			}
			int num = text[0];
			if (num < 2)
			{
				Console.WriteLine("OpenGL 2.0 not available. GLSL not supported.\nVersion: " + text);
			}
			this.Shader = Shader;
			this.Game = Game;
		}

		public void LoadVideo(string uri)
		{
			if (gstThread != null)
			{
				isrunning = false;
				gstThread.Join();
				gstThread = new Thread(KeepPolling);
			}
			if (playBin != null)
			{
				playerState = VideoPlayerState.STOPPED;
				Console.WriteLine("STOPPED");
				playBin.SetState(State.Null);
				playBin.Dispose();
				appSink.SetState(State.Null);
				appSink.Dispose();
				playBin = new PlayBin2();
				appSink = ElementFactory.Make("appsink", "sink") as AppSink;
				appSink.Caps = new Caps("video/x-raw-yuv");
				appSink.Drop = true;
				appSink.MaxBuffers = 8u;
				playBin.VideoSink = appSink;
			}
			else
			{
				playBin = new PlayBin2();
				appSink = ElementFactory.Make("appsink", "sink") as AppSink;
				appSink.Caps = new Caps("video/x-raw-yuv");
				appSink.Drop = true;
				appSink.MaxBuffers = 8u;
				playBin.VideoSink = appSink;
			}
			texturesOK = false;
			width = 0;
			height = 0;
			string text = uri;
			if (!text.StartsWith("file:///"))
			{
				text = "file:///" + uri;
			}
			StateChangeReturn stateChangeReturn = playBin.SetState(State.Ready);
			playBin.Uri = text;
			stateChangeReturn = playBin.SetState(State.Playing);
			Console.WriteLine(stateChangeReturn.ToString());
			playerState = VideoPlayerState.LOADING;
			Console.WriteLine("LOADING:" + text);
			if (gstThread == null)
			{
				gstThread = new Thread(KeepPolling);
			}
			isrunning = true;
			gstThread.Start();
		}

		private void Bus_Message(object o, MessageArgs args)
		{
			throw new NotImplementedException();
		}

		private void KeepPolling()
		{
			while (isrunning && Game.Running)
			{
				switch (playerState)
				{
					case VideoPlayerState.LOADING:
						{
							int result = 0;
							int result2 = 0;
							Gst.Buffer buffer2 = appSink.PullBuffer();
							if (buffer2 == null)
							{
								break;
							}
							Console.WriteLine(buffer2.Caps.ToString());
							int.TryParse(buffer2.Caps[0u].GetValue("width").Val.ToString(), out result);
							int.TryParse(buffer2.Caps[0u].GetValue("height").Val.ToString(), out result2);
							if (result * result2 != 0)
							{
								lock (lockFrameBuf)
								{
									width = result;
									height = result2;
									bufferY = new byte[width * height];
									bufferU = new byte[width * height / 4];
									bufferV = new byte[width * height / 4];
									IntPtr data2 = buffer2.Data;
									Marshal.Copy(data2, bufferY, 0, width * height);
									data2 = new IntPtr(data2.ToInt64() + width * height);
									Marshal.Copy(data2, bufferU, 0, width * height / 4);
									data2 = new IntPtr(data2.ToInt64() + width * height / 4);
									Marshal.Copy(data2, bufferV, 0, width * height / 4);
									isFrameNew = true;
									buffer2.Dispose();
								}
								Console.WriteLine("PLAYING");
								playerState = VideoPlayerState.PLAYING;
								continue;
							}
							break;
						}
					case VideoPlayerState.PLAYING:
						{
							Gst.Buffer buffer = appSink.PullBuffer();
							if (buffer != null)
							{
								lock (lockFrameBuf)
								{
									IntPtr data = buffer.Data;
									Marshal.Copy(data, bufferY, 0, width * height);
									data = new IntPtr(data.ToInt64() + width * height);
									Marshal.Copy(data, bufferU, 0, width * height / 4);
									data = new IntPtr(data.ToInt64() + width * height / 4);
									Marshal.Copy(data, bufferV, 0, width * height / 4);
									isFrameNew = true;
								}
								buffer.Dispose();
							}
							else
							{
								appSink.Seek(1.0, Format.Time, SeekFlags.Flush, SeekType.Set, 0L, SeekType.None, 0L);
								playBin.Seek(1.0, Format.Time, SeekFlags.Flush, SeekType.Set, 0L, SeekType.None, 0L);
							}
							break;
						}
				}
				Thread.Sleep(10);
			}
			PlayerState = VideoPlayerState.STOPPED;
			playBin.SetState(State.Null);
			playBin.Dispose();
			appSink.SetState(State.Null);
			appSink.Dispose();
			playBin = null;
			appSink = null;
		}

		public void Stop()
		{
			isrunning = false;
			if (gstThread != null)
			{
				gstThread.Join();
			}
		}

		public void Update()
		{
			switch (playerState)
			{
				case VideoPlayerState.PLAYING:
					if (!texturesOK)
					{
						SetupTexture(width, height);
						texturesOK = true;
					}
					if (isFrameNew)
					{
						lock (lockFrameBuf)
						{
							UpdateTexture(width, height);
						}
						isFrameNew = false;
					}
					break;
				case VideoPlayerState.STOPPED:
				case VideoPlayerState.LOADING:
				case VideoPlayerState.PAUSED:
					break;
			}
		}

		public void UpdateVertices(float x, float y, float w, float h)
		{
			Vertices = new VideoVertex[6];
			ref VideoVertex reference = ref Vertices[0];
			reference = new VideoVertex(new Vector3(x, y + h, 0f), new Vector2(0f, 1f));
			ref VideoVertex reference2 = ref Vertices[1];
			reference2 = new VideoVertex(new Vector3(x + w, y, 0f), new Vector2(1f, 0f));
			ref VideoVertex reference3 = ref Vertices[2];
			reference3 = new VideoVertex(new Vector3(x, y, 0f), new Vector2(0f, 0f));
			ref VideoVertex reference4 = ref Vertices[3];
			reference4 = new VideoVertex(new Vector3(x, y + h, 0f), new Vector2(0f, 1f));
			ref VideoVertex reference5 = ref Vertices[4];
			reference5 = new VideoVertex(new Vector3(x + w, y + h, 0f), new Vector2(1f, 1f));
			ref VideoVertex reference6 = ref Vertices[5];
			reference6 = new VideoVertex(new Vector3(x + w, y, 0f), new Vector2(1f, 0f));
			VB = new GLVertexBuffer(VideoVertex.GetFormat(Shader.GetID()));
			VB.SetBufferData(Vertices, BufferUsageHint.StaticDraw);
		}

		public void Draw()
		{
			if (texturesOK)
			{
				Shader.SetActive();
				VB.SetActive();
				YTexture.SetActive(TextureUnit.Texture0);
				UTexture.SetActive(TextureUnit.Texture1);
				VTexture.SetActive(TextureUnit.Texture2);
				GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
				YTexture.SetInactive(TextureUnit.Texture0);
				UTexture.SetInactive(TextureUnit.Texture1);
				VTexture.SetInactive(TextureUnit.Texture2);
				VB.SetInactive();
				Shader.SetInactive();
			}
		}

		private void UpdateTexture(int w, int h)
		{
			YTexture.BindData(bufferY, w, h, PixelInternalFormat.One, PixelFormat.Luminance);
			UTexture.BindData(bufferU, w / 2, h / 2, PixelInternalFormat.One, PixelFormat.Luminance);
			VTexture.BindData(bufferV, w / 2, h / 2, PixelInternalFormat.One, PixelFormat.Luminance);
		}

		private void SetupTexture(int w, int h)
		{
			YTexture = new GLTexture();
			UTexture = new GLTexture();
			VTexture = new GLTexture();
			Shader.SetInteger("y_sampler", 0);
			Shader.SetInteger("u_sampler", 1);
			Shader.SetInteger("v_sampler", 2);
		}
	}
}