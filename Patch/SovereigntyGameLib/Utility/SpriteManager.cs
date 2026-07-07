using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK.UI;

namespace SovereigntyTK.Utility
{
	public class SpriteManager
	{
		public event SpriteBatchDelegate OnBatchCreated;

		public SpriteManager(FileManager FileSystem, CameraBase Camera)
		{
			this.FileSystem = FileSystem;
			this.Batches = new List<GLSpriteBatch>();
			this.OnTopBatches = new List<GLSpriteBatch>();
			this.InteractiveSprites = new List<GLBaseSprite>();
			this.Shader = new GLShader(FileSystem, "Data\\Shaders\\Sprite.vert", "Data\\Shaders\\Sprite.frag");
			this.LineShader = new GLShader(FileSystem, "Data\\Shaders\\LineSprite.vert", "Data\\Shaders\\LineSprite.frag");
			this.PointShader = new GLShader(FileSystem, "Data\\Shaders\\PointSprite.vert", "Data\\Shaders\\PointSprite.frag");
			Camera.ViewMatrixChanged += this.Camera_ViewMatrixChanged;
		}

		private void Camera_ViewMatrixChanged()
		{
			this.UpdateMatrices(this.Shader);
		}

		private void UpdateMatrices(GLShader Shader)
		{
			Matrix4 identity = Matrix4.Identity;
			Shader.SetMatrix("World", ref identity);
			Shader.SetMatrix("View", ref this.Camera.ViewMatrix);
			Shader.SetMatrix("Projection", ref this.Camera.ProjectionMatrix);
		}

		public SpriteManager(GameBase Game)
		{
			this.Game = Game;
			this.Batches = new List<GLSpriteBatch>();
			this.OnTopBatches = new List<GLSpriteBatch>();
			this.InteractiveSprites = new List<GLBaseSprite>();
			this.Shader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\Sprite.vert", "Data\\Shaders\\Sprite.frag", true);
			this.LineShader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\LineSprite.vert", "Data\\Shaders\\LineSprite.frag", true);
			this.PointShader = Game.Utilities.ShaderManager.GetShader("Data\\Shaders\\PointSprite.vert", "Data\\Shaders\\PointSprite.frag", true);
			this.Shader.SetTexture("Texture", 0);
			this.LineShader.SetTexture("Texture", 0);
			this.PointShader.SetTexture("Texture", 0);
		}

		public GLSprite CreateSprite(string BatchName, GLTexture Texture, bool Interactive = false)
		{
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == BatchName.ToLowerInvariant());
			if (glspriteBatch == null)
			{
				if (this.Game != null)
				{
					glspriteBatch = new GLSpriteBatch(this.Game, BatchName, Texture);
				}
				else
				{
					glspriteBatch = new GLSpriteBatch(this.Shader, BatchName, Texture);
				}
				this.Batches.Add(glspriteBatch);
				if (this.OnBatchCreated != null)
				{
					this.OnBatchCreated(glspriteBatch);
				}
			}
			GLSprite glsprite = new GLSprite(this.Game, glspriteBatch, this);
			if (Interactive)
			{
				this.InteractiveSprites.Add(glsprite);
			}
			return glsprite;
		}

		public GLCameraSpaceLineSprite CreateLineSprite(string BatchName, GLTexture Texture, bool Interactive = false)
		{
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == BatchName.ToLowerInvariant());
			if (glspriteBatch == null)
			{
				if (this.Game != null)
				{
					glspriteBatch = new GLSpriteBatch(this.Game, BatchName, Texture);
				}
				else
				{
					glspriteBatch = new GLSpriteBatch(this.LineShader, BatchName, Texture);
				}
				this.Batches.Add(glspriteBatch);
				if (this.OnBatchCreated != null)
				{
					this.OnBatchCreated(glspriteBatch);
				}
			}
			GLCameraSpaceLineSprite glcameraSpaceLineSprite = new GLCameraSpaceLineSprite(this.Game, glspriteBatch, this);
			if (Interactive)
			{
				this.InteractiveSprites.Add(glcameraSpaceLineSprite);
			}
			return glcameraSpaceLineSprite;
		}

		public GLCameraSpacePointSprite CreatePointSprite(string BatchName, GLTexture Texture, bool Interactive = false)
		{
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == BatchName.ToLowerInvariant());
			if (glspriteBatch == null)
			{
				if (this.Game != null)
				{
					glspriteBatch = new GLSpriteBatch(this.Game, BatchName, Texture);
				}
				else
				{
					glspriteBatch = new GLSpriteBatch(this.PointShader, BatchName, Texture);
				}
				this.Batches.Add(glspriteBatch);
				if (this.OnBatchCreated != null)
				{
					this.OnBatchCreated(glspriteBatch);
				}
			}
			GLCameraSpacePointSprite glcameraSpacePointSprite = new GLCameraSpacePointSprite(this.Game, glspriteBatch, this);
			if (Interactive)
			{
				this.InteractiveSprites.Add(glcameraSpacePointSprite);
			}
			return glcameraSpacePointSprite;
		}

		public GLSprite CreateSprite(string TextureName, bool Interactive = false)
		{
			TextureName = TextureName.ToLowerInvariant();
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == TextureName.ToLowerInvariant());
			if (glspriteBatch == null)
			{
				glspriteBatch = new GLSpriteBatch(this.Game, TextureName);
				this.Batches.Add(glspriteBatch);
			}
			GLSprite glsprite = new GLSprite(this.Game, glspriteBatch, this);
			if (Interactive)
			{
				this.InteractiveSprites.Add(glsprite);
			}
			return glsprite;
		}

		public GLSprite CreateIndexedSprite(string TextureName, int IconIndex, bool Interactive = false, int SizeX = 128, int SizeY = 128)
		{
			TextureName = TextureName.ToLowerInvariant();
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == TextureName.ToLowerInvariant());
			if (glspriteBatch == null)
			{
				glspriteBatch = new GLSpriteBatch(this.Game, TextureName, SizeX, SizeY);
				this.Batches.Add(glspriteBatch);
			}
			GLSprite glsprite = new GLSprite(this.Game, glspriteBatch, this);
			glsprite.SetTextureCoords(IconIndex);
			if (Interactive)
			{
				this.InteractiveSprites.Add(glsprite);
			}
			return glsprite;
		}

		public GLSprite CreateIndexedSprite(string BatchName, int IconIndex, GLTexture Texture, bool Interactive = false)
		{
			BatchName = BatchName.ToLowerInvariant();
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == BatchName);
			if (glspriteBatch == null)
			{
				if (this.Game != null)
				{
					glspriteBatch = new GLSpriteBatch(this.Game, BatchName, Texture);
				}
				else
				{
					glspriteBatch = new GLSpriteBatch(this.Shader, BatchName, Texture);
				}
				this.Batches.Add(glspriteBatch);
			}
			GLSprite glsprite = new GLSprite(this.Game, glspriteBatch, this);
			glsprite.SetTextureCoords(IconIndex);
			if (Interactive)
			{
				this.InteractiveSprites.Add(glsprite);
			}
			return glsprite;
		}

		public GLSprite CreateIndexedSprite(string TextureName, string IconName, bool Interactive = false)
		{
			TextureName = TextureName.ToLowerInvariant();
			IconName = IconName.ToLowerInvariant();
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == TextureName.ToLowerInvariant());
			if (glspriteBatch == null)
			{
				string text = Path.GetDirectoryName(TextureName) + "\\" + Path.GetFileNameWithoutExtension(TextureName) + ".xml";
				glspriteBatch = new GLSpriteBatch(this.Game, TextureName, text);
				this.Batches.Add(glspriteBatch);
			}
			GLSprite glsprite = new GLSprite(this.Game, glspriteBatch, this);
			glsprite.SetTextureCoords(IconName);
			if (Interactive)
			{
				this.InteractiveSprites.Add(glsprite);
			}
			return glsprite;
		}

		public void Render()
		{
			foreach (GLSpriteBatch glspriteBatch in this.Batches)
			{
				glspriteBatch.Render();
			}
		}

		public void Dispose()
		{
			foreach (GLSpriteBatch glspriteBatch in this.Batches)
			{
				glspriteBatch.Dispose();
			}
			this.Batches.Clear();
		}

		internal bool HandleMouseMove(MouseMoveEventArgs e)
		{
			if (this.InteractionDisabled)
			{
				return false;
			}
			Vector3 terrainIntersect = this.Game.Camera.GetTerrainIntersect(e.X, e.Y);
			GLSprite glsprite = null;
			foreach (GLBaseSprite glbaseSprite in this.InteractiveSprites)
			{
				GLSprite glsprite2 = (GLSprite)glbaseSprite;
				if (!glsprite2.IgnoreMouse && glsprite2.PointInside(terrainIntersect.X, terrainIntersect.Z))
				{
					glsprite = glsprite2;
					break;
				}
			}
			if (glsprite != this.LastMouseoverSprite)
			{
				if (this.LastMouseoverSprite != null)
				{
					this.LastMouseoverSprite.HandleMouseLeave();
				}
				if (glsprite != null)
				{
					glsprite.HandleMouseEnter();
				}
				this.LastMouseoverSprite = glsprite;
			}
			return this.LastMouseoverSprite != null;
		}

		public GLSpriteBatch GetOnTopBatch(string Filename, bool Create = true)
		{
			Filename = Filename.ToLowerInvariant();
			GLSpriteBatch glspriteBatch = this.OnTopBatches.SingleOrDefault((GLSpriteBatch x) => x.Filename == Filename);
			if (glspriteBatch == null && Create)
			{
				if (this.Game != null)
				{
					glspriteBatch = new GLSpriteBatch(this.Game, Filename);
					this.OnTopBatches.Add(glspriteBatch);
				}
				else
				{
					GLSpriteBatch glspriteBatch2 = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == Filename);
					glspriteBatch = new GLSpriteBatch(glspriteBatch2.LocalShader, Filename, glspriteBatch2.BatchTexture);
					this.OnTopBatches.Add(glspriteBatch);
				}
			}
			return glspriteBatch;
		}

		public GLSpriteBatch GetBatch(string Filename, bool Create = true)
		{
			Filename = Filename.ToLowerInvariant();
			GLSpriteBatch glspriteBatch = this.Batches.SingleOrDefault((GLSpriteBatch x) => x.Filename == Filename);
			if (glspriteBatch == null && Create)
			{
				glspriteBatch = new GLSpriteBatch(this.Game, Filename);
				this.Batches.Add(glspriteBatch);
			}
			return glspriteBatch;
		}

		internal bool HandleMouseDown(MouseButtonEventArgs e)
		{
			if (this.InteractionDisabled)
			{
				return false;
			}
			Vector3 terrainIntersect = this.Game.Camera.GetTerrainIntersect(e.X, e.Y);
			GLSprite glsprite = null;
			foreach (GLBaseSprite glbaseSprite in this.InteractiveSprites)
			{
				GLSprite glsprite2 = (GLSprite)glbaseSprite;
				if (!glsprite2.IgnoreMouseClicks && glsprite2.PointInside(terrainIntersect.X, terrainIntersect.Z))
				{
					glsprite = glsprite2;
					break;
				}
			}
			if (glsprite == null)
			{
				return false;
			}
			glsprite.HandleMouseDown(e.Button);
			return true;
		}

		internal bool HandleMouseUp(MouseButtonEventArgs e)
		{
			if (this.InteractionDisabled)
			{
				return false;
			}
			Vector3 terrainIntersect = this.Game.Camera.GetTerrainIntersect(e.X, e.Y);
			GLSprite glsprite = null;
			foreach (GLBaseSprite glbaseSprite in this.InteractiveSprites)
			{
				GLSprite glsprite2 = (GLSprite)glbaseSprite;
				if (!glsprite2.IgnoreMouseClicks && glsprite2.PointInside(terrainIntersect.X, terrainIntersect.Z))
				{
					glsprite = glsprite2;
					break;
				}
			}
			if (glsprite == null)
			{
				return false;
			}
			glsprite.HandleMouseUp(e.Button);
			return true;
		}

		internal void SpriteDisposed(GLBaseSprite Sprite)
		{
			if (Thread.CurrentThread.Name != "Main Thread")
			{
				throw new Exception("Only main thread should access sprites");
			}
			this.InteractiveSprites.Remove(Sprite);
		}

		public void RenderSpecialBatches()
		{
			foreach (GLSpriteBatch glspriteBatch in this.OnTopBatches)
			{
				glspriteBatch.Render();
			}
		}

		public void SetToBack(GLSpriteBatch Batch)
		{
			if (this.Batches.Contains(Batch))
			{
				this.Batches.Remove(Batch);
				this.Batches.Insert(0, Batch);
			}
			if (this.OnTopBatches.Contains(Batch))
			{
				this.OnTopBatches.Remove(Batch);
				this.OnTopBatches.Insert(0, Batch);
			}
		}

		internal void SetGlobalAlpha(float AlphaValue)
		{
			foreach (GLSpriteBatch glspriteBatch in this.Batches)
			{
				glspriteBatch.GlobalAlpha = AlphaValue;
			}
		}

		internal void DisposeBatch(GLSpriteBatch Batch)
		{
			this.Batches.Remove(Batch);
			Batch.Dispose();
		}

		internal void BringBatchToFront(GLSpriteBatch Batch)
		{
			if (this.Batches.Contains(Batch))
			{
				this.Batches.Remove(Batch);
				this.Batches.Add(Batch);
			}
			if (this.OnTopBatches.Contains(Batch))
			{
				this.OnTopBatches.Remove(Batch);
				this.OnTopBatches.Add(Batch);
			}
		}

		public List<GLSpriteBatch> Batches;

		public List<GLSpriteBatch> OnTopBatches;

		public GameBase Game;

		public List<GLBaseSprite> InteractiveSprites;

		private GLBaseSprite LastMouseoverSprite;

		public bool InteractionDisabled;

		private GLShader Shader;

		private GLShader LineShader;

		private GLShader PointShader;

		private FileManager FileSystem;

		private CameraBase Camera;
	}
}
