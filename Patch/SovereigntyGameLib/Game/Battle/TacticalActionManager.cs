using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenTK;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.Game.Battle
{
	public class TacticalActionManager
	{
		public TacticalActionManager(SovereigntyGame Game, TacticalBattleController Battle, WorkingUnit UnitA, WorkingUnit UnitB, Point TargetTile, CombatAction ActionType, UnitActionData UnitActions)
		{
			this.Game = Game;
			this.Battle = Battle;
			this.UnitA = UnitA;
			this.UnitB = UnitB;
			this.TargetTile = TargetTile;
			this.ActionType = ActionType;
			this.Actions = new List<TacticalCombatAction>();
			if (ActionType == CombatAction.Move)
			{
				if (Game.GameCore.CurrentBattleMap.IsDisengageMove(UnitA, TargetTile))
				{
					List<WorkingUnit> engagingEnemies = Game.GameCore.CurrentBattleMap.GetEngagingEnemies(UnitA);
					foreach (WorkingUnit enemy in engagingEnemies)
					{
						if (enemy.BattleData != null && enemy.BattleData.CanFight && !enemy.Disabled)
						{
							TacticalCombatAction disengageAttack = new TacticalCombatAction(TacticalActionType.Fight);
							disengageAttack.UnitA = enemy;
							disengageAttack.UnitB = UnitA;
							disengageAttack.Ranged = false;
							disengageAttack.AllowRetal = false;
							disengageAttack.CounterFire = true;
							this.Actions.Add(disengageAttack);
						}
					}
				}
				this.CreateMoveAction(UnitA, TargetTile, UnitActions);
				BattleTile tile = Game.GameCore.CurrentBattleMap.GetTile(UnitA.BattleData.BattleX, UnitA.BattleData.BattleY);
				BattleTile tile2 = Game.GameCore.CurrentBattleMap.GetTile(TargetTile.X, TargetTile.Y);
				Battle.UnitLeaveTile(tile, UnitA);
				Battle.UnitEnterTile(tile2, UnitA);
				tile.UnitID = -1;
				UnitA.BattleData.BattleX = TargetTile.X;
				UnitA.BattleData.BattleY = TargetTile.Y;
				tile2.UnitID = UnitA.ID;
				Battle.TileOccupied(TargetTile, UnitA);
				if (tile2.Terrain.BaseType.IsAnyType(new string[] { "river", "sea" }) && !tile2.HasRoad() && UnitA.MoveType == MoveTypes.Land && !UnitA.HasStatus("IgnoreWaterEffects", new object[0]))
				{
					UnitA.RemoveNamedFlags("Disorder", 0);
					UnitFlag unitFlag = UnitFlag.CreateNamedFlag(Game.GameCore, "Disorder");
					unitFlag.TurnCount = 2;
					UnitA.GrantFlag(unitFlag);
				}
			}
			if (ActionType == CombatAction.Moveattack || ActionType == CombatAction.ChargeAttack)
			{
				List<Point> list = Game.GameCore.Data.CombatMap.GetAdjacentTiles(UnitB.BattleData.BattleLocation.X, UnitB.BattleData.BattleLocation.Y);
				list = list.Where((Point x) => UnitActions.MovementCosts.ContainsKey(x) && Battle.Map.GetTile(x).Unit == null).ToList<Point>();
				Point point = (from x in list.OrderBy(delegate(Point x)
					{
						if (!Battle.Map.GetTile(x).Terrain.IsNaval)
						{
							return 0;
						}
						return 1;
					})
					orderby UnitActions.MovementCosts[x]
					select x).FirstOrDefault<Point>();
				if (ActionType == CombatAction.ChargeAttack)
				{
					List<Point> movePath = Battle.Map.GetMovePath(UnitA.BattleData.BattleLocation, point, UnitActions);
					if (!Battle.Map.PathIsBlocked(movePath) && !Battle.Map.GetTile(UnitB.BattleData.BattleLocation).HasTown())
					{
						int num = Math.Min(4, movePath.Count - 1);
						this.ChargeFlag = UnitFlag.CreateNamedFlag(Game.GameCore, "Charge");
						this.ChargeFlag.SetVariable("Amount", num);
						UnitA.GrantFlag(this.ChargeFlag);
					}
				}
				this.CreateMoveAction(UnitA, point, UnitActions);
				Game.GameCore.CurrentBattleMap.GetTile(UnitA.BattleData.BattleX, UnitA.BattleData.BattleY).UnitID = -1;
				UnitA.BattleData.BattleX = point.X;
				UnitA.BattleData.BattleY = point.Y;
				Game.GameCore.CurrentBattleMap.GetTile(point.X, point.Y).UnitID = UnitA.ID;
				Battle.TileOccupied(TargetTile, UnitA);
			}
			if (ActionType == CombatAction.Heal)
			{
				TacticalCombatAction tacticalCombatAction = new TacticalCombatAction(TacticalActionType.Heal);
				tacticalCombatAction.UnitA = UnitA;
				tacticalCombatAction.UnitB = UnitB;
				tacticalCombatAction.Ranged = true;
				tacticalCombatAction.AllowRetal = false;
				this.Actions.Add(tacticalCombatAction);
			}
			if (ActionType != CombatAction.Move && ActionType != CombatAction.Heal)
			{
				List<WorkingUnit> list2 = Battle.Map.GetAdjacentEnemies(UnitB.BattleData.BattleLocation, UnitA.OwnerRealmID);
				list2 = list2.Where((WorkingUnit x) => x.Class == UnitClasses.Archer && x.BattleData.CanFight && x.RangedAttack.GetValue(UnitA) > 0).ToList<WorkingUnit>();
				foreach (WorkingUnit workingUnit in list2)
				{
					if (!workingUnit.HasStatus("ClassAbilitiesBlocked", new object[0]))
					{
						UnitActionData unitActions = Battle.GetUnitActions(workingUnit);
						if (unitActions.TilesinRange.Contains(UnitA.BattleData.BattleLocation))
						{
							TacticalCombatAction tacticalCombatAction2 = new TacticalCombatAction(TacticalActionType.Fight);
							tacticalCombatAction2.UnitA = workingUnit;
							tacticalCombatAction2.UnitB = UnitA;
							tacticalCombatAction2.Ranged = true;
							tacticalCombatAction2.AllowRetal = false;
							tacticalCombatAction2.CounterFire = true;
							this.Actions.Add(tacticalCombatAction2);
						}
					}
				}
				if (UnitB.Class == UnitClasses.Archer && UnitB.BattleData.CanFight && UnitB.RangedAttack.GetValue(UnitA) > 0 && !UnitB.HasStatus("ClassAbilitiesBlocked", new object[0]))
				{
					UnitActionData unitActions2 = Battle.GetUnitActions(UnitB);
					if (unitActions2.TilesinRange.Contains(UnitA.BattleData.BattleLocation))
					{
						TacticalCombatAction tacticalCombatAction3 = new TacticalCombatAction(TacticalActionType.Fight);
						tacticalCombatAction3.UnitA = UnitB;
						tacticalCombatAction3.UnitB = UnitA;
						tacticalCombatAction3.Ranged = true;
						tacticalCombatAction3.AllowRetal = false;
						tacticalCombatAction3.CounterFire = true;
						this.Actions.Add(tacticalCombatAction3);
					}
				}
				bool flag;
				if (ActionType == CombatAction.RangedAttack)
				{
					flag = UnitB.BattleData.CanFight;
					if (flag)
					{
						UnitActionData unitActions3 = Battle.GetUnitActions(UnitB);
						if (!unitActions3.Targets.ContainsKey(UnitA))
						{
							flag = false;
						}
						else if (!unitActions3.Targets[UnitA].Contains(CombatAction.RangedAttack))
						{
							flag = false;
						}
					}
				}
				else
				{
					flag = true;
				}
				TacticalCombatAction tacticalCombatAction4 = new TacticalCombatAction(TacticalActionType.Fight);
				tacticalCombatAction4.UnitA = UnitA;
				tacticalCombatAction4.UnitB = UnitB;
				tacticalCombatAction4.Ranged = ActionType == CombatAction.RangedAttack;
				tacticalCombatAction4.AllowRetal = flag;
				this.Actions.Add(tacticalCombatAction4);
			}
		}

		private void CreateMoveAction(WorkingUnit Unit, Point Tile, UnitActionData UnitActions)
		{
			TacticalCombatAction tacticalCombatAction = new TacticalCombatAction(TacticalActionType.Move);
			tacticalCombatAction.UnitA = Unit;
			tacticalCombatAction.Tile = Tile;
			tacticalCombatAction.Path = new List<Vector2>();
			List<Point> movePath = this.Battle.Map.GetMovePath(Unit.BattleData.BattleLocation, Tile, UnitActions);
			Unit.HandleMovePath(movePath);
			foreach (Point point in movePath)
			{
				PointF scaledTileCoords = this.Game.GameCore.Data.CombatMap.GetScaledTileCoords(point.X, point.Y);
				tacticalCombatAction.Path.Add(new Vector2(scaledTileCoords.X, scaledTileCoords.Y));
			}
			tacticalCombatAction.PathIndex = 1;
			tacticalCombatAction.CurrentPathLocation = tacticalCombatAction.Path[0];
			tacticalCombatAction.MoveCost = UnitActions.MovementCosts[Tile];
			this.Actions.Add(tacticalCombatAction);
		}

		public void Update(float ElapsedTime)
		{
			if (this.CurrentAction >= this.Actions.Count)
			{
				this.Completed = true;
				return;
			}
			this.Actions[this.CurrentAction].Update(ElapsedTime);
			if (this.Actions[this.CurrentAction].Completed)
			{
				this.CurrentAction++;
			}
		}

		public void Dispose()
		{
			if (this.ChargeFlag != null)
			{
				this.UnitA.RemoveFlag(this.ChargeFlag);
			}
		}

		internal void UpdateUnits()
		{
			if (this.UnitA != null && !this.UnitA.Disabled && this.UnitA.BattleData != null)
			{
				this.UnitA.BattleData.UpdateImage();
			}
			if (this.UnitB != null && !this.UnitB.Disabled && this.UnitB.BattleData != null)
			{
				this.UnitB.BattleData.UpdateImage();
			}
		}

		public SovereigntyGame Game;

		public TacticalBattleController Battle;

		public WorkingUnit UnitA;

		public WorkingUnit UnitB;

		public Point TargetTile;

		public CombatAction ActionType;

		public bool Completed;

		private UnitFlag ChargeFlag;

		private List<TacticalCombatAction> Actions;

		private int CurrentAction;
	}
}
