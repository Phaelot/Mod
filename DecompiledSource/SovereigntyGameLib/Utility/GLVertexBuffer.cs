// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLVertexBuffer
using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Utility
{
	public class GLVertexBuffer
	{
		private GLVertexFormat VertexFormat;

		private int BufferID;

		public GLVertexBuffer(GLVertexFormat VertexFormat)
		{
			this.VertexFormat = VertexFormat;
			BufferID = GL.GenBuffer();
			VertexFormat.ApplyToBuffer(BufferID);
		}

		public void SetBufferData<T>(T[] data, BufferUsageHint usage) where T : struct
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, BufferID);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Marshal.SizeOf(typeof(T)) * data.Length), data, usage);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void SetActive()
		{
			VertexFormat.ApplyToBuffer(BufferID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, BufferID);
			VertexFormat.MakeActive();
		}

		public void SetInactive()
		{
			VertexFormat.MakeInactive();
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void Dispose()
		{
			GL.DeleteBuffer(BufferID);
		}
	}
}