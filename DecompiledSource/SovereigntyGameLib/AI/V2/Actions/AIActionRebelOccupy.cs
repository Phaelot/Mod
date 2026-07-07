using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionRebelOccupy : AIAction
	{
		public AIActionRebelOccupy(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			this.Stack.NodeID = this.Province.LandNode.ID;
			this.Province.LandNode.CurrentStackID = this.Stack.ID;
			this.Stack.UpdateSprite();
			this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { this.Province });
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public WorkingProvince Province;

		public WorkingStack Stack;
	}
}
