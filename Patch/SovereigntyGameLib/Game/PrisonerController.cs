using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class PrisonerController
	{
		public PrisonerController(SovereigntyGame Game)
		{
			this.Game = Game;
		}

		internal void RecruitUnits(List<WorkingUnit> Units, WorkingRealm TargetRealm, bool MakeUndead)
		{
			new Random();
			List<WorkingRealm> list = new List<WorkingRealm>();
			foreach (WorkingUnit workingUnit in Units)
			{
				if (workingUnit.OwnerRealm != null && !workingUnit.OwnerRealm.RealmIsDead && workingUnit.OwnerRealm != this.Game.RebelRealm && !list.Contains(workingUnit.OwnerRealm))
				{
					list.Add(workingUnit.OwnerRealm);
				}
			}
			if (MakeUndead)
			{
				foreach (WorkingRealm workingRealm in list)
				{
					DiplomaticEventData diplomaticEventData = new DiplomaticEventData();
					diplomaticEventData.DecayRate = 0.5f;
					diplomaticEventData.DispositionEffect = -20f;
					diplomaticEventData.DisplayName = "EVENT_RAISE_PRIMARY";
					diplomaticEventData.StackMode = DiplomaticStackModes.Refresh;
					diplomaticEventData.Expires = true;
					diplomaticEventData.EventName = "EventRaisePrimary";
					workingRealm.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData);
				}
				if (list.Count <= 0)
				{
					goto IL_0239;
				}
				using (IEnumerator<WorkingRealm> enumerator3 = this.Game.AllRealms.Values.Where((WorkingRealm x) => x.Race != Races.Undead).GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						WorkingRealm workingRealm2 = enumerator3.Current;
						DiplomaticEventData diplomaticEventData2 = new DiplomaticEventData();
						diplomaticEventData2.DecayRate = 0.5f;
						diplomaticEventData2.DispositionEffect = -10f;
						diplomaticEventData2.DisplayName = "EVENT_RAISE_SECONDARY";
						diplomaticEventData2.StackMode = DiplomaticStackModes.Refresh;
						diplomaticEventData2.Expires = true;
						diplomaticEventData2.EventName = "EventRaiseSecondary";
						workingRealm2.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData2);
					}
					goto IL_0239;
				}
			}
			foreach (WorkingRealm workingRealm3 in list)
			{
				DiplomaticEventData diplomaticEventData3 = new DiplomaticEventData();
				diplomaticEventData3.DecayRate = 0.5f;
				diplomaticEventData3.DispositionEffect = -10f;
				diplomaticEventData3.DisplayName = "EVENT_RECRUIT";
				diplomaticEventData3.StackMode = DiplomaticStackModes.Refresh;
				diplomaticEventData3.Expires = true;
				diplomaticEventData3.EventName = "EventRecruit";
				workingRealm3.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData3);
			}
			IL_0239:
			foreach (WorkingUnit workingUnit2 in Units)
			{
				TargetRealm.Prison.ReleasePrisoner(workingUnit2);
				int recruitCost = this.GetRecruitCost(workingUnit2, TargetRealm);
				TargetRealm.SpendPrisonGold(recruitCost);
				this.Game.GameCore.FireEvent("UnitOwnershipChanged", new object[] { workingUnit2, workingUnit2.OwnerRealm, TargetRealm });
				if (MakeUndead)
				{
					workingUnit2.Race = Races.Undead;
					workingUnit2.HealRate.BaseValue = 0;
					if ((workingUnit2.Rank == UnitRanks.Standard || workingUnit2.Rank == UnitRanks.Mercenary) && !workingUnit2.HasAnyNamedFlag("Brave"))
					{
						workingUnit2.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Brave"));
					}
					if (workingUnit2.Rank == UnitRanks.Elite || workingUnit2.Rank == UnitRanks.Unique)
					{
						if (!workingUnit2.HasAnyNamedFlag("Attack_Death"))
						{
							workingUnit2.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Attack_Death"));
						}
						if (!workingUnit2.HasAnyNamedFlag("Darkdweller"))
						{
							workingUnit2.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Darkdweller"));
						}
					}
					if (workingUnit2.Rank == UnitRanks.Unique && !workingUnit2.HasAnyNamedFlag("Fearsome"))
					{
						workingUnit2.GrantFlag(UnitFlag.CreateNamedFlag(this.Game.GameCore, "Fearsome"));
					}
				}
				else if (workingUnit2.Rank == UnitRanks.Standard)
				{
					workingUnit2.Rank = UnitRanks.Mercenary;
					workingUnit2.Upkeep.BaseValue *= 3;
				}
				workingUnit2.RemoveNamedFlags("Auxiliary", 0);
				workingUnit2.OwnerRealmID = TargetRealm.ID;
				TargetRealm.QueueUnit(workingUnit2, true, false);
			}
		}

		internal void ReleaseUnits(List<WorkingUnit> Units, WorkingRealm TargetRealm)
		{
			new Random();
			Dictionary<WorkingRealm, float> dictionary = new Dictionary<WorkingRealm, float>();
			foreach (WorkingUnit workingUnit in Units)
			{
				TargetRealm.Prison.ReleasePrisoner(workingUnit);
				workingUnit.OwnerRealm.QueueUnit(workingUnit, true, false);
				if (!dictionary.ContainsKey(workingUnit.OwnerRealm))
				{
					dictionary.Add(workingUnit.OwnerRealm, 0f);
				}
				Dictionary<WorkingRealm, float> dictionary2;
				WorkingRealm ownerRealm;
				(dictionary2 = dictionary)[ownerRealm = workingUnit.OwnerRealm] = dictionary2[ownerRealm] + 0.2f;
			}
			foreach (KeyValuePair<WorkingRealm, float> keyValuePair in dictionary)
			{
				DiplomaticEventData diplomaticEventData = new DiplomaticEventData();
				diplomaticEventData.DecayRate = 0.2f;
				diplomaticEventData.DispositionEffect = keyValuePair.Value;
				diplomaticEventData.DisplayName = "EVENT_RELEASE";
				diplomaticEventData.StackMode = DiplomaticStackModes.Stack;
				diplomaticEventData.Expires = true;
				diplomaticEventData.EventName = "EventRelease";
				keyValuePair.Key.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData);
			}
		}

		internal void SacrificeUnits(List<WorkingUnit> Units, WorkingRealm TargetRealm)
		{
			new Random();
			List<WorkingRealm> list = new List<WorkingRealm>();
			foreach (WorkingUnit workingUnit in Units)
			{
				if (!workingUnit.OwnerRealm.RealmIsDead && workingUnit.OwnerRealm != this.Game.RebelRealm && !list.Contains(workingUnit.OwnerRealm))
				{
					list.Add(workingUnit.OwnerRealm);
				}
			}
			foreach (WorkingRealm workingRealm in list)
			{
				DiplomaticEventData diplomaticEventData = new DiplomaticEventData();
				diplomaticEventData.DecayRate = 0.5f;
				diplomaticEventData.DispositionEffect = -15f;
				diplomaticEventData.DisplayName = "EVENT_EXECUTE_PRIMARY";
				diplomaticEventData.StackMode = DiplomaticStackModes.Refresh;
				diplomaticEventData.Expires = true;
				diplomaticEventData.EventName = "EventExecutePrimary";
				workingRealm.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData);
			}
			if (list.Count > 0)
			{
				foreach (WorkingRealm workingRealm2 in this.Game.AllRealms.Values)
				{
					int num;
					if (workingRealm2.CodeOfWar)
					{
						num = 8;
					}
					else
					{
						num = 4;
					}
					DiplomaticEventData diplomaticEventData2 = new DiplomaticEventData();
					diplomaticEventData2.DecayRate = 0.5f;
					diplomaticEventData2.DispositionEffect = (float)(-(float)num);
					diplomaticEventData2.DisplayName = "EVENT_EXECUTE_SECONDARY";
					diplomaticEventData2.StackMode = DiplomaticStackModes.Refresh;
					diplomaticEventData2.Expires = true;
					diplomaticEventData2.EventName = "EventExecuteSecondary";
					workingRealm2.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData2);
				}
			}
			foreach (WorkingUnit workingUnit2 in Units)
			{
				TargetRealm.Prison.ReleasePrisoner(workingUnit2);
				this.Game.DestroyUnit(workingUnit2);
				TargetRealm.MagicData.ModifyCooldown(-500);
			}
			this.Game.GameCore.FireEvent("SpellsUpdated", new object[0]);
		}

		internal void ExecuteUnits(List<WorkingUnit> Units, WorkingRealm TargetRealm)
		{
			new Random();
			List<WorkingRealm> list = new List<WorkingRealm>();
			foreach (WorkingUnit workingUnit in Units)
			{
				if (!workingUnit.OwnerRealm.RealmIsDead && workingUnit.OwnerRealm != this.Game.RebelRealm && !list.Contains(workingUnit.OwnerRealm))
				{
					list.Add(workingUnit.OwnerRealm);
				}
			}
			foreach (WorkingRealm workingRealm in list)
			{
				DiplomaticEventData diplomaticEventData = new DiplomaticEventData();
				diplomaticEventData.DecayRate = 0.5f;
				diplomaticEventData.DispositionEffect = -10f;
				diplomaticEventData.DisplayName = "EVENT_EXECUTE_PRIMARY";
				diplomaticEventData.StackMode = DiplomaticStackModes.Refresh;
				diplomaticEventData.Expires = true;
				diplomaticEventData.EventName = "EventExecutePrimary";
				workingRealm.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData);
			}
			if (list.Count > 0)
			{
				foreach (WorkingRealm workingRealm2 in this.Game.AllRealms.Values.Where((WorkingRealm x) => x.CodeOfWar))
				{
					DiplomaticEventData diplomaticEventData2 = new DiplomaticEventData();
					diplomaticEventData2.DecayRate = 0.5f;
					diplomaticEventData2.DispositionEffect = -5f;
					diplomaticEventData2.DisplayName = "EVENT_EXECUTE_SECONDARY";
					diplomaticEventData2.StackMode = DiplomaticStackModes.Refresh;
					diplomaticEventData2.Expires = true;
					diplomaticEventData2.EventName = "EventExecuteSecondary";
					workingRealm2.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData2);
				}
			}
			foreach (WorkingUnit workingUnit2 in Units)
			{
				TargetRealm.Prison.ReleasePrisoner(workingUnit2);
				this.Game.DestroyUnit(workingUnit2);
			}
		}

		internal void EnslaveUnits(List<WorkingUnit> Units, WorkingRealm TargetRealm)
		{
			Random random = new Random();
			List<WorkingRealm> list = new List<WorkingRealm>();
			foreach (WorkingUnit workingUnit in Units)
			{
				if (!workingUnit.OwnerRealm.RealmIsDead && workingUnit.OwnerRealm != this.Game.RebelRealm && !list.Contains(workingUnit.OwnerRealm))
				{
					list.Add(workingUnit.OwnerRealm);
				}
			}
			foreach (WorkingRealm workingRealm in list)
			{
				DiplomaticEventData diplomaticEventData = new DiplomaticEventData();
				diplomaticEventData.DecayRate = 0.5f;
				diplomaticEventData.DispositionEffect = -10f;
				diplomaticEventData.DisplayName = "EVENT_SLAVERY_PRIMARY";
				diplomaticEventData.StackMode = DiplomaticStackModes.Refresh;
				diplomaticEventData.Expires = true;
				diplomaticEventData.EventName = "EventSlaveryPrimary";
				workingRealm.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData);
			}
			if (list.Count > 0)
			{
				foreach (WorkingRealm workingRealm2 in this.Game.AllRealms.Values.Where((WorkingRealm x) => x.Alignment == RealmAlignments.Good))
				{
					DiplomaticEventData diplomaticEventData2 = new DiplomaticEventData();
					diplomaticEventData2.DecayRate = 0.5f;
					diplomaticEventData2.DispositionEffect = -5f;
					diplomaticEventData2.DisplayName = "EVENT_SLAVERY_SECONDARY";
					diplomaticEventData2.StackMode = DiplomaticStackModes.Refresh;
					diplomaticEventData2.Expires = true;
					diplomaticEventData2.EventName = "EventSlaverySecondary";
					workingRealm2.DiplomacyManager.TriggerEvent(TargetRealm, diplomaticEventData2);
				}
			}
			foreach (WorkingUnit workingUnit2 in Units)
			{
				TargetRealm.Prison.ReleasePrisoner(workingUnit2);
				this.Game.DestroyUnit(workingUnit2);
				List<WorkingProvince> list2 = TargetRealm.Provinces.Where((WorkingProvince x) => x.CurrentEconomy < 10).ToList<WorkingProvince>();
				if (list2.Count > 0)
				{
					WorkingProvince workingProvince = list2[random.Next(list2.Count)];
					workingProvince.AddSlaveBonus(5);
				}
			}
		}

		public int GetRecruitCost(WorkingUnit Unit, WorkingRealm Recruiter)
		{
			if (Recruiter == null)
			{
				throw new Exception("Null realm tried to recruit prisoners");
			}
			float num = (float)Unit.BaseCost;
			float num2 = 1f;
			if (Unit.OwnerRealm != null && Unit.OwnerRealm.CodeOfWar == Recruiter.CodeOfWar)
			{
				num2 += 1.1f;
			}
			else
			{
				num2 += 1.5f;
			}
			if (Unit.OwnerRealm != null && Unit.OwnerRealm.Race == Recruiter.Race)
			{
				num2 += 1.1f;
			}
			else
			{
				num2 += 1.8f;
			}
			if (Unit.OwnerRealm != null && Unit.OwnerRealm.Alignment == Recruiter.Alignment)
			{
				num2 += 1.1f;
			}
			else
			{
				num2 += 1.8f;
			}
			return (int)(num * num2);
		}

		private SovereigntyGame Game;
	}
}
