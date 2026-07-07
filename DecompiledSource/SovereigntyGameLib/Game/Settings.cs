// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Game.Settings
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.Game;

namespace SovereigntyTK.Game
{
	public class Settings
	{
		private Dictionary<string, Setting> AllSettings;

		public Dictionary<string, Setting> GraphicSettings;

		public Dictionary<string, Setting> AudioSettings;

		public Dictionary<string, Setting> GameplaySettings;

		public Dictionary<string, Setting> InputSettings;

		public GameBase Game;

		public bool BackupSaves;

		public WindowState Windowmode_State;

		public Rectangle Windowmode_Bounds;

		public Settings(GameBase Game)
		{
			this.Game = Game;
			GraphicSettings = new Dictionary<string, Setting>();
			AudioSettings = new Dictionary<string, Setting>();
			GameplaySettings = new Dictionary<string, Setting>();
			InputSettings = new Dictionary<string, Setting>();
			AllSettings = new Dictionary<string, Setting>();
			AddSetting(GraphicSettings, new BooleanSetting("Fullscreen", "SETTING_FULLSCREEN", "SETTING_FULLSCREEN_TT", DefaultValue: true));
			DisplayModeSetting newsetting = new DisplayModeSetting("DisplayMode", "SETTING_MODE", "SETTING_MODE_TT", GetFullscreenDefault())
			{
				DisplayValues = GetAllPossibleResolutions()
			};
			AddSetting(GraphicSettings, newsetting);
			List<int> multiSampleModes = Game.GetMultiSampleModes();
			NumberListSetting numberListSetting = new NumberListSetting("AA", "SETTING_AA", "SETTING_AA_TT", 0);
			foreach (int item in multiSampleModes)
			{
				numberListSetting.DisplayValues.Add(item);
			}
			AddSetting(GraphicSettings, numberListSetting);
			AddSetting(GraphicSettings, new BooleanSetting("MapGrid", "SETTING_MAPGRID", "SETTING_MAPGRID_TT", DefaultValue: false));
			AddSetting(AudioSettings, new NumericSetting("SoundVolume", "SETTING_SOUNDVOL", "SETTING_SOUNDVOL_TT", 100, 0, 100));
			AddSetting(AudioSettings, new NumericSetting("MusicVolume", "SETTING_MUSICVOL", "SETTING_MUSICVOL_TT", 30, 0, 100));
			AddSetting(GameplaySettings, new BooleanSetting("TipsEnabled", "SETTING_TIPSENABLED", "SETTING_TIPSENABLED_TT", DefaultValue: true));
			AddSetting(GameplaySettings, new BooleanSetting("ViewAllyBattles", "SETTING_ALLYBATTLE", "SETTING_ALLYBATTLE_TT", DefaultValue: false));
			AddSetting(GameplaySettings, new BooleanSetting("ViewEnemyBattles", "SETTING_ENEMYBATTLE", "SETTING_ENEMYBATTLE_TT", DefaultValue: false));
			AddSetting(GameplaySettings, new BooleanSetting("ViewRebelBattles", "SETTING_REBELBATTLE", "SETTING_REBELBATTLE_TT", DefaultValue: false));
			AddSetting(GameplaySettings, new BooleanSetting("ViewTradeBattles", "SETTING_TRADEBATTLE", "SETTING_TRADEBATTLE_TT", DefaultValue: false));
			AddSetting(GameplaySettings, new NumericSetting("InfoCardDelay", "SETTING_CARDDELAY", "SETTING_CARDDELAY_TT", 1, 1, 15));
			AddSetting(GameplaySettings, new NumericSetting("AutoBattleSpeed", "SETTING_AUTOBATTLESPEED", "SETTING_AUTOBATTLESPEED_TT", 3, 1, 5));
			AddSetting(GameplaySettings, new NumericSetting("TacticalBattleSpeed", "SETTING_BATTLESPEED", "SETTING_BATTLESPEED_TT", 5, 1, 10));
			AddSetting(GameplaySettings, new NumericSetting("CampaignSpeedHuman", "SETTING_CAMPAIGNSPEED", "SETTING_CAMPAIGNSPEED_TT", 5, 1, 10));
			AddSetting(GameplaySettings, new NumericSetting("CampaignSpeedAI", "SETTING_CAMPAIGNAISPEED", "SETTING_CAMPAIGNAISPEED_TT", 5, 1, 10));
			EnumeratedSetting newsetting2 = new EnumeratedSetting("Difficulty", "SETTING_DIFFICULTY", "SETTING_DIFFICULTY_TT", 2, 1, 5)
			{
				DisplayValues =
			{
				{ 1, "DIFFICULTYNAME_1" },
				{ 2, "DIFFICULTYNAME_2" },
				{ 3, "DIFFICULTYNAME_3" },
				{ 4, "DIFFICULTYNAME_4" },
				{ 5, "DIFFICULTYNAME_5" }
			}
			};
			AddSetting(GameplaySettings, newsetting2);
			AddSetting(GameplaySettings, new BooleanSetting("SwapActionButtons", "SETTING_SWAPBUTTONS", "SETTING_SWAPBUTTONS_TT", DefaultValue: false));
			AddSetting(GameplaySettings, new BooleanSetting("DisableTactical", "SETTING_NOTACTICAL", "SETTING_NOTACTICAL_TT", DefaultValue: false));
			AddSetting(GameplaySettings, new BooleanSetting("FollowCamera", "SETTING_FOLLOWCAMERA", "SETTING_FOLLOWCAMERA_TT", DefaultValue: false));
			EnumeratedSetting newsetting3 = new EnumeratedSetting("UIScale", "SETTING_UISCALE", "SETTING_UISCALE_TT", 3, 1, 4)
			{
				DisplayValues =
			{
				{ 1, "UISCALE_0" },
				{ 2, "UISCALE_1" },
				{ 3, "UISCALE_2" },
				{ 4, "UISCALE_3" }
			}
			};
			AddSetting(GraphicSettings, newsetting3);
			StringListSetting stringListSetting = new StringListSetting("Language", "SETTING_LANGUAGE", "SETTING_LANGUAGE_TT", "English");
			foreach (string languageName in Game.Utilities.TextManager.LanguageNames)
			{
				stringListSetting.DisplayValues.Add(languageName, "LANGUAGENAME");
			}
			AddSetting(GraphicSettings, stringListSetting);
			AddSetting(InputSettings, new KeybindSetting("KeyMagicPanel", "SETTING_KEYMAGIC", "SETTING_KEYMAGIC_TT", Key.Number1));
			AddSetting(InputSettings, new KeybindSetting("KeyQueuePanel", "SETTING_KEYQUEUE", "SETTING_KEYQUEUE_TT", Key.Number2));
			AddSetting(InputSettings, new KeybindSetting("KeyListsPanel", "SETTING_KEYLISTS", "SETTING_KEYLISTS_TT", Key.Number3));
			AddSetting(InputSettings, new KeybindSetting("KeyResourcesPanel", "SETTING_KEYRESOURCES", "SETTING_KEYRESOURCES_TT", Key.Number4));
			AddSetting(InputSettings, new KeybindSetting("KeyRankingsPanel", "SETTING_KEYRANKINGS", "SETTING_KEYRANKINGS_TT", Key.Number5));
			AddSetting(InputSettings, new KeybindSetting("KeyPrisonersPanel", "SETTING_KEYPRISONERS", "SETTING_KEYPRISONERS_TT", Key.Number6));
			AddSetting(InputSettings, new KeybindSetting("KeySpellsPanel", "SETTING_KEYSPELLS", "SETTING_KEYSPELLS_TT", Key.Number7));
			AddSetting(InputSettings, new KeybindSetting("KeyObjectivesPanel", "SETTING_KEYOBJECTIVES", "SETTING_KEYOBJECTIVES_TT", Key.Number8));
			AddSetting(InputSettings, new KeybindSetting("KeyUpgrade", "SETTING_KEYUPGRADE", "SETTING_KEYUPGRADE_TT", Key.U));
			AddSetting(InputSettings, new KeybindSetting("KeyHome", "SETTING_KEYHOME", "SETTING_KEYHOME_TT", Key.C));
			AddSetting(InputSettings, new KeybindSetting("KeyPurchase", "SETTING_KEYPURCHASE", "SETTING_KEYPURCHASE_TT", Key.P));
			AddSetting(InputSettings, new KeybindSetting("KeyRelations", "SETTING_KEYFOREIGN", "SETTING_KEYFOREIGN_TT", Key.F));
			AddSetting(InputSettings, new KeybindSetting("KeyEndTurn", "SETTING_KEYTURN", "SETTING_KEYTURN_TT", Key.T));
			AddSetting(InputSettings, new KeybindSetting("KeyMapGrid", "SETTING_KEYGRID", "SETTING_KEYGRID_TT", Key.G));
		}

		public void ConvertFromOldFile(string OldFilename)
		{
			OldSettings oldSettings = new OldSettings(OldFilename, Game);
			if (oldSettings.OldSettingsFound)
			{
				BackupSaves = true;
				return;
			}
			(AllSettings["Fullscreen"] as BooleanSetting).SetValue(oldSettings.FullScreen);
			(AllSettings["DisplayMode"] as DisplayModeSetting).SetValue(oldSettings.FullscreenMode);
			(AllSettings["AA"] as NumberListSetting).SetValue(oldSettings.AALevel);
			(AllSettings["UIScale"] as EnumeratedSetting).SetValue(oldSettings.ScaleIndex + 1);
			(AllSettings["Language"] as StringListSetting).SetValue(oldSettings.LanguageName);
			(AllSettings["SoundVolume"] as NumericSetting).SetValue(oldSettings.SoundVolume);
			(AllSettings["MusicVolume"] as NumericSetting).SetValue(oldSettings.MusicVolume);
			(AllSettings["TipsEnabled"] as BooleanSetting).SetValue(oldSettings.TutorialsEnabled);
			(AllSettings["ViewAllyBattles"] as BooleanSetting).SetValue(oldSettings.ViewBattlesAlly);
			(AllSettings["ViewEnemyBattles"] as BooleanSetting).SetValue(oldSettings.ViewBattlesEnemy);
			(AllSettings["ViewTradeBattles"] as BooleanSetting).SetValue(oldSettings.ViewBattlesTrade);
			(AllSettings["AutoBattleSpeed"] as NumericSetting).SetValue(oldSettings.AutoBattleSpeed);
			(AllSettings["TacticalBattleSpeed"] as NumericSetting).SetValue(oldSettings.TacticalBattleSpeed);
			(AllSettings["Difficulty"] as EnumeratedSetting).SetValue(oldSettings.GetDifficulty());
			(AllSettings["SwapActionButtons"] as BooleanSetting).SetValue(oldSettings.RightClickAction);
			(AllSettings["DisableTactical"] as BooleanSetting).SetValue(oldSettings.NoTacticalBattles);
			Windowmode_Bounds = oldSettings.Windowmode_Bounds;
			Windowmode_State = oldSettings.Windowmode_State;
		}

		public DisplayResolution GetFullscreenDefault()
		{
			List<DisplayResolution> allPossibleResolutions = GetAllPossibleResolutions();
			int BestWidth = 0;
			foreach (DisplayResolution item in allPossibleResolutions)
			{
				if (item.Width > BestWidth)
				{
					BestWidth = item.Width;
				}
			}
			allPossibleResolutions.RemoveAll((DisplayResolution x) => x.Width != BestWidth);
			int BestHeight = 0;
			foreach (DisplayResolution item2 in allPossibleResolutions)
			{
				if (item2.Height > BestHeight)
				{
					BestHeight = item2.Height;
				}
			}
			allPossibleResolutions.RemoveAll((DisplayResolution x) => x.Height != BestHeight);
			float BestRate = 0f;
			foreach (DisplayResolution item3 in allPossibleResolutions)
			{
				if (item3.Width == BestWidth && item3.Height == BestHeight && item3.RefreshRate > BestRate && item3.RefreshRate <= 60f)
				{
					BestRate = item3.RefreshRate;
				}
			}
			if (BestRate == 0f)
			{
				BestRate = 2.1474836E+09f;
				foreach (DisplayResolution item4 in allPossibleResolutions)
				{
					if (item4.Width == BestWidth && item4.Height == BestHeight && item4.RefreshRate < BestRate)
					{
						BestRate = item4.RefreshRate;
					}
				}
			}
			allPossibleResolutions.RemoveAll((DisplayResolution x) => x.RefreshRate != BestRate);
			DisplayResolution displayResolution = allPossibleResolutions.FirstOrDefault();
			if (displayResolution == null)
			{
				throw new Exception("Unable to find supported resolution on primary display device");
			}
			return displayResolution;
		}

		public List<DisplayResolution> GetAllPossibleResolutions()
		{
			DisplayDevice display = DisplayDevice.GetDisplay(DisplayIndex.First);
			List<DisplayResolution> list = new List<DisplayResolution>();
			foreach (DisplayResolution Res in display.AvailableResolutions)
			{
				if (Res.Width >= 1024 && Res.Height >= 768 && Res.BitsPerPixel >= 32 && list.Count((DisplayResolution x) => x.Width == Res.Width && x.Height == Res.Height && x.BitsPerPixel == Res.BitsPerPixel && x.RefreshRate == Res.RefreshRate) <= 0)
				{
					list.Add(Res);
				}
			}
			list.Sort(SizeComparer);
			return list;
		}

		private int SizeComparer(DisplayResolution A, DisplayResolution B)
		{
			if (A.Width.CompareTo(B.Width) != 0)
			{
				return A.Width.CompareTo(B.Width);
			}
			if (A.Height.CompareTo(B.Height) != 0)
			{
				return A.Height.CompareTo(B.Height);
			}
			return A.RefreshRate.CompareTo(B.RefreshRate);
		}

		private void AddSetting(Dictionary<string, Setting> Category, Setting Newsetting)
		{
			Category.Add(Newsetting.SettingName, Newsetting);
			AllSettings.Add(Newsetting.SettingName, Newsetting);
		}

		public void Reset()
		{
			foreach (Setting value in AllSettings.Values)
			{
				value.Reset();
			}
		}

		public void Load(string Filename)
		{
			XElement xElement = XElement.Load(Filename);
			foreach (XElement item in xElement.Elements())
			{
				if (item.Name.LocalName == "WindowState")
				{
					LoadWindowState(item);
				}
				Setting value = null;
				AllSettings.TryGetValue(item.Name.LocalName, out value);
				value?.Load(item);
			}
		}

		private void LoadWindowState(XElement Element)
		{
			Windowmode_State = (WindowState)int.Parse(Element.Element("State").Value);
			Windowmode_Bounds.X = int.Parse(Element.Element("Bounds").Attribute("X").Value);
			Windowmode_Bounds.Y = int.Parse(Element.Element("Bounds").Attribute("Y").Value);
			Windowmode_Bounds.Width = int.Parse(Element.Element("Bounds").Attribute("W").Value);
			Windowmode_Bounds.Height = int.Parse(Element.Element("Bounds").Attribute("H").Value);
		}

		private void SaveWindowState(XElement Element)
		{
			XElement xElement = new XElement("Bounds");
			xElement.SetAttributeValue("X", Windowmode_Bounds.X);
			xElement.SetAttributeValue("Y", Windowmode_Bounds.Y);
			xElement.SetAttributeValue("W", Windowmode_Bounds.Width);
			xElement.SetAttributeValue("H", Windowmode_Bounds.Height);
			Element.Add(xElement);
			XElement xElement2 = new XElement("State");
			int windowmode_State = (int)Windowmode_State;
			xElement2.Value = windowmode_State.ToString();
			Element.Add(xElement2);
		}

		public void Save(string Filename)
		{
			XElement xElement = new XElement("Root");
			foreach (Setting value in AllSettings.Values)
			{
				XElement xElement2 = new XElement(value.SettingName);
				value.Save(xElement2);
				xElement.Add(xElement2);
			}
			XElement xElement3 = new XElement("WindowState");
			SaveWindowState(xElement3);
			xElement.Add(xElement3);
			xElement.Save(Filename);
		}

		public Settings Clone()
		{
			Settings settings = new Settings(Game);
			foreach (Setting value in AllSettings.Values)
			{
				settings.AllSettings[value.SettingName].CopyValue(value);
			}
			return settings;
		}

		public int GetNumericListSetting(string SettingName)
		{
			if (!AllSettings.ContainsKey(SettingName))
			{
				return 0;
			}
			return (AllSettings[SettingName] as NumberListSetting).Value;
		}

		public bool GetBooleanSetting(string SettingName)
		{
			if (!AllSettings.ContainsKey(SettingName))
			{
				return false;
			}
			return (AllSettings[SettingName] as BooleanSetting).Value;
		}

		public int GetIntSetting(string SettingName)
		{
			if (!AllSettings.ContainsKey(SettingName))
			{
				return 0;
			}
			return (AllSettings[SettingName] as NumericSetting).Value;
		}

		public DisplayResolution GetDisplaySetting(string SettingName)
		{
			if (!AllSettings.ContainsKey(SettingName))
			{
				return GetFullscreenDefault();
			}
			return (AllSettings[SettingName] as DisplayModeSetting).Value;
		}

		public int GetEnumeratedSetting(string SettingName)
		{
			if (!AllSettings.ContainsKey(SettingName))
			{
				return 0;
			}
			return (AllSettings[SettingName] as EnumeratedSetting).Value;
		}

		public string GetStringSetting(string SettingName)
		{
			if (!AllSettings.ContainsKey(SettingName))
			{
				return "";
			}
			return (AllSettings[SettingName] as StringListSetting).Value;
		}

		public void Revert(Settings OldSettings)
		{
			foreach (Setting value in OldSettings.AllSettings.Values)
			{
				AllSettings[value.SettingName].CopyValue(value);
			}
		}

		public bool ScreenChanged(Settings OldSettings)
		{
			if (OldSettings == null)
			{
				return false;
			}
			DisplayModeSetting displayModeSetting = AllSettings["DisplayMode"] as DisplayModeSetting;
			DisplayModeSetting displayModeSetting2 = OldSettings.AllSettings["DisplayMode"] as DisplayModeSetting;
			return displayModeSetting.Value != displayModeSetting2.Value;
		}

		public bool ScaleChanged(Settings OldSettings)
		{
			if (OldSettings == null)
			{
				return false;
			}
			EnumeratedSetting enumeratedSetting = AllSettings["UIScale"] as EnumeratedSetting;
			EnumeratedSetting enumeratedSetting2 = OldSettings.AllSettings["UIScale"] as EnumeratedSetting;
			return enumeratedSetting.Value != enumeratedSetting2.Value;
		}

		public bool AAChanged(Settings OldSettings)
		{
			if (OldSettings == null)
			{
				return false;
			}
			NumberListSetting numberListSetting = AllSettings["AA"] as NumberListSetting;
			NumberListSetting numberListSetting2 = OldSettings.AllSettings["AA"] as NumberListSetting;
			return numberListSetting.Value != numberListSetting2.Value;
		}

		public Key GetKeySetting(string SettingName)
		{
			return (AllSettings[SettingName] as KeybindSetting).Value;
		}
	}
}