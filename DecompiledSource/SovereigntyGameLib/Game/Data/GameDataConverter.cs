using System;

namespace SovereigntyTK.Game.Data
{
	public abstract class GameDataConverter
	{
		public abstract object Convert(string Data);

		public abstract string ConvertToString(object Data);
	}
}
