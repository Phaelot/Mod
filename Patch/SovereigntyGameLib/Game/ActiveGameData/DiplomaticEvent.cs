using System;
using System.IO;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class DiplomaticEvent
	{
		public DiplomaticEvent()
		{
		}

		public DiplomaticEvent(DiplomaticEventData Data)
		{
			this.Data = Data;
			this.CurrentValue = Data.DispositionEffect;
		}

		internal void Update()
		{
			this.CurrentValue += this.Data.DecayRate;
			if (this.Data.DispositionEffect > 0f && this.CurrentValue < 0f)
			{
				this.CurrentValue = 0f;
			}
			if (this.Data.DispositionEffect < 0f && this.CurrentValue > 0f)
			{
				this.CurrentValue = 0f;
			}
		}

		internal bool IsExpired()
		{
			return this.CurrentValue == 0f && this.Data.Expires;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.CurrentValue);
			this.Data.Save(w);
		}

		internal void Load(BinaryReader r)
		{
			this.CurrentValue = r.ReadSingle();
			this.Data = new DiplomaticEventData();
			this.Data.Load(r);
		}

		public DiplomaticEventData Data;

		public float CurrentValue;
	}
}
