using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.Game.Battle
{
	public class UnitActionData
	{
		public UnitActionData(WorkingUnit Unit)
		{
			this.Unit = Unit;
		}

		public bool CanHeal(WorkingUnit Unit)
		{
			return this.Targets.ContainsKey(Unit) && this.Targets[Unit].Contains(CombatAction.Heal);
		}

		public bool CanAttack()
		{
			return this.Targets.Count((KeyValuePair<WorkingUnit, List<CombatAction>> x) => x.Value.Contains(CombatAction.ChargeAttack) || x.Value.Contains(CombatAction.MeleeAttack) || x.Value.Contains(CombatAction.Moveattack) || x.Value.Contains(CombatAction.RangedAttack)) > 0;
		}

		public bool CanAttack(WorkingUnit Unit)
		{
			return this.Targets.ContainsKey(Unit) && (this.Targets[Unit].Contains(CombatAction.ChargeAttack) || this.Targets[Unit].Contains(CombatAction.MeleeAttack) || this.Targets[Unit].Contains(CombatAction.Moveattack) || this.Targets[Unit].Contains(CombatAction.RangedAttack));
		}

		internal bool RangedAttackPossible(WorkingUnit Unit)
		{
			return this.Targets.ContainsKey(Unit) && this.Targets[Unit].Contains(CombatAction.RangedAttack);
		}

		internal CombatAction GetAction(WorkingUnit Unit, bool AdjacentOnly)
		{
			if (!AdjacentOnly && this.Targets[Unit].Contains(CombatAction.RangedAttack))
			{
				return CombatAction.RangedAttack;
			}
			return this.Targets[Unit][0];
		}

		public bool MeleeAttackPossible()
		{
			foreach (KeyValuePair<WorkingUnit, List<CombatAction>> keyValuePair in this.Targets)
			{
				if (keyValuePair.Value.Contains(CombatAction.ChargeAttack))
				{
					return true;
				}
				if (keyValuePair.Value.Contains(CombatAction.MeleeAttack))
				{
					return true;
				}
				if (keyValuePair.Value.Contains(CombatAction.Moveattack))
				{
					return true;
				}
			}
			return false;
		}

		public bool RangedAttackPossible()
		{
			foreach (KeyValuePair<WorkingUnit, List<CombatAction>> keyValuePair in this.Targets)
			{
				if (keyValuePair.Value.Contains(CombatAction.RangedAttack))
				{
					return true;
				}
			}
			return false;
		}

		public bool RangedAttackPossibleWithRetal()
		{
			foreach (KeyValuePair<WorkingUnit, List<CombatAction>> keyValuePair in this.Targets)
			{
				if (keyValuePair.Value.Contains(CombatAction.RangedAttack) && keyValuePair.Key.BattleData.Battle.GetUnitActions(keyValuePair.Key).RangedAttackPossible(this.Unit))
				{
					return true;
				}
			}
			return false;
		}

		public bool AttackPossibleWithFireSupport()
		{
			SovereigntyGame game = this.Unit.Game;
			TacticalBattleController Battle = this.Unit.BattleData.Battle;
			foreach (KeyValuePair<WorkingUnit, List<CombatAction>> keyValuePair in this.Targets)
			{
				bool flag = false;
				Point point = Point.Empty;
				WorkingUnit key = keyValuePair.Key;
				if (keyValuePair.Value.Contains(CombatAction.Moveattack) || keyValuePair.Value.Contains(CombatAction.ChargeAttack))
				{
					flag = true;
					List<Point> adjacentTiles = game.GameCore.Data.CombatMap.GetAdjacentTiles(key.BattleData.BattleLocation.X, key.BattleData.BattleLocation.Y);
					point = adjacentTiles.FirstOrDefault((Point x) => this.MovementCosts.ContainsKey(x) && Battle.Map.GetTile(x).Unit == null);
				}
				else if (keyValuePair.Value.Contains(CombatAction.MeleeAttack) || keyValuePair.Value.Contains(CombatAction.RangedAttack))
				{
					flag = true;
					point = this.Unit.BattleData.BattleLocation;
				}
				if (flag)
				{
					List<WorkingUnit> list = Battle.Map.GetAdjacentEnemies(key.BattleData.BattleLocation, this.Unit.OwnerRealmID);
					list = list.Where((WorkingUnit x) => x.BattleData.CanFight && x.RangedAttack.GetValue(this.Unit) > 0).ToList<WorkingUnit>();
					foreach (WorkingUnit workingUnit in list)
					{
						if (!this.Unit.HasStatus("ClassAbilitiesBlocked", new object[0]))
						{
							UnitActionData unitActions = Battle.GetUnitActions(workingUnit);
							if (unitActions.TilesinRange.Contains(point))
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public WorkingUnit Unit;

		public Dictionary<Point, float> MovementCosts;

		public List<Point> TilesinRange;

		public Dictionary<WorkingUnit, List<CombatAction>> Targets;
	}
}
