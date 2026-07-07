using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2
{
	public class AIMagicManager
	{
		public SovereigntyTK.AI.V2.AIPlayer AI;

		public AIFundData Funds;

		public AIMagicManager(SovereigntyTK.AI.V2.AIPlayer AI)
		{
			this.AI = AI;
			Funds = new AIFundData();
		}

		internal void Dispose()
		{
		}

		private bool LearnSpell(int Level)
		{
			AI.Log("  Looking for spell to learn (level " + Level + ")");
			List<RealmMagicData> availableLevelSpells = AI.Realm.MagicData.GetAvailableLevelSpells(Level);
			if (availableLevelSpells.Count == 0)
			{
				AI.Log("  No spells available");
				return false;
			}
			availableLevelSpells = availableLevelSpells.Where((RealmMagicData x) => AI.Realm.MagicData.CanLearnSpell(x.Name)).ToList();
			if (availableLevelSpells.Count == 0)
			{
				AI.Log("Cannot learn any spells");
				return false;
			}
			int index = AI.RNG.Next(availableLevelSpells.Count);
			RealmMagicData realmMagicData = availableLevelSpells[index];
			AI.Log("  Learning spell: " + realmMagicData.Name);
			AIActionLearnSpell aIActionLearnSpell = AI.ActionManager.CreateAction<AIActionLearnSpell>();
			aIActionLearnSpell.Spell = realmMagicData;
			AI.ActionManager.AddAction(aIActionLearnSpell);
			return true;
		}

		internal void SpendPoints()
		{
			AI.Log("");
			AI.Log("Magic Manager updating (spend phase)");
			if (AI.Realm.MagicData.SpellPoints == 0)
			{
				AI.Log("  No magic points available to spend");
			}
			else if (AI.Realm.MagicData.SpellLevel == 5)
			{
				int num = 5;
				while (num >= 1 && !LearnSpell(num))
				{
					num--;
				}
			}
			else if (AI.Realm.MagicData.GetKnownSpells(AI.Realm.MagicData.SpellLevel).Count > 0)
			{
				AI.Log("  Spell at this level already known, increasing magic level to level " + AI.Realm.MagicData.MagicLevel + 1);
				AI.Realm.MagicData.IncreaseSpellLevel();
			}
			else if (!LearnSpell(AI.Realm.MagicData.SpellLevel))
			{
				AI.Log("Cannot learn spell, increasing magic to level " + AI.Realm.MagicData.MagicLevel + 1);
				AI.Realm.MagicData.IncreaseSpellLevel();
			}
		}

		private void CastSpell(SpellCastingData Spell, object Target)
		{
			AIActionCastSpell aIActionCastSpell = AI.ActionManager.CreateAction<AIActionCastSpell>();
			aIActionCastSpell.Spell = Spell.Spell;
			aIActionCastSpell.Target = Target;
			AI.ActionManager.AddAction(aIActionCastSpell);
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
					foreach (WorkingRealm value in AI.Game.AllRealms.Values)
					{
						if (!value.RealmIsDead && Spell.TargetIsValid(value))
						{
							list.Add(value);
						}
					}
					break;
				case SpellTargets.Province:
					foreach (WorkingProvince value2 in AI.Game.AllProvinces.Values)
					{
						if (Spell.TargetIsValid(value2))
						{
							list.Add(value2);
						}
					}
					break;
				case SpellTargets.SeaZone:
					foreach (WorkingZone value3 in AI.Game.AllZones.Values)
					{
						if (Spell.TargetIsValid(value3))
						{
							list.Add(value3);
						}
					}
					break;
				case SpellTargets.Stack:
					foreach (WorkingStack value4 in AI.Game.AllStacks.Values)
					{
						if (Spell.TargetIsValid(value4))
						{
							list.Add(value4);
						}
					}
					break;
				case SpellTargets.Unit:
					foreach (WorkingStack value5 in AI.Game.AllStacks.Values)
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

		internal void CastSpells()
		{
			AI.Log("");
			AI.Log("Magic Manager updating (cast phase)");
			if (AI.Realm.MagicData.SpellCoolDown > 0)
			{
				AI.Log("  Cooldown active, unable to cast");
				return;
			}
			List<SpellCastingData> list = new List<SpellCastingData>();
			foreach (RealmMagicData knownSpell in AI.Realm.MagicData.GetKnownSpells())
			{
				SpellCastingData spellCastingData = new SpellCastingData(SpellEffect.CreateEffect(AI.Game, knownSpell, AI.Realm));
				list.Add(spellCastingData);
				spellCastingData.Targets = GetSpellTargets(spellCastingData.Spell);
				spellCastingData.CastChance = spellCastingData.Spell.GetAICastChance(spellCastingData.Targets);
			}
			if (list.Count == 0)
			{
				AI.Log("  No spells known, unable to cast");
				return;
			}
			list.Sort(CastChanceComparer);
			if (list[0].CastChance <= 0)
			{
				AI.Log("  All spells have negative cast chance, unable to cast");
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
				AI.Log("  Failed to choose spell, unable to cast");
				return;
			}
			AI.Log("  Casting spell: " + spellCastingData2.Spell.SpellName);
			if (spellCastingData2.Spell.SpellData.TargetType == SpellTargets.None)
			{
				AI.Log("    No target required, casting");
				CastSpell(spellCastingData2, null);
				return;
			}
			if (spellCastingData2.Spell.SpellData.TargetType == SpellTargets.Realm && spellCastingData2.Spell.SpellData.Range == 0)
			{
				AI.Log("    Target is own realm, casting");
				CastSpell(spellCastingData2, AI.Realm);
				return;
			}
			if (spellCastingData2.Targets.Count == 0)
			{
				AI.Log("    No valid targets for spell, cast failed");
				return;
			}
			spellCastingData2.CreateTargetChances();
			if (spellCastingData2.TargetCastChances.Max() == 0)
			{
				AI.Log("    All targets unsuitable, cast failed");
				return;
			}
			int maxValue = spellCastingData2.TargetCastChances.Sum();
			int num = AI.RNG.Next(maxValue);
			int num2 = 0;
			for (int i = 0; i < spellCastingData2.TargetCastChances.Count; i++)
			{
				num2 += spellCastingData2.TargetCastChances[i];
				if (num2 > num)
				{
					AI.Log("    Cast target selected, casting");
					CastSpell(spellCastingData2, spellCastingData2.Targets[i]);
					break;
				}
			}
		}

		internal void Save(BinaryWriter w)
		{
			Funds.Save(w);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			Funds.Load(r, SaveVersion);
		}

		internal void UpdateInvestment()
		{
			AI.Log("");
			AI.Log("Magic manager updating (investment phase");
			AI.Log("  Available funds for investment: " + Funds.CurrentGold);
			AI.Realm.MagicData.CurrentInvestment = 100;
		}
	}
}
