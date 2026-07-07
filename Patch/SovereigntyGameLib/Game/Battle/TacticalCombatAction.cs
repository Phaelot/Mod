using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Battle
{
	public class TacticalCombatAction
	{
		public TacticalCombatAction(TacticalActionType Type)
		{
			this.Type = Type;
		}

		public void Update(float ElapsedTime)
		{
			switch (this.Type)
			{
			case TacticalActionType.Move:
				this.UpdateMoveAction(ElapsedTime);
				return;
			case TacticalActionType.Fight:
				this.UpdateFightAction(ElapsedTime);
				return;
			case TacticalActionType.Heal:
				this.UpdateHealAction(ElapsedTime);
				return;
			default:
				return;
			}
		}

		private void UpdateHealAction(float ElapsedTime)
		{
			if (this.Timer == 0f)
			{
				this.UnitA.BattleData.CanFight = false;
				this.UnitB.ApplyHealing(this.UnitA.HealRate * 10, false, this.UnitA);
			}
			float num = 0f;
			switch (this.UnitA.Game.GameCore.Settings.GetIntSetting("TacticalBattleSpeed"))
			{
			case 1:
				num = 3f;
				break;
			case 2:
				num = 2f;
				break;
			case 3:
				num = 1f;
				break;
			case 4:
				num = 0.9f;
				break;
			case 5:
				num = 0.75f;
				break;
			case 6:
				num = 0.65f;
				break;
			case 7:
				num = 0.55f;
				break;
			case 8:
				num = 0.45f;
				break;
			case 9:
				num = 0.35f;
				break;
			case 10:
				num = 0.25f;
				break;
			}
			this.Timer += ElapsedTime;
			if (this.Timer > num)
			{
				this.Completed = true;
			}
		}

		private void UpdateFightAction(float ElapsedTime)
		{
			if (this.Result == null)
			{
				if (this.UnitA.BattleData == null || this.UnitB.BattleData == null)
				{
					this.Completed = true;
					return;
				}
				this.UnitA.BattleData.CanFight = false;
				if (!this.UnitA.CanMoveAndAttack())
				{
					this.UnitA.MoveCombat(100f);
				}
				if (this.Ranged)
				{
					this.UnitA.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + this.UnitA.RangedSound + ".wav");
				}
				else
				{
					this.UnitA.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + this.UnitA.MeleeSound + ".wav");
				}
				if (this.UnitA.Game.GameCore.Settings.GetBooleanSetting("FollowCamera"))
				{
					this.UnitA.Game.GameCore.Camera.BeginAutoMove(new Vector3(this.UnitA.BattleData.Sprite.GetPosition().X, this.UnitA.Game.GameCore.Camera.CamPos.Y, this.UnitA.BattleData.Sprite.GetPosition().Y), 0.25f);
				}
				this.Result = CombatManager.PerformCombat(this.UnitA, this.UnitB, CombatType.Simulated, this.Ranged, this.AllowRetal, this.CounterFire);
				if (this.Result.DefenderCasualties > 0)
				{
					GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_DAMAGE", new object[] { this.Result.DefenderCasualties });
					gameText.AddChildText(GameText.CreateLocalised(this.Result.Attacker.OwnerRealm.DisplayName, new object[0]));
					gameText.AddChildText(GameText.CreateLocalised(this.Result.Attacker.DisplayName, new object[0]));
					gameText.AddChildText(GameText.CreateLocalised(this.Result.Defender.OwnerRealm.DisplayName, new object[0]));
					gameText.AddChildText(GameText.CreateLocalised(this.Result.Defender.DisplayName, new object[0]));
					this.UnitA.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
				}
				if (this.Result.AttackerCasualties > 0)
				{
					GameText gameText2 = GameText.CreateLocalised("FORMAT_BATTLELOG_DAMAGE", new object[] { this.Result.AttackerCasualties });
					gameText2.AddChildText(GameText.CreateLocalised(this.Result.Defender.OwnerRealm.DisplayName, new object[0]));
					gameText2.AddChildText(GameText.CreateLocalised(this.Result.Defender.DisplayName, new object[0]));
					gameText2.AddChildText(GameText.CreateLocalised(this.Result.Attacker.OwnerRealm.DisplayName, new object[0]));
					gameText2.AddChildText(GameText.CreateLocalised(this.Result.Attacker.DisplayName, new object[0]));
					this.UnitA.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText2 });
				}
				this.Result.ApplyDamage();
				this.UnitA.CombatNotification("AfterAttacking", this.UnitB, this.Ranged);
				this.UnitB.CombatNotification("AfterAttacked", this.UnitA, this.Ranged);
				if (this.Result.DefenderCasualties > 0 && this.Result.Attacker.Class == UnitClasses.Fort)
				{
					this.Result.Attacker.Game.GameCore.FireEvent("UnitDamagedByFort", new object[] { this.Result.Defender });
				}
				if (this.UnitA.Class == UnitClasses.Siege && !this.UnitA.HasAnyNamedFlag("Shatter") && !this.UnitB.Disabled)
				{
					GameText gameText3 = GameText.CreateLocalised("FORMAT_BATTLELOG_SHATTER", new object[0]);
					gameText3.AddChildText(GameText.CreateLocalised(this.UnitB.OwnerRealm.DisplayName, new object[0]));
					gameText3.AddChildText(GameText.CreateLocalised(this.UnitB.DisplayName, new object[0]));
					this.UnitA.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText3 });
					UnitFlag unitFlag = UnitFlag.CreateNamedFlag(this.UnitA.Game.GameCore, "Shatter");
					unitFlag.TurnCount = 2;
					this.UnitB.GrantFlag(unitFlag);
				}
			}
			float num = 0f;
			switch (this.UnitA.Game.GameCore.Settings.GetIntSetting("TacticalBattleSpeed"))
			{
			case 1:
				num = 3f;
				break;
			case 2:
				num = 2f;
				break;
			case 3:
				num = 1f;
				break;
			case 4:
				num = 0.9f;
				break;
			case 5:
				num = 0.75f;
				break;
			case 6:
				num = 0.65f;
				break;
			case 7:
				num = 0.55f;
				break;
			case 8:
				num = 0.45f;
				break;
			case 9:
				num = 0.35f;
				break;
			case 10:
				num = 0.25f;
				break;
			}
			this.Timer += ElapsedTime;
			if (this.Timer > num)
			{
				this.Completed = true;
			}
		}

		private void UpdateMoveAction(float ElapsedTime)
		{
			if (!this.MoveStarted)
			{
				this.MoveStarted = true;
				this.UnitA.MoveCombat(this.MoveCost);
				if (!this.UnitA.CanMoveAndAttack())
				{
					this.UnitA.BattleData.CanFight = false;
				}
				this.UnitA.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + this.UnitA.MoveSound + ".wav");
			}
			float num = 0f;
			switch (this.UnitA.Game.GameCore.Settings.GetIntSetting("TacticalBattleSpeed"))
			{
			case 1:
				num = 10f;
				break;
			case 2:
				num = 20f;
				break;
			case 3:
				num = 30f;
				break;
			case 4:
				num = 40f;
				break;
			case 5:
				num = 60f;
				break;
			case 6:
				num = 100f;
				break;
			case 7:
				num = 130f;
				break;
			case 8:
				num = 140f;
				break;
			case 9:
				num = 150f;
				break;
			case 10:
				num = 160f;
				break;
			}
			float num3;
			for (float num2 = ElapsedTime * num; num2 > 0f; num2 -= num3)
			{
				Vector2 currentPathLocation = this.CurrentPathLocation;
				Vector2 vector = this.Path[this.PathIndex];
				Vector2 vector2 = vector - currentPathLocation;
				float length = vector2.Length;
				num3 = Math.Min(length, num2);
				if (num3 == length)
				{
					this.SetPosition(this.Path[this.PathIndex]);
					this.PathIndex++;
					if (this.PathIndex == this.Path.Count)
					{
						this.Completed = true;
						return;
					}
				}
				else
				{
					vector2.Normalize();
					vector2 *= num3;
					Vector2 vector3 = this.CurrentPathLocation + vector2;
					this.SetPosition(vector3);
				}
			}
		}

		private void SetPosition(Vector2 Position)
		{
			this.CurrentPathLocation = Position;
			this.UnitA.BattleData.SetPosition(Position.X, Position.Y);
			if (this.UnitA.Game.GameCore.Settings.GetBooleanSetting("FollowCamera"))
			{
				this.UnitA.Game.GameCore.Camera.BeginAutoMove(new Vector3(this.UnitA.BattleData.Sprite.GetPosition().X, this.UnitA.Game.GameCore.Camera.CamPos.Y, this.UnitA.BattleData.Sprite.GetPosition().Y), 0.25f);
			}
		}

		public TacticalActionType Type;

		public WorkingUnit UnitA;

		public WorkingUnit UnitB;

		public Point Tile;

		public List<Vector2> Path;

		public int PathIndex;

		public Vector2 CurrentPathLocation;

		public bool Ranged;

		public bool AllowRetal;

		public bool Completed;

		public bool CounterFire;

		private CombatResults Result;

		private float Timer;

		private bool MoveStarted;

		public float MoveCost;
	}
}
