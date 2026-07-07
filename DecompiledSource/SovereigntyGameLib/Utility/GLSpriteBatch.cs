// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLSpriteBatch
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SovereigntyTK;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Utility
{
	public class GLSpriteBatch
	{
		private static GLShader SpriteShader;

		internal GLShader LocalShader;

		internal GLTexture BatchTexture;

		private GLVertexBuffer VB;

		private GLIndexBuffer IB;

		internal SpriteVertex[] Vertices;

		private bool[] ActiveSprites;

		private GameBase Game;

		private static int MAX_SIZE = 5000000;

		private static int INITIAL_SIZE = 350;

		private static int INCREASE_SIZE = 100;

		internal int ActiveCount;

		public float GlobalAlpha = 1f;

		public int TexCountX;

		public int TexCountY;

		private int CurrentSize;

		public bool BufferModified;

		private int IndexPointer;

		public string Filename;

		public GLSpriteBatch(GameBase Game, string TextureName, int SizeX, int SizeY)
		{
			GL.Disable(EnableCap.CullFace);
			this.Game = Game;
			Filename = TextureName.ToLowerInvariant();
			if (SpriteShader == null)
			{
				SpriteShader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Sprite.vert", "Data\\Shaders\\Sprite.frag", UsesCamera: true);
				SpriteShader.SetTexture("Texture", 0);
			}
			BatchTexture = Game.Utilities.TextureManager.GetTexture(TextureName);
			TexCountX = BatchTexture.Width / SizeX;
			TexCountY = BatchTexture.Height / SizeY;
			CreateVertices();
		}

		public GLSpriteBatch(GameBase Game, string TextureName)
		{
			GL.Disable(EnableCap.CullFace);
			this.Game = Game;
			Filename = TextureName.ToLowerInvariant();
			if (SpriteShader == null)
			{
				SpriteShader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Sprite.vert", "Data\\Shaders\\Sprite.frag", UsesCamera: true);
				SpriteShader.SetTexture("Texture", 0);
			}
			BatchTexture = Game.Utilities.TextureManager.GetTexture(TextureName);
			TexCountX = BatchTexture.Width / 128;
			TexCountY = BatchTexture.Height / 128;
			CreateVertices();
		}

		public void Dispose()
		{
			if (VB != null)
			{
				VB.Dispose();
				IB.Dispose();
				Game.Utilities.TextureManager.ReleaseTexture(BatchTexture);
				VB = null;
				IB = null;
				BatchTexture = null;
			}
		}

		public int GetNextIndex()
		{
			int indexPointer = IndexPointer;
			while (ActiveSprites[IndexPointer])
			{
				IndexPointer++;
				if (IndexPointer >= CurrentSize)
				{
					IndexPointer = 0;
				}
				if (IndexPointer == indexPointer)
				{
					Resize();
				}
			}
			ActiveSprites[IndexPointer] = true;
			ActiveCount++;
			return IndexPointer;
		}

		private void Resize()
		{
			if (CurrentSize >= MAX_SIZE)
			{
				throw new Exception("Too many sprites in batch");
			}
			CurrentSize += INCREASE_SIZE;
			INCREASE_SIZE += 100;
			if (CurrentSize > MAX_SIZE)
			{
				CurrentSize = MAX_SIZE;
			}
			SpriteVertex[] array = new SpriteVertex[CurrentSize * 4];
			Vertices.CopyTo(array, 0);
			Vertices = array;
			IndexPointer = ActiveSprites.Length;
			bool[] array2 = new bool[CurrentSize];
			ActiveSprites.CopyTo(array2, 0);
			ActiveSprites = array2;
			CreateIndices();
		}

		private void CreateVertices()
		{
			CurrentSize = INITIAL_SIZE;
			Vertices = new SpriteVertex[INITIAL_SIZE * 4];
			ActiveSprites = new bool[INITIAL_SIZE];
			GLShader gLShader = SpriteShader;
			if (LocalShader != null)
			{
				gLShader = LocalShader;
			}
			VB = new GLVertexBuffer(SpriteVertex.GetFormat(gLShader.GetID()));
			IB = new GLIndexBuffer();
			CreateIndices();
		}

		private void CreateIndices()
		{
			int num = 0;
			uint[] array = new uint[CurrentSize * 6];
			for (uint num2 = 0u; num2 < CurrentSize; num2++)
			{
				uint num3 = num2 * 4;
				array[num++] = num3;
				array[num++] = num3 + 1;
				array[num++] = num3 + 2;
				array[num++] = num3 + 1;
				array[num++] = num3 + 3;
				array[num++] = num3 + 2;
			}
			IB.SetBufferData(array, BufferUsageHint.DynamicDraw);
		}

		public GLSpriteBatch(GLShader Shader, string TextureName, GLTexture Texture)
		{
			Filename = TextureName.ToLowerInvariant();
			LocalShader = Shader;
			BatchTexture = Texture;
			TexCountX = BatchTexture.Width / 128;
			TexCountY = BatchTexture.Height / 128;
			CreateVertices();
		}

		public GLSpriteBatch(GameBase Game, string TextureName, GLTexture Texture)
		{
			this.Game = Game;
			Filename = TextureName.ToLowerInvariant();
			if (SpriteShader == null)
			{
				SpriteShader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Sprite.vert", "Data\\Shaders\\Sprite.frag", UsesCamera: true);
				SpriteShader.SetTexture("Texture", 0);
			}
			BatchTexture = Texture;
			CreateVertices();
		}

		public GLSpriteBatch(GameBase Game, string TextureName, string LookupName)
		{
			this.Game = Game;
			Filename = TextureName.ToLowerInvariant();
			if (SpriteShader == null)
			{
				SpriteShader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Sprite.vert", "Data\\Shaders\\Sprite.frag", UsesCamera: true);
				SpriteShader.SetTexture("Texture", 0);
			}
			BatchTexture = Game.Utilities.TextureManager.GetTexture(TextureName, LookupName);
			CreateVertices();
		}

		public void Render()
		{
			if (VB != null)
			{
				if (BufferModified)
				{
					BufferModified = false;
					VB.SetBufferData(Vertices, BufferUsageHint.DynamicDraw);
				}
				GLShader gLShader = SpriteShader;
				if (LocalShader != null)
				{
					gLShader = LocalShader;
				}
				if (ActiveCount != 0)
				{
					gLShader.SetFloat("GlobalAlpha", GlobalAlpha);
					gLShader.SetActive();
					VB.SetActive();
					IB.SetActive();
					BatchTexture.SetActive(TextureUnit.Texture0);
					GL.DrawElements(PrimitiveType.Triangles, 6 * CurrentSize, DrawElementsType.UnsignedInt, 0);
					BatchTexture.SetInactive(TextureUnit.Texture0);
					VB.SetInactive();
					IB.SetInactive();
					gLShader.SetInactive();
				}
			}
		}

		internal void ReleaseSprite(int SpriteID)
		{
			int num = SpriteID * 4;
			Vertices[num++].Position = Vector3.Zero;
			Vertices[num++].Position = Vector3.Zero;
			Vertices[num++].Position = Vector3.Zero;
			Vertices[num++].Position = Vector3.Zero;
			ActiveSprites[SpriteID] = false;
			BufferModified = true;
			ActiveCount--;
		}

		internal Vector4 GetCoords(int Index)
		{
			int num = Index % TexCountX;
			int num2 = Index / TexCountX;
			float num3 = 1f / (float)TexCountX;
			float num4 = 1f / (float)TexCountY;
			Vector4 result = default(Vector4);
			result.X = num3 * (float)num;
			result.Y = num4 * (float)num2;
			result.Z = result.X + num3;
			result.W = result.Y + num4;
			float num5 = 0.5f / (float)BatchTexture.Width;
			float num6 = 0.5f / (float)BatchTexture.Height;
			result.X += num5;
			result.Y += num6;
			result.Z -= num5;
			result.W -= num6;
			return result;
		}

		internal Vector4 GetCoords(string IconName)
		{
			if (BatchTexture is GLTextureAtlas)
			{
				return (BatchTexture as GLTextureAtlas).GetNamedTextureCoords(IconName);
			}
			throw new Exception("Attempted to perform atlas lookup on normal texture");
		}

		internal void DisableFiltering()
		{
			if (BatchTexture != null)
			{
				BatchTexture.SetFilter(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
			}
		}
	}
}