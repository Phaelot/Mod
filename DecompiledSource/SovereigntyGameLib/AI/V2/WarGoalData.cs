using System;
using System.Collections.Generic;
using System.IO;

namespace SovereigntyTK.AI.V2
{
	public class WarGoalData
	{
		public WarGoalData()
		{
			this.ProvinceTargets = new List<int>();
			this.UnitTargets = new List<int>();
		}

		public void Load(BinaryReader r, int SaveVersion)
		{
			this.GoalType = (WarGoalTypes)r.ReadInt16();
			this.NumericTarget = r.ReadSingle();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				this.ProvinceTargets.Add(r.ReadInt32());
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				this.UnitTargets.Add(r.ReadInt32());
			}
		}

		public void Save(BinaryWriter w)
		{
			w.Write((short)this.GoalType);
			w.Write(this.NumericTarget);
			w.Write(this.ProvinceTargets.Count);
			foreach (int num in this.ProvinceTargets)
			{
				w.Write(num);
			}
			w.Write(this.UnitTargets.Count);
			foreach (int num2 in this.UnitTargets)
			{
				w.Write(num2);
			}
		}

		public WarGoalTypes GoalType;

		public List<int> ProvinceTargets;

		public float NumericTarget;

		public List<int> UnitTargets;
	}
}
