using System;
using System.Collections.Generic;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;

namespace SovereigntyTK.AI
{
	public class AIAction
	{
		public AIAction(AIActionTypes ActionName)
		{
			this.ActionName = ActionName;
		}

		internal AIActionTypes ActionName;

		internal List<UnitData> UnitTypes;

		internal Dictionary<UnitQueueItem, ActivePathNode> DeployTargets;

		internal List<UnitMoveData> MoveTargets;

		public WorkingRealm Realm;

		public WorkingProvince Province;

		public WorkingStack Stack;

		public List<WorkingUnit> Units;

		public ActivePathNode Node;

		public bool Completed;

		public RealmMagicData Spell;

		public SpellEffect SpellEffect;

		public object SpellTarget;

		public List<MarketTradeData> TradeList;

		public TradeOffer TradeOffer;

		public List<WorkingProvince> ProvinceList;

		public List<WorkingRealm> Realms;

		public BuildingEffect Building;

		public WorkingStack InterceptStack;
	}
}
