// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.AI.AIMagicManager
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.AI;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI
{
	public class AIMagicManager
	{
		public AIPlayer AI;

		private SovereigntyGame Game;

		public AIMagicManager(AIPlayer AI, SovereigntyGame Game)
		{
			this.AI = AI;
			this.Game = Game;
		}

		internal void SpendMagicPoints()
		{
			if (AI.Realm.MagicData.SpellPoints == 0)
			{
				return;
			}
			if (AI.Realm.MagicData.SpellLevel == 5)
			{
				int num = 5;
				while (num >= 1 && !LearnSpell(num))
				{
					num--;
				}
			}
			else if (AI.Realm.MagicData.GetKnownSpells(AI.Realm.MagicData.SpellLevel).Count > 0)
			{
				AI.Realm.MagicData.IncreaseSpellLevel();
			}
			else if (!LearnSpell(AI.Realm.MagicData.SpellLevel))
			{
				AI.Realm.MagicData.IncreaseSpellLevel();
			}
		}

		private bool LearnSpell(int Level)
		{
			List<RealmMagicData> availableLevelSpells = AI.Realm.MagicData.GetAvailableLevelSpells(Level);
			if (availableLevelSpells.Count == 0)
			{
				return false;
			}
			availableLevelSpells = availableLevelSpells.Where((RealmMagicData x) => AI.Realm.MagicData.CanLearnSpell(x.Name)).ToList();
			if (availableLevelSpells.Count == 0)
			{
				return false;
			}
			int index = AI.RNG.Next(availableLevelSpells.Count);
			RealmMagicData spell = availableLevelSpells[index];
			AIAction aIAction = new AIAction(AIActionTypes.LearnSpell);
			aIAction.Spell = spell;
			AI.SetAction(aIAction);
			return true;
		}

		internal void CastSpell()
		{
			if (AI.Realm.MagicData.SpellCoolDown > 0)
			{
				return;
			}
			List<SpellCastingData> list = new List<SpellCastingData>();
			foreach (RealmMagicData knownSpell in AI.Realm.MagicData.GetKnownSpells())
			{
				SpellCastingData spellCastingData = new SpellCastingData(SpellEffect.CreateEffect(Game, knownSpell, AI.Realm));
				list.Add(spellCastingData);
				spellCastingData.Targets = GetSpellTargets(spellCastingData.Spell);
				spellCastingData.CastChance = spellCastingData.Spell.GetAICastChance(spellCastingData.Targets);
			}
			if (list.Count == 0)
			{
				return;
			}
			list.Sort(CastChanceComparer);
			if (list[0].CastChance <= 0)
			{
				return;
			}
			SpellCastingData spellCastingData2 = null;
			foreach (SpellCastingData item in list)
			{
				if (AI.RNG.Next(100) < item.CastChance)
				{
					spellCastingData2 = item;
					break;
				}
			}
			if (spellCastingData2 == null)
			{
				return;
			}
			if (spellCastingData2.Spell.SpellData.TargetType == SpellTargets.None)
			{
				CastSpell(spellCastingData2, null);
			}
			else if (spellCastingData2.Spell.SpellData.TargetType == SpellTargets.Realm && spellCastingData2.Spell.SpellData.Range == 0)
			{
				CastSpell(spellCastingData2, AI.Realm);
			}
			else
			{
				if (spellCastingData2.Targets.Count == 0)
				{
					return;
				}
				spellCastingData2.CreateTargetChances();
				if (spellCastingData2.TargetCastChances.Max() == 0)
				{
					return;
				}
				int maxValue = spellCastingData2.TargetCastChances.Sum();
				int num = AI.RNG.Next(maxValue);
				int num2 = 0;
				for (int i = 0; i < spellCastingData2.TargetCastChances.Count; i++)
				{
					num2 += spellCastingData2.TargetCastChances[i];
					if (num2 >= num)
					{
						CastSpell(spellCastingData2, spellCastingData2.Targets[i]);
						break;
					}
				}
			}
		}

		private void CastSpell(SpellCastingData Spell, object Target)
		{
			AIAction aIAction = new AIAction(AIActionTypes.CastSpell);
			aIAction.SpellEffect = Spell.Spell;
			aIAction.SpellTarget = Target;
			AI.SetAction(aIAction);
		}

		private int CastChanceComparer(SpellCastingData x, SpellCastingData y)
		{
			return y.CastChance.CompareTo(x.CastChance);
		}

		private List<object> GetSpellTargets(SpellEffect Spell)
		{
			List<object> list = new List<object>();
			switch (Spell.SpellData.TargetType)
			{
				case SpellTargets.Realm:
					foreach (WorkingRealm value in Game.AllRealms.Values)
					{
						if (!value.RealmIsDead && Spell.TargetIsValid(value))
						{
							list.Add(value);
						}
					}
					break;
				case SpellTargets.Province:
					foreach (WorkingProvince value2 in Game.AllProvinces.Values)
					{
						if (Spell.TargetIsValid(value2))
						{
							list.Add(value2);
						}
					}
					break;
				case SpellTargets.SeaZone:
					foreach (WorkingZone value3 in Game.AllZones.Values)
					{
						if (Spell.TargetIsValid(value3))
						{
							list.Add(value3);
						}
					}
					break;
				case SpellTargets.Stack:
					foreach (WorkingStack value4 in Game.AllStacks.Values)
					{
						if (Spell.TargetIsValid(value4))
						{
							list.Add(value4);
						}
					}
					break;
				case SpellTargets.Unit:
					foreach (WorkingStack value5 in Game.AllStacks.Values)
					{
						foreach (WorkingUnit unit in value5.Units)
						{
							if (Spell.TargetIsValid(unit))
							{
								list.Add(unit);
							}
						}
					}
					break;
				default:
					return null;
				case SpellTargets.None:
					break;
			}
			return list;
		}
	}
}