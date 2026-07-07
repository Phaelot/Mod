using System;
using System.Collections.Generic;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Utility
{
	public class MessageManager
	{
		public MessageManager(Sovereignty Game)
		{
			this.Game = Game;
		}

		public void ShowMessage(MessageBoxData Msg)
		{
			this.Game.FireEvent("DisplayMessage", new object[] { Msg });
		}

		public void ShowActionMessage(ActionMessageData Msg)
		{
			this.Game.FireEvent("AddActionMessage", new object[] { Msg });
		}

		public void HandleYes(MessageBoxData Msg)
		{
			MessageType msgType = Msg.MsgType;
			if (msgType <= MessageType.RetreatConfirm)
			{
				if (msgType != MessageType.WarPrompt)
				{
					if (msgType != MessageType.RetreatConfirm)
					{
						goto IL_05B9;
					}
					this.Game.CurrentGame.CurrentTacticalBattle.RetreatPlayer(this.Game.CurrentGame.PlayerRealm);
					goto IL_05B9;
				}
			}
			else
			{
				switch (msgType)
				{
				case MessageType.HeroHire:
					this.Game.Map.CurrentDeployHero = Msg.Hero;
					this.Game.Map.ChangeMode(MapModes.DeployHero, false);
					this.Game.FireEvent("HeroHired", new object[] { Msg.Hero });
					this.Game.CurrentGame.PlayerRealm.Gold.Value -= Msg.Gold;
					goto IL_05B9;
				case MessageType.HeroLevel:
					Msg.Hero.LegendaryAbilityName = Msg.Hero.AbilityOption1;
					Msg.Hero.Legendary = true;
					Msg.Hero.OwnerStack.UpdateSprite();
					this.Game.FireEvent("HeroPromoted", new object[] { Msg.Hero });
					goto IL_05B9;
				default:
					switch (msgType)
					{
					case MessageType.Disband:
						this.Game.CurrentGame.DestroyUnit(Msg.Unit);
						this.Game.FireEvent("UnitDisbanded", new object[] { Msg.Unit });
						goto IL_05B9;
					case MessageType.AttackConfirm:
						this.Game.CurrentGame.PlayerMoveManager.RequestMoveToNode(Msg.Stack, Msg.Node, false);
						goto IL_05B9;
					case MessageType.AttackIntercept:
					case MessageType.GameEnd:
					case MessageType.CampaignDecisionResult:
					case MessageType.TacticalBattle:
					case MessageType.AmbassadorConfirm:
					case MessageType.SaveConfirm:
					case MessageType.SpyConfirm:
					case MessageType.Tutorial:
					case MessageType.Revolt:
					case MessageType.Dispel:
					case MessageType.SpellConfirm:
					case MessageType.RecallConfirm:
					case MessageType.UpgradeFail:
						goto IL_05B9;
					case MessageType.Campaign:
						this.Game.CurrentGame.CurrentCampaign.HandleMessageYesClick(Msg);
						goto IL_05B9;
					case MessageType.TreatyBreak:
						this.Game.CurrentGame.AllianceController.BreakCurrentTreaty(this.Game.CurrentGame.PlayerRealm, Msg.Realm, true, false);
						goto IL_05B9;
					case MessageType.Liberate:
						this.Game.CurrentGame.WithdrawStack(Msg.Province.LandNode.CurrentStack);
						Msg.Province.OwnerRealm.DiplomacyManager.TriggerEvent(this.Game.CurrentGame.PlayerRealm, "LiberatedAlly");
						goto IL_05B9;
					case MessageType.DisbandQueue:
						Msg.QueueItem.Unit.OwnerRealm.CancelUnitTraining(Msg.QueueItem);
						goto IL_05B9;
					case MessageType.UpholdTreatyWar:
						break;
					case MessageType.EnslavePrisoners:
						this.Game.CurrentGame.PrisonerController.EnslaveUnits(Msg.UnitList, this.Game.CurrentGame.PlayerRealm);
						this.Game.FireEvent("PrisonersChanged", new object[0]);
						goto IL_05B9;
					case MessageType.ExecutePrisoners:
						this.Game.CurrentGame.PrisonerController.ExecuteUnits(Msg.UnitList, this.Game.CurrentGame.PlayerRealm);
						this.Game.FireEvent("PrisonersChanged", new object[0]);
						goto IL_05B9;
					case MessageType.SacrificePrisoners:
						this.Game.CurrentGame.PrisonerController.SacrificeUnits(Msg.UnitList, this.Game.CurrentGame.PlayerRealm);
						this.Game.FireEvent("PrisonersChanged", new object[0]);
						goto IL_05B9;
					case MessageType.ReleasePrisoners:
						this.Game.CurrentGame.PrisonerController.ReleaseUnits(Msg.UnitList, this.Game.CurrentGame.PlayerRealm);
						this.Game.FireEvent("PrisonersChanged", new object[0]);
						goto IL_05B9;
					case MessageType.RecruitPrisoners:
						this.Game.CurrentGame.PrisonerController.RecruitUnits(Msg.UnitList, this.Game.CurrentGame.PlayerRealm, false);
						this.Game.FireEvent("PrisonersChanged", new object[0]);
						goto IL_05B9;
					case MessageType.RaisePrisoners:
						this.Game.CurrentGame.PrisonerController.RecruitUnits(Msg.UnitList, this.Game.CurrentGame.PlayerRealm, true);
						this.Game.FireEvent("PrisonersChanged", new object[0]);
						goto IL_05B9;
					case MessageType.Demolish:
						Msg.Building.Demolish();
						Msg.Province.ConstructionState = ConstructionStates.Demolishing;
						this.Game.FireEvent("BuildingsChanged", new object[] { Msg.Province });
						goto IL_05B9;
					case MessageType.MoveConfirm:
						this.Game.CurrentGame.PlayerMoveManager.RequestMoveToNode(Msg.Stack, Msg.Node, false);
						goto IL_05B9;
					case MessageType.DisbandHero:
						this.Game.CurrentGame.DestroyHero(Msg.Hero);
						this.Game.FireEvent("HeroDisbanded", new object[] { Msg.Hero });
						goto IL_05B9;
					default:
						goto IL_05B9;
					}
					break;
				}
			}
			this.Game.CurrentGame.AllianceController.EstablishWar(this.Game.CurrentGame.PlayerRealm, Msg.Realm);
			if (Msg.Node != null && (!this.Game.CurrentGame.PlayerRealm.CodeOfWar || this.Game.CurrentGame.PlayerRealm.HasStatus("IgnoreCode", new object[0])))
			{
				this.Game.CurrentGame.PlayerMoveManager.RequestMoveToNode(Msg.Stack, Msg.Node, false);
			}
			if (Msg.Node != null)
			{
				this.Game.Map.RefreshMode();
			}
			IL_05B9:
			if (Msg.OnYesResponse != null)
			{
				Msg.OnYesResponse();
			}
		}

		public void HandleNo(MessageBoxData Msg)
		{
			MessageType msgType = Msg.MsgType;
			if (msgType <= MessageType.Campaign)
			{
				switch (msgType)
				{
				case MessageType.HeroHire:
					this.Game.CurrentGame.DestroyHero(Msg.Hero);
					break;
				case MessageType.HeroLevel:
					Msg.Hero.LegendaryAbilityName = Msg.Hero.AbilityOption2;
					Msg.Hero.Legendary = true;
					Msg.Hero.OwnerStack.UpdateSprite();
					this.Game.FireEvent("HeroPromoted", new object[] { Msg.Hero });
					break;
				default:
					if (msgType == MessageType.Campaign)
					{
						this.Game.CurrentGame.CurrentCampaign.HandleMessageNoClick(Msg);
					}
					break;
				}
			}
			else if (msgType != MessageType.Liberate)
			{
				if (msgType != MessageType.UpholdTreatyWar)
				{
					if (msgType == MessageType.SpyCaught)
					{
						Msg.Realm.DiplomacyManager.TriggerEvent(this.Game.CurrentGame.PlayerRealm, "SpyCaught");
						Msg.Agent.Recall();
						if (Msg.Agent.OwnerRealm.AIPlayer != null)
						{
							Msg.Agent.OwnerRealm.AIPlayer.EspionageManager.CancelAgentMission(Msg.Agent);
						}
					}
				}
				else
				{
					GameText gameText = GameText.CreateLocalised("MSG_TREATYLOST_TEXT", new object[0]);
					gameText.AddChildText(GameText.CreateLocalised(Msg.Ally.DisplayName, new object[0]));
					this.Game.MessageHandler.ShowInfoMessage(GameText.CreateLocalised("MSG_TREATYLOST_TITLE", new object[0]), gameText);
					this.Game.CurrentGame.AllianceController.BreakCurrentTreaty(this.Game.CurrentGame.PlayerRealm, Msg.Ally, true, false);
				}
			}
			else
			{
				Msg.Province.OwnerRealm.DiplomacyManager.TriggerEvent(this.Game.CurrentGame.PlayerRealm, "AnnexAlly");
				Msg.Province.OwnerRealm.AIPlayer.RelationsManager.RespondToFailedLiberate(this.Game.CurrentGame.PlayerRealm);
			}
			if (Msg.OnNoResponse != null)
			{
				Msg.OnNoResponse();
			}
		}

		public void HandleClose(MessageBoxData Msg)
		{
			MessageType msgType = Msg.MsgType;
			if (msgType == MessageType.Promotion)
			{
				foreach (KeyValuePair<WorkingUnit, int> keyValuePair in Msg.MedalChoices)
				{
					string text;
					if (keyValuePair.Value == 1)
					{
						text = keyValuePair.Key.GetFirstMedalName();
					}
					else
					{
						text = keyValuePair.Key.GetSecondMedalName();
					}
					UnitFlag unitFlag = UnitFlag.CreateNamedFlag(this.Game, text);
					keyValuePair.Key.GrantFlag(unitFlag);
					keyValuePair.Key.Medals++;
					keyValuePair.Key.MedalNames.Add(text);
					if (keyValuePair.Key.OwnerStack != null)
					{
						keyValuePair.Key.OwnerStack.AwardHeroXP(10);
					}
					this.Game.FireEvent("UnitPromoted", new object[] { keyValuePair.Key });
				}
				return;
			}
			switch (msgType)
			{
			case MessageType.AttackIntercept:
				this.Game.CurrentGame.StartBattle(Msg.Stack, Msg.InterceptStack, null);
				return;
			case MessageType.Campaign:
				this.Game.CurrentGame.CurrentCampaign.HandleMessageCancelClick(Msg);
				return;
			case MessageType.GameEnd:
				this.Game.EndGame();
				return;
			default:
				return;
			}
		}

		public void HandleChoice(MessageBoxData Msg, object Choice)
		{
			switch (Msg.DisplayType)
			{
			case MessageBoxType.Cradle:
			case MessageBoxType.Patron:
			{
				ArtScienceTypes artScienceTypes = (ArtScienceTypes)Choice;
				Msg.Province.Cradle = artScienceTypes;
				Msg.Province.UpdateCradleSprite();
				if ((artScienceTypes == ArtScienceTypes.PublicArt && Msg.Province.IsCapitol) || artScienceTypes == ArtScienceTypes.Statecraft)
				{
					foreach (WorkingRealm workingRealm in this.Game.CurrentGame.AllRealms.Values)
					{
						if (workingRealm != Msg.Province.OwnerRealm && workingRealm != this.Game.CurrentGame.RebelRealm)
						{
							workingRealm.DiplomacyManager.AdjustBaseValue(Msg.Province.OwnerRealm, 1f);
						}
					}
				}
				if (artScienceTypes == ArtScienceTypes.PublicArt && Msg.Province.IsCapitol)
				{
					Msg.Province.AILust -= 10;
				}
				this.Game.CurrentGame.PlayerRealm.CheckCradleEffects();
				if (Msg.DisplayType == MessageBoxType.Patron)
				{
					this.Game.FireEvent("PatronPlaced", new object[] { Msg.Province });
					return;
				}
				if (Msg.DisplayType == MessageBoxType.Cradle)
				{
					this.Game.FireEvent("CradlePlaced", new object[] { Msg.Province });
				}
				break;
			}
			case MessageBoxType.Tutorial:
				break;
			default:
				return;
			}
		}

		public void ShowAdvisorMessage(string Caption, string Text)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised(Caption, new object[0]);
			messageBoxData.MessageText = GameText.CreateLocalised(Text, new object[0]);
			messageBoxData.MsgType = MessageType.Tutorial;
			messageBoxData.DisplayType = MessageBoxType.Info;
			this.Game.FireEvent("ShowTutorialMessage", new object[] { messageBoxData });
		}

		public void ShowInfoMessage(GameText Caption, GameText Text)
		{
			this.ShowMessage(new MessageBoxData
			{
				CaptionText = Caption,
				MessageText = Text,
				MsgType = MessageType.GenericInfo,
				DisplayType = MessageBoxType.Info
			});
		}

		public void ShowInfoMessage(string CaptionString, string TextString)
		{
			this.ShowMessage(new MessageBoxData
			{
				CaptionText = GameText.CreateLocalised(CaptionString, new object[0]),
				MessageText = GameText.CreateLocalised(TextString, new object[0]),
				MsgType = MessageType.GenericInfo,
				DisplayType = MessageBoxType.Info
			});
		}

		private Sovereignty Game;
	}
}
