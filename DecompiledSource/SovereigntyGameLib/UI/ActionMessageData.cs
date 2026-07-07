using System;
using System.Drawing;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI
{
	public class ActionMessageData
	{
		public ActionMessageData(GameText TitleText, GameText MessageText, bool Essential, string EventName, Point CameraTarget, params object[] EventArgs)
		{
			this.TitleText = TitleText;
			this.MessageText = MessageText;
			this.Eventname = EventName;
			this.EventArgs = EventArgs;
			this.Essential = Essential;
			this.CameraCoords = CameraTarget;
		}

		public GameText TitleText;

		public GameText MessageText;

		public string Eventname;

		public object[] EventArgs;

		public bool Essential;

		public string ActionType;

		public Point CameraCoords;
	}
}
