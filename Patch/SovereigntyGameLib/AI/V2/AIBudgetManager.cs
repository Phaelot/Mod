using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SovereigntyTK.AI.V2
{
	public class AIBudgetManager
	{
		public AIBudgetManager(AIPlayer AI)
		{
			this.AI = AI;
			AI.Realm.Gold.OnStatIncreased += this.Gold_OnStatIncreased;
			this.AllFunds = new List<AIFundData>();
			this.AllFunds.Add(AI.MagicManager.Funds);
			this.AllFunds.Add(AI.UnitsManager.Funds);
			this.AllFunds.Add(AI.ResourcesManager.Funds);
			this.AllFunds.Add(AI.TradeManager.Funds);
			this.AllFunds.Add(AI.ConstructionManager.Funds);
			this.AllFunds.Add(AI.RelationsManager.Funds);
			this.AllFunds.Add(AI.EspionageManager.Funds);
			this.AllFunds.Add(AI.PrisonManager.Funds);
			AI.PrisonManager.Funds.CurrentPercentage = 0.1f;
			AI.PrisonManager.Funds.MaximumGold = 5000;
			AI.MagicManager.Funds.CurrentPercentage = 0.15f;
			AI.MagicManager.Funds.MaximumGold = 10000;
			AI.EspionageManager.Funds.CurrentPercentage = 0.05f;
			AI.EspionageManager.Funds.MaximumGold = 2000;
			AI.RelationsManager.Funds.CurrentPercentage = 0.05f;
			AI.RelationsManager.Funds.MaximumGold = 4000;
			AI.ConstructionManager.Funds.CurrentPercentage = 0.1f;
			AI.ConstructionManager.Funds.MaximumGold = 20000;
			AI.TradeManager.Funds.CurrentPercentage = 0.1f;
			AI.ResourcesManager.Funds.CurrentPercentage = 0.1f;
			AI.UnitsManager.Funds.CurrentPercentage = 0.45f;
		}

		private void Gold_OnStatIncreased()
		{
			this.AddMoney(this.AI.Realm.Gold);
		}

		private string GetFundName(AIFundData Fund)
		{
			if (Fund == this.AI.MagicManager.Funds)
			{
				return "Magic";
			}
			if (Fund == this.AI.UnitsManager.Funds)
			{
				return "Units";
			}
			if (Fund == this.AI.ResourcesManager.Funds)
			{
				return "Resources";
			}
			if (Fund == this.AI.TradeManager.Funds)
			{
				return "Trade";
			}
			if (Fund == this.AI.ConstructionManager.Funds)
			{
				return "Construction";
			}
			if (Fund == this.AI.RelationsManager.Funds)
			{
				return "Relations";
			}
			if (Fund == this.AI.EspionageManager.Funds)
			{
				return "Espionage";
			}
			if (Fund == this.AI.PrisonManager.Funds)
			{
				return "Prison";
			}
			return "Unknown";
		}

		private string FormatBudgetFunds()
		{
			return string.Join(", ", (from x in this.AllFunds
				select string.Concat(new object[] { this.GetFundName(x), "=", x.CurrentGold, " @", x.CurrentPercentage, " max=", x.MaximumGold })).ToArray<string>());
		}

		private void LogBudgetToFile(string Text)
		{
			try
			{
				string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SovereigntyTurnDebugLogs");
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
				string file = Path.Combine(folder, "AIBudget.log");
				string turn = (this.AI.Game != null && this.AI.Game.TurnController != null) ? ("T" + this.AI.Game.TurnController.TurnNumber) : "T?";
				string realm = (this.AI.Realm != null) ? this.AI.Realm.Name : "Unknown Realm";
				string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				File.AppendAllText(file, stamp + " " + turn + " [" + realm + "] " + Text + "\r\n");
			}
			catch
			{
			}
		}

		public void Update()
		{
			this.AI.Log("");
			this.AI.Log("Budget Manager Updating");
			float currentPercentage = this.AI.UnitsManager.Funds.CurrentPercentage;
			this.AI.UnitsManager.Funds.CurrentPercentage = 0.45f + (float)(this.AI.Realm.Enemies.Count - 1) * 0.2f;
			this.LogBudgetToFile(string.Concat(new object[]
			{
				"Update: realm gold=",
				this.AI.Realm.Gold.Value,
				", enemies=",
				this.AI.Realm.Enemies.Count,
				", Units percentage ",
				currentPercentage,
				" -> ",
				this.AI.UnitsManager.Funds.CurrentPercentage,
				", active percentage sum=",
				this.AllFunds.Where((AIFundData x) => x.MaximumGold == 0 || x.CurrentGold < x.MaximumGold).Sum((AIFundData x) => x.CurrentPercentage),
				", funds: ",
				this.FormatBudgetFunds()
			}));
			AIPlayer ai = this.AI;
			object[] array = new object[4];
			array[0] = "  Units budget set to ";
			array[1] = this.AI.UnitsManager.Funds.CurrentPercentage;
			array[2] = ":";
			array[3] = this.AllFunds.Sum((AIFundData x) => x.CurrentPercentage);
			ai.Log(string.Concat(array));
		}

		public void AddMoney(int Amount)
		{
			List<AIFundData> list = this.AllFunds.Where((AIFundData x) => x.MaximumGold == 0 || x.CurrentGold < x.MaximumGold).ToList<AIFundData>();
			float num = list.Sum((AIFundData x) => x.CurrentPercentage);
			float num2 = (float)Amount;
			int value = this.AI.Realm.Gold.Value;
			int totalGold = this.GetTotalGold();
			List<string> list2 = new List<string>();
			int num3 = 0;
			foreach (AIFundData aifundData in list)
			{
				float num4 = aifundData.CurrentPercentage / num;
				int num5 = (int)Math.Ceiling((double)(num2 * num4));
				aifundData.CurrentGold += num5;
				num3 += num5;
				list2.Add(string.Concat(new object[] { this.GetFundName(aifundData), " +", num5, " (share=", num4, ", pct=", aifundData.CurrentPercentage, ")" }));
			}
			this.AI.Realm.Gold.Value -= Amount;
			this.LogBudgetToFile(string.Concat(new object[]
			{
				"AddMoney: incoming=",
				Amount,
				", realm gold ",
				value,
				" -> ",
				this.AI.Realm.Gold.Value,
				", fund total ",
				totalGold,
				" -> ",
				this.GetTotalGold(),
				", active percentage sum=",
				num,
				", allocated=",
				num3,
				", rounding delta=",
				num3 - Amount,
				", allocations: ",
				string.Join("; ", list2.ToArray()),
				", funds after: ",
				this.FormatBudgetFunds()
			}));
		}

		internal void Dispose()
		{
		}

		internal void Save(BinaryWriter w)
		{
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
		}

		internal int GetTotalGold()
		{
			return this.AllFunds.Sum((AIFundData x) => x.CurrentGold);
		}

		public AIPlayer AI;

		private List<AIFundData> AllFunds;
	}
}
