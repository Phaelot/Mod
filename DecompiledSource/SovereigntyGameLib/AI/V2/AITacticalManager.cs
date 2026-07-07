// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.AI.V2.AITacticalManager
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using OpenTK;
using SovereigntyTK;
using SovereigntyTK.AI;
using SovereigntyTK.AI.V2;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.AI.V2
{
	public class AITacticalManager
	{
		public SovereigntyTK.AI.V2.AIPlayer AI;

		public TacticalBattleController Battle;

		private TacticalStates CurrentState;

		private Point CurrentLocation;

		private AIFormationManager FormationManager;

		public AITacticalManager(SovereigntyTK.AI.V2.AIPlayer AI)
		{
			this.AI = AI;
		}

		internal void Dispose()
		{
		}

		internal void InitForBattle(TacticalBattleController Battle)
		{
			this.Battle = Battle;
			FormationManager = new AIFormationManager(Battle);
		}

		private void PlayTurnStartCard()
		{
			if (AI.Realm.BattleData.CardPlayed)
			{
				return;
			}
			foreach (CardEffect item in AI.Realm.BattleData.ActiveCards.ToList())
			{
				if (item.GetAICastTime() == CastTimes.TurnStart)
				{
					int aICastChance = item.GetAICastChance();
					int num = AI.RNG.Next(100);
					if (num <= aICastChance && PlayCard(item))
					{
						break;
					}
				}
			}
		}

		private void PlayTurnEndCard()
		{
			if (AI.Realm.BattleData.CardPlayed)
			{
				return;
			}
			foreach (CardEffect item in AI.Realm.BattleData.ActiveCards.ToList())
			{
				if (item.GetAICastTime() == CastTimes.TurnEnd)
				{
					int aICastChance = item.GetAICastChance();
					int num = AI.RNG.Next(100);
					if (num <= aICastChance && PlayCard(item))
					{
						break;
					}
				}
			}
		}

		private bool PlayCard(CardEffect Card)
		{
			List<CardTargetData> targetData = Card.GetTargetData();
			if (targetData != null)
			{
				int num = 0;
				foreach (CardTargetData item in targetData)
				{
					Dictionary<Point, int> dictionary = new Dictionary<Point, int>();
					foreach (Point allTile in Battle.Map.AllTiles)
					{
						if (Card.TileTargetValid(allTile, num))
						{
							dictionary.Add(allTile, Card.GetTargetWeight(allTile));
						}
					}
					if (dictionary.Count == 0)
					{
						return false;
					}
					int num2 = dictionary.Sum((KeyValuePair<Point, int> x) => x.Value);
					if (num2 <= 0)
					{
						return false;
					}
					int num3 = AI.RNG.Next(num2);
					int num4 = 0;
					foreach (KeyValuePair<Point, int> item2 in dictionary)
					{
						num4 += item2.Value;
						if (num3 < num4)
						{
							item.Tile = item2.Key;
							break;
						}
					}
					num++;
				}
			}
			AITacActionPlayCard aITacActionPlayCard = AI.ActionManager.CreateAction<AITacActionPlayCard>();
			aITacActionPlayCard.Card = Card;
			aITacActionPlayCard.CardTargets = targetData;
			AI.ActionManager.AddAction(aITacActionPlayCard);
			return true;
		}

		public float GetDamageRatio()
		{
			IList<WorkingUnit> units;
			IList<WorkingUnit> units2;
			if (Battle.Defender.OwnerID == AI.Realm.ID)
			{
				units = Battle.Defender.Units;
				units2 = Battle.Attacker.Units;
			}
			else
			{
				units = Battle.Attacker.Units;
				units2 = Battle.Defender.Units;
			}
			units = units.Where((WorkingUnit x) => !x.Disabled).ToList();
			units2 = units2.Where((WorkingUnit x) => !x.Disabled).ToList();
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			foreach (WorkingUnit item in units)
			{
				foreach (WorkingUnit item2 in units2)
				{
					bool ranged = item.RangedAttack.GetValue(item2) > 0;
					CombatResults combatResults = CombatManager.PerformCombat(item, item2, CombatType.MinDamage, ranged, IncludeRetal: true, IsCounterFire: false);
					CombatResults combatResults2 = CombatManager.PerformCombat(item, item2, CombatType.MaxDamage, ranged, IncludeRetal: true, IsCounterFire: false);
					float num4 = (combatResults2.AttackerCasualties - combatResults.AttackerCasualties) / 2 + combatResults.AttackerCasualties;
					float num5 = (combatResults2.DefenderCasualties - combatResults.DefenderCasualties) / 2 + combatResults.DefenderCasualties;
					num = num4;
					num2 = num5;
					num3 += 1f;
				}
			}
			num *= (float)units2.Count / num3;
			num2 *= (float)units.Count / num3;
			if (num < 1f)
			{
				num = 1f;
			}
			float num6 = num2 / num;
			num6 += (float)AI.Realm.Traits[AITraits.Warmonger] * 0.05f;
			return num6 - (float)AI.Realm.Traits[AITraits.Diplomat] * 0.02f;
		}

		private bool ShouldRetreat()
		{
			if (Battle.TurnCounter.CurrentTurn <= 4)
			{
				return false;
			}
			int val = Battle.TurnCounter.CurrentTurn - 4;
			if (Battle.ActiveStack == Battle.Defender)
			{
				val = Math.Max(0, val);
				val *= 5;
				val = 100 - val;
				if (AI.RNG.Next(100) < val)
				{
					return false;
				}
			}
			else
			{
				val = Math.Max(0, val);
				val *= 10;
				val = 50 - val;
				if (AI.RNG.Next(100) < val)
				{
					return false;
				}
			}
			if (Battle.ActiveStack.Owner == AI.Game.RebelRealm)
			{
				return false;
			}
			RetreatManager retreatManager = new RetreatManager(AI.Game, Battle.ActiveStack);
			RetreatData retreatList = retreatManager.GetRetreatList(Battle.ActiveStack, null);
			WorkingStack activeStack = Battle.ActiveStack;
			int num = activeStack.Units.Count((WorkingUnit x) => x.Class != UnitClasses.Fort && !x.Disabled);
			if (retreatList.RetreatTargets.Count == 0)
			{
				if (AI.RNG.Next(100) > 5)
				{
					return false;
				}
			}
			else if (retreatList.RetreatTargets.Count < num && AI.RNG.Next(100) > 25)
			{
				return false;
			}
			if (Battle.Node.Province != null && Battle.Node.Province.OwnerRealm == AI.Realm && Battle.Node.Province.IsCapitol)
			{
				return false;
			}
			return (double)GetDamageRatio() < 0.35;
		}

		private void InitialisePosition()
		{
			Vector2 vector = default(Vector2);
			int num = 0;
			Point frontmostVP = Battle.Map.GetFrontmostVP();
			Vector2 vector2 = new Vector2(frontmostVP.X, frontmostVP.Y);
			foreach (WorkingUnit unit in Battle.Attacker.Units)
			{
				Vector2 vector3 = new Vector2(unit.BattleData.BattleX, unit.BattleData.BattleY);
				vector += vector3 - vector2;
				num++;
			}
			vector /= (float)num;
			float length = vector.Length;
			vector.Normalize();
			if (Battle.AttackerRealm == AI.Realm)
			{
				Facings direction = AITacticalUtilities.ConvertDirectionToFacing(-vector);
				vector *= length;
				CurrentLocation = new Point((int)vector2.X, (int)vector2.Y);
				CurrentLocation.X += (int)vector.X;
				CurrentLocation.Y += (int)vector.Y;
				CurrentLocation = AITacticalUtilities.MoveInDirection(CurrentLocation, direction, 1, Battle.Map);
			}
			else
			{
				Facings direction2 = AITacticalUtilities.ConvertDirectionToFacing(vector);
				CurrentLocation = new Point((int)vector2.X, (int)vector2.Y);
				CurrentLocation = AITacticalUtilities.MoveInDirection(CurrentLocation, direction2, 1, Battle.Map);
			}
		}

		private float GetCohesionValue(IList<WorkingUnit> Units, Point TargetPoint)
		{
			float num = 0f;
			float num2 = 0f;
			foreach (WorkingUnit Unit in Units)
			{
				if (!Unit.Disabled)
				{
					float num3 = Math.Abs(Unit.BattleData.BattleX - TargetPoint.X);
					float num4 = Math.Abs(Unit.BattleData.BattleY - TargetPoint.Y);
					float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
					float num6 = 1f - num5 * 0.05f;
					num += num6;
					num2 += 1f;
				}
			}
			return num / num2;
		}

		private double GetAttackRatio(WorkingStack FriendlyStack, WorkingStack EnemyStack)
		{
			int num = 0;
			foreach (WorkingUnit unit in FriendlyStack.Units)
			{
				if (!unit.Disabled && Battle.GetUnitActions(unit).CanAttack())
				{
					num++;
				}
			}
			return (float)num / (float)FriendlyStack.Units.Count((WorkingUnit x) => !x.Disabled);
		}

		private float GetRangedRatio(WorkingStack Stack)
		{
			return (float)Stack.Units.Count((WorkingUnit x) => (int)x.Range > 1) / (float)Stack.Units.Count;
		}

		private float GetDefenceAverage(WorkingStack Stack)
		{
			return (float)Stack.Units.Average((WorkingUnit x) => x.Defence);
		}

		private TacticalStates DetermineCurrentStrategy()
		{
			if (Battle.AttackerRealm == AI.Realm)
			{
				if (GetCohesionValue(Battle.Attacker.Units, CurrentLocation) < 0.6f)
				{
					return TacticalStates.Formingup;
				}
				if (GetDamageRatio() > 0.8f)
				{
					if (GetAttackRatio(Battle.Attacker, Battle.Defender) >= 0.25)
					{
						return TacticalStates.Melee;
					}
					if (GetRangedRatio(Battle.Attacker) > 0.6f)
					{
						return TacticalStates.Skirmishing;
					}
					return TacticalStates.Advancing;
				}
				return TacticalStates.Avoiding;
			}
			if (GetCohesionValue(Battle.Defender.Units, CurrentLocation) < 0.6f)
			{
				return TacticalStates.Formingup;
			}
			if (GetDamageRatio() > 1f)
			{
				if (GetRangedRatio(Battle.Defender) > 0.4f)
				{
					return TacticalStates.Skirmishing;
				}
				if (GetRangedRatio(Battle.Attacker) > 0.4f)
				{
					return TacticalStates.Advancing;
				}
				if (GetDefenceAverage(Battle.Defender) >= 2f)
				{
					return TacticalStates.Holding;
				}
				return TacticalStates.Advancing;
			}
			if ((double)GetDamageRatio() > 0.8)
			{
				if (GetRangedRatio(Battle.Defender) > 0.6f)
				{
					return TacticalStates.Skirmishing;
				}
				if (GetRangedRatio(Battle.Attacker) > 0.6f)
				{
					return TacticalStates.Advancing;
				}
				return TacticalStates.Holding;
			}
			if (GetRangedRatio(Battle.Defender) > 0.4f)
			{
				return TacticalStates.Skirmishing;
			}
			return TacticalStates.Retreating;
		}

		private void UpdateSiegeUnits(WorkingStack FriendlyStack, WorkingStack EnemyStack)
		{
			UnitData unit = AI.Realm.UnitPurchaseManager.GetUnit("Baggage Train");
			if (unit == null)
			{
				return;
			}
			foreach (WorkingUnit item in FriendlyStack.Units.ToList())
			{
				if (item.Disabled)
				{
					continue;
				}
				bool flag = true;
				if (item.Class == UnitClasses.Siege && item.CanPack)
				{
					foreach (WorkingUnit unit2 in EnemyStack.Units)
					{
						if (!unit2.Disabled && Battle.GetUnitActions(item).RangedAttackPossible(unit2))
						{
							flag = false;
							break;
						}
					}
					if (flag && item.CanPack && Battle.Map.GetAdjacentEnemies(item.BattleData.BattleLocation, AI.Realm.ID).Count == 0)
					{
						AITacActionPackUnit aITacActionPackUnit = AI.ActionManager.CreateAction<AITacActionPackUnit>();
						aITacActionPackUnit.Unit = item;
						aITacActionPackUnit.UnitType = unit;
						AI.ActionManager.AddAction(aITacActionPackUnit);
					}
				}
				else
				{
					if (item.Class != UnitClasses.Wagon || item.CarriedUnit == null)
					{
						continue;
					}
					int num = item.Range;
					int num2 = item.CarriedUnit.Range;
					num2 -= num;
					UnitFlag unitFlag = UnitFlag.CreateNamedFlag(AI.Game.GameCore, "RangeBonus");
					unitFlag.SetVariable("Amount", num2);
					item.GrantFlag(unitFlag);
					UnitFlag flag2 = UnitFlag.CreateNamedFlag(AI.Game.GameCore, "RangedAttackBonus");
					unitFlag.SetVariable("Amount", 1);
					item.GrantFlag(flag2);
					bool flag3 = false;
					foreach (WorkingUnit unit3 in EnemyStack.Units)
					{
						if (!unit3.Disabled && Battle.GetUnitActions(item).RangedAttackPossible(unit3))
						{
							flag3 = true;
							break;
						}
					}
					item.RemoveFlag(unitFlag);
					item.RemoveFlag(flag2);
					if (flag3)
					{
						AITacActionUnackUnit aITacActionUnackUnit = AI.ActionManager.CreateAction<AITacActionUnackUnit>();
						aITacActionUnackUnit.Unit = item;
						AI.ActionManager.AddAction(aITacActionUnackUnit);
					}
				}
			}
		}

		private void DoHeal(WorkingUnit Unit, WorkingStack Stack)
		{
			UnitActionData unitActions = Battle.GetUnitActions(Unit);
			WorkingUnit workingUnit = null;
			float num = 0f;
			foreach (WorkingUnit key in unitActions.Targets.Keys)
			{
				if (unitActions.CanHeal(key) && key.Race != Races.Undead && (int)key.Health != 100 && key.Class != UnitClasses.Fort)
				{
					int num2 = 100 - (int)key.Health;
					if ((float)num2 > num)
					{
						workingUnit = key;
						num = num2;
					}
				}
			}
			if (workingUnit != null && num > 10f)
			{
				AITacActionHeal aITacActionHeal = AI.ActionManager.CreateAction<AITacActionHeal>();
				aITacActionHeal.Unit = Unit;
				aITacActionHeal.TargetUnit = workingUnit;
				aITacActionHeal.UnitActions = unitActions;
				AI.ActionManager.AddAction(aITacActionHeal);
			}
		}

		private void DoHeals(WorkingStack Stack)
		{
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled && (int)unit.HealRate != 0 && unit.BattleData != null)
				{
					DoHeal(unit, Stack);
					if (Battle.BattleEnded)
					{
						break;
					}
				}
			}
		}

		private bool NextToTarget(Point Location, Point Target)
		{
			if (Location == Target)
			{
				return true;
			}
			if (AI.Game.GameCore.Data.CombatMap.GetAdjacentTiles(Location.X, Location.Y).Contains(Target))
			{
				return true;
			}
			return false;
		}

		private void DoAttack(WorkingUnit Unit, WorkingStack EnemyStack, bool AdjacentOnly)
		{
			if (Battle.ActiveStack.Units.Count((WorkingUnit x) => !x.Disabled && x.Class != UnitClasses.Fort) == 1 && Battle.InactiveStack.Units.Count((WorkingUnit x) => !x.Disabled && x.Class != UnitClasses.Fort) > 2)
			{
				return;
			}
			UnitActionData unitActions = Battle.GetUnitActions(Unit);
			WorkingUnit workingUnit = null;
			float num = 0f;
			foreach (WorkingUnit key in unitActions.Targets.Keys)
			{
				if (unitActions.CanAttack(key) && (!AdjacentOnly || NextToTarget(Unit.BattleData.BattleLocation, key.BattleData.BattleLocation)))
				{
					bool ranged = Unit.RangedAttack.GetValue(key) > 0;
					if (AdjacentOnly)
					{
						ranged = false;
					}
					CombatResults combatResults = CombatManager.PerformCombat(Unit, key, CombatType.Simulated, ranged, IncludeRetal: true, IsCounterFire: false);
					float num2 = 100f;
					if (combatResults.AttackerCasualties > 0)
					{
						num2 = (float)combatResults.DefenderCasualties / (float)combatResults.AttackerCasualties;
					}
					if (Unit.Class == UnitClasses.Siege && key.Class == UnitClasses.Fort)
					{
						num2 += 10f;
					}
					if (key.HasAnyNamedFlag("Craven") && key.HasAnyNamedFlag("Shield Wall"))
					{
						num2 += 8f;
					}
					if (num2 > num)
					{
						num = num2;
						workingUnit = key;
					}
				}
			}
			if (workingUnit != null && num > 0.6f)
			{
				CombatAction action = unitActions.GetAction(workingUnit, AdjacentOnly);
				AITacActionFight aITacActionFight = AI.ActionManager.CreateAction<AITacActionFight>();
				aITacActionFight.Unit = Unit;
				aITacActionFight.TargetUnit = workingUnit;
				aITacActionFight.ActionType = action;
				aITacActionFight.UnitActions = unitActions;
				AI.ActionManager.AddAction(aITacActionFight);
			}
		}

		private void DoRangedAttacks(WorkingStack Stack, WorkingStack EnemyStack)
		{
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled && (int)unit.RangedAttack != 0)
				{
					DoAttack(unit, EnemyStack, AdjacentOnly: false);
					if (Battle.BattleEnded)
					{
						break;
					}
				}
			}
		}

		private void DoFortAttacks(WorkingStack Stack, WorkingStack EnemyStack)
		{
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit.Class == UnitClasses.Fort && !unit.Disabled && (int)unit.RangedAttack != 0)
				{
					DoAttack(unit, EnemyStack, AdjacentOnly: false);
					if (Battle.BattleEnded)
					{
						break;
					}
				}
			}
		}

		private void DoAdjacentAttacks(WorkingStack Stack, WorkingStack EnemyStack)
		{
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled && (int)unit.Attack != 0)
				{
					DoAttack(unit, EnemyStack, AdjacentOnly: true);
					if (Battle.BattleEnded)
					{
						break;
					}
				}
			}
		}

		private void DoMeleeAttacks(WorkingStack Stack, WorkingStack EnemyStack)
		{
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled && (int)unit.Attack != 0)
				{
					DoAttack(unit, EnemyStack, AdjacentOnly: false);
					if (Battle.BattleEnded)
					{
						break;
					}
				}
			}
		}

		private void MoveUnitTowardsTargets(WorkingUnit Unit, List<Point> points)
		{
			UnitActionData unitActions = Battle.GetUnitActions(Unit);
			DistanceMap distanceMap = Battle.Map.CreateDistanceMap(points);
			float num = 2.1474836E+09f;
			Point point = Point.Empty;
			foreach (Point key in unitActions.MovementCosts.Keys)
			{
				if (Battle.Map.AllTiles.Contains(key) && Battle.Map.GetTile(key).Unit == null && !(distanceMap[key.X, key.Y] >= num) && (Battle.Map.GetAdjacentEnemies(key, AI.Realm.ID).Count <= 0 || (int)Unit.RangedAttack <= (int)Unit.Attack))
				{
					num = distanceMap[key.X, key.Y];
					point = key;
				}
			}
			if (!(point == Point.Empty) && !(point == Unit.BattleData.BattleLocation))
			{
				AITacActionMove aITacActionMove = AI.ActionManager.CreateAction<AITacActionMove>();
				aITacActionMove.Unit = Unit;
				aITacActionMove.UnitActions = unitActions;
				aITacActionMove.TargetTile = point;
				AI.ActionManager.AddAction(aITacActionMove);
			}
		}

		private void MoveUnitsTowardsEnemies(WorkingStack Stack, WorkingStack EnemyStack)
		{
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled)
				{
					MoveUnitTowardsTargets(unit, EnemyStack.GetBattlePoints());
				}
			}
		}

		private int UnitDefenceComparer(WorkingUnit A, WorkingUnit B)
		{
			bool flag = A.HasAnyNamedFlag("Craven");
			bool flag2 = B.HasAnyNamedFlag("Craven");
			if (flag && !flag2)
			{
				return -1;
			}
			if (flag2 && !flag)
			{
				return 1;
			}
			return B.Defence.CompareTo(A.Defence);
		}

		private Dictionary<int, List<WorkingUnit>> BatchUnits(List<WorkingUnit> Units)
		{
			Dictionary<int, List<WorkingUnit>> dictionary = new Dictionary<int, List<WorkingUnit>>();
			foreach (WorkingUnit Unit in Units)
			{
				if (!dictionary.ContainsKey(Unit.Defence))
				{
					dictionary.Add(Unit.Defence, new List<WorkingUnit>());
				}
				dictionary[Unit.Defence].Add(Unit);
			}
			return dictionary;
		}

		private void MoveUnitsTo(int FirstUnit, List<WorkingUnit> Units, List<Point> TargetPoints)
		{
			foreach (WorkingUnit Unit in Units)
			{
				bool flag = false;
				for (int i = FirstUnit; i < FirstUnit + Units.Count; i++)
				{
					if (i < TargetPoints.Count && TargetPoints[i] == Unit.BattleData.BattleLocation)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
				List<Point> list = new List<Point>();
				for (int j = FirstUnit; j < FirstUnit + Units.Count; j++)
				{
					if (j < TargetPoints.Count && Battle.Map.AllTiles.Contains(TargetPoints[j]))
					{
						if (Battle.DefenderRealm == AI.Realm && Battle.Map.GetTile(TargetPoints[j]).Terrain.BaseType.IsAnyType("river"))
						{
							return;
						}
						if (Battle.Map.GetTile(TargetPoints[j]).Unit == null)
						{
							list.Add(TargetPoints[j]);
						}
					}
				}
				if (list.Count == 0)
				{
					for (int k = FirstUnit; k < FirstUnit + Units.Count; k++)
					{
						if (k < TargetPoints.Count && Battle.Map.AllTiles.Contains(TargetPoints[k]))
						{
							if (Battle.DefenderRealm == AI.Realm && Battle.Map.GetTile(TargetPoints[k]).Terrain.BaseType.IsAnyType("river"))
							{
								return;
							}
							list.Add(TargetPoints[k]);
						}
					}
				}
				if (list.Count != 0)
				{
					MoveUnitTowardsTargets(Unit, list);
				}
			}
		}

		private void MoveUnits(Facings Facing)
		{
			WorkingStack activeStack = Battle.ActiveStack;
			_ = Battle.InactiveStack;
			List<WorkingUnit> list = activeStack.Units.Where((WorkingUnit x) => (x.Class == UnitClasses.Infantry || x.Class == UnitClasses.Irregular) && !x.Disabled).ToList();
			List<WorkingUnit> list2 = activeStack.Units.Where((WorkingUnit x) => x.Class == UnitClasses.Archer && !x.Disabled).ToList();
			List<WorkingUnit> list3 = activeStack.Units.Where((WorkingUnit x) => (x.Class == UnitClasses.Siege || x.Class == UnitClasses.Wagon) && !x.Disabled).ToList();
			List<WorkingUnit> list4 = activeStack.Units.Where((WorkingUnit x) => x.Class == UnitClasses.Cavalry && !x.Disabled).ToList();
			list.Sort(UnitDefenceComparer);
			list2.Sort(UnitDefenceComparer);
			list3.Sort(UnitDefenceComparer);
			list4.Sort(UnitDefenceComparer);
			int num = list4.Count;
			int num2 = list.Count + list2.Count + list3.Count;
			while (num > 8)
			{
				num2++;
				num--;
				WorkingUnit item = list4[list4.Count - 1];
				list4.Remove(item);
				list.Add(item);
			}
			List<Point> mainLinePositions = FormationManager.GetMainLinePositions(num2, CurrentLocation, Facing);
			int num3 = 0;
			Dictionary<int, List<WorkingUnit>> dictionary = BatchUnits(list);
			foreach (KeyValuePair<int, List<WorkingUnit>> item2 in dictionary)
			{
				MoveUnitsTo(num3, item2.Value, mainLinePositions);
				num3 += item2.Value.Count;
			}
			dictionary = BatchUnits(list2);
			foreach (KeyValuePair<int, List<WorkingUnit>> item3 in dictionary)
			{
				MoveUnitsTo(num3, item3.Value, mainLinePositions);
				num3 += item3.Value.Count;
			}
			dictionary = BatchUnits(list3);
			foreach (KeyValuePair<int, List<WorkingUnit>> item4 in dictionary)
			{
				MoveUnitsTo(num3, item4.Value, mainLinePositions);
				num3 += item4.Value.Count;
			}
			mainLinePositions = FormationManager.GetFlankPositions(num, CurrentLocation, Facing, mainLinePositions);
			num3 = 0;
			dictionary = BatchUnits(list4);
			foreach (KeyValuePair<int, List<WorkingUnit>> item5 in dictionary)
			{
				MoveUnitsTo(num3, item5.Value, mainLinePositions);
				num3 += item5.Value.Count;
			}
		}

		private Point GetNearestEnemyVP()
		{
			Point result = Point.Empty;
			float num = float.MaxValue;
			foreach (Point vPTile in Battle.Map.GetVPTiles())
			{
				if (Battle.GetVPOwner(vPTile) != AI.Realm)
				{
					float num2 = Math.Abs(CurrentLocation.X - vPTile.X);
					float num3 = Math.Abs(CurrentLocation.Y - vPTile.Y);
					float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
					if (num4 < num)
					{
						num = num4;
						result = vPTile;
					}
				}
			}
			return result;
		}

		private double GetArcherAttackRatio(WorkingStack FriendlyStack, WorkingStack EnemyStack)
		{
			List<WorkingUnit> list = FriendlyStack.Units.Where((WorkingUnit x) => (int)x.Range > 1 && !x.Disabled).ToList();
			int num = 0;
			foreach (WorkingUnit item in list)
			{
				UnitActionData unitActions = Battle.GetUnitActions(item);
				if (unitActions.CanAttack())
				{
					num++;
				}
			}
			return (float)num / (float)list.Count;
		}

		private void DoMainPhase(WorkingStack FriendlyStack, WorkingStack EnemyStack, Facings Facing)
		{
			switch (CurrentState)
			{
				case TacticalStates.Melee:
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoFortAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoAdjacentAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoMeleeAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					MoveUnitsTowardsEnemies(FriendlyStack, EnemyStack);
					if (!Battle.BattleEnded)
					{
						DoRangedAttacks(FriendlyStack, EnemyStack);
						if (!Battle.BattleEnded)
						{
							DoAdjacentAttacks(FriendlyStack, EnemyStack);
						}
					}
					break;
				case TacticalStates.Formingup:
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoFortAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoAdjacentAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					MoveUnits(Facing);
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (!Battle.BattleEnded)
					{
						DoAdjacentAttacks(FriendlyStack, EnemyStack);
						if (!Battle.BattleEnded)
						{
						}
					}
					break;
				case TacticalStates.Holding:
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoFortAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoAdjacentAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					MoveUnits(Facing);
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (!Battle.BattleEnded)
					{
						DoAdjacentAttacks(FriendlyStack, EnemyStack);
						if (!Battle.BattleEnded)
						{
						}
					}
					break;
				case TacticalStates.Retreating:
					{
						DoRangedAttacks(FriendlyStack, EnemyStack);
						if (Battle.BattleEnded)
						{
							break;
						}
						DoFortAttacks(FriendlyStack, EnemyStack);
						if (Battle.BattleEnded)
						{
							break;
						}
						DoAdjacentAttacks(FriendlyStack, EnemyStack);
						if (Battle.BattleEnded)
						{
							break;
						}
						Point backmostVP = Battle.Map.GetBackmostVP();
						if (!NextToTarget(CurrentLocation, backmostVP))
						{
							Vector2 vector3 = new Vector2(backmostVP.X, backmostVP.Y);
							Vector2 vector4 = new Vector2(CurrentLocation.X, CurrentLocation.Y);
							Vector2 direction4 = vector3 - vector4;
							Facings direction5 = AITacticalUtilities.ConvertDirectionToFacing(direction4);
							CurrentLocation = AITacticalUtilities.MoveInDirection(CurrentLocation, direction5, 2, Battle.Map);
							MoveUnits(Facing);
						}
						DoRangedAttacks(FriendlyStack, EnemyStack);
						if (!Battle.BattleEnded)
						{
							DoAdjacentAttacks(FriendlyStack, EnemyStack);
							if (!Battle.BattleEnded)
							{
							}
						}
						break;
					}
				case TacticalStates.Advancing:
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoFortAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoAdjacentAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoMeleeAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					CurrentLocation = AITacticalUtilities.MoveInDirection(CurrentLocation, Facing, 4, Battle.Map);
					MoveUnits(Facing);
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (!Battle.BattleEnded)
					{
						DoAdjacentAttacks(FriendlyStack, EnemyStack);
						if (!Battle.BattleEnded)
						{
						}
					}
					break;
				case TacticalStates.Avoiding:
					{
						DoRangedAttacks(FriendlyStack, EnemyStack);
						if (Battle.BattleEnded)
						{
							break;
						}
						DoFortAttacks(FriendlyStack, EnemyStack);
						if (Battle.BattleEnded)
						{
							break;
						}
						Point nearestEnemyVP = GetNearestEnemyVP();
						if (nearestEnemyVP != Point.Empty)
						{
							Vector2 vector = new Vector2(nearestEnemyVP.X, nearestEnemyVP.Y);
							Vector2 vector2 = new Vector2(CurrentLocation.X, CurrentLocation.Y);
							Vector2 direction2 = vector - vector2;
							Facings direction3 = AITacticalUtilities.ConvertDirectionToFacing(direction2);
							CurrentLocation = AITacticalUtilities.MoveInDirection(CurrentLocation, direction3, 4, Battle.Map);
						}
						MoveUnits(Facing);
						DoRangedAttacks(FriendlyStack, EnemyStack);
						if (!Battle.BattleEnded)
						{
							DoAdjacentAttacks(FriendlyStack, EnemyStack);
							if (!Battle.BattleEnded)
							{
							}
						}
						break;
					}
				case TacticalStates.Skirmishing:
					DoRangedAttacks(FriendlyStack, EnemyStack);
					if (Battle.BattleEnded)
					{
						break;
					}
					DoFortAttacks(FriendlyStack, EnemyStack);
					if (!Battle.BattleEnded)
					{
						if (GetArcherAttackRatio(FriendlyStack, EnemyStack) >= 0.7)
						{
							Facings direction = AITacticalUtilities.RotateFacing(Facing, 4);
							CurrentLocation = AITacticalUtilities.MoveInDirection(CurrentLocation, direction, 2, Battle.Map);
						}
						else
						{
							CurrentLocation = AITacticalUtilities.MoveInDirection(CurrentLocation, Facing, 1, Battle.Map);
						}
						MoveUnits(Facing);
						DoRangedAttacks(FriendlyStack, EnemyStack);
						if (!Battle.BattleEnded)
						{
							DoAdjacentAttacks(FriendlyStack, EnemyStack);
							_ = Battle.BattleEnded;
						}
					}
					break;
			}
		}

		[HandleProcessCorruptedStateExceptions]
		private void TakeTurn()
		{
			try
			{
				PlayTurnStartCard();
				if (ShouldRetreat())
				{
					AITacActionRetreat action = AI.ActionManager.CreateAction<AITacActionRetreat>();
					AI.ActionManager.AddAction(action);
					return;
				}
				if (CurrentState == TacticalStates.FirstTurn)
				{
					InitialisePosition();
				}
				CurrentState = DetermineCurrentStrategy();
				Vector2 direction = default(Vector2);
				Vector2 vector = new Vector2(CurrentLocation.X, CurrentLocation.Y);
				int num = 0;
				WorkingStack activeStack = Battle.ActiveStack;
				WorkingStack inactiveStack = Battle.InactiveStack;
				foreach (WorkingUnit unit in inactiveStack.Units)
				{
					Vector2 vector2 = new Vector2(unit.BattleData.BattleX, unit.BattleData.BattleY);
					direction += vector2 - vector;
					num++;
				}
				direction /= (float)num;
				direction.Normalize();
				Facings facing = AITacticalUtilities.ConvertDirectionToFacing(direction);
				UpdateSiegeUnits(activeStack, inactiveStack);
				DoHeals(activeStack);
				DoMainPhase(activeStack, inactiveStack, facing);
				DoHeals(activeStack);
				PlayTurnEndCard();
				if (activeStack.Units.Count((WorkingUnit x) => x.BattleData != null && (x.BattleData.CanMove || x.BattleData.CanFight)) > 0)
				{
					DoMainPhase(activeStack, inactiveStack, facing);
				}
				AITacActionEndTurn action2 = AI.ActionManager.CreateAction<AITacActionEndTurn>();
				AI.ActionManager.AddAction(action2);
			}
			catch (Exception ex)
			{
				ErrorDialog errorDialog = new ErrorDialog(ex.Message, ex.StackTrace, AI.Game.GameCore);
				errorDialog.ShowDialog();
				AI.Game.GameCore.Stop();
			}
		}

		internal void BeginTurn()
		{
			Thread thread = new Thread(TakeTurn);
			thread.Start();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write((short)CurrentState);
			w.Write(CurrentLocation.X);
			w.Write(CurrentLocation.Y);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			CurrentState = (TacticalStates)r.ReadInt16();
			CurrentLocation = new Point(r.ReadInt32(), r.ReadInt32());
		}
	}
}