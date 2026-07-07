using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.AI.V2.Actions;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2
{
	public class AIRevoltManager
	{
		public AIRevoltManager(AIPlayer AI)
		{
			this.AI = AI;
		}

		internal void Dispose()
		{
		}

		internal void BeginRevoltChecks()
		{
			this.RebelProvinceID = 0;
			this.CheckForRevolts();
		}

		public void CheckForRevolts()
		{
			if (!this.RevoltsEnabled)
			{
				return;
			}
			Random random = new Random();
			while (this.RebelProvinceID < this.AI.Game.AllProvinces.Count)
			{
				WorkingProvince value = this.AI.Game.AllProvinces.ElementAt(this.RebelProvinceID).Value;
				this.RebelProvinceID++;
				if (!value.Occupied && !value.IsCapitol)
				{
					int num = value.RevoltChance;
					int num2 = 100;
					if (random.Next(num2) < num)
					{
						this.CreateRebellion(value);
					}
				}
			}
		}

		private void CreateRebellion(WorkingProvince Province)
		{
			AIACtionCreateRebelStack aiactionCreateRebelStack = this.AI.ActionManager.CreateAction<AIACtionCreateRebelStack>();
			aiactionCreateRebelStack.Province = Province;
			this.AI.ActionManager.AddAction(aiactionCreateRebelStack, true);
			foreach (WorkingUnit workingUnit in aiactionCreateRebelStack.Stack.Units)
			{
				workingUnit.Selected = true;
			}
			if (Province.LandNode.CurrentStack != null)
			{
				AIActionAttack aiactionAttack = this.AI.ActionManager.CreateAction<AIActionAttack>();
				aiactionAttack.Province = Province;
				aiactionAttack.Stack = aiactionCreateRebelStack.Stack;
				aiactionAttack.Units = aiactionCreateRebelStack.Stack.Units.ToList<WorkingUnit>();
				aiactionAttack.Node = Province.LandNode;
				aiactionAttack.Realm = Province.OwnerRealm;
				aiactionAttack.AI = this.AI;
				aiactionAttack.IgnoreInvasionCheck = true;
				this.AI.ActionManager.AddAction(aiactionAttack, true);
				return;
			}
			AIActionRebelOccupy aiactionRebelOccupy = this.AI.ActionManager.CreateAction<AIActionRebelOccupy>();
			aiactionRebelOccupy.Province = Province;
			aiactionRebelOccupy.Stack = aiactionCreateRebelStack.Stack;
			this.AI.ActionManager.AddAction(aiactionRebelOccupy, true);
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.RevoltsEnabled);
			w.Write(this.RebelProvinceID);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.RevoltsEnabled = r.ReadBoolean();
			this.RebelProvinceID = r.ReadInt32();
		}

		public AIPlayer AI;

		public int RebelProvinceID;

		public bool RevoltsEnabled;
	}
}
