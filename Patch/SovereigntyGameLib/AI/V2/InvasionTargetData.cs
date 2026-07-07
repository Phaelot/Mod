using System;
using System.IO;

namespace SovereigntyTK.AI.V2
{
	public class InvasionTargetData
	{
		public InvasionTargetData(WarGoalData Data, int Turns)
		{
			this.WarGoal = Data;
			this.TurnsLeft = Turns;
		}

		public InvasionTargetData(BinaryReader r, int SaveVersion)
		{
			this.TurnsLeft = r.ReadInt32();
			this.WarGoal = new WarGoalData();
			this.WarGoal.Load(r, SaveVersion);
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.TurnsLeft);
			this.WarGoal.Save(w);
		}

		public WarGoalData WarGoal;

		public int TurnsLeft;
	}
}
