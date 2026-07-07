using System;

namespace SovereigntyTK.Utility
{
	public class LoadedTextureData
	{
		public LoadedTextureData(GLTexture Tex)
		{
			this.Tex = Tex;
			this.ReferenceCount = 0;
		}

		public GLTexture Tex;

		public int ReferenceCount;
	}
}
