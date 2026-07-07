using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Battle
{
	public class CombatPreviewResult
	{
		public CombatPreviewResult(SovereigntyGame Game, TacticalBattleController Battle, WorkingUnit Attacker, WorkingUnit Defender, CombatAction ActionType, UnitActionData AttackerActions)
		{
			this.Game = Game;
			this.Battle = Battle;
			this.Attacker = Attacker;
			this.Defender = Defender;
			this.ActionType = ActionType;
			Point battleLocation = Attacker.BattleData.BattleLocation;
			this.AttackerResult = new PreviewResultData(Attacker, ActionType);
			this.DefenderResult = new PreviewResultData(Defender, ActionType);
			this.FireSupportUnits = new List<WorkingUnit>();
			UnitFlag unitFlag = null;
			if (ActionType == CombatAction.Heal)
			{
				this.DefenderResult.Heal = Attacker.HealRate * 10;
				return;
			}
			Point point = Attacker.BattleData.BattleLocation;
			if (ActionType == CombatAction.ChargeAttack || ActionType == CombatAction.Moveattack)
			{
				List<Point> list = Game.GameCore.Data.CombatMap.GetAdjacentTiles(Defender.BattleData.BattleLocation.X, Defender.BattleData.BattleLocation.Y);
				list = list.Where((Point x) => AttackerActions.MovementCosts.ContainsKey(x) && Battle.Map.GetTile(x).Unit == null).ToList<Point>();
				point = (from x in list.OrderBy(delegate(Point x)
					{
						if (!Battle.Map.GetTile(x).Terrain.IsNaval)
						{
							return 0;
						}
						return 1;
					})
					orderby AttackerActions.MovementCosts[x]
					select x).FirstOrDefault<Point>();
				Point battleLocation2 = Attacker.BattleData.BattleLocation;
				Battle.TeleportUnit(Attacker, point);
				if (ActionType == CombatAction.ChargeAttack)
				{
					List<Point> movePath = Battle.Map.GetMovePath(battleLocation2, point, AttackerActions);
					int num = Math.Min(4, movePath.Count - 1);
					if (!Battle.Map.PathIsBlocked(movePath) && !Battle.Map.GetTile(Defender.BattleData.BattleLocation).HasTown())
					{
						unitFlag = UnitFlag.CreateNamedFlag(Game.GameCore, "Charge");
						unitFlag.NoFloaties = true;
						unitFlag.SetVariable("Amount", num);
						Attacker.GrantFlag(unitFlag);
						this.DefenderResult.StatusEffects.Add(GameText.CreateLocalised("COMBATCHARGE", new object[] { num }));
					}
				}
			}
			List<WorkingUnit> list2 = Battle.Map.GetAdjacentEnemies(Defender.BattleData.BattleLocation, Attacker.OwnerRealmID);
			list2 = list2.Where((WorkingUnit x) => x.Class == UnitClasses.Archer && x.BattleData.CanFight && x.RangedAttack.GetValue(Attacker) > 0).ToList<WorkingUnit>();
			foreach (WorkingUnit workingUnit in list2)
			{
				UnitActionData unitActions = Battle.GetUnitActions(workingUnit);
				if (unitActions.TilesinRange.Contains(point))
				{
					CombatResults combatResults = CombatManager.PerformCombat(workingUnit, Attacker, CombatType.MinDamage, true, false, true);
					CombatResults combatResults2 = CombatManager.PerformCombat(workingUnit, Attacker, CombatType.MaxDamage, true, false, true);
					if (combatResults2.DefenderCasualties > 0)
					{
						this.AttackerResult.MaxSupportCasualties += combatResults.DefenderCasualties;
						this.AttackerResult.MaxSupportCasualties += combatResults.DefenderCasualties;
						this.FireSupportUnits.Add(workingUnit);
					}
				}
			}
			if (Attacker.Class == UnitClasses.Siege && !Defender.HasAnyNamedFlag("Shatter"))
			{
				this.DefenderResult.StatusEffects.Add(GameText.CreateLocalised("COMBATEFFECT_SHATTER", new object[0]));
			}
			bool flag;
			if (ActionType == CombatAction.RangedAttack)
			{
				flag = Defender.BattleData.CanFight;
				if (flag)
				{
					UnitActionData unitActions2 = Battle.GetUnitActions(Defender);
					if (!unitActions2.Targets.ContainsKey(Attacker))
					{
						flag = false;
					}
					else if (!unitActions2.Targets[Attacker].Contains(CombatAction.RangedAttack))
					{
						flag = false;
					}
				}
			}
			else
			{
				flag = true;
			}
			List<GameText> combatStatusEffectsOnSelf = Attacker.GetCombatStatusEffectsOnSelf(true, false, ActionType == CombatAction.RangedAttack, Defender);
			combatStatusEffectsOnSelf.AddRange(Defender.GetCombatStatusEffectsOnEnemy(false, false, ActionType == CombatAction.RangedAttack, Attacker));
			List<GameText> combatStatusEffectsOnSelf2 = Defender.GetCombatStatusEffectsOnSelf(false, false, ActionType == CombatAction.RangedAttack, Attacker);
			combatStatusEffectsOnSelf2.AddRange(Attacker.GetCombatStatusEffectsOnEnemy(true, false, ActionType == CombatAction.RangedAttack, Defender));
			if (flag)
			{
				combatStatusEffectsOnSelf.AddRange(Attacker.GetCombatStatusEffectsOnSelf(false, true, ActionType == CombatAction.RangedAttack, Defender));
				combatStatusEffectsOnSelf.AddRange(Defender.GetCombatStatusEffectsOnEnemy(true, true, ActionType == CombatAction.RangedAttack, Attacker));
				combatStatusEffectsOnSelf2.AddRange(Defender.GetCombatStatusEffectsOnSelf(true, true, ActionType == CombatAction.RangedAttack, Attacker));
				combatStatusEffectsOnSelf2.AddRange(Attacker.GetCombatStatusEffectsOnEnemy(false, true, ActionType == CombatAction.RangedAttack, Defender));
			}
			this.AttackerResult.StatusEffects.AddRange(combatStatusEffectsOnSelf);
			this.DefenderResult.StatusEffects.AddRange(combatStatusEffectsOnSelf2);
			CombatResults combatResults3 = CombatManager.PerformCombat(Attacker, Defender, CombatType.MinDamage, ActionType == CombatAction.RangedAttack, flag, false);
			CombatResults combatResults4 = CombatManager.PerformCombat(Attacker, Defender, CombatType.MaxDamage, ActionType == CombatAction.RangedAttack, flag, false);
			this.AttackerResult.MinCasualties = combatResults3.AttackerCasualties;
			this.AttackerResult.MaxCasualties = combatResults4.AttackerCasualties;
			this.DefenderResult.MinCasualties = combatResults3.DefenderCasualties;
			this.DefenderResult.MaxCasualties = combatResults4.DefenderCasualties;
			if (unitFlag != null)
			{
				Attacker.RemoveFlag(unitFlag);
			}
			Battle.TeleportUnit(Attacker, battleLocation);
		}

		public WorkingUnit Attacker;

		public WorkingUnit Defender;

		public TacticalBattleController Battle;

		public SovereigntyGame Game;

		public CombatAction ActionType;

		public PreviewResultData AttackerResult;

		public PreviewResultData DefenderResult;

		public List<WorkingUnit> FireSupportUnits;
	}
}
