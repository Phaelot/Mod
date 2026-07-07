using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionConstructBuilding : AIAction
	{
		public AIActionConstructBuilding(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
		}

		public override void Execute()
		{
			BuildingData buildingData = null;
			this.Game.GameCore.Data.Buildings.TryGetValue(this.BuildingName, out buildingData);
			WorkingProvince province = this.Game.GetProvince(this.ProvinceName);
			if (buildingData == null)
			{
				return;
			}
			if (province == null)
			{
				return;
			}
			BuildingEffect buildingEffect = BuildingEffect.CreateEffect(this.Game, buildingData, province);
			buildingEffect.Construct(province.OwnerRealm, province, true);
			province.ConstructionState = ConstructionStates.Building;
			this.State = AiActionStates.Finished;
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		public string BuildingName;

		public string ProvinceName;
	}
}
