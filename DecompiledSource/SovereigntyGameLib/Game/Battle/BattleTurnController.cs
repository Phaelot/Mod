using System;
using System.IO;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Battle
{
	public class BattleTurnController
	{
		public int CurrentPlayerID
		{
			get
			{
				if (this.AttackerTurn)
				{
					return this.Battle.Attacker.OwnerID;
				}
				return this.Battle.Defender.OwnerID;
			}
		}

		public BattleTurnController(TacticalBattleController Battle)
		{
			this.Battle = Battle;
			this.DefenderTurnsLeft = Battle.GetMaxTurns();
			this.AttackerTurn = false;
		}

		public BattleTurnController(TacticalBattleController Battle, BinaryReader r)
		{
			this.Battle = Battle;
			this.AttackerTurnsLeft = r.ReadInt32();
			this.DefenderTurnsLeft = r.ReadInt32();
			this.AttackerSkipCount = r.ReadInt32();
			this.DefenderSkipCount = r.ReadInt32();
			this.CurrentTurn = r.ReadInt32();
			this.AttackerTurn = r.ReadBoolean();
			this.TimerEnabled = r.ReadBoolean();
			this.AttackerIsWinning = r.ReadBoolean();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.AttackerTurnsLeft);
			w.Write(this.DefenderTurnsLeft);
			w.Write(this.AttackerSkipCount);
			w.Write(this.DefenderSkipCount);
			w.Write(this.CurrentTurn);
			w.Write(this.AttackerTurn);
			w.Write(this.TimerEnabled);
			w.Write(this.AttackerIsWinning);
		}

		public int GetWinnerTurns()
		{
			if (this.AttackerIsWinning)
			{
				return this.AttackerTurnsLeft;
			}
			return this.DefenderTurnsLeft;
		}

		public WorkingRealm GetWinningRealm()
		{
			if (this.AttackerIsWinning)
			{
				return this.Battle.AttackerRealm;
			}
			return this.Battle.DefenderRealm;
		}

		public void SkipTurn(WorkingRealm Realm)
		{
			if (Realm == this.Battle.AttackerRealm)
			{
				this.AttackerSkipCount++;
				return;
			}
			this.DefenderSkipCount++;
		}

		public void IncreaseDefenderTurns(int Count)
		{
			this.DefenderTurnsLeft += Count;
			this.Battle.Game.GameCore.FireEvent("BattleTurnChanged", new object[0]);
		}

		public void DecreaseDefenderTurns(int Count)
		{
			this.DefenderTurnsLeft -= Count;
			if (this.DefenderTurnsLeft < 1)
			{
				this.DefenderTurnsLeft = 1;
			}
			this.Battle.Game.GameCore.FireEvent("BattleTurnChanged", new object[0]);
		}

		public void DisableTimer()
		{
			this.TimerEnabled = false;
		}

		internal void AdvanceTurn()
		{
			foreach (WorkingUnit workingUnit in this.Battle.Attacker.Units)
			{
				workingUnit.ResetBattleData();
				workingUnit.ResetBattleMoves();
			}
			foreach (WorkingUnit workingUnit2 in this.Battle.Defender.Units)
			{
				workingUnit2.ResetBattleData();
				workingUnit2.ResetBattleMoves();
			}
			if (this.AttackerTurn)
			{
				this.AttackerTurn = false;
				this.Battle.BeginDefenderTurn();
				if (this.DefenderSkipCount > 0)
				{
					this.DefenderSkipCount--;
					this.Battle.RequestEndTurn(this.Battle.Defender.Owner, false);
					return;
				}
			}
			else
			{
				this.AttackerTurn = true;
				this.CurrentTurn++;
				if (!this.AttackerIsWinning)
				{
					this.DefenderTurnsLeft--;
				}
				else
				{
					this.AttackerTurnsLeft--;
				}
				if (this.DefenderTurnsLeft == 0)
				{
					this.Battle.DeclareWinner(this.Battle.Defender);
				}
				else if (this.AttackerTurnsLeft == 0)
				{
					this.Battle.DeclareWinner(this.Battle.Attacker);
				}
				if (this.DefenderTurnsLeft == 0 || this.AttackerTurnsLeft == 0)
				{
					return;
				}
				this.Battle.Game.GameCore.FireEvent("BattleTurnChanged", new object[0]);
				this.Battle.BeginAttackerTurn();
				if (this.AttackerSkipCount > 0)
				{
					this.AttackerSkipCount--;
					this.Battle.RequestEndTurn(this.Battle.Attacker.Owner, false);
					return;
				}
			}
			if (this.Battle.ActiveStack.Owner.AIPlayer != null)
			{
				this.Battle.BeginAITurn();
			}
		}

		public void BeginBattle()
		{
			if (this.Battle.ActiveStack.Owner.AIPlayer != null)
			{
				this.Battle.BeginAITurn();
			}
		}

		internal void CheckVictoryTiles()
		{
			if (this.Battle.GetControlledVPCount(this.Battle.Attacker.Owner) == this.Battle.GetTotalVPCount())
			{
				if (!this.AttackerIsWinning)
				{
					this.AttackerIsWinning = true;
					this.AttackerTurnsLeft = 2;
					GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_WINNING", new object[0]);
					gameText.AddChildText(GameText.CreateLocalised(this.Battle.Attacker.Owner.DisplayName, new object[0]));
					this.Battle.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
				}
			}
			else if (this.AttackerIsWinning)
			{
				this.AttackerIsWinning = false;
				this.AttackerTurnsLeft = 1000;
				GameText gameText2 = GameText.CreateLocalised("FORMAT_BATTLELOG_WINNING", new object[0]);
				gameText2.AddChildText(GameText.CreateLocalised(this.Battle.Defender.Owner.DisplayName, new object[0]));
				this.Battle.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText2 });
			}
			this.Battle.Game.GameCore.FireEvent("BattleTurnChanged", new object[0]);
		}

		public TacticalBattleController Battle;

		public int AttackerTurnsLeft = 1000;

		public int DefenderTurnsLeft = 12;

		private int AttackerSkipCount;

		private int DefenderSkipCount;

		public int CurrentTurn = 1;

		public bool AttackerTurn;

		private bool TimerEnabled = true;

		public bool AttackerIsWinning;
	}
}
