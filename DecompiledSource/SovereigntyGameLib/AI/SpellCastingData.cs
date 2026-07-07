using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI
{
	internal class SpellCastingData
	{
		public SpellCastingData(SpellEffect Spell)
		{
			this.Spell = Spell;
		}

		internal void CreateTargetChances()
		{
			this.TargetCastChances = new List<int>();
			foreach (object obj in this.Targets)
			{
				int num = this.Spell.AITargetWeight(obj);
				num = Math.Max(0, num);
				this.TargetCastChances.Add(num);
			}
		}

		public SpellEffect Spell;

		public List<object> Targets;

		public int CastChance;

		public List<int> TargetCastChances;
	}
}
