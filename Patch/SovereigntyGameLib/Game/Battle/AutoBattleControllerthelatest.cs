using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Battle
{
	public class AutoBattleController
	{
		public AutoBattleController(SovereigntyGame Game, BattleStarter PendingBattle)
		{
			Game.GameCore.FireEvent("TickerMessage", new object[]
			{
				new TickerMessage(GameText.CreateFromLiteral(string.Concat(new string[]
				{
					PendingBattle.Attacker.Owner.Name,
					" attacks ",
					PendingBattle.Defender.Owner.Name,
					" at ",
					PendingBattle.Defender.Node.GetRegion().Name
				})), TickerMessageType.Default, 1)
			});
			this.RNG = new Random();
			this.Game = Game;
			this.Attacker = PendingBattle.Attacker;
			this.Defender = PendingBattle.Defender;
			this.Node = this.Defender.Node;
			this.CapturedattackerUnits = new List<WorkingUnit>();
			this.CapturedDefenderUnits = new List<WorkingUnit>();
			this.BattleLog = new List<string>();
			this.DetailedBattleLog = new List<string>();
			this.ChargedCavalryUnits = new List<WorkingUnit>();
			this.CavalryChargeStageUsed = false;
			if (PendingBattle.AttackPath == null)
			{
				if (this.Node.NodeType != PathNodeTypes.Sea)
				{
					throw new Exception("null path when starting non-naval battle");
				}
				this.AttackerNode = this.Attacker.Node;
			}
			else
			{
				this.AttackerNode = PendingBattle.AttackPath.PathPoints[PendingBattle.AttackPath.PathPoints.Count - 2].Node;
			}
			this.InitialAttackers = this.Attacker.Units.Count;
			this.InitialDefenders = this.Defender.Units.Count;
			if (this.Defender.Node.Province != null && this.Defender.Node.Province.BattleField != null)
			{
				this.Attacker.AwardHeroXP(5);
				this.Defender.AwardHeroXP(5);
			}
			if (PendingBattle.AttackPath == null)
			{
				this.RiverCrossing = false;
			}
			else
			{
				this.RiverCrossing = PendingBattle.AttackPath.DoesCrossRiver();
			}
			if (this.Node.Province != null)
			{
				for (int i = 0; i < this.Node.Province.FortLevel; i++)
				{
					WorkingUnit workingUnit = this.Node.Province.Forts[i];
					workingUnit.OwnerRealmID = this.Defender.OwnerID;
					this.Defender.AddUnit(workingUnit, false, true);
				}
			}
			this.UnpackTransports();
			this.AttackerPositions = new WorkingUnit[3, 10];
			this.DefenderPositions = new WorkingUnit[3, 10];
			this.PlayerWatching = PendingBattle.PlayerWatching;
			PendingBattle.Dispose();
		}

		private void UnpackTransports()
		{
			this.TransportedUnits = new List<WorkingUnit>();
			foreach (WorkingUnit workingUnit in this.Attacker.Units.Where((WorkingUnit x) => x.CarriedUnit != null))
			{
				WorkingUnit carriedUnit = workingUnit.CarriedUnit;
				workingUnit.CarriedUnitID = -1;
				this.Attacker.RemoveUnit(workingUnit);
				this.Attacker.AddUnit(carriedUnit, true, false);
				carriedUnit.Move(100f);
				this.Game.DestroyUnit(workingUnit);
				this.TransportedUnits.Add(carriedUnit);
			}
		}

		public void RepackTransports()
		{
			foreach (WorkingUnit workingUnit in this.TransportedUnits)
			{
				UnitData unitData = this.Attacker.Owner.UnitPurchaseManager.GetUnitsInClass(UnitClasses.Naval).FirstOrDefault((UnitData x) => x.AllowTransport);
				WorkingUnit workingUnit2 = this.Game.CreateUnit(this.Attacker.OwnerID, unitData);
				workingUnit2.CarriedUnitID = workingUnit.ID;
				workingUnit2.Move(100f);
				this.Attacker.RemoveUnit(workingUnit);
				this.Attacker.AddUnit(workingUnit2, true, false);
			}
		}

		public void Init()
		{
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				workingUnit.CreateBattleData(this);
				workingUnit.BattleStarted();
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				workingUnit2.CreateBattleData(this);
				workingUnit2.BattleStarted();
			}
			this.DeployUnits(this.Attacker.Units, ref this.AttackerPositions);
			this.DeployUnits(this.Defender.Units, ref this.DefenderPositions);
			this.AddBattleLogLine("Battle begins: " + this.GetStackDisplayName(this.Attacker) + " attacks " + this.GetStackDisplayName(this.Defender) + ".");
			this.AddBattleDebugLine("Initial attacker stack: " + this.GetDetailedStackSummary(this.Attacker));
			this.AddBattleDebugLine("Initial defender stack: " + this.GetDetailedStackSummary(this.Defender));
			this.AddHeroCommandLogLine(this.Attacker);
			this.AddHeroCommandLogLine(this.Defender);
			this.AddBattleLogLine(this.GetBattlefieldLogLine());
			string terrainModLog = this.GetTerrainModifierLogLines();
			if (!string.IsNullOrEmpty(terrainModLog))
			{
				this.AddBattleLogLine(terrainModLog);
			}
			if (this.RiverCrossing)
			{
				this.AddBattleLogLine("River crossing: Attacker suffers -50% penalty (flyers immune). No cavalry charge possible.");
			}
			this.ApplyHeroAbilities(this.Attacker);
			this.ApplyHeroAbilities(this.Defender);
			if (this.Defender.Owner != this.Game.PlayerRealm)
			{
				int attackerStrength = this.GetArmyStrength(this.Attacker);
				int defenderStrength = this.GetArmyStrength(this.Defender);
				if (defenderStrength > 0 && (float)attackerStrength > (float)defenderStrength * 2.5f)
				{
					this.AddBattleLogLine("Defender is vastly outnumbered (" + defenderStrength + " vs " + attackerStrength + "). The army retreats!");
					this.Retreat(this.Defender);
					return;
				}
			}
			this.Game.GameCore.FireEvent("AutoBattleStart", new object[] { this });
			if (this.GetPlayerStack() == null)
			{
				this.StartPhase(1);
			}
		}

		private void AddBattleLogLine(string Text)
		{
			if (this.BattleLog == null)
			{
				this.BattleLog = new List<string>();
			}
			if (!string.IsNullOrEmpty(Text))
			{
				this.BattleLog.Add(Text);
			}
			if (this.PlayerWatching)
			{
				this.Game.GameCore.FireEvent("AutoBattleLogUpdated", new object[] { this });
			}
		}

		private void AddBattleDebugLine(string Text)
		{
			if (this.DetailedBattleLog == null)
			{
				this.DetailedBattleLog = new List<string>();
			}
			if (!string.IsNullOrEmpty(Text))
			{
				this.DetailedBattleLog.Add("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + Text);
			}
		}

		private string FormatFloat(float Value)
		{
			return Value.ToString("0.00");
		}

		private string DescribeCombatResult(CombatResults Results)
		{
			if (Results == null)
			{
				return "null combat result";
			}
			return this.GetUnitDisplayName(Results.Attacker) + " -> " + this.GetUnitDisplayName(Results.Defender) + " dealt=" + Results.DefenderCasualties + ", received=" + Results.AttackerCasualties + ", net=" + (Results.DefenderCasualties - Results.AttackerCasualties);
		}

		private string GetDetailedStackSummary(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return "null stack";
			}
			List<string> parts = new List<string>();
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (unit != null)
				{
					parts.Add(this.GetUnitDisplayName(unit) + "[HP=" + unit.Health + ", A=" + unit.Attack + ", D=" + unit.Defence + ", RA=" + unit.RangedAttack + ", Range=" + unit.Range + ", Init=" + unit.Initiative + ", Class=" + unit.Class + ", Race=" + unit.Race + "]");
				}
			}
			return this.GetStackDisplayName(Stack) + " units=" + Stack.Units.Count + ": " + string.Join("; ", parts.ToArray());
		}

		private string MakeSafeFilePart(string Text)
		{
			if (string.IsNullOrEmpty(Text))
			{
				return "unknown";
			}
			string safe = Text;
			foreach (char c in System.IO.Path.GetInvalidFileNameChars())
			{
				safe = safe.Replace(c, '_');
			}
			if (safe.Length > 40)
			{
				safe = safe.Substring(0, 40);
			}
			return safe;
		}

		private void SaveBattleLogToFile()
		{
			try
			{
				string basePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				if (string.IsNullOrEmpty(basePath))
				{
					basePath = ".";
				}
				string logDir = System.IO.Path.Combine(basePath, "SovereigntyBattleLogs");
				Directory.CreateDirectory(logDir);

				string attackerName = this.MakeSafeFilePart(this.GetStackDisplayName(this.Attacker));
				string defenderName = this.MakeSafeFilePart(this.GetStackDisplayName(this.Defender));
				string regionName = this.MakeSafeFilePart(this.GetRegionDisplayName());
				string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + attackerName + "_vs_" + defenderName + "_" + regionName + ".txt";
				string filePath = System.IO.Path.Combine(logDir, fileName);

				List<string> lines = new List<string>();
				lines.Add("Sovereignty AutoBattle Log");
				lines.Add("Saved: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				lines.Add("Location: " + this.GetRegionDisplayName());
				lines.Add("Terrain: " + this.GetTerrainDisplayName());
				lines.Add("River crossing: " + this.RiverCrossing);
				lines.Add("Winner: " + this.GetStackDisplayName(this.Winner));
				lines.Add("");
				lines.Add("=== BATTLE LOG ===");
				if (this.BattleLog != null)
				{
					lines.AddRange(this.BattleLog);
				}
				lines.Add("");
				lines.Add("=== DETAILED DEBUG LOG ===");
				if (this.DetailedBattleLog != null)
				{
					lines.AddRange(this.DetailedBattleLog);
				}

				File.WriteAllLines(filePath, lines.ToArray());
			}
			catch (Exception ex)
			{
				this.AddBattleDebugLine("Failed to save battle log: " + ex.Message);
			}
		}

		private string ResolveText(string TextName)
		{
			if (string.IsNullOrEmpty(TextName))
			{
				return "";
			}
			try
			{
				return GameText.CreateLocalised(TextName, new object[0]).GetActualText(this.Game.GameCore);
			}
			catch
			{
				return TextName;
			}
		}

		private string GetStackDisplayName(WorkingStack Stack)
		{
			if (Stack == null || Stack.Owner == null)
			{
				return "Unknown Army";
			}
			return this.ResolveText(Stack.Owner.DisplayName);
		}

		private int GetHeroTargetSelectionBonus(WorkingUnit Unit)
		{
			if (Unit == null || Unit.OwnerStack == null || Unit.OwnerStack.Hero == null)
			{
				return 0;
			}
			return 10;
		}

		private void AddHeroCommandLogLine(WorkingStack Stack)
		{
			if (Stack == null || Stack.Hero == null)
			{
				return;
			}
			this.AddBattleLogLine(this.GetUnitDisplayName(Stack.Hero) + " commands " + this.GetStackDisplayName(Stack) + ": target selection improved by +10%.");
		}

		private string GetUnitDisplayName(WorkingUnit Unit)
		{
			if (Unit == null)
			{
				return "Unknown Unit";
			}
			return this.ResolveText(Unit.DisplayName);
		}

		private string GetRegionDisplayName()
		{
			try
			{
				if (this.Node != null && this.Node.GetRegion() != null)
				{
					return this.ResolveText(this.Node.GetRegion().DisplayName);
				}
			}
			catch
			{
			}
			return "unknown ground";
		}

		private string GetTerrainDisplayName()
		{
			try
			{
				if (this.Node != null && this.Node.GetRegion() != null && this.Node.GetRegion().Terrain != null)
				{
					return this.ResolveText(this.Node.GetRegion().Terrain.DisplayName);
				}
			}
			catch
			{
			}
			return "Unknown Terrain";
		}

		private string GetBattlefieldLogLine()
		{
			string text = "Battlefield: " + this.GetTerrainDisplayName() + " at " + this.GetRegionDisplayName() + ".";
			if (this.RiverCrossing)
			{
				text += " The attack crosses a river.";
			}
			if (this.Node != null && this.Node.Province != null && this.Node.Province.FortLevel > 0)
			{
				text += " Fortifications are present.";
			}
			return text;
		}

		private string GetBattleTerrainType()
		{
			try
			{
				if (this.Node != null && this.Node.GetRegion() != null && this.Node.GetRegion().Terrain != null)
				{
					return this.Node.GetRegion().Terrain.BaseType.ToLowerInvariant();
				}
			}
			catch
			{
			}
			return "plains";
		}

		private float GetDefenderDefenseBonus(WorkingUnit unit)
		{
			if (unit.OwnerStack != this.Defender)
			{
				return 0f;
			}
			string terrain = this.GetBattleTerrainType();
			float bonus = 0f;
			switch (terrain)
			{
			case "hills":
				if (unit.Race == Races.Dwarf || unit.HasAnyNamedFlag("Mountaineer"))
				{
					bonus = 0.30f;
				}
				else
				{
					bonus = 0.20f;
				}
				break;
			case "lt forest":
				if (unit.Race == Races.Elf || unit.HasAnyNamedFlag("Forester"))
				{
					bonus = 0.25f;
				}
				else
				{
					bonus = 0.15f;
				}
				break;
			case "old forest":
				if (unit.Race == Races.Elf || unit.HasAnyNamedFlag("Forester"))
				{
					bonus = 0.35f;
				}
				else if (unit.HasAnyNamedFlag("Darkdweller"))
				{
					bonus = 0.30f;
				}
				else
				{
					bonus = 0.25f;
				}
				break;
			case "mountain":
				if (unit.Race == Races.Dwarf || unit.HasAnyNamedFlag("Mountaineer"))
				{
					bonus = 0.50f;
				}
				else
				{
					bonus = 0.40f;
				}
				break;
			}
			if (unit.HasAnyNamedFlag("Scout") && bonus > 0f)
			{
				bonus = Math.Min(bonus + 0.05f, 0.55f);
			}
			return bonus;
		}

		private float GetAttackerOffenseModifier(WorkingUnit unit)
		{
			string terrain = this.GetBattleTerrainType();
			float mod = 1f;
			if (this.RiverCrossing && unit.OwnerStack == this.Attacker && unit.MoveType != MoveTypes.Air && !unit.HasAnyNamedFlags("Bridging", "Mariner"))
			{
				mod *= 0.50f;
			}
			if (terrain == "swamp" && unit.OwnerStack == this.Attacker)
			{
				if (unit.Race == Races.Undead || unit.HasAnyNamedFlag("Darkdweller"))
				{
					mod = 1f;
				}
				else if (unit.Race == Races.Orc)
				{
					mod = 0.85f;
				}
				else
				{
					mod = 0.70f;
				}
			}
			if (terrain == "wasteland")
			{
				if (unit.Race != Races.Undead && unit.Race != Races.Orc && unit.Race != Races.Dragon && !unit.HasAnyNamedFlag("Darkdweller"))
				{
					mod *= 0.85f;
				}
			}
			if (terrain == "mountain" && unit.Race == Races.Giant)
			{
				mod *= 1.20f;
			}
			if (unit.HasAnyNamedFlag("Raider") && unit.OwnerStack == this.Attacker && (terrain == "plains" || terrain == "wasteland"))
			{
				mod *= 1.15f;
			}
			if (unit.HasAnyNamedFlag("Scout") && mod < 1f)
			{
				mod = 1f - (1f - mod) * 0.5f;
			}
			if (unit.HasAnyNamedFlag("Forester") && (terrain == "lt forest" || terrain == "old forest") && mod < 1f)
			{
				mod = 1f - (1f - mod) * 0.5f;
			}
			if (unit.HasAnyNamedFlag("Mountaineer") && (terrain == "hills" || terrain == "mountain") && mod < 1f)
			{
				mod = 1f - (1f - mod) * 0.5f;
			}
			return mod;
		}

		private float GetDefenseDamageModifier(WorkingUnit unit)
		{
			string terrain = this.GetBattleTerrainType();
			if (terrain == "wasteland" && unit.Race != Races.Undead && unit.Race != Races.Orc && unit.Race != Races.Dragon && !unit.HasAnyNamedFlag("Darkdweller"))
			{
				return 1.15f;
			}
			return 1f;
		}

		private void ApplyTerrainModifiers(CombatResults results)
		{
			int rawDefenderCasualties = results.DefenderCasualties;
			int rawAttackerCasualties = results.AttackerCasualties;

			float attackerMod = this.GetAttackerOffenseModifier(results.Attacker);
			float defenderBonus = this.GetDefenderDefenseBonus(results.Defender);
			float defenderDamageMod = this.GetDefenseDamageModifier(results.Defender);
			results.DefenderCasualties = Math.Max(0, (int)(results.DefenderCasualties * attackerMod * defenderDamageMod * (1f - defenderBonus)));

			float counterMod = this.GetAttackerOffenseModifier(results.Defender);
			float attackerDefBonus = this.GetDefenderDefenseBonus(results.Attacker);
			float attackerDamageMod = this.GetDefenseDamageModifier(results.Attacker);
			results.AttackerCasualties = Math.Max(0, (int)(results.AttackerCasualties * counterMod * attackerDamageMod * (1f - attackerDefBonus)));

			this.AddBattleDebugLine("Terrain modifiers for " + this.DescribeCombatResult(results) + ": dealt raw=" + rawDefenderCasualties + " * attackerMod=" + this.FormatFloat(attackerMod) + " * defenderDamageMod=" + this.FormatFloat(defenderDamageMod) + " * defenderBonusFactor=" + this.FormatFloat(1f - defenderBonus) + " => " + results.DefenderCasualties + "; received raw=" + rawAttackerCasualties + " * counterMod=" + this.FormatFloat(counterMod) + " * attackerDamageMod=" + this.FormatFloat(attackerDamageMod) + " * attackerDefBonusFactor=" + this.FormatFloat(1f - attackerDefBonus) + " => " + results.AttackerCasualties + ".");
		}

		private int GetCavalryFlankChance(WorkingUnit unit)
		{
			string terrain = this.GetBattleTerrainType();
			int baseChance = 20;
			switch (terrain)
			{
			case "plains":
				baseChance = 30;
				break;
			case "hills":
				baseChance = 15;
				break;
			case "lt forest":
				baseChance = 10;
				break;
			case "old forest":
				baseChance = 5;
				break;
			case "mountain":
				baseChance = 0;
				break;
			case "swamp":
				baseChance = 10;
				break;
			}
			if (unit != null && unit.HasAnyNamedFlag("Charge"))
			{
				baseChance += 10;
			}
			return baseChance;
		}

		private int GetCavalryRearStrikeChance(WorkingUnit unit)
		{
			string terrain = this.GetBattleTerrainType();
			int baseChance = 10;
			switch (terrain)
			{
			case "plains":
				baseChance = 20;
				break;
			case "hills":
				baseChance = 5;
				break;
			case "lt forest":
				baseChance = 5;
				break;
			case "old forest":
				baseChance = 3;
				break;
			case "mountain":
				baseChance = 0;
				break;
			case "swamp":
				baseChance = 5;
				break;
			}
			if (unit != null && unit.HasAnyNamedFlag("Charge"))
			{
				baseChance += 5;
			}
			return baseChance;
		}

		private float GetCavalryChargeDamageModifier()
		{
			string terrain = this.GetBattleTerrainType();
			switch (terrain)
			{
			case "old forest":
				return 0.70f;
			case "swamp":
				return 0.50f;
			case "mountain":
				return 0f;
			default:
				return 1f;
			}
		}

		private bool IsCavalryChargeAllowed()
		{
			if (this.RiverCrossing)
			{
				return false;
			}
			return this.GetBattleTerrainType() != "mountain";
		}

		private string GetTerrainModifierLogLines()
		{
			string terrain = this.GetBattleTerrainType();
			switch (terrain)
			{
			case "plains":
				return "Terrain: Open plains. Cavalry flank chance increased. Raiders gain +15% attack.";
			case "hills":
				return "Terrain: Hills. Defender +20% defense (Dwarves/Mountaineers +30%). Cavalry less effective.";
			case "lt forest":
				return "Terrain: Light forest. Defender +15% defense (Elves/Foresters +25%). Cavalry impeded.";
			case "old forest":
				return "Terrain: Dense forest. Defender +25% defense (Elves/Foresters +35%, Darkdwellers +30%). Cavalry severely hampered.";
			case "mountain":
				return "Terrain: Mountains. Defender +40% defense (Dwarves/Mountaineers +50%). Giants +20% attack. No cavalry charge.";
			case "swamp":
				return "Terrain: Swamp. Attacker -30% (Undead/Darkdwellers immune, Orcs -15%). Cavalry charge halved.";
			case "wasteland":
				return "Terrain: Wasteland. All units -15% attack and defense (Undead, Orcs, Dragons, Darkdwellers immune). Raiders gain +15% attack.";
			default:
				return "";
			}
		}

		private void ApplyHeroAbilities(WorkingStack Stack)
		{
			if (Stack == null || Stack.Hero == null)
			{
				return;
			}
			WorkingHero hero = Stack.Hero;
			if (this.RNG.Next(100) >= 70)
			{
				return;
			}
			string ability = hero.BaseAbility;
			WorkingStack enemyStack = (Stack == this.Attacker) ? this.Defender : this.Attacker;
			string heroName = hero.DisplayName != null ? this.ResolveText(hero.DisplayName) : "Hero";
			string stackName = this.GetStackDisplayName(Stack);
			switch (ability)
			{
			case "Fireball":
			case "Ethereal Touch":
			case "Poison Vial":
			case "Boil and Bubble":
			case "Blood Ritual":
			{
				WorkingUnit target = this.GetRandomLivingUnit(enemyStack);
				if (target != null)
				{
					int damage = 15 + this.RNG.Next(20);
					target.Health.Value = Math.Max(0, target.Health.Value - damage);
					this.AddBattleLogLine(heroName + " of " + stackName + " casts " + ability + "! " + this.GetUnitDisplayName(target) + " takes " + damage + " damage.");
					if (target.Health.Value <= 0)
					{
						this.AddBattleLogLine(this.GetUnitDisplayName(target) + " is destroyed.");
					}
				}
				break;
			}
			case "Headlong Rush":
			case "Redouble Assault":
			case "Company Orders":
			case "O' Fortuna":
			{
				WorkingUnit ally = this.GetRandomLivingUnit(Stack);
				if (ally != null)
				{
					int healAmount = 10 + this.RNG.Next(15);
					ally.Health.Value = Math.Min(100, ally.Health.Value + healAmount);
					this.AddBattleLogLine(heroName + " of " + stackName + " inspires the troops with " + ability + "! " + this.GetUnitDisplayName(ally) + " is rallied (+" + healAmount + " HP).");
				}
				break;
			}
			case "Wood Lore":
			case "Grasp of Roots":
			case "Favorable Wind":
			case "Windward Gage":
			case "Concealed Fire":
			case "Relentless Tracker":
			{
				this.AddBattleLogLine(heroName + " of " + stackName + " uses " + ability + "! Troops gain tactical advantage.");
				break;
			}
			default:
			{
				this.AddBattleLogLine(heroName + " of " + stackName + " uses " + ability + "!");
				break;
			}
			}
		}

		private int GetArmyStrength(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return 0;
			}
			float strength = 0f;
			bool isDefender = Stack == this.Defender;
			bool isAttacker = Stack == this.Attacker;
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled)
				{
					float unitStrength = (float)(unit.Attack + unit.Defence + 1);
					if (isDefender)
					{
						float defBonus = this.GetDefenderDefenseBonus(unit);
						unitStrength *= (1f + defBonus);
					}
					if (isAttacker)
					{
						float offMod = this.GetAttackerOffenseModifier(unit);
						unitStrength *= offMod;
					}
					strength += unitStrength;
				}
			}
			return (int)strength;
		}

		private WorkingUnit GetRandomLivingUnit(WorkingStack Stack)
		{
			List<WorkingUnit> alive = new List<WorkingUnit>();
			foreach (WorkingUnit unit in Stack.Units)
			{
				if (!unit.Disabled && unit.Health > 0)
				{
					alive.Add(unit);
				}
			}
			if (alive.Count == 0)
			{
				return null;
			}
			return alive[this.RNG.Next(alive.Count)];
		}

		private string GetPhaseLogName(int Phase)
		{
			switch (Phase)
			{
			case 1:
				return "Opening missile exchange";
			case 2:
				return "Assault and skirmish";
			case 3:
				return "Ranged fire";
			case 4:
				return "Melee clash";
			case 5:
				return "Battle line reforms";
			case 6:
				return "Melee continues";
			case 7:
				return "Final melee";
			case 8:
				return "Healing and recovery";
			case 10:
				return "Cavalry charge";
			default:
				return "Battle phase " + Phase;
			}
		}

		private void AddCombatLogLine(CombatResults Results, bool Ranged)
		{
			if (Results == null || Results.Attacker == null || Results.Defender == null)
			{
				return;
			}
			int attackerDamage = Results.AttackerCasualties;
			int defenderDamage = Results.DefenderCasualties;
			if (attackerDamage <= 0 && defenderDamage <= 0)
			{
				return;
			}
			string attackType = Ranged ? "shoots at" : "attacks";
			if (defenderDamage > 50)
			{
				attackType = Ranged ? "devastates with ranged fire" : "devastates";
			}
			this.AddBattleLogLine(this.GetUnitDisplayName(Results.Attacker) + " " + attackType + " " + this.GetUnitDisplayName(Results.Defender) + " (" + defenderDamage + " dealt, " + attackerDamage + " received).");
			if (Results.Defender.Health <= 0)
			{
				this.AddBattleLogLine(this.GetUnitDisplayName(Results.Defender) + " is destroyed.");
			}
			if (Results.Attacker.Health <= 0)
			{
				this.AddBattleLogLine(this.GetUnitDisplayName(Results.Attacker) + " is destroyed.");
			}
		}

		private void DeployUnits(IList<WorkingUnit> Units, ref WorkingUnit[,] Positions)
		{
			List<WorkingUnit> list = Units.Where((WorkingUnit x) => x.Class == UnitClasses.Infantry || x.Class == UnitClasses.Irregular).ToList<WorkingUnit>();
			List<WorkingUnit> list2 = Units.Where((WorkingUnit x) => x.Class == UnitClasses.Archer || x.Class == UnitClasses.Siege || x.Class == UnitClasses.Naval).ToList<WorkingUnit>();
			List<WorkingUnit> list3 = Units.Where((WorkingUnit x) => x.Class == UnitClasses.Cavalry).ToList<WorkingUnit>();
			List<WorkingUnit> list4 = Units.Where((WorkingUnit x) => x.Class == UnitClasses.Fort).ToList<WorkingUnit>();
			int num = list.Count + list2.Count;
			int num2 = (int)Math.Ceiling((double)((float)num / 2f));
			num2 = Math.Min(10, num2);
			int num3 = 0;
			int num4 = 5;
			int num5 = 0;
			int num6 = 1;
			list.AddRange(list2);
			foreach (WorkingUnit workingUnit in list)
			{
				Positions[num3, num4] = workingUnit;
				if (num6 % 2 == 0)
				{
					num4 += num6;
				}
				else
				{
					num4 -= num6;
				}
				num6++;
				num5++;
				if (num5 == num2)
				{
					num4 = 5;
					num6 = 1;
					num5 = 0;
					num3++;
				}
			}
			num4 = 5;
			num3 = 0;
			num6 = 1;
			foreach (WorkingUnit workingUnit2 in list3)
			{
				while (Positions[num3, num4] != null)
				{
					if (num6 % 2 == 0)
					{
						num4 += num6;
					}
					else
					{
						num4 -= num6;
					}
					num6++;
					if (num4 == 10)
					{
						num4 = 5;
						num3++;
						num6 = 1;
					}
				}
				Positions[num3, num4] = workingUnit2;
			}
			num4 = 5;
			num3 = 2;
			num6 = 1;
			foreach (WorkingUnit workingUnit3 in list4)
			{
				while (Positions[num3, num4] != null)
				{
					if (num6 % 2 == 0)
					{
						num4 += num6;
					}
					else
					{
						num4 -= num6;
					}
					num6++;
				}
				Positions[num3, num4] = workingUnit3;
			}
		}

		public void StartPhase(int Phase)
		{
			this.CurrentPhase = Phase;
			this.PhaseStarted = false;
			this.NextActionTimer = 0.5f;
			this.AddBattleLogLine("Phase: " + this.GetPhaseLogName(Phase) + ".");
			if (this.PlayerWatching)
			{
				this.Game.GameCore.FireEvent("AutoBattlePhaseStart", new object[] { this, this.CurrentPhase });
			}
		}

		public void Update(float ElapsedTime)
		{
			if (this.CurrentPhase == 0)
			{
				return;
			}
			if (this.Winner != null)
			{
				return;
			}
			if (!this.PlayerWatching)
			{
				for (int i = 0; i < 50; i++)
				{
					if (this.Winner != null)
					{
						return;
					}
					this.PerformNextAction();
				}
				return;
			}
			if (this.NoPauses)
			{
				this.PerformNextAction();
				return;
			}
			if (this.NextActionTimer > 0f)
			{
				this.NextActionTimer -= ElapsedTime;
				if (this.NextActionTimer <= 0f)
				{
					this.PerformNextAction();
				}
			}
		}

		private void PerformNextAction()
		{
			switch (this.CurrentPhase)
			{
			case 1:
				this.DoPhase1Action();
				return;
			case 2:
				this.DoPhase2Action();
				return;
			case 3:
				this.DoPhase3Action();
				return;
			case 4:
				this.DoPhase4Action();
				return;
			case 5:
				this.DoPhase5Action();
				return;
			case 6:
				this.DoPhase6Action();
				return;
			case 7:
				this.DoPhase7Action();
				return;
			case 8:
				this.DoPhase8Action();
				return;
			case 9:
				this.DoPhase9Action();
				return;
			case 10:
				this.DoCavalryChargeAction();
				return;
			default:
				return;
			}
		}

		private void DoPhase8Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.PhaseStarted = true;
				this.UnitList1 = this.GetMeleeList();
				this.UnitList1Index = 0;
				this.NextActionTimer = 0.5f;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					this.StartPhase(9);
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				WorkingStack workingStack = this.Attacker;
				if (workingUnit.OwnerStack == this.Attacker)
				{
					workingStack = this.Defender;
				}
				flag = this.PerformOpportunityAttack(workingUnit, workingStack, false);
				this.UnitList1Index++;
			}
		}

		private void DoPhase9Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.PhaseStarted = true;
				this.UnitList1 = this.GetMeleeList();
				this.UnitList1Index = 0;
				this.NextActionTimer = 0.5f;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					this.EndBattle(this.Defender);
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				WorkingStack workingStack = this.Attacker;
				if (workingUnit.OwnerStack == this.Attacker)
				{
					workingStack = this.Defender;
				}
				flag = this.PerformOpportunityAttack(workingUnit, workingStack, false);
				this.UnitList1Index++;
			}
		}

		private void DoCavalryChargeAction()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.CavalryChargeStageUsed = true;
				this.PhaseStarted = true;
				this.UnitList1 = this.GetCavalryList();
				this.UnitList1Index = 0;
				this.NextActionTimer = 0.5f;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					if (this.PlayerWatching)
					{
						this.Game.GameCore.FireEvent("AutoBattleStageOver", new object[] { 10, this });
					}
					if (this.GetPlayerStack() == null)
					{
						this.StartPhase(3);
					}
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				if (workingUnit.BattleData != null && !workingUnit.Disabled)
				{
					WorkingStack workingStack = this.Attacker;
					if (workingUnit.OwnerStack == this.Attacker)
					{
						workingStack = this.Defender;
					}
					flag = this.PerformCavalryCharge(workingUnit, workingStack);
				}
				this.UnitList1Index++;
			}
		}

		private CombatResults SelectWeightedTarget(List<CombatResults> Results, WorkingUnit ActingUnit)
		{
			if (Results == null || Results.Count == 0)
			{
				return null;
			}
			Results.Sort((a, b) => (b.DefenderCasualties - b.AttackerCasualties).CompareTo(a.DefenderCasualties - a.AttackerCasualties));

			List<string> options = new List<string>();
			foreach (CombatResults result in Results)
			{
				options.Add(this.DescribeCombatResult(result));
			}
			this.AddBattleDebugLine("Target options for " + this.GetUnitDisplayName(ActingUnit) + ": " + string.Join(" | ", options.ToArray()));

			if (Results.Count == 1)
			{
				this.AddBattleDebugLine("Target selection for " + this.GetUnitDisplayName(ActingUnit) + ": only valid target selected -> " + this.DescribeCombatResult(Results[0]));
				return Results[0];
			}

			int heroBonus = this.GetHeroTargetSelectionBonus(ActingUnit);
			int roll = this.RNG.Next(100);
			CombatResults selected = null;
			string bucket = "";
			if (Results.Count == 2)
			{
				int bestChance = Math.Min(90, 60 + heroBonus);
				if (roll < bestChance)
				{
					selected = Results[0];
					bucket = "best";
				}
				else
				{
					selected = Results[1];
					bucket = "worst";
				}
				this.AddBattleDebugLine("Target selection for " + this.GetUnitDisplayName(ActingUnit) + ": roll=" + roll + ", heroBonus=" + heroBonus + ", bestChance=" + bestChance + "%, picked " + bucket + " -> " + this.DescribeCombatResult(selected));
				return selected;
			}

			int bestChance3 = Math.Min(80, 30 + heroBonus);
			int middleChance3 = Math.Max(0, 50 - heroBonus);
			if (roll < bestChance3)
			{
				selected = Results[0];
				bucket = "best";
			}
			else if (roll < bestChance3 + middleChance3)
			{
				selected = Results[Results.Count / 2];
				bucket = "middle";
			}
			else
			{
				selected = Results[Results.Count - 1];
				bucket = "worst";
			}
			this.AddBattleDebugLine("Target selection for " + this.GetUnitDisplayName(ActingUnit) + ": roll=" + roll + ", heroBonus=" + heroBonus + ", bestChance=" + bestChance3 + "%, middleChance=" + middleChance3 + "%, worstChance=" + (100 - bestChance3 - middleChance3) + "%, picked " + bucket + " -> " + this.DescribeCombatResult(selected));
			return selected;
		}

		private bool StackHasLivingCavalry(WorkingStack Stack)
		{
			if (Stack == null)
			{
				return false;
			}
			foreach (WorkingUnit workingUnit in Stack.Units)
			{
				if (!workingUnit.Disabled && workingUnit.Class == UnitClasses.Cavalry)
				{
					return true;
				}
			}
			return false;
		}

		private bool PerformCavalryCharge(WorkingUnit Unit, WorkingStack EnemyStack)
		{
			if (Unit.Disabled || Unit.BattleData == null)
			{
				return false;
			}

			List<CombatResults> allResults = new List<CombatResults>();
			foreach (WorkingUnit workingUnit in EnemyStack.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData != null)
				{
					CombatResults combatResults2 = CombatManager.PerformCombat(Unit, workingUnit, CombatType.Simulated, false, workingUnit.BattleData.CanFight, false);
					if (combatResults2 != null)
					{
						allResults.Add(combatResults2);
					}
				}
			}

			if (allResults.Count == 0)
			{
				return false;
			}

			CombatResults combatResults = this.SelectWeightedTarget(allResults, Unit);
			if (combatResults == null)
			{
				return false;
			}

			Unit.BattleData.CanFight = false;
			if (this.ChargedCavalryUnits != null && !this.ChargedCavalryUnits.Contains(Unit))
			{
				this.ChargedCavalryUnits.Add(Unit);
			}
			bool targetHasGuardian = combatResults.Defender.HasAnyNamedFlag("Guardian");
			bool targetHasPikes = combatResults.Defender.HasAnyNamedFlag("Pikes");
			bool enemyHasLivingCavalry = this.StackHasLivingCavalry(EnemyStack);
			int rearChance = this.GetCavalryRearStrikeChance(Unit);
			int flankChance = this.GetCavalryFlankChance(Unit);
			int rearRoll = -1;
			int flankRoll = -1;
			bool rearStrike = false;
			bool flanked = false;
			if (!targetHasGuardian && !targetHasPikes)
			{
				if (!enemyHasLivingCavalry)
				{
					rearRoll = this.RNG.Next(100);
					rearStrike = rearRoll < rearChance;
				}
				if (!rearStrike)
				{
					flankRoll = this.RNG.Next(100);
					flanked = flankRoll < flankChance;
				}
			}
			this.AddBattleDebugLine("Cavalry charge special check: " + this.GetUnitDisplayName(Unit) + " vs " + this.GetUnitDisplayName(combatResults.Defender) + ", targetPikes=" + targetHasPikes + ", targetGuardian=" + targetHasGuardian + ", enemyHasCavalry=" + enemyHasLivingCavalry + ", rearChance=" + rearChance + "%, rearRoll=" + rearRoll + ", rearStrike=" + rearStrike + ", flankChance=" + flankChance + "%, flankRoll=" + flankRoll + ", flanked=" + flanked + ".");
			float cavDamageMod = this.GetCavalryChargeDamageModifier();
			int preChargeDefenderCasualties = combatResults.DefenderCasualties;
			int preChargeAttackerCasualties = combatResults.AttackerCasualties;
			if (targetHasPikes)
			{
				combatResults.DefenderCasualties = (int)(combatResults.DefenderCasualties * 0.5f);
				combatResults.AttackerCasualties = (int)(combatResults.AttackerCasualties * 1.5f);
			}
			else if (rearStrike)
			{
				combatResults.DefenderCasualties = (int)(combatResults.DefenderCasualties * 2f * cavDamageMod);
				combatResults.AttackerCasualties = (int)(combatResults.AttackerCasualties * 0.1f);
			}
			else if (flanked)
			{
				combatResults.DefenderCasualties = (int)(combatResults.DefenderCasualties * 1.5f * cavDamageMod);
				combatResults.AttackerCasualties = (int)(combatResults.AttackerCasualties * 0.5f);
			}
			else
			{
				combatResults.DefenderCasualties = (int)(combatResults.DefenderCasualties * cavDamageMod);
			}
			this.AddBattleDebugLine("Cavalry charge damage modifiers: raw dealt=" + preChargeDefenderCasualties + ", raw received=" + preChargeAttackerCasualties + ", cavDamageMod=" + this.FormatFloat(cavDamageMod) + ", pikes=" + targetHasPikes + ", rearStrike=" + rearStrike + ", flank=" + flanked + " => dealt=" + combatResults.DefenderCasualties + ", received=" + combatResults.AttackerCasualties + ".");

			if (this.PlayerWatching)
			{
				if (combatResults.AttackerCasualties > 0)
				{
					this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Defender.MeleeSound + ".wav");
				}
				if (combatResults.DefenderCasualties > 0)
				{
					this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Attacker.MeleeSound + ".wav");
				}
			}

			this.ApplyTerrainModifiers(combatResults);
			combatResults.ApplyDamage();
			if (targetHasPikes)
			{
				this.AddBattleLogLine(this.GetUnitDisplayName(Unit) + " charges " + this.GetUnitDisplayName(combatResults.Defender) + " but is impaled on pikes! (" + combatResults.DefenderCasualties + " dealt, " + combatResults.AttackerCasualties + " received).");
				if (combatResults.Defender.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Defender) + " is destroyed.");
				}
				if (combatResults.Attacker.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Attacker) + " is destroyed.");
				}
			}
			else if (targetHasGuardian)
			{
				this.AddBattleLogLine(this.GetUnitDisplayName(Unit) + " charges " + this.GetUnitDisplayName(combatResults.Defender) + " but the line holds! (" + combatResults.DefenderCasualties + " dealt, " + combatResults.AttackerCasualties + " received).");
				if (combatResults.Defender.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Defender) + " is destroyed.");
				}
				if (combatResults.Attacker.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Attacker) + " is destroyed.");
				}
			}
			else if (rearStrike)
			{
				string rearAttackType = combatResults.DefenderCasualties > 50 ? "devastates the rear of" : "strikes the rear of";
				this.AddBattleLogLine(this.GetUnitDisplayName(Unit) + " " + rearAttackType + " " + this.GetUnitDisplayName(combatResults.Defender) + "! (" + combatResults.DefenderCasualties + " dealt, " + combatResults.AttackerCasualties + " received).");
				if (combatResults.Defender.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Defender) + " is destroyed.");
				}
				if (combatResults.Attacker.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Attacker) + " is destroyed.");
				}
			}
			else if (flanked)
			{
				string flankAttackType = combatResults.DefenderCasualties > 50 ? "devastates the flank of" : "attacks the flank of";
				this.AddBattleLogLine(this.GetUnitDisplayName(Unit) + " " + flankAttackType + " " + this.GetUnitDisplayName(combatResults.Defender) + "! (" + combatResults.DefenderCasualties + " dealt, " + combatResults.AttackerCasualties + " received).");
				if (combatResults.Defender.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Defender) + " is destroyed.");
				}
				if (combatResults.Attacker.Health <= 0)
				{
					this.AddBattleLogLine(this.GetUnitDisplayName(combatResults.Attacker) + " is destroyed.");
				}
			}
			else
			{
				this.AddCombatLogLine(combatResults, false);
			}

			if (this.PlayerWatching)
			{
				this.Game.GameCore.FireEvent("AutoBattleFight", new object[] { this, combatResults });
			}
			this.UpdateAfterCombat(combatResults.Attacker, combatResults.Defender, false);
			this.NextActionTimer = 0.5f;
			this.CheckForWinner();
			return true;
		}

		private void EndBattle(WorkingStack WinnerStack)
		{
			if (this.BattleEnded)
			{
				return;
			}
			this.Winner = WinnerStack;
			this.BattleEnded = true;
			this.AddBattleLogLine("Battle ended: " + this.GetStackDisplayName(WinnerStack) + " is victorious.");
			if (this.Node.Province != null && this.Winner == this.Attacker)
			{
				float num = (float)this.Node.Province.CurrentLoot * 0.4f;
				this.Winner.Owner.Gold.Value += (int)num;
				this.Node.Province.CurrentLoot -= (int)num;
			}
			this.SaveBattleLogToFile();
			this.Game.GameCore.FireEvent("AutoBattleEnded", new object[] { this, WinnerStack });
			if (!this.PlayerWatching)
			{
				this.Game.GameCore.FireEvent("BattleCompleted", new object[0]);
			}
		}

		private void DoPhase7Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.PhaseStarted = true;
				this.UnitList1 = this.GetMeleeList();
				this.UnitList1Index = 0;
				this.NextActionTimer = 0.5f;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					this.StartPhase(8);
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				if (workingUnit.BattleData != null)
				{
					WorkingStack workingStack = this.Attacker;
					if (workingUnit.OwnerStack == this.Attacker)
					{
						workingStack = this.Defender;
					}
					flag = this.PerformOpportunityAttack(workingUnit, workingStack, false);
				}
				this.UnitList1Index++;
			}
		}

		private void DoPhase6Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.PhaseStarted = true;
				this.UnitList1 = this.GetMeleeList();
				this.UnitList1Index = 0;
				this.NextActionTimer = 0.5f;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					this.StartPhase(7);
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				if (workingUnit.BattleData != null)
				{
					WorkingStack workingStack = this.Attacker;
					if (workingUnit.OwnerStack == this.Attacker)
					{
						workingStack = this.Defender;
					}
					flag = this.PerformOpportunityAttack(workingUnit, workingStack, false);
				}
				this.UnitList1Index++;
			}
		}

		private void DoPhase5Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.PhaseStarted = true;
				this.AssaultPhaseLineup = true;
				this.AssaultPhaseShoot = false;
				this.AssaultPhaseMelee = false;
				this.AssaultRow = 0;
				this.AssaultCol = 5;
				this.AssaultDirection = 1;
			}
			if (this.AssaultPhaseLineup)
			{
				WorkingUnit workingUnit = this.AttackerPositions[this.AssaultRow, this.AssaultCol];
				WorkingUnit workingUnit2 = this.DefenderPositions[this.AssaultRow, this.AssaultCol];
				while (this.UnitUnableToFight(workingUnit) || this.UnitUnableToFight(workingUnit2))
				{
					if (this.AssaultDirection % 2 == 0)
					{
						this.AssaultCol += this.AssaultDirection;
					}
					else
					{
						this.AssaultCol -= this.AssaultDirection;
					}
					this.AssaultDirection++;
					if (this.AssaultCol == 10)
					{
						workingUnit = null;
						workingUnit2 = null;
						break;
					}
					workingUnit = this.AttackerPositions[this.AssaultRow, this.AssaultCol];
					workingUnit2 = this.DefenderPositions[this.AssaultRow, this.AssaultCol];
				}
				if (workingUnit != null && workingUnit2 != null)
				{
					CombatResults combatResults = CombatManager.PerformCombat(workingUnit2, workingUnit, CombatType.Simulated, false, workingUnit.BattleData.CanFight, false);
					if (this.PlayerWatching)
					{
						if (combatResults.AttackerCasualties > 0)
						{
							this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Defender.MeleeSound + ".wav");
						}
						if (combatResults.DefenderCasualties > 0)
						{
							this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Attacker.MeleeSound + ".wav");
						}
					}
					this.ApplyTerrainModifiers(combatResults);
					combatResults.ApplyDamage();
					this.AddCombatLogLine(combatResults, false);
					workingUnit2.BattleData.CanFight = false;
					if (this.PlayerWatching)
					{
						this.Game.GameCore.FireEvent("AutoBattleFight", new object[] { this, combatResults });
					}
					this.UpdateAfterCombat(workingUnit, workingUnit2, false);
					this.NextActionTimer = 0.5f;
					this.CheckForWinner();
					return;
				}
				if (this.AssaultRow == 0)
				{
					this.AssaultRow = 1;
					this.AssaultCol = 5;
					this.AssaultDirection = 1;
					this.NextActionTimer = 0.5f;
					return;
				}
				this.AssaultPhaseLineup = false;
				this.AssaultPhaseShoot = true;
				this.UnitList1 = this.GetArcherList(2);
				this.UnitList1Index = 0;
				this.NextActionTimer = 0.5f;
				return;
			}
			else
			{
				if (this.AssaultPhaseShoot)
				{
					bool flag = false;
					while (!flag)
					{
						if (this.UnitList1Index >= this.UnitList1.Count)
						{
							this.AssaultPhaseShoot = false;
							this.AssaultPhaseMelee = true;
							this.UnitList1 = this.GetMeleeList();
							this.UnitList1Index = 0;
							this.NextActionTimer = 0.5f;
							return;
						}
						WorkingUnit workingUnit3 = this.UnitList1[this.UnitList1Index];
						if (workingUnit3.BattleData != null)
						{
							WorkingStack workingStack = this.Attacker;
							if (workingUnit3.OwnerStack == this.Attacker)
							{
								workingStack = this.Defender;
							}
							flag = this.PerformOpportunityAttack(workingUnit3, workingStack, true);
						}
						this.UnitList1Index++;
					}
					return;
				}
				if (this.AssaultPhaseMelee)
				{
					bool flag2 = false;
					while (!flag2)
					{
						if (this.UnitList1Index >= this.UnitList1.Count)
						{
							if (this.PlayerWatching)
							{
								this.Game.GameCore.FireEvent("AutoBattleStageOver", new object[] { 2, this });
							}
							if (this.GetPlayerStack() == null)
							{
								this.StartPhase(6);
							}
							return;
						}
						WorkingUnit workingUnit4 = this.UnitList1[this.UnitList1Index];
						if (workingUnit4.BattleData != null)
						{
							WorkingStack workingStack2 = this.Attacker;
							if (workingUnit4.OwnerStack == this.Attacker)
							{
								workingStack2 = this.Defender;
							}
							flag2 = this.PerformOpportunityAttack(workingUnit4, workingStack2, false);
						}
						this.UnitList1Index++;
					}
				}
				return;
			}
		}

		private void DoPhase4Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.PhaseStarted = true;
				this.AssaultPhaseLineup = true;
				this.AssaultPhaseShoot = false;
				this.AssaultRow = 0;
				this.AssaultCol = 5;
				this.AssaultDirection = 1;
			}
			if (!this.AssaultPhaseLineup)
			{
				if (this.AssaultPhaseShoot)
				{
					bool flag = false;
					while (!flag)
					{
						if (this.UnitList1Index >= this.UnitList1.Count)
						{
							this.StartPhase(5);
							return;
						}
						WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
						WorkingStack workingStack = this.Attacker;
						if (workingUnit.OwnerStack == this.Attacker)
						{
							workingStack = this.Defender;
						}
						flag = this.PerformOpportunityAttack(workingUnit, workingStack, true);
						this.UnitList1Index++;
					}
				}
				return;
			}
			WorkingUnit workingUnit2 = this.AttackerPositions[this.AssaultRow, this.AssaultCol];
			WorkingUnit workingUnit3 = this.DefenderPositions[this.AssaultRow, this.AssaultCol];
			while (this.UnitUnableToFight(workingUnit2) || this.UnitUnableToFight(workingUnit3))
			{
				if (this.AssaultDirection % 2 == 0)
				{
					this.AssaultCol += this.AssaultDirection;
				}
				else
				{
					this.AssaultCol -= this.AssaultDirection;
				}
				this.AssaultDirection++;
				if (this.AssaultCol == 10)
				{
					workingUnit2 = null;
					workingUnit3 = null;
					break;
				}
				workingUnit2 = this.AttackerPositions[this.AssaultRow, this.AssaultCol];
				workingUnit3 = this.DefenderPositions[this.AssaultRow, this.AssaultCol];
			}
			if (workingUnit2 != null && workingUnit3 != null)
			{
				CombatResults combatResults = CombatManager.PerformCombat(workingUnit3, workingUnit2, CombatType.Simulated, false, workingUnit2.BattleData.CanFight, false);
				if (this.PlayerWatching)
				{
					if (combatResults.AttackerCasualties > 0)
					{
						this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Defender.MeleeSound + ".wav");
					}
					if (combatResults.DefenderCasualties > 0)
					{
						this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Attacker.MeleeSound + ".wav");
					}
				}
				this.ApplyTerrainModifiers(combatResults);
				combatResults.ApplyDamage();
				this.AddCombatLogLine(combatResults, false);
				workingUnit3.BattleData.CanFight = false;
				if (this.PlayerWatching)
				{
					this.Game.GameCore.FireEvent("AutoBattleFight", new object[] { this, combatResults });
				}
				this.UpdateAfterCombat(workingUnit2, workingUnit3, false);
				this.NextActionTimer = 0.5f;
				this.CheckForWinner();
				return;
			}
			if (this.AssaultRow == 0)
			{
				this.AssaultRow = 1;
				this.AssaultCol = 5;
				this.AssaultDirection = 1;
				this.NextActionTimer = 0.5f;
				return;
			}
			this.AssaultPhaseLineup = false;
			this.AssaultPhaseShoot = true;
			this.UnitList1 = this.GetArcherList(2);
			this.UnitList1Index = 0;
			this.NextActionTimer = 0.5f;
		}

		private bool UnitHasAlreadyCharged(WorkingUnit Unit)
		{
			return Unit != null && Unit.Class == UnitClasses.Cavalry && this.ChargedCavalryUnits != null && this.ChargedCavalryUnits.Contains(Unit);
		}

		private bool UnitUnableToFight(WorkingUnit Unit)
		{
			return Unit == null || Unit.Disabled || Unit.BattleData == null || !Unit.BattleData.CanFight || this.UnitHasAlreadyCharged(Unit);
		}

		private void DoPhase3Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.PhaseStarted = true;
				this.AssaultPhaseLineup = true;
				this.AssaultPhaseShoot = false;
				this.AssaultRow = 0;
				this.AssaultCol = 5;
				this.AssaultDirection = 1;
			}
			if (!this.AssaultPhaseLineup)
			{
				if (this.AssaultPhaseShoot)
				{
					bool flag = false;
					while (!flag)
					{
						if (this.UnitList1Index >= this.UnitList1.Count)
						{
							this.StartPhase(4);
							return;
						}
						WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
						WorkingStack workingStack = this.Attacker;
						if (workingUnit.OwnerStack == this.Attacker)
						{
							workingStack = this.Defender;
						}
						flag = this.PerformOpportunityAttack(workingUnit, workingStack, true);
						this.UnitList1Index++;
					}
				}
				return;
			}
			WorkingUnit workingUnit2 = this.AttackerPositions[this.AssaultRow, this.AssaultCol];
			WorkingUnit workingUnit3 = this.DefenderPositions[this.AssaultRow, this.AssaultCol];
			while (this.UnitUnableToFight(workingUnit2) || this.UnitUnableToFight(workingUnit3))
			{
				if (this.AssaultDirection % 2 == 0)
				{
					this.AssaultCol += this.AssaultDirection;
				}
				else
				{
					this.AssaultCol -= this.AssaultDirection;
				}
				this.AssaultDirection++;
				if (this.AssaultCol == 10)
				{
					workingUnit2 = null;
					workingUnit3 = null;
					break;
				}
				workingUnit2 = this.AttackerPositions[this.AssaultRow, this.AssaultCol];
				workingUnit3 = this.DefenderPositions[this.AssaultRow, this.AssaultCol];
			}
			if (workingUnit2 != null && workingUnit3 != null)
			{
				CombatResults combatResults = CombatManager.PerformCombat(workingUnit2, workingUnit3, CombatType.Simulated, false, workingUnit3.BattleData.CanFight, false);
				if (this.PlayerWatching)
				{
					if (combatResults.AttackerCasualties > 0)
					{
						this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Defender.MeleeSound + ".wav");
					}
					if (combatResults.DefenderCasualties > 0)
					{
						this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Attacker.MeleeSound + ".wav");
					}
				}
				this.ApplyTerrainModifiers(combatResults);
				combatResults.ApplyDamage();
				this.AddCombatLogLine(combatResults, false);
				workingUnit2.BattleData.CanFight = false;
				if (this.PlayerWatching)
				{
					this.Game.GameCore.FireEvent("AutoBattleFight", new object[] { this, combatResults });
				}
				this.UpdateAfterCombat(workingUnit2, workingUnit3, false);
				this.NextActionTimer = 0.5f;
				this.CheckForWinner();
				return;
			}
			this.AssaultPhaseLineup = false;
			this.AssaultPhaseShoot = true;
			this.UnitList1 = this.GetArcherList(2);
			this.UnitList1Index = 0;
			this.NextActionTimer = 0.5f;
		}

		private void DoPhase1Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.UnitList1 = this.GetArcherList(2);
				this.UnitList1Index = 0;
				this.PhaseStarted = true;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					this.StartPhase(2);
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				WorkingStack workingStack = this.Attacker;
				if (workingUnit.OwnerStack == this.Attacker)
				{
					workingStack = this.Defender;
				}
				flag = this.PerformOpportunityAttack(workingUnit, workingStack, true);
				this.UnitList1Index++;
			}
		}

		private void ResetcombatData()
		{
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				workingUnit.ResetBattleData();
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				workingUnit2.ResetBattleData();
			}
		}

		private void DoPhase2Action()
		{
			this.ResetcombatData();
			if (!this.PhaseStarted)
			{
				this.UnitList1 = this.GetArcherList(2);
				this.UnitList1Index = 0;
				this.PhaseStarted = true;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					if (this.PlayerWatching)
					{
						this.Game.GameCore.FireEvent("AutoBattleStageOver", new object[] { 1, this });
					}
					if (this.GetPlayerStack() == null)
					{
						if (this.HasCavalryAvailableForCharge() && this.IsCavalryChargeAllowed())
						{
							this.CavalryChargeStageUsed = true;
							this.StartPhase(10);
						}
						else
						{
							this.StartPhase(3);
						}
					}
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				WorkingStack workingStack = this.Attacker;
				if (workingUnit.OwnerStack == this.Attacker)
				{
					workingStack = this.Defender;
				}
				flag = this.PerformOpportunityAttack(workingUnit, workingStack, true);
				this.UnitList1Index++;
			}
		}

		private void DoOldPhase3Action()
		{
			if (!this.PhaseStarted)
			{
				this.ResetcombatData();
				this.UnitList1 = this.GetArcherList(2);
				this.UnitList1Index = 0;
				this.PhaseStarted = true;
			}
			bool flag = false;
			while (!flag)
			{
				if (this.UnitList1Index >= this.UnitList1.Count)
				{
					this.StartPhase(4);
					return;
				}
				WorkingUnit workingUnit = this.UnitList1[this.UnitList1Index];
				WorkingStack workingStack = this.Attacker;
				if (workingUnit.OwnerStack == this.Attacker)
				{
					workingStack = this.Defender;
				}
				flag = this.PerformOpportunityAttack(workingUnit, workingStack, true);
				this.UnitList1Index++;
			}
		}

		private bool PerformOpportunityAttack(WorkingUnit Unit, WorkingStack EnemyStack, bool UseRangedAttack)
		{
			if (Unit.Disabled)
			{
				return false;
			}
			if (Unit.HasStatus("Healing", new object[0]))
			{
				int num = Unit.HealRate + Unit.RangedAttack;
				if (this.RNG.Next(num) <= Unit.HealRate && this.PerformHeal(Unit))
				{
					return true;
				}
			}
			CombatResults combatResults = null;
			List<CombatResults> allResults = new List<CombatResults>();
			foreach (WorkingUnit workingUnit in EnemyStack.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData != null)
				{
					CombatResults combatResults2 = CombatManager.PerformCombat(Unit, workingUnit, CombatType.Simulated, UseRangedAttack, workingUnit.BattleData.CanFight, false);
					if (combatResults2 != null)
					{
						allResults.Add(combatResults2);
					}
				}
			}
			combatResults = this.SelectWeightedTarget(allResults, Unit);
			if (combatResults != null)
			{
				Unit.BattleData.CanFight = false;
				if (this.PlayerWatching)
				{
					if (combatResults.AttackerCasualties > 0)
					{
						if (UseRangedAttack)
						{
							this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Defender.RangedSound + ".wav");
						}
						else
						{
							this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Defender.MeleeSound + ".wav");
						}
					}
					if (combatResults.DefenderCasualties > 0)
					{
						if (UseRangedAttack)
						{
							this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Attacker.RangedSound + ".wav");
						}
						else
						{
							this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\" + combatResults.Attacker.MeleeSound + ".wav");
						}
					}
				}
				this.ApplyTerrainModifiers(combatResults);
				combatResults.ApplyDamage();
				this.AddCombatLogLine(combatResults, UseRangedAttack);
				if (this.PlayerWatching)
				{
					this.Game.GameCore.FireEvent("AutoBattleFight", new object[] { this, combatResults });
				}
				this.UpdateAfterCombat(combatResults.Attacker, combatResults.Defender, UseRangedAttack);
				this.NextActionTimer = 0.5f;
				this.CheckForWinner();
				return true;
			}
			return false;
		}

		private void UpdateAfterCombat(WorkingUnit Attacker, WorkingUnit Defender, bool Ranged)
		{
			int num = Attacker.Health;
			int num2 = Defender.Health;
			Attacker.CombatNotification("AfterAttacking", Defender, Ranged);
			Defender.CombatNotification("AfterAttacked", Attacker, Ranged);
			if (Attacker.Health != num)
			{
				this.Game.GameCore.FireEvent("AutoBattleUpdateUnit", new object[] { this, Attacker });
			}
			if (Defender.Health != num2)
			{
				this.Game.GameCore.FireEvent("AutoBattleUpdateUnit", new object[] { this, Defender });
			}
		}

		private bool PerformHeal(WorkingUnit Unit)
		{
			WorkingStack ownerStack = Unit.OwnerStack;
			int num = 0;
			WorkingUnit workingUnit = null;
			foreach (WorkingUnit workingUnit2 in ownerStack.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.Health <= 90 && workingUnit2.Race != Races.Undead && workingUnit2.Class != UnitClasses.Fort)
				{
					int num2 = 100 - workingUnit2.Health;
					if (num2 > num)
					{
						num = num2;
						workingUnit = workingUnit2;
					}
				}
			}
			if (workingUnit == null)
			{
				return false;
			}
			Unit.BattleData.CanFight = false;
			workingUnit.ApplyHealing(Unit.HealRate * 10, false, Unit);
			this.AddBattleLogLine(this.GetUnitDisplayName(Unit) + " heals " + this.GetUnitDisplayName(workingUnit) + " for " + Math.Min(Unit.HealRate * 10, num) + ".");
			if (this.PlayerWatching)
			{
				this.Game.GameCore.FireEvent("AutoBattleHeal", new object[]
				{
					this,
					Unit,
					workingUnit,
					Math.Min(Unit.HealRate * 10, num)
				});
			}
			return true;
		}

		private List<WorkingUnit> GetMeleeList()
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData.CanFight && workingUnit.Attack > 0 && !this.UnitHasAlreadyCharged(workingUnit))
				{
					list.Add(workingUnit);
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.BattleData.CanFight && workingUnit2.Attack > 0 && !this.UnitHasAlreadyCharged(workingUnit2))
				{
					list.Add(workingUnit2);
				}
			}
			list.Sort(new Comparison<WorkingUnit>(this.InitiativeComparer));
			return list;
		}

		private List<WorkingUnit> GetArcherList(int MinRange)
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData.CanFight && workingUnit.Range >= MinRange && workingUnit.RangedAttack > 0 && !this.UnitHasAlreadyCharged(workingUnit))
				{
					list.Add(workingUnit);
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.BattleData.CanFight && workingUnit2.Range >= MinRange && workingUnit2.RangedAttack > 0 && !this.UnitHasAlreadyCharged(workingUnit2))
				{
					list.Add(workingUnit2);
				}
			}
			list.Sort(new Comparison<WorkingUnit>(this.InitiativeComparer));
			return list;
		}

		private List<WorkingUnit> GetCavalryList()
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData.CanFight && workingUnit.Class == UnitClasses.Cavalry && workingUnit.Attack > 0 && !this.UnitHasAlreadyCharged(workingUnit))
				{
					list.Add(workingUnit);
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.BattleData.CanFight && workingUnit2.Class == UnitClasses.Cavalry && workingUnit2.Attack > 0 && !this.UnitHasAlreadyCharged(workingUnit2))
				{
					list.Add(workingUnit2);
				}
			}
			list.Sort(new Comparison<WorkingUnit>(this.InitiativeComparer));
			return list;
		}

		private bool HasCavalryAvailableForCharge()
		{
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				if (!workingUnit.Disabled && workingUnit.Class == UnitClasses.Cavalry && !this.UnitHasAlreadyCharged(workingUnit))
				{
					return true;
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.Class == UnitClasses.Cavalry && !this.UnitHasAlreadyCharged(workingUnit2))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasCavalryChargeStage()
		{
			return this.HasCavalryAvailableForCharge() && this.IsCavalryChargeAllowed();
		}

		public bool HasCavalry()
		{
			foreach (WorkingUnit workingUnit in this.Attacker.Units)
			{
				if (!workingUnit.Disabled && workingUnit.Class == UnitClasses.Cavalry)
				{
					return true;
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.Class == UnitClasses.Cavalry)
				{
					return true;
				}
			}
			return false;
		}


		private int InitiativeComparer(WorkingUnit A, WorkingUnit B)
		{
			return B.Initiative.CompareTo(A.Initiative);
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

		public void ContinueFight()
		{
			if (this.CurrentPhase == 0)
			{
				this.AddBattleLogLine("--- Stage 1: Ranged exchange ---");
				this.StartPhase(1);
				return;
			}
			if (this.CurrentPhase == 2)
			{
				if (this.HasCavalryAvailableForCharge() && this.IsCavalryChargeAllowed())
				{
					this.CavalryChargeStageUsed = true;
					this.AddBattleLogLine("--- Stage 2: Cavalry charge ---");
					this.StartPhase(10);
				}
				else
				{
					this.AddBattleLogLine("--- Stage 2: Main assault ---");
					this.StartPhase(3);
				}
				return;
			}
			if (this.CurrentPhase == 10)
			{
				this.AddBattleLogLine("--- Stage 3: Main assault ---");
				this.StartPhase(3);
				return;
			}
			if (this.CurrentPhase == 5)
			{
				this.AddBattleLogLine("--- Stage " + (this.CavalryChargeStageUsed ? "4" : "3") + ": Final melee ---");
				this.StartPhase(6);
				return;
			}
		}

		public void Retreat(WorkingStack Stack)
		{
			this.Game.GameCore.FireEvent("ArmyRetreated", new object[]
			{
				Stack,
				Stack == this.Defender
			});
			if (Stack == this.Attacker)
			{
				this.EndBattle(this.Defender);
			}
			if (Stack == this.Defender)
			{
				this.EndBattle(this.Attacker);
			}
		}

		public void CheckForWinner()
		{
			if (this.BattleEnded)
			{
				return;
			}
			if (this.Attacker.Units.Count((WorkingUnit x) => !x.Disabled) == 0)
			{
				this.EndBattle(this.Defender);
				return;
			}
			if (this.Defender.Units.Count((WorkingUnit x) => !x.Disabled && x.Class != UnitClasses.Fort) == 0)
			{
				this.EndBattle(this.Attacker);
			}
		}

		public WorkingRealm GetWinnerRealm()
		{
			if (this.Winner == this.Attacker)
			{
				return this.Attacker.Owner;
			}
			return this.Defender.Owner;
		}

		public WorkingRealm GetLoserRealm()
		{
			if (this.Winner == this.Attacker)
			{
				return this.Defender.Owner;
			}
			return this.Attacker.Owner;
		}

		public void Cleanup()
		{
			this.Game.CleanupAutoBattle(this);
		}

		public WorkingStack GetLoserStack()
		{
			if (this.Winner == this.Attacker)
			{
				return this.Defender;
			}
			return this.Attacker;
		}

		public WorkingStack GetWinnerStack()
		{
			return this.Winner;
		}

		internal void Dispose()
		{
			this.Attacker = null;
			this.Defender = null;
			this.Node = null;
			this.BattleLog = null;
			this.DetailedBattleLog = null;
			this.ChargedCavalryUnits = null;
		}

		public void CaptureUnit(WorkingUnit Unit)
		{
			if (Unit.OwnerStack == this.Attacker)
			{
				this.Attacker.RemoveUnit(Unit);
				Unit.IsPrisoner = true;
				Unit.ClearBattleData();
				this.CapturedattackerUnits.Add(Unit);
				this.Game.GameCore.FireEvent("AutoCapture", new object[] { Unit, true });
			}
			else
			{
				this.Defender.RemoveUnit(Unit);
				Unit.IsPrisoner = true;
				Unit.ClearBattleData();
				this.CapturedDefenderUnits.Add(Unit);
				this.Game.GameCore.FireEvent("AutoCapture", new object[] { Unit, false });
			}
			this.CheckForWinner();
		}

		public bool RealmPresent(string RealmName)
		{
			return this.Attacker.Owner.Name == RealmName || this.Defender.Owner.Name == RealmName;
		}

		private SovereigntyGame Game;

		public WorkingUnit[,] AttackerPositions;

		public WorkingUnit[,] DefenderPositions;

		public WorkingStack Attacker;

		public WorkingStack Defender;

		public ActivePathNode Node;

		public ActivePathNode AttackerNode;

		public WorkingStack Winner;

		public int CurrentPhase;

		public bool PhaseStarted;

		private float NextActionTimer;

		private List<WorkingUnit> UnitList1;

		private int UnitList1Index;

		private bool AssaultPhaseLineup;

		private bool AssaultPhaseShoot;

		private bool AssaultPhaseMelee;

		private int AssaultRow;

		private int AssaultCol;

		private int AssaultDirection;

		private int ActionCounter;

		private bool BattleEnded;

		public bool NoPauses;

		public bool PlayerWatching;

		public bool RiverCrossing;

		public int InitialAttackers;

		public int InitialDefenders;

		public List<string> BattleLog;

		private List<string> DetailedBattleLog;

		private List<WorkingUnit> ChargedCavalryUnits;

		private bool CavalryChargeStageUsed;

		public List<WorkingUnit> CapturedattackerUnits;

		public List<WorkingUnit> CapturedDefenderUnits;

		private Random RNG;

		private List<WorkingUnit> TransportedUnits;
	}
}
