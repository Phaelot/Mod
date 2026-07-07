using System;
using System.Drawing;
using OpenTK;

namespace SovereigntyTK.Utility
{
	public class GLCameraSpacePointSprite : GLBaseSprite
	{
		public GLCameraSpacePointSprite(GameBase Game, GLSpriteBatch Batch, SpriteManager Manager)
			: base(Game, Batch, Manager)
		{
			this.UpdateTextureCoords();
		}

		public override void UpdateTextureCoords()
		{
			this.Batch.Vertices[this.BaseVertex].TexCoords = new Vector2(0f, 0f);
			this.Batch.Vertices[this.BaseVertex + 1].TexCoords = new Vector2(1f, 0f);
			this.Batch.Vertices[this.BaseVertex + 2].TexCoords = new Vector2(0f, 1f);
			this.Batch.Vertices[this.BaseVertex + 3].TexCoords = new Vector2(1f, 1f);
			this.Batch.BufferModified = true;
		}

		public override void UpdateVertices()
		{
			float num = this.SizeW * 0.5f;
			float num2 = this.SizeH * 0.5f;
			this.Batch.Vertices[this.BaseVertex].Position = new Vector3(this.Location.X, 0f, this.Location.Y);
			this.Batch.Vertices[this.BaseVertex + 1].Position = new Vector3(this.Location.X, 0f, this.Location.Y);
			this.Batch.Vertices[this.BaseVertex + 2].Position = new Vector3(this.Location.X, 0f, this.Location.Y);
			this.Batch.Vertices[this.BaseVertex + 3].Position = new Vector3(this.Location.X, 0f, this.Location.Y);
			this.Batch.Vertices[this.BaseVertex].ExtraData = new Vector4(-num, -num2, 0f, 0f);
			this.Batch.Vertices[this.BaseVertex + 1].ExtraData = new Vector4(num, -num2, 0f, 0f);
			this.Batch.Vertices[this.BaseVertex + 2].ExtraData = new Vector4(-num, num2, 0f, 0f);
			this.Batch.Vertices[this.BaseVertex + 3].ExtraData = new Vector4(num, num2, 0f, 0f);
			for (int i = 0; i < 4; i++)
			{
				this.Batch.Vertices[this.BaseVertex + i].SetAlpha(this.Alpha);
			}
			this.Batch.BufferModified = true;
		}

		public void SetPosition(float x, float y)
		{
			this.Location = new PointF(x, y);
			this.UpdateVertices();
		}

		public void SetSize(float w, float h)
		{
			this.SizeW = w;
			this.SizeH = h;
			this.UpdateVertices();
		}

		private PointF Location;

		private float SizeW;

		private float SizeH;
	}
}
