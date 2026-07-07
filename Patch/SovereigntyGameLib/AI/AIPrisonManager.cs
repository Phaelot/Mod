using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI
{
	public class AIPrisonManager
	{
		public AIPrisonManager(AIPlayer AI, SovereigntyGame Game)
		{
			this.AI = AI;
			this.Game = Game;
		}

		internal void UpdatePrisoners()
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			List<WorkingUnit> list2 = new List<WorkingUnit>();
			List<WorkingUnit> list3 = new List<WorkingUnit>();
			List<WorkingUnit> list4 = new List<WorkingUnit>();
			List<WorkingUnit> list5 = new List<WorkingUnit>();
			List<WorkingUnit> list6 = new List<WorkingUnit>();
			int num = this.AI.Realm.Provinces.Sum((WorkingProvince x) => x.GetSlaverySlots());
			int num2 = this.AI.Realm.MagicData.SpellCoolDown;
			int num3 = this.Game.EconomyController.GetRealmTotalIncome(this.AI.Realm) - this.Game.EconomyController.GetTotalExpenses(this.AI.Realm);
			int num4 = this.AI.Realm.Gold;
			foreach (WorkingUnit workingUnit in this.AI.Realm.Prison.AllPrisoners)
			{
				RelationStates relation = this.AI.Realm.DiplomacyManager.GetRelation(workingUnit.OwnerRealm);
				float num5;
				if (workingUnit.OwnerRealm.AIPlayer != null)
				{
					num5 = workingUnit.OwnerRealm.AIPlayer.TradeManager.GetPrisonerValue(workingUnit);
				}
				else
				{
					num5 = this.AI.Trade.GetPrisonerValue(workingUnit);
				}
				if (workingUnit.OwnerRealm == this.Game.RebelRealm || workingUnit.OwnerRealm.RealmIsDead)
				{
					num5 = 0f;
				}
				if (relation == RelationStates.War)
				{
					if (this.AI.Realm.Alignment == RealmAlignments.Evil && num > 0)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Enslave, workingUnit))
						{
							continue;
						}
						if (num5 < 50f || (num5 < 200f && this.AI.RNG.Next(100) > 75))
						{
							list.Add(workingUnit);
							num--;
							continue;
						}
					}
					if (this.AI.Realm.MagicValue > 0 && num2 > 0 && !this.AI.Realm.CodeOfWar)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Experiment, workingUnit))
						{
							continue;
						}
						if (num5 < 50f || (num5 < 200f && this.AI.RNG.Next(100) > 75))
						{
							list.Add(workingUnit);
							num2--;
							continue;
						}
					}
					if (num3 < 0 && num4 < 500)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Execute, workingUnit))
						{
							continue;
						}
						if (num5 < 50f || this.AI.RNG.Next(100) > 75)
						{
							list5.Add(workingUnit);
							num3 += workingUnit.Upkeep / 3;
							continue;
						}
					}
					if (num3 > workingUnit.Upkeep && num4 > this.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm) + 500)
					{
						if (this.AI.Realm.Race == Races.Undead)
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.MakeUndead, workingUnit))
							{
								continue;
							}
							list4.Add(workingUnit);
						}
						else
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Recruit, workingUnit))
							{
								continue;
							}
							list3.Add(workingUnit);
						}
						num4 -= this.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm);
						num3 -= workingUnit.Upkeep;
					}
				}
				else
				{
					if (this.AI.Realm.CodeOfWar)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Release, workingUnit))
						{
							continue;
						}
						if (workingUnit.OwnerRealm != this.Game.RebelRealm && !workingUnit.OwnerRealm.RealmIsDead)
						{
							if (relation == RelationStates.Peace)
							{
								list2.Add(workingUnit);
								continue;
							}
							continue;
						}
					}
					if (this.AI.Realm.Alignment == RealmAlignments.Evil && num > 0)
					{
						if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Enslave, workingUnit))
						{
							continue;
						}
						if (num5 < 50f || (num5 < 200f && this.AI.RNG.Next(100) > 75))
						{
							list.Add(workingUnit);
							num--;
							continue;
						}
					}
					if (num3 > workingUnit.Upkeep && num4 > this.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm) + 500)
					{
						if (this.AI.Realm.Race == Races.Undead)
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.MakeUndead, workingUnit))
							{
								continue;
							}
							list4.Add(workingUnit);
						}
						else
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Recruit, workingUnit))
							{
								continue;
							}
							list3.Add(workingUnit);
						}
						num4 -= this.Game.PrisonerController.GetRecruitCost(workingUnit, this.AI.Realm);
						num3 -= workingUnit.Upkeep;
					}
					else
					{
						if (this.AI.Realm.DiplomacyManager.GetDisposition(workingUnit.OwnerRealm) > 5f)
						{
							if (!this.AI.Realm.Prison.ActionPossible(PrisonActions.Release, workingUnit))
							{
								continue;
							}
							if (workingUnit.OwnerRealm != this.Game.RebelRealm && !workingUnit.OwnerRealm.RealmIsDead)
							{
								list2.Add(workingUnit);
								continue;
							}
						}
						if (this.AI.Realm.MagicValue > 0 && num2 > 0 && !this.AI.Realm.CodeOfWar && this.AI.Realm.Prison.ActionPossible(PrisonActions.Experiment, workingUnit) && (num5 < 50f || (num5 < 200f && this.AI.RNG.Next(100) > 75)))
						{
							list.Add(workingUnit);
							num2--;
						}
					}
				}
			}
			if (list2.Count > 0)
			{
				AIAction aiaction = new AIAction(AIActionTypes.ReleaseUnits);
				aiaction.Units = list2;
				this.AI.SetAction(aiaction);
			}
			if (list5.Count > 0)
			{
				AIAction aiaction2 = new AIAction(AIActionTypes.ExecuteUnits);
				aiaction2.Units = list5;
				this.AI.SetAction(aiaction2);
			}
			if (list.Count > 0)
			{
				AIAction aiaction3 = new AIAction(AIActionTypes.Enslave);
				aiaction3.Units = list;
				this.AI.SetAction(aiaction3);
			}
			if (list6.Count > 0)
			{
				AIAction aiaction4 = new AIAction(AIActionTypes.SacrificeUnits);
				aiaction4.Units = list6;
				this.AI.SetAction(aiaction4);
			}
			if (list3.Count > 0)
			{
				AIAction aiaction5 = new AIAction(AIActionTypes.RecruitUnits);
				aiaction5.Units = list3;
				this.AI.SetAction(aiaction5);
			}
			if (list4.Count > 0)
			{
				AIAction aiaction6 = new AIAction(AIActionTypes.RaiseUnits);
				aiaction6.Units = list4;
				this.AI.SetAction(aiaction6);
			}
		}

		public AIPlayer AI;

		public SovereigntyGame Game;
	}
}
