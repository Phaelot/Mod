// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// NVorbis.OpenTKSupport.ALHelper
using System;
using NVorbis.OpenTKSupport;
using OpenTK.Audio.OpenAL;

namespace NVorbis.OpenTKSupport
{
	public static class ALHelper
	{
		internal static readonly XRamExtension XRam = new XRamExtension();

		internal static readonly EffectsExtension Efx = new EffectsExtension();

		public static void CheckCapabilities(ILogger logger)
		{
			logger.Log(LogEventBoolean.IsOpenAlSoft, AL.Get(ALGetString.Version).Contains("SOFT"));
			logger.Log(LogEventBoolean.XRamSupport, XRam.IsInitialized);
			logger.Log(LogEventBoolean.EfxSupport, Efx.IsInitialized);
		}

		internal static void Check()
		{
			ALError error;
			if ((error = AL.GetError()) != ALError.NoError)
			{
				throw new InvalidOperationException(AL.GetErrorString(error));
			}
		}
	}
}