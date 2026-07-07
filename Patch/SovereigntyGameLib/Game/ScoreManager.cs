using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game
{
	public class ScoreManager
	{
		public ScoreManager(SovereigntyGame Game)
		{
			this.Game = Game;
			this.Categories = 7;
			this.Scores = new Dictionary<int, float[]>();
			this.GreatPowers = new List<int>();
			foreach (WorkingRealm workingRealm in Game.AllRealms.Values)
			{
				if (workingRealm != Game.RebelRealm)
				{
					this.Scores.Add(workingRealm.ID, new float[this.Categories]);
				}
			}
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			int num = r.ReadInt32();
			this.GreatPowers = new List<int>();
			for (int i = 0; i < num; i++)
			{
				this.GreatPowers.Add(r.ReadInt32());
			}
			num = r.ReadInt32();
			this.Categories = r.ReadInt32();
			this.Scores = new Dictionary<int, float[]>();
			for (int j = 0; j < num; j++)
			{
				int num2 = r.ReadInt32();
				float[] array = new float[this.Categories];
				for (int k = 0; k < this.Categories; k++)
				{
					array[k] = r.ReadSingle();
				}
				this.Scores.Add(num2, array);
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.GreatPowers.Count);
			foreach (int num in this.GreatPowers)
			{
				w.Write(num);
			}
			w.Write(this.Scores.Count);
			w.Write(this.Categories);
			foreach (KeyValuePair<int, float[]> keyValuePair in this.Scores)
			{
				w.Write(keyValuePair.Key);
				for (int i = 0; i < this.Categories; i++)
				{
					w.Write(keyValuePair.Value[i]);
				}
			}
		}

		public Dictionary<WorkingRealm, float> GetRealmScores(RankingCategories Category)
		{
			Dictionary<WorkingRealm, float> dictionary = new Dictionary<WorkingRealm, float>();
			foreach (KeyValuePair<int, float[]> keyValuePair in this.Scores.OrderByDescending((KeyValuePair<int, float[]> x) => x.Value[(int)Category]))
			{
				WorkingRealm workingRealm = this.Game.AllRealms[keyValuePair.Key];
				if (!workingRealm.RealmIsDead)
				{
					dictionary.Add(workingRealm, keyValuePair.Value[(int)Category]);
				}
			}
			return dictionary;
		}

		public PowerGroup GetPowerGroup(WorkingRealm Realm, RankingCategories Category)
		{
			Dictionary<WorkingRealm, float> realmScores = this.GetRealmScores(Category);
			int num = 0;
			foreach (KeyValuePair<WorkingRealm, float> keyValuePair in realmScores)
			{
				if (keyValuePair.Key == Realm)
				{
					break;
				}
				num++;
			}
			if (num == 0)
			{
				return PowerGroup.First;
			}
			if (num < 5)
			{
				return PowerGroup.Great;
			}
			if (num > realmScores.Count - 5)
			{
				return PowerGroup.Minor;
			}
			return PowerGroup.Average;
		}

		public void UpdateScores()
		{
			PowerGroup powerGroup = this.GetPowerGroup(this.Game.PlayerRealm, RankingCategories.World);
			foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
			{
				if (workingRealm != this.Game.RebelRealm)
				{
					this.Scores[workingRealm.ID][6] = this.GetDiplomacyScore(workingRealm);
					this.Scores[workingRealm.ID][4] = this.GetEconomyScore(workingRealm);
					this.Scores[workingRealm.ID][2] = this.GetLandScore(workingRealm);
					this.Scores[workingRealm.ID][3] = this.GetSeaScore(workingRealm);
					this.Scores[workingRealm.ID][1] = this.GetGeographicScore(workingRealm);
					this.Scores[workingRealm.ID][5] = this.GetMagicScore(workingRealm);
					this.Scores[workingRealm.ID][0] = this.GetWorldScore(workingRealm);
				}
			}
			this.GreatPowers.Clear();
			int num = 5;
			foreach (KeyValuePair<int, float[]> keyValuePair in this.Scores.OrderByDescending((KeyValuePair<int, float[]> x) => x.Value[0]))
			{
				num--;
				this.GreatPowers.Add(keyValuePair.Key);
				if (num == 0)
				{
					break;
				}
			}
			PowerGroup powerGroup2 = this.GetPowerGroup(this.Game.PlayerRealm, RankingCategories.World);
			if (powerGroup != powerGroup2)
			{
				this.Game.GameCore.FireEvent("PowerGroupChanged", new object[] { powerGroup, powerGroup2 });
			}
		}

		private float GetWorldScore(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return 0f;
			}
			float num = 0f;
			num += this.Scores[Realm.ID][6];
			num += this.Scores[Realm.ID][4];
			num += this.Scores[Realm.ID][1];
			return num + this.Scores[Realm.ID][5];
		}

		private float GetMagicScore(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return 0f;
			}
			float num = 0f;
			return num + (float)(Realm.MagicData.GetKnownSpells().Sum((RealmMagicData x) => x.Level) * 5);
		}

		private float GetGeographicScore(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return 0f;
			}
			float num = 0f;
			return num + (this.Scores[Realm.ID][2] + this.Scores[Realm.ID][3]);
		}

		private float GetSeaScore(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return 0f;
			}
			float num = 0f;
			foreach (WorkingUnit workingUnit in Realm.Units)
			{
				if (workingUnit.OwnerStack != null && workingUnit.OwnerStack.Node != null && workingUnit.OwnerStack.Node.NodeType != PathNodeTypes.Land && workingUnit.Class == UnitClasses.Naval && workingUnit.CarriedUnit == null)
				{
					switch (workingUnit.Rank)
					{
					case UnitRanks.Standard:
					case UnitRanks.Mercenary:
						num += 1f;
						break;
					case UnitRanks.Elite:
						num += 2f;
						break;
					case UnitRanks.Unique:
						num += 5f;
						break;
					}
				}
			}
			num += (float)(Realm.Provinces.Count((WorkingProvince x) => x.HasHarbour) * 5);
			return num;
		}

		private float GetLandScore(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return 0f;
			}
			float num = 0f;
			foreach (WorkingUnit workingUnit in Realm.Units)
			{
				if (workingUnit.OwnerStack != null && workingUnit.OwnerStack.Node != null && workingUnit.OwnerStack.Node.NodeType != PathNodeTypes.Sea && workingUnit.Class != UnitClasses.Naval)
				{
					switch (workingUnit.Rank)
					{
					case UnitRanks.Standard:
					case UnitRanks.Mercenary:
						num += 1f;
						break;
					case UnitRanks.Elite:
						num += 2f;
						break;
					case UnitRanks.Unique:
						num += 5f;
						break;
					}
				}
			}
			num += (float)(Realm.Provinces.Sum((WorkingProvince x) => x.FortLevel) * 2);
			return num;
		}

		private float GetEconomyScore(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return 0f;
			}
			float num = 0f;
			foreach (WorkingProvince workingProvince in Realm.Provinces)
			{
				float num2 = (float)workingProvince.CurrentEconomy;
				num2 += (float)(workingProvince.Buildings.Count<BuildingEffect>() * 3);
				if (workingProvince.Cradle != ArtScienceTypes.None)
				{
					num2 += 15f;
					if (workingProvince.Cradle == ArtScienceTypes.Statecraft)
					{
						num2 += 30f;
					}
				}
				if (workingProvince.Resource != null)
				{
					num2 += (float)(workingProvince.ResourceIncome.GetValue() * 4);
				}
				if (workingProvince.IsCapitol)
				{
					num2 *= 2f;
				}
				if (workingProvince.Occupied)
				{
					num2 *= 0.5f;
				}
				num += num2;
			}
			num += (float)(Realm.TradeManager.GetTradeCount() * 3);
			return num;
		}

		private float GetDiplomacyScore(WorkingRealm Realm)
		{
			if (Realm.RealmIsDead)
			{
				return 0f;
			}
			float num = 0f;
			num += (float)(Realm.Allies.Count<WorkingRealm>() * 25);
			num += (float)((Realm.Enemies.Count<WorkingRealm>() - 1) * -15);
			if (this.GreatPowers.Contains(Realm.ID))
			{
				num += 75f;
			}
			foreach (WorkingUnit workingUnit in Realm.Prison.AllPrisoners)
			{
				switch (workingUnit.Rank)
				{
				case UnitRanks.Standard:
					num += 0.5f;
					break;
				case UnitRanks.Elite:
					num += 1f;
					break;
				case UnitRanks.Unique:
					num += 3f;
					break;
				}
			}
			return num;
		}

		public int GetRank(WorkingRealm Realm, RankingCategories Category)
		{
			int num = 1;
			foreach (KeyValuePair<int, float[]> keyValuePair in this.Scores.OrderByDescending((KeyValuePair<int, float[]> x) => x.Value[(int)Category]))
			{
				if (keyValuePair.Key == Realm.ID)
				{
					return num;
				}
				num++;
			}
			return -1;
		}

		internal float GetScoreAtRank(int Rank)
		{
			foreach (KeyValuePair<int, float[]> keyValuePair in this.Scores.OrderByDescending((KeyValuePair<int, float[]> x) => x.Value[0]))
			{
				Rank--;
				if (Rank == 0)
				{
					return keyValuePair.Value[0];
				}
			}
			return 0f;
		}

		internal float GetScoreForRealm(WorkingRealm Realm)
		{
			return this.Scores[Realm.ID][0];
		}

		public const int STD_UNIT_MULT = 1;

		public const int ELITE_UNIT_MULT = 2;

		public const int UNIQUE_UNIT_MULT = 5;

		public const int HERO_MULT = 6;

		public const int NAVAL_UNIT_MULT = 3;

		public const int HARBOUR_MULT = 5;

		public const int TRADE_RT_MULT = 3;

		public const int ECON_MULT = 4;

		public const int ECON_CAP_MULT = 2;

		public const float ECON_OCC_MULT = 0.5f;

		public const int VALUE_MULT = 2;

		public const int ALLY_MULT = 25;

		public const int ENEMY_MULT = -15;

		public const int FORT_MULT = 2;

		public const int LANDMARK_MULT = 2;

		public const int GREAT_POWER_VAL = 75;

		public const int SPELL_LVL_MULT = 5;

		public const int CRADLE_VAL = 15;

		public const int STATECRAFT_VAL = 30;

		public const float STD_PRISONER_MULT = 0.5f;

		public const int ELITE_PRISONER_MULT = 1;

		public const int UNIQUE_PRISONER_MULT = 3;

		public const int HERO_PRISONER_MULT = 3;

		private Dictionary<int, float[]> Scores;

		private SovereigntyGame Game;

		private List<int> GreatPowers;

		private int Categories;
	}
}
