using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI
{
	public class WarData
	{
		public WarData(SovereigntyGame Game, WorkingRealm Enemy, AIPlayer AI)
		{
			this.Game = Game;
			this.AI = AI;
			this.EnemyID = Enemy.ID;
			this.StartTurn = Game.TurnController.TurnNumber;
			Game.OnProvinceOwnerChanged += this.Game_OnProvinceOwnerChanged;
		}

		private void Game_OnProvinceOwnerChanged(WorkingProvince Province, WorkingRealm OldRealm, WorkingRealm Realm)
		{
			if (OldRealm.ID == this.AI.RealmID && Realm.ID == this.EnemyID)
			{
				this.ProvincesGained--;
			}
			if (OldRealm.ID == this.EnemyID && Realm.ID == this.AI.RealmID)
			{
				this.ProvincesGained++;
			}
		}

		public WarData(SovereigntyGame Game, AIPlayer AI, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.AI = AI;
			this.EnemyID = r.ReadInt32();
			this.StartTurn = r.ReadInt32();
			this.UnitsLost = r.ReadInt32();
			this.UnitsKilled = r.ReadInt32();
			this.ProvincesGained = r.ReadInt32();
		}

		public bool NoBloodSpilled()
		{
			return this.UnitsKilled + this.UnitsLost == 0 && this.ProvincesGained == 0;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.EnemyID);
			w.Write(this.StartTurn);
			w.Write(this.UnitsLost);
			w.Write(this.UnitsKilled);
			w.Write(this.ProvincesGained);
		}

		public List<Tuple<string, int>> GetWarScoreBreakDown()
		{
			return new List<Tuple<string, int>>
			{
				new Tuple<string, int>("Attrition (Units):", this.GetUnitAttrition()),
				new Tuple<string, int>("Attrition (Time):", this.GetTimeAttrition()),
				new Tuple<string, int>("Gains (Province):", this.GetProvinceGains()),
				new Tuple<string, int>("Gains (k/d ratio):", this.GetKillDeathBonus())
			};
		}

		private int GetKillDeathBonus()
		{
			if (this.UnitsKilled + this.UnitsLost < 5)
			{
				return 0;
			}
			float num = (float)this.UnitsKilled / (float)this.UnitsLost;
			if (num < 1f)
			{
				return 0;
			}
			if (num > 10f)
			{
				num = 10f;
			}
			return (int)(num * 2f);
		}

		private int GetProvinceGains()
		{
			if (this.ProvincesGained < 0)
			{
				return this.ProvincesGained * 12;
			}
			return this.ProvincesGained * 6;
		}

		private int GetTimeAttrition()
		{
			return -1 * ((this.Game.TurnController.TurnNumber - this.StartTurn) / 5);
		}

		private int GetUnitAttrition()
		{
			return this.UnitsLost / -10;
		}

		public int GetWarScore()
		{
			int num = this.GetUnitAttrition();
			num += this.GetTimeAttrition();
			int num2 = this.GetProvinceGains();
			num2 += this.GetKillDeathBonus();
			return num2 + num;
		}

		internal void Dispose()
		{
		}

		internal bool LosingRealWar()
		{
			if (this.UnitsKilled + this.UnitsLost < 5)
			{
				return false;
			}
			float num = (float)this.UnitsKilled / (float)this.UnitsLost;
			return num < 1f && this.ProvincesGained <= -1;
		}

		public SovereigntyGame Game;

		public int EnemyID;

		public AIPlayer AI;

		public int StartTurn;

		public int UnitsLost;

		public int UnitsKilled;

		public int ProvincesGained;
	}
}
