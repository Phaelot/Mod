using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SovereigntyTK.Utility
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VideoVertex
	{
		public VideoVertex(Vector3 Position, Vector2 TexCoords)
		{
			this.Position = Position;
			this.TexCoords = TexCoords;
		}

		public static GLVertexFormat GetFormat(int ShaderID)
		{
			int attribLocation = GL.GetAttribLocation(ShaderID, "in_Position");
			int attribLocation2 = GL.GetAttribLocation(ShaderID, "in_TexCoords");
			GLVertexFormat glvertexFormat = new GLVertexFormat();
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation, 3, VertexAttribPointerType.Float, false, 20, 0));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation2, 2, VertexAttribPointerType.Float, false, 20, 12));
			return glvertexFormat;
		}

		public Vector3 Position;

		public Vector2 TexCoords;

		public static int Size = Marshal.SizeOf(typeof(VideoVertex));
	}
}
