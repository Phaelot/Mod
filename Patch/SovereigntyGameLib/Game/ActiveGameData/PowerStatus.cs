using System;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class PowerStatus
	{
		public virtual PowerGroup PowerGroup { get; private set; }

		public virtual int Rank { get; private set; }

		public PowerStatus()
		{
		}

		public PowerStatus(PowerGroup PowerGroup, int Rank)
		{
			this.PowerGroup = PowerGroup;
			this.Rank = Rank;
		}

		public override bool Equals(object obj)
		{
			PowerStatus powerStatus = obj as PowerStatus;
			if (powerStatus != null)
			{
				return powerStatus.PowerGroup == this.PowerGroup && powerStatus.Rank == this.Rank;
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return 7 * this.Rank * this.PowerGroup.GetHashCode();
		}
	}
}
