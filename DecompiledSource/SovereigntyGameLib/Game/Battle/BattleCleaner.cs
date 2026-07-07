using System;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Battle
{
	public class BattleCleaner
	{
		public BattleCleaner(SovereigntyGame Game, AutoBattleController Battle)
		{
			this.Battle = Battle;
			this.Game = Game;
		}

		public BattleCleaner(SovereigntyGame Game, TacticalBattleController Battle)
		{
			this.TacticalBattle = Battle;
			this.Game = Game;
		}

		private void CleanupTacticalBattle(TacticalBattleController Battle)
		{
			BattleCleanupData battleCleanupData = new BattleCleanupData();
			battleCleanupData.SetData(Battle);
			this.Game.GameCore.FireEvent("TickerMessage", new object[]
			{
				new TickerMessage(GameText.CreateFromLiteral(battleCleanupData.WinnerName + " defeats " + battleCleanupData.LoserName), TickerMessageType.Default, 1)
			});
			Battle.DisposeDeadUnits();
			Battle.RemoveFortification();
			battleCleanupData.Winner.AwardHeroXP(10);
			battleCleanupData.Loser.AwardHeroXP(1);
			this.Game.GameCore.FireEvent("BattleEnded", new object[]
			{
				battleCleanupData.WinnerName,
				battleCleanupData.LoserName,
				battleCleanupData.Winner == battleCleanupData.Attacker
			});
			this.CheckBattlefieldStatus(battleCleanupData);
			this.CheckAIOccupation(battleCleanupData);
			this.CleanupBattleUnits(battleCleanupData);
			this.KillRebelUnits(battleCleanupData);
			this.RetreatRemainingUnits(battleCleanupData);
			this.TransferPrisoners(battleCleanupData);
			this.RetreatHarbourStack(battleCleanupData);
			this.MoveWinnerForward(battleCleanupData);
			battleCleanupData.Winner.ClearDeadUnits();
			Battle.Dispose();
		}

		private void CleanupAutoBattle(AutoBattleController Battle)
		{
			BattleCleanupData battleCleanupData = new BattleCleanupData();
			battleCleanupData.SetData(Battle);
			this.Game.GameCore.FireEvent("TickerMessage", new object[]
			{
				new TickerMessage(GameText.CreateFromLiteral(battleCleanupData.WinnerName + " defeats " + battleCleanupData.LoserName), TickerMessageType.Default, 1)
			});
			battleCleanupData.Winner.AwardHeroXP(10);
			battleCleanupData.Loser.AwardHeroXP(1);
			this.Game.GameCore.FireEvent("BattleEnded", new object[]
			{
				battleCleanupData.WinnerName,
				battleCleanupData.LoserName,
				battleCleanupData.Winner == battleCleanupData.Attacker
			});
			this.CheckBattlefieldStatus(battleCleanupData);
			this.CheckAIOccupation(battleCleanupData);
			this.CleanupBattleUnits(battleCleanupData);
			this.KillRebelUnits(battleCleanupData);
			this.RetreatRemainingUnits(battleCleanupData);
			this.TransferPrisoners(battleCleanupData);
			this.RetreatHarbourStack(battleCleanupData);
			this.MoveWinnerForward(battleCleanupData);
			battleCleanupData.Winner.ClearDeadUnits();
		}

		private void MoveWinnerForward(BattleCleanupData Data)
		{
			if (Data.Attacker == Data.Winner)
			{
				Data.Node.CurrentStackID = Data.Winner.ID;
				if (Data.Attacker.Owner == this.Game.PlayerRealm && Data.Node.Province != null)
				{
					this.Game.GameCore.FireEvent("PlayerOccupiedProvince", new object[] { Data.Node.Province });
				}
				if (Data.Node.Province != null)
				{
					Data.Attacker.UnpackTransports();
					this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { Data.Node.Province });
					this.CheckForLiberation(Data.Node.Province);
				}
			}
		}

		private void RetreatHarbourStack(BattleCleanupData Data)
		{
			if (Data.Defender == Data.Loser && Data.HarbourStack != null && Data.HarbourStack.Owner != Data.Winner.Owner)
			{
				RetreatManager retreatManager = new RetreatManager(this.Game, Data.HarbourStack);
				retreatManager.Retreat(Data.Winner.Owner, null);
				this.Game.DestroyStack(Data.HarbourStack);
			}
		}

		private void TransferPrisoners(BattleCleanupData Data)
		{
			foreach (WorkingUnit workingUnit in Data.CapturedAttackerUnits)
			{
				Data.DefenderRealm.Prison.CaptureUnit(workingUnit);
			}
			foreach (WorkingUnit workingUnit2 in Data.CapturedDefenderUnits)
			{
				Data.AttackerRealm.Prison.CaptureUnit(workingUnit2);
			}
		}

		private void RetreatRemainingUnits(BattleCleanupData Data)
		{
			if (Data.Winner == null)
			{
				throw new Exception("Winner was null");
			}
			if (Data.CapturedAttackerUnits == null)
			{
				throw new Exception("capped attackers was null");
			}
			if (Data.CapturedDefenderUnits == null)
			{
				throw new Exception("capped defenders was null");
			}
			if (Data.Attacker == null)
			{
				throw new Exception("attacker was null");
			}
			if (Data.Defender == null)
			{
				throw new Exception("defender was null");
			}
			if (this.Game == null)
			{
				throw new Exception("Game was null");
			}
			RetreatManager retreatManager = new RetreatManager(this.Game, Data.Loser);
			int num;
			if (Data.Winner == Data.Attacker)
			{
				num = retreatManager.Retreat(Data.Winner.Owner, null);
			}
			else
			{
				num = retreatManager.Retreat(Data.Winner.Owner, Data.AttackerNode);
			}
			this.Game.DestroyStack(Data.Loser);
			if (num == 0)
			{
				if (Data.Loser == Data.Attacker)
				{
					foreach (WorkingUnit workingUnit in Data.CapturedDefenderUnits)
					{
						if (workingUnit != null)
						{
							workingUnit.IsPrisoner = false;
							Data.Defender.AddUnit(workingUnit, false, false);
						}
					}
					Data.CapturedDefenderUnits.Clear();
					return;
				}
				foreach (WorkingUnit workingUnit2 in Data.CapturedAttackerUnits)
				{
					if (workingUnit2 != null)
					{
						workingUnit2.IsPrisoner = false;
						Data.Attacker.AddUnit(workingUnit2, false, false);
					}
				}
				Data.CapturedAttackerUnits.Clear();
			}
		}

		private void KillRebelUnits(BattleCleanupData Data)
		{
			if (Data.Attacker == Data.Loser && Data.Attacker.Owner == this.Game.RebelRealm)
			{
				foreach (WorkingUnit workingUnit in Data.Loser.Units.ToList<WorkingUnit>())
				{
					Data.Loser.RemoveUnit(workingUnit);
					this.Game.DestroyUnit(workingUnit);
				}
			}
		}

		private void CheckAIOccupation(BattleCleanupData Data)
		{
			if (Data.Defender == Data.Loser && Data.Defender.Owner == this.Game.PlayerRealm && Data.Defender.Node.Province != null && Data.Defender.Node.Province.OwnerRealm == this.Game.PlayerRealm)
			{
				this.Game.GameCore.FireEvent("AIOccupied", new object[] { Data.Defender.Node.Province.ID });
			}
		}

		private void CheckBattlefieldStatus(BattleCleanupData Data)
		{
			if (Data.WinnerCount >= 10 && Data.LoserCount >= 10)
			{
				float num = (float)(Data.Winner.Units.Count((WorkingUnit x) => !x.Disabled) + Data.Loser.Units.Count((WorkingUnit x) => !x.Disabled));
				float num2 = (float)(Data.WinnerCount + Data.LoserCount);
				int num3 = Data.WinnerCount - Data.Winner.Units.Count((WorkingUnit x) => !x.Disabled);
				int num4 = Data.LoserCount - Data.Loser.Units.Count((WorkingUnit x) => !x.Disabled);
				if ((double)(num / num2) < 0.5 && Data.Defender.Node.Province != null && Data.Defender.Node.Province.BattleField == null)
				{
					BattleFieldData battleFieldData = new BattleFieldData(Data.Defender.Node.Province, this.Game);
					battleFieldData.SetData(Data.Defender.Node.Province, Data.Winner.Owner, Data.Loser.Owner, num3, num4, Data.Defender == Data.Winner);
					Data.Defender.Node.Province.BattleField = battleFieldData;
					this.Game.GameCore.FireEvent("BattlefieldCreated", new object[] { battleFieldData });
					Data.Winner.AwardHeroXP(65);
					Data.Loser.AwardHeroXP(65);
				}
			}
		}

		private void CleanupBattleUnits(BattleCleanupData Data)
		{
			Data.Attacker.Owner.EndBattle();
			Data.Defender.Owner.EndBattle();
			foreach (WorkingUnit workingUnit in Data.Attacker.Units)
			{
				workingUnit.ClearBattleData();
			}
			foreach (WorkingUnit workingUnit2 in Data.Defender.Units)
			{
				workingUnit2.ClearBattleData();
			}
			foreach (WorkingUnit workingUnit3 in Data.Defender.Units.ToList<WorkingUnit>())
			{
				if (workingUnit3.Class == UnitClasses.Fort)
				{
					Data.Defender.RemoveUnit(workingUnit3);
				}
				if (workingUnit3.TempUnit)
				{
					Data.Defender.RemoveUnit(workingUnit3);
					this.Game.DestroyUnit(workingUnit3);
				}
			}
			foreach (WorkingUnit workingUnit4 in Data.Attacker.Units.ToList<WorkingUnit>())
			{
				if (workingUnit4.TempUnit)
				{
					Data.Attacker.RemoveUnit(workingUnit4);
					this.Game.DestroyUnit(workingUnit4);
				}
			}
		}

		internal void Cleanup()
		{
			if (this.Battle != null)
			{
				this.CleanupAutoBattle(this.Battle);
			}
			if (this.TacticalBattle != null)
			{
				this.CleanupTacticalBattle(this.TacticalBattle);
			}
		}

		private void CheckForLiberation(WorkingProvince Province)
		{
			if (!Province.Occupied)
			{
				return;
			}
			if (Province.OccupierRealm.DiplomacyManager.GetRelation(Province.OwnerRealm) == RelationStates.War)
			{
				return;
			}
			WorkingRealm occupierRealm = Province.OccupierRealm;
			if (Province.OccupierRealm.DiplomacyManager.GetRelation(Province.OwnerRealm) == RelationStates.ForcedPeace)
			{
				if (Province.OccupierRealm.AIPlayer == null)
				{
					MessageBoxData messageBoxData = new MessageBoxData();
					messageBoxData.CaptionText = GameText.CreateLocalised("LIBERATEFORCE_TITLE", new object[0]);
					messageBoxData.MessageText = GameText.CreateLocalised("LIBERATEFORCE_TEXT", new object[0]);
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Province.DisplayName, new object[0]));
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Province.OwnerRealm.DisplayName, new object[0]));
					messageBoxData.MessageText.AddChildText(this.Game.AllianceController.GetTreatyName(Province.OccupierRealm.DiplomacyManager.GetRelation(Province.OwnerRealm)));
					messageBoxData.DisplayType = MessageBoxType.Info;
				}
				this.Game.WithdrawStack(Province.LandNode.CurrentStack);
				Province.OwnerRealm.DiplomacyManager.TriggerEvent(occupierRealm, "LiberatedAlly");
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { Province });
				if (Province.OwnerRealm.AIPlayer == null)
				{
					GameText gameText = GameText.CreateLocalised("MSG_AILIBERATE_TITLE", new object[0]);
					GameText gameText2 = GameText.CreateLocalised("MSG_AILIBERATE_TEXT", new object[0]);
					gameText2.AddChildText(GameText.CreateLocalised(Province.OwnerRealm.DisplayName, new object[0]));
					gameText2.AddChildText(GameText.CreateLocalised(Province.DisplayName, new object[0]));
					this.Game.GameCore.MessageHandler.ShowInfoMessage(gameText, gameText2);
				}
				return;
			}
			if (Province.OccupierRealm.AIPlayer == null)
			{
				MessageBoxData messageBoxData2 = new MessageBoxData();
				messageBoxData2.CaptionText = GameText.CreateLocalised("LIBERATE_TITLE", new object[0]);
				messageBoxData2.MessageText = GameText.CreateLocalised("LIBERATE_TEXT", new object[0]);
				messageBoxData2.MessageText.AddChildText(GameText.CreateLocalised(Province.DisplayName, new object[0]));
				messageBoxData2.MessageText.AddChildText(GameText.CreateLocalised(Province.OwnerRealm.DisplayName, new object[0]));
				messageBoxData2.MessageText.AddChildText(this.Game.AllianceController.GetTreatyName(Province.OccupierRealm.DiplomacyManager.GetRelation(Province.OwnerRealm)));
				messageBoxData2.YesText = GameText.CreateLocalised("RETURNTEXT", new object[0]);
				messageBoxData2.NoText = GameText.CreateLocalised("KEEPTEXT", new object[0]);
				messageBoxData2.DisplayType = MessageBoxType.YesNo;
				messageBoxData2.MsgType = MessageType.Liberate;
				messageBoxData2.Province = Province;
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData2);
				return;
			}
			Province.OccupierRealm.AIPlayer.RelationsManager.ConsiderLiberation(Province);
		}

		private AutoBattleController Battle;

		private TacticalBattleController TacticalBattle;

		private SovereigntyGame Game;
	}
}
