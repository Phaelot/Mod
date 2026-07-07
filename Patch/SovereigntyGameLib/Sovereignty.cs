// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Sovereignty
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using OpenTK;
using OpenTK.Input;
using SovereigntyTK;
using SovereigntyTK.Game;
using SovereigntyTK.Game.Campaign;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Map;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK
{
	public class Sovereignty : GameBase
	{
		public SovereigntyGame CurrentGame;

		public SovereigntyData Data;

		public CampaignMap Map;

		public SaveManager SaveManager;

		public MessageManager MessageHandler;

		public BattleMap CurrentBattleMap;

		public TradeRestrictionData TradeRestrictions;

		public MusicTypes CurrentMusicType;

		public string CurrentMusicRace;

		private UIControl CurrentRoot;

		private CheatConsole Console;

		public bool FirstStart = true;

		internal bool DebugMessagesEnabled;

		public bool UnitIDsEnabled;

		public bool HeroIDsEnabled;

		private bool ConsoleEnabled;

		private List<string> UIWhitelist;

		public bool PreventSelfMoves;

		public bool PreventEnemyMoves;

		public bool ShowMoveOverlay;

		public int CurrentSaveNumber;

		private SovereigntyGame OldGame;

		private AchievementManager AchievementManager;

		public bool BlockEndTurn;

		public List<ModData> ActiveMods;

		public event Action OnViewportChanged;

		public event PointDelegate OnMousePositionChanged;

		public Icon GetExecutableIcon()
		{
			ExtractIconEx(Application.ExecutablePath, 0, out var piLargeVersion, out var _, 1);
			if (piLargeVersion.ToInt32() == 0)
			{
				return null;
			}
			return Icon.FromHandle(piLargeVersion);
		}

		[DllImport("Shell32")]
		public static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

		public Sovereignty(GameWindow Window)
			: base(Window, "Sovereignty: Crown of Kings", "Sovereignty")
		{
			UIWhitelist = new List<string>();
			TradeRestrictions = new TradeRestrictionData();
			Icon executableIcon = GetExecutableIcon();
			if (executableIcon != null)
			{
				Window.Icon = executableIcon;
			}
		}

		public void ClearUIRestrictions()
		{
			UIWhitelist.Clear();
		}

		public void AddUIAllowedAction(string Action)
		{
			UIWhitelist.Add(Action);
		}

		public bool UIHasRestictions()
		{
			return UIWhitelist.Count > 0;
		}

		public bool UIActionAllowed(string Action)
		{
			if (UIWhitelist.Count == 0)
			{
				return true;
			}
			return UIWhitelist.Contains(Action);
		}

		public void SetFullscreenMode()
		{
			try
			{
				WriteLog("Setting up full screen mode");
				DisplayDevice.GetDisplay(DisplayIndex.Primary).ChangeResolution(Settings.GetDisplaySetting("DisplayMode"));
				Window.WindowBorder = WindowBorder.Hidden;
				Window.WindowState = WindowState.Fullscreen;
				Window_Resize(null, null);
			}
			catch
			{
				WriteLog("Full screen window creation failed, falling back to window");
				SetWindowedMode();
				if (MessageHandler != null)
				{
					MessageHandler.ShowInfoMessage("MSG_SCREENFAIL_TITLE", "MSG_SCREENFAIL_TEXT");
				}
			}
		}

		public void SetWindowedMode()
		{
			WriteLog("Setting up windowed mode");
			DisplayDevice.GetDisplay(DisplayIndex.Primary).RestoreResolution();
			Window.WindowBorder = WindowBorder.Resizable;
			Window.Bounds = Settings.Windowmode_Bounds;
			Window.WindowState = Settings.Windowmode_State;
			Window_Resize(null, null);
		}

		public override void Init()
		{
			Utilities.FileSystem.CreateSaveDirectory();
			GetAutosaveNumber();
			Window.WindowStateChanged += Window_WindowStateChanged;
			Window.Resize += Window_Resize;
			Window.Move += Window_Move;
			WriteLog("Setting UI scaling mode");
			UIManager.SetScaleIndex(Settings.GetEnumeratedSetting("UIScale"));
			if (Settings.GetBooleanSetting("Fullscreen"))
			{
				SetFullscreenMode();
			}
			else
			{
				SetWindowedMode();
			}
			WriteLog("Setting defaults for sound system");
			Utilities.SoundManager.MusicVolume = (float)Settings.GetIntSetting("MusicVolume") * 0.01f;
			Utilities.SoundManager.SoundVolume = (float)Settings.GetIntSetting("SoundVolume") * 0.01f;
			SoundManager soundManager = Utilities.SoundManager;
			soundManager.OnMusicFinished = (Action)Delegate.Combine(soundManager.OnMusicFinished, new Action(MusicFinished));
			WriteLog("Loading Mod List");
			ActiveMods = new List<ModData>();
			if (Utilities.FileSystem.FileExists("ModList.xml", FileTypes.User))
			{
				Stream stream = Utilities.FileSystem.OpenFile("ModList.xml", FileTypes.User);
				XElement xElement = XElement.Load(stream);
				foreach (XElement item2 in xElement.Elements())
				{
					string value = item2.Value;
					string filename = "Mods" + System.IO.Path.DirectorySeparatorChar + value + System.IO.Path.DirectorySeparatorChar + "data.xml";
					if (Utilities.FileSystem.FileExists(filename, FileTypes.User))
					{
						filename = Utilities.FileSystem.ConvertFilename(filename, FileTypes.User);
						ModData item = new ModData(filename);
						ActiveMods.Add(item);
					}
				}
			}
			WriteLog("Loading game database");
			Data = new SovereigntyData(this);
			Data.LoadData("Data\\sov_data.xml");
			Data.LoadTables("Data\\sov_tables.xml");
			Data.LoadSpellTree("Data\\SpellTrees.xml");
			WriteLog("Loading map list");
			Data.LoadWorlds(Utilities.FileSystem, ActiveMods);
			WriteLog("Loading Mods");
			Data.LoadMods(ActiveMods);
			Utilities.CreateScriptManager(ActiveMods);
			Data.LoadTables("Data\\sov_tables.xml");
			Utilities.TextManager.SetLanguage(Settings.GetStringSetting("Language"));
			WriteLog("Creating game camera");
			Camera = new GameCamera(this);
			Camera.SetBounds(new RectangleF(-500f, -500f, 5000f, 3400f));
			Camera.MinZoomLevel = 800f;
			Map = new CampaignMap(this);
			WriteLog("Initialising save system");
			SaveManager = new SaveManager(this);
			if (Settings.BackupSaves)
			{
				SaveManager.BackupOldSaves();
			}
			MessageHandler = new MessageManager(this);
			WriteLog("Loading TitleForm UI from file");
			XElement rootElement = XElement.Load(Utilities.FileSystem.OpenFile("Data\\UI\\TitleForm.xml", FileTypes.Application));
			ControlForm controlForm = new ControlForm(this);
			controlForm.LoadFromXML(rootElement);
			CurrentRoot = controlForm;
			UIManager.AddControl(controlForm);
			if (Utilities.FileSystem.FileExists("debug.txt", FileTypes.User))
			{
				Stream stream2 = Utilities.FileSystem.OpenFile("debug.txt", FileTypes.User);
				StreamReader streamReader = new StreamReader(stream2);
				string text = streamReader.ReadLine();
				streamReader.Close();
				ConsoleEnabled = text != null && text.ToLowerInvariant() == "console=1";
			}
			AchievementManager = new AchievementManager(this);
			Camera.Reset();
		}

		private void GetAutosaveNumber()
		{
			string filename = "Saves\\saves.dat";
			if (!Utilities.FileSystem.FileExists(filename, FileTypes.User))
			{
				CurrentSaveNumber = 1;
				return;
			}
			Stream input = Utilities.FileSystem.OpenFile(filename, FileTypes.User);
			BinaryReader binaryReader = new BinaryReader(input);
			CurrentSaveNumber = binaryReader.ReadInt32();
			binaryReader.Close();
		}

		internal void RecordAutosaveNumber()
		{
			string filename = "Saves\\saves.dat";
			Stream output = Utilities.FileSystem.OpenFile(filename, FileTypes.User, FileModes.ReadWrite);
			BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write(CurrentSaveNumber);
			binaryWriter.Close();
		}

		private void MusicFinished()
		{
			switch (CurrentMusicType)
			{
				case MusicTypes.Background:
					PlayBackgroundMusic();
					break;
				case MusicTypes.Battle:
					PlayBattleMusic(CurrentMusicRace);
					break;
				case MusicTypes.BattleEnded:
					CurrentMusicType = MusicTypes.Background;
					PlayBackgroundMusic();
					break;
			}
		}

		public void PlayBackgroundMusic()
		{
			CurrentMusicType = MusicTypes.Background;
			Utilities.SoundManager.EndMusic();
			Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\Ambient.ogg");
		}

		public void PlayBattleMusic(string RaceName)
		{
			CurrentMusicRace = RaceName;
			CurrentMusicType = MusicTypes.Battle;
			Utilities.SoundManager.EndMusic();
			switch (RaceName)
			{
				case "Elf":
					Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\Elvish_Theme.ogg");
					break;
				case "Orc":
					Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\Orcish_Theme.ogg");
					break;
				case "Human":
					Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\Human_Theme.ogg");
					break;
				case "Undead":
					Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\Undead_Theme.ogg");
					break;
				case "Viking":
					Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\Viking_Theme.ogg");
					break;
				default:
					Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\Ambient.ogg");
					break;
			}
		}

		public void PlayVictoryMusic()
		{
			CurrentMusicType = MusicTypes.BattleEnded;
			Utilities.SoundManager.EndMusic();
			Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\BattleWon3.ogg");
		}

		public void PlayDefeatMusic()
		{
			CurrentMusicType = MusicTypes.BattleEnded;
			Utilities.SoundManager.EndMusic();
			Utilities.SoundManager.PlayMusic("Data\\Sound\\Music\\BattleLost.ogg");
		}

		private void Window_Move(object sender, EventArgs e)
		{
			Settings.Windowmode_Bounds = Window.Bounds;
		}

		private new void Window_Resize(object sender, EventArgs e)
		{
			base.Window_Resize(sender, e);
			Settings.Windowmode_Bounds = Window.Bounds;
		}

		private void Window_WindowStateChanged(object sender, EventArgs e)
		{
			if (Window.WindowState != WindowState.Fullscreen)
			{
				Settings.Windowmode_State = Window.WindowState;
			}
		}

		public void StartGame(string RealmName, CampaignBase Campaign, bool Ironman, string IronmanName = null)
		{
			CurrentGame = new SovereigntyGame(this, Campaign, Ironman);
			CurrentGame.IronmanName = IronmanName;
			Campaign.InitCampaign();
			CurrentGame.Init(RealmName);
			UIManager.RemoveControl(CurrentRoot);
			CurrentRoot.Dispose();
			XElement rootElement = XElement.Load(Utilities.FileSystem.OpenFile("Data\\UI\\MainForm.xml", FileTypes.Application));
			ControlForm controlForm = new ControlForm(this);
			controlForm.LoadFromXML(rootElement);
			CurrentRoot = controlForm;
			UIManager.AddControl(controlForm);
			CurrentGame.CurrentCampaign.BeginCampaign();
			Map.ChangeMode(MapModes.Default);
			Map.GameStarted();
			Point capitolCoords = CurrentGame.PlayerRealm.CapitolProvince.CapitolCoords;
			float y = 900f;
			if (Camera.CamPos.Y < 900f)
			{
				y = Camera.CamPos.Y;
			}
			Camera.BeginAutoMove(new Vector3(capitolCoords.X, y, capitolCoords.Y), 1f);
		}

		public void SaveGame(string Filename)
		{
			Stream output = Utilities.FileSystem.OpenFile(Filename, FileTypes.User, FileModes.ReadWrite);
			BinaryWriter binaryWriter = new BinaryWriter(output);
			SaveManager.Save(binaryWriter, TurnStart: false);
			binaryWriter.Close();
		}

		public void LoadGame(string Filename)
		{
			Stream stream = Utilities.FileSystem.OpenFile(Filename, FileTypes.User);
			if (stream == null)
			{
				MessageHandler.ShowInfoMessage("MSG_FILEMISSING_TITLE", "MSG_FILEMISSING_TEXT");
				return;
			}
			BinaryReader binaryReader = new BinaryReader(stream);
			SaveData Data = SaveManager.Load(binaryReader);
			if (Data.Version == 52)
			{
				MessageHandler.ShowInfoMessage("MSG_FILEBAD_TITLE", "MSG_FILEBAD_TEXT");
				return;
			}
			this.Data.LoadWorld(this.Data.Worlds.First((NewWorldData x) => x.InternalName == Data.WorldName));
			RealmData value = null;
			this.Data.ActiveRealms.TryGetValue(Data.RealmName, out value);
			if (value == null)
			{
				throw new Exception("Realm " + Data.RealmName + " does not exist");
			}
			List<CampaignBase> campaignsForRealm = GetCampaignsForRealm(value, IncludeTutorial: true);
			CampaignBase campaignBase = campaignsForRealm.FirstOrDefault((CampaignBase x) => x.ID == Data.CampaignID);
			foreach (CampaignBase item in campaignsForRealm)
			{
				if (item != campaignBase)
				{
					item.Dispose();
				}
			}
			if (campaignBase == null)
			{
				throw new Exception("Campaign " + Data.CampaignID + " does not exist");
			}
			CurrentGame = new SovereigntyGame(this, campaignBase, Data.Hardcore);
			campaignBase.InitCampaign();
			CurrentGame.Load(binaryReader, Data.Version);
			campaignBase.Load(binaryReader, Data.Version);
			CurrentGame.IronmanName = Data.IronManName;
			bool flag = binaryReader.ReadBoolean();
			binaryReader.Close();
			UIManager.RemoveControl(CurrentRoot);
			CurrentRoot.Dispose();
			XElement rootElement = XElement.Load(Utilities.FileSystem.OpenFile("Data\\UI\\MainForm.xml", FileTypes.Application));
			ControlForm controlForm = new ControlForm(this);
			controlForm.LoadFromXML(rootElement);
			CurrentRoot = controlForm;
			UIManager.AddControl(controlForm);
			CurrentGame.CurrentCampaign.RestoreCampaign();
			Map.ChangeMode(MapModes.Default);
			Map.GameStarted();
			CurrentGame.PostInit();
			if (CurrentGame.CurrentTacticalBattle == null)
			{
				Point capitolCoords = CurrentGame.PlayerRealm.CapitolProvince.CapitolCoords;
				float y = 700f;
				if (Camera.CamPos.Y < 700f)
				{
					y = Camera.CamPos.Y;
				}
				Camera.BeginAutoMove(new Vector3(capitolCoords.X, y, capitolCoords.Y), 1f);
			}
			if (flag)
			{
				CurrentGame.TurnController.RedoTurnStart();
			}
		}

		public void EndGame()
		{
			FireEvent("ClearPendingMessages");
			ClearUIRestrictions();
			UIManager.RemoveControl(CurrentRoot);
			CurrentRoot.Dispose();
			CurrentGame.Dispose();
			if (CurrentGame.PendingDispose)
			{
				OldGame = CurrentGame;
			}
			CurrentGame = null;
			XElement rootElement = XElement.Load(Utilities.FileSystem.OpenFile("Data\\UI\\TitleForm.xml", FileTypes.Application));
			ControlForm controlForm = new ControlForm(this);
			controlForm.LoadFromXML(rootElement);
			UIManager.AddControl(controlForm);
			CurrentRoot = controlForm;
			Map.ChangeMode(MapModes.RealmSelect, Force: true);
			Map.GameEnded();
			Camera.Reset();
		}

		public override void Update()
		{
		}

		public override void Render(float ElapsedTime)
		{
			if (CurrentGame != null)
			{
				CurrentGame.Update(ElapsedTime);
			}
			if (OldGame != null)
			{
				OldGame.Update(ElapsedTime);
				if (!OldGame.PendingDispose)
				{
					OldGame = null;
				}
			}
			Camera.Update();
			Map.Render(ElapsedTime);
			if (CurrentBattleMap != null)
			{
				CurrentBattleMap.Render(ElapsedTime);
			}
		}

		public override void ShutDown()
		{
			if (AchievementManager != null)
			{
				AchievementManager.Dispose();
			}
			if (Map != null)
			{
				Map.Dispose();
			}
			if (Settings != null)
			{
				if (Settings.GetBooleanSetting("Fullscreen"))
				{
					SetWindowedMode();
				}
				SaveSettings();
			}
		}

		public void SaveSettings()
		{
			string text = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + System.IO.Path.DirectorySeparatorChar + "Sovereignty";
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			Settings.Save(text + System.IO.Path.DirectorySeparatorChar + "settings.xml");
		}

		public override void HandleWheelDown(MouseWheelEventArgs e)
		{
			Map.HandleWheelDown(e);
		}

		public override void HandleWheelUp(MouseWheelEventArgs e)
		{
			Map.HandleWheelUp(e);
		}

		public override void HandleKeyDown(KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Period && CurrentGame != null)
			{
				CurrentGame.IgnoreHumanPlayer = !CurrentGame.IgnoreHumanPlayer;
			}
			if (e.Key == Key.F2)
			{
				if (!ConsoleEnabled)
				{
					return;
				}
				if (Console == null)
				{
					Console = new CheatConsole(this);
				}
				Console.Toggle();
			}
			if (e.Key == Key.F8 && CurrentGame != null)
			{
				CurrentGame.Stats.Display();
			}
			if (CurrentBattleMap != null)
			{
				CurrentBattleMap.HandleKeyDown(e);
			}
			else
			{
				Map.HandleKeyDown(e);
			}
			FireEvent("KeyDown", e.Key);
		}

		public override void HandleKeyUp(KeyboardKeyEventArgs e)
		{
			if (CurrentBattleMap != null)
			{
				CurrentBattleMap.HandleKeyUp(e);
			}
			else
			{
				Map.HandleKeyUp(e);
			}
		}

		internal override void HandleMousePositionChanged(int x, int y)
		{
			base.HandleMousePositionChanged(x, y);
			if (this.OnMousePositionChanged != null)
			{
				this.OnMousePositionChanged(new Point(x, y));
			}
		}

		public override void HandleMouseMove(MouseMoveEventArgs e)
		{
			if (CurrentBattleMap != null)
			{
				CurrentBattleMap.HandleMouseMove(e);
			}
			else if (!Utilities.SpriteManager.HandleMouseMove(e))
			{
				Map.HandleMouseMove(e);
			}
		}

		public override void HandleMouseDown(MouseButtonEventArgs e)
		{
			if (CurrentBattleMap != null)
			{
				CurrentBattleMap.HandleMouseDown(e);
			}
			else if (!Utilities.SpriteManager.HandleMouseDown(e))
			{
				Map.HandleMouseDown(e);
			}
		}

		public override void HandleGeneralMouseUp(MouseButtonEventArgs e)
		{
			Map.ForceMouseUp(e.Button);
		}

		public override void HandleMouseUp(MouseButtonEventArgs e)
		{
			if (CurrentBattleMap != null)
			{
				CurrentBattleMap.HandleMouseUp(e);
			}
			else if (!Utilities.SpriteManager.HandleMouseUp(e))
			{
				Map.HandleMouseUp(e);
			}
		}

		protected override void ViewportChanged()
		{
			base.ViewportChanged();
			if (Camera != null)
			{
				Camera.ViewportChanged();
			}
			if (this.OnViewportChanged != null)
			{
				this.OnViewportChanged();
			}
		}

		public CampaignBase GetNamedCampaign(string CampaignName, string RealmName)
		{
			List<Type> list = (from t in Utilities.ScriptManager.CampaignAssembly.GetTypes()
							   where t.IsSubclassOf(typeof(CampaignBase))
							   select t).ToList();
			foreach (Type item in list)
			{
				if (item.GetConstructors().Count((ConstructorInfo x) => x.GetParameters().Count() == 2) != 0)
				{
					CampaignBase campaignBase = (CampaignBase)Activator.CreateInstance(item, this, RealmName);
					if (campaignBase.ID == CampaignName)
					{
						return campaignBase;
					}
					campaignBase.Dispose();
				}
			}
			return null;
		}

		public List<CampaignBase> GetCampaignsForRealm(RealmData Realm, bool IncludeTutorial = false)
		{
			List<CampaignBase> list = new List<CampaignBase>();
			List<Type> list2 = (from t in Utilities.ScriptManager.CampaignAssembly.GetTypes()
								where t.IsSubclassOf(typeof(CampaignBase))
								select t).ToList();
			foreach (Type item2 in list2)
			{
				if (item2.GetConstructors().Count((ConstructorInfo x) => x.GetParameters().Count() == 2) != 0)
				{
					CampaignBase item = (CampaignBase)Activator.CreateInstance(item2, this, Realm.Name);
					list.Add(item);
				}
			}
			List<CampaignBase> list3 = list.Where((CampaignBase x) => !x.CampaignAvailable(Realm)).ToList();
			list = (from x in list
					where x.CampaignAvailable(Realm) || (IncludeTutorial && x.ID == "Tutorial")
					orderby x.CampaignOrder
					select x).ToList();
			foreach (CampaignBase item3 in list3)
			{
				item3.Dispose();
			}
			return list;
		}

		public void UpdateScreenMode()
		{
			if (Settings.GetBooleanSetting("Fullscreen"))
			{
				SetFullscreenMode();
			}
			else
			{
				SetWindowedMode();
			}
		}

		public void SetCameraForNewGame()
		{
			RectangleF bounds = Camera.Bounds;
			Camera.SetBounds(new RectangleF(0f, 0f, 4500f, 2500f));
			Camera.CamPos = new Vector3(1672f, 100f, 1272f);
			Camera.ZoomOutMaximum();
			Camera.SetBounds(bounds);
		}

		public GameText GetVersionText()
		{
			switch (GlobalData.VersionType)
			{
				case VersionTypes.EarlyAccess:
					{
						GameText gameText3 = GameText.CreateLocalised("VERSIONTEXT", GlobalData.VERSION_EA_MAJOR, GlobalData.VERSION_EA_MINOR, GlobalData.VERSION_EA_REVISION, GlobalData.VERSION_BUILD);
						gameText3.AddChildText(GameText.CreateLocalised("VERSION_EA"));
						return gameText3;
					}
				case VersionTypes.ReleaseCandidate:
					return GameText.CreateLocalised("RCTEXT", GlobalData.VERSION_RC, GlobalData.VERSION_BUILD);
				case VersionTypes.Public:
					{
						GameText gameText2 = GameText.CreateLocalised("VERSIONTEXT", GlobalData.VERSION_MAJOR, GlobalData.VERSION_MINOR, GlobalData.VERSION_REVISION, GlobalData.VERSION_BUILD);
						gameText2.AddChildText(GameText.CreateLocalised("VERSION_PUBLIC"));
						return gameText2;
					}
				default:
					{
						GameText gameText = GameText.CreateLocalised("VERSIONTEXT", GlobalData.VERSION_MAJOR, GlobalData.VERSION_MINOR, GlobalData.VERSION_REVISION, GlobalData.VERSION_BUILD);
						gameText.AddChildText(GameText.CreateLocalised("VERSION_PUBLIC"));
						return gameText;
					}
			}
		}

		public bool IsBeta()
		{
			return GlobalData.VersionType == VersionTypes.EarlyAccess;
		}
	}
}