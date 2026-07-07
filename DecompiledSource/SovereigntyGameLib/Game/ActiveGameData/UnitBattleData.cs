using System;
using System.Drawing;
using System.IO;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class UnitBattleData
	{
		public bool CanFight
		{
			get
			{
				bool flag = this.m_CanFight;
				if (this.Unit.HasStatus("AttackBlocked", new object[0]))
				{
					flag = false;
				}
				return flag;
			}
			set
			{
				this.m_CanFight = value;
			}
		}

		public Point BattleLocation
		{
			get
			{
				return new Point(this.BattleX, this.BattleY);
			}
		}

		public UnitBattleData(SovereigntyGame Game, TacticalBattleController Battle, AutoBattleController AutoBattle, WorkingUnit Unit)
		{
			this.Game = Game;
			this.Unit = Unit;
			this.Battle = Battle;
			this.AutoBattle = AutoBattle;
			this.CurrentXP = 0;
			this.Sprite = new UnitBattleSprite(Game, Unit);
			this.Renderer = new TextRenderer(Game.GameCore);
			if (Battle != null)
			{
				if (Battle.Defender.Units.Contains(Unit))
				{
					this.CurrentXP++;
				}
				int count = Battle.Defender.Units.Count;
				int count2 = Battle.Attacker.Units.Count;
				if (Unit.OwnerStack == Battle.Attacker && count > count2)
				{
					this.CurrentXP++;
				}
				if (Unit.OwnerStack == Battle.Defender && count2 > count)
				{
					this.CurrentXP++;
				}
				if (Battle.RiverCrossing && Battle.Attacker.Units.Contains(Unit) && Unit.HasStatus("Bridging", new object[0]))
				{
					this.CurrentXP += 7;
				}
				if (this.CurrentXP > 0 && Unit.Rank == UnitRanks.Unique)
				{
					this.CurrentXP += 2;
				}
			}
			if (AutoBattle != null)
			{
				if (AutoBattle.Defender.Units.Contains(Unit))
				{
					this.CurrentXP++;
				}
				int count3 = AutoBattle.Defender.Units.Count;
				int count4 = AutoBattle.Attacker.Units.Count;
				if (Unit.OwnerStack == AutoBattle.Attacker && count3 > count4)
				{
					this.CurrentXP++;
				}
				if (Unit.OwnerStack == AutoBattle.Defender && count4 > count3)
				{
					this.CurrentXP++;
				}
				if (AutoBattle.RiverCrossing && AutoBattle.Attacker.Units.Contains(Unit) && Unit.HasStatus("Bridging", new object[0]))
				{
					this.CurrentXP += 7;
				}
				if (this.CurrentXP > 0 && Unit.Rank == UnitRanks.Unique)
				{
					this.CurrentXP += 2;
				}
			}
		}

		internal void Load(BinaryReader r)
		{
			this.m_CanFight = r.ReadBoolean();
			this.CanMove = r.ReadBoolean();
			this.BattleX = r.ReadInt32();
			this.BattleY = r.ReadInt32();
			this.CurrentXP = r.ReadInt32();
			this.CausedDisorder = r.ReadBoolean();
			this.CausedRetreat = r.ReadBoolean();
			this.CapturedUnit = r.ReadBoolean();
			this.AttackedUnit = r.ReadBoolean();
			this.KilledUnit = r.ReadBoolean();
			this.HealedUnit = r.ReadBoolean();
			this.Sprite.Show();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.m_CanFight);
			w.Write(this.CanMove);
			w.Write(this.BattleX);
			w.Write(this.BattleY);
			w.Write(this.CurrentXP);
			w.Write(this.CausedDisorder);
			w.Write(this.CausedRetreat);
			w.Write(this.CapturedUnit);
			w.Write(this.AttackedUnit);
			w.Write(this.KilledUnit);
			w.Write(this.HealedUnit);
		}

		public void Reset()
		{
			this.CanFight = true;
			this.CanMove = true;
		}

		public void Dispose(bool AwardXP)
		{
			if (AwardXP && !this.Unit.IsPrisoner && this.Unit.Health > 0)
			{
				this.Unit.XP += this.CurrentXP;
			}
			this.Sprite.Dispose();
			if (this.Renderer != null)
			{
				this.Renderer.Dispose();
			}
		}

		internal void Update(float ElapsedTime)
		{
			this.Sprite.Update(ElapsedTime);
		}

		public void UpdateImage()
		{
			this.Sprite.UpdateImage();
		}

		internal void SetTile(Point DeployTile)
		{
			this.BattleX = DeployTile.X;
			this.BattleY = DeployTile.Y;
			this.Sprite.UpdateImage();
		}

		internal void SetPosition(float X, float Y)
		{
			this.Sprite.SetPosition(X, Y);
		}

		public void ShowHighlight()
		{
			this.Sprite.Highlighted = true;
			this.Sprite.UpdateImage();
		}

		public void HideHighlight()
		{
			this.Sprite.Highlighted = false;
			this.Sprite.UpdateImage();
		}

		public void AddIconFloatie(string Filename)
		{
			if (this.Battle == null)
			{
				return;
			}
			if (this.Sprite == null)
			{
				return;
			}
			Bitmap bitmap = new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile(Filename, FileTypes.Application, FileModes.ReadOnly, true));
			GLTexture gltexture = new GLTexture(bitmap);
			this.Sprite.AddFloatingSprite(gltexture, new PointF((float)(bitmap.Width / 12), (float)(bitmap.Height / 12)));
			bitmap.Dispose();
		}

		public void AddFlagFloatie(UnitFlag Flag, bool Lost)
		{
			if (this.Battle == null)
			{
				return;
			}
			Bitmap bitmap = new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\HUD\\battle\\floating\\combat_titlebar.png", FileTypes.Application, FileModes.ReadOnly, true));
			Bitmap bitmap2 = new Bitmap(240, 60);
			string actualText = GameText.CreateLocalised(Flag.DisplayName, new object[0]).GetActualText(this.Game.GameCore);
			if (UnitBattleData.Font == null)
			{
				string font = this.Game.GameCore.Utilities.FontConvertor.GetFont("Trebuchet MS", true);
				UnitBattleData.Font = GameFont.GetFont(this.Game.GameCore, "Trebuchet MS", "Data\\Fonts\\" + font, 24);
			}
			this.Renderer.MaxWidth = 240f;
			this.Renderer.MaxHeight = 60f;
			this.Renderer.DefaultFont = UnitBattleData.Font;
			if (Lost)
			{
				this.Renderer.DefaultColour = Color.Red;
			}
			else
			{
				this.Renderer.DefaultColour = Color.DeepSkyBlue;
			}
			this.Renderer.TextAnchor = AnchorPoints.Middle;
			this.Renderer.SetText(actualText, this.Renderer.DefaultFont, this.Renderer.DefaultColour, this.Renderer.MaxWidth, this.Renderer.MaxHeight);
			this.Renderer.RenderOverImage(bitmap2, this.Renderer.DefaultColour);
			Bitmap bitmap3 = new Bitmap(300, 60);
			Graphics graphics = Graphics.FromImage(bitmap3);
			graphics.DrawImage(bitmap, new Rectangle(0, 0, 300, 60));
			graphics.DrawImage(bitmap2, 60, 0);
			graphics.Dispose();
			GLTexture gltexture = new GLTexture(bitmap3);
			bitmap3.Dispose();
			bitmap2.Dispose();
			bitmap.Dispose();
			this.Sprite.AddFloatingSprite(gltexture, new PointF(25f, 5f));
		}

		public FloatingSpriteData GetStatusFloatie(GameText Status)
		{
			if (this.Battle == null)
			{
				return null;
			}
			Bitmap bitmap = new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\HUD\\battle\\floating\\combat_titlebar.png", FileTypes.Application, FileModes.ReadOnly, true));
			Bitmap bitmap2 = new Bitmap(240, 60);
			string actualText = Status.GetActualText(this.Game.GameCore);
			if (UnitBattleData.Font == null)
			{
				string font = this.Game.GameCore.Utilities.FontConvertor.GetFont("Trebuchet MS", true);
				UnitBattleData.Font = GameFont.GetFont(this.Game.GameCore, "Trebuchet MS", "Data\\Fonts\\" + font, 24);
			}
			this.Renderer.MaxWidth = 240f;
			this.Renderer.MaxHeight = 60f;
			this.Renderer.DefaultFont = UnitBattleData.Font;
			this.Renderer.DefaultColour = Color.FromArgb(255, 255, 255);
			this.Renderer.TextAnchor = AnchorPoints.Middle;
			this.Renderer.SetText(actualText, this.Renderer.DefaultFont, this.Renderer.DefaultColour, this.Renderer.MaxWidth, this.Renderer.MaxHeight);
			this.Renderer.RenderOverImage(bitmap2, this.Renderer.DefaultColour);
			Bitmap bitmap3 = new Bitmap(300, 60);
			Graphics graphics = Graphics.FromImage(bitmap3);
			graphics.DrawImage(bitmap, new Rectangle(0, 0, 300, 60));
			graphics.DrawImage(bitmap2, 60, 0);
			graphics.Dispose();
			GLTexture gltexture = new GLTexture(bitmap3);
			bitmap3.Dispose();
			bitmap2.Dispose();
			bitmap.Dispose();
			return this.Sprite.GetFloatingSprite(gltexture, new PointF(25f, 5f));
		}

		public void AddStatusFloatie(GameText Status)
		{
			if (this.Battle == null)
			{
				return;
			}
			Bitmap bitmap = new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\HUD\\battle\\floating\\combat_titlebar.png", FileTypes.Application, FileModes.ReadOnly, true));
			Bitmap bitmap2 = new Bitmap(240, 60);
			string actualText = Status.GetActualText(this.Game.GameCore);
			if (UnitBattleData.Font == null)
			{
				string font = this.Game.GameCore.Utilities.FontConvertor.GetFont("Trebuchet MS", true);
				UnitBattleData.Font = GameFont.GetFont(this.Game.GameCore, "Trebuchet MS", "Data\\Fonts\\" + font, 24);
			}
			this.Renderer.MaxWidth = 240f;
			this.Renderer.MaxHeight = 60f;
			this.Renderer.DefaultFont = UnitBattleData.Font;
			this.Renderer.DefaultColour = Color.FromArgb(255, 255, 255);
			this.Renderer.TextAnchor = AnchorPoints.Middle;
			this.Renderer.SetText(actualText, this.Renderer.DefaultFont, this.Renderer.DefaultColour, this.Renderer.MaxWidth, this.Renderer.MaxHeight);
			this.Renderer.RenderOverImage(bitmap2, this.Renderer.DefaultColour);
			Bitmap bitmap3 = new Bitmap(300, 60);
			Graphics graphics = Graphics.FromImage(bitmap3);
			graphics.DrawImage(bitmap, new Rectangle(0, 0, 300, 60));
			graphics.DrawImage(bitmap2, 60, 0);
			graphics.Dispose();
			GLTexture gltexture = new GLTexture(bitmap3);
			bitmap3.Dispose();
			bitmap2.Dispose();
			bitmap.Dispose();
			this.Sprite.AddFloatingSprite(gltexture, new PointF(25f, 5f));
		}

		public void AddAnimation(string Name, string Tag)
		{
			if (this.Battle == null)
			{
				return;
			}
			AnimationData animationData = this.Game.Data.Animations[Name];
			this.Sprite.AddAnimation(animationData, new PointF(10f, 10f), Tag);
		}

		public void AddHealthFloatie(int Amount)
		{
			if (this.Battle == null)
			{
				return;
			}
			Bitmap bitmap = new Bitmap(this.Game.GameCore.Utilities.FileSystem.OpenFile("Data\\Images\\HUD\\battle\\damage_counter.png", FileTypes.Application, FileModes.ReadOnly, true));
			Bitmap bitmap2 = new Bitmap(60, 60);
			string actualText = GameText.CreateLocalised("FORMAT_NUMBER", new object[] { Amount }).GetActualText(this.Game.GameCore);
			if (UnitBattleData.Font == null)
			{
				string font = this.Game.GameCore.Utilities.FontConvertor.GetFont("Trebuchet MS", true);
				UnitBattleData.Font = GameFont.GetFont(this.Game.GameCore, "Trebuchet MS", "Data\\Fonts\\" + font, 24);
			}
			this.Renderer.MaxWidth = 60f;
			this.Renderer.MaxHeight = 60f;
			if (Amount < 0)
			{
				this.Renderer.DefaultColour = Color.Red;
			}
			else
			{
				this.Renderer.DefaultColour = Color.Green;
			}
			this.Renderer.DefaultFont = UnitBattleData.Font;
			this.Renderer.TextAnchor = AnchorPoints.Middle;
			this.Renderer.SetText(actualText, this.Renderer.DefaultFont, this.Renderer.DefaultColour, this.Renderer.MaxWidth, this.Renderer.MaxHeight);
			this.Renderer.RenderOverImage(bitmap2, this.Renderer.DefaultColour);
			Bitmap bitmap3 = new Bitmap(60, 60);
			Graphics graphics = Graphics.FromImage(bitmap3);
			graphics.DrawImage(bitmap, new Rectangle(0, 0, 60, 60));
			graphics.DrawImage(bitmap2, 0, 0);
			graphics.Dispose();
			GLTexture gltexture = new GLTexture(bitmap3);
			bitmap3.Dispose();
			bitmap2.Dispose();
			bitmap.Dispose();
			this.Sprite.AddFloatingSprite(gltexture, new PointF(5f, 5f));
		}

		public bool IsStandingOn(params string[] Terrains)
		{
			return this.Battle.Map.GetTile(this.BattleLocation).Terrain.BaseType.IsAnyType(Terrains);
		}

		public GLTexture GetTexture()
		{
			return this.Sprite.Texture;
		}

		public void RecordDisorder()
		{
			if (!this.AllowMultipleEvents && this.CausedDisorder)
			{
				return;
			}
			this.CausedDisorder = true;
			this.CurrentXP++;
		}

		public void RecordRetreat()
		{
			if (!this.AllowMultipleEvents && this.CausedRetreat)
			{
				return;
			}
			this.CausedRetreat = true;
			this.CurrentXP += 3;
		}

		public void RecordCapture()
		{
			if (!this.AllowMultipleEvents && this.CapturedUnit)
			{
				return;
			}
			this.CapturedUnit = true;
			this.CurrentXP += 3;
		}

		public void RecordAttack()
		{
			if (!this.AllowMultipleEvents && this.AttackedUnit)
			{
				return;
			}
			this.AttackedUnit = true;
			this.CurrentXP++;
		}

		public void RecordKill()
		{
			if (!this.AllowMultipleEvents && this.KilledUnit)
			{
				return;
			}
			this.KilledUnit = true;
			this.CurrentXP += 5;
		}

		public void RecordHeal()
		{
			if (!this.AllowMultipleEvents && this.HealedUnit)
			{
				return;
			}
			this.HealedUnit = true;
			this.CurrentXP += 2;
		}

		private WorkingUnit Unit;

		private SovereigntyGame Game;

		public UnitBattleSprite Sprite;

		public TacticalBattleController Battle;

		public AutoBattleController AutoBattle;

		private bool m_CanFight;

		public int BattleX;

		public int BattleY;

		public bool CanMove;

		public int CurrentXP;

		public bool CausedDisorder;

		public bool CausedRetreat;

		public bool CapturedUnit;

		public bool AttackedUnit;

		public bool KilledUnit;

		public bool HealedUnit;

		private bool AllowMultipleEvents = true;

		private TextRenderer Renderer;

		private static GameFont Font;

		public bool Selected;
	}
}
