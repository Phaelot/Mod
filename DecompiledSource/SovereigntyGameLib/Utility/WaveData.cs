// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.Utility.WaveData
using System;
using System.IO;
using OpenTK.Audio.OpenAL;

namespace SovereigntyTK.Utility
{
	public class WaveData
	{
		private int channels;

		private int bits_per_sample;

		private int sample_rate;

		private byte[] sound_data;

		public byte[] SoundData => sound_data;

		public int Channels => channels;

		public int BitsPerSample => bits_per_sample;

		public int SampleRate => sample_rate;

		public ALFormat SoundFormat => getSoundFormat(channels, bits_per_sample);

		public WaveData(Stream File)
		{
			sound_data = LoadWave(File, out channels, out bits_per_sample, out sample_rate);
		}

		private byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			byte[] array;
			using (BinaryReader binaryReader = new BinaryReader(stream))
			{
				string text = new string(binaryReader.ReadChars(4));
				if (text != "RIFF")
				{
					throw new NotSupportedException("Specified stream is not a wave file.");
				}
				binaryReader.ReadInt32();
				string text2 = new string(binaryReader.ReadChars(4));
				if (text2 != "WAVE")
				{
					throw new NotSupportedException("Specified stream is not a wave file.");
				}
				string text3 = new string(binaryReader.ReadChars(4));
				if (text3.ToLowerInvariant() == "bext")
				{
					int num = binaryReader.ReadInt32();
					binaryReader.BaseStream.Seek((long)num, SeekOrigin.Current);
					text3 = new string(binaryReader.ReadChars(4));
				}
				if (text3 != "fmt ")
				{
					throw new NotSupportedException("Specified wave file is not supported.");
				}
				binaryReader.ReadInt32();
				binaryReader.ReadInt16();
				int num2 = (int)binaryReader.ReadInt16();
				int num3 = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt16();
				int num4 = (int)binaryReader.ReadInt16();
				string text4 = new string(binaryReader.ReadChars(4));
				if (text4.ToLowerInvariant() == "pad ")
				{
					int num5 = binaryReader.ReadInt32();
					binaryReader.BaseStream.Seek((long)num5, SeekOrigin.Current);
					text4 = new string(binaryReader.ReadChars(4));
				}
				if (text4 != "data")
				{
					throw new NotSupportedException("Specified wave file is not supported.");
				}
				int num6 = binaryReader.ReadInt32();
				channels = num2;
				bits = num4;
				rate = num3;
				array = binaryReader.ReadBytes(num6);
			}
			return array;
		}

		private ALFormat getSoundFormat(int channels, int bits)
		{
			switch (channels)
			{
				case 1:
					if (bits != 8)
					{
						return ALFormat.Mono16;
					}
					return ALFormat.Mono8;
				case 2:
					if (bits != 8)
					{
						return ALFormat.Stereo16;
					}
					return ALFormat.Stereo8;
				default:
					throw new NotSupportedException("The specified sound format is not supported.");
			}
		}

		public void dispose()
		{
			sound_data = null;
		}
	}
}