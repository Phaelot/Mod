using System;
using System.IO;

namespace SovereigntyTK.Utility
{
	public class LogManager
	{
		public LogManager(GameBase Game)
		{
			this.Game = Game;
			this.LogFile = new StreamWriter(Game.Utilities.FileSystem.OpenFile("log.txt", FileTypes.User, FileModes.ReadWrite, true));
		}

		public void Write(string Data)
		{
			this.LogFile.WriteLine(Data);
			this.LogFile.Flush();
		}

		public void Dispose()
		{
			this.LogFile.Flush();
			this.LogFile.Close();
		}

		private GameBase Game;

		private StreamWriter LogFile;
	}
}
