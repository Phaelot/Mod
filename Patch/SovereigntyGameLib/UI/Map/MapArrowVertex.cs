using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MapArrowVertex
	{
		public MapArrowVertex(Vector3 Position)
		{
			this.Position = Position;
		}

		public static GLVertexFormat GetFormat(int ShaderID)
		{
			int attribLocation = GL.GetAttribLocation(ShaderID, "in_Position");
			GLVertexFormat glvertexFormat = new GLVertexFormat();
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation, 3, VertexAttribPointerType.Float, false, 12, 0));
			return glvertexFormat;
		}

		public Vector3 Position;

		public static int Size = Marshal.SizeOf(typeof(MapArrowVertex));
	}
}
