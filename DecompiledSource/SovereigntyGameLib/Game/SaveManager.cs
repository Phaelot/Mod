using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game
{
	public class SaveManager
	{
		public SaveManager(Sovereignty Game)
		{
			this.Game = Game;
		}

		private string GetCurrentKey(string CurrentGameID)
		{
			Stream stream = this.Game.Utilities.FileSystem.OpenFile("Hardcore.dat", FileTypes.User, FileModes.ReadOnly, true);
			if (stream == null)
			{
				stream = this.Game.Utilities.FileSystem.CreateFile("Hardcore.dat", FileTypes.User);
				BinaryWriter binaryWriter = new BinaryWriter(stream);
				binaryWriter.Write(0);
				binaryWriter.Close();
				stream = this.Game.Utilities.FileSystem.OpenFile("Hardcore.dat", FileTypes.User, FileModes.ReadOnly, true);
			}
			BinaryReader binaryReader = new BinaryReader(stream);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				dictionary.Add(binaryReader.ReadString(), binaryReader.ReadString());
			}
			binaryReader.Close();
			string text = null;
			dictionary.TryGetValue(CurrentGameID, out text);
			return text;
		}

		public List<SaveData> GetSavegameList(bool Hardcore, bool Autosaves)
		{
			List<Tuple<string, bool>> list = new List<Tuple<string, bool>>();
			if (!Hardcore)
			{
				foreach (string text in this.Game.Utilities.FileSystem.GetFilenamesOfType("Saves\\Manual", "sov", FileTypes.User))
				{
					list.Add(new Tuple<string, bool>(text, false));
				}
				if (Autosaves)
				{
					foreach (string text2 in this.Game.Utilities.FileSystem.GetFilenamesOfType("Saves\\Auto", "sov", FileTypes.User))
					{
						list.Add(new Tuple<string, bool>(text2, true));
					}
				}
			}
			else
			{
				foreach (string text3 in this.Game.Utilities.FileSystem.GetFilenamesOfType("Saves\\Ironman", "sov", FileTypes.User))
				{
					list.Add(new Tuple<string, bool>(text3, false));
				}
			}
			List<SaveData> list2 = new List<SaveData>();
			for (int l = 0; l < list.Count; l++)
			{
				SaveData saveData = new SaveData();
				saveData.ShortFilename = System.IO.Path.GetFileNameWithoutExtension(list[l].Item1);
				saveData.FullFilename = list[l].Item1;
				saveData.Date = File.GetLastWriteTime(list[l].Item1);
				saveData.Auto = list[l].Item2;
				BinaryReader binaryReader = null;
				try
				{
					Stream stream = this.Game.Utilities.FileSystem.OpenFile(list[l].Item1, FileTypes.User, FileModes.ReadOnly, false);
					binaryReader = new BinaryReader(stream);
					if (stream.Length >= 8L)
					{
						int num = binaryReader.ReadInt32();
						int num2 = binaryReader.ReadInt32();
						if (num2 >= 26)
						{
							if (num == 55734563 && num2 <= GlobalData.SAVEVERSION_CURRENT && num2 >= 1)
							{
								saveData.Version = num2;
								saveData.CurrentGameID = binaryReader.ReadString();
								string text4 = binaryReader.ReadString();
								saveData.RealmDisplayName = binaryReader.ReadString();
								saveData.TurnNumber = binaryReader.ReadInt32();
								string currentKey = this.GetCurrentKey(saveData.CurrentGameID);
								if (currentKey != null)
								{
									saveData.Hardcore = true;
									if (text4 != currentKey)
									{
										goto IL_03ED;
									}
								}
								else
								{
									saveData.Hardcore = false;
								}
								saveData.CampaignID = binaryReader.ReadString();
								saveData.RealmName = binaryReader.ReadString();
								if (saveData.Hardcore)
								{
									saveData.IronManName = binaryReader.ReadString();
								}
								saveData.CampaignDisplayName = binaryReader.ReadString();
								if (saveData.Version > 45)
								{
									saveData.WorldName = binaryReader.ReadString();
									int num3 = binaryReader.ReadInt32();
									for (int m = 0; m < num3; m++)
									{
										saveData.ActiveModIDs.Add(binaryReader.ReadString());
										saveData.ActiveModNames.Add(binaryReader.ReadString());
									}
								}
								else
								{
									saveData.WorldName = this.Game.Data.Worlds[0].InternalName;
								}
								bool flag = true;
								using (List<string>.Enumerator enumerator = saveData.ActiveModIDs.GetEnumerator())
								{
									while (enumerator.MoveNext())
									{
										string ModID = enumerator.Current;
										if (this.Game.ActiveMods.Count((ModData x) => x.ID == ModID) == 0)
										{
											flag = false;
										}
									}
								}
								foreach (ModData modData in this.Game.ActiveMods)
								{
									if (!saveData.ActiveModIDs.Contains(modData.ID))
									{
										flag = false;
									}
								}
								if (flag)
								{
									if (Hardcore == saveData.Hardcore)
									{
										list2.Add(saveData);
									}
								}
							}
						}
					}
				}
				catch
				{
				}
				finally
				{
					if (binaryReader != null)
					{
						binaryReader.Close();
					}
				}
				IL_03ED:;
			}
			list2.Sort(new Comparison<SaveData>(this.CompareSaveData));
			return list2;
		}

		public SaveData Load(BinaryReader r)
		{
			SaveData saveData = new SaveData();
			r.ReadInt32();
			saveData.Version = r.ReadInt32();
			saveData.CurrentGameID = r.ReadString();
			r.ReadString();
			saveData.RealmDisplayName = r.ReadString();
			saveData.TurnNumber = r.ReadInt32();
			string currentKey = this.GetCurrentKey(saveData.CurrentGameID);
			if (currentKey != null)
			{
				saveData.Hardcore = true;
			}
			else
			{
				saveData.Hardcore = false;
			}
			saveData.CampaignID = r.ReadString();
			saveData.RealmName = r.ReadString();
			if (saveData.Hardcore)
			{
				saveData.IronManName = r.ReadString();
			}
			saveData.CampaignDisplayName = r.ReadString();
			if (saveData.Version >= 45)
			{
				saveData.WorldName = r.ReadString();
				int num = r.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					saveData.ActiveModIDs.Add(r.ReadString());
					saveData.ActiveModNames.Add(r.ReadString());
				}
			}
			else
			{
				saveData.WorldName = this.Game.Data.Worlds[0].InternalName;
			}
			return saveData;
		}

		public void BackupOldSaves()
		{
			List<string> list = new List<string>(this.Game.Utilities.FileSystem.GetFilenamesOfType("", "sov", FileTypes.User));
			foreach (string text in list)
			{
				string text2 = text.Replace("Sovereignty\\", "Sovereignty\\Backup\\Saves\\");
				string directoryName = System.IO.Path.GetDirectoryName(text2);
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				File.Move(text, text2);
			}
			list = new List<string>(this.Game.Utilities.FileSystem.GetFilenamesOfType("Autosaves\\", "sov", FileTypes.User));
			foreach (string text3 in list)
			{
				string text4 = text3.Replace("Sovereignty\\", "Sovereignty\\Backup\\Saves\\Auto\\");
				string directoryName2 = System.IO.Path.GetDirectoryName(text4);
				if (!Directory.Exists(directoryName2))
				{
					Directory.CreateDirectory(directoryName2);
				}
				File.Move(text3, text4);
			}
			this.Game.Utilities.FileSystem.DeleteUserDirectory("Autosaves");
		}

		public void Autosave(bool TurnStart)
		{
			if (this.Game.CurrentGame.CurrentCampaign.ID == "Tutorial")
			{
				return;
			}
			if (this.Game.CurrentGame.Ironman)
			{
				if (this.Game.CurrentGame.PlayerRealm.RealmIsDead)
				{
					string text = "Saves\\Ironman\\" + this.Game.CurrentGame.GameID + ".sov";
					this.Game.Utilities.FileSystem.DeleteFile(text, FileTypes.User, true);
					return;
				}
				string text2 = "Saves\\Ironman\\" + this.Game.CurrentGame.GameID + ".sov";
				Stream stream = this.Game.Utilities.FileSystem.OpenFile(text2, FileTypes.User, FileModes.ReadWrite, true);
				BinaryWriter binaryWriter = new BinaryWriter(stream);
				this.Save(binaryWriter, TurnStart);
				binaryWriter.Close();
				return;
			}
			else
			{
				if (this.Game.CurrentGame.PlayerRealm.RealmIsDead)
				{
					return;
				}
				string text3 = "Saves\\Auto\\Autosave_" + this.Game.CurrentSaveNumber + ".sov";
				Stream stream2 = this.Game.Utilities.FileSystem.OpenFile(text3, FileTypes.User, FileModes.ReadWrite, true);
				BinaryWriter binaryWriter2 = new BinaryWriter(stream2);
				this.Save(binaryWriter2, TurnStart);
				binaryWriter2.Close();
				this.Game.CurrentSaveNumber++;
				if (this.Game.CurrentSaveNumber > 10)
				{
					this.Game.CurrentSaveNumber = 1;
				}
				this.Game.RecordAutosaveNumber();
				return;
			}
		}

		public void Save(BinaryWriter w, bool TurnStart)
		{
			SovereigntyGame currentGame = this.Game.CurrentGame;
			string text = Guid.NewGuid().ToString();
			if (currentGame.Ironman)
			{
				this.UpdateIronmanKeys(currentGame.GameID, text);
			}
			w.Write(55734563);
			w.Write(GlobalData.SAVEVERSION_CURRENT);
			w.Write(currentGame.GameID);
			w.Write(text);
			w.Write(currentGame.PlayerRealm.DisplayName);
			w.Write(currentGame.TurnController.TurnNumber);
			w.Write(currentGame.CurrentCampaign.ID);
			w.Write(currentGame.PlayerRealm.Name);
			if (currentGame.Ironman)
			{
				w.Write(currentGame.IronmanName);
			}
			w.Write(currentGame.CurrentCampaign.NameText.TextName);
			w.Write(currentGame.GameCore.Data.CurrentWorld.InternalName);
			w.Write(currentGame.GameCore.ActiveMods.Count);
			foreach (ModData modData in currentGame.GameCore.ActiveMods)
			{
				w.Write(modData.ID);
				w.Write(modData.Name);
			}
			currentGame.Save(w);
			currentGame.CurrentCampaign.Save(w);
			w.Write(TurnStart);
		}

		private void UpdateIronmanKeys(string CurrentGameID, string CurrentSaveID)
		{
			Stream stream = this.Game.Utilities.FileSystem.OpenFile("Hardcore.dat", FileTypes.User, FileModes.ReadOnly, true);
			if (stream == null)
			{
				stream = this.Game.Utilities.FileSystem.OpenFile("Hardcore.dat", FileTypes.User, FileModes.ReadWrite, true);
				BinaryWriter binaryWriter = new BinaryWriter(stream);
				binaryWriter.Write(0);
				binaryWriter.Close();
				stream = this.Game.Utilities.FileSystem.OpenFile("Hardcore.dat", FileTypes.User, FileModes.ReadOnly, true);
			}
			BinaryReader binaryReader = new BinaryReader(stream);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				dictionary.Add(binaryReader.ReadString(), binaryReader.ReadString());
			}
			binaryReader.Close();
			if (!dictionary.ContainsKey(CurrentGameID))
			{
				dictionary.Add(CurrentGameID, CurrentSaveID);
			}
			else
			{
				dictionary[CurrentGameID] = CurrentSaveID;
			}
			stream = this.Game.Utilities.FileSystem.OpenFile("Hardcore.dat", FileTypes.User, FileModes.ReadWrite, true);
			BinaryWriter binaryWriter2 = new BinaryWriter(stream);
			binaryWriter2.Write(dictionary.Count);
			foreach (KeyValuePair<string, string> keyValuePair in dictionary)
			{
				binaryWriter2.Write(keyValuePair.Key);
				binaryWriter2.Write(keyValuePair.Value);
			}
			binaryWriter2.Close();
		}

		public int CompareSaveData(SaveData A, SaveData B)
		{
			return B.Date.CompareTo(A.Date);
		}

		public int GetTotalSaveCount()
		{
			List<string> list = new List<string>();
			list.AddRange(this.Game.Utilities.FileSystem.GetFilenamesOfType("Saves\\Manual", "sov", FileTypes.User));
			list.AddRange(this.Game.Utilities.FileSystem.GetFilenamesOfType("Saves\\Auto", "sov", FileTypes.User));
			list.AddRange(this.Game.Utilities.FileSystem.GetFilenamesOfType("Saves\\Ironman", "sov", FileTypes.User));
			return list.Count<string>();
		}

		private const int SAVE_MIN_VERSION = 1;

		private const int SAVE_MAGIC_NUMBER = 55734563;

		public Sovereignty Game;
	}
}
