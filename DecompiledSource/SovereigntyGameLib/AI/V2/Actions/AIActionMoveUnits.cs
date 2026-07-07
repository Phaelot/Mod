using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionMoveUnits : AIAction
	{
		public AIActionMoveUnits(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			foreach (UnitMoveData unitMoveData in this.MoveTargets)
			{
				WorkingStack workingStack = unitMoveData.TargetNode.GetRealmStack(this.AI.Realm);
				WorkingStack ownerStack = unitMoveData.Unit.OwnerStack;
				if (workingStack == null || workingStack.Disposed)
				{
					workingStack = this.Game.CreateStack(this.AI.Realm.ID, unitMoveData.TargetNode.ID, false);
					if (unitMoveData.TargetNode.Province != null && !unitMoveData.TargetNode.Province.Occupied && unitMoveData.TargetNode.Province.OwnerRealm != this.AI.Realm)
					{
						unitMoveData.TargetNode.AllyStacks.Add(workingStack.ID);
					}
					else
					{
						unitMoveData.TargetNode.CurrentStackID = workingStack.ID;
					}
				}
				this.Game.MoveUnit(ownerStack, workingStack, unitMoveData.Unit, unitMoveData.MovePath, false);
				unitMoveData.Unit.ClearMoves();
				if (ownerStack.Units.Count == 0)
				{
					if (ownerStack.Node != null && ownerStack.Node.Province != null && !ownerStack.Node.Province.Occupied)
					{
						this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { ownerStack.Node.Province });
					}
					this.Game.RemoveStack(ownerStack);
				}
				if (workingStack.Units.Count == 0)
				{
					if (workingStack.Node != null && workingStack.Node.Province != null && !workingStack.Node.Province.Occupied)
					{
						this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingStack.Node.Province });
					}
					this.Game.RemoveStack(workingStack);
				}
			}
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public List<UnitMoveData> MoveTargets;
	}
}
