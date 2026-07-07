using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SovereigntyTK.Utility
{
	public class UtilityManager
	{
		public UtilityManager(string AppName, GameBase Game)
		{
			this.Game = Game;
			this.FileSystem = new FileManager(AppName);
		}

		public void Init()
		{
			this.Game.WriteLog("Creating Text Manager");
			this.TextManager = new TextManager();
			this.Game.WriteLog("Loading text database");
			XElement xelement = XElement.Load(this.FileSystem.OpenFile("Data\\sov_text.xml", FileTypes.Application, FileModes.ReadOnly, true));
			foreach (XElement xelement2 in xelement.Elements())
			{
				this.TextManager.LoadTextData(xelement2);
			}
			this.Game.WriteLog("Creating Font Manager");
			this.FontConvertor = new FontConvertor(this.Game);
			this.Game.WriteLog("Creating Texture Manager");
			this.TextureManager = new TextureManager(this.Game, this);
			this.Game.WriteLog("Creating Shader Manager");
			this.ShaderManager = new ShaderManager(this.Game);
			this.Game.WriteLog("Creating Audio Manager");
			this.SoundManager = new SoundManager(this.Game);
			this.Game.WriteLog("Creating Main Sprite Manager");
			this.SpriteManager = new SpriteManager(this.Game);
			this.Game.WriteLog("Creating Battle Sprite Manager");
			this.BattleSpriteManager = new SpriteManager(this.Game);
			this.TooltipFactory = new TooltipFactory();
			this.Logger = new LogManager(this.Game);
		}

		public void CreateScriptManager(List<ModData> ActiveMods)
		{
			this.Game.WriteLog("Initialising scripts");
			this.ScriptManager = new ScriptManager(this.Game, ActiveMods);
		}

		public void Dispose()
		{
			this.SoundManager.Dispose();
			this.TextureManager.Dispose();
			this.ShaderManager.Dispose();
			this.SpriteManager.Dispose();
			this.BattleSpriteManager.Dispose();
		}

		public FileManager FileSystem;

		public TextureManager TextureManager;

		public ShaderManager ShaderManager;

		public ScriptManager ScriptManager;

		public FontConvertor FontConvertor;

		public TextManager TextManager;

		public SoundManager SoundManager;

		public SpriteManager SpriteManager;

		public TooltipFactory TooltipFactory;

		public SpriteManager BattleSpriteManager;

		public LogManager Logger;

		private GameBase Game;
	}
}
