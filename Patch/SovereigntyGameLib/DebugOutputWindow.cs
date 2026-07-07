using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK
{
	public partial class DebugOutputWindow : Form
	{
		public DebugOutputWindow(Sovereignty Game)
		{
			this.InitializeComponent();
			this.Game = Game;
			Game.RegisterEvent(new GenericDelegate(this.HandleLogEvent), "AILogAction");
			Game.RegisterEvent(new GenericDelegate(this.HandleNewGame), "NewGameStarted");
			Game.RegisterEvent(new GenericDelegate(this.HandleGameEnded), "GameEnded");
			Game.RegisterEvent(new GenericDelegate(this.HandleNewTurn), "GameTurnStarted");
			this.groupBox1.Enabled = false;
		}

		private void HandleNewTurn(string EventName, params object[] Args)
		{
			if (this.CurrentGame == null)
			{
				return;
			}
			this.numericUpDown1.Maximum = this.CurrentGame.TurnController.TurnNumber;
			this.numericUpDown2.Maximum = this.CurrentGame.TurnController.TurnNumber;
		}

		private void HandleGameEnded(string EventName, params object[] Args)
		{
			this.Log = null;
			this.groupBox1.Enabled = false;
		}

		private void HandleNewGame(string EventName, params object[] Args)
		{
			this.Log = new Dictionary<int, Dictionary<string, List<string>>>();
			this.groupBox1.Enabled = true;
			this.CurrentGame = Args[0] as SovereigntyGame;
			this.comboBox1.Items.Clear();
			foreach (WorkingRealm workingRealm in this.CurrentGame.AllRealms.Values)
			{
				if (workingRealm.AIPlayer != null)
				{
					this.comboBox1.Items.Add(workingRealm.Name);
				}
			}
			this.numericUpDown1.Maximum = this.CurrentGame.TurnController.TurnNumber;
			this.numericUpDown2.Maximum = this.CurrentGame.TurnController.TurnNumber;
		}

		private void HandleLogEvent(string EventName, params object[] Args)
		{
			string text = (string)Args[0];
			WorkingRealm currentRealm = this.CurrentGame.TurnController.CurrentRealm;
			int turnNumber = this.CurrentGame.TurnController.TurnNumber;
			if (!this.Log.ContainsKey(turnNumber))
			{
				this.Log.Add(turnNumber, new Dictionary<string, List<string>>());
			}
			if (!this.Log[turnNumber].ContainsKey(currentRealm.Name))
			{
				this.Log[turnNumber].Add(currentRealm.Name, new List<string>());
			}
			this.Log[turnNumber][currentRealm.Name].Add(text);
		}

		public new void Dispose()
		{
			base.Dispose();
			this.Game.UnregisterEvent(new GenericDelegate(this.HandleLogEvent), "AILogAction");
			this.Game.UnregisterEvent(new GenericDelegate(this.HandleNewGame), "NewGameStarted");
			this.Game.UnregisterEvent(new GenericDelegate(this.HandleGameEnded), "GameEnded");
			this.Game.UnregisterEvent(new GenericDelegate(this.HandleNewTurn), "GameTurnStarted");
		}

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
		}

		private void button1_Click(object sender, EventArgs e)
		{
			int num = (int)this.numericUpDown1.Value;
			int num2 = (int)this.numericUpDown2.Value;
			string text = this.comboBox1.Text;
			this.textBox1.Clear();
			StringBuilder stringBuilder = new StringBuilder();
			string text2 = "AI Log for " + text;
			string text3 = "";
			while (text3.Length < text2.Length)
			{
				text3 += "=";
			}
			stringBuilder.AppendLine(text2);
			stringBuilder.AppendLine(text3);
			foreach (KeyValuePair<int, Dictionary<string, List<string>>> keyValuePair in this.Log.OrderBy((KeyValuePair<int, Dictionary<string, List<string>>> x) => x.Key))
			{
				if (keyValuePair.Key >= num && keyValuePair.Key <= num2)
				{
					stringBuilder.AppendLine("");
					stringBuilder.AppendLine("Turn " + keyValuePair.Key);
					stringBuilder.AppendLine("=======");
					if (!keyValuePair.Value.ContainsKey(text))
					{
						stringBuilder.AppendLine("Nothing logged for " + text);
					}
					else
					{
						foreach (string text4 in keyValuePair.Value[text])
						{
							stringBuilder.AppendLine(text4);
						}
					}
				}
			}
			this.textBox1.Text = stringBuilder.ToString();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.numericUpDown1.Value = this.numericUpDown1.Maximum - 1m;
			this.numericUpDown2.Value = this.numericUpDown2.Maximum - 1m;
		}

		public Dictionary<int, Dictionary<string, List<string>>> Log;

		private Sovereignty Game;

		private SovereigntyGame CurrentGame;
	}
}
