using System;
using System.IO;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class DiplomaticCondition
	{
		public DiplomaticCondition()
		{
		}

		public DiplomaticCondition(DiplomaticConditionData Data)
		{
			this.Data = Data;
			this.CurrentValue = Data.DispositionEffect;
			this.Enabled = true;
			this.DoNotSave = Data.DoNotSave;
		}

		internal void Update()
		{
			if (this.Enabled)
			{
				this.CurrentValue += this.Data.DispositionEffect;
			}
			else
			{
				this.CurrentValue += this.Data.DecayRate;
			}
			if (this.CurrentValue > this.Data.MaximumEffect)
			{
				this.CurrentValue = this.Data.MaximumEffect;
			}
			if (this.CurrentValue < this.Data.MinimumEffect)
			{
				this.CurrentValue = this.Data.MinimumEffect;
			}
		}

		internal bool IsExpired()
		{
			return this.CurrentValue == 0f;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.CurrentValue);
			w.Write(this.Enabled);
			this.Data.Save(w);
		}

		internal void Load(BinaryReader r)
		{
			this.CurrentValue = r.ReadSingle();
			this.Enabled = r.ReadBoolean();
			this.Data = new DiplomaticConditionData();
			this.Data.Load(r);
		}

		internal bool IsMaxed()
		{
			if (this.Data.DispositionEffect > 0f)
			{
				return this.CurrentValue >= this.Data.MaximumEffect;
			}
			return this.CurrentValue <= this.Data.MinimumEffect;
		}

		public DiplomaticConditionData Data;

		public float CurrentValue;

		public bool Enabled;

		public bool DoNotSave;
	}
}
