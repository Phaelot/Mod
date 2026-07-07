using System;
using SovereigntyTK.Game;

namespace SovereigntyTK.AI.V2
{
	public abstract class AIAction
	{
		public abstract void Execute();

		public AIAction(AIActionManager Manager, SovereigntyGame Game)
		{
			this.Manager = Manager;
			this.Game = Game;
			this.State = AiActionStates.Created;
		}

		public virtual void Update()
		{
		}

		public virtual void Dispose()
		{
		}

		public void Submit()
		{
			this.Manager.AddAction(this, true);
		}

		public Random RNG;

		public AIPlayer AI;

		public AIActionManager Manager;

		public SovereigntyGame Game;

		public AiActionStates State;

		public int ID;
	}
}
