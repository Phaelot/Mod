// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Game.OldSettings
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenTK;
using SovereigntyTK;
using SovereigntyTK.Game;

namespace SovereigntyTK.Game
{
	public class OldSettings
	{
		public DisplayResolution FullscreenMode;

		public bool FullScreen;

		public int AALevel;

		public int MusicVolume;

		public int SoundVolume;

		public bool TutorialsEnabled;

		public bool ViewBattlesAlly;

		public bool ViewBattlesEnemy;

		public bool ViewBattlesTrade;

		public string LanguageName;

		public int AutoBattleSpeed;

		private int GameDifficulty;

		public int TacticalBattleSpeed;

		public bool RightClickAction;

		public bool NoTacticalBattles;

		public int ScaleIndex;

		public WindowState Windowmode_State;

		public Rectangle Windowmode_Bounds;

		private string SettingsFile;

		private GameBase Game;

		public bool OldSettingsFound;

		private OldSettings(GameBase Game)
		{
			this.Game = Game;
		}

		public OldSettings(string SettingsFile, GameBase Game)
			: this(Game)
		{
			this.SettingsFile = SettingsFile;
			Load();
		}

		private bool OldSettingsFormat()
		{
			Stream stream = null;
			BinaryReader binaryReader = null;
			int num = 0;
			try
			{
				stream = File.OpenRead(SettingsFile);
				binaryReader = new BinaryReader(stream);
				num = binaryReader.ReadInt32();
				binaryReader.Close();
			}
			catch
			{
				OldSettingsFound = true;
				return true;
			}
			if (num >= 9)
			{
				return num > 100;
			}
			return true;
		}

		public int GetDifficulty()
		{
			return GameDifficulty;
		}

		public void SetDifficulty(int Difficulty)
		{
			GameDifficulty = Difficulty;
		}

		public void ChangeSoundVolume(int NewVolume)
		{
			if (NewVolume < 0)
			{
				NewVolume = 0;
			}
			if (NewVolume > 100)
			{
				NewVolume = 100;
			}
			SoundVolume = NewVolume;
			Game.Utilities.SoundManager.SoundVolume = (float)NewVolume / 100f;
			Game.Utilities.SoundManager.VolumeUpdateNeeded = true;
		}

		public void ChangeMusicVolume(int NewVolume)
		{
			if (NewVolume < 0)
			{
				NewVolume = 0;
			}
			if (NewVolume > 100)
			{
				NewVolume = 100;
			}
			MusicVolume = NewVolume;
			Game.Utilities.SoundManager.MusicVolume = (float)NewVolume / 100f;
			Game.Utilities.SoundManager.VolumeUpdateNeeded = true;
		}

		private void Create()
		{
			FullscreenMode = GetFullscreenDefault();
			FullScreen = true;
			AALevel = Game.GetMultiSampleModes().Last();
			MusicVolume = 30;
			SoundVolume = 50;
			TutorialsEnabled = true;
			ViewBattlesAlly = true;
			ViewBattlesEnemy = true;
			ViewBattlesTrade = false;
			NoTacticalBattles = false;
			LanguageName = "English";
			AutoBattleSpeed = 3;
			GameDifficulty = 2;
			TacticalBattleSpeed = 5;
			ScaleIndex = 2;
			RightClickAction = false;
			Windowmode_State = WindowState.Normal;
			Windowmode_Bounds = new Rectangle(0, 0, 1024, 768);
			Save();
		}

		public void Load()
		{
			Stream stream = null;
			BinaryReader binaryReader = null;
			try
			{
				stream = File.OpenRead(SettingsFile);
				binaryReader = new BinaryReader(stream);
				int num = binaryReader.ReadInt32();
				float RefreshRate = 60f;
				int ScreenWidth;
				if (num > 100)
				{
					ScreenWidth = num;
					num = 1;
				}
				else
				{
					ScreenWidth = binaryReader.ReadInt32();
				}
				int ScreenHeight = binaryReader.ReadInt32();
				if (num >= 8)
				{
					RefreshRate = binaryReader.ReadSingle();
				}
				List<DisplayResolution> allPossibleResolutions = GetAllPossibleResolutions();
				FullscreenMode = allPossibleResolutions.FirstOrDefault((DisplayResolution x) => x.Width == ScreenWidth && x.Height == ScreenHeight && x.RefreshRate == RefreshRate && x.BitsPerPixel == 32);
				if (FullscreenMode == null)
				{
					FullscreenMode = GetFullscreenDefault();
				}
				FullScreen = binaryReader.ReadBoolean();
				AALevel = binaryReader.ReadInt32();
				CheckAALevel();
				MusicVolume = binaryReader.ReadInt32();
				SoundVolume = binaryReader.ReadInt32();
				if (MusicVolume < 0)
				{
					MusicVolume = 0;
				}
				if (MusicVolume > 100)
				{
					MusicVolume = 100;
				}
				if (SoundVolume < 0)
				{
					SoundVolume = 0;
				}
				if (SoundVolume > 100)
				{
					SoundVolume = 100;
				}
				TutorialsEnabled = binaryReader.ReadBoolean();
				if (num >= 2)
				{
					ViewBattlesAlly = binaryReader.ReadBoolean();
					ViewBattlesEnemy = binaryReader.ReadBoolean();
					ViewBattlesTrade = binaryReader.ReadBoolean();
				}
				else
				{
					ViewBattlesAlly = true;
					ViewBattlesEnemy = true;
					ViewBattlesTrade = false;
				}
				if (num >= 3)
				{
					LanguageName = binaryReader.ReadString();
				}
				else
				{
					LanguageName = "English";
				}
				if (num >= 4)
				{
					AutoBattleSpeed = binaryReader.ReadInt32();
					GameDifficulty = binaryReader.ReadInt32();
				}
				else
				{
					AutoBattleSpeed = 3;
					GameDifficulty = 2;
				}
				if (AutoBattleSpeed < 1)
				{
					AutoBattleSpeed = 1;
				}
				if (AutoBattleSpeed > 5)
				{
					AutoBattleSpeed = 5;
				}
				if (GameDifficulty < 1)
				{
					GameDifficulty = 1;
				}
				if (GameDifficulty > 5)
				{
					GameDifficulty = 5;
				}
				if (num >= 5)
				{
					TacticalBattleSpeed = binaryReader.ReadInt32();
				}
				else
				{
					TacticalBattleSpeed = 5;
				}
				if (TacticalBattleSpeed < 1)
				{
					TacticalBattleSpeed = 1;
				}
				if (TacticalBattleSpeed > 10)
				{
					TacticalBattleSpeed = 10;
				}
				if (num >= 6)
				{
					RightClickAction = binaryReader.ReadBoolean();
				}
				else
				{
					RightClickAction = false;
				}
				if (num >= 7)
				{
					NoTacticalBattles = binaryReader.ReadBoolean();
				}
				else
				{
					NoTacticalBattles = false;
				}
				if (num >= 9)
				{
					Windowmode_Bounds.X = binaryReader.ReadInt32();
					Windowmode_Bounds.Y = binaryReader.ReadInt32();
					Windowmode_Bounds.Width = binaryReader.ReadInt32();
					Windowmode_Bounds.Height = binaryReader.ReadInt32();
					Windowmode_State = (WindowState)binaryReader.ReadInt16();
				}
				if (num >= 10)
				{
					ScaleIndex = binaryReader.ReadInt32();
				}
				else
				{
					ScaleIndex = 2;
				}
				if (ScaleIndex < 0)
				{
					ScaleIndex = 0;
				}
				if (ScaleIndex > 3)
				{
					ScaleIndex = 3;
				}
			}
			catch
			{
				if (binaryReader != null)
				{
					binaryReader.Close();
				}
				else
				{
					stream?.Close();
				}
				binaryReader = null;
				stream = null;
				Create();
			}
			finally
			{
				if (binaryReader != null)
				{
					binaryReader.Close();
				}
				else
				{
					stream?.Close();
				}
			}
		}

		public void Save()
		{
			Stream output = File.Create(SettingsFile);
			BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write(10);
			binaryWriter.Write(FullscreenMode.Width);
			binaryWriter.Write(FullscreenMode.Height);
			binaryWriter.Write(FullscreenMode.RefreshRate);
			binaryWriter.Write(FullScreen);
			binaryWriter.Write(AALevel);
			binaryWriter.Write(MusicVolume);
			binaryWriter.Write(SoundVolume);
			binaryWriter.Write(TutorialsEnabled);
			binaryWriter.Write(ViewBattlesAlly);
			binaryWriter.Write(ViewBattlesEnemy);
			binaryWriter.Write(ViewBattlesTrade);
			binaryWriter.Write(LanguageName);
			binaryWriter.Write(AutoBattleSpeed);
			binaryWriter.Write(GameDifficulty);
			binaryWriter.Write(TacticalBattleSpeed);
			binaryWriter.Write(RightClickAction);
			binaryWriter.Write(NoTacticalBattles);
			binaryWriter.Write(Windowmode_Bounds.X);
			binaryWriter.Write(Windowmode_Bounds.Y);
			binaryWriter.Write(Windowmode_Bounds.Width);
			binaryWriter.Write(Windowmode_Bounds.Height);
			binaryWriter.Write((short)Windowmode_State);
			binaryWriter.Write(ScaleIndex);
			binaryWriter.Close();
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
			foreach (DisplayResolution availableResolution in display.AvailableResolutions)
			{
				if (availableResolution.Width >= 1024 && availableResolution.Height >= 768 && availableResolution.BitsPerPixel >= 32)
				{
					list.Add(availableResolution);
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

		public OldSettings Clone()
		{
			OldSettings oldSettings = new OldSettings(Game);
			oldSettings.AALevel = AALevel;
			oldSettings.FullScreen = FullScreen;
			oldSettings.MusicVolume = MusicVolume;
			oldSettings.FullscreenMode = FullscreenMode;
			oldSettings.SoundVolume = SoundVolume;
			oldSettings.TutorialsEnabled = TutorialsEnabled;
			oldSettings.ViewBattlesAlly = ViewBattlesAlly;
			oldSettings.ViewBattlesEnemy = ViewBattlesEnemy;
			oldSettings.ViewBattlesTrade = ViewBattlesTrade;
			oldSettings.LanguageName = LanguageName;
			oldSettings.AutoBattleSpeed = AutoBattleSpeed;
			oldSettings.GameDifficulty = GameDifficulty;
			oldSettings.TacticalBattleSpeed = TacticalBattleSpeed;
			oldSettings.ScaleIndex = ScaleIndex;
			return oldSettings;
		}

		public void Revert(OldSettings OldSettings)
		{
			if (OldSettings != null)
			{
				AALevel = OldSettings.AALevel;
				FullScreen = OldSettings.FullScreen;
				ChangeMusicVolume(OldSettings.MusicVolume);
				ChangeSoundVolume(OldSettings.SoundVolume);
				FullscreenMode = OldSettings.FullscreenMode;
				FullScreen = OldSettings.FullScreen;
				TutorialsEnabled = OldSettings.TutorialsEnabled;
				ViewBattlesAlly = OldSettings.ViewBattlesAlly;
				ViewBattlesEnemy = OldSettings.ViewBattlesEnemy;
				ViewBattlesTrade = OldSettings.ViewBattlesTrade;
				LanguageName = OldSettings.LanguageName;
				AutoBattleSpeed = OldSettings.AutoBattleSpeed;
				GameDifficulty = OldSettings.GameDifficulty;
				TacticalBattleSpeed = OldSettings.TacticalBattleSpeed;
				ScaleIndex = OldSettings.ScaleIndex;
			}
		}

		private void CheckAALevel()
		{
			List<int> multiSampleModes = Game.GetMultiSampleModes();
			for (int i = 0; i < multiSampleModes.Count; i++)
			{
				if (multiSampleModes[i] == AALevel)
				{
					return;
				}
			}
			AALevel = 0;
		}

		public void ReduceAA()
		{
			List<int> multiSampleModes = Game.GetMultiSampleModes();
			for (int i = 0; i < multiSampleModes.Count; i++)
			{
				if (multiSampleModes[i] == AALevel && i > 0)
				{
					AALevel = multiSampleModes[i - 1];
					break;
				}
			}
		}

		public void IncreaseAA()
		{
			List<int> multiSampleModes = Game.GetMultiSampleModes();
			for (int i = 0; i < multiSampleModes.Count; i++)
			{
				if (multiSampleModes[i] == AALevel && i < multiSampleModes.Count - 1)
				{
					AALevel = multiSampleModes[i + 1];
					break;
				}
			}
		}

		public bool AAChanged(OldSettings OldSettings)
		{
			if (OldSettings == null)
			{
				return false;
			}
			return OldSettings.AALevel != AALevel;
		}

		public bool ScreenChanged(OldSettings OldSettings)
		{
			if (OldSettings == null)
			{
				return false;
			}
			if (OldSettings.FullScreen != FullScreen)
			{
				return true;
			}
			if (OldSettings.FullscreenMode != FullscreenMode)
			{
				return true;
			}
			return false;
		}
	}
}