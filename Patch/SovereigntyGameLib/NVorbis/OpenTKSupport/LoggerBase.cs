using System;

namespace NVorbis.OpenTKSupport
{
	public abstract class LoggerBase : ILogger
	{
		public void Log(LogEventBoolean eventType, Func<bool> context)
		{
			this.Log(eventType, context());
		}

		public abstract void Log(LogEventBoolean eventType, bool context);

		public void Log(LogEventSingle eventType, Func<float> context)
		{
			this.Log(eventType, context());
		}

		public abstract void Log(LogEventSingle eventType, float context);

		public abstract void Log(LogEvent eventType, OggStream stream);
	}
}
