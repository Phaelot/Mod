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
			try
			{
				if (this.Card != null)
				{
					try
					{
						this.Card.CastEffect(this.CardTargets);
					}
					catch (NullReferenceException ex)
					{
						// Stale tile/unit between AI target pick and acknowledgement; the card is still consumed.
						if (this.AI != null)
						{
							this.AI.Log("Card " + this.Card.Name + " failed to cast: " + ex.Message + "\n" + ex.StackTrace);
						}
					}
				}

				// CastEffect can end the battle (last unit killed/removed). Realm.BattleData
				// and unit BattleData may already be cleared — finish safely.
				if (this.AI != null && this.AI.Realm != null && this.AI.Realm.BattleData != null && this.Card != null)
				{
					GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_CARD", new object[0]);
					gameText.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
					gameText.AddChildText(GameText.CreateLocalised(this.Card.DisplayName, new object[0]));
					this.Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
					this.AI.Realm.BattleData.UsedCards.Add(this.Card);
					this.AI.Realm.BattleData.ActiveCards.Remove(this.Card);
					this.AI.Realm.BattleData.CardPlayed = true;
				}

				if (this.Battle != null && !this.Battle.BattleEnded)
				{
					this.Battle.UpdateShieldWallStatus();
				}
			}
			finally
			{
				this.State = AiActionStates.Finished;
			}
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
