using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Battle
{
	public class PreviewResultData
	{
		public PreviewResultData(WorkingUnit Unit, CombatAction ActionType)
		{
			this.Unit = Unit;
			this.ActionType = ActionType;
			this.StatusEffects = new List<GameText>();
		}

		public bool HasDamage()
		{
			return this.MaxCasualties + this.MaxSupportCasualties > 0;
		}

		public WorkingUnit Unit;

		public int MinCasualties;

		public int MaxCasualties;

		public int MinSupportCasualties;

		public int MaxSupportCasualties;

		public int Heal;

		public List<GameText> StatusEffects;

		public CombatAction ActionType;
	}
}
