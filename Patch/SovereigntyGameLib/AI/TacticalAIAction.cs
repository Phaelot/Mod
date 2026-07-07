using System;
using System.Collections.Generic;
using System.Drawing;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI
{
	public class TacticalAIAction
	{
		public TacticalAIAction(TacticalActionTypes Type)
		{
			this.Type = Type;
			this.Complete = false;
		}

		public void UpdateAction(SovereigntyGame Game, TacticalBattleController Battle, TacticalAIPlayer AI)
		{
			switch (this.Type)
			{
			case TacticalActionTypes.CardAction:
				if (!this.WaitingForCardResponse)
				{
					this.Card.CastEffect(this.CardTargets);
					GameText gameText = GameText.CreateLocalised("FORMAT_BATTLELOG_CARD", new object[0]);
					gameText.AddChildText(GameText.CreateLocalised(AI.Realm.DisplayName, new object[0]));
					gameText.AddChildText(GameText.CreateLocalised(this.Card.DisplayName, new object[0]));
					Game.GameCore.FireEvent("BattleLogEvent", new object[] { gameText });
					AI.Realm.BattleData.UsedCards.Add(this.Card);
					AI.Realm.BattleData.ActiveCards.Remove(this.Card);
					AI.Realm.BattleData.CardPlayed = true;
					Battle.UpdateShieldWallStatus();
					this.Complete = true;
					return;
				}
				break;
			case TacticalActionTypes.MoveAction:
			case TacticalActionTypes.FightAction:
			case TacticalActionTypes.HealAction:
				if (Battle.CurrentAction != null)
				{
					return;
				}
				this.Complete = true;
				break;
			case TacticalActionTypes.Retreat:
			case TacticalActionTypes.PackUnit:
			case TacticalActionTypes.UnpackUnit:
				break;
			default:
				return;
			}
		}

		public void ExecuteAction(SovereigntyGame Game, TacticalBattleController Battle, TacticalAIPlayer AI)
		{
			switch (this.Type)
			{
			case TacticalActionTypes.EndTurn:
				Battle.RequestEndTurn(AI.Realm, false);
				this.Complete = true;
				break;
			case TacticalActionTypes.CardAction:
				Game.GameCore.FireEvent("AIBattleCard", new object[] { this.Card });
				this.WaitingForCardResponse = true;
				this.Game = Game;
				Game.GameCore.RegisterEvent(new GenericDelegate(this.GameCore_OnScriptEvent), "AICardAcknowledged");
				break;
			case TacticalActionTypes.MoveAction:
				Battle.RequestMoveUnit(this.Unit, this.TargetTile, this.UnitActions);
				break;
			case TacticalActionTypes.FightAction:
				Battle.RequestAttackUnit(this.Unit, this.TargetUnit, this.UnitActions, this.ActionType);
				break;
			case TacticalActionTypes.Retreat:
				if (Game.CurrentTacticalBattle.RetreatingPlayer != null)
				{
					this.Complete = true;
					return;
				}
				Game.GameCore.MessageHandler.ShowInfoMessage("MSG_ENEMYRETREAT_TITLE", "MSG_ENEMYRETREAT_TEXT");
				Battle.RetreatPlayer(AI.Realm);
				this.Complete = true;
				break;
			case TacticalActionTypes.PackUnit:
				Battle.PackUnit(this.Unit, this.UnitType);
				this.Complete = true;
				break;
			case TacticalActionTypes.UnpackUnit:
				Battle.UnpackUnit(this.Unit);
				this.Complete = true;
				break;
			case TacticalActionTypes.HealAction:
				Battle.RequestAttackUnit(this.Unit, this.TargetUnit, this.UnitActions, CombatAction.Heal);
				break;
			}
			this.Executed = true;
		}

		private void GameCore_OnScriptEvent(string EventName, params object[] Args)
		{
			if (EventName == "AICardAcknowledged")
			{
				this.WaitingForCardResponse = false;
			}
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.GameCore_OnScriptEvent), "AICardAcknowledged");
		}

		public TacticalActionTypes Type;

		public CardEffect Card;

		public List<CardTargetData> CardTargets;

		public bool Complete;

		public bool Executed;

		private bool WaitingForCardResponse;

		public WorkingUnit Unit;

		public WorkingUnit TargetUnit;

		public CombatAction ActionType;

		public UnitActionData UnitActions;

		public Point TargetTile;

		public UnitData UnitType;

		public SovereigntyGame Game;
	}
}
