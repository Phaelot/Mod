using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI
{
	public class AIBattleManager
	{
		public AIBattleManager(AIPlayer AI, SovereigntyGame Game)
		{
			this.Game = Game;
			this.AI = AI;
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.CurrentStackID = r.ReadInt32();
			this.RealmStackIDs = new List<int>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.RealmStackIDs.Add(r.ReadInt32());
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.CurrentStackID);
			if (this.RealmStackIDs == null)
			{
				w.Write(0);
				return;
			}
			w.Write(this.RealmStackIDs.Count);
			foreach (int num in this.RealmStackIDs)
			{
				w.Write(num);
			}
		}

		internal void BeginAttacks()
		{
			this.RealmStackIDs = new List<int>();
			foreach (WorkingStack workingStack in this.AI.Realm.Stacks)
			{
				this.RealmStackIDs.Add(workingStack.ID);
			}
			this.CurrentStackID = 0;
		}

		internal void DoAttacks()
		{
			if (this.Game.AIAttacksDisabled)
			{
				return;
			}
			while (this.CurrentStackID < this.RealmStackIDs.Count)
			{
				WorkingStack workingStack = null;
				this.Game.AllStacks.TryGetValue(this.RealmStackIDs[this.CurrentStackID], out workingStack);
				this.CurrentStackID++;
				if (workingStack != null && (workingStack.Node.Province == null || !workingStack.Node.Province.Occupied))
				{
					Dictionary<WorkingProvince, float> dictionary = new Dictionary<WorkingProvince, float>();
					if (workingStack.Node.NodeType == PathNodeTypes.Harbour)
					{
						if (this.RealmIsEnemy(workingStack.Node.Province.OccupierRealm))
						{
							dictionary.Add(workingStack.Node.Province, this.GetTargetValue(workingStack.Node.Province, false));
						}
					}
					else
					{
						foreach (GameRegion gameRegion in workingStack.Node.GetRegion().GetAllConnectedRegions())
						{
							if (!(gameRegion is WorkingZone))
							{
								WorkingProvince workingProvince = gameRegion as WorkingProvince;
								if ((workingProvince.HarbourNode == null || workingProvince.HarbourNode.CurrentStack == null || workingProvince.HarbourNode.CurrentStack.Owner != this.AI.Realm) && this.RealmIsEnemy(workingProvince.OccupierRealm) && !dictionary.ContainsKey(workingProvince))
								{
									dictionary.Add(workingProvince, this.GetTargetValue(workingProvince, workingStack.Node.GetRegion() is WorkingZone));
								}
							}
						}
					}
					foreach (KeyValuePair<WorkingProvince, float> keyValuePair in dictionary.OrderByDescending((KeyValuePair<WorkingProvince, float> x) => x.Value))
					{
						if (!this.AI.IgnoreProvinces.Contains(keyValuePair.Key.ID) && (!this.AI.Realm.CodeOfWar || keyValuePair.Key.OccupierRealm.DiplomacyManager.GetRelationTime(this.AI.Realm) != 0) && this.CanWinProvince(workingStack, keyValuePair.Key, workingStack.Node.GetRegion() is WorkingZone))
						{
							ActivePathNode activePathNode = keyValuePair.Key.LandNode;
							if (workingStack.Node.GetRegion() is WorkingZone && keyValuePair.Key.HarbourNode != null)
							{
								activePathNode = keyValuePair.Key.HarbourNode;
							}
							List<WorkingUnit> list = new List<WorkingUnit>();
							foreach (WorkingUnit workingUnit in workingStack.Units)
							{
								if (this.Game.DestinationChecker.NodeOKForUnit(workingUnit, activePathNode) == UnitMoveResult.OK && workingUnit.HasMoves())
								{
									list.Add(workingUnit);
								}
							}
							if (list.Count != 0)
							{
								if ((activePathNode.NodeType == PathNodeTypes.Harbour || activePathNode.NodeType == PathNodeTypes.RiverHarbour) && workingStack.Node.GetRegion() is WorkingZone && activePathNode.Province.OccupierRealm != workingStack.Owner)
								{
									WorkingStack interceptStack = this.Game.GetInterceptStack(activePathNode.Province.OccupierRealm, workingStack.Node.Zone, workingStack);
									if (interceptStack != null)
									{
										AIAction aiaction = new AIAction(AIActionTypes.Intercept);
										aiaction.Stack = workingStack;
										aiaction.Units = list;
										aiaction.Node = activePathNode;
										aiaction.InterceptStack = interceptStack;
										this.AI.SetAction(aiaction);
										list = new List<WorkingUnit>();
										foreach (WorkingUnit workingUnit2 in workingStack.Units)
										{
											if (this.Game.DestinationChecker.NodeOKForUnit(workingUnit2, activePathNode) == UnitMoveResult.OK)
											{
												list.Add(workingUnit2);
											}
										}
										if (list.Count == 0)
										{
											continue;
										}
									}
								}
								AIAction aiaction2 = new AIAction(AIActionTypes.Attack);
								aiaction2.Province = keyValuePair.Key;
								aiaction2.Stack = workingStack;
								aiaction2.Units = list;
								aiaction2.Node = activePathNode;
								aiaction2.Realm = keyValuePair.Key.OccupierRealm;
								this.AI.SetAction(aiaction2);
								if (this.AI.Disposed)
								{
									return;
								}
								break;
							}
						}
					}
				}
			}
		}

		private bool CanWinProvince(WorkingStack Stack, WorkingProvince Province, bool NavalAttack)
		{
			ActivePathNode activePathNode = Province.LandNode;
			if (NavalAttack && Province.HarbourNode != null)
			{
				activePathNode = Province.HarbourNode;
			}
			if (Province.OwnerRealm == this.Game.RebelRealm && this.AI.IgnoreRebels > 0)
			{
				return false;
			}
			if (this.AI.Realm.Restrictions.IgnoreProvinces.Contains(Province.Name))
			{
				return false;
			}
			float num;
			if (activePathNode.CurrentStack == null)
			{
				num = 100f;
			}
			else
			{
				List<WorkingUnit> list = new List<WorkingUnit>(activePathNode.CurrentStack.Units);
				int num2 = activePathNode.Province.FortLevel;
				int num3 = 0;
				foreach (WorkingUnit workingUnit in activePathNode.Province.Forts)
				{
					if (num3 >= num2)
					{
						break;
					}
					if (!workingUnit.Disabled)
					{
						list.Add(workingUnit);
						num3++;
					}
				}
				List<WorkingUnit> list2 = new List<WorkingUnit>();
				foreach (WorkingUnit workingUnit2 in Stack.Units)
				{
					if (this.Game.DestinationChecker.NodeOKForUnit(workingUnit2, activePathNode) == UnitMoveResult.OK)
					{
						list2.Add(workingUnit2);
					}
				}
				int num4 = 0;
				int num5 = 0;
				foreach (WorkingUnit workingUnit3 in list2)
				{
					bool flag = workingUnit3.RangedAttack > 0;
					foreach (WorkingUnit workingUnit4 in list)
					{
						CombatResults combatResults = CombatManager.PerformCombat(workingUnit3, workingUnit4, CombatType.Simulated, flag, true, false);
						if (combatResults != null)
						{
							num4 += combatResults.AttackerCasualties;
							num5 += combatResults.DefenderCasualties;
						}
					}
				}
				num = (float)num5 / (float)num4;
				float num6 = (float)list2.Sum((WorkingUnit x) => x.Health) / (float)list.Sum((WorkingUnit x) => x.Health);
				num *= num6;
			}
			num += (float)this.Game.Data.AITraits[this.AI.Realm.Name].Warmonger * 0.01f;
			return num > 1f;
		}

		private float GetTargetValue(WorkingProvince Province, bool NavalAttack)
		{
			float num = 0f;
			ActivePathNode activePathNode = Province.LandNode;
			if (NavalAttack && Province.HarbourNode != null)
			{
				activePathNode = Province.HarbourNode;
			}
			if (Province.OccupierRealm == this.Game.RebelRealm)
			{
				num += 10f;
			}
			if (activePathNode.CurrentStack == null)
			{
				num += 20f;
			}
			else
			{
				float num2 = 10f;
				num2 -= (float)activePathNode.CurrentStack.Units.Count;
				num2 -= (float)Province.FortLevel;
				num += num2;
			}
			num += (float)Province.ResearchPoints;
			if (Province.Resource != null && this.AI.Realm.UnitPurchaseManager.ResourceIsUseful(Province.Resource))
			{
				num += 10f;
			}
			num -= (float)Province.CurrentEconomy;
			int num3 = 0;
			foreach (GameRegion gameRegion in Province.GetAllConnectedRegions())
			{
				if (!(gameRegion is WorkingZone) && (gameRegion as WorkingProvince).OwnerID == this.AI.RealmID)
				{
					num3++;
				}
			}
			num3 = 4 - num3;
			num -= (float)(num3 * 5);
			num += (float)Province.AILust;
			num += (float)this.AI.GetLustModifier(Province.Name);
			if (Province.IsCapitol && Province.OwnerRealm != this.AI.Realm && !this.AI.IgnoreCapitolLust)
			{
				num -= 40f;
			}
			return num;
		}

		private bool RealmIsEnemy(WorkingRealm Realm)
		{
			return this.AI.Realm != Realm && (this.AI.Realm.Enemies.Contains(Realm) || this.AI.InvasionTargets.ContainsKey(Realm.ID));
		}

		public AIPlayer AI;

		public SovereigntyGame Game;

		private List<int> RealmStackIDs;

		private int CurrentStackID;
	}
}
