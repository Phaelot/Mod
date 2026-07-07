using System;
using System.Collections.Generic;
using System.IO;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmRestrictions
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

		public RealmRestrictions(SovereigntyGame Game, int RealmID)
		{
			this.Game = Game;
			this.RealmID = RealmID;
			this.PermaAllies = new List<string>();
			this.PermaPeace = new List<string>();
			this.PermaWar = new List<string>();
			this.IgnoreSpells = new List<string>();
			this.AllowUnits = new List<string>();
			this.DenyUnits = new List<string>();
			this.IgnoreProvinces = new List<string>();
			this.GuaranteedTrades = new List<string>();
			this.AllowedAlliances = new List<string>();
			this.ForceAllowAlliance = new List<string>();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.AllowAlliances);
			w.Write(this.AllowWar);
			w.Write(this.PreventHeroes);
			this.SaveList(w, this.PermaAllies);
			this.SaveList(w, this.PermaPeace);
			this.SaveList(w, this.PermaWar);
			this.SaveList(w, this.IgnoreSpells);
			this.SaveList(w, this.AllowUnits);
			this.SaveList(w, this.DenyUnits);
			this.SaveList(w, this.IgnoreProvinces);
			this.SaveList(w, this.GuaranteedTrades);
			this.SaveList(w, this.AllowedAlliances);
			this.SaveList(w, this.ForceAllowAlliance);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.AllowAlliances = r.ReadBoolean();
			this.AllowWar = r.ReadBoolean();
			this.PreventHeroes = r.ReadBoolean();
			this.LoadList(r, ref this.PermaAllies);
			this.LoadList(r, ref this.PermaPeace);
			this.LoadList(r, ref this.PermaWar);
			this.LoadList(r, ref this.IgnoreSpells);
			this.LoadList(r, ref this.AllowUnits);
			this.LoadList(r, ref this.DenyUnits);
			this.LoadList(r, ref this.IgnoreProvinces);
			this.LoadList(r, ref this.GuaranteedTrades);
			this.LoadList(r, ref this.AllowedAlliances);
			this.LoadList(r, ref this.ForceAllowAlliance);
		}

		private void LoadList(BinaryReader r, ref List<string> List)
		{
			List = new List<string>();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				List.Add(r.ReadString());
			}
		}

		private void SaveList(BinaryWriter w, List<string> List)
		{
			w.Write(List.Count);
			foreach (string text in List)
			{
				w.Write(text);
			}
		}

		public bool IsPermAlly(WorkingRealm realm)
		{
			return this.PermaAllies.Contains(realm.Name);
		}

		internal void Dispose()
		{
		}

		public bool CanDeclareWar(WorkingRealm TargetRealm)
		{
			return this.AllowWar && TargetRealm != this.Realm && this.Realm.DiplomacyManager.GetRelation(TargetRealm) != RelationStates.ForcedPeace && this.Realm.DiplomacyManager.GetRelation(TargetRealm) != RelationStates.Alliance && this.Realm.DiplomacyManager.GetRelation(TargetRealm) != RelationStates.Defence && this.Realm.DiplomacyManager.GetRelation(TargetRealm) != RelationStates.NAP && this.Realm.DiplomacyManager.GetRelation(TargetRealm) != RelationStates.War && !this.PermaPeace.Contains(TargetRealm.Name);
		}

		public bool CanOfferPeace(WorkingRealm TargetRealm)
		{
			return TargetRealm != this.Realm && !this.PermaWar.Contains(TargetRealm.Name) && TargetRealm.DiplomacyManager.GetRelationTime(this.Realm) >= 5;
		}

		public List<string> PermaAllies;

		public List<string> PermaPeace;

		public List<string> PermaWar;

		public List<string> IgnoreSpells;

		public List<string> AllowUnits;

		public List<string> DenyUnits;

		public List<string> IgnoreProvinces;

		public List<string> GuaranteedTrades;

		public List<string> AllowedAlliances;

		public List<string> ForceAllowAlliance;

		public bool AllowAlliances = true;

		public bool AllowWar = true;

		public int RealmID;

		public SovereigntyGame Game;

		public bool PreventHeroes;

		public bool AllowTrade;
	}
}
