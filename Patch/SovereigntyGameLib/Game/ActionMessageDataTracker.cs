using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game
{
	internal class ActionMessageDataTracker
	{
		public ActionMessageDataTracker(SovereigntyGame Game)
		{
			this.Game = Game;
			this.GainedProvinces = new List<int>();
			this.LostProvinces = new List<int>();
			this.OccupiedProvinces = new List<int>();
			this.PlagueProvinces = new List<int>();
			this.TrainedUnits = new List<int>();
			this.DisbandedUnits = new Dictionary<string, Dictionary<string, int>>();
			this.TradesArrived = new List<Tuple<WorkingRealm, TradeOfferList>>();
			this.DestroyedRealms = new List<Tuple<WorkingRealm, WorkingRealm>>();
			this.IncitedProvinces = new List<int>();
			this.Reset();
			Game.GameCore.RegisterEvents(new GenericDelegate(this.GameCore_OnScriptEvent), new string[]
			{
				"ProvinceIncited", "TradeArrived", "UnitsDisbanded", "ProvinceOwnerChanged", "HarvestFaire", "PlagueSpread", "CradlePlaced", "PatronPlaced", "RealmDestroyed", "MagicLevelGained",
				"AIOccupied", "PowerGroupChanged"
			});
		}

		public void Dispose()
		{
			this.Game.GameCore.UnregisterEvents(new GenericDelegate(this.GameCore_OnScriptEvent), new string[]
			{
				"ProvinceIncited", "TradeArrived", "UnitsDisbanded", "ProvinceOwnerChanged", "HarvestFaire", "PlagueSpread", "CradlePlaced", "PatronPlaced", "RealmDestroyed", "MagicLevelGained",
				"AIOccupied", "PowerGroupChanged"
			});
		}

		private void GameCore_OnScriptEvent(string EventName, params object[] Args)
		{
			if (EventName == "ProvinceIncited")
			{
				WorkingProvince workingProvince = Args[0] as WorkingProvince;
				if (workingProvince.OwnerRealm == this.Game.PlayerRealm)
				{
					this.IncitedProvinces.Add(workingProvince.ID);
				}
			}
			if (EventName == "TradeArrived")
			{
				WorkingRealm workingRealm = this.Game.AllRealms[(int)Args[0]];
				TradeOfferList tradeOfferList = (TradeOfferList)Args[1];
				if (tradeOfferList.GoldLump == 0 && tradeOfferList.GetResourceLump().Count == 0)
				{
					return;
				}
				this.TradesArrived.Add(new Tuple<WorkingRealm, TradeOfferList>(workingRealm, tradeOfferList));
			}
			if (EventName == "UnitsDisbanded")
			{
				WorkingRealm workingRealm2 = (WorkingRealm)Args[0];
				if (workingRealm2 == this.Game.PlayerRealm)
				{
					this.DisbandedUnits = (Dictionary<string, Dictionary<string, int>>)Args[1];
				}
			}
			if (EventName == "ProvinceOwnerChanged")
			{
				WorkingProvince workingProvince2 = Args[0] as WorkingProvince;
				WorkingRealm workingRealm3 = Args[1] as WorkingRealm;
				WorkingRealm workingRealm4 = Args[2] as WorkingRealm;
				if (workingRealm3 == this.Game.PlayerRealm && !this.GainedProvinces.Contains(workingProvince2.ID))
				{
					this.GainedProvinces.Add(workingProvince2.ID);
				}
				if (workingRealm4 == this.Game.PlayerRealm && !this.LostProvinces.Contains(workingProvince2.ID))
				{
					this.LostProvinces.Add(workingProvince2.ID);
				}
			}
			if (EventName == "HarvestFaire")
			{
				this.HarvestFaire = (int)Args[0];
				this.HarvestFaireChampion = (WorkingUnit)Args[1];
			}
			if (EventName == "PlagueSpread")
			{
				this.PlagueProvinces.Add(((WorkingProvince)Args[0]).ID);
			}
			if (EventName == "CradlePlaced" || EventName == "PatronPlaced")
			{
				WorkingProvince workingProvince3 = (WorkingProvince)Args[0];
				if (workingProvince3.OwnerRealm == this.Game.PlayerRealm)
				{
					return;
				}
				if (workingProvince3.HasScience())
				{
					this.CradleProvince = workingProvince3.ID;
				}
				if (workingProvince3.HasArts())
				{
					this.PatronProvince = workingProvince3.ID;
				}
			}
			if (EventName == "RealmDestroyed")
			{
				WorkingRealm workingRealm5 = (WorkingRealm)Args[0];
				WorkingRealm workingRealm6 = (WorkingRealm)Args[1];
				this.DestroyedRealms.Add(new Tuple<WorkingRealm, WorkingRealm>(workingRealm5, workingRealm6));
			}
			if (EventName == "MagicLevelGained" && (WorkingRealm)Args[0] == this.Game.PlayerRealm)
			{
				this.LevelGained = true;
			}
			if (EventName == "AIOccupied")
			{
				this.OccupiedProvinces.Add((int)Args[0]);
			}
			if (EventName == "PowerGroupChanged")
			{
				this.PowerChanged = true;
				this.OldGroup = (PowerGroup)Args[0];
				this.NewGroup = (PowerGroup)Args[1];
			}
		}

		public void Reset()
		{
			this.GainedProvinces.Clear();
			this.LostProvinces.Clear();
			this.OccupiedProvinces.Clear();
			this.PlagueProvinces.Clear();
			this.TrainedUnits.Clear();
			this.DisbandedUnits.Clear();
			this.TradesArrived.Clear();
			this.IncitedProvinces.Clear();
			this.HarvestFaire = -1;
			this.CradleProvince = -1;
			this.PatronProvince = -1;
			this.DestroyedRealms.Clear();
			this.LevelGained = false;
			this.PowerChanged = false;
		}

		private string GetRealmDestroyedImageFile(WorkingRealm Realm)
		{
			if (Realm == null)
			{
				return "Data\\Images\\Events\\RealmDestroyed\\generic.png";
			}
			switch (Realm.Race)
			{
			case Races.Human:
				return "Data\\Images\\Events\\RealmDestroyed\\human.png";
			case Races.Elf:
				return "Data\\Images\\Events\\RealmDestroyed\\elf.png";
			case Races.Dwarf:
				return "Data\\Images\\Events\\RealmDestroyed\\dwarf.png";
			case Races.Undead:
				return "Data\\Images\\Events\\RealmDestroyed\\undead.png";
			case Races.Orc:
				return "Data\\Images\\Events\\RealmDestroyed\\orc.png";
			case Races.Monster:
				return "Data\\Images\\Events\\RealmDestroyed\\monster.png";
			case Races.Giant:
				return "Data\\Images\\Events\\RealmDestroyed\\giant.png";
			case Races.Dragon:
				return "Data\\Images\\Events\\RealmDestroyed\\dragon.png";
			case Races.Outcast:
				return "Data\\Images\\Events\\RealmDestroyed\\outcast.png";
			default:
				return "Data\\Images\\Events\\RealmDestroyed\\generic.png";
			}
		}

		private string GetRealmDisplayText(WorkingRealm Realm)
		{
			if (Realm == null)
			{
				return "Unknown Realm";
			}
			try
			{
				return GameText.CreateLocalised(Realm.DisplayName, new object[0]).GetActualText(this.Game.GameCore);
			}
			catch
			{
				if (!string.IsNullOrEmpty(Realm.Name))
				{
					return Realm.Name;
				}
				return Realm.DisplayName;
			}
		}

		private string GetRealmDestroyedFlavorText(WorkingRealm DefeatedRealm, WorkingRealm VictorRealm, bool DiplomaticOffense)
		{
			string defeatedName = this.GetRealmDisplayText(DefeatedRealm);
			string victorName = this.GetRealmDisplayText(VictorRealm);
			Races defeatedRace = (DefeatedRealm != null) ? DefeatedRealm.Race : Races.None;
			Races victorRace = (VictorRealm != null) ? VictorRealm.Race : Races.None;

			string opening;
			switch (victorRace)
			{
			case Races.Human:
				opening = "The banners of " + victorName + " fly over the broken realm of " + defeatedName + ".";
				break;
			case Races.Elf:
				opening = victorName + " has ended " + defeatedName + " with blade, bow, and old wrath.";
				break;
			case Races.Dwarf:
				opening = victorName + " has cast down " + defeatedName + "; its victory is carved in iron and stone.";
				break;
			case Races.Undead:
				opening = defeatedName + " has fallen to " + victorName + ", and the silence of the grave follows.";
				break;
			case Races.Orc:
				opening = "The warbands of " + victorName + " have smashed the gates of " + defeatedName + ".";
				break;
			case Races.Monster:
				opening = defeatedName + " has been torn apart by the monstrous host of " + victorName + ".";
				break;
			case Races.Giant:
				opening = victorName + " has crushed " + defeatedName + " beneath giant foot and broken stone.";
				break;
			case Races.Dragon:
				opening = defeatedName + " has burned beneath the shadow of " + victorName + ".";
				break;
			case Races.Outcast:
				opening = "The outcasts of " + victorName + " have dragged " + defeatedName + " from the map.";
				break;
			default:
				opening = defeatedName + " has been destroyed by " + victorName + ".";
				break;
			}

			string aftermath;
			switch (defeatedRace)
			{
			case Races.Human:
				aftermath = "Its crown lies broken, and its towns close their gates in fear.";
				break;
			case Races.Elf:
				aftermath = "Its ancient groves fall silent, and old songs turn to mourning.";
				break;
			case Races.Dwarf:
				aftermath = "Its halls echo without hammers, and the hold-fires grow cold.";
				break;
			case Races.Undead:
				aftermath = "Its grave-pacts are shattered, and restless dead scatter without command.";
				break;
			case Races.Orc:
				aftermath = "Its war drums are still, and rival chiefs already circle the ruins.";
				break;
			case Races.Monster:
				aftermath = "Its lairs are emptied, and the wild things flee from the victors.";
				break;
			case Races.Giant:
				aftermath = "Its great halls stand abandoned beneath toppled stones.";
				break;
			case Races.Dragon:
				aftermath = "Its hoards lie exposed, and smoke fades from the high aeries.";
				break;
			case Races.Outcast:
				aftermath = "Its scattered people vanish into roads, hills, and borderlands.";
				break;
			default:
				aftermath = "Its banners are cast down, and its lands pass into another age.";
				break;
			}

			string diplomacy = DiplomaticOffense ? "The courts of the world call this a grievous offense." : "Few courts will protest this sanctioned destruction.";
			return opening + " " + aftermath + " " + diplomacy;
		}

		public void CheckPromotions()
		{
			List<WorkingHero> list = this.Game.PlayerRealm.Heroes.Where((WorkingHero x) => !x.Legendary && x.XP >= 100).ToList<WorkingHero>();
			foreach (WorkingHero workingHero in list)
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.CaptionText = GameText.CreateLocalised("HEROLEVELMSGTITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("HEROLEVELMSGTEXT", new object[0]);
				HeroAbilityData heroAbilityData = this.Game.Data.HeroAbilities[workingHero.BaseAbility];
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(workingHero.DisplayName, new object[0]));
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(heroAbilityData.DisplayDesc, new object[0]));
				messageBoxData.MsgType = MessageType.HeroLevel;
				HeroAbilityData heroAbilityData2 = this.Game.Data.HeroAbilities[workingHero.AbilityOption1];
				HeroAbilityData heroAbilityData3 = this.Game.Data.HeroAbilities[workingHero.AbilityOption2];
				messageBoxData.NoText = GameText.CreateLocalised(heroAbilityData3.DisplayName, new object[0]);
				messageBoxData.YesText = GameText.CreateLocalised(heroAbilityData2.DisplayName, new object[0]);
				messageBoxData.YesTT = GameText.CreateLocalised(heroAbilityData2.DisplayDesc, new object[0]);
				messageBoxData.NoTT = GameText.CreateLocalised(heroAbilityData3.DisplayDesc, new object[0]);
				messageBoxData.DisplayType = MessageBoxType.YesNo;
				messageBoxData.Hero = workingHero;
				GameText gameText = GameText.CreateLocalised("SIDEBARHEROPROMOTETITLE", new object[0]);
				GameText gameText2 = GameText.CreateLocalised("SIDEBARHEROPROMOTEMSG", new object[0]);
				ActionMessageData actionMessageData = new ActionMessageData(gameText, gameText2, true, "DisplayMessage", workingHero.OwnerStack.Node.MapCoords, new object[] { messageBoxData });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData);
			}
			List<WorkingUnit> list2 = this.Game.PlayerRealm.Units.Where((WorkingUnit x) => x.ReadytoPromote()).ToList<WorkingUnit>();
			if (list2.Count > 0)
			{
				GameText gameText3 = GameText.CreateLocalised("SIDEBARPROMOTETITLE", new object[0]);
				GameText gameText4 = GameText.CreateLocalised("SIDEBARPROMOTEMSG", new object[0]);
				MessageBoxData messageBoxData2 = new MessageBoxData();
				messageBoxData2.CaptionText = GameText.CreateLocalised("PROMOTEMSGTITLE", new object[0]);
				messageBoxData2.MessageText = GameText.CreateLocalised("PROMOTEMSGTEXT", new object[0]);
				messageBoxData2.DisplayType = MessageBoxType.Promotion;
				messageBoxData2.MsgType = MessageType.Promotion;
				messageBoxData2.UnitList = list2;
				ActionMessageData actionMessageData2 = new ActionMessageData(gameText3, gameText4, true, "DisplayMessage", Point.Empty, new object[] { messageBoxData2 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData2);
			}
		}

		public void ShowMessages()
		{
			this.CheckPromotions();
			IList<UnitQueueItem> currentUnitQueue = this.Game.PlayerRealm.GetCurrentUnitQueue();
			if (currentUnitQueue.Count((UnitQueueItem x) => x.TurnsLeft == 0) > 0)
			{
				GameText gameText = GameText.CreateLocalised("SIDEBARDEPLOYTITLE", new object[0]);
				GameText gameText2 = GameText.CreateLocalised("SIDEBARDEPLOYMSG", new object[0]);
				ActionMessageData actionMessageData = new ActionMessageData(gameText, gameText2, false, "OpenPanel", Point.Empty, new object[] { "Queue" });
				actionMessageData.ActionType = "ArmyPanel";
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData);
			}
			if (this.PowerChanged)
			{
				GameText gameText3 = null;
				GameText gameText4 = null;
				if (this.NewGroup == PowerGroup.First)
				{
					gameText3 = GameText.CreateLocalised("EVENT_FIRSTPOWER_TITLE", new object[0]);
					gameText4 = GameText.CreateLocalised("EVENT_FIRSTPOWER", new object[0]);
				}
				if (this.NewGroup == PowerGroup.Great)
				{
					gameText3 = GameText.CreateLocalised("EVENT_GREATPOWER_TITLE", new object[0]);
					if (this.OldGroup == PowerGroup.First)
					{
						gameText4 = GameText.CreateLocalised("EVENT_GREATPOWER_DOWN", new object[0]);
					}
					else
					{
						gameText4 = GameText.CreateLocalised("EVENT_GREATPOWER_UP", new object[0]);
					}
				}
				if (this.NewGroup == PowerGroup.Average)
				{
					gameText3 = GameText.CreateLocalised("EVENT_AVERAGEPOWER_TITLE", new object[0]);
					if (this.OldGroup == PowerGroup.Minor)
					{
						gameText4 = GameText.CreateLocalised("EVENT_AVERAGEPOWER_UP", new object[0]);
					}
					else
					{
						gameText4 = GameText.CreateLocalised("EVENT_AVERAGEPOWER_DOWN", new object[0]);
					}
				}
				if (this.NewGroup == PowerGroup.Minor)
				{
					gameText3 = GameText.CreateLocalised("EVENT_MINORPOWER_TITLE", new object[0]);
					gameText4 = GameText.CreateLocalised("EVENT_MINORPOWER", new object[0]);
				}
				ActionMessageData actionMessageData2 = new ActionMessageData(gameText3, gameText4, false, "OpenPanel", Point.Empty, new object[] { "Rankings" });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData2);
			}
			if (this.DisbandedUnits.Count > 0)
			{
				GameText gameText5 = GameText.CreateLocalised("DISBANDMSGTITLE", new object[0]);
				string text = "SIDEBARDISBANDMSG";
				object[] array = new object[1];
				array[0] = this.DisbandedUnits.Sum((KeyValuePair<string, Dictionary<string, int>> x) => x.Value.Sum((KeyValuePair<string, int> y) => y.Value));
				GameText gameText6 = GameText.CreateLocalised(text, array);
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.CaptionText = GameText.CreateLocalised("DISBANDMSGTITLE", new object[0]);
				messageBoxData.MessageTextList = new List<GameText>();
				messageBoxData.MessageTextList.Add(GameText.CreateLocalised("DISBANDMSGTEXT", new object[0]));
				foreach (KeyValuePair<string, Dictionary<string, int>> keyValuePair in this.DisbandedUnits)
				{
					foreach (KeyValuePair<string, int> keyValuePair2 in keyValuePair.Value)
					{
						GameText gameText7 = GameText.CreateLocalised("DISBANDITEMTEMPLATE", new object[] { keyValuePair2.Value });
						gameText7.AddChildText(GameText.CreateLocalised(keyValuePair2.Key, new object[0]));
						gameText7.AddChildText(GameText.CreateLocalised(keyValuePair.Key, new object[0]));
						messageBoxData.MessageTextList.Add(gameText7);
					}
				}
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData3 = new ActionMessageData(gameText5, gameText6, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData3);
			}
			foreach (Tuple<WorkingRealm, TradeOfferList> tuple in this.TradesArrived)
			{
				if (tuple.Item2.GoldLump != 0 || tuple.Item2.GetResourceLump().Count != 0)
				{
					GameText gameText8 = GameText.CreateLocalised("SIDEBARTRADETITLE", new object[0]);
					GameText gameText9 = GameText.CreateLocalised("SIDEBARTRADEMSG", new object[0]);
					gameText9.AddChildText(GameText.CreateLocalised(tuple.Item1.DisplayName, new object[0]));
					MessageBoxData messageBoxData2 = new MessageBoxData();
					messageBoxData2.CaptionText = GameText.CreateLocalised("SIDEBARTRADETITLE", new object[0]);
					GameText gameText10 = GameText.CreateLocalised("TRADEMSG_TEXT", new object[0]);
					gameText10.AddChildText(GameText.CreateLocalised(tuple.Item1.DisplayName, new object[0]));
					messageBoxData2.MessageTextList = new List<GameText>();
					messageBoxData2.MessageTextList.Add(gameText10);
					if (tuple.Item2.GoldLump > 0)
					{
						gameText10 = GameText.CreateLocalised("TRADELIST_GOLD", new object[] { tuple.Item2.GoldLump });
						messageBoxData2.MessageTextList.Add(gameText10);
					}
					foreach (KeyValuePair<ResourceData, int> keyValuePair3 in tuple.Item2.GetResourceLump())
					{
						gameText10 = GameText.CreateLocalised("TRADELIST_RESOURCE", new object[] { keyValuePair3.Value });
						gameText10.AddChildText(GameText.CreateLocalised(keyValuePair3.Key.DisplayName, new object[0]));
						messageBoxData2.MessageTextList.Add(gameText10);
					}
					messageBoxData2.DisplayType = MessageBoxType.Info;
					messageBoxData2.MsgType = MessageType.GenericInfo;
					ActionMessageData actionMessageData4 = new ActionMessageData(gameText8, gameText9, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData2 });
					this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData4);
				}
			}
			if (this.LevelGained)
			{
				GameText gameText11 = GameText.CreateLocalised("SIDEBARMAGICTITLE", new object[0]);
				GameText gameText12 = GameText.CreateLocalised("SIDEBARMAGICMSG", new object[0]);
				ActionMessageData actionMessageData5 = new ActionMessageData(gameText11, gameText12, false, "OpenPanel", Point.Empty, new object[] { "Magic" });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData5);
			}
			if (this.CradleProvince > -1)
			{
				WorkingProvince workingProvince = this.Game.AllProvinces[this.CradleProvince];
				GameText gameText13 = GameText.CreateLocalised("SIDEBARCRADLETITLE", new object[0]);
				GameText gameText14 = GameText.CreateLocalised("SIDEBARCRADLEMSG", new object[0]);
				gameText14.AddChildText(GameText.CreateLocalised(workingProvince.DisplayName, new object[0]));
				ActionMessageData actionMessageData6 = new ActionMessageData(gameText13, gameText14, false, "", workingProvince.CapitolCoords, null);
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData6);
			}
			if (this.PatronProvince > -1)
			{
				WorkingProvince workingProvince2 = this.Game.AllProvinces[this.PatronProvince];
				GameText gameText15 = GameText.CreateLocalised("SIDEBARARTSTITLE", new object[0]);
				GameText gameText16 = GameText.CreateLocalised("SIDEBARARTSMSG", new object[0]);
				gameText16.AddChildText(GameText.CreateLocalised(workingProvince2.DisplayName, new object[0]));
				ActionMessageData actionMessageData7 = new ActionMessageData(gameText15, gameText16, false, "", workingProvince2.CapitolCoords, null);
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData7);
			}
			foreach (Tuple<WorkingRealm, WorkingRealm> tuple2 in this.DestroyedRealms)
			{
				GameText gameText17 = GameText.CreateLocalised("REALMMSGTITLE", new object[0]);
				GameText gameText18 = GameText.CreateLocalised("SIDEBARREALMMSG", new object[0]);
				gameText18.AddChildText(GameText.CreateLocalised(tuple2.Item1.DisplayName, new object[0]));
				MessageBoxData messageBoxData3 = new MessageBoxData();
				messageBoxData3.CaptionText = GameText.CreateLocalised("REALMMSGTITLE", new object[0]);
				bool diplomaticOffense = !tuple2.Item1.DiplomacyManager.IgnoreDestroyPenalties.Contains(tuple2.Item2.Name);
				messageBoxData3.MessageText = GameText.CreateFromLiteral(this.GetRealmDestroyedFlavorText(tuple2.Item1, tuple2.Item2, diplomaticOffense));
				messageBoxData3.EventImageFile = this.GetRealmDestroyedImageFile(tuple2.Item1);
				messageBoxData3.DisplayType = MessageBoxType.Info;
				messageBoxData3.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData8 = new ActionMessageData(gameText17, gameText18, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData3 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData8);

				// Important world event: open the message immediately.
				// The action message remains in the sidebar/history, but the player does not need to click it.
				this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData3);
			}
			if (this.OccupiedProvinces.Count > 0)
			{
				GameText gameText19 = GameText.CreateLocalised("ACTION_PROVINCEOCCUPY_TITLE", new object[0]);
				GameText gameText20 = GameText.CreateLocalised("ACTION_PROVINCEOCCUPY_TEXT", new object[] { this.OccupiedProvinces.Count });
				MessageBoxData messageBoxData4 = new MessageBoxData();
				messageBoxData4.CaptionText = GameText.CreateLocalised("PROVINCEOCCUPYTITLE", new object[0]);
				messageBoxData4.MessageTextList = new List<GameText>();
				messageBoxData4.MessageTextList.Add(GameText.CreateLocalised("PROVINCEOCCUPYTEXT", new object[0]));
				foreach (int num in this.OccupiedProvinces)
				{
					messageBoxData4.MessageTextList.Add(GameText.CreateLocalised(this.Game.AllProvinces[num].DisplayName, new object[0]));
					messageBoxData4.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				}
				messageBoxData4.DisplayType = MessageBoxType.Info;
				messageBoxData4.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData9 = new ActionMessageData(gameText19, gameText20, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData4 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData9);
			}
			if (this.GainedProvinces.Count > 0)
			{
				GameText gameText21 = GameText.CreateLocalised("ACTION_PROVINCEGAIN_TITLE", new object[0]);
				GameText gameText22 = GameText.CreateLocalised("ACTION_PROVINCEGAIN_TEXT", new object[] { this.GainedProvinces.Count });
				MessageBoxData messageBoxData5 = new MessageBoxData();
				messageBoxData5.CaptionText = GameText.CreateLocalised("PROVINCEGAINTITLE", new object[0]);
				messageBoxData5.MessageTextList = new List<GameText>();
				messageBoxData5.MessageTextList.Add(GameText.CreateLocalised("PROVINCEGAINTEXT", new object[0]));
				foreach (int num2 in this.GainedProvinces)
				{
					messageBoxData5.MessageTextList.Add(GameText.CreateLocalised(this.Game.AllProvinces[num2].DisplayName, new object[0]));
					messageBoxData5.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				}
				messageBoxData5.DisplayType = MessageBoxType.Info;
				messageBoxData5.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData10 = new ActionMessageData(gameText21, gameText22, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData5 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData10);
			}
			if (this.LostProvinces.Count > 0)
			{
				GameText gameText23 = GameText.CreateLocalised("ACTION_PROVINCELOST_TITLE", new object[0]);
				GameText gameText24 = GameText.CreateLocalised("ACTION_PROVINCELOST_TEXT", new object[] { this.LostProvinces.Count });
				MessageBoxData messageBoxData6 = new MessageBoxData();
				messageBoxData6.CaptionText = GameText.CreateLocalised("PROVINCELOSTTITLE", new object[0]);
				messageBoxData6.MessageTextList = new List<GameText>();
				messageBoxData6.MessageTextList.Add(GameText.CreateLocalised("PROVINCELOSTTEXT", new object[0]));
				foreach (int num3 in this.LostProvinces)
				{
					messageBoxData6.MessageTextList.Add(GameText.CreateLocalised(this.Game.AllProvinces[num3].DisplayName, new object[0]));
					messageBoxData6.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				}
				messageBoxData6.DisplayType = MessageBoxType.Info;
				messageBoxData6.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData11 = new ActionMessageData(gameText23, gameText24, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData6 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData11);
			}
			if (this.OccupiedProvinces.Count > 0)
			{
				GameText gameText25 = GameText.CreateLocalised("ACTION_PROVINCEOCCUPY_TITLE", new object[0]);
				GameText gameText26 = GameText.CreateLocalised("ACTION_PROVINCEOCCUPY_TEXT", new object[] { this.OccupiedProvinces.Count });
				MessageBoxData messageBoxData7 = new MessageBoxData();
				messageBoxData7.CaptionText = GameText.CreateLocalised("PROVINCEOCCUPYTITLE", new object[0]);
				messageBoxData7.MessageTextList = new List<GameText>();
				messageBoxData7.MessageTextList.Add(GameText.CreateLocalised("PROVINCEOCCUPYTEXT", new object[0]));
				foreach (int num4 in this.OccupiedProvinces)
				{
					messageBoxData7.MessageTextList.Add(GameText.CreateLocalised(this.Game.AllProvinces[num4].DisplayName, new object[0]));
					messageBoxData7.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				}
				messageBoxData7.DisplayType = MessageBoxType.Info;
				messageBoxData7.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData12 = new ActionMessageData(gameText25, gameText26, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData7 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData12);
			}
			if (this.IncitedProvinces.Count > 0)
			{
				GameText gameText27 = GameText.CreateLocalised("ACTION_PROVINCEINCITE_TITLE", new object[0]);
				GameText gameText28 = GameText.CreateLocalised("ACTION_PROVINCEINCITE_TEXT", new object[] { this.OccupiedProvinces.Count });
				MessageBoxData messageBoxData8 = new MessageBoxData();
				messageBoxData8.CaptionText = GameText.CreateLocalised("PROVINCEINCITETITLE", new object[0]);
				messageBoxData8.MessageTextList = new List<GameText>();
				messageBoxData8.MessageTextList.Add(GameText.CreateLocalised("PROVINCEINCITETEXT", new object[0]));
				foreach (int num5 in this.OccupiedProvinces)
				{
					messageBoxData8.MessageTextList.Add(GameText.CreateLocalised(this.Game.AllProvinces[num5].DisplayName, new object[0]));
					messageBoxData8.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				}
				messageBoxData8.DisplayType = MessageBoxType.Info;
				messageBoxData8.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData13 = new ActionMessageData(gameText27, gameText28, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData8 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData13);
			}
			if (this.HarvestFaire != -1)
			{
				WorkingProvince workingProvince3 = this.Game.AllProvinces[this.HarvestFaire];
				GameText gameText29 = GameText.CreateLocalised("ACTION_FAIR_TITLE", new object[0]);
				GameText gameText30 = GameText.CreateLocalised("ACTION_FAIR_TEXT", new object[0]);
				gameText30.AddChildText(GameText.CreateLocalised(workingProvince3.DisplayName, new object[0]));
				if (workingProvince3.OwnerRealm == this.Game.PlayerRealm)
				{
					MessageBoxData messageBoxData9 = new MessageBoxData();
					messageBoxData9.CaptionText = GameText.CreateLocalised("ACTION_FAIR_TITLE", new object[0]);
					GameText gameText31 = GameText.CreateLocalised("HARVEST_FAIR_MSG", new object[0]);
					gameText31.AddChildText(GameText.CreateLocalised(workingProvince3.DisplayName, new object[0]));
					GameText gameText32;
					if (this.HarvestFaireChampion != null)
					{
						gameText32 = GameText.CreateLocalised("HARVEST_FAIR_UNIT", new object[0]);
						gameText32.AddChildText(GameText.CreateLocalised(this.HarvestFaireChampion.DisplayName, new object[0]));
					}
					else
					{
						gameText32 = GameText.CreateLocalised("HARVEST_FAIR_NOUNIT", new object[0]);
					}
					messageBoxData9.MessageTextList = new List<GameText>();
					messageBoxData9.MessageTextList.Add(gameText31);
					messageBoxData9.MessageTextList.Add(gameText32);
					messageBoxData9.DisplayType = MessageBoxType.Info;
					messageBoxData9.MsgType = MessageType.GenericInfo;
					ActionMessageData actionMessageData14 = new ActionMessageData(gameText29, gameText30, false, "DisplayMessage", workingProvince3.CapitolCoords, new object[] { messageBoxData9 });
					this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData14);
				}
				else
				{
					ActionMessageData actionMessageData15 = new ActionMessageData(gameText29, gameText30, false, "", workingProvince3.CapitolCoords, null);
					this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData15);
				}
			}
			if (this.PlagueProvinces.Count > 0)
			{
				GameText gameText33 = GameText.CreateLocalised("ACTION_PLAGUE_TITLE", new object[0]);
				GameText gameText34 = GameText.CreateLocalised("ACTION_PLAGUE_TEXT", new object[] { this.PlagueProvinces.Distinct<int>().Count<int>() });
				MessageBoxData messageBoxData10 = new MessageBoxData();
				messageBoxData10.CaptionText = GameText.CreateLocalised("PLAGUETITLE", new object[0]);
				messageBoxData10.MessageTextList = new List<GameText>();
				messageBoxData10.MessageTextList.Add(GameText.CreateLocalised("PLAGUETEXT", new object[0]));
				foreach (int num6 in this.PlagueProvinces.Distinct<int>())
				{
					messageBoxData10.MessageTextList.Add(GameText.CreateLocalised(this.Game.AllProvinces[num6].DisplayName, new object[0]));
					messageBoxData10.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				}
				messageBoxData10.DisplayType = MessageBoxType.Info;
				messageBoxData10.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData16 = new ActionMessageData(gameText33, gameText34, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData10 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData16);
			}
			if (this.TrainedUnits.Count > 0)
			{
				GameText gameText35 = GameText.CreateLocalised("ACTION_TRAINED_TITLE", new object[0]);
				GameText gameText36 = GameText.CreateLocalised("ACTION_TRAINED_TEXT", new object[] { this.TrainedUnits.Count });
				MessageBoxData messageBoxData11 = new MessageBoxData();
				messageBoxData11.CaptionText = GameText.CreateLocalised("TRAINEDTITLE", new object[0]);
				messageBoxData11.MessageTextList = new List<GameText>();
				messageBoxData11.MessageTextList.Add(GameText.CreateLocalised("TRAINEDTEXT", new object[0]));
				foreach (int num7 in this.TrainedUnits)
				{
					messageBoxData11.MessageTextList.Add(GameText.CreateLocalised(this.Game.AllUnits[num7].DisplayName, new object[0]));
					messageBoxData11.MessageTextList.Add(GameText.CreateLocalised("FORMAT_NEWLINE", new object[0]));
				}
				messageBoxData11.DisplayType = MessageBoxType.Info;
				messageBoxData11.MsgType = MessageType.GenericInfo;
				ActionMessageData actionMessageData17 = new ActionMessageData(gameText35, gameText36, false, "DisplayMessage", Point.Empty, new object[] { messageBoxData11 });
				this.Game.GameCore.MessageHandler.ShowActionMessage(actionMessageData17);
			}
		}

		public List<int> GainedProvinces;

		public List<int> LostProvinces;

		public List<int> OccupiedProvinces;

		public List<int> IncitedProvinces;

		public int HarvestFaire;

		public WorkingUnit HarvestFaireChampion;

		public List<int> PlagueProvinces;

		public List<int> TrainedUnits;

		public Dictionary<string, Dictionary<string, int>> DisbandedUnits;

		public List<Tuple<WorkingRealm, TradeOfferList>> TradesArrived;

		public List<Tuple<WorkingRealm, WorkingRealm>> DestroyedRealms;

		public int CradleProvince;

		public int PatronProvince;

		public bool LevelGained;

		public bool PowerChanged;

		private PowerGroup OldGroup;

		private PowerGroup NewGroup;

		private SovereigntyGame Game;
	}
}
