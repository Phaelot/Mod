using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Map
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MapOutlineVertex
	{
		public MapOutlineVertex(Vector3 Position, float AlphaValue)
		{
			this.Position = Position;
			this.ExtraData = new Vector2(AlphaValue, 0f);
		}

		public MapOutlineVertex(Vector3 Position, float ColourValue, float AlphaValue)
		{
			this.Position = Position;
			this.ExtraData = new Vector2(AlphaValue, ColourValue);
		}

		public static GLVertexFormat GetFormat(int ShaderID)
		{
			int attribLocation = GL.GetAttribLocation(ShaderID, "in_Position");
			int attribLocation2 = GL.GetAttribLocation(ShaderID, "in_Data");
			GLVertexFormat glvertexFormat = new GLVertexFormat();
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation, 3, VertexAttribPointerType.Float, false, 20, 0));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation2, 2, VertexAttribPointerType.Float, false, 20, 12));
			return glvertexFormat;
		}

		public Vector3 Position;

		public Vector2 ExtraData;

		public static int Size = Marshal.SizeOf(typeof(MapOutlineVertex));
	}
}
