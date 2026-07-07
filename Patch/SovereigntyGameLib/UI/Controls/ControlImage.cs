using System;
using System.Drawing;
using System.Xml.Linq;
using OpenTK;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI.Controls
{
	public class ControlImage : UIControl
	{
		public ControlImage(GameBase Game)
			: base(Game)
		{
		}

		protected override void ParseElement(XElement Element)
		{
			string localName;
			if ((localName = Element.Name.LocalName) != null && localName == "imagefile")
			{
				this.SetImageFile(Element.Value);
				return;
			}
			base.ParseElement(Element);
		}

		public override void Dispose()
		{
			this.RemoveTexture();
			base.Dispose();
		}

		public void SetImageFile(string Filename)
		{
			if (this.Sprite == null)
			{
				return;
			}
			if (this.Sprite.CurrentTexture != null)
			{
				this.RemoveTexture();
			}
			if (this.Game == null)
			{
				return;
			}
			this.Sprite.CurrentTexture = this.Game.Utilities.TextureManager.GetTexture(Filename);
			this.BitmapSprite = false;
		}

		public override void Render(GLShader Shader, float ElapsedTime)
		{
			base.Render(Shader, ElapsedTime);
		}

		private void RemoveTexture()
		{
			if (this.Sprite == null)
			{
				return;
			}
			if (this.BitmapSprite)
			{
				this.Sprite.CurrentTexture.Dispose();
			}
			else if (!this.TextureSprite)
			{
				this.Game.Utilities.TextureManager.ReleaseTexture(this.Sprite.CurrentTexture);
			}
			this.Sprite.CurrentTexture = null;
		}

		public void SetIndexedImage(GLTextureAtlas Atlas, string IconName)
		{
			if (this.Sprite.CurrentTexture != null)
			{
				this.RemoveTexture();
			}
			this.Sprite.CurrentTexture = Atlas;
			Vector4 namedTextureCoords = Atlas.GetNamedTextureCoords(IconName);
			this.Sprite.SetCoords(namedTextureCoords);
			this.TextureSprite = true;
		}

		public void SetImage(GLTexture Texture)
		{
			if (this.Sprite.CurrentTexture != null)
			{
				this.RemoveTexture();
			}
			this.Sprite.CurrentTexture = Texture;
			this.TextureSprite = true;
		}

		public void SetImage(Bitmap Image)
		{
			if (this.Sprite.CurrentTexture != null)
			{
				this.RemoveTexture();
			}
			this.Sprite.CurrentTexture = new GLTexture(Image);
			this.BitmapSprite = true;
		}

		private bool BitmapSprite;

		private bool TextureSprite;
	}
}
