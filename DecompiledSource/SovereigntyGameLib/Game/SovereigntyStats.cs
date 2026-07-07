using System;
using System.Windows.Forms;

namespace SovereigntyTK.Game
{
	public class SovereigntyStats
	{
		public SovereigntyStats(SovereigntyGame Game)
		{
			this.Game = Game;
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleBattleEnded), "BattleEnded");
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleWarStarted), "WarStarted");
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleAlliance), "AllianceFormed");
		}

		public void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleBattleEnded), "BattleEnded");
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleWarStarted), "WarStarted");
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleAlliance), "AllianceFormed");
		}

		private void HandleAlliance(string EventName, params object[] Args)
		{
			this.AllyCount++;
		}

		private void HandleWarStarted(string EventName, params object[] Args)
		{
			this.WarCount++;
		}

		public void Display()
		{
			string text = "";
			object obj = text;
			text = string.Concat(new object[] { obj, "Alliances:\t", this.AllyCount, "\n" });
			object obj2 = text;
			text = string.Concat(new object[] { obj2, "Wars:\t\t", this.WarCount, "\n" });
			object obj3 = text;
			text = string.Concat(new object[] { obj3, "Attacker Wins:\t", this.AttackerWins, "\n" });
			text = text + "Defender Wins:\t" + this.DefenderWins;
			MessageBox.Show(text);
		}

		private void HandleBattleEnded(string EventName, params object[] Args)
		{
			if ((bool)Args[2])
			{
				this.AttackerWins++;
				return;
			}
			this.DefenderWins++;
		}

		private SovereigntyGame Game;

		private int AttackerWins;

		private int DefenderWins;

		private int WarCount;

		private int AllyCount;
	}
}
