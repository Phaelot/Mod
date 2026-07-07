using System;
using SovereigntyTK.UI.Controls;

namespace SovereigntyTK.UI
{
	public abstract class ScriptBase
	{
		public void Init(Sovereignty Game, UIControl GameForm)
		{
			this.GameSystem = Game;
			this.GameForm = GameForm;
		}

		public virtual void Update(float ElapsedTime)
		{
		}

		public abstract void Dispose();

		protected Sovereignty GameSystem;

		protected UIControl GameForm;
	}
}
