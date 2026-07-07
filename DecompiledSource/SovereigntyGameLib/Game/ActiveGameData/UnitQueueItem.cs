using System;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class UnitQueueItem
	{
		public WorkingUnit Unit
		{
			get
			{
				WorkingUnit workingUnit = null;
				this.Game.AllUnits.TryGetValue(this.UnitID, out workingUnit);
				return workingUnit;
			}
		}

		public UnitQueueItem(SovereigntyGame Game)
		{
			this.Game = Game;
		}

		public void LoadState(BinaryReader r, int SaveVersion)
		{
			this.UnitID = r.ReadInt32();
			this.TurnsLeft = r.ReadInt32();
			this.UnitCost = r.ReadInt32();
		}

		public void SaveCurrentState(BinaryWriter w)
		{
			w.Write(this.UnitID);
			w.Write(this.TurnsLeft);
			w.Write(this.UnitCost);
		}

		public int GetRefundValue()
		{
			float num = 1f;
			if (this.TotalTurns > 0)
			{
				num = (float)this.TurnsLeft / (float)this.TotalTurns;
			}
			float num2 = (float)this.UnitCost * num;
			return (int)num2;
		}

		public int UnitID;

		public int TurnsLeft;

		public int UnitCost;

		public int TotalTurns;

		private SovereigntyGame Game;
	}
}
