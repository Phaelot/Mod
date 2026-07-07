using System;
using System.Drawing;
using SovereigntyTK.Game;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI
{
	public class FloatingSpriteData
	{
		public float AlphaFactor
		{
			get
			{
				switch (this.Game.GameCore.Settings.GetIntSetting("TacticalBattleSpeed"))
				{
				case 1:
					return 0.1f;
				case 2:
					return 0.2f;
				case 3:
					return 0.3f;
				case 4:
					return 0.4f;
				case 5:
					return 0.5f;
				case 6:
					return 0.6f;
				case 7:
					return 0.7f;
				case 8:
					return 0.8f;
				case 9:
					return 0.9f;
				case 10:
					return 1f;
				default:
					return 0.5f;
				}
			}
		}

		public float SpeedFactor
		{
			get
			{
				switch (this.Game.GameCore.Settings.GetIntSetting("TacticalBattleSpeed"))
				{
				case 1:
					return 4f;
				case 2:
					return 5f;
				case 3:
					return 6f;
				case 4:
					return 7f;
				case 5:
					return 8f;
				case 6:
					return 10f;
				case 7:
					return 12f;
				case 8:
					return 14f;
				case 9:
					return 16f;
				case 10:
					return 18f;
				default:
					return 8f;
				}
			}
		}

		public float TimeFactor
		{
			get
			{
				switch (this.Game.GameCore.Settings.GetIntSetting("TacticalBattleSpeed"))
				{
				case 1:
					return 4f;
				case 2:
					return 3.5f;
				case 3:
					return 3f;
				case 4:
					return 2.5f;
				case 5:
					return 2f;
				case 6:
					return 1.8f;
				case 7:
					return 1.6f;
				case 8:
					return 1.4f;
				case 9:
					return 1.2f;
				case 10:
					return 1f;
				default:
					return 8f;
				}
			}
		}

		public FloatingSpriteData(SovereigntyGame Game, GLTexture Texture, PointF Position, PointF Size)
		{
			this.Game = Game;
			this.Sprite = Game.GameCore.Utilities.BattleSpriteManager.CreateSprite(Guid.NewGuid().ToString(), Texture, false);
			this.Sprite.SetPosition(Position.X, Position.Y);
			this.Sprite.SetSize(Size.X, Size.Y);
		}

		public void Update(float ElapsedTime)
		{
			if (!this.Enabled)
			{
				this.Sprite.SetAlpha(0f);
				return;
			}
			this.TotalTime += ElapsedTime;
			this.Alpha = (this.TotalTime * this.AlphaFactor - 0.2f) / 0.8f;
			this.Sprite.SetAlpha(1f - this.Alpha);
			this.Sprite.SetPosition(this.Sprite.Bounds.X, this.Sprite.Bounds.Y - ElapsedTime * this.SpeedFactor);
			if (this.TotalTime > this.TimeFactor)
			{
				this.Complete = true;
			}
		}

		public void Dispose()
		{
			this.Sprite.Dispose(true);
		}

		public SovereigntyGame Game;

		public GLSprite Sprite;

		public float Alpha = 1f;

		public float TotalTime;

		public bool Complete;

		public bool Enabled;
	}
}
