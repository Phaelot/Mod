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

		private List<Point> PriorityVPs;

		private int TacticalTurnNumber;

		private void LogTactical(string Text)
		{
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "TacticalBattle.log");
				System.IO.File.AppendAllText(file, Text + "\r\n");
			}
			catch
			{
			}
		}

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
			this.TacticalTurnNumber = 0;
			FormationManager = new AIFormationManager(Battle);
			LogTactical("");
			LogTactical("========================================");
			LogTactical("TACTICAL BATTLE START: " + Battle.AttackerRealm.Name + " vs " + Battle.DefenderRealm.Name);
			LogTactical("  AI controls: " + AI.Realm.Name);
			LogTactical("  Attacker: " + Battle.Attacker.Units.Count + " units");
			LogTactical("  Defender: " + Battle.Defender.Units.Count + " units");
			if (Battle.Node != null && Battle.Node.Province != null)
			{
				LogTactical("  Province: " + Battle.Node.Province.Name);
			}
			LogTactical("========================================");
		}

		private int GetDefenderUnitCount()
		{
			return Battle.Defender.Units.Count((WorkingUnit x) => !x.Disabled && x.Class != UnitClasses.Fort);
		}

		private int GetPriorityVPCount()
		{
			int unitCount = GetDefenderUnitCount();
			int totalVPs = Battle.GetTotalVPCount();
			if (totalVPs <= 1)
			{
				return 1;
			}
			if (unitCount >= 18)
			{
				return totalVPs;
			}
			if (unitCount >= 12)
			{
				return Math.Min(2, totalVPs);
			}
			return 1;
		}

		private void UpdatePriorityVPs()
		{
			if (Battle.DefenderRealm != AI.Realm)
			{
				PriorityVPs = null;
				return;
			}
			int count = GetPriorityVPCount();
			int defenderUnits = GetDefenderUnitCount();
			int totalVPs = Battle.GetTotalVPCount();
			LogTactical("  VP priority: " + defenderUnits + " defenders, " + totalVPs + " total VPs, concentrating on " + count);
			List<Point> allVPs = Battle.Map.GetVPTiles();
			List<Point> ownedVPs = new List<Point>();
			foreach (Point vp in allVPs)
			{
				if (Battle.GetVPOwner(vp) == AI.Realm)
				{
					ownedVPs.Add(vp);
				}
			}
			if (ownedVPs.Count == 0)
			{
				ownedVPs = allVPs;
			}
			if (count >= ownedVPs.Count)
			{
				PriorityVPs = ownedVPs;
				return;
			}
			if (count == 1)
			{
				Point backmost = Battle.Map.GetBackmostVP();
				if (ownedVPs.Contains(backmost))
				{
					PriorityVPs = new List<Point> { backmost };
				}
				else
				{
					PriorityVPs = new List<Point> { ownedVPs[0] };
				}
				return;
			}
			float bestDist = float.MaxValue;
			List<Point> bestPair = null;
			for (int i = 0; i < ownedVPs.Count; i++)
			{
				for (int j = i + 1; j < ownedVPs.Count; j++)
				{
					float dx = ownedVPs[i].X - ownedVPs[j].X;
					float dy = ownedVPs[i].Y - ownedVPs[j].Y;
					float dist = (float)Math.Sqrt(dx * dx + dy * dy);
					if (dist < bestDist)
					{
						bestDist = dist;
						bestPair = new List<Point> { ownedVPs[i], ownedVPs[j] };
					}
				}
			}
			PriorityVPs = bestPair ?? new List<Point> { ownedVPs[0] };
		}

		private Point GetPriorityVPCenter()
		{
			if (PriorityVPs == null || PriorityVPs.Count == 0)
			{
				return Battle.Map.GetBackmostVP();
			}
			float cx = 0f;
			float cy = 0f;
			foreach (Point vp in PriorityVPs)
			{
				cx += vp.X;
				cy += vp.Y;
			}
			return new Point((int)(cx / PriorityVPs.Count), (int)(cy / PriorityVPs.Count));
		}

		private int GetTileDefenseScore(BattleTile tile)
		{
			if (tile == null || tile.Terrain == null)
			{
				return 0;
			}
			int score = 0;
			if (tile.Terrain.BaseType.IsAnyType("mountain"))
			{
				score = 4;
			}
			else if (tile.Terrain.BaseType.IsAnyType("hills"))
			{
				score = 3;
			}
			else if (tile.Terrain.BaseType.IsAnyType("old forest"))
			{
				score = 3;
			}
			else if (tile.Terrain.BaseType.IsAnyType("lt forest"))
			{
				score = 2;
			}
			else if (tile.Terrain.BaseType.IsAnyType("swamp"))
			{
				score = 1;
			}
			else if (tile.Terrain.BaseType.IsAnyType("wasteland"))
			{
				score = 1;
			}
			if (tile.HasTown())
			{
				score += 1;
			}
			return score;
		}

		private List<Point> GetDefensivePositions(int unitCount)
		{
			if (PriorityVPs == null || PriorityVPs.Count == 0)
			{
				return new List<Point>();
			}
			Dictionary<Point, int> tileScores = new Dictionary<Point, int>();
			foreach (Point vp in PriorityVPs)
			{
				if (!tileScores.ContainsKey(vp))
				{
					BattleTile vpTile = Battle.Map.GetTile(vp);
					if (vpTile != null && vpTile.InsideBattle)
					{
						tileScores[vp] = GetTileDefenseScore(vpTile) + 5;
					}
				}
				List<Point> ring1 = AI.Game.GameCore.Data.CombatMap.GetAdjacentTiles(vp.X, vp.Y);
				foreach (Point adj in ring1)
				{
					if (tileScores.ContainsKey(adj))
					{
						continue;
					}
					BattleTile tile = Battle.Map.GetTile(adj);
					if (tile == null || !tile.InsideBattle || tile.Terrain == null)
					{
						continue;
					}
					if (tile.Terrain.IsNaval)
					{
						continue;
					}
					tileScores[adj] = GetTileDefenseScore(tile) + 3;
				}
				foreach (Point adj in ring1)
				{
					List<Point> ring2 = AI.Game.GameCore.Data.CombatMap.GetAdjacentTiles(adj.X, adj.Y);
					foreach (Point adj2 in ring2)
					{
						if (tileScores.ContainsKey(adj2))
						{
							continue;
						}
						BattleTile tile2 = Battle.Map.GetTile(adj2);
						if (tile2 == null || !tile2.InsideBattle || tile2.Terrain == null)
						{
							continue;
						}
						if (tile2.Terrain.IsNaval)
						{
							continue;
						}
						tileScores[adj2] = GetTileDefenseScore(tile2);
					}
				}
			}
			List<KeyValuePair<Point, int>> sorted = new List<KeyValuePair<Point, int>>(tileScores);
			sorted.Sort((KeyValuePair<Point, int> a, KeyValuePair<Point, int> b) => b.Value.CompareTo(a.Value));
			List<Point> result = new List<Point>();
			foreach (KeyValuePair<Point, int> kvp in sorted)
			{
				result.Add(kvp.Key);
				if (result.Count >= unitCount)
				{
					break;
				}
			}
			return result;
		}

		private void MoveUnitsToDefensivePositions(WorkingStack FriendlyStack)
		{
			List<WorkingUnit> units = FriendlyStack.Units.Where((WorkingUnit x) => !x.Disabled && x.Class != UnitClasses.Fort && x.Class != UnitClasses.Siege).ToList();
			units.Sort(UnitDefenceComparer);
			List<Point> positions = GetDefensivePositions(units.Count);
			if (positions.Count == 0)
			{
				return;
			}
			foreach (WorkingUnit unit in units)
			{
				if (unit.BattleData == null)
				{
					continue;
				}
				bool alreadyInPosition = false;
				foreach (Point pos in positions)
				{
					if (pos == unit.BattleData.BattleLocation)
					{
						alreadyInPosition = true;
						break;
					}
				}
				if (alreadyInPosition)
				{
					continue;
				}
				List<Point> emptyPositions = new List<Point>();
				foreach (Point pos in positions)
				{
					BattleTile tile = Battle.Map.GetTile(pos);
					if (tile != null && tile.Unit == null)
					{
						emptyPositions.Add(pos);
					}
				}
				if (emptyPositions.Count > 0)
				{
					MoveUnitTowardsTargets(unit, emptyPositions);
				}
			}
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
				LogTactical("  Retreat check: turn " + Battle.TurnCounter.CurrentTurn + " <= 4, too early");
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
					LogTactical("  Retreat check: defender timer skip (chance=" + val + "%)");
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
					LogTactical("  Retreat check: attacker timer skip (chance=" + val + "%)");
					return false;
				}
			}
			if (Battle.ActiveStack.Owner == AI.Game.RebelRealm)
			{
				LogTactical("  Retreat check: rebels never retreat");
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
					LogTactical("  Retreat check: no retreat targets, fighting on (95% chance)");
					return false;
				}
			}
			else if (retreatList.RetreatTargets.Count < num && AI.RNG.Next(100) > 25)
			{
				LogTactical("  Retreat check: not enough retreat room (" + retreatList.RetreatTargets.Count + "/" + num + "), fighting on (75% chance)");
				return false;
			}
			if (Battle.Node.Province != null && Battle.Node.Province.OwnerRealm == AI.Realm && Battle.Node.Province.IsCapitol)
			{
				LogTactical("  Retreat check: defending capital, never retreat");
				return false;
			}
			float dmgRatio = GetDamageRatio();
			bool shouldRetreat = (double)dmgRatio < 0.35;
			LogTactical("  Retreat check: damageRatio=" + dmgRatio.ToString("F2") + " (threshold 0.35) -> " + (shouldRetreat ? "RETREAT" : "FIGHT ON"));
			return shouldRetreat;
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
				UpdatePriorityVPs();
				Point vpCenter = GetPriorityVPCenter();
				Facings direction2 = AITacticalUtilities.ConvertDirectionToFacing(vector);
				CurrentLocation = new Point(vpCenter.X, vpCenter.Y);
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
			float damageRatio = GetDamageRatio();
			bool isAttacker = Battle.AttackerRealm == AI.Realm;
			if (isAttacker)
			{
				float cohesion = GetCohesionValue(Battle.Attacker.Units, CurrentLocation);
				float attackRatio = (float)GetAttackRatio(Battle.Attacker, Battle.Defender);
				float rangedRatio = GetRangedRatio(Battle.Attacker);
				LogTactical("  Strategy eval (attacker): damageRatio=" + damageRatio.ToString("F2") + " cohesion=" + cohesion.ToString("F2") + " attackRatio=" + attackRatio.ToString("F2") + " rangedRatio=" + rangedRatio.ToString("F2"));
				if (cohesion < 0.6f)
				{
					LogTactical("    -> Formingup (cohesion " + cohesion.ToString("F2") + " < 0.6)");
					return TacticalStates.Formingup;
				}
				if (damageRatio > 0.8f)
				{
					if (attackRatio >= 0.25)
					{
						LogTactical("    -> Melee (dmgRatio > 0.8, atkRatio >= 0.25)");
						return TacticalStates.Melee;
					}
					if (rangedRatio > 0.6f)
					{
						LogTactical("    -> Skirmishing (dmgRatio > 0.8, rangedRatio > 0.6)");
						return TacticalStates.Skirmishing;
					}
					LogTactical("    -> Advancing (dmgRatio > 0.8, no melee/skirmish conditions)");
					return TacticalStates.Advancing;
				}
				LogTactical("    -> Avoiding (dmgRatio " + damageRatio.ToString("F2") + " <= 0.8)");
				return TacticalStates.Avoiding;
			}
			float defCohesion = GetCohesionValue(Battle.Defender.Units, CurrentLocation);
			float defRangedRatio = GetRangedRatio(Battle.Defender);
			float atkRangedRatio = GetRangedRatio(Battle.Attacker);
			float defAvg = GetDefenceAverage(Battle.Defender);
			bool concentratingDefense = PriorityVPs != null && PriorityVPs.Count < Battle.GetTotalVPCount();
			LogTactical("  Strategy eval (defender): damageRatio=" + damageRatio.ToString("F2") + " cohesion=" + defCohesion.ToString("F2") + " defRangedRatio=" + defRangedRatio.ToString("F2") + " atkRangedRatio=" + atkRangedRatio.ToString("F2") + " defAvg=" + defAvg.ToString("F1") + " concentrating=" + concentratingDefense);
			if (defCohesion < 0.6f)
			{
				LogTactical("    -> Formingup (cohesion " + defCohesion.ToString("F2") + " < 0.6)");
				return TacticalStates.Formingup;
			}
			if (damageRatio > 1f)
			{
				if (defRangedRatio > 0.4f)
				{
					LogTactical("    -> Skirmishing (dmgRatio > 1.0, defRangedRatio > 0.4)");
					return TacticalStates.Skirmishing;
				}
				if (concentratingDefense)
				{
					LogTactical("    -> Holding (dmgRatio > 1.0, concentrating VPs)");
					return TacticalStates.Holding;
				}
				if (atkRangedRatio > 0.4f)
				{
					LogTactical("    -> Advancing (dmgRatio > 1.0, enemy has ranged > 0.4, closing gap)");
					return TacticalStates.Advancing;
				}
				if (defAvg >= 2f)
				{
					LogTactical("    -> Holding (dmgRatio > 1.0, defAvg >= 2.0)");
					return TacticalStates.Holding;
				}
				LogTactical("    -> Advancing (dmgRatio > 1.0, fallthrough)");
				return TacticalStates.Advancing;
			}
			if ((double)damageRatio > 0.8)
			{
				if (defRangedRatio > 0.6f)
				{
					LogTactical("    -> Skirmishing (dmgRatio > 0.8, defRangedRatio > 0.6)");
					return TacticalStates.Skirmishing;
				}
				if (concentratingDefense)
				{
					LogTactical("    -> Holding (dmgRatio > 0.8, concentrating VPs)");
					return TacticalStates.Holding;
				}
				if (atkRangedRatio > 0.6f)
				{
					LogTactical("    -> Advancing (dmgRatio > 0.8, enemy ranged > 0.6)");
					return TacticalStates.Advancing;
				}
				LogTactical("    -> Holding (dmgRatio > 0.8, fallthrough)");
				return TacticalStates.Holding;
			}
			if (defRangedRatio > 0.4f)
			{
				LogTactical("    -> Skirmishing (dmgRatio <= 0.8, defRangedRatio > 0.4)");
				return TacticalStates.Skirmishing;
			}
			LogTactical("    -> Retreating (dmgRatio " + damageRatio.ToString("F2") + " <= 0.8, no ranged fallback)");
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
				LogTactical("    " + Unit.DisplayName + ": last unit standing, holding back");
				return;
			}
			UnitActionData unitActions = Battle.GetUnitActions(Unit);
			WorkingUnit workingUnit = null;
			float num = 0f;
			int candidateCount = 0;
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
					candidateCount++;
					LogTactical("      vs " + key.DisplayName + " (HP=" + (int)key.Health + "): deal=" + combatResults.DefenderCasualties + " take=" + combatResults.AttackerCasualties + " ratio=" + num2.ToString("F1") + (ranged ? " [ranged]" : "") + (num2 > num ? " *best*" : ""));
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
				LogTactical("    " + Unit.DisplayName + " -> " + action.ToString() + " " + workingUnit.DisplayName + " (ratio=" + num.ToString("F1") + ", " + candidateCount + " candidates)");
				AITacActionFight aITacActionFight = AI.ActionManager.CreateAction<AITacActionFight>();
				aITacActionFight.Unit = Unit;
				aITacActionFight.TargetUnit = workingUnit;
				aITacActionFight.ActionType = action;
				aITacActionFight.UnitActions = unitActions;
				AI.ActionManager.AddAction(aITacActionFight);
			}
			else if (candidateCount > 0)
			{
				LogTactical("    " + Unit.DisplayName + ": no target worth attacking (best ratio=" + num.ToString("F1") + " < 0.6, " + candidateCount + " candidates)");
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
			bool avoidDisengage = CurrentState != TacticalStates.Retreating && CurrentState != TacticalStates.Avoiding;
			foreach (Point key in unitActions.MovementCosts.Keys)
			{
				if (Battle.Map.AllTiles.Contains(key) && Battle.Map.GetTile(key).Unit == null && !(distanceMap[key.X, key.Y] >= num) && (Battle.Map.GetAdjacentEnemies(key, AI.Realm.ID).Count <= 0 || (int)Unit.RangedAttack <= (int)Unit.Attack))
				{
					if (avoidDisengage && Battle.Map.IsDisengageMove(Unit, key))
					{
						continue;
					}
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
			LogTactical("  Executing phase: " + CurrentState.ToString());
			switch (CurrentState)
			{
				case TacticalStates.Melee:
					LogTactical("    [Ranged attacks]");
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
					if (PriorityVPs != null && PriorityVPs.Count > 0 && Battle.DefenderRealm == AI.Realm)
					{
						MoveUnitsToDefensivePositions(FriendlyStack);
					}
					else
					{
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
					if (PriorityVPs != null && PriorityVPs.Count > 0 && Battle.DefenderRealm == AI.Realm)
					{
						MoveUnitsToDefensivePositions(FriendlyStack);
					}
					else
					{
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
				TacticalTurnNumber++;
				string side = (Battle.AttackerRealm == AI.Realm) ? "Attacker" : "Defender";
				int friendlyCount = Battle.ActiveStack.Units.Count((WorkingUnit x) => !x.Disabled);
				int enemyCount = Battle.InactiveStack.Units.Count((WorkingUnit x) => !x.Disabled);
				LogTactical("");
				LogTactical("=== TACTICAL TURN " + TacticalTurnNumber + " === " + AI.Realm.Name + " (" + side + ") === " + friendlyCount + " vs " + enemyCount + " ===");
				PlayTurnStartCard();
				if (ShouldRetreat())
				{
					LogTactical("  DECISION: RETREAT");
					AITacActionRetreat action = AI.ActionManager.CreateAction<AITacActionRetreat>();
					AI.ActionManager.AddAction(action);
					return;
				}
				if (CurrentState == TacticalStates.FirstTurn)
				{
					LogTactical("  First turn: initializing position");
					InitialisePosition();
				}
				if (Battle.DefenderRealm == AI.Realm)
				{
					UpdatePriorityVPs();
					Point vpCenter = GetPriorityVPCenter();
					CurrentLocation = vpCenter;
					LogTactical("  Defender VP center: (" + vpCenter.X + "," + vpCenter.Y + ")");
				}
				CurrentState = DetermineCurrentStrategy();
				LogTactical("  STRATEGY: " + CurrentState.ToString());
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
				LogTactical("  Enemy facing: " + facing.ToString());
				UpdateSiegeUnits(activeStack, inactiveStack);
				DoHeals(activeStack);
				LogTactical("  --- Main phase 1 ---");
				DoMainPhase(activeStack, inactiveStack, facing);
				DoHeals(activeStack);
				PlayTurnEndCard();
				if (activeStack.Units.Count((WorkingUnit x) => x.BattleData != null && (x.BattleData.CanMove || x.BattleData.CanFight)) > 0)
				{
					LogTactical("  --- Main phase 2 (units still have actions) ---");
					DoMainPhase(activeStack, inactiveStack, facing);
				}
				LogTactical("  Turn end. Friendly: " + activeStack.Units.Count((WorkingUnit x) => !x.Disabled) + " active. Enemy: " + inactiveStack.Units.Count((WorkingUnit x) => !x.Disabled) + " active.");
				AITacActionEndTurn action2 = AI.ActionManager.CreateAction<AITacActionEndTurn>();
				AI.ActionManager.AddAction(action2);
			}
			catch (Exception ex)
			{
				LogTactical("  EXCEPTION: " + ex.Message + "\r\n" + ex.StackTrace);
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