using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionAttack : AIAction
	{
		public AIActionAttack(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
		}

		private void HandleBattleEnd(string EventName, params object[] Args)
		{
			this.State = AiActionStates.Finished;
		}


		private bool NodeHasActiveFortifications(ActivePathNode Node)
		{
			if (Node == null || Node.Province == null)
			{
				return false;
			}
			if (Node.Province.FortLevel <= 0 || Node.Province.Forts == null)
			{
				return false;
			}
			int fortCount = Math.Min(Node.Province.FortLevel, Node.Province.Forts.Count);
			for (int i = 0; i < fortCount; i++)
			{
				WorkingUnit fort = Node.Province.Forts[i];
				if (fort != null && !fort.Disabled && (int)fort.Health > 0)
				{
					return true;
				}
			}
			return false;
		}

		private WorkingStack CreateFortDefenderStack()
		{
			WorkingRealm defenderRealm = this.Node.Province.OccupierRealm;
			if (defenderRealm == null)
			{
				defenderRealm = this.Node.Province.OwnerRealm;
			}
			return this.Game.CreateStack(defenderRealm.ID, this.Node.ID, false);
		}

		private void SelectHeroForAttackingArmy()
		{
			if (this.Stack == null || this.Stack.Hero == null || this.Units == null || this.Units.Count == 0)
			{
				return;
			}
			this.Stack.Hero.Selected = true;
		}

		private void TransferHeroToAttackStack(WorkingStack AttackStack)
		{
			if (this.Stack == null || this.Stack.Hero == null || AttackStack == null || AttackStack.Disposed || AttackStack.Hero != null)
			{
				return;
			}
			AttackStack.TransferHeroFromStack(this.Stack, this.Stack.Hero);
		}


		private void LogNavalAttackDebug(string Text)
		{
			try
			{
				string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs");
				if (!System.IO.Directory.Exists(folder))
				{
					System.IO.Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "naval_attack_debug.txt");
				System.IO.File.AppendAllText(file, System.DateTime.Now.ToString("HH:mm:ss.fff") + " [" + this.AI.Realm.Name + "] " + Text + "\r\n");
			}
			catch
			{
			}
		}

		private bool IsHarbourLikeNode(ActivePathNode Node)
		{
			return Node != null && (Node.NodeType == PathNodeTypes.Harbour || Node.NodeType == PathNodeTypes.RiverHarbour);
		}

		private bool HasLiveCurrentStack(ActivePathNode Node)
		{
			return Node != null && Node.CurrentStack != null && !Node.CurrentStack.Disposed && Node.CurrentStack.Units.Count > 0;
		}

		private string NodeText(ActivePathNode Node)
		{
			if (Node == null)
			{
				return "null";
			}
			string text = "node=" + Node.ID + " type=" + Node.NodeType.ToString();
			if (Node.Province != null)
			{
				text += " province=" + Node.Province.Name;
			}
			if (Node.Zone != null)
			{
				text += " zone=" + Node.Zone.Name;
			}
			return text;
		}

		private bool ValidateOrRedirectEmptyHarbourAttack()
		{
			if (!this.IsHarbourLikeNode(this.Node) || this.Node.Province == null || this.HasLiveCurrentStack(this.Node))
			{
				return true;
			}
			if (this.Stack == null || this.Stack.Node == null || !(this.Stack.Node.GetRegion() is WorkingZone))
			{
				return true;
			}
			if (this.Node.Province.OccupierRealm == this.AI.Realm)
			{
				return true;
			}
			ActivePathNode landNode = this.Node.Province.LandNode;
			List<WorkingUnit> landingUnits = new List<WorkingUnit>();
			if (this.Units != null && landNode != null)
			{
				foreach (WorkingUnit unit in this.Units)
				{
					if (unit != null && this.Game.DestinationChecker.NodeOKForUnit(unit, landNode) == UnitMoveResult.OK)
					{
						landingUnits.Add(unit);
					}
				}
			}
			if (landingUnits.Count > 0)
			{
				this.LogNavalAttackDebug("Redirecting empty harbour attack to land invasion: from=" + this.NodeText(this.Stack.Node) + " harbour=" + this.NodeText(this.Node) + " land=" + this.NodeText(landNode) + " units=" + landingUnits.Count);
				this.Node = landNode;
				this.Units = landingUnits;
				return true;
			}
			if (this.Units != null)
			{
				foreach (WorkingUnit unit2 in this.Units)
				{
					if (unit2 != null)
					{
						unit2.ClearMoves();
					}
				}
			}
			this.LogNavalAttackDebug("Blocked empty harbour attack with no landing-capable units: from=" + this.NodeText(this.Stack.Node) + " harbour=" + this.NodeText(this.Node) + " provinceOwner=" + ((this.Node.Province.OwnerRealm == null) ? "null" : this.Node.Province.OwnerRealm.Name));
			this.State = AiActionStates.Finished;
			return false;
		}

		public override void Execute()
		{
			WorkingRealm occupierRealm = this.Node.Province.OccupierRealm;
			if (this.AI.Realm.DiplomacyManager.GetRelation(occupierRealm) != RelationStates.War)
			{
				if (this.AI.WarManager.InvasionTargets.ContainsKey(occupierRealm.ID))
				{
					this.Game.AllianceController.EstablishWar(this.AI.Realm, occupierRealm);
				}
				else if (!this.IgnoreInvasionCheck)
				{
					throw new Exception("Attempted to invade invalid target realm");
				}
			}
			if (!this.ValidateOrRedirectEmptyHarbourAttack())
			{
				return;
			}
			Path path = this.Game.PathManager.GetPath(this.Stack.Node, this.Node, this.Units, false, this.Stack.Owner, false);
			if (this.Node.CurrentStack == null || this.Node.CurrentStack.Units.Count <= 0)
			{
				if (this.NodeHasActiveFortifications(this.Node))
				{
					if (this.Node.CurrentStack != null && this.Node.CurrentStack.Units.Count <= 0)
					{
						WorkingProvince staleDefenderProvince = (this.Node.CurrentStack.Node != null) ? this.Node.CurrentStack.Node.Province : null;
						this.Game.RemoveStack(this.Node.CurrentStack);
						if (staleDefenderProvince != null && !staleDefenderProvince.Occupied)
						{
							this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { staleDefenderProvince });
						}
					}
					WorkingStack fortStack = this.CreateFortDefenderStack();
					foreach (WorkingUnit workingUnit2 in this.Stack.Units)
					{
						workingUnit2.Selected = false;
					}
					foreach (WorkingUnit workingUnit3 in this.Units)
					{
						workingUnit3.Selected = true;
					}
					this.SelectHeroForAttackingArmy();
					this.Game.StartBattle(this.Stack, fortStack, path);
					if (this.Game.PendingBattle == null)
					{
						this.State = AiActionStates.Finished;
						return;
					}
					this.State = AiActionStates.Executing;
					return;
				}
				WorkingStack workingStack = this.Node.CurrentStack;
				if (workingStack == null || workingStack.Disposed)
				{
					workingStack = this.Game.CreateStack(this.AI.Realm.ID, this.Node.ID, true);
				}
				foreach (WorkingUnit workingUnit in this.Units)
				{
					if (workingStack.Disposed)
					{
						workingStack = this.Game.CreateStack(this.AI.Realm.ID, this.Node.ID, true);
					}
					this.Game.MoveUnit(this.Stack, workingStack, workingUnit, path, false);
					workingUnit.ClearMoves();
				}
				this.TransferHeroToAttackStack(workingStack);
				if (this.Stack.Units.Count == 0)
				{
					WorkingProvince attackSourceProvince = (this.Stack.Node != null) ? this.Stack.Node.Province : null;
					this.Game.RemoveStack(this.Stack);
					if (attackSourceProvince != null && !attackSourceProvince.Occupied)
					{
						this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { attackSourceProvince });
					}
				}
				if (this.Node.Province != null)
				{
					this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { this.Node.Province });
				}
				this.State = AiActionStates.Finished;
				return;
			}
			foreach (WorkingUnit workingUnit2 in this.Stack.Units)
			{
				workingUnit2.Selected = false;
			}
			foreach (WorkingUnit workingUnit3 in this.Units)
			{
				workingUnit3.Selected = true;
			}
			this.SelectHeroForAttackingArmy();
			this.Game.StartBattle(this.Stack, this.Node.CurrentStack, path);
			if (this.Game.PendingBattle == null)
			{
				this.State = AiActionStates.Finished;
				return;
			}
			this.State = AiActionStates.Executing;
		}

		public override void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleBattleEnd), "BattleCompleted");
			base.Dispose();
		}

		public WorkingProvince Province;

		public WorkingStack Stack;

		public WorkingRealm Realm;

		public List<WorkingUnit> Units;

		public ActivePathNode Node;

		public AutoBattleController AutoBattle;

		public TacticalBattleController TacticalBattle;

		public bool IgnoreInvasionCheck;
	}
}
