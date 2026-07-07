using System;
using System.Collections.Generic;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class SpellTargetData
	{
		public event Action SpellsChanged;

		public IList<SpellEffect> ActiveSpells
		{
			get
			{
				List<SpellEffect> list = new List<SpellEffect>();
				foreach (int num in this.m_ActiveSpells)
				{
					SpellEffect spellEffect = null;
					this.Game.AllSpells.TryGetValue(num, out spellEffect);
					if (spellEffect != null)
					{
						list.Add(spellEffect);
					}
				}
				return list.AsReadOnly();
			}
		}

		public SpellTargetData(SovereigntyGame Game)
		{
			this.Game = Game;
			this.m_ActiveSpells = new List<int>();
			this.IgnoreTypes = new List<SpellTypes>();
			this.IgnoreSpells = new List<string>();
		}

		public void AddSpell(SpellEffect Spell)
		{
			if (this.m_ActiveSpells.Contains(Spell.ID))
			{
				throw new Exception("Double added spell");
			}
			this.m_ActiveSpells.Add(Spell.ID);
			if (this.SpellsChanged != null)
			{
				this.SpellsChanged();
			}
		}

		public void RemoveSpell(SpellEffect Spell)
		{
			if (!this.m_ActiveSpells.Contains(Spell.ID))
			{
				return;
			}
			this.m_ActiveSpells.Remove(Spell.ID);
			if (this.SpellsChanged != null)
			{
				this.SpellsChanged();
			}
		}

		public bool AffectedBySpell(string SpellName)
		{
			foreach (SpellEffect spellEffect in this.ActiveSpells)
			{
				if (spellEffect.SpellName == SpellName)
				{
					return true;
				}
			}
			return false;
		}

		public SpellEffect GetSpellOfType(Type t)
		{
			if (!t.IsSubclassOf(typeof(SpellEffect)))
			{
				throw new Exception(t.Name + " is not a spell effect class");
			}
			foreach (SpellEffect spellEffect in this.ActiveSpells)
			{
				if (spellEffect.GetType() == t)
				{
					return spellEffect;
				}
			}
			return null;
		}

		public bool IgnoreSpell(string SpellName)
		{
			return this.IgnoreSpells.Contains(SpellName);
		}

		public bool IgnoreSpellType(SpellTypes Type)
		{
			return this.IgnoreTypes.Contains(Type);
		}

		public List<int> m_ActiveSpells;

		public SovereigntyGame Game;

		public List<SpellTypes> IgnoreTypes;

		public List<string> IgnoreSpells;
	}
}
