// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.GLFrameBuffer
using OpenTK.Graphics.OpenGL;

namespace SovereigntyTK.Utility
{
	public class GLFrameBuffer
	{
		public int BufferID;

		public int TextureID;

		public GLFrameBuffer(int Width, int Height, int Multisamples)
		{
			BufferID = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, BufferID);
			TextureID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2DMultisample, TextureID);
			GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, Multisamples, PixelInternalFormat.Rgba8, Width, Height, fixedsamplelocations: false);
			GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2DMultisample, TextureParameterName.TextureMagFilter, 9729);
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureID, 0);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}

		public void Enable()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, BufferID);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		}

		public void Disable()
		{
			GL.DrawBuffer(DrawBufferMode.Back);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}

		public void Dispose()
		{
			GL.DeleteFramebuffer(BufferID);
			GL.DeleteTexture(TextureID);
		}
	}
}