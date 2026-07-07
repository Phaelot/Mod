using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game
{
	public class AllianceController
	{
		public event RelationDelegate OnWarDeclared;

		public event RelationDelegate OnWarEnded;

		public event RelationDelegate OnAllianceStarted;

		public event RelationDelegate OnAllianceEnded;

		public AllianceController(SovereigntyGame Game)
		{
			this.Game = Game;
		}

		public void Dispose()
		{
			this.OnAllianceEnded = null;
			this.OnAllianceStarted = null;
			this.OnWarDeclared = null;
			this.OnWarEnded = null;
		}

		public void LoadSavedState(BinaryReader r, int SaveVersion)
		{
		}

		public void SaveCurrentState(BinaryWriter w)
		{
		}

		public bool TreatyIsPossible(WorkingRealm RealmA, WorkingRealm RealmB, TreatyTypes Treaty)
		{
			switch (Treaty)
			{
			case TreatyTypes.Alliance:
				return RealmA.Restrictions.AllowAlliances && RealmB.Restrictions.AllowAlliances && RealmA.Allies.Count < RealmA.AllyValue && RealmB.Allies.Count < RealmB.AllyValue && (RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.Peace || RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.NAP || RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.Defence);
			case TreatyTypes.NonAggression:
				return RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.Peace;
			case TreatyTypes.MutualDefence:
				return RealmA.Restrictions.AllowAlliances && RealmB.Restrictions.AllowAlliances && (RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.Peace || RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.NAP);
			case TreatyTypes.Peace:
				return RealmA.DiplomacyManager.GetRelation(RealmB) == RelationStates.War;
			default:
				return false;
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public virtual bool HasMutualEnemy(WorkingRealm realm1, WorkingRealm realm2)
		{
			foreach (WorkingRealm workingRealm in realm1.Enemies)
			{
				if (realm2.Enemies.Contains(workingRealm))
				{
					return true;
				}
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public virtual void EndWar(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			Realm1.DiplomacyManager.SetRelation(Realm2, RelationStates.ForcedPeace);
			Realm2.DiplomacyManager.SetRelation(Realm1, RelationStates.ForcedPeace);
			Realm1.WarTracker.StopTrackingWar(Realm2);
			Realm2.WarTracker.StopTrackingWar(Realm1);
			this.Game.GameCore.FireEvent("TickerMessage", new object[] { this.BuildTickerMessage("WAR_ENDED", Realm1, Realm2, null) });
			this.Game.GameCore.FireEvent("WarsChanged", new object[0]);
			if (Realm1 == this.Game.PlayerRealm || Realm2 == this.Game.PlayerRealm)
			{
				this.Game.GameCore.FireEvent("PlayerAlliesChanged", new object[0]);
			}
			this.Game.CleanupWar(Realm1, Realm2);
			Realm1.DiplomacyManager.DisableCondition(Realm2, "War");
			Realm2.DiplomacyManager.DisableCondition(Realm1, "War");
			if (this.OnWarEnded != null)
			{
				this.OnWarEnded(Realm1, Realm2);
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.Relations)
			{
				this.Game.GameCore.Map.RefreshMode();
			}
		}

		public void EstablishWar(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			bool flag = !Realm1.CodeOfWar;
			this.EstablishWar(Realm1, Realm2, flag);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public virtual void EstablishWar(WorkingRealm realm1, WorkingRealm realm2, bool Invasion)
		{
			this.BreakCurrentTreaty(realm1, realm2, true, false);
			realm1.DiplomacyManager.SetRelation(realm2, RelationStates.War);
			realm2.DiplomacyManager.SetRelation(realm1, RelationStates.War);
			realm1.WarTracker.BeginTrackingWar(realm2, Invasion, true);
			realm2.WarTracker.BeginTrackingWar(realm1, Invasion, false);
			realm2.DiplomacyManager.TriggerEvent(realm1, "DeclareWar");
			this.Game.GameCore.FireEvent("TickerMessage", new object[] { this.BuildTickerMessage("WAR_STARTED", realm1, realm2, null) });
			if (realm1 == this.Game.PlayerRealm || realm2 == this.Game.PlayerRealm)
			{
				this.Game.GameCore.FireEvent("PlayerAlliesChanged", new object[0]);
				this.Game.GameCore.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\footballcrowd_oh02.wav");
			}
			this.Game.GameCore.FireEvent("WarStarted", new object[] { realm1, realm2 });
			this.Game.GameCore.FireEvent("WarsChanged", new object[0]);
			if (realm2 == this.Game.PlayerRealm)
			{
				string text;
				string text2;
				if (realm1.CodeOfWar)
				{
					text = "MSG_WAR_TITLE";
					text2 = "MSG_WAR_TEXT";
				}
				else
				{
					text = "MSG_INVASION_TITLE";
					text2 = "MSG_INVASION_TEXT";
				}
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.CaptionText = GameText.CreateLocalised(text, new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised(text2, new object[0]);
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(realm1.DisplayName, new object[0]));
				messageBoxData.MsgType = MessageType.GenericInfo;
				messageBoxData.DisplayType = MessageBoxType.Info;
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
			this.CallAllies(realm1, realm2, false, true, !realm1.CodeOfWar);
			this.CallAllies(realm2, realm1, true, false, !realm1.CodeOfWar);
			realm1.DisbandAuxiliaries(realm2, this.Game);
			realm2.DisbandAuxiliaries(realm1, this.Game);
			realm1.DiplomacyManager.EnableCondition(realm2, "War");
			realm2.DiplomacyManager.EnableCondition(realm1, "War");
			if (this.OnWarDeclared != null)
			{
				this.OnWarDeclared(realm1, realm2);
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.Relations)
			{
				this.Game.GameCore.Map.RefreshMode();
			}
		}

		public void CallAllies(WorkingRealm Realm, WorkingRealm Target, bool IncludeMutualDefence, bool Aggressor, bool Invasion)
		{
			List<WorkingRealm> list = Realm.Allies.ToList<WorkingRealm>();
			if (IncludeMutualDefence)
			{
				list.AddRange(Realm.DefenceRealms);
			}
			foreach (WorkingRealm workingRealm in list)
			{
				if (!workingRealm.Enemies.Contains(Target) && !workingRealm.Allies.Contains(Target))
				{
					if (workingRealm == this.Game.PlayerRealm)
					{
						this.OfferPlayerWar(Realm, Target);
					}
					else if (workingRealm.AIPlayer.WarManager.ShouldJoinWar(Realm, Target))
					{
						if (!Aggressor)
						{
							this.EstablishWar(Target, workingRealm, Invasion);
						}
						else
						{
							this.EstablishWar(workingRealm, Target, Invasion);
						}
					}
					else
					{
						if (Realm == this.Game.PlayerRealm)
						{
							GameText gameText = GameText.CreateLocalised("MSG_AITREATYLOST_TEXT", new object[0]);
							gameText.AddChildText(GameText.CreateLocalised(workingRealm.DisplayName, new object[0]));
							this.Game.GameCore.MessageHandler.ShowInfoMessage(GameText.CreateLocalised("MSG_TREATYLOST_TITLE", new object[0]), gameText);
						}
						this.BreakCurrentTreaty(workingRealm, Realm, false, true);
					}
				}
			}
		}

		public void OfferPlayerWar(WorkingRealm Ally, WorkingRealm Target)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised("MSG_ALLYWAR_TITLE", new object[0]);
			List<GameText> list = new List<GameText>();
			GameText gameText = GameText.CreateLocalised("MSG_ALLYWAR_TEXT", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(Ally.DisplayName, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(Target.DisplayName, new object[0]));
			list.Add(gameText);
			List<WorkingUnit> auxiliaryUnits = this.Game.PlayerRealm.GetAuxiliaryUnits(Target);
			if (auxiliaryUnits.Count > 0)
			{
				GameText gameText2 = GameText.CreateLocalised("MSG_AUXILIARY_WARNING", new object[] { auxiliaryUnits.Count });
				gameText2.AddChildText(GameText.CreateLocalised(auxiliaryUnits[0].DisplayName, new object[0]));
				list.Add(gameText2);
			}
			messageBoxData.MessageTextList = list;
			messageBoxData.DisplayType = MessageBoxType.YesNo;
			messageBoxData.MsgType = MessageType.UpholdTreatyWar;
			messageBoxData.YesText = GameText.CreateLocalised("MSG_ALLYWAR_YES", new object[0]);
			messageBoxData.NoText = GameText.CreateLocalised("MSG_ALLYWAR_NO", new object[0]);
			messageBoxData.CustomData = "ALLYWAR";
			messageBoxData.Realm = Target;
			messageBoxData.Ally = Ally;
			this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
		}

		private TickerMessage BuildTickerMessage(string MessageKey, WorkingRealm Realm1, WorkingRealm Realm2, GameText TreatyName)
		{
			GameText gameText = GameText.CreateLocalised(MessageKey, new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(Realm1.DisplayName, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(Realm2.DisplayName, new object[0]));
			gameText.AddChildText(TreatyName);
			return new TickerMessage(gameText, TickerMessageType.Diplomacy, 10);
		}

		public virtual bool CanSeverAlliance(WorkingRealm realm1, WorkingRealm realm2, bool Forcedsever)
		{
			return !realm1.Restrictions.IsPermAlly(realm2) && !realm2.Restrictions.IsPermAlly(realm1) && realm1.DiplomacyManager.GetRelation(realm2) == RelationStates.Alliance && (Forcedsever || realm1.DiplomacyManager.GetRelationTime(realm2) >= 5);
		}

		public virtual bool CanOfferAlliance(WorkingRealm realm1, WorkingRealm realm2)
		{
			return realm1.Allies.Count < realm1.AllyValue && realm2.Allies.Count < realm2.AllyValue && realm1.Restrictions.AllowAlliances && realm2.Restrictions.AllowAlliances && (realm1.AllyValue <= 0 || realm1.Allies.Count < realm1.AllyValue) && (realm2.AllyValue <= 0 || realm2.Allies.Count < realm2.AllyValue) && (realm1.Restrictions.AllowedAlliances.Count <= 0 || realm1.Restrictions.AllowedAlliances.Contains(realm2.Name)) && (realm2.Restrictions.AllowedAlliances.Count <= 0 || realm2.Restrictions.AllowedAlliances.Contains(realm1.Name)) && (realm1.Restrictions.ForceAllowAlliance.Contains(realm2.Name) || realm2.Restrictions.ForceAllowAlliance.Contains(realm1.Name) || (realm1.DiplomacyManager.GetRelationTime(realm2) >= 2 && ((realm1.Name == "Cor Vilaad" && realm2.Name == "Brogen Hur") || (realm2.Name == "Cor Vilaad" && realm1.Name == "Brogen Hur") || (((!(realm1.Name == "Sirucil") && !(realm1.Name == "Cor Vilaad")) || realm2.Race != Races.Human) && ((!(realm2.Name == "Sirucil") && !(realm2.Name == "Cor Vilaad")) || realm1.Race != Races.Human) && ((!(realm1.Name == "Maledor") && !(realm1.Name == "Palemoor")) || realm2.Alignment != RealmAlignments.Good) && ((!(realm2.Name == "Maledor") && !(realm2.Name == "Palemoor")) || realm1.Alignment != RealmAlignments.Good) && (!(realm1.Name == "Ariselle") || realm2.PowerGroup != PowerGroup.First) && (!(realm2.Name == "Ariselle") || realm1.PowerGroup != PowerGroup.First) && ((realm1.Name == "Ariselle" && (realm2.Alignment != RealmAlignments.Evil || realm2.CodeOfWar)) || (realm2.Name == "Ariselle" && (realm1.Alignment != RealmAlignments.Evil || realm1.CodeOfWar)) || (realm1.DiplomacyManager.GetRelation(realm2) != RelationStates.War && realm1.DiplomacyManager.GetRelation(realm2) != RelationStates.ForcedPeace && (realm1 != this.Game.PlayerRealm || realm1.GetRelationsGold() >= 500) && (realm1.Alignment == realm2.Alignment || realm1.CodeOfWar == realm2.CodeOfWar)))))));
		}

		internal bool AnyTreatyIsPossible(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			return this.TreatyIsPossible(Realm1, Realm2, TreatyTypes.Alliance) || this.TreatyIsPossible(Realm1, Realm2, TreatyTypes.MutualDefence) || this.TreatyIsPossible(Realm1, Realm2, TreatyTypes.NonAggression);
		}

		internal TreatyTypes GetBestTreaty(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			if (this.TreatyIsPossible(Realm1, Realm2, TreatyTypes.NonAggression))
			{
				return TreatyTypes.NonAggression;
			}
			if (this.TreatyIsPossible(Realm1, Realm2, TreatyTypes.MutualDefence))
			{
				return TreatyTypes.MutualDefence;
			}
			if (this.TreatyIsPossible(Realm1, Realm2, TreatyTypes.Alliance))
			{
				return TreatyTypes.Alliance;
			}
			return TreatyTypes.Peace;
		}

		public void FormTreaty(WorkingRealm Realm1, WorkingRealm Realm2, TreatyTypes NewTreaty)
		{
			RelationStates relation = Realm1.DiplomacyManager.GetRelation(Realm2);
			RelationStates relationStates = RelationStates.Peace;
			switch (NewTreaty)
			{
			case TreatyTypes.Alliance:
				relationStates = RelationStates.Alliance;
				break;
			case TreatyTypes.NonAggression:
				relationStates = RelationStates.NAP;
				break;
			case TreatyTypes.MutualDefence:
				relationStates = RelationStates.Defence;
				break;
			}
			if (relation == RelationStates.War)
			{
				if (relationStates == RelationStates.Peace)
				{
					relationStates = RelationStates.ForcedPeace;
				}
				this.EndWar(Realm1, Realm2);
			}
			Realm1.DiplomacyManager.SetRelation(Realm2, relationStates);
			Realm2.DiplomacyManager.SetRelation(Realm1, relationStates);
			if (relation == RelationStates.War)
			{
				this.Game.CleanupWar(Realm1, Realm2);
			}
			if (relationStates == RelationStates.Alliance)
			{
				this.Game.GameCore.FireEvent("AlliancesChanged", new object[0]);
			}
			this.Game.GameCore.FireEvent("TickerMessage", new object[] { this.BuildTickerMessage("NEW_TREATY", Realm1, Realm2, this.GetTreatyName(relationStates)) });
			if ((Realm1 == this.Game.PlayerRealm || Realm2 == this.Game.PlayerRealm) && relationStates == RelationStates.Alliance)
			{
				this.Game.GameCore.FireEvent("PlayerAlliesChanged", new object[0]);
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.MsgType = MessageType.GenericInfo;
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.CaptionText = GameText.CreateLocalised("MSG_AUX_GAINED_TITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("MSG_AUX_GAINED_TEXT", new object[0]);
				if (Realm1 == this.Game.PlayerRealm)
				{
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Realm2.DisplayName, new object[0]));
					UnitData localAuxiliary = Realm2.UnitPurchaseManager.GetLocalAuxiliary();
					if (localAuxiliary != null)
					{
						messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(localAuxiliary.DisplayName, new object[0]));
					}
				}
				if (Realm2 == this.Game.PlayerRealm)
				{
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Realm1.DisplayName, new object[0]));
					UnitData localAuxiliary2 = Realm1.UnitPurchaseManager.GetLocalAuxiliary();
					if (localAuxiliary2 != null)
					{
						messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(localAuxiliary2.DisplayName, new object[0]));
					}
				}
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
			Realm1.DiplomacyManager.DisableCondition(Realm2, "Alliance");
			Realm2.DiplomacyManager.DisableCondition(Realm1, "Alliance");
			Realm1.DiplomacyManager.DisableCondition(Realm2, "Defence");
			Realm2.DiplomacyManager.DisableCondition(Realm1, "Defence");
			Realm1.DiplomacyManager.DisableCondition(Realm2, "NAP");
			Realm2.DiplomacyManager.DisableCondition(Realm1, "NAP");
			if (relationStates == RelationStates.Alliance)
			{
				Realm1.DiplomacyManager.EnableCondition(Realm2, "Alliance");
				Realm2.DiplomacyManager.EnableCondition(Realm1, "Alliance");
				this.Game.GameCore.FireEvent("AllianceFormed", new object[] { Realm1, Realm2 });
			}
			if (relationStates == RelationStates.Defence)
			{
				Realm1.DiplomacyManager.EnableCondition(Realm2, "Defence");
				Realm2.DiplomacyManager.EnableCondition(Realm1, "Defence");
			}
			if (relationStates == RelationStates.NAP)
			{
				Realm1.DiplomacyManager.EnableCondition(Realm2, "NAP");
				Realm2.DiplomacyManager.EnableCondition(Realm1, "NAP");
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.Relations)
			{
				this.Game.GameCore.Map.RefreshMode();
			}
		}

		public void BreakCurrentTreaty(WorkingRealm Realm1, WorkingRealm Realm2, bool ShowMessage = true, bool ForceServer = false)
		{
			RelationStates relation = Realm1.DiplomacyManager.GetRelation(Realm2);
			if (relation == RelationStates.Peace || relation == RelationStates.ForcedPeace)
			{
				return;
			}
			if (relation == RelationStates.Alliance && !this.CanSeverAlliance(Realm1, Realm2, ForceServer))
			{
				return;
			}
			Realm1.DiplomacyManager.SetRelation(Realm2, RelationStates.ForcedPeace);
			Realm2.DiplomacyManager.SetRelation(Realm1, RelationStates.ForcedPeace);
			Realm2.DiplomacyManager.TriggerEvent(Realm1, "BrokenTreaty");
			if (relation == RelationStates.Alliance)
			{
				this.Game.GameCore.FireEvent("AlliancesChanged", new object[0]);
			}
			this.Game.GameCore.FireEvent("TickerMessage", new object[] { this.BuildTickerMessage("END_TREATY", Realm1, Realm2, this.GetTreatyName(relation)) });
			if (Realm2 == this.Game.PlayerRealm)
			{
				GameText treatyName = this.GetTreatyName(relation);
				GameText gameText = GameText.CreateLocalised("MSG_TREATYEND_TITLE", new object[0]);
				GameText gameText2 = GameText.CreateLocalised("MSG_TREATYEND_TEXT", new object[0]);
				gameText2.AddChildText(GameText.CreateLocalised(Realm1.DisplayName, new object[0]));
				gameText2.AddChildText(treatyName);
				this.Game.GameCore.MessageHandler.ShowInfoMessage(gameText, gameText2);
			}
			if ((Realm1 == this.Game.PlayerRealm || Realm2 == this.Game.PlayerRealm) && relation == RelationStates.Alliance)
			{
				this.Game.GameCore.FireEvent("PlayerAlliesChanged", new object[0]);
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.MsgType = MessageType.GenericInfo;
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.CaptionText = GameText.CreateLocalised("MSG_AUX_LOST_TITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("MSG_AUX_LOST_TEXT", new object[0]);
				if (Realm1 == this.Game.PlayerRealm)
				{
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Realm2.DisplayName, new object[0]));
					UnitData localAuxiliary = Realm2.UnitPurchaseManager.GetLocalAuxiliary();
					if (localAuxiliary != null)
					{
						messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(localAuxiliary.DisplayName, new object[0]));
					}
				}
				if (Realm2 == this.Game.PlayerRealm)
				{
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(Realm1.DisplayName, new object[0]));
					UnitData localAuxiliary2 = Realm1.UnitPurchaseManager.GetLocalAuxiliary();
					if (localAuxiliary2 != null)
					{
						messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(localAuxiliary2.DisplayName, new object[0]));
					}
				}
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
			if (relation == RelationStates.Alliance)
			{
				this.Game.CleanupAlliance(Realm1, Realm2);
				Realm1.DiplomacyManager.DisableCondition(Realm2, "Alliance");
				Realm2.DiplomacyManager.DisableCondition(Realm1, "Alliance");
				if (this.OnAllianceEnded != null)
				{
					this.OnAllianceEnded(Realm1, Realm2);
				}
			}
			if (relation == RelationStates.Defence)
			{
				Realm1.DiplomacyManager.DisableCondition(Realm2, "Defence");
				Realm2.DiplomacyManager.DisableCondition(Realm1, "Defence");
			}
			if (relation == RelationStates.NAP)
			{
				Realm1.DiplomacyManager.DisableCondition(Realm2, "NAP");
				Realm2.DiplomacyManager.DisableCondition(Realm1, "NAP");
			}
			if (this.Game.GameCore.Map.CurrentMode == MapModes.Relations)
			{
				this.Game.GameCore.Map.RefreshMode();
			}
		}

		public GameText GetTreatyName(RelationStates Relation)
		{
			switch (Relation)
			{
			case RelationStates.Alliance:
				return GameText.CreateLocalised("TREATY_ALLY", new object[0]);
			case RelationStates.Defence:
				return GameText.CreateLocalised("TREATY_DEFENCE", new object[0]);
			case RelationStates.NAP:
				return GameText.CreateLocalised("TREATY_NAP", new object[0]);
			case RelationStates.Peace:
				return GameText.CreateLocalised("TREATY_PEACE", new object[0]);
			case RelationStates.ForcedPeace:
				return GameText.CreateLocalised("TREATY_FORCED", new object[0]);
			default:
				return GameText.CreateLocalised("TREATY_UNKNOWN", new object[0]);
			}
		}

		public GameText GetTreatyName(TreatyTypes Treaty)
		{
			switch (Treaty)
			{
			case TreatyTypes.Alliance:
				return GameText.CreateLocalised("TREATY_ALLY", new object[0]);
			case TreatyTypes.NonAggression:
				return GameText.CreateLocalised("TREATY_NAP", new object[0]);
			case TreatyTypes.MutualDefence:
				return GameText.CreateLocalised("TREATY_DEFENCE", new object[0]);
			case TreatyTypes.Peace:
				return GameText.CreateLocalised("TREATY_PEACE", new object[0]);
			default:
				return GameText.CreateLocalised("TREATY_UNKNOWN", new object[0]);
			}
		}

		internal void ForcePeace(WorkingRealm OriginRealm, WorkingRealm TargetRealm)
		{
			OriginRealm.DiplomacyManager.SetRelation(TargetRealm, RelationStates.ForcedPeace);
			TargetRealm.DiplomacyManager.SetRelation(OriginRealm, RelationStates.ForcedPeace);
		}

		public const int MIN_ALLIANCE_TURNS = 5;

		public const int MIN_ALLIANCE_OFFER_TURNS = 5;

		public const int ALLIANCE_COST = 500;

		public const int MESSAGE_PRIORITY = 10;

		public const TickerMessageType MESSAGE_TYPE = TickerMessageType.Diplomacy;

		public const string MESSAGE_KEY_ESTABLISH_ALLIANCE = "ESTABLISH_ALLIANCE";

		public const string MESSAGE_KEY_END_ALLIANCE = "END_ALLIANCE";

		public const string MESSAGE_KEY_ESTABLISH_WAR = "ESTABLISH_WAR";

		public const string MESSAGE_KEY_END_WAR = "END_WAR";

		private SovereigntyGame Game;
	}
}
