// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLIndexBuffer
using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace SovereigntyTK.Utility
{
	public class GLIndexBuffer
	{
		private int BufferID;

		public GLIndexBuffer()
		{
			BufferID = GL.GenBuffer();
		}

		public void SetBufferData<T>(T[] data, BufferUsageHint usage) where T : struct
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, BufferID);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Marshal.SizeOf(typeof(T)) * data.Length), data, usage);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		public void SetActive()
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, BufferID);
		}

		public void SetInactive()
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		public void Dispose()
		{
			GL.DeleteBuffer(BufferID);
		}
	}
}