using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using OpenTK;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Utility;

namespace SovereigntyTK.UI
{
	public class UnitBattleSprite
	{
		public event AnimationDelegate OnAnimationCompleted;

		public UnitBattleSprite(SovereigntyGame Game, WorkingUnit Unit)
		{
			if (UnitBattleSprite.UnitImages == null)
			{
				UnitBattleSprite.UnitImages = new Dictionary<string, Bitmap>();
				UnitBattleSprite.FlagImages = new Dictionary<string, Bitmap>();
				UnitBattleSprite.Highlight = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\HUD\\MainHUDNew\\stackselect.png", FileTypes.Application, FileModes.ReadOnly, true));
				UnitBattleSprite.BarOutline = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\Combat\\healthbar.png", FileTypes.Application, FileModes.ReadOnly, true));
				UnitBattleSprite.NoMove = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\Units\\Combat\\Unit_nomove.png", FileTypes.Application, FileModes.ReadOnly, true));
				UnitBattleSprite.NoAttack = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\Units\\Combat\\Unit_noattack.png", FileTypes.Application, FileModes.ReadOnly, true));
				UnitBattleSprite.AttackAvailImage = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\Combat\\attack_available.png", FileTypes.Application, FileModes.ReadOnly, true));
				UnitBattleSprite.MoveAvailImage = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\Combat\\move_available.png", FileTypes.Application, FileModes.ReadOnly, true));
				UnitBattleSprite.FlagOutline = new Bitmap(Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\HUD\\flags\\flagoutline.png", FileTypes.Application, FileModes.ReadOnly, true));
			}
			this.Game = Game;
			this.Unit = Unit;
			this.FloatingSprites = new List<FloatingSpriteData>();
			this.Animations = new List<AnimationSpriteData>();
			this.HighlightSprite = Game.GameCore.Utilities.BattleSpriteManager.CreateSprite("Data\\Images\\Units\\stack_highlight.png", false);
			this.HighlightSprite.RenderOnTop = true;
			this.CounterImage = new Bitmap(180, 188);
			this.Texture = new GLTexture(this.CounterImage);
			this.Sprite = Game.GameCore.Utilities.BattleSpriteManager.CreateSprite(Guid.NewGuid().ToString(), this.Texture, false);
		}

		public void Hide()
		{
			this.Visible = false;
			this.UpdateImage();
		}

		public void Show()
		{
			this.Visible = true;
			this.UpdateImage();
		}

		internal void Dispose()
		{
			this.Sprite.Dispose(true);
			this.Texture.Dispose();
			this.CounterImage.Dispose();
			this.HighlightSprite.Dispose(false);
			foreach (FloatingSpriteData floatingSpriteData in this.FloatingSprites)
			{
				floatingSpriteData.Dispose();
			}
			foreach (AnimationSpriteData animationSpriteData in this.Animations)
			{
				animationSpriteData.Dispose();
			}
			this.OnAnimationCompleted = null;
		}

		private unsafe Bitmap CreateMaskedFlag(Bitmap input)
		{
			Bitmap bitmap = new Bitmap(input.Width, input.Height);
			Graphics graphics = Graphics.FromImage(bitmap);
			graphics.FillEllipse(Brushes.Red, 7, 2, 70, 70);
			Bitmap bitmap2 = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb);
			Rectangle rectangle = new Rectangle(0, 0, input.Width, input.Height);
			BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			BitmapData bitmapData2 = input.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			BitmapData bitmapData3 = bitmap2.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			for (int i = 0; i < input.Height; i++)
			{
				byte* ptr = (byte*)bitmapData.Scan0 + i * bitmapData.Stride;
				byte* ptr2 = (byte*)bitmapData2.Scan0 + i * bitmapData2.Stride;
				byte* ptr3 = (byte*)bitmapData3.Scan0 + i * bitmapData3.Stride;
				for (int j = 0; j < input.Width; j++)
				{
					ptr3[4 * j] = ptr2[4 * j];
					ptr3[4 * j + 1] = ptr2[4 * j + 1];
					ptr3[4 * j + 2] = ptr2[4 * j + 2];
					ptr3[4 * j + 3] = ptr[4 * j + 2];
				}
			}
			bitmap.UnlockBits(bitmapData);
			input.UnlockBits(bitmapData2);
			bitmap2.UnlockBits(bitmapData3);
			input.Dispose();
			bitmap.Dispose();
			return bitmap2;
		}

		public void UpdateImage()
		{
			if (this.Unit.BattleData != null && this.Unit.BattleData.AutoBattle != null && !this.Unit.BattleData.AutoBattle.PlayerWatching)
			{
				return;
			}
			string text;
			if (this.Unit.CarriedUnit != null && this.Unit.Class == UnitClasses.Naval && this.Unit.BattleData != null && this.Unit.BattleData.Battle != null && this.Unit.BattleData.Battle.DeployPhase)
			{
				text = "Data\\Images\\Units\\Combat\\" + this.Unit.CarriedUnit.ImageFile + ".png";
			}
			else
			{
				text = "Data\\Images\\Units\\Combat\\" + this.Unit.ImageFile + ".png";
			}
			if (!UnitBattleSprite.UnitImages.ContainsKey(text))
			{
				UnitBattleSprite.UnitImages.Add(text, new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile(text, FileTypes.Application, FileModes.ReadOnly, true)));
			}
			Bitmap bitmap = UnitBattleSprite.UnitImages[text];
			Graphics graphics = Graphics.FromImage(this.CounterImage);
			graphics.Clear(Color.Transparent);
			Rectangle rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			Rectangle rectangle2 = new Rectangle(16, 16, 152, 160);
			graphics.DrawImage(bitmap, rectangle2, rectangle, GraphicsUnit.Pixel);
			if (this.Unit.BattleData.Selected)
			{
				rectangle = new Rectangle(0, 0, UnitBattleSprite.Highlight.Width, UnitBattleSprite.Highlight.Height);
				rectangle2 = new Rectangle(0, 0, 180, 188);
				graphics.DrawImage(UnitBattleSprite.Highlight, rectangle2, rectangle, GraphicsUnit.Pixel);
			}
			if (this.Unit.Health < 100)
			{
				float num = 1.68f * (float)this.Unit.Health;
				rectangle2 = new Rectangle(7, 167, (int)num, 10);
				graphics.DrawImage(UnitBattleSprite.BarOutline, new Rectangle(5, 165, 170, 14));
				graphics.FillRectangle(Brushes.Red, rectangle2);
			}
			if (this.Unit.Morale < 100)
			{
				float num2 = 1.68f * (float)this.Unit.Morale;
				rectangle2 = new Rectangle(7, 152, (int)num2, 10);
				graphics.DrawImage(UnitBattleSprite.BarOutline, new Rectangle(5, 150, 170, 14));
				Brush brush = new SolidBrush(Color.FromArgb(233, 212, 149));
				graphics.FillRectangle(brush, rectangle2);
				brush.Dispose();
			}
			if (this.Unit.BattleData != null && this.Unit.BattleData.Battle != null && !this.Unit.BattleData.Battle.DeployPhase)
			{
				if (!this.Unit.BattleData.CanFight)
				{
					graphics.DrawImage(UnitBattleSprite.NoAttack, 16, 16, 152, 160);
				}
				else
				{
					graphics.DrawImage(UnitBattleSprite.AttackAvailImage, 16, 120, 40, 40);
				}
				if (!this.Unit.BattleData.CanMove)
				{
					graphics.DrawImage(UnitBattleSprite.NoMove, 16, 16, 152, 160);
				}
				else
				{
					graphics.DrawImage(UnitBattleSprite.MoveAvailImage, 124, 120, 40, 40);
				}
			}
			string text2 = "Data\\Images\\HUD\\flags\\" + this.Unit.OwnerRealm.FlagFilename + ".png";
			if (!UnitBattleSprite.FlagImages.ContainsKey(text2))
			{
				Bitmap bitmap2 = new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile(text2, FileTypes.Application, FileModes.ReadOnly, true));
				bitmap2 = this.CreateMaskedFlag(bitmap2);
				UnitBattleSprite.FlagImages.Add(text2, bitmap2);
			}
			Bitmap bitmap3 = UnitBattleSprite.FlagImages[text2];
			graphics.DrawImage(bitmap3, this.CounterImage.Width - 84, 0, 84, 72);
			graphics.DrawImage(UnitBattleSprite.FlagOutline, this.CounterImage.Width - 84, 0, 84, 72);
			graphics.Dispose();
			this.Texture.BindBitmap(this.CounterImage, false);
			PointF scaledTileCoords = this.Game.GameCore.Data.CombatMap.GetScaledTileCoords(this.Unit.BattleData.BattleX, this.Unit.BattleData.BattleY);
			this.Sprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
			this.Sprite.SetSize(this.Game.GameCore.Data.CombatMap.TileSizeX, this.Game.GameCore.Data.CombatMap.TileSizeY);
			if (!this.Visible)
			{
				this.Sprite.SetSize(0f, 0f);
			}
			this.HighlightSprite.SetPosition(scaledTileCoords.X, scaledTileCoords.Y);
			if (this.Highlighted)
			{
				this.HighlightSprite.SetSize(this.Game.GameCore.Data.CombatMap.TileSizeX, this.Game.GameCore.Data.CombatMap.TileSizeY);
				return;
			}
			this.HighlightSprite.SetSize(0f, 0f);
		}

		public PointF GetPosition()
		{
			return this.Sprite.Bounds.Location;
		}

		internal void SetPosition(float X, float Y)
		{
			this.Sprite.SetPosition(X, Y);
		}

		internal void Update(float ElapsedTime)
		{
			this.TimeSinceLastSprite += ElapsedTime;
			foreach (FloatingSpriteData floatingSpriteData in this.FloatingSprites.ToList<FloatingSpriteData>())
			{
				if (!floatingSpriteData.Enabled && this.TimeSinceLastSprite > floatingSpriteData.TimeFactor * 0.25f)
				{
					floatingSpriteData.Enabled = true;
					this.TimeSinceLastSprite = 0f;
				}
				floatingSpriteData.Update(ElapsedTime);
				if (floatingSpriteData.Complete)
				{
					floatingSpriteData.Dispose();
					this.FloatingSprites.Remove(floatingSpriteData);
				}
			}
			if (this.Animations.Count > 0)
			{
				foreach (AnimationSpriteData animationSpriteData in this.Animations.ToList<AnimationSpriteData>())
				{
					animationSpriteData.Update(ElapsedTime);
				}
			}
			if (this.Highlighted)
			{
				this.AlphaTime += ElapsedTime * 5f;
				if ((double)this.AlphaTime > 6.283185307179586)
				{
					this.AlphaTime -= 6.2831855f;
				}
				float num = (float)Math.Sin((double)this.AlphaTime);
				if (num < 0f)
				{
					num = 0f;
				}
				this.HighlightSprite.SetAlpha(num);
			}
		}

		internal void AddAnimation(AnimationData Anim, PointF Size, string Tag)
		{
			AnimationSpriteData animationSpriteData = new AnimationSpriteData(this.Game, Anim, new Vector2(this.Sprite.Bounds.X, this.Sprite.Bounds.Y), new Vector2(Size.X, Size.Y), Tag, true);
			animationSpriteData.OnAnimationCompleted += this.Data_OnAnimationCompleted;
			this.Animations.Add(animationSpriteData);
		}

		private void Data_OnAnimationCompleted(AnimationSpriteData Animation)
		{
			if (this.OnAnimationCompleted != null)
			{
				this.OnAnimationCompleted(Animation);
			}
			Animation.OnAnimationCompleted -= this.Data_OnAnimationCompleted;
			this.Animations.Remove(Animation);
			Animation.Dispose();
		}

		internal FloatingSpriteData GetFloatingSprite(GLTexture Tex, PointF Size)
		{
			return new FloatingSpriteData(this.Game, Tex, this.Sprite.Bounds.Location, Size);
		}

		internal void AddFloatingSprite(GLTexture Tex, PointF Size)
		{
			FloatingSpriteData floatingSpriteData = new FloatingSpriteData(this.Game, Tex, this.Sprite.Bounds.Location, Size);
			this.FloatingSprites.Add(floatingSpriteData);
			if (this.FloatingSprites.Count == 1)
			{
				floatingSpriteData.Enabled = true;
				this.TimeSinceLastSprite = 0f;
			}
		}

		private WorkingUnit Unit;

		private SovereigntyGame Game;

		private GLSprite Sprite;

		internal GLTexture Texture;

		private Bitmap CounterImage;

		private GLSprite HighlightSprite;

		private float TimeSinceLastSprite;

		private List<FloatingSpriteData> FloatingSprites;

		private List<AnimationSpriteData> Animations;

		public bool Highlighted;

		private float AlphaTime;

		private bool Visible;

		private static Dictionary<string, Bitmap> UnitImages;

		private static Dictionary<string, Bitmap> FlagImages;

		private static Bitmap Highlight;

		private static Bitmap BarOutline;

		private static Bitmap NoMove;

		private static Bitmap NoAttack;

		private static Bitmap AttackAvailImage;

		private static Bitmap MoveAvailImage;

		private static Bitmap FlagOutline;
	}
}
