using System;
using OpenTK;

namespace SovereigntyTK.Utility
{
	public class GLCameraSpaceLineSprite : GLBaseSprite
	{
		public GLCameraSpaceLineSprite(GameBase Game, GLSpriteBatch Batch, SpriteManager Manager)
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
			this.Batch.Vertices[this.BaseVertex].Position = this.Point1Pos;
			this.Batch.Vertices[this.BaseVertex + 1].Position = this.Point2Pos;
			this.Batch.Vertices[this.BaseVertex + 2].Position = this.Point1Pos;
			this.Batch.Vertices[this.BaseVertex + 3].Position = this.Point2Pos;
			float num = this.Thickness * 0.5f;
			this.Batch.Vertices[this.BaseVertex].ExtraData = new Vector4(this.Point2Pos, num);
			this.Batch.Vertices[this.BaseVertex + 1].ExtraData = new Vector4(this.Point1Pos, -num);
			this.Batch.Vertices[this.BaseVertex + 2].ExtraData = new Vector4(this.Point2Pos, -num);
			this.Batch.Vertices[this.BaseVertex + 3].ExtraData = new Vector4(this.Point1Pos, num);
			for (int i = 0; i < 4; i++)
			{
				this.Batch.Vertices[this.BaseVertex + i].SetAlpha(this.Alpha);
			}
			this.Batch.BufferModified = true;
		}

		public void SetData(Vector3 Point1Pos, Vector3 Point2Pos, float Thickness)
		{
			this.Point1Pos = Point1Pos;
			this.Point2Pos = Point2Pos;
			this.Thickness = Thickness;
			this.UpdateVertices();
		}

		public Vector3 Point1Pos;

		public Vector3 Point2Pos;

		public float Thickness;
	}
}
