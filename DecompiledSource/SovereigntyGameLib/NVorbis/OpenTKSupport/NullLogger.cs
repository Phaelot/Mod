using System;

namespace NVorbis.OpenTKSupport
{
	public class NullLogger : ILogger
	{
		public void Log(LogEventBoolean eventType, Func<bool> context)
		{
		}

		public void Log(LogEventBoolean eventType, bool context)
		{
		}

		public void Log(LogEventSingle eventType, Func<float> context)
		{
		}

		public void Log(LogEventSingle eventType, float context)
		{
		}

		public void Log(LogEvent eventType, OggStream stream)
		{
		}

		public static readonly ILogger Default = new NullLogger();
	}
}
