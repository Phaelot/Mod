// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.AchievementManager
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Utility;
using Steamworks;

namespace SovereigntyTK
{
	public class AchievementManager
	{
		private Sovereignty Game;

		private List<StoredAchievementData> StoredAchievements;

		private ulong SteamID;

		public AchievementManager(Sovereignty Game)
		{
			this.Game = Game;
			SteamID = SteamUser.GetSteamID().m_SteamID;
			Load();
			List<StoredAchievementData> storedAchievements = StoredAchievements;
			Func<StoredAchievementData, bool> predicate = (StoredAchievementData x) => x.SteamID == SteamID;
			if (storedAchievements.Count(predicate) == 0)
			{
				StoredAchievements.Add(new StoredAchievementData(SteamID));
			}
			UploadSpecialStats();
			Game.RegisterEvent(HandleHeroDeployed, "HeroDeployed");
			Game.RegisterEvent(HandleBattleStart, "AutoBattleStart");
			Game.RegisterEvent(HandleSpellLearned, "SpellLearned");
			Game.RegisterEvents(HandleBattleEnded, "AutoBattleEnded", "TacticalBattleEnded");
			Game.RegisterEvent(HandleUnitPromoted, "UnitPromoted");
			Game.RegisterEvent(HandleBattlefield, "BattlefieldCreated");
			Game.RegisterEvent(HandleOccupiedRetreat, "OccupiedCapitolRetreat");
			Game.RegisterEvent(HandleTradeSent, "TradeOfferSent");
			Game.RegisterEvent(HandleAlliance, "AllianceFormed");
			Game.RegisterEvent(HandleCradle, "CradlePlaced");
			Game.RegisterEvent(HandlePatron, "PatronPlaced");
			Game.RegisterEvent(HandleSpellCast, "SpellCast");
			Game.RegisterEvent(HandleHorde, "HordeCalled");
			Game.RegisterEvent(HandlePrisoner, "PrisonerTaken");
			Game.RegisterEvent(HandleUnitKilled, "UnitKilled");
			Game.RegisterEvent(HandleGameStarted, "GameStarted");
			Game.RegisterEvent(HandleGoldChanged, "PlayerGoldChanged");
			Game.RegisterEvent(HandleRealmDestroyed, "RealmDestroyed");
			Game.RegisterEvent(HandleVictory, "CampaignVictory");
		}

		private void HandleVictory(string EventName, params object[] Args)
		{
			StoredAchievementData storedAchievementData = StoredAchievements.SingleOrDefault((StoredAchievementData x) => x.SteamID == SteamID);
			if (storedAchievementData == null)
			{
				return;
			}
			if (Game.CurrentGame.Ironman)
			{
				int pData = 0;
				SteamUserStats.GetStat("STAT_IRON_WINS", out pData);
				SteamUserStats.SetStat("STAT_IRON_WINS", pData + 1);
				SteamUserStats.StoreStats();
			}
			string iD = Game.CurrentGame.CurrentCampaign.ID;
			string name = Game.CurrentGame.PlayerRealm.Name;
			if (!storedAchievementData.CampaignRealms.Contains(name))
			{
				storedAchievementData.CampaignRealms.Add(name);
			}
			switch (iD)
			{
				case "Core_Conquest":
					if (!storedAchievementData.ConquestRealms.Contains(name))
					{
						storedAchievementData.ConquestRealms.Add(name);
						Save();
					}
					break;
				case "Core_Powergames":
					if (!storedAchievementData.PowerGamesRealms.Contains(name))
					{
						storedAchievementData.PowerGamesRealms.Add(name);
						Save();
					}
					break;
				case "Core_LMS":
					if (!storedAchievementData.LMSRealms.Contains(name))
					{
						storedAchievementData.LMSRealms.Add(name);
						Save();
					}
					if (name == "Icespire")
					{
						SteamUserStats.SetAchievement("ACHIEVE_LMS_ICESPIRE");
					}
					if (name == "Ladvia")
					{
						SteamUserStats.SetAchievement("ACHIEVE_LMS_LADVIA");
					}
					if (name == "Crivia")
					{
						SteamUserStats.SetAchievement("ACHIEVE_LMS_CRIVIA");
					}
					break;
				case "Core_Rivalry":
					if (!storedAchievementData.RivalryRealms.Contains(name))
					{
						storedAchievementData.RivalryRealms.Add(name);
						Save();
					}
					break;
			}
			SteamUserStats.StoreStats();
			UploadSpecialStats();
		}

		private void HandleRealmDestroyed(string EventName, params object[] Args)
		{
			WorkingRealm workingRealm = Args[0] as WorkingRealm;
			WorkingRealm workingRealm2 = Args[1] as WorkingRealm;
			if (workingRealm2 == Game.CurrentGame.PlayerRealm)
			{
				string text = "";
				switch (workingRealm.Race)
				{
					case Races.Dwarf:
						text = "STAT_REALMS_DWARF";
						break;
					case Races.Human:
						text = "STAT_REALMS_HUMAN";
						break;
					case Races.Orc:
						text = "STATS_REALMS_ORC";
						break;
					case Races.Elf:
						text = "STAT_REALMS_ELF";
						break;
					case Races.Undead:
						text = "STAT_REALMS_DEATH";
						break;
				}
				if (!(text == ""))
				{
					int pData = 0;
					SteamUserStats.GetStat(text, out pData);
					SteamUserStats.SetStat(text, pData + 1);
					SteamUserStats.StoreStats();
				}
			}
		}

		private void HandleGoldChanged(string EventName, params object[] Args)
		{
			int pData = 0;
			SteamUserStats.GetStat("STAT_GOLD", out pData);
			SteamUserStats.SetStat("STAT_GOLD", Math.Max(pData, Game.CurrentGame.PlayerRealm.Gold));
			SteamUserStats.StoreStats();
		}

		private void HandleGameStarted(string EventName, params object[] Args)
		{
			StoredAchievementData storedAchievementData = StoredAchievements.SingleOrDefault((StoredAchievementData x) => x.SteamID == SteamID);
			if (storedAchievementData != null && !storedAchievementData.RacesPlayed.Contains(Game.CurrentGame.PlayerRealm.Race))
			{
				storedAchievementData.RacesPlayed.Add(Game.CurrentGame.PlayerRealm.Race);
				Save();
				UploadSpecialStats();
			}
		}

		private void HandleUnitKilled(string EventName, params object[] Args)
		{
			WorkingUnit workingUnit = Args[0] as WorkingUnit;
			WorkingUnit workingUnit2 = Args[1] as WorkingUnit;
			if (workingUnit2.OwnerRealm == Game.CurrentGame.PlayerRealm)
			{
				if (workingUnit.Class == UnitClasses.Naval)
				{
					int pData = 0;
					SteamUserStats.GetStat("STAT_KILLS_NAVAL", out pData);
					SteamUserStats.SetStat("STAT_KILLS_NAVAL", pData + 1);
					SteamUserStats.StoreStats();
				}
				else
				{
					int pData2 = 0;
					SteamUserStats.GetStat("STAT_KILLS_LAND", out pData2);
					SteamUserStats.SetStat("STAT_KILLS_LAND", pData2 + 1);
					SteamUserStats.StoreStats();
				}
			}
		}

		private void HandlePrisoner(string EventName, params object[] Args)
		{
			int num = (int)Args[0];
			if (num == Game.CurrentGame.PlayerRealm.ID)
			{
				int pData = 0;
				SteamUserStats.GetStat("STAT_PRISONERS", out pData);
				SteamUserStats.SetStat("STAT_PRISONERS", pData + 1);
				SteamUserStats.StoreStats();
			}
		}

		private void HandleHorde(string EventName, params object[] Args)
		{
			string text = (string)Args[0];
			if (text == Game.CurrentGame.PlayerRealm.Name)
			{
				SteamUserStats.SetAchievement("ACHIEVE_HORDE");
				SteamUserStats.StoreStats();
			}
		}

		private void HandleSpellCast(string EventName, params object[] Args)
		{
			SpellEffect spellEffect = Args[0] as SpellEffect;
			if (spellEffect.Caster == Game.CurrentGame.PlayerRealm && spellEffect.SpellName == "Fading Age")
			{
				SteamUserStats.SetAchievement("ACHIEVE_FADINGAGE");
				SteamUserStats.StoreStats();
			}
		}

		private void HandlePatron(string EventName, params object[] Args)
		{
			WorkingProvince workingProvince = Args[0] as WorkingProvince;
			if (workingProvince.OwnerRealm == Game.CurrentGame.PlayerRealm)
			{
				int pData = 0;
				SteamUserStats.GetStat("STAT_PATRONS", out pData);
				SteamUserStats.SetStat("STAT_PATRONS", pData + 1);
				SteamUserStats.StoreStats();
			}
		}

		private void HandleCradle(string EventName, params object[] Args)
		{
			WorkingProvince workingProvince = Args[0] as WorkingProvince;
			if (workingProvince.OwnerRealm == Game.CurrentGame.PlayerRealm)
			{
				int pData = 0;
				SteamUserStats.GetStat("STAT_CRADLES", out pData);
				SteamUserStats.SetStat("STAT_CRADLES", pData + 1);
				SteamUserStats.StoreStats();
			}
		}

		private void HandleAlliance(string EventName, params object[] Args)
		{
			WorkingRealm workingRealm = Args[0] as WorkingRealm;
			WorkingRealm workingRealm2 = Args[1] as WorkingRealm;
			if (workingRealm == Game.CurrentGame.PlayerRealm || workingRealm2 == Game.CurrentGame.PlayerRealm)
			{
				int pData = 0;
				SteamUserStats.GetStat("STAT_ALLIES", out pData);
				SteamUserStats.SetStat("STAT_ALLIES", pData + 1);
				SteamUserStats.StoreStats();
			}
		}

		private void HandleTradeSent(string EventName, params object[] Args)
		{
			int pData = 0;
			SteamUserStats.GetStat("STAT_TRADE", out pData);
			SteamUserStats.SetStat("STAT_TRADE", pData + 1);
			SteamUserStats.StoreStats();
		}

		private void HandleOccupiedRetreat(string EventName, params object[] Args)
		{
			SteamUserStats.SetAchievement("ACHIEVE_CODE");
			SteamUserStats.StoreStats();
		}

		private void HandleBattlefield(string EventName, params object[] Args)
		{
			BattleFieldData battleFieldData = Args[0] as BattleFieldData;
			if (battleFieldData.WinnerRealm == Game.CurrentGame.PlayerRealm)
			{
				SteamUserStats.SetAchievement("ACHIEVE_BATTLEFIELD");
				SteamUserStats.StoreStats();
			}
		}

		private void HandleUnitPromoted(string EventName, params object[] Args)
		{
			WorkingUnit workingUnit = Args[0] as WorkingUnit;
			if (workingUnit.OwnerRealm == Game.CurrentGame.PlayerRealm)
			{
				if (workingUnit.Medals == 1)
				{
					SteamUserStats.SetAchievement("ACHIEVE_MEDAL1");
					SteamUserStats.StoreStats();
				}
				if (workingUnit.Medals == 2)
				{
					SteamUserStats.SetAchievement("ACHIEVE_MEDAL2");
					SteamUserStats.StoreStats();
				}
			}
		}

		private void HandleBattleEnded(string EventName, params object[] Args)
		{
			if (EventName == "AutoBattleEnded")
			{
				AutoBattleController autoBattleController = Args[0] as AutoBattleController;
				if (autoBattleController.Defender.Owner == Game.CurrentGame.PlayerRealm && autoBattleController.Defender.Units.Count((WorkingUnit x) => !x.Disabled) == 1 && autoBattleController.Node.Province != null && autoBattleController.Winner == autoBattleController.Defender)
				{
					SteamUserStats.SetAchievement("ACHIEVE_STAND");
					SteamUserStats.StoreStats();
				}
			}
			if (EventName == "TacticalBattleEnded")
			{
				TacticalBattleController tacticalBattleController = Args[0] as TacticalBattleController;
				if (tacticalBattleController.Defender.Owner == Game.CurrentGame.PlayerRealm && tacticalBattleController.Defender.Units.Count((WorkingUnit x) => x.Disabled) == 1 && tacticalBattleController.Node.Province != null && tacticalBattleController.Winner == tacticalBattleController.Defender)
				{
					SteamUserStats.SetAchievement("ACHIEVE_STAND");
					SteamUserStats.StoreStats();
				}
			}
		}

		private void HandleSpellLearned(string EventName, params object[] Args)
		{
			RealmMagicData realmMagicData = Args[0] as RealmMagicData;
			StoredAchievementData storedAchievementData = StoredAchievements.SingleOrDefault((StoredAchievementData x) => x.SteamID == SteamID);
			if (storedAchievementData != null && !storedAchievementData.SpellsLearned.Contains(realmMagicData.Name))
			{
				storedAchievementData.SpellsLearned.Add(realmMagicData.Name);
				Save();
				UploadSpecialStats();
			}
		}

		private void HandleBattleStart(string EventName, params object[] Args)
		{
			AutoBattleController autoBattleController = Args[0] as AutoBattleController;
			if (autoBattleController.Node.Province != null && autoBattleController.Attacker.Owner == Game.CurrentGame.PlayerRealm && autoBattleController.Attacker.Units.Count == 20)
			{
				SteamUserStats.SetAchievement("ACHIEVE_MASS");
				SteamUserStats.StoreStats();
			}
		}

		private void HandleHeroDeployed(string EventName, params object[] Args)
		{
			WorkingHero workingHero = Args[0] as WorkingHero;
			if (workingHero.OwnerRealm == Game.CurrentGame.PlayerRealm)
			{
				SteamUserStats.SetAchievement("ACHIEVE_HERO");
				SteamUserStats.StoreStats();
			}
		}

		private void UploadSpecialStats()
		{
			StoredAchievementData storedAchievementData = StoredAchievements.SingleOrDefault((StoredAchievementData x) => x.SteamID == SteamID);
			SteamUserStats.SetStat("STAT_CAMPAIGN_WINS", storedAchievementData.CampaignRealms.Count);
			SteamUserStats.SetStat("STAT_CONQUEST_WINS", storedAchievementData.ConquestRealms.Count);
			SteamUserStats.SetStat("STAT_LMS_WINS", storedAchievementData.LMSRealms.Count);
			SteamUserStats.SetStat("STAT_POWER_WINS", storedAchievementData.PowerGamesRealms.Count);
			SteamUserStats.SetStat("STAT_RIVALRY_WINS", storedAchievementData.RivalryRealms.Count);
			SteamUserStats.SetStat("STAT_RACES", storedAchievementData.RacesPlayed.Count);
			int[] array = new int[5];
			foreach (string item in storedAchievementData.SpellsLearned)
			{
				RealmMagicData value = null;
				Game.Data.Spells.TryGetValue(item, out value);
				if (value != null)
				{
					array[value.Level - 1]++;
				}
			}
			SteamUserStats.SetStat("STAT_SPELLS1", array[0]);
			SteamUserStats.SetStat("STAT_SPELLS2", array[1]);
			SteamUserStats.SetStat("STAT_SPELLS3", array[2]);
			SteamUserStats.SetStat("STAT_SPELLS4", array[3]);
			SteamUserStats.SetStat("STAT_SPELLS5", array[4]);
			SteamUserStats.StoreStats();
		}

		private void Load()
		{
			StoredAchievements = new List<StoredAchievementData>();
			if (!Game.Utilities.FileSystem.FileExists("steam.dat", FileTypes.User))
			{
				Save();
			}
			Stream input = Game.Utilities.FileSystem.OpenFile("steam.dat", FileTypes.User);
			BinaryReader binaryReader = new BinaryReader(input);
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				StoredAchievementData item = new StoredAchievementData(binaryReader);
				StoredAchievements.Add(item);
			}
			binaryReader.Close();
		}

		private void Save()
		{
			Stream output = Game.Utilities.FileSystem.OpenFile("steam.dat", FileTypes.User, FileModes.ReadWrite);
			BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write(StoredAchievements.Count);
			foreach (StoredAchievementData storedAchievement in StoredAchievements)
			{
				storedAchievement.Save(binaryWriter);
			}
			binaryWriter.Close();
		}

		internal void Dispose()
		{
			SteamUserStats.StoreStats();
			Save();
			Game.UnregisterEvent(HandleHeroDeployed, "HeroDeployed");
			Game.UnregisterEvent(HandleBattleStart, "AutoBattleStart");
			Game.UnregisterEvent(HandleSpellLearned, "SpellLearned");
			Game.UnregisterEvents(HandleBattleEnded, "AutoBattleEnded", "TacticalBattleEnded");
			Game.UnregisterEvent(HandleUnitPromoted, "UnitPromoted");
			Game.UnregisterEvent(HandleBattlefield, "BattlefieldNamed");
			Game.UnregisterEvent(HandleOccupiedRetreat, "OccupiedCapitolRetreat");
			Game.UnregisterEvent(HandleTradeSent, "TradeOfferSent");
			Game.UnregisterEvent(HandleAlliance, "AllianceFormed");
			Game.UnregisterEvent(HandleCradle, "CradlePlaced");
			Game.UnregisterEvent(HandlePatron, "PatronPlaced");
			Game.UnregisterEvent(HandleSpellCast, "SpellCast");
			Game.UnregisterEvent(HandleHorde, "HordeCalled");
			Game.UnregisterEvent(HandlePrisoner, "PrisonerTaken");
			Game.UnregisterEvent(HandleUnitKilled, "UnitKilled");
			Game.UnregisterEvent(HandleGameStarted, "GameStarted");
			Game.UnregisterEvent(HandleGoldChanged, "PlayerGoldChanged");
			Game.UnregisterEvent(HandleRealmDestroyed, "RealmDestroyed");
			Game.UnregisterEvent(HandleVictory, "CampaignVictory");
		}
	}
}