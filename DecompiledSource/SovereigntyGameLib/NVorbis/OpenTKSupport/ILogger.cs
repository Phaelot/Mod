using System;

namespace NVorbis.OpenTKSupport
{
	public interface ILogger
	{
		void Log(LogEventBoolean eventType, Func<bool> context);

		void Log(LogEventBoolean eventType, bool context);

		void Log(LogEventSingle eventType, Func<float> context);

		void Log(LogEventSingle eventType, float context);

		void Log(LogEvent eventType, OggStream stream);
	}
}
