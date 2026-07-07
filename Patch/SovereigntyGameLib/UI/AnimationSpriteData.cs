using System;
using OpenTK;
using SovereigntyTK.Game;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI
{
	public class AnimationSpriteData
	{
		public event AnimationDelegate OnAnimationCompleted;

		public AnimationSpriteData(SovereigntyGame Game, AnimationData Data, Vector2 Location, Vector2 Size, string Tag, bool BattleSprite = true)
		{
			this.Tag = Tag;
			if (BattleSprite)
			{
				this.AnimSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\combat\\anim\\" + Data.TextureName + ".png", false);
			}
			else
			{
				this.AnimSprite = Game.GameCore.Utilities.SpriteManager.CreateSprite("Data\\Images\\combat\\anim\\" + Data.TextureName + ".png", false);
			}
			this.AnimSprite.SetPosition(Location.X, Location.Y);
			this.AnimSprite.SetSize(Size.X, Size.Y);
			this.FramesX = Data.FramesX;
			this.FramesY = Data.FramesY;
			this.FramesTotal = Data.FrameCount;
			this.CurrentFrame = 0;
			this.ElapsedTime = 0f;
			this.TotalTime = Data.Time;
			this.AnimSprite.SetTextureCoords(this.GetTextureCoords(0));
		}

		public void Dispose()
		{
			this.AnimSprite.Dispose(false);
			this.OnAnimationCompleted = null;
		}

		public Vector4 GetTextureCoords(int Frame)
		{
			int num = Frame / this.FramesX;
			int num2 = Frame % this.FramesX;
			float num3 = (float)num2 / (float)this.FramesX;
			float num4 = (float)num / (float)this.FramesY;
			float num5 = num3 + 1f / (float)this.FramesX;
			float num6 = num4 + 1f / (float)this.FramesY;
			return new Vector4(num3, num4, num5, num6);
		}

		public void Update(float ElapsedTime)
		{
			if (this.ElapsedTime >= this.TotalTime)
			{
				return;
			}
			this.ElapsedTime += ElapsedTime;
			float num = this.ElapsedTime / this.TotalTime;
			if (num > 1f)
			{
				num = 1f;
			}
			float num2 = num * (float)this.FramesTotal;
			int num3 = (int)num2;
			if (this.CurrentFrame != num3)
			{
				this.CurrentFrame = num3;
				this.AnimSprite.SetTextureCoords(this.GetTextureCoords(this.CurrentFrame));
			}
			if (this.ElapsedTime >= this.TotalTime)
			{
				if (this.OnAnimationCompleted != null)
				{
					this.OnAnimationCompleted(this);
				}
				this.Complete = true;
			}
		}

		private int FramesX;

		private int FramesY;

		private int FramesTotal;

		private int CurrentFrame;

		private float ElapsedTime;

		private float TotalTime;

		private GLSprite AnimSprite;

		public bool Complete;

		public string Tag;
	}
}
