using System;
using System.Drawing;
using OpenTK.Input;

namespace SovereigntyTK.Utility
{
	public abstract class GLBaseSprite
	{
		public abstract void UpdateVertices();

		public abstract void UpdateTextureCoords();

		public bool RenderOnTop
		{
			get
			{
				return this.m_RenderOnTop;
			}
			set
			{
				if (value == this.m_RenderOnTop)
				{
					return;
				}
				this.m_RenderOnTop = value;
				this.UpdateRenderOnTop();
			}
		}

		public GLBaseSprite(GameBase Game, GLSpriteBatch Batch, SpriteManager Manager)
		{
			this.Game = Game;
			this.Batch = Batch;
			this.Manager = Manager;
			this.BaseVertex = Batch.GetNextIndex() * 4;
		}

		public virtual void Dispose(bool DisposeBatch = false)
		{
			this.Batch.ReleaseSprite(this.BaseVertex / 4);
			if (DisposeBatch && this.Batch.ActiveCount == 0)
			{
				this.Manager.DisposeBatch(this.Batch);
			}
			this.Manager.SpriteDisposed(this);
		}

		public virtual void HandleMouseEnter()
		{
		}

		public virtual void HandleMouseLeave()
		{
		}

		public virtual void HandleMouseDown(MouseButton Button)
		{
		}

		public virtual void HandleMouseUp(MouseButton Button)
		{
		}

		public void BringToFront()
		{
			this.Manager.BringBatchToFront(this.Batch);
		}

		internal void SetImage(string Filename)
		{
			this.Batch.ReleaseSprite(this.BaseVertex / 4);
			if (this.m_RenderOnTop)
			{
				this.Batch = this.Manager.GetOnTopBatch(Filename, true);
			}
			else
			{
				this.Batch = this.Manager.GetBatch(Filename, true);
			}
			this.BaseVertex = this.Batch.GetNextIndex() * 4;
			this.UpdateVertices();
			this.UpdateTextureCoords();
		}

		internal void ForceToBack()
		{
			this.Manager.SetToBack(this.Batch);
		}

		public void SetAlpha(float Alpha)
		{
			this.Alpha = Alpha;
			for (int i = 0; i < 4; i++)
			{
				this.Batch.Vertices[this.BaseVertex + i].SetAlpha(Alpha);
			}
			this.Batch.BufferModified = true;
		}

		protected void UpdateRenderOnTop()
		{
			GLSpriteBatch batch = this.Manager.GetBatch(this.Batch.Filename, true);
			GLSpriteBatch onTopBatch = this.Manager.GetOnTopBatch(this.Batch.Filename, true);
			GLSpriteBatch glspriteBatch = batch;
			GLSpriteBatch glspriteBatch2 = onTopBatch;
			if (!this.m_RenderOnTop)
			{
				glspriteBatch = onTopBatch;
				glspriteBatch2 = batch;
			}
			glspriteBatch.ReleaseSprite(this.BaseVertex / 4);
			this.Batch = glspriteBatch2;
			this.BaseVertex = this.Batch.GetNextIndex() * 4;
			this.UpdateVertices();
			this.UpdateTextureCoords();
		}

		public void SetBlendColour(Color BlendColour)
		{
			this.BlendColour = BlendColour;
			for (int i = 0; i < 4; i++)
			{
				this.Batch.Vertices[this.BaseVertex + i].SetBlendColour(BlendColour);
			}
			this.Batch.BufferModified = true;
		}

		public GameBase Game;

		public GLSpriteBatch Batch;

		public SpriteManager Manager;

		protected int BaseVertex;

		protected bool m_RenderOnTop;

		protected float Alpha = 1f;

		protected Color BlendColour;
	}
}
