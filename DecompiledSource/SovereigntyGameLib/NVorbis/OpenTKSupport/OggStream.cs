// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// NVorbis.OpenTKSupport.OggStream
using System;
using System.IO;
using NVorbis;
using NVorbis.OpenTKSupport;
using OpenTK.Audio.OpenAL;

namespace NVorbis.OpenTKSupport
{
	public class OggStream : IDisposable
	{
		private const int DefaultBufferCount = 3;

		internal readonly object stopMutex = new object();

		internal readonly object prepareMutex = new object();

		internal readonly int alSourceId;

		internal readonly int[] alBufferIds;

		private readonly int alFilterId;

		private readonly Stream underlyingStream;

		private float lowPassHfGain;

		private float volume;

		internal VorbisReader Reader { get; private set; }

		public bool Ready { get; private set; }

		internal bool Preparing { get; private set; }

		public int BufferCount { get; private set; }

		public ILogger Logger { private get; set; }

		public float LowPassHFGain
		{
			get
			{
				return lowPassHfGain;
			}
			set
			{
				if (ALHelper.Efx.IsInitialized)
				{
					ALHelper.Efx.Filter(alFilterId, EfxFilterf.LowpassGainHF, lowPassHfGain = value);
					ALHelper.Efx.BindFilterToSource(alSourceId, alFilterId);
					ALHelper.Check();
				}
			}
		}

		public float Volume
		{
			get
			{
				return volume;
			}
			set
			{
				AL.Source(alSourceId, ALSourcef.Gain, volume = value);
				ALHelper.Check();
			}
		}

		public bool IsLooped { get; set; }

		public event Action OnFinished;

		public OggStream(string filename, int bufferCount = 3)
			: this(File.OpenRead(filename), bufferCount)
		{
		}

		public OggStream(Stream stream, int bufferCount = 3)
		{
			BufferCount = bufferCount;
			alBufferIds = AL.GenBuffers(bufferCount);
			alSourceId = AL.GenSource();
			if (ALHelper.XRam.IsInitialized)
			{
				ALHelper.XRam.SetBufferMode(BufferCount, ref alBufferIds[0], XRamExtension.XRamStorage.Hardware);
				ALHelper.Check();
			}
			Volume = 1f;
			if (ALHelper.Efx.IsInitialized)
			{
				alFilterId = ALHelper.Efx.GenFilter();
				ALHelper.Efx.Filter(alFilterId, EfxFilteri.FilterType, 1);
				ALHelper.Efx.Filter(alFilterId, EfxFilterf.LowpassGain, 1f);
				LowPassHFGain = 1f;
			}
			underlyingStream = stream;
			Logger = NullLogger.Default;
		}

		public void Prepare()
		{
			if (Preparing)
			{
				return;
			}
			ALSourceState sourceState = AL.GetSourceState(alSourceId);
			lock (stopMutex)
			{
				switch (sourceState)
				{
					case ALSourceState.Playing:
					case ALSourceState.Paused:
						return;
					case ALSourceState.Stopped:
						lock (prepareMutex)
						{
							Reader.DecodedTime = TimeSpan.Zero;
							Ready = false;
							Empty();
						}
						break;
				}
				if (!Ready)
				{
					lock (prepareMutex)
					{
						Preparing = true;
						Logger.Log(LogEvent.BeginPrepare, this);
						Open(precache: true);
						Logger.Log(LogEvent.EndPrepare, this);
						return;
					}
				}
			}
		}

		public void Play()
		{
			switch (AL.GetSourceState(alSourceId))
			{
				case ALSourceState.Playing:
					return;
				case ALSourceState.Paused:
					Resume();
					return;
			}
			Prepare();
			Logger.Log(LogEvent.Play, this);
			AL.SourcePlay(alSourceId);
			ALHelper.Check();
			Preparing = false;
			OggStreamer.Instance.AddStream(this);
		}

		public void Pause()
		{
			if (AL.GetSourceState(alSourceId) == ALSourceState.Playing)
			{
				OggStreamer.Instance.RemoveStream(this);
				Logger.Log(LogEvent.Pause, this);
				AL.SourcePause(alSourceId);
				ALHelper.Check();
			}
		}

		public void Resume()
		{
			if (AL.GetSourceState(alSourceId) == ALSourceState.Paused)
			{
				OggStreamer.Instance.AddStream(this);
				Logger.Log(LogEvent.Resume, this);
				AL.SourcePlay(alSourceId);
				ALHelper.Check();
			}
		}

		public void Stop()
		{
			ALSourceState sourceState = AL.GetSourceState(alSourceId);
			if (sourceState == ALSourceState.Playing || sourceState == ALSourceState.Paused)
			{
				Logger.Log(LogEvent.Stop, this);
				StopPlayback();
			}
			lock (stopMutex)
			{
				NotifyFinished();
				OggStreamer.Instance.RemoveStream(this);
			}
		}

		public void Dispose()
		{
			ALSourceState sourceState = AL.GetSourceState(alSourceId);
			if (sourceState == ALSourceState.Playing || sourceState == ALSourceState.Paused)
			{
				StopPlayback();
			}
			lock (prepareMutex)
			{
				OggStreamer.Instance.RemoveStream(this);
				if (sourceState != ALSourceState.Initial)
				{
					Empty();
				}
				Close();
				underlyingStream.Dispose();
			}
			AL.DeleteSource(alSourceId);
			AL.DeleteBuffers(alBufferIds);
			if (ALHelper.Efx.IsInitialized)
			{
				ALHelper.Efx.DeleteFilter(alFilterId);
			}
			ALHelper.Check();
			Logger.Log(LogEventSingle.MemoryUsage, () => GC.GetTotalMemory(forceFullCollection: true));
		}

		private void StopPlayback()
		{
			AL.SourceStop(alSourceId);
			ALHelper.Check();
		}

		internal void NotifyFinished()
		{
			if (this.OnFinished != null)
			{
				this.OnFinished();
			}
		}

		private void Empty()
		{
			AL.GetSource(alSourceId, ALGetSourcei.BuffersQueued, out var value);
			if (value > 0)
			{
				try
				{
					AL.SourceUnqueueBuffers(alSourceId, value);
					ALHelper.Check();
				}
				catch (InvalidOperationException)
				{
					AL.GetSource(alSourceId, ALGetSourcei.BuffersProcessed, out var value2);
					int[] bids = new int[value2];
					if (value2 > 0)
					{
						AL.SourceUnqueueBuffers(alSourceId, value2, bids);
						ALHelper.Check();
					}
					AL.SourceStop(alSourceId);
					ALHelper.Check();
					Empty();
				}
			}
			Logger.Log(LogEvent.Empty, this);
		}

		internal void Open(bool precache = false)
		{
			underlyingStream.Seek(0L, SeekOrigin.Begin);
			Reader = new VorbisReader(underlyingStream, closeStreamOnDispose: false);
			if (precache)
			{
				OggStreamer.Instance.FillBuffer(this, alBufferIds[0]);
				AL.SourceQueueBuffer(alSourceId, alBufferIds[0]);
				ALHelper.Check();
				OggStreamer.Instance.AddStream(this);
			}
			Ready = true;
		}

		internal void Close()
		{
			if (Reader != null)
			{
				Reader.Dispose();
				Reader = null;
			}
			Ready = false;
		}
	}
}