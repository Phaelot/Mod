using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.Game.Trackers
{
	public class NodeUnitTracker
	{
		public event UnitDelegate OnUnitEnterNode;

		public event UnitDelegate OnUnitLeaveNode;

		public NodeUnitTracker(SovereigntyGame Game, ActivePathNode Node)
		{
			this.Game = Game;
			this.Node = Node;
			Game.OnUnitNodeChanged += this.Game_OnUnitNodeChanged;
			this.CurrentUnits = new List<int>();
		}

		public void Init()
		{
			if (this.Node.CurrentStack != null)
			{
				foreach (WorkingUnit workingUnit in this.Node.CurrentStack.Units)
				{
					this.Game_OnUnitNodeChanged(workingUnit.ID, this.Node.ID);
				}
			}
			foreach (int num in this.Node.AllyStacks)
			{
				WorkingStack workingStack = this.Game.AllStacks[num];
				foreach (WorkingUnit workingUnit2 in workingStack.Units)
				{
					this.Game_OnUnitNodeChanged(workingUnit2.ID, this.Node.ID);
				}
			}
		}

		public void Dispose()
		{
			this.Game.OnUnitNodeChanged -= this.Game_OnUnitNodeChanged;
			if (this.Node.CurrentStack != null)
			{
				foreach (WorkingUnit workingUnit in this.Node.CurrentStack.Units)
				{
					this.Game_OnUnitNodeChanged(workingUnit.ID, -1);
				}
			}
			foreach (int num in this.Node.AllyStacks)
			{
				WorkingStack workingStack = this.Game.AllStacks[num];
				foreach (WorkingUnit workingUnit2 in workingStack.Units)
				{
					this.Game_OnUnitNodeChanged(workingUnit2.ID, -1);
				}
			}
			this.OnUnitEnterNode = null;
			this.OnUnitLeaveNode = null;
		}

		private void Game_OnUnitNodeChanged(int UnitID, int NodeID)
		{
			WorkingUnit workingUnit = null;
			this.Game.AllUnits.TryGetValue(UnitID, out workingUnit);
			if (workingUnit == null)
			{
				return;
			}
			if (NodeID == this.Node.ID)
			{
				if (this.OnUnitEnterNode != null)
				{
					this.OnUnitEnterNode(workingUnit);
				}
				this.CurrentUnits.Add(UnitID);
				return;
			}
			if (this.CurrentUnits.Contains(UnitID))
			{
				if (this.OnUnitLeaveNode != null)
				{
					this.OnUnitLeaveNode(workingUnit);
				}
				this.CurrentUnits.Remove(UnitID);
			}
		}

		private SovereigntyGame Game;

		private ActivePathNode Node;

		private List<int> CurrentUnits;
	}
}
