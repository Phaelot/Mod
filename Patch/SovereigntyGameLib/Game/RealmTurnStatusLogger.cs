using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	internal static class RealmTurnStatusLogger
	{
		public static void WriteTurnStatus(SovereigntyGame Game)
		{
			if (Game == null || Game.TurnController == null)
			{
				return;
			}
			try
			{
				string directory = GetLogDirectory();
				Directory.CreateDirectory(directory);
				int turnNumber = Game.TurnController.TurnNumber;
				string path = System.IO.Path.Combine(directory, "Turn_" + turnNumber.ToString("0000") + ".csv");
				List<string> lines = new List<string>();
				lines.Add("Turn,Date,Realm,Dead,Alignment,Race,Gold,Income,Expenses,NetGold,ProvinceIncome,TradeIncome,SpecialIncome,InterestIncome,UnitUpkeep,MagicExpenses,TotalResourceStock,Resources,ResourceFlow,Units,ActiveUnits,QueuedUnits,Stacks,Heroes,Agents,Provinces,OccupiedProvinces,Wars,Enemies");
				foreach (WorkingRealm realm in Game.AllRealms.Values.OrderBy((WorkingRealm x) => x.ID))
				{
					if (realm == Game.RebelRealm)
					{
						continue;
					}
					lines.Add(BuildRealmLine(Game, realm, turnNumber));
				}
				File.WriteAllLines(path, lines.ToArray(), Encoding.UTF8);
			}
			catch
			{
				// Diagnostics must never interrupt the campaign turn.
			}
		}

		private static string GetLogDirectory()
		{
			string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (string.IsNullOrEmpty(documents))
			{
				documents = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			}
			if (string.IsNullOrEmpty(documents))
			{
				documents = AppDomain.CurrentDomain.BaseDirectory;
			}
			return System.IO.Path.Combine(documents, "SovereigntyTurnStatusLogs");
		}

		private static string BuildRealmLine(SovereigntyGame Game, WorkingRealm Realm, int TurnNumber)
		{
			int income = SafeInt(delegate { return Game.EconomyController.GetRealmTotalIncome(Realm); });
			int expenses = SafeInt(delegate { return Game.EconomyController.GetTotalExpenses(Realm); });
			int provinceIncome = SafeInt(delegate { return Game.EconomyController.GetRealmProvinceIncome(Realm); });
			int tradeIncome = SafeInt(delegate { return Game.EconomyController.GetRealmTradeIncome(Realm); });
			int specialIncome = SafeInt(delegate { return Realm.SpecialIncomes.Sum(); });
			int interestIncome = SafeInt(delegate { return (int)(Realm.InterestRate.GetValue() * 0.01f * Realm.GetTotalGold()); });
			int unitUpkeep = SafeInt(delegate { return Game.EconomyController.GetUnitUpkeep(Realm); });
			int magicExpenses = SafeInt(delegate { return Game.EconomyController.GetMagicExpenses(Realm); });
			int totalResourceStock = SafeInt(delegate { return Game.Data.Resources.Values.Sum((ResourceData x) => Realm.GetStockpiledResource(x)); });
			List<WorkingRealm> enemies = Realm.Enemies.Where((WorkingRealm x) => x != null && x != Realm && x != Game.RebelRealm && !x.RealmIsDead).OrderBy((WorkingRealm x) => x.Name).ToList<WorkingRealm>();
			List<string> fields = new List<string>();
			fields.Add(TurnNumber.ToString());
			fields.Add(Game.TurnController.CurrentDate.Value.ToString("yyyy-MM-dd"));
			fields.Add(Realm.Name);
			fields.Add(Realm.RealmIsDead ? "1" : "0");
			fields.Add(Realm.Alignment.ToString());
			fields.Add(Realm.Race.ToString());
			fields.Add(SafeInt(delegate { return Realm.Gold.Value; }).ToString());
			fields.Add(income.ToString());
			fields.Add(expenses.ToString());
			fields.Add((income - expenses).ToString());
			fields.Add(provinceIncome.ToString());
			fields.Add(tradeIncome.ToString());
			fields.Add(specialIncome.ToString());
			fields.Add(interestIncome.ToString());
			fields.Add(unitUpkeep.ToString());
			fields.Add(magicExpenses.ToString());
			fields.Add(totalResourceStock.ToString());
			fields.Add(BuildResourceStock(Game, Realm));
			fields.Add(BuildResourceFlow(Game, Realm));
			fields.Add(SafeInt(delegate { return Realm.Units.Count; }).ToString());
			fields.Add(SafeInt(delegate { return Realm.Stacks.Sum((WorkingStack x) => x.Units.Count); }).ToString());
			fields.Add(SafeInt(delegate { return Realm.GetCurrentUnitQueue().Count; }).ToString());
			fields.Add(SafeInt(delegate { return Realm.Stacks.Count; }).ToString());
			fields.Add(SafeInt(delegate { return Realm.Heroes.Count; }).ToString());
			fields.Add(SafeInt(delegate { return Realm.Agents.Count; }).ToString());
			fields.Add(SafeInt(delegate { return Realm.Provinces.Count; }).ToString());
			fields.Add(SafeInt(delegate { return Realm.OccupiedProvinces.Count; }).ToString());
			fields.Add(enemies.Count.ToString());
			fields.Add(string.Join(";", enemies.Select((WorkingRealm x) => x.Name).ToArray()));
			return string.Join(",", fields.Select(new Func<string, string>(Csv)).ToArray());
		}

		private static string BuildResourceStock(SovereigntyGame Game, WorkingRealm Realm)
		{
			List<string> parts = new List<string>();
			foreach (ResourceData resource in Game.Data.Resources.Values.OrderBy((ResourceData x) => x.ResourceName))
			{
				int stock = SafeInt(delegate { return Realm.GetStockpiledResource(resource); });
				parts.Add(resource.ResourceName + "=" + stock.ToString());
			}
			return string.Join(";", parts.ToArray());
		}

		private static string BuildResourceFlow(SovereigntyGame Game, WorkingRealm Realm)
		{
			List<string> parts = new List<string>();
			foreach (ResourceData resource in Game.Data.Resources.Values.OrderBy((ResourceData x) => x.ResourceName))
			{
				int income = SafeInt(delegate { return Realm.GetResourceIncome(resource, true); });
				int expenses = SafeInt(delegate { return Realm.GetResourceExpenses(resource); });
				parts.Add(resource.ResourceName + "=+" + income.ToString() + "/-" + expenses.ToString());
			}
			return string.Join(";", parts.ToArray());
		}

		private static int SafeInt(Func<int> Getter)
		{
			try
			{
				return Getter();
			}
			catch
			{
				return 0;
			}
		}

		private static string Csv(string Value)
		{
			if (Value == null)
			{
				Value = "";
			}
			bool quote = Value.Contains(",") || Value.Contains("\"") || Value.Contains("\r") || Value.Contains("\n") || Value.Contains(";");
			if (quote)
			{
				return "\"" + Value.Replace("\"", "\"\"") + "\"";
			}
			return Value;
		}
	}
}
