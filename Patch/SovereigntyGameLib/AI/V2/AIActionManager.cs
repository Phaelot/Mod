using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game;

namespace SovereigntyTK.AI.V2
{
	public class AIActionManager
	{
		public AIActionManager(AIPlayer AI)
		{
			this.Game = AI.Game;
			this.AI = AI;
			this.RNG = new Random();
			this.BattleActionQueue = new List<AIAction>();
			this.ActionQueue = new List<AIAction>();
		}

		private string GetActionText(AIAction Action)
		{
			if (Action == null)
			{
				return "null action";
			}
			return Action.GetType().Name + "#" + Action.ID + " state=" + Action.State;
		}

		private void LogActionTrace(string Text)
		{
			try
			{
				string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SovereigntyTurnDebugLogs");
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "AIV2Actions.log");
				string realmName = (this.AI == null || this.AI.Realm == null) ? "null" : this.AI.Realm.Name;
				string turnText = "";
				try
				{
					turnText = "turn=" + this.Game.TurnController.TurnNumber + " date=" + this.Game.TurnController.CurrentDate.Value.ToString("dd.MM.yyyy") + " ";
				}
				catch
				{
				}
				File.AppendAllText(file, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + turnText + "realm=" + realmName + " :: " + Text + "\r\n");
			}
			catch
			{
			}
		}

		private void ExecuteActionLogged(AIAction Action, bool BattleAction)
		{
			string prefix = BattleAction ? "battle " : "";
			this.LogActionTrace("EXECUTE BEGIN " + prefix + this.GetActionText(Action));
			try
			{
				Action.Execute();
				this.LogActionTrace("EXECUTE END " + prefix + this.GetActionText(Action));
			}
			catch (Exception ex)
			{
				this.LogActionTrace("EXECUTE EXCEPTION " + prefix + this.GetActionText(Action) + ": " + ex.GetType().FullName + ": " + ex.Message + "\r\n" + ex.StackTrace);
				throw;
			}
		}

		private void UpdateActionLogged(AIAction Action, bool BattleAction)
		{
			string prefix = BattleAction ? "battle " : "";
			this.LogActionTrace("UPDATE BEGIN " + prefix + this.GetActionText(Action));
			try
			{
				Action.Update();
				this.LogActionTrace("UPDATE END " + prefix + this.GetActionText(Action));
			}
			catch (Exception ex)
			{
				this.LogActionTrace("UPDATE EXCEPTION " + prefix + this.GetActionText(Action) + ": " + ex.GetType().FullName + ": " + ex.Message + "\r\n" + ex.StackTrace);
				throw;
			}
		}

		public T CreateAction<T>() where T : AIAction
		{
			T t;
			if (typeof(T).IsSubclassOf(typeof(AITacAction)))
			{
				t = (T)((object)Activator.CreateInstance(typeof(T), new object[]
				{
					this,
					this.Game,
					this.Game.CurrentTacticalBattle
				}));
			}
			else
			{
				t = (T)((object)Activator.CreateInstance(typeof(T), new object[] { this, this.Game }));
			}
			t.RNG = this.RNG;
			t.AI = this.AI;
			t.ID = AIActionManager.NextID++;
			return t;
		}

		public void AddAction(AIAction Action, bool Wait = true)
		{
			lock (this.ActionQueue)
			{
				Action.State = AiActionStates.Queued;
				this.ActionQueue.Add(Action);
			}
			this.LogActionTrace("QUEUE " + this.GetActionText(Action) + " wait=" + Wait);
			if (Wait)
			{
				while (Action.State != AiActionStates.Finished)
				{
					Thread.Sleep(1);
				}
			}
		}

		public void AddBattleAction(AIAction Action, bool Wait = true)
		{
			lock (this.BattleActionQueue)
			{
				Action.State = AiActionStates.Queued;
				this.BattleActionQueue.Add(Action);
			}
			this.LogActionTrace("QUEUE battle " + this.GetActionText(Action) + " wait=" + Wait);
			if (Wait)
			{
				while (Action.State != AiActionStates.Finished)
				{
					Thread.Sleep(1);
				}
			}
		}

		public void CheckForBattleActions()
		{
			if (this.CurrentBattleActions == null)
			{
				lock (this.BattleActionQueue)
				{
					this.CurrentBattleActions = new List<AIAction>(this.BattleActionQueue);
					this.BattleActionQueue.Clear();
				}
				this.BattleActionIndex = 0;
			}
			if (this.CurrentBattleActions.Count == 0)
			{
				this.CurrentActions = null;
				return;
			}
			AIAction aiaction = this.CurrentBattleActions[this.BattleActionIndex];
			switch (aiaction.State)
			{
			case AiActionStates.Created:
			case AiActionStates.Queued:
				this.ExecuteActionLogged(aiaction, true);
				return;
			case AiActionStates.Executing:
				this.UpdateActionLogged(aiaction, true);
				return;
			case AiActionStates.Finished:
				aiaction.Dispose();
				this.BattleActionIndex++;
				if (this.BattleActionIndex >= this.CurrentBattleActions.Count)
				{
					this.CurrentBattleActions = null;
				}
				return;
			default:
				return;
			}
		}

		public void CheckForActions()
		{
			if (this.CurrentActions == null)
			{
				lock (this.ActionQueue)
				{
					this.CurrentActions = new List<AIAction>(this.ActionQueue);
					this.ActionQueue.Clear();
				}
				this.CurrentActionIndex = 0;
			}
			if (this.CurrentActions.Count == 0)
			{
				this.CurrentActions = null;
				return;
			}
			AIAction aiaction = this.CurrentActions[this.CurrentActionIndex];
			switch (aiaction.State)
			{
			case AiActionStates.Created:
			case AiActionStates.Queued:
				this.ExecuteActionLogged(aiaction, false);
				return;
			case AiActionStates.Executing:
				this.UpdateActionLogged(aiaction, false);
				return;
			case AiActionStates.Finished:
				aiaction.Dispose();
				this.CurrentActionIndex++;
				if (this.CurrentActionIndex >= this.CurrentActions.Count)
				{
					this.CurrentActions = null;
				}
				return;
			default:
				return;
			}
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

		private SovereigntyGame Game;

		private List<AIAction> ActionQueue;

		private List<AIAction> BattleActionQueue;

		private int CurrentActionIndex;

		private int BattleActionIndex;

		private List<AIAction> CurrentActions;

		private List<AIAction> CurrentBattleActions;

		private static int NextID;

		private AIPlayer AI;

		public Random RNG;
	}
}
