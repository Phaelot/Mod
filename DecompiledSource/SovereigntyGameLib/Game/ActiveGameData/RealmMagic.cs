using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmMagic
	{
		public IList<SpellEffect> CastSpells
		{
			get
			{
				return this.Game.AllSpells.Values.Where((SpellEffect x) => x.CasterID == this.RealmID).ToList<SpellEffect>().AsReadOnly();
			}
		}

		public WorkingRealm Realm
		{
			get
			{
				return this.Game.AllRealms[this.RealmID];
			}
		}

		public RealmMagic(SovereigntyGame Game, WorkingRealm Realm)
		{
			this.RealmID = Realm.ID;
			this.Game = Game;
			this.m_CastSpells = new List<int>();
			this.IgnoreSpells = new List<string>();
			this.SpellPath = new List<string>();
			this.SpellLevel = 1;
			this.SpellPoints = Realm.MagicValue;
			this.SpellTree = new RealmSpell[3, 5];
			foreach (RealmMagicData realmMagicData in Game.GameCore.Data.Spells.Values)
			{
				SpellOwnerData ownerData = realmMagicData.GetOwnerData(Realm.Name);
				if (ownerData != null)
				{
					this.SpellTree[ownerData.Column - 1, realmMagicData.Level - 1] = new RealmSpell(realmMagicData, ownerData.PrerequisiteSpells);
				}
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.MagicXP);
			w.Write(this.MagicLevel);
			w.Write(this.SpellLevel);
			w.Write(this.SpellPoints);
			w.Write(this.SpellCoolDown);
			w.Write(this.SpellCount);
			w.Write(this.SpellCast);
			w.Write(this.CurrentInvestment);
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					if (this.SpellTree[i, j] == null)
					{
						w.Write(false);
					}
					else
					{
						w.Write(true);
						w.Write(this.SpellTree[i, j].Data.Name);
						w.Write(this.SpellTree[i, j].Learned);
						w.Write(this.SpellTree[i, j].RequiredSpells.Count);
						foreach (string text in this.SpellTree[i, j].RequiredSpells)
						{
							w.Write(text);
						}
					}
				}
			}
			w.Write(this.m_CastSpells.Count);
			foreach (int num in this.m_CastSpells)
			{
				w.Write(num);
			}
			w.Write(this.IgnoreSpells.Count);
			foreach (string text2 in this.IgnoreSpells)
			{
				w.Write(text2);
			}
			w.Write(this.SpellPath.Count);
			foreach (string text3 in this.SpellPath)
			{
				w.Write(text3);
			}
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.MagicXP = r.ReadInt32();
			this.MagicLevel = r.ReadInt32();
			this.SpellLevel = r.ReadInt32();
			this.SpellPoints = r.ReadInt32();
			this.SpellCoolDown = r.ReadInt32();
			this.SpellCount = r.ReadInt32();
			this.SpellCast = r.ReadBoolean();
			if (SaveVersion >= 46)
			{
				this.CurrentInvestment = r.ReadInt32();
			}
			this.SpellTree = new RealmSpell[3, 5];
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					if (r.ReadBoolean())
					{
						string text = r.ReadString();
						bool flag = r.ReadBoolean();
						List<string> list = new List<string>();
						int num = r.ReadInt32();
						for (int k = 0; k < num; k++)
						{
							list.Add(r.ReadString());
						}
						if (this.Game.Data.Spells.ContainsKey(text))
						{
							this.SpellTree[i, j] = new RealmSpell(this.Game.Data.Spells[text], list);
							this.SpellTree[i, j].Learned = flag;
						}
					}
				}
			}
			this.m_CastSpells = new List<int>();
			int num2 = r.ReadInt32();
			for (int l = 0; l < num2; l++)
			{
				this.m_CastSpells.Add(r.ReadInt32());
			}
			num2 = r.ReadInt32();
			for (int m = 0; m < num2; m++)
			{
				this.IgnoreSpells.Add(r.ReadString());
			}
			num2 = r.ReadInt32();
			for (int n = 0; n < num2; n++)
			{
				this.SpellPath.Add(r.ReadString());
			}
		}

		public void UpdateActiveSpells()
		{
			int num = this.Realm.Provinces.Sum((WorkingProvince x) => x.ResearchPoints);
			if (this.Realm.AIPlayer != null)
			{
				switch (this.Game.GameCore.Settings.GetEnumeratedSetting("Difficulty"))
				{
				case 1:
					num -= 2;
					break;
				case 2:
					num = num;
					break;
				case 3:
					num += 2;
					break;
				case 4:
					num += 5;
					break;
				case 5:
					num += 10;
					break;
				}
			}
			if (this.Realm.Lucky)
			{
				num += 40;
			}
			else if (this.Realm.Allies.Count((WorkingRealm x) => x.Lucky) > 0)
			{
				num += 10;
			}
			this.GiveMagicXP(num);
			if (this.SpellCoolDown > 0)
			{
				this.SpellCoolDown--;
			}
			this.SpellCast = false;
			foreach (SpellEffect spellEffect in this.CastSpells.ToList<SpellEffect>())
			{
				spellEffect.UpdateTimer();
			}
		}

		internal void GiveSpellPoints(int Points)
		{
			this.SpellPoints += Points;
		}

		public void GiveMagicXP(int XP)
		{
			if (this.MagicLevel == 20)
			{
				return;
			}
			this.MagicXP += XP;
			this.CheckMagicLevel();
		}

		public int GetXPForNextLevel()
		{
			return this.Game.GameCore.Data.GetXPForLevel(this.MagicLevel + 1);
		}

		public void ForceXP(int Value)
		{
			this.MagicXP = Value;
			this.CheckMagicLevel();
		}

		private void CheckMagicLevel()
		{
			while (this.MagicXP >= this.Game.GameCore.Data.GetXPForLevel(this.MagicLevel + 1))
			{
				this.SpellPoints++;
				this.MagicLevel++;
				this.Game.GameCore.FireEvent("MagicLevelGained", new object[] { this.Realm });
				if (this.Realm.CapitolProvince != null && this.Realm.CapitolProvince.LandNode.CurrentStack != null)
				{
					this.Realm.CapitolProvince.LandNode.CurrentStack.AwardHeroXP(10);
				}
			}
		}

		public bool CanLearnSpell(string Name)
		{
			if (this.SpellPoints == 0)
			{
				return false;
			}
			if (this.SpellCount == 6)
			{
				return false;
			}
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					if (this.SpellTree[i, j] != null && !(this.SpellTree[i, j].Data.Name != Name) && !this.SpellTree[i, j].Learned && this.SpellLevel >= j + 1)
					{
						bool flag = true;
						foreach (string text in this.SpellTree[i, j].RequiredSpells)
						{
							if (!this.SpellIsKnown(text))
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public List<RealmMagicData> GetAvailableLevelSpells(int Level)
		{
			List<RealmMagicData> list = new List<RealmMagicData>();
			for (int i = 0; i < 3; i++)
			{
				int num = Level - 1;
				if (this.SpellTree[i, num] != null && !this.SpellTree[i, num].Learned && !this.IgnoreSpells.Contains(this.SpellTree[i, num].Data.Name) && (this.SpellPath.Count <= 0 || !(this.SpellPath[0] != this.SpellTree[i, num].Data.Name)))
				{
					list.Add(this.SpellTree[i, num].Data);
				}
			}
			return list;
		}

		public List<RealmMagicData> GetKnownSpells(int Level)
		{
			List<RealmMagicData> list = new List<RealmMagicData>();
			for (int i = 0; i < 3; i++)
			{
				int num = Level - 1;
				if (this.SpellTree[i, num] != null && this.SpellTree[i, num].Learned)
				{
					list.Add(this.SpellTree[i, num].Data);
				}
			}
			return list;
		}

		public List<RealmMagicData> GetKnownSpells()
		{
			List<RealmMagicData> list = new List<RealmMagicData>();
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					if (this.SpellTree[i, j] != null && this.SpellTree[i, j].Learned)
					{
						list.Add(this.SpellTree[i, j].Data);
					}
				}
			}
			return list;
		}

		public bool SpellIsKnown(string Name)
		{
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					if (this.SpellTree[i, j] != null && !(this.SpellTree[i, j].Data.Name != Name) && this.SpellTree[i, j].Learned)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void LearnSpell(string Name)
		{
			if (this.SpellPoints == 0)
			{
				return;
			}
			if (!this.CanLearnSpell(Name))
			{
				return;
			}
			if (this.SpellPath.Count > 0 && this.SpellPath[0] == Name)
			{
				this.SpellPath.RemoveAt(0);
			}
			this.SpellPoints--;
			this.SpellCount++;
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					if (this.SpellTree[i, j] != null && !(this.SpellTree[i, j].Data.Name != Name))
					{
						this.SpellTree[i, j].Learned = true;
						this.Realm.HandleSpellLearned(this.SpellTree[i, j].Data);
						return;
					}
				}
			}
		}

		internal void Dispose()
		{
			foreach (SpellEffect spellEffect in this.CastSpells.ToList<SpellEffect>())
			{
				spellEffect.Dispel(true);
			}
		}

		public RealmSpell GetSpell(int Column, int Level)
		{
			return this.SpellTree[Column - 1, Level - 1];
		}

		internal void AddCastSpell(int ID)
		{
			this.m_CastSpells.Add(ID);
			this.SpellCast = true;
		}

		internal void RemoveCastSpell(int ID)
		{
			this.m_CastSpells.Remove(ID);
		}

		public void IncreaseSpellLevel()
		{
			if (this.SpellPoints <= 0)
			{
				return;
			}
			this.SpellPoints--;
			this.SpellLevel++;
		}

		public int GetResearchCost()
		{
			float num = (float)this.Game.EconomyController.GetRealmTotalIncome(this.Realm);
			return (int)(num / 100f);
		}

		public void Invest(int Amount)
		{
			this.CurrentInvestment += Amount;
			if (this.CurrentInvestment < 0)
			{
				this.CurrentInvestment = 0;
			}
			if (this.CurrentInvestment > 100)
			{
				this.CurrentInvestment = 100;
			}
		}

		public int GetCurrentInvestCost()
		{
			int researchCost = this.GetResearchCost();
			int i;
			for (i = researchCost * this.CurrentInvestment; i > this.Realm.GetMagicGold(); i -= researchCost)
			{
			}
			return i;
		}

		public void UpdateInvestment()
		{
			int i = this.GetResearchCost() * this.CurrentInvestment;
			int num = this.CurrentInvestment;
			while (i > this.Realm.GetMagicGold())
			{
				i -= this.GetResearchCost();
				num--;
			}
			this.GiveMagicXP(num);
			if (this.Realm.AIPlayer == null)
			{
				this.Realm.Gold.Value -= i;
				return;
			}
			this.Realm.AIPlayer.MagicManager.Funds.CurrentGold -= i;
		}

		public void SetSpellPath(params string[] Spells)
		{
			this.SpellPath.Clear();
			foreach (string text in Spells)
			{
				this.SpellPath.Add(text);
			}
		}

		public int GetInvestmentCost()
		{
			int i;
			for (i = this.GetResearchCost() * this.CurrentInvestment; i > this.Realm.GetMagicGold(); i -= this.GetResearchCost())
			{
			}
			return i;
		}

		internal void ModifyCooldown(int Amount)
		{
			this.SpellCoolDown += Amount;
			if (this.SpellCoolDown < 0)
			{
				this.SpellCoolDown = 0;
			}
		}

		public int MagicXP;

		public int MagicLevel;

		public int SpellLevel;

		public int SpellPoints;

		public int SpellCoolDown;

		private int SpellCount;

		public RealmSpell[,] SpellTree;

		private SovereigntyGame Game;

		private int RealmID;

		private List<int> m_CastSpells;

		public List<string> IgnoreSpells;

		private List<string> SpellPath;

		public int CurrentInvestment;

		public bool SpellCast;
	}
}
