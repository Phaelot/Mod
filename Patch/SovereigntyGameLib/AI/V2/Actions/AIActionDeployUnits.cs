using System;
using System.Collections.Generic;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionDeployUnits : AIAction
	{
		public AIActionDeployUnits(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			foreach (KeyValuePair<UnitQueueItem, ActivePathNode> keyValuePair in this.DeployTargets)
			{
				if (!this.AIQueueUnitCanDeployToNode(keyValuePair.Key.Unit, keyValuePair.Value))
				{
					continue;
				}
				if (keyValuePair.Value.CurrentStack != null)
				{
					if (keyValuePair.Value.CurrentStack.Units.Count((WorkingUnit x) => x.Class != UnitClasses.Fort) >= 20)
					{
						continue;
					}
				}
				this.Game.DeployUnit(keyValuePair.Key.Unit, keyValuePair.Value);
				this.AI.Realm.EndUnitTraining(keyValuePair.Key);
			}
			this.State = AiActionStates.Finished;
		}

		private bool AIQueueUnitCanDeployToNode(WorkingUnit Unit, ActivePathNode Node)
		{
			if (Unit == null || Node == null)
			{
				return false;
			}
			if (Unit.Class == UnitClasses.Naval)
			{
				return Node.NodeType == PathNodeTypes.Harbour || Node.NodeType == PathNodeTypes.RiverHarbour;
			}
			return Node.NodeType == PathNodeTypes.Land;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public Dictionary<UnitQueueItem, ActivePathNode> DeployTargets;
	}
}
