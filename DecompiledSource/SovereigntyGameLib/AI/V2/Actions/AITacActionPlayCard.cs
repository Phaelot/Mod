using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AITacActionPlayCard : AITacAction
	{
		public AITacActionPlayCard(AIActionManager Manager, SovereigntyGame Game, TacticalBattleController Battle)
			: base(Manager, Game, Battle)
		{
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleCardShown), "AICardAcknowledged");
		}

		private void HandleCardShown(string EventName, params object[] Args)
		{
			this.Card.CastEffect(this.CardTargets);
			GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_CARD", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(this.Card.DisplayName, new object[0]));
			this.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
			this.AI.Realm.BattleData.UsedCards.Add(this.Card);
			this.AI.Realm.BattleData.ActiveCards.Remove(this.Card);
			this.AI.Realm.BattleData.CardPlayed = true;
			this.Battle.UpdateShieldWallStatus();
			this.State = AiActionStates.Finished;
		}

		public override void Execute()
		{
			this.Game.GameCore.FireEvent("AIBattleCard", new object[] { this.Card });
			this.State = AiActionStates.Executing;
		}

		public override void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleCardShown), "AICardAcknowledged");
		}

		public CardEffect Card;

		public List<CardTargetData> CardTargets;
	}
}
