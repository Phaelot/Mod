using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NVorbis.OpenTKSupport;
using OpenTK.Audio;

namespace SovereigntyTK.Utility
{
	public class SoundManager
	{
		public SoundManager(GameBase Game)
		{
			this.Game = Game;
			this.LockObject = new object();
			this.ActiveSounds = new List<WavePlayer>();
			try
			{
				if (AudioContext.AvailableDevices.Count != 0)
				{
					this.AC = new AudioContext();
					this.Streamer = new OggStreamer(this.AC, false, 44100, 10f);
					new Thread(new ThreadStart(this.Update))
					{
						Priority = ThreadPriority.Lowest
					}.Start();
				}
			}
			catch
			{
			}
		}

		private void Cleanup()
		{
			if (this.CurrentMusic != null)
			{
				try
				{
					this.CurrentMusic.Stop();
					this.CurrentMusic.Dispose();
				}
				catch
				{
				}
			}
			foreach (WavePlayer wavePlayer in this.ActiveSounds)
			{
				try
				{
					wavePlayer.Dispose(this.AC);
				}
				catch
				{
				}
			}
			try
			{
				this.Streamer.Dispose();
				this.AC.Dispose();
				this.AC = null;
			}
			catch
			{
			}
		}

		public void Dispose()
		{
			if (this.AC == null)
			{
				return;
			}
			if (this.Disposing)
			{
				return;
			}
			this.Disposing = true;
		}

		public void Update()
		{
			for (;;)
			{
				Thread.Sleep(10);
				lock (this.LockObject)
				{
					if (!this.Disposing)
					{
						if (this.CurrentMusic != null)
						{
							this.Streamer.EnsureBuffersFilled();
							if (this.VolumeUpdateNeeded)
							{
								this.CurrentMusic.Volume = this.MusicVolume;
							}
							if (this.MusicFinished && this.CurrentMusic != null)
							{
								this.EndMusic();
								if (this.OnMusicFinished != null)
								{
									this.OnMusicFinished();
								}
							}
						}
						foreach (WavePlayer wavePlayer in this.ActiveSounds.ToList<WavePlayer>())
						{
							if (this.VolumeUpdateNeeded)
							{
								wavePlayer.SetGain(this.SoundVolume, this.AC);
							}
							if (wavePlayer.Finished)
							{
								this.ActiveSounds.Remove(wavePlayer);
								wavePlayer.Dispose(this.AC);
							}
						}
						this.VolumeUpdateNeeded = false;
						continue;
					}
					this.Cleanup();
				}
				break;
			}
		}

		public void PlaySound(string Filename)
		{
			if (this.AC == null)
			{
				return;
			}
			lock (this.LockObject)
			{
				Stream stream = this.Game.Utilities.FileSystem.OpenFile(Filename, FileTypes.Application, FileModes.ReadOnly, true);
				if (stream != null)
				{
					WavePlayer wavePlayer = new WavePlayer(stream, this.SoundVolume, this.AC);
					this.ActiveSounds.Add(wavePlayer);
					stream.Close();
				}
			}
		}

		public void EndMusic()
		{
			if (this.AC == null)
			{
				return;
			}
			lock (this.LockObject)
			{
				if (this.CurrentMusic != null)
				{
					this.CurrentMusic.Dispose();
					this.CurrentMusic = null;
				}
			}
		}

		public void PlayMusic(string Filename)
		{
			if (this.AC == null)
			{
				return;
			}
			lock (this.LockObject)
			{
				this.EndMusic();
				this.MusicFinished = false;
				this.CurrentMusic = new OggStream(this.Game.Utilities.FileSystem.OpenFile(Filename, FileTypes.Application, FileModes.ReadOnly, true), 3);
				this.CurrentMusic.Volume = this.MusicVolume;
				this.CurrentMusic.OnFinished += this.CurrentMusic_OnFinished;
				this.CurrentMusic.Prepare();
				this.CurrentMusic.Play();
			}
		}

		private void CurrentMusic_OnFinished()
		{
			this.MusicFinished = true;
		}

		public bool VolumeUpdateNeeded;

		public float SoundVolume;

		public float MusicVolume;

		public GameBase Game;

		private List<WavePlayer> ActiveSounds;

		private AudioContext AC;

		private bool Disposing;

		private object LockObject;

		public Action OnMusicFinished;

		private OggStream CurrentMusic;

		private OggStreamer Streamer;

		private bool MusicFinished;
	}
}
