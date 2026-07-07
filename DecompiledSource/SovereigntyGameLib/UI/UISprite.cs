// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.UI.UISprite
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK.UI;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI
{
	public class UISprite
	{
		public RectangleF Bounds;

		internal UIVertex[] Vertices;

		private GLVertexBuffer VB;

		internal GLTexture CurrentTexture;

		public static GLShader Shader;

		internal UISprite()
		{
			Bounds = new RectangleF(0f, 0f, 50f, 50f);
			Vertices = new UIVertex[6];
			ref UIVertex reference = ref Vertices[0];
			reference = new UIVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 1f));
			ref UIVertex reference2 = ref Vertices[1];
			reference2 = new UIVertex(new Vector3(0f, 0f, 0f), new Vector2(1f, 0f));
			ref UIVertex reference3 = ref Vertices[2];
			reference3 = new UIVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 0f));
			ref UIVertex reference4 = ref Vertices[3];
			reference4 = new UIVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 1f));
			ref UIVertex reference5 = ref Vertices[4];
			reference5 = new UIVertex(new Vector3(0f, 0f, 0f), new Vector2(1f, 1f));
			ref UIVertex reference6 = ref Vertices[5];
			reference6 = new UIVertex(new Vector3(0f, 0f, 0f), new Vector2(1f, 0f));
			VB = new GLVertexBuffer(UIVertex.GetFormat(Shader.GetID()));
			WriteVertices();
		}

		internal void SetCoords(Vector4 Coords)
		{
			float x = Coords.X;
			float y = Coords.Y;
			float z = Coords.Z;
			float w = Coords.W;
			Vertices[0].TexCoords = new Vector2(x, w);
			Vertices[1].TexCoords = new Vector2(z, y);
			Vertices[2].TexCoords = new Vector2(x, y);
			Vertices[3].TexCoords = new Vector2(x, w);
			Vertices[4].TexCoords = new Vector2(z, w);
			Vertices[5].TexCoords = new Vector2(z, y);
			WriteVertices();
		}

		internal void Dispose()
		{
			VB.Dispose();
		}

		internal void UpdateVertices()
		{
			float x = Bounds.X;
			float y = Bounds.Y;
			float x2 = x + Bounds.Width;
			float y2 = y + Bounds.Height;
			Vertices[0].Position = new Vector3(x, y2, 0f);
			Vertices[1].Position = new Vector3(x2, y, 0f);
			Vertices[2].Position = new Vector3(x, y, 0f);
			Vertices[3].Position = new Vector3(x, y2, 0f);
			Vertices[4].Position = new Vector3(x2, y2, 0f);
			Vertices[5].Position = new Vector3(x2, y, 0f);
			WriteVertices();
		}

		internal float GetAlpha()
		{
			return Vertices[0].Data.X;
		}

		public void SetAlpha(float Value)
		{
			for (int i = 0; i < 6; i++)
			{
				Vertices[i].Data.X = Value;
			}
			WriteVertices();
		}

		internal void WriteVertices()
		{
			VB.SetBufferData(Vertices, BufferUsageHint.DynamicDraw);
		}

		internal void Render(GLShader Shader)
		{
			if (CurrentTexture != null)
			{
				VB.SetActive();
				Shader.SetActive();
				CurrentTexture.SetActive(TextureUnit.Texture0);
				GL.DrawArrays(PrimitiveType.Triangles, 0, Vertices.Length);
				CurrentTexture.SetInactive(TextureUnit.Texture0);
				Shader.SetInactive();
				VB.SetInactive();
			}
		}

		internal bool HitTest(float X, float Y)
		{
			if (CurrentTexture == null)
			{
				return false;
			}
			CurrentTexture.SetActive(TextureUnit.Texture0);
			GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int @params);
			GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int params2);
			float num = (float)@params / Bounds.Width;
			float num2 = (float)params2 / Bounds.Height;
			X *= num;
			Y *= num2;
			byte[] array = new byte[@params * params2 * 4];
			GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
			CurrentTexture.SetInactive(TextureUnit.Texture0);
			int num3 = (int)Y * @params * 4 + (int)X * 4 + 3;
			if (num3 >= array.Length)
			{
				return false;
			}
			return array[num3] != 0;
		}

		internal void SetBlendColour(Color Col)
		{
			for (int i = 0; i < 6; i++)
			{
				Vertices[i].Colour = new Vector4((float)(int)Col.R / 255f, (float)(int)Col.G / 255f, (float)(int)Col.B / 255f, (float)(int)Col.A / 255f);
			}
			WriteVertices();
		}
	}
}