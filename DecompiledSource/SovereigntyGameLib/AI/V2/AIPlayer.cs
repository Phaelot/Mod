using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.AI.V2.Scripts;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2
{
	public class AIPlayer
	{
		public AIPlayer(SovereigntyGame Game, WorkingRealm Realm)
		{
			this.Realm = Realm;
			this.Game = Game;
			this.RNG = new Random();
			this.GetPersonality();
			this.ActionManager = new AIActionManager(this);
			this.RelationsManager = new AIRelationsManager(this);
			this.ConstructionManager = new AIConstructionManager(this);
			this.EspionageManager = new AIEspionageManager(this);
			this.MagicManager = new AIMagicManager(this);
			this.PrisonManager = new AIPrisonManager(this);
			this.ResourcesManager = new AIResourcesManager(this);
			this.TacticalManager = new AITacticalManager(this);
			this.TradeManager = new AITradeManager(this);
			this.UnitsManager = new AIUnitsManager(this);
			this.WarManager = new AIWarManager(this);
			this.RevoltManager = new AIRevoltManager(this);
			this.BudgetManager = new AIBudgetManager(this);
			float num = (float)Realm.Agents.Count;
			int num2 = (int)Math.Ceiling((double)(num * 0.5f));
			IList<WorkingAgent> agents = Realm.Agents;
			for (int i = 0; i < num2; i++)
			{
				this.TradeManager.AssignAgent(agents[i]);
			}
			int num3 = num2;
			while ((float)num3 < num)
			{
				this.EspionageManager.AssignAgent(agents[num3]);
				num3++;
			}
			this.Utility = new AIUtilities(this);
		}

		private void GetPersonality()
		{
			List<AIPersonality> list = new List<AIPersonality>();
			foreach (Type type in this.Game.GameCore.Utilities.ScriptManager.AIAssembly.GetTypes())
			{
				if (type.IsSubclassOf(typeof(AIPersonality)))
				{
					AIPersonality aipersonality = (AIPersonality)Activator.CreateInstance(type);
					if (!aipersonality.UseForRealm(this.Realm.Name))
					{
						aipersonality.Dispose();
					}
					else
					{
						list.Add(aipersonality);
					}
				}
			}
			if (list.Count == 0)
			{
				throw new Exception("Unable to find personality script for " + this.Realm.Name);
			}
			this.Personality = list[this.RNG.Next(list.Count)];
			foreach (AIPersonality aipersonality2 in list)
			{
				if (aipersonality2 != this.Personality)
				{
					aipersonality2.Dispose();
				}
			}
			this.Personality.Init(this);
		}

		public void TakeTurn()
		{
			new Stopwatch();
			new List<string>();
			try
			{
				this.TurnActive = true;
				if (this.Realm.RealmIsDead)
				{
					this.ActionManager.AddAction(this.ActionManager.CreateAction<AIActionEndTurn>(), true);
					this.TurnActive = false;
				}
				else if (this.Realm == this.Game.RebelRealm)
				{
					this.RevoltManager.BeginRevoltChecks();
					this.ActionManager.AddAction(this.ActionManager.CreateAction<AIActionEndTurn>(), true);
					this.TurnActive = false;
				}
				else
				{
					this.BudgetManager.Update();
					this.TradeManager.UpdateCooldowns();
					this.WarManager.UpdateInvasionTargets();
					this.PrisonManager.UpdatePrisoners();
					this.MagicManager.UpdateInvestment();
					this.MagicManager.SpendPoints();
					this.MagicManager.CastSpells();
					this.UnitsManager.PreparePurchaseList();
					this.ResourcesManager.UpdateResources();
					this.RelationsManager.EndTreaties();
					this.RelationsManager.OfferTreaties();
					this.WarManager.UpdateWarStatus();
					this.WarManager.MakePeaceOffers();
					this.WarManager.DeclareWars();
					this.EspionageManager.Update();
					this.UnitsManager.DeployUnits();
					this.UnitsManager.PurchaseUnits();
					this.ConstructionManager.ConstructBuildings();
					this.UnitsManager.BeginAttacks();
					this.UnitsManager.MoveUnits();
					this.IgnoreRebels--;
					this.ActionManager.AddAction(this.ActionManager.CreateAction<AIActionEndTurn>(), true);
					this.TurnActive = false;
				}
			}
			catch (Exception ex)
			{
				ErrorDialog errorDialog = new ErrorDialog(ex.Message, ex.StackTrace, this.Game.GameCore);
				errorDialog.ShowDialog();
				this.Game.GameCore.Stop();
			}
		}

		public void TakeTurnResume()
		{
			try
			{
				this.TurnActive = true;
				this.SavedBattleActive = true;
				this.Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
				while (this.SavedBattleActive)
				{
					Thread.Sleep(1);
				}
				this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
				if (this.Realm == this.Game.RebelRealm)
				{
					this.RevoltManager.CheckForRevolts();
					this.ActionManager.AddAction(this.ActionManager.CreateAction<AIActionEndTurn>(), true);
					this.TurnActive = false;
				}
				else
				{
					this.UnitsManager.DoAttacks();
					this.IgnoreRebels--;
					this.ActionManager.AddAction(this.ActionManager.CreateAction<AIActionEndTurn>(), true);
					this.TurnActive = false;
				}
			}
			catch (Exception ex)
			{
				ErrorDialog errorDialog = new ErrorDialog(ex.Message, ex.StackTrace, this.Game.GameCore);
				errorDialog.ShowDialog();
				this.Game.GameCore.Stop();
			}
		}

		private void HandleBattleEnd(string EventName, params object[] Args)
		{
		}

		public void Dispose()
		{
			this.ActionManager.Dispose();
			this.BudgetManager.Dispose();
			this.RelationsManager.Dispose();
			this.ConstructionManager.Dispose();
			this.EspionageManager.Dispose();
			this.MagicManager.Dispose();
			this.PrisonManager.Dispose();
			this.ResourcesManager.Dispose();
			this.TacticalManager.Dispose();
			this.TradeManager.Dispose();
			this.UnitsManager.Dispose();
			this.WarManager.Dispose();
			this.RevoltManager.Dispose();
		}

		internal void BeginTurn()
		{
			Thread thread = new Thread(new ThreadStart(this.TakeTurn));
			thread.Start();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.IgnoreCapitolLust);
			w.Write(this.IgnoreRebels);
			this.ActionManager.Save(w);
			this.BudgetManager.Save(w);
			this.RelationsManager.Save(w);
			this.ConstructionManager.Save(w);
			this.EspionageManager.Save(w);
			this.MagicManager.Save(w);
			this.PrisonManager.Save(w);
			this.ResourcesManager.Save(w);
			this.TacticalManager.Save(w);
			this.TradeManager.Save(w);
			this.UnitsManager.Save(w);
			this.WarManager.Save(w);
			this.RevoltManager.Save(w);
			this.Personality.Save(w);
		}

		internal void ResumeTurn()
		{
			Thread thread = new Thread(new ThreadStart(this.TakeTurnResume));
			thread.Start();
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			if (SaveVersion <= 55)
			{
				this.LoadLegacySave(r, SaveVersion);
				return;
			}
			this.IgnoreCapitolLust = r.ReadBoolean();
			this.IgnoreRebels = r.ReadInt32();
			this.ActionManager.Load(r, SaveVersion);
			this.RelationsManager.Load(r, SaveVersion);
			this.ConstructionManager.Load(r, SaveVersion);
			this.EspionageManager.Load(r, SaveVersion);
			this.MagicManager.Load(r, SaveVersion);
			this.PrisonManager.Load(r, SaveVersion);
			this.ResourcesManager.Load(r, SaveVersion);
			this.TacticalManager.Load(r, SaveVersion);
			this.TradeManager.Load(r, SaveVersion);
			this.UnitsManager.Load(r, SaveVersion);
			this.WarManager.Load(r, SaveVersion);
			this.RevoltManager.Load(r, SaveVersion);
			this.BudgetManager.Load(r, SaveVersion);
			this.Personality.Load(r, SaveVersion);
		}

		private void LoadLegacySave(BinaryReader r, int SaveVersion)
		{
			if (r.ReadBoolean())
			{
				this.UnitsManager.LoadLegacyStackList(r, SaveVersion);
			}
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int num2 = r.ReadInt32();
				WarData warData = new WarData(this.Game, this, r, SaveVersion);
				this.WarManager.Wars.Add(num2, warData);
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				r.ReadInt32();
				r.ReadInt32();
			}
			this.UnitsManager.IgnoreProvinces = new List<int>();
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				this.UnitsManager.IgnoreProvinces.Add(r.ReadInt32());
			}
			this.IgnoreCapitolLust = r.ReadBoolean();
			num = r.ReadInt32();
			for (int l = 0; l < num; l++)
			{
				string text = r.ReadString();
				int num3 = r.ReadInt32();
				if (!this.WarManager.LustModifiers.ContainsKey(text))
				{
					this.WarManager.LustModifiers.Add(text, num3);
				}
				else
				{
					Dictionary<string, int> lustModifiers;
					string text2;
					(lustModifiers = this.WarManager.LustModifiers)[text2 = text] = lustModifiers[text2] + num3;
				}
			}
			this.RevoltManager.RebelProvinceID = r.ReadInt32();
		}

		internal void Log(string LogText)
		{
			this.Game.GameCore.FireEvent("AILogAction", new object[] { LogText });
		}

		public AIPersonality Personality;

		public AIBudgetManager BudgetManager;

		public AIRelationsManager RelationsManager;

		public AIConstructionManager ConstructionManager;

		public AIEspionageManager EspionageManager;

		public AIMagicManager MagicManager;

		public AIPrisonManager PrisonManager;

		public AIResourcesManager ResourcesManager;

		public AITacticalManager TacticalManager;

		public AITradeManager TradeManager;

		public AIUnitsManager UnitsManager;

		public AIWarManager WarManager;

		public AIRevoltManager RevoltManager;

		public AIUtilities Utility;

		public AIActionManager ActionManager;

		public WorkingRealm Realm;

		public SovereigntyGame Game;

		public Random RNG;

		public bool TurnActive;

		public int IgnoreRebels;

		public bool IgnoreCapitolLust;

		public bool Disposed;

		private bool SavedBattleActive;
	}
}
