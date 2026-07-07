using System;
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
			this.Game.GameCore.FireEvent("AutoBattleStart", new object[] { this });
			if (this.GetPlayerStack() == null)
			{
				this.StartPhase(1);
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

		private void EndBattle(WorkingStack WinnerStack)
		{
			if (this.BattleEnded)
			{
				return;
			}
			this.Winner = WinnerStack;
			this.BattleEnded = true;
			if (this.Node.Province != null && this.Winner == this.Attacker)
			{
				float num = (float)this.Node.Province.CurrentLoot * 0.4f;
				this.Winner.Owner.Gold.Value += (int)num;
				this.Node.Province.CurrentLoot -= (int)num;
			}
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
					combatResults.ApplyDamage();
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
				combatResults.ApplyDamage();
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

		private bool UnitUnableToFight(WorkingUnit Unit)
		{
			return Unit == null || Unit.Disabled || Unit.BattleData == null || !Unit.BattleData.CanFight;
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
				combatResults.ApplyDamage();
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
						this.StartPhase(3);
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
			int num2 = 0;
			foreach (WorkingUnit workingUnit in EnemyStack.Units)
			{
				if (!workingUnit.Disabled && workingUnit.BattleData != null)
				{
					CombatResults combatResults2 = CombatManager.PerformCombat(Unit, workingUnit, CombatType.Simulated, UseRangedAttack, workingUnit.BattleData.CanFight, false);
					if (combatResults2 != null && combatResults2.DefenderCasualties - combatResults2.AttackerCasualties > num2)
					{
						num2 = combatResults2.DefenderCasualties - combatResults2.AttackerCasualties;
						combatResults = combatResults2;
					}
				}
			}
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
				combatResults.ApplyDamage();
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
				if (!workingUnit.Disabled && workingUnit.BattleData.CanFight && workingUnit.Attack > 0)
				{
					list.Add(workingUnit);
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.BattleData.CanFight && workingUnit2.Attack > 0)
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
				if (!workingUnit.Disabled && workingUnit.BattleData.CanFight && workingUnit.Range >= MinRange && workingUnit.RangedAttack > 0)
				{
					list.Add(workingUnit);
				}
			}
			foreach (WorkingUnit workingUnit2 in this.Defender.Units)
			{
				if (!workingUnit2.Disabled && workingUnit2.BattleData.CanFight && workingUnit2.Range >= MinRange && workingUnit2.RangedAttack > 0)
				{
					list.Add(workingUnit2);
				}
			}
			list.Sort(new Comparison<WorkingUnit>(this.InitiativeComparer));
			return list;
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
				this.StartPhase(1);
			}
			if (this.CurrentPhase == 2)
			{
				this.StartPhase(3);
			}
			if (this.CurrentPhase == 5)
			{
				this.StartPhase(6);
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

		public List<WorkingUnit> CapturedattackerUnits;

		public List<WorkingUnit> CapturedDefenderUnits;

		private Random RNG;

		private List<WorkingUnit> TransportedUnits;
	}
}
