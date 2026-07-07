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
				aiaction.Execute();
				return;
			case AiActionStates.Executing:
				aiaction.Update();
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
				aiaction.Execute();
				return;
			case AiActionStates.Executing:
				aiaction.Update();
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
