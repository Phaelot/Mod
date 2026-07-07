using System;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SovereigntyTK.Utility
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SpriteVertex
	{
		public SpriteVertex(Vector3 Position, Vector2 TexCoords)
		{
			this.Position = Position;
			this.TexCoords = TexCoords;
			this.BlendColour = Vector4.Zero;
			this.Data = new Vector2(1f, 0f);
			this.ExtraData = Vector4.Zero;
		}

		public SpriteVertex(Vector3 Position, Vector2 TexCoords, Color BlendColour)
		{
			this.Position = Position;
			this.TexCoords = TexCoords;
			this.BlendColour = new Vector4((float)BlendColour.R / 255f, (float)BlendColour.G / 255f, (float)BlendColour.B / 255f, (float)BlendColour.A / 255f);
			this.Data = new Vector2(1f, 0f);
			this.ExtraData = Vector4.Zero;
		}

		internal void SetAlpha(float Alpha)
		{
			this.Data.X = Alpha;
		}

		internal void SetBlendColour(Color BlendColour)
		{
			this.BlendColour = new Vector4((float)BlendColour.R / 255f, (float)BlendColour.G / 255f, (float)BlendColour.B / 255f, (float)BlendColour.A / 255f);
		}

		public static GLVertexFormat GetFormat(int ShaderID)
		{
			int attribLocation = GL.GetAttribLocation(ShaderID, "in_Position");
			int attribLocation2 = GL.GetAttribLocation(ShaderID, "in_TexCoords");
			int attribLocation3 = GL.GetAttribLocation(ShaderID, "in_BlendColour");
			int attribLocation4 = GL.GetAttribLocation(ShaderID, "in_Data");
			int attribLocation5 = GL.GetAttribLocation(ShaderID, "in_ExtraData");
			GLVertexFormat glvertexFormat = new GLVertexFormat();
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation, 3, VertexAttribPointerType.Float, false, 60, 0));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation2, 2, VertexAttribPointerType.Float, false, 60, 12));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation3, 4, VertexAttribPointerType.Float, false, 60, 20));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation4, 2, VertexAttribPointerType.Float, false, 60, 36));
			glvertexFormat.AddAttribute(new GLVertexAttribute(attribLocation5, 4, VertexAttribPointerType.Float, false, 60, 44));
			return glvertexFormat;
		}

		public Vector3 Position;

		public Vector2 TexCoords;

		public Vector4 BlendColour;

		public Vector2 Data;

		public Vector4 ExtraData;
	}
}
