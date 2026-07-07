using System;
using System.Linq;
using OpenTK;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.Game.Battle
{
	public class BattleStarter
	{
		public BattleStarter(WorkingStack Attacker, WorkingStack Defender, Path AttackPath, SovereigntyGame Game)
		{
			this.Attacker = Attacker;
			this.Defender = Defender;
			this.AttackPath = AttackPath;
			this.Game = Game;
			this.PlayerWatching = false;
			if (Attacker.Owner == Game.PlayerRealm)
			{
				this.PlayerWatching = true;
			}
			if (Defender.Owner == Game.PlayerRealm)
			{
				this.PlayerWatching = true;
			}
			if ((Attacker.Owner == Game.RebelRealm || Defender.Owner == Game.RebelRealm) && Game.GameCore.Settings.GetBooleanSetting("ViewRebelBattles"))
			{
				this.PlayerWatching = true;
			}
			if (Attacker.Owner != Game.RebelRealm && Game.PlayerRealm.Enemies.Contains(Attacker.Owner) && Game.GameCore.Settings.GetBooleanSetting("ViewEnemyBattles"))
			{
				this.PlayerWatching = true;
			}
			if (Defender.Owner != Game.RebelRealm && Game.PlayerRealm.Enemies.Contains(Defender.Owner) && Game.GameCore.Settings.GetBooleanSetting("ViewEnemyBattles"))
			{
				this.PlayerWatching = true;
			}
			if (Game.PlayerRealm.Allies.Contains(Attacker.Owner) && Game.GameCore.Settings.GetBooleanSetting("ViewAllyBattles"))
			{
				this.PlayerWatching = true;
			}
			if (Game.PlayerRealm.Allies.Contains(Defender.Owner) && Game.GameCore.Settings.GetBooleanSetting("ViewAllyBattles"))
			{
				this.PlayerWatching = true;
			}
			if (Game.PlayerRealm.TradeManager.GetTradeCount(Attacker.Owner) > 0 && Game.GameCore.Settings.GetBooleanSetting("ViewTradeBattles"))
			{
				this.PlayerWatching = true;
			}
			if (Game.PlayerRealm.TradeManager.GetTradeCount(Defender.Owner) > 0 && Game.GameCore.Settings.GetBooleanSetting("ViewTradeBattles"))
			{
				this.PlayerWatching = true;
			}
			if (Game.WatchAllBattles)
			{
				this.PlayerWatching = true;
			}
			if (Defender.Node.NodeType != PathNodeTypes.Sea)
			{
				WorkingStack workingStack = Game.CreateStack(Attacker.OwnerID, Defender.Node.ID, false);
				try
				{
					foreach (WorkingUnit workingUnit in Attacker.Units.Where((WorkingUnit x) => x.Selected))
					{
						int wanderChance = Defender.Node.Province.GetWanderChance(workingUnit);
						if (wanderChance > 0)
						{
							Random random = new Random();
							if (random.Next(100) < wanderChance)
							{
								continue;
							}
						}
						Game.MoveUnit(Attacker, workingStack, workingUnit, AttackPath, true);
						if (workingStack.Disposed)
						{
							workingStack = Game.CreateStack(Attacker.OwnerID, Defender.Node.ID, false);
						}
					}
					Game.GameCore.FireEvent("BattleStacksCreated", new object[]
					{
						Defender.Node.Province,
						Attacker,
						Defender
					});
					if (Attacker.Hero != null && Attacker.Hero.Selected)
					{
						workingStack.TransferHeroFromStack(Attacker, Attacker.Hero);
					}
					this.Attacker = workingStack;
					if (Attacker.Units.Count == 0)
					{
						Game.DestroyStack(Attacker);
					}
					workingStack.UpdateSprite();
				}
				catch (Exception ex)
				{
					string text = "Error creating attacker stack for battle\r\n";
					text = text + "Error: " + ex.Message + "\r\n";
					text = text + "Attacking from: " + Attacker.Node.GetRegion().Name + "\r\n";
					text = text + "Target: " + Defender.Node.GetRegion().Name + "\r\n";
					text += "Units to move: ";
					foreach (WorkingUnit workingUnit2 in Attacker.Units.Where((WorkingUnit x) => x.Selected))
					{
						text = text + workingUnit2.BaseName + ", ";
					}
					text += "\r\n";
					text += "Units moved: ";
					foreach (WorkingUnit workingUnit3 in workingStack.Units.Where((WorkingUnit x) => x.Selected))
					{
						text = text + workingUnit3.BaseName + ", ";
					}
					text += "\r\n\r\nOriginal Stack Trace:";
					text = text + "\r\n" + ex.StackTrace;
					throw new Exception(text);
				}
			}
			if (this.PlayerWatching)
			{
				if (Game.GameCore.Map.CurrentMode == MapModes.StackMove)
				{
					Game.GameCore.Map.ChangeMode(MapModes.Default, false);
				}
				Game.GameCore.Map.ShowAttackArrow(AttackPath);
				Game.GameCore.Camera.BeginAutoMove(new Vector3((float)Defender.Node.MapCoords.X, Game.GameCore.Camera.CamPos.Y, (float)Defender.Node.MapCoords.Y), 0.5f);
			}
		}

		public bool ReadyToStart()
		{
			return !this.Attacker.HasMovingUnits() && !this.Game.GameCore.Camera.DoingAutoMove;
		}

		internal void Dispose()
		{
			this.Game.GameCore.Map.RemoveAttackArrow();
			this.Attacker = null;
			this.Defender = null;
			this.Game = null;
			if (this.AttackPath != null)
			{
				this.AttackPath.Dispose();
				this.AttackPath = null;
			}
		}

		public WorkingStack GetPlayerStack()
		{
			if (this.Attacker.Owner == this.Game.PlayerRealm)
			{
				return this.Attacker;
			}
			if (this.Defender.Owner == this.Game.PlayerRealm)
			{
				return this.Defender;
			}
			return null;
		}

		public WorkingStack Attacker;

		public WorkingStack Defender;

		public Path AttackPath;

		public SovereigntyGame Game;

		public bool PlayerWatching;
	}
}
