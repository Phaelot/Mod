using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.Game.Battle
{
	public class BattleCleanupData
	{
		public void SetData(AutoBattleController Battle)
		{
			this.WinnerName = Battle.Winner.Owner.Name;
			this.LoserName = Battle.GetLoserRealm().Name;
			this.CapturedAttackerUnits = Battle.CapturedattackerUnits;
			this.CapturedDefenderUnits = Battle.CapturedDefenderUnits;
			this.Attacker = Battle.Attacker;
			this.Defender = Battle.Defender;
			this.AttackerRealm = this.Attacker.Owner;
			this.DefenderRealm = this.Defender.Owner;
			this.Loser = Battle.GetLoserStack();
			this.Winner = Battle.GetWinnerStack();
			this.Node = Battle.Node;
			this.AttackerNode = Battle.AttackerNode;
			if (this.Attacker == this.Winner)
			{
				this.WinnerCount = Battle.InitialAttackers;
				this.LoserCount = Battle.InitialDefenders;
			}
			else
			{
				this.WinnerCount = Battle.InitialDefenders;
				this.LoserCount = Battle.InitialAttackers;
			}
			if (this.Node.Province != null && this.Node.Province.HarbourNode != null && this.Node.Province.HarbourNode.CurrentStack != null)
			{
				this.HarbourStack = this.Node.Province.HarbourNode.CurrentStack;
			}
		}

		public void SetData(TacticalBattleController TacticalBattle)
		{
			this.WinnerName = TacticalBattle.Winner.Owner.Name;
			this.LoserName = TacticalBattle.Loser.Owner.Name;
			this.CapturedAttackerUnits = TacticalBattle.CapturedAttackerUnits;
			this.CapturedDefenderUnits = TacticalBattle.CapturedDefenderUnits;
			this.Attacker = TacticalBattle.Attacker;
			this.Defender = TacticalBattle.Defender;
			this.AttackerRealm = this.Attacker.Owner;
			this.DefenderRealm = this.Defender.Owner;
			this.Loser = TacticalBattle.Loser;
			this.Winner = TacticalBattle.Winner;
			this.Node = TacticalBattle.Node;
			this.AttackerNode = TacticalBattle.AttackerNode;
			if (this.Attacker == this.Winner)
			{
				this.WinnerCount = TacticalBattle.InitialAttackers;
				this.LoserCount = TacticalBattle.InitialDefenders;
			}
			else
			{
				this.WinnerCount = TacticalBattle.InitialDefenders;
				this.LoserCount = TacticalBattle.InitialAttackers;
			}
			if (this.Node.Province != null && this.Node.Province.HarbourNode != null && this.Node.Province.HarbourNode.CurrentStack != null)
			{
				this.HarbourStack = this.Node.Province.HarbourNode.CurrentStack;
			}
		}

		public WorkingStack Attacker;

		public WorkingStack Defender;

		public WorkingStack Loser;

		public WorkingStack Winner;

		public ActivePathNode Node;

		public ActivePathNode AttackerNode;

		public WorkingRealm AttackerRealm;

		public WorkingRealm DefenderRealm;

		public List<WorkingUnit> CapturedAttackerUnits;

		public List<WorkingUnit> CapturedDefenderUnits;

		public WorkingStack HarbourStack;

		public string WinnerName = "";

		public string LoserName = "";

		public int WinnerCount;

		public int LoserCount;
	}
}
