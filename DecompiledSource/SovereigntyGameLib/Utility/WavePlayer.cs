// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.WavePlayer
using System.IO;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Utility
{
	public class WavePlayer
	{
		private int SourceID;

		public int BufferID;

		public bool Finished => AL.GetSourceState(SourceID) == ALSourceState.Stopped;

		public WavePlayer(Stream WaveStream, float Volume, AudioContext AC)
		{
			AC.MakeCurrent();
			BufferID = AL.GenBuffer();
			SourceID = AL.GenSource();
			WaveData waveData = new WaveData(WaveStream);
			AL.BufferData(BufferID, waveData.SoundFormat, waveData.SoundData, waveData.SoundData.Length, waveData.SampleRate);
			waveData.dispose();
			AL.Source(SourceID, ALSource3f.Position, 0f, 0f, 0f);
			AL.Source(SourceID, ALSource3f.Velocity, 0f, 0f, 0f);
			AL.Source(SourceID, ALSource3f.Direction, 0f, 0f, 0f);
			AL.Source(SourceID, ALSourcef.RolloffFactor, 0f);
			AL.Source(SourceID, ALSourceb.SourceRelative, value: true);
			AL.Source(SourceID, ALSourcef.Gain, Volume);
			AL.Source(SourceID, ALSourcei.Buffer, BufferID);
			AL.SourcePlay(SourceID);
		}

		public void Dispose(AudioContext AC)
		{
			AC.MakeCurrent();
			AL.SourceStop(SourceID);
			AL.DeleteSource(SourceID);
			AL.DeleteBuffer(BufferID);
		}

		internal void SetGain(float Value, AudioContext AC)
		{
			AC.MakeCurrent();
			AL.Source(SourceID, ALSourcef.Gain, Value);
		}
	}
}