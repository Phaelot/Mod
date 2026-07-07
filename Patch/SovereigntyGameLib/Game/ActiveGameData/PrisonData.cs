using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class PrisonData
	{
		public WorkingRealm Realm
		{
			get
			{
				WorkingRealm workingRealm = null;
				this.Game.AllRealms.TryGetValue(this.RealmID, out workingRealm);
				return workingRealm;
			}
		}

		public IList<WorkingUnit> AllPrisoners
		{
			get
			{
				List<WorkingUnit> list = new List<WorkingUnit>();
				foreach (int num in this.m_Prisoners.ToList<int>())
				{
					WorkingUnit workingUnit = null;
					this.Game.AllUnits.TryGetValue(num, out workingUnit);
					if (workingUnit != null)
					{
						list.Add(workingUnit);
					}
					else
					{
						this.m_Prisoners.Remove(num);
					}
				}
				return list.AsReadOnly();
			}
		}

		public List<WorkingUnit> GetRealmPrisoners(WorkingRealm Realm)
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			if (this.Prisoners.ContainsKey(Realm.ID))
			{
				foreach (int num in this.Prisoners[Realm.ID].ToList<int>())
				{
					WorkingUnit workingUnit = null;
					this.Game.AllUnits.TryGetValue(num, out workingUnit);
					if (workingUnit != null)
					{
						list.Add(workingUnit);
					}
					else
					{
						this.Prisoners[Realm.ID].Remove(num);
					}
				}
			}
			return list;
		}

		public List<WorkingRealm> GetRealms()
		{
			List<WorkingRealm> list = new List<WorkingRealm>();
			foreach (int num in this.Prisoners.Keys)
			{
				if (this.Prisoners[num].Count > 0)
				{
					WorkingRealm workingRealm = null;
					this.Game.AllRealms.TryGetValue(num, out workingRealm);
					if (workingRealm != null)
					{
						list.Add(workingRealm);
					}
				}
			}
			return list;
		}

		public int PrisonerCount
		{
			get
			{
				return this.m_Prisoners.Count;
			}
		}

		public PrisonData(SovereigntyGame Game, int RealmID)
		{
			this.Game = Game;
			this.RealmID = RealmID;
			this.Prisoners = new Dictionary<int, List<int>>();
			this.m_Prisoners = new List<int>();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.m_Prisoners.Count);
			foreach (int num in this.m_Prisoners)
			{
				w.Write(num);
			}
			w.Write(this.Prisoners.Count);
			foreach (KeyValuePair<int, List<int>> keyValuePair in this.Prisoners)
			{
				w.Write(keyValuePair.Key);
				w.Write(keyValuePair.Value.Count);
				foreach (int num2 in keyValuePair.Value)
				{
					w.Write(num2);
				}
			}
			w.Write(this.PrisonerUpkeepMultiplier);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			int num = r.ReadInt32();
			this.m_Prisoners = new List<int>();
			for (int i = 0; i < num; i++)
			{
				this.m_Prisoners.Add(r.ReadInt32());
			}
			this.Prisoners = new Dictionary<int, List<int>>();
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				int num2 = r.ReadInt32();
				int num3 = r.ReadInt32();
				List<int> list = new List<int>();
				for (int k = 0; k < num3; k++)
				{
					list.Add(r.ReadInt32());
				}
				this.Prisoners.Add(num2, list);
			}
			if (SaveVersion >= 29)
			{
				this.PrisonerUpkeepMultiplier = r.ReadSingle();
				return;
			}
			this.PrisonerUpkeepMultiplier = 0.3f;
		}


		private string GetUnitDebugName(WorkingUnit Unit)
		{
			if (Unit == null)
			{
				return "null";
			}
			if (!string.IsNullOrEmpty(Unit.DisplayName))
			{
				return Unit.DisplayName;
			}
			if (!string.IsNullOrEmpty(Unit.BaseName))
			{
				return Unit.BaseName;
			}
			return "unit#" + Unit.ID.ToString();
		}

		private void LogPrisonDebug(string Text)
		{
			try
			{
				string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SovereigntyAILogs");
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
				string file = System.IO.Path.Combine(folder, "prison_capture_debug.txt");
				WorkingRealm workingRealm = this.Realm;
				string realmName = (workingRealm == null) ? ("realmID=" + this.RealmID.ToString()) : (workingRealm.Name + "#" + workingRealm.ID.ToString());
				File.AppendAllText(file, DateTime.Now.ToString("HH:mm:ss.fff") + " [" + realmName + "] " + Text + "\r\n");
			}
			catch
			{
			}
		}

		private int GetStoredPrisonerCountForOwnerID(int OwnerRealmID)
		{
			if (this.Prisoners != null && this.Prisoners.ContainsKey(OwnerRealmID))
			{
				return this.Prisoners[OwnerRealmID].Count;
			}
			return 0;
		}

		private bool HasStoredPrisonerForOwnerID(int OwnerRealmID, int UnitID)
		{
			return this.Prisoners != null && this.Prisoners.ContainsKey(OwnerRealmID) && this.Prisoners[OwnerRealmID].Contains(UnitID);
		}

		public void ReleasePrisoner(WorkingUnit Unit)
		{
			int ownerRealmID = Unit.OwnerRealmID;
			int id = Unit.ID;
			Unit.IsPrisoner = false;
			if (!this.Prisoners.ContainsKey(ownerRealmID))
			{
				this.Prisoners.Add(ownerRealmID, new List<int>());
			}
			this.m_Prisoners.Remove(id);
			this.Prisoners[ownerRealmID].Remove(id);
			this.Game.GameCore.FireEvent("PrisonersChanged", new object[0]);
		}

		public void CaptureUnit(WorkingUnit Unit)
		{
			if (Unit == null)
			{
				this.LogPrisonDebug("CAPTURE ERROR: CaptureUnit called with null Unit");
				return;
			}
			int ownerRealmID = Unit.OwnerRealmID;
			int id = Unit.ID;
			WorkingRealm ownerRealm = Unit.OwnerRealm;
			string ownerName = (ownerRealm == null) ? ("ownerRealmID=" + ownerRealmID.ToString()) : (ownerRealm.Name + "#" + ownerRealm.ID.ToString());
			int beforeTotal = this.m_Prisoners.Count;
			int beforeOwner = this.GetStoredPrisonerCountForOwnerID(ownerRealmID);
			bool beforeTotalContains = this.m_Prisoners.Contains(id);
			bool beforeOwnerContains = this.HasStoredPrisonerForOwnerID(ownerRealmID, id);
			bool beforePrisonerFlag = Unit.IsPrisoner;
			Unit.IsPrisoner = true;
			if (!this.Prisoners.ContainsKey(ownerRealmID))
			{
				this.Prisoners.Add(ownerRealmID, new List<int>());
			}
			this.m_Prisoners.Add(id);
			this.Prisoners[ownerRealmID].Add(id);
			int afterTotal = this.m_Prisoners.Count;
			int afterOwner = this.GetStoredPrisonerCountForOwnerID(ownerRealmID);
			bool afterTotalContains = this.m_Prisoners.Contains(id);
			bool afterOwnerContains = this.HasStoredPrisonerForOwnerID(ownerRealmID, id);
			this.LogPrisonDebug("CAPTURE STOCK RESULT: unit=" + id + " name=" + this.GetUnitDebugName(Unit) + " class=" + Unit.Class.ToString() + " originalOwner=" + ownerName + " prisonerFlag " + beforePrisonerFlag.ToString() + "->" + Unit.IsPrisoner.ToString() + " total " + beforeTotal.ToString() + "->" + afterTotal.ToString() + " ownerBucket " + beforeOwner.ToString() + "->" + afterOwner.ToString() + " containsTotal " + beforeTotalContains.ToString() + "->" + afterTotalContains.ToString() + " containsOwner " + beforeOwnerContains.ToString() + "->" + afterOwnerContains.ToString());
			if (beforeTotalContains || beforeOwnerContains)
			{
				this.LogPrisonDebug("CAPTURE WARNING: duplicate prisoner entry before capture unit=" + id + " originalOwner=" + ownerName + " beforeTotalContains=" + beforeTotalContains.ToString() + " beforeOwnerContains=" + beforeOwnerContains.ToString());
			}
			if (!afterTotalContains || !afterOwnerContains || !Unit.IsPrisoner)
			{
				this.LogPrisonDebug("CAPTURE ERROR: unit not present in capturer stock after CaptureUnit unit=" + id + " originalOwner=" + ownerName + " afterTotalContains=" + afterTotalContains.ToString() + " afterOwnerContains=" + afterOwnerContains.ToString() + " prisonerFlag=" + Unit.IsPrisoner.ToString());
			}
			this.Game.GameCore.FireEvent("PrisonersChanged", new object[0]);
			this.Game.GameCore.FireEvent("PrisonerTaken", new object[] { this.RealmID });
		}

		public bool ActionPossible(PrisonActions Action, WorkingUnit Unit)
		{
			switch (Action)
			{
			case PrisonActions.Release:
				return this.Realm.DiplomacyManager.GetRelation(Unit.OwnerRealm) != RelationStates.War && Unit.OwnerRealm != this.Game.RebelRealm && !Unit.OwnerRealm.RealmIsDead;
			case PrisonActions.Trade:
				return Unit.OwnerRealm != this.Game.RebelRealm && !Unit.OwnerRealm.RealmIsDead;
			case PrisonActions.Enslave:
				if (this.Realm.Alignment != RealmAlignments.Evil)
				{
					return false;
				}
				return this.Realm.Provinces.Sum((WorkingProvince x) => x.GetSlaverySlots()) >= 1;
			case PrisonActions.Execute:
				return true;
			case PrisonActions.Experiment:
				return this.Realm.MagicValue != 0 && this.Realm.MagicData.SpellCoolDown >= 1;
			case PrisonActions.MakeUndead:
				return this.Realm.UnitPurchaseManager.GetUnitByRace(Races.Undead) != null && Unit.Race != Races.Undead && !Unit.CanPack;
			case PrisonActions.Recruit:
				return this.Realm.Race != Races.Undead && this.Realm.DiplomacyManager.GetRelation(Unit.OwnerRealm) != RelationStates.War && Unit.Rank != UnitRanks.Elite && Unit.Rank != UnitRanks.Unique && Unit.Race != Races.Undead && Unit.Race != Races.Monster && Unit.BaseCost != 0;
			default:
				return false;
			}
		}

		public bool ActionPossible(PrisonActions Action, List<WorkingUnit> Units)
		{
			if (Units.Count == 0)
			{
				return false;
			}
			switch (Action)
			{
			case PrisonActions.Release:
				if (Units.Count((WorkingUnit x) => this.Realm.DiplomacyManager.GetRelation(x.OwnerRealm) == RelationStates.War) > 0)
				{
					return false;
				}
				return Units.Count((WorkingUnit x) => x.OwnerRealm == this.Game.RebelRealm || x.OwnerRealm.RealmIsDead) <= 0;
			case PrisonActions.Trade:
			{
				if (Units.Count((WorkingUnit x) => x.OwnerRealm == this.Game.RebelRealm || x.OwnerRealm.RealmIsDead) > 0)
				{
					return false;
				}
				WorkingRealm Realm = Units[0].OwnerRealm;
				return Units.Count((WorkingUnit x) => x.OwnerRealm != Realm) <= 0;
			}
			case PrisonActions.Enslave:
				if (this.Realm.Alignment != RealmAlignments.Evil)
				{
					return false;
				}
				return this.Realm.Provinces.Sum((WorkingProvince x) => x.GetSlaverySlots()) >= Units.Count;
			case PrisonActions.Execute:
				return true;
			case PrisonActions.Experiment:
				return this.Realm.MagicValue != 0 && Units.Count == 1;
			case PrisonActions.MakeUndead:
			{
				if (this.Realm.UnitPurchaseManager.GetUnitByRace(Races.Undead) == null)
				{
					return false;
				}
				if (Units.Count((WorkingUnit x) => x.Race == Races.Undead || x.CanPack) > 0)
				{
					return false;
				}
				int num = Units.Sum((WorkingUnit x) => this.Game.PrisonerController.GetRecruitCost(x, this.Realm));
				return num <= this.Realm.GetPrisonGold();
			}
			case PrisonActions.Recruit:
			{
				if (this.Realm.Race == Races.Undead)
				{
					return false;
				}
				if (this.Realm.DiplomacyManager.GetRelation(Units[0].OwnerRealm) == RelationStates.War)
				{
					return false;
				}
				if (Units.Count((WorkingUnit x) => x.Rank == UnitRanks.Elite || x.Rank == UnitRanks.Unique || x.Race == Races.Undead || x.Race == Races.Monster) > 0)
				{
					return false;
				}
				if (Units.Count((WorkingUnit x) => x.BaseCost == 0) > 0)
				{
					return false;
				}
				int num2 = Units.Sum((WorkingUnit x) => this.Game.PrisonerController.GetRecruitCost(x, this.Realm));
				return num2 <= this.Realm.GetPrisonGold();
			}
			default:
				return false;
			}
		}

		public bool ContainsUnit(int ID)
		{
			return this.m_Prisoners.Contains(ID);
		}

		public int RealmID;

		public SovereigntyGame Game;

		private Dictionary<int, List<int>> Prisoners;

		private List<int> m_Prisoners;

		public float PrisonerUpkeepMultiplier = 0.1f;
	}
}
