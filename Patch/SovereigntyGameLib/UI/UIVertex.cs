using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct UIVertex
	{
		public UIVertex(Vector3 Position, Vector2 TexCoords)
		{
			this.Position = Position;
			this.TexCoords = TexCoords;
			this.Colour = new Vector4(0f, 0f, 0f, 0f);
			this.Data = new Vector2(1f, 0f);
		}

		public static GLVertexFormat GetFormat(int ShaderID)
		{
			int attribLocation = GL.GetAttribLocation(ShaderID, "in_Position");
			int attribLocation2 = GL.GetAttribLocation(ShaderID, "in_TexCoords");
			int attribLocation3 = GL.GetAttribLocation(ShaderID, "in_Colour");
			int attribLocation4 = GL.GetAttribLocation(ShaderID, "in_Data");
			GLVertexFormat glvertexFormat = new GLVertexFormat();
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation, 3, VertexAttribPointerType.Float, false, 44, 0));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation2, 2, VertexAttribPointerType.Float, false, 44, 12));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation3, 4, VertexAttribPointerType.Float, false, 44, 20));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation4, 2, VertexAttribPointerType.Float, false, 44, 36));
			return glvertexFormat;
		}

		public Vector3 Position;

		public Vector2 TexCoords;

		public Vector4 Colour;

		public Vector2 Data;

		public static int Size = Marshal.SizeOf(typeof(UIVertex));
	}
}
