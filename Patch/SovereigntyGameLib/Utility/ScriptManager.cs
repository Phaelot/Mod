using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;

namespace SovereigntyTK.Utility
{
	public class ScriptManager
	{
		public ScriptManager(GameBase Game, List<ModData> ActiveMods)
		{
			this.Game = Game;
			Game.Utilities.FileSystem.CreateUserDirectory("Compiled Scripts");
			this.UIAssembly = this.Compile("UI", Game.Utilities.FileSystem.GetFilenamesOfType("Data\\UI", "cs", FileTypes.Application, ActiveMods));
			this.FlagAssembly = this.Compile("Flags", Game.Utilities.FileSystem.GetFilenamesOfType("Data\\Scripts\\Unitflags", "cs", FileTypes.Application, ActiveMods));
			this.SpellAssembly = this.Compile("Spells", Game.Utilities.FileSystem.GetFilenamesOfType("Data\\Scripts\\Spells", "cs", FileTypes.Application, ActiveMods));
			this.CardAssembly = this.Compile("Cards", Game.Utilities.FileSystem.GetFilenamesOfType("Data\\Scripts\\Cards", "cs", FileTypes.Application, ActiveMods));
			this.CampaignAssembly = this.Compile("Campaigns", Game.Utilities.FileSystem.GetFilenamesOfType("Data\\Scripts\\Campaigns", "cs", FileTypes.Application, ActiveMods));
			this.BuildingAssembly = this.Compile("Buildings", Game.Utilities.FileSystem.GetFilenamesOfType("Data\\Scripts\\Buildings", "cs", FileTypes.Application, ActiveMods));
			this.AIAssembly = this.Compile("AI", Game.Utilities.FileSystem.GetFilenamesOfType("Data\\Scripts\\AI", "cs", FileTypes.Application, ActiveMods));
		}

		private Assembly Compile(string LibraryName, string[] Files)
		{
			if (Files.Length == 0)
			{
				return null;
			}
			CodeDomProvider codeDomProvider = new CSharpCodeProvider();
			CompilerParameters compilerParameters = new CompilerParameters();
			compilerParameters.GenerateExecutable = false;
			compilerParameters.GenerateInMemory = false;
			compilerParameters.ReferencedAssemblies.Add("System.dll");
			compilerParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
			compilerParameters.ReferencedAssemblies.Add("System.Core.dll");
			compilerParameters.ReferencedAssemblies.Add("System.Drawing.dll");
			compilerParameters.ReferencedAssemblies.Add("System.Xml.Linq.dll");
			compilerParameters.ReferencedAssemblies.Add("System.Xml.dll");
			compilerParameters.ReferencedAssemblies.Add("Sovereignty.exe");
			compilerParameters.ReferencedAssemblies.Add("SovereigntyGameLib.dll");
			compilerParameters.ReferencedAssemblies.Add("OpenTK.dll");
			compilerParameters.OutputAssembly = string.Concat(new object[]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				Path.DirectorySeparatorChar,
				"Sovereignty",
				Path.DirectorySeparatorChar,
				LibraryName,
				".dll"
			});
			string directoryName = Path.GetDirectoryName(compilerParameters.OutputAssembly);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			int num = 1;
			while (File.Exists(compilerParameters.OutputAssembly))
			{
				try
				{
					File.Delete(compilerParameters.OutputAssembly);
				}
				catch
				{
					compilerParameters.OutputAssembly = string.Concat(new object[]
					{
						Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
						Path.DirectorySeparatorChar,
						"Sovereignty",
						Path.DirectorySeparatorChar,
						LibraryName,
						"_",
						num++,
						".dll"
					});
				}
			}
			CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromFile(compilerParameters, Files);
			if (compilerResults.Errors.Count > 0)
			{
				foreach (object obj in compilerResults.Errors)
				{
					CompilerError compilerError = (CompilerError)obj;
					if (!compilerError.IsWarning)
					{
						throw new Exception("Error compiling scripts:\n\n" + compilerError);
					}
				}
			}
			return compilerResults.CompiledAssembly;
		}

		public Assembly UIAssembly;

		public Assembly FlagAssembly;

		public Assembly SpellAssembly;

		public Assembly CardAssembly;

		public Assembly CampaignAssembly;

		public Assembly BuildingAssembly;

		public Assembly AIAssembly;

		private GameBase Game;
	}
}
