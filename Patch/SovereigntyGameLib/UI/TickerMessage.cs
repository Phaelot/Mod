using System;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.UI
{
	public struct TickerMessage
	{
		public TickerMessage(GameText Text, TickerMessageType Type, int Priority)
		{
			this.MessagePriority = Priority;
			this.MessageText = Text;
			this.MessageType = Type;
		}

		public GameText MessageText;

		public TickerMessageType MessageType;

		public int MessagePriority;
	}
}
