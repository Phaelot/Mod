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

		public override void Dispose()
		{
			base.Dispose();
		}

		public Dictionary<UnitQueueItem, ActivePathNode> DeployTargets;
	}
}
