// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLTexture
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace SovereigntyTK.Utility
{
	public class GLTexture
	{
		private int TextureID;

		public int Width;

		public int Height;

		public GLTexture()
		{
			TextureID = 0;
		}

		public GLTexture(Bitmap Image)
		{
			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			Width = Image.Width;
			Height = Image.Height;
			BitmapData bitmapData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
			Image.UnlockBits(bitmapData);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void SetWrapMode(TextureWrapMode Mode)
		{
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)Mode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)Mode);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, 0);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void SetFilter(TextureMinFilter MinFilter, TextureMagFilter MagFilter)
		{
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)MinFilter);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)MagFilter);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public GLTexture(Stream s)
		{
			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			Bitmap bitmap = new Bitmap(s);
			Width = bitmap.Width;
			Height = bitmap.Height;
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
			bitmap.UnlockBits(bitmapData);
			bitmap.Dispose();
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void BindData(byte[] Data, int Width, int Height, PixelInternalFormat InternalFormat, OpenTK.Graphics.OpenGL.PixelFormat Format)
		{
			if (TextureID != 0)
			{
				GL.DeleteTexture(TextureID);
			}
			this.Width = Width;
			this.Height = Height;
			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, Width, Height, 0, Format, PixelType.UnsignedByte, Data);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void BindBitmap(Bitmap Image, bool GenerateMipMap = false)
		{
			if (TextureID != 0)
			{
				GL.DeleteTexture(TextureID);
			}
			Width = Image.Width;
			Height = Image.Height;
			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			BitmapData bitmapData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
			Image.UnlockBits(bitmapData);
			if (GenerateMipMap)
			{
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			}
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void SetActive(TextureUnit Location)
		{
			GL.ActiveTexture(Location);
			GL.BindTexture(TextureTarget.Texture2D, TextureID);
		}

		public void SetInactive(TextureUnit Location)
		{
			GL.ActiveTexture(Location);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void Dispose()
		{
			if (TextureID != 0)
			{
				GL.DeleteTexture(TextureID);
				TextureID = 0;
			}
		}
	}
}