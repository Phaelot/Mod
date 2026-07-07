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

		public void Update()
		{
			this.AI.Log("");
			this.AI.Log("Budget Manager Updating");
			this.AI.UnitsManager.Funds.CurrentPercentage = 0.45f + (float)(this.AI.Realm.Enemies.Count - 1) * 0.2f;
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
			foreach (AIFundData aifundData in list)
			{
				float num3 = aifundData.CurrentPercentage / num;
				aifundData.CurrentGold += (int)Math.Ceiling((double)(num2 * num3));
			}
			this.AI.Realm.Gold.Value -= Amount;
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
