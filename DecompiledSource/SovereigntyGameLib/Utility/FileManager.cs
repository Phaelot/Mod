using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SovereigntyTK.Utility
{
	public class FileManager
	{
		public FileManager(string UserFolderName)
		{
			this.BaseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			this.UserFolderName = UserFolderName;
			this.FileMappings = new Dictionary<string, string>();
			this.ProcessMappings(this.BaseDirectory, this.BaseDirectory);
		}

		public void ProcessMappings(string CurrentDirectory, string BaseDirectory)
		{
			if (!Directory.Exists(CurrentDirectory))
			{
				return;
			}
			string[] files = Directory.GetFiles(CurrentDirectory);
			string[] directories = Directory.GetDirectories(CurrentDirectory);
			foreach (string text in files)
			{
				string text2 = text.Substring(BaseDirectory.Length + 1).ToLowerInvariant();
				if (this.FileMappings.ContainsKey(text2))
				{
					this.FileMappings[text2] = text;
				}
				else
				{
					this.FileMappings.Add(text2, text.ToLowerInvariant());
				}
			}
			foreach (string text3 in directories)
			{
				string text4 = text3.Substring(BaseDirectory.Length + 1).ToLowerInvariant();
				if (this.FileMappings.ContainsKey(text4))
				{
					this.FileMappings[text4] = text3;
				}
				else
				{
					this.FileMappings.Add(text4, text3.ToLowerInvariant());
				}
				this.ProcessMappings(text3, BaseDirectory);
			}
		}

		public string ConvertFilenameForMod(string Filename, string ModID)
		{
			Filename = Filename.Replace('\\', Path.DirectorySeparatorChar);
			Filename = Filename.ToLowerInvariant();
			return string.Concat(new object[]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.Personal),
				Path.DirectorySeparatorChar,
				this.UserFolderName,
				Path.DirectorySeparatorChar,
				"mods",
				Path.DirectorySeparatorChar,
				ModID,
				Path.DirectorySeparatorChar,
				Filename
			});
		}

		public string ConvertFilename(string Filename, FileTypes Type, bool Relative = true)
		{
			Filename = Filename.Replace('\\', Path.DirectorySeparatorChar).TrimStart(new char[] { Path.DirectorySeparatorChar });
			if (Relative)
			{
				Filename = Filename.ToLowerInvariant();
			}
			switch (Type)
			{
			case FileTypes.Application:
				if (Relative)
				{
					string text = "";
					this.FileMappings.TryGetValue(Filename, out text);
					return text;
				}
				return Filename;
			case FileTypes.User:
				if (Relative)
				{
					return string.Concat(new object[]
					{
						Environment.GetFolderPath(Environment.SpecialFolder.Personal),
						Path.DirectorySeparatorChar,
						this.UserFolderName,
						Path.DirectorySeparatorChar,
						Filename
					});
				}
				return Filename;
			default:
				return Filename;
			}
		}

		public string ConvertFoldername(string Filename, FileTypes Type, bool Relative = true)
		{
			Filename = Filename.Replace('\\', Path.DirectorySeparatorChar).TrimStart(new char[] { Path.DirectorySeparatorChar });
			if (Relative)
			{
				Filename = Filename.ToLowerInvariant();
			}
			switch (Type)
			{
			case FileTypes.Application:
				if (Relative)
				{
					return this.BaseDirectory + Path.DirectorySeparatorChar + Filename;
				}
				return Filename;
			case FileTypes.User:
				if (Relative)
				{
					return string.Concat(new object[]
					{
						Environment.GetFolderPath(Environment.SpecialFolder.Personal),
						Path.DirectorySeparatorChar,
						this.UserFolderName,
						Path.DirectorySeparatorChar,
						Filename
					});
				}
				return Filename;
			default:
				return Filename;
			}
		}

		public bool FileExists(string Filename, FileTypes Type, bool Relative = true)
		{
			string text = this.ConvertFilename(Filename, Type, Relative);
			return File.Exists(text);
		}

		public Stream OpenEditorFile(string Filename, FileTypes Type, List<string> ModIDs, FileModes Mode = FileModes.ReadOnly, bool Relative = true)
		{
			string text = null;
			for (int i = ModIDs.Count - 1; i >= 0; i--)
			{
				string text2 = this.ConvertFilenameForMod(Filename, ModIDs[i]);
				if (File.Exists(text2))
				{
					text = text2;
					break;
				}
			}
			if (text == null)
			{
				text = this.ConvertFilename(Filename, Type, Relative);
			}
			if (text == "")
			{
				return null;
			}
			if (Mode == FileModes.ReadWrite)
			{
				string directoryName = Path.GetDirectoryName(text);
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
			}
			if (!File.Exists(text) && Mode == FileModes.ReadOnly)
			{
				return null;
			}
			switch (Mode)
			{
			case FileModes.ReadOnly:
				return File.OpenRead(text);
			case FileModes.ReadWrite:
				return File.Create(text);
			default:
				return null;
			}
		}

		public Stream OpenFile(string Filename, FileTypes Type, FileModes Mode = FileModes.ReadOnly, bool Relative = true)
		{
			string text = this.ConvertFilename(Filename, Type, Relative);
			if (text == "")
			{
				return null;
			}
			if (Mode == FileModes.ReadWrite)
			{
				string directoryName = Path.GetDirectoryName(text);
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
			}
			if (!File.Exists(text) && Mode == FileModes.ReadOnly)
			{
				return null;
			}
			switch (Mode)
			{
			case FileModes.ReadOnly:
				return File.OpenRead(text);
			case FileModes.ReadWrite:
				return File.Create(text);
			default:
				return null;
			}
		}

		public void Dispose()
		{
		}

		public void CreateUserDirectory(string DirectoryName)
		{
			string text = string.Concat(new object[]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.Personal),
				Path.DirectorySeparatorChar,
				this.UserFolderName,
				Path.DirectorySeparatorChar,
				DirectoryName
			});
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
		}

		public void CreateSaveDirectory()
		{
			string userDirectory = this.GetUserDirectory();
			if (!Directory.Exists(userDirectory + "\\Saves\\Auto"))
			{
				Directory.CreateDirectory(userDirectory + "\\Saves\\Auto");
			}
			if (!Directory.Exists(userDirectory + "\\Saves\\Manual"))
			{
				Directory.CreateDirectory(userDirectory + "\\Saves\\Manual");
			}
		}

		public string[] GetFilenamesOfType(string FolderName, string Mask, FileTypes Type)
		{
			string text = this.ConvertFoldername(FolderName, Type, true);
			if (!Directory.Exists(text))
			{
				return new string[0];
			}
			return Directory.GetFiles(text, "*." + Mask, SearchOption.AllDirectories);
		}

		public string[] GetFolderNames(string FolderName, List<ModData> Mods)
		{
			string text = this.ConvertFoldername(FolderName, FileTypes.Application, true);
			List<string> list = new List<string>();
			if (Directory.Exists(text))
			{
				foreach (string text2 in Directory.GetDirectories(text))
				{
					list.Add(text2);
				}
			}
			foreach (ModData modData in Mods)
			{
				string text3 = "Mods" + Path.DirectorySeparatorChar + modData.ID;
				text = text3 + Path.DirectorySeparatorChar + FolderName;
				text = this.ConvertFoldername(text, FileTypes.User, true);
				if (Directory.Exists(text))
				{
					foreach (string text4 in Directory.GetDirectories(text))
					{
						list.Add(text4);
					}
				}
			}
			return list.ToArray();
		}

		public string[] GetFilenamesOfType(string FolderName, string Mask, FileTypes Type, List<ModData> Mods)
		{
			string text = this.ConvertFoldername(FolderName, Type, true);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (Directory.Exists(text))
			{
				foreach (string text2 in Directory.GetFiles(text, "*." + Mask, SearchOption.AllDirectories))
				{
					string text3 = text2.Substring(this.BaseDirectory.Length + 1);
					dictionary.Add(text3.ToLowerInvariant(), text2.ToLowerInvariant());
				}
			}
			foreach (ModData modData in Mods)
			{
				string text4 = "Mods" + Path.DirectorySeparatorChar + modData.ID;
				text = this.ConvertFoldername(text4, FileTypes.User, true);
				string text5 = text;
				text = text + Path.DirectorySeparatorChar + FolderName;
				if (Directory.Exists(text))
				{
					foreach (string text6 in Directory.GetFiles(text, "*." + Mask, SearchOption.AllDirectories))
					{
						string text7 = text6.Substring(text5.Length + 1).ToLowerInvariant();
						if (dictionary.ContainsKey(text7))
						{
							dictionary[text7] = text6.ToLowerInvariant();
						}
						else
						{
							dictionary.Add(text7, text6.ToLowerInvariant());
						}
					}
				}
			}
			return dictionary.Values.ToArray<string>();
		}

		public string GetUserDirectory()
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar + this.UserFolderName;
		}

		internal void DeleteUserDirectory(string Foldername)
		{
			Foldername = this.GetUserDirectory() + Path.DirectorySeparatorChar + Foldername;
			if (Directory.Exists(Foldername))
			{
				Directory.Delete(Foldername);
			}
		}

		public Stream CreateFile(string FileName, FileTypes Type)
		{
			string text = this.ConvertFilename(FileName, Type, true);
			string directoryName = Path.GetDirectoryName(text);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			return File.Create(text);
		}

		public bool DeleteFile(string Filename, FileTypes Type, bool Relative)
		{
			string text = this.ConvertFilename(Filename, Type, Relative);
			if (File.Exists(text))
			{
				File.Delete(text);
				return true;
			}
			return false;
		}

		public string BaseDirectory;

		public string UserFolderName;

		private Dictionary<string, string> FileMappings;
	}
}
