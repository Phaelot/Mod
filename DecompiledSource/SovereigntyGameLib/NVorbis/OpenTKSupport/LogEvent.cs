using System;

namespace NVorbis.OpenTKSupport
{
	public enum LogEvent
	{
		BeginPrepare,
		EndPrepare,
		Play,
		Stop,
		Pause,
		Resume,
		Empty,
		NewPacket,
		LastPacket,
		BufferUnderrun
	}
}
