// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// NVorbis.OpenTKSupport.OggStreamer
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NVorbis.OpenTKSupport;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace NVorbis.OpenTKSupport
{
	public class OggStreamer : IDisposable
	{
		private const float DefaultUpdateRate = 10f;

		private const int DefaultBufferSize = 44100;

		private static readonly object singletonMutex = new object();

		private readonly object iterationMutex = new object();

		private readonly object readMutex = new object();

		private readonly float[] readSampleBuffer;

		private readonly short[] castBuffer;

		private readonly HashSet<OggStream> streams = new HashSet<OggStream>();

		private readonly List<OggStream> threadLocalStreams = new List<OggStream>();

		private Thread underlyingThread;

		private volatile bool cancelled;

		private AudioContext AC;

		private static OggStreamer instance;

		public float UpdateRate { get; private set; }

		public int BufferSize { get; private set; }

		public ILogger Logger { private get; set; }

		public static OggStreamer Instance
		{
			get
			{
				lock (singletonMutex)
				{
					if (instance == null)
					{
						throw new InvalidOperationException("No instance running");
					}
					return instance;
				}
			}
			private set
			{
				lock (singletonMutex)
				{
					instance = value;
				}
			}
		}

		public OggStreamer(AudioContext AC, bool internalThread = true, int bufferSize = 44100, float updateRate = 10f)
		{
			lock (singletonMutex)
			{
				if (instance != null)
				{
					throw new InvalidOperationException("Already running");
				}
				this.AC = AC;
				Instance = this;
				if (internalThread)
				{
					underlyingThread = new Thread(EnsureBuffersFilled)
					{
						Priority = ThreadPriority.Lowest
					};
					underlyingThread.Start();
				}
				else
				{
					updateRate = 0f;
				}
			}
			UpdateRate = updateRate;
			BufferSize = bufferSize;
			readSampleBuffer = new float[bufferSize];
			castBuffer = new short[bufferSize];
			Logger = NullLogger.Default;
		}

		public void Dispose()
		{
			lock (singletonMutex)
			{
				cancelled = true;
				lock (iterationMutex)
				{
					streams.Clear();
				}
				Instance = null;
				underlyingThread = null;
			}
		}

		internal bool AddStream(OggStream stream)
		{
			lock (iterationMutex)
			{
				return streams.Add(stream);
			}
		}

		internal bool RemoveStream(OggStream stream)
		{
			lock (iterationMutex)
			{
				return streams.Remove(stream);
			}
		}

		public bool FillBuffer(OggStream stream, int bufferId)
		{
			int num;
			lock (readMutex)
			{
				num = stream.Reader.ReadSamples(readSampleBuffer, 0, BufferSize);
				CastBuffer(readSampleBuffer, castBuffer, num);
			}
			AL.BufferData(bufferId, (stream.Reader.Channels == 1) ? ALFormat.Mono16 : ALFormat.Stereo16, castBuffer, num * 2, stream.Reader.SampleRate);
			ALHelper.Check();
			if (num == BufferSize)
			{
				Logger.Log(LogEvent.NewPacket, stream);
			}
			else
			{
				Logger.Log(LogEvent.LastPacket, stream);
			}
			Logger.Log(LogEventSingle.MemoryUsage, () => GC.GetTotalMemory(forceFullCollection: true));
			return num != BufferSize;
		}

		public static void CastBuffer(float[] inBuffer, short[] outBuffer, int length)
		{
			for (int i = 0; i < length; i++)
			{
				int num = (int)(32767f * inBuffer[i]);
				if (num > 32767)
				{
					num = 32767;
				}
				else if (num < -32768)
				{
					num = -32768;
				}
				outBuffer[i] = (short)num;
			}
		}

		public void EnsureBuffersFilled()
		{
			do
			{
				AC.MakeCurrent();
				threadLocalStreams.Clear();
				lock (iterationMutex)
				{
					threadLocalStreams.AddRange(streams);
				}
				foreach (OggStream threadLocalStream in threadLocalStreams)
				{
					lock (threadLocalStream.prepareMutex)
					{
						lock (iterationMutex)
						{
							if (!streams.Contains(threadLocalStream))
							{
								continue;
							}
						}
						bool flag = false;
						AL.GetSource(threadLocalStream.alSourceId, ALGetSourcei.BuffersQueued, out var value);
						ALHelper.Check();
						AL.GetSource(threadLocalStream.alSourceId, ALGetSourcei.BuffersProcessed, out var value2);
						ALHelper.Check();
						if (value2 == 0 && value == threadLocalStream.BufferCount)
						{
							continue;
						}
						int[] array = ((value2 <= 0) ? threadLocalStream.alBufferIds.Skip(value).ToArray() : AL.SourceUnqueueBuffers(threadLocalStream.alSourceId, value2));
						int i;
						for (i = 0; i < array.Length; i++)
						{
							flag |= FillBuffer(threadLocalStream, array[i]);
							if (!flag)
							{
								continue;
							}
							if (threadLocalStream.IsLooped)
							{
								threadLocalStream.Reader.DecodedTime = TimeSpan.Zero;
								if (i != 0)
								{
								}
								continue;
							}
							lock (threadLocalStream.stopMutex)
							{
								threadLocalStream.NotifyFinished();
							}
							streams.Remove(threadLocalStream);
							break;
						}
						AL.SourceQueueBuffers(threadLocalStream.alSourceId, i, array);
						ALHelper.Check();
						if (flag && !threadLocalStream.IsLooped)
						{
							continue;
						}
						goto IL_01bc;
					}
				IL_01bc:
					lock (threadLocalStream.stopMutex)
					{
						if (threadLocalStream.Preparing)
						{
							continue;
						}
						lock (iterationMutex)
						{
							if (!streams.Contains(threadLocalStream))
							{
								continue;
							}
						}
						ALSourceState sourceState = AL.GetSourceState(threadLocalStream.alSourceId);
						if (sourceState == ALSourceState.Stopped)
						{
							Logger.Log(LogEvent.BufferUnderrun, threadLocalStream);
							AL.SourcePlay(threadLocalStream.alSourceId);
							ALHelper.Check();
						}
					}
				}
				if (UpdateRate > 0f)
				{
					Thread.Sleep((int)(1000f / UpdateRate));
				}
			}
			while (underlyingThread != null && !cancelled);
		}
	}
}