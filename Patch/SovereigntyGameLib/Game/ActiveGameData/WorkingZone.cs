using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WorkingZone : GameRegion
	{
		public IList<ActivePathNode> Nodes
		{
			get
			{
				return this.Game.AllNodes.Values.Where((ActivePathNode x) => x.SeaZoneID == this.ID).ToList<ActivePathNode>().AsReadOnly();
			}
		}

		public WorkingZone(int ID, SovereigntyGame Game, SeaZoneData Data)
		{
			this.ID = ID;
			this.Game = Game;
			this.RegionID = Data.ID;
			this.ImageFile = Data.ArtName;
			this.Name = Data.Name;
			this.DisplayName = Data.DisplayName;
			this.TerrainString = Data.Terrain;
			this.SpellEffects = new SpellTargetData(Game);
			this.PathingNodes = new List<int>();
		}

		public WorkingZone(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.ID = r.ReadInt32();
			this.RegionID = r.ReadInt32();
			this.ImageFile = r.ReadString();
			this.Name = r.ReadString();
			this.DisplayName = r.ReadString();
			this.TerrainString = r.ReadString();
			this.SpellEffects = new SpellTargetData(Game);
			int num = r.ReadInt32();
			this.PathingNodes = new List<int>();
			for (int i = 0; i < num; i++)
			{
				this.PathingNodes.Add(r.ReadInt32());
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ID);
			w.Write(this.RegionID);
			w.Write(this.ImageFile);
			w.Write(this.Name);
			w.Write(this.DisplayName);
			w.Write(this.TerrainString);
			w.Write(this.PathingNodes.Count);
			foreach (int num in this.PathingNodes)
			{
				w.Write(num);
			}
		}

		public bool HasEnemies(WorkingRealm Realm)
		{
			foreach (ActivePathNode activePathNode in this.Nodes)
			{
				if (activePathNode.CurrentStack != null && activePathNode.CurrentStack.OwnerID != Realm.ID && activePathNode.CurrentStack.Owner.DiplomacyManager.GetRelation(Realm) == RelationStates.War)
				{
					return true;
				}
			}
			return false;
		}

		public int GetEnemyCount(WorkingRealm Realm)
		{
			int num = 0;
			foreach (ActivePathNode activePathNode in this.Nodes)
			{
				if (activePathNode.CurrentStack != null && activePathNode.CurrentStack.OwnerID != Realm.ID && activePathNode.CurrentStack.Owner.DiplomacyManager.GetRelation(Realm) == RelationStates.War)
				{
					num += activePathNode.CurrentStack.Units.Count;
				}
			}
			return num;
		}

		public int GetFriendlyCount(WorkingRealm Realm)
		{
			int num = 0;
			foreach (ActivePathNode activePathNode in this.Nodes)
			{
				if (activePathNode.CurrentStack != null && (activePathNode.CurrentStack.OwnerID == Realm.ID || activePathNode.CurrentStack.Owner.DiplomacyManager.GetRelation(Realm) == RelationStates.Alliance))
				{
					num += activePathNode.CurrentStack.Units.Count;
				}
			}
			return num;
		}

		internal void AddNode(int ID)
		{
			this.PathingNodes.Add(ID);
		}

		public List<WorkingUnit> GetAllUnits()
		{
			List<WorkingUnit> list = new List<WorkingUnit>();
			foreach (ActivePathNode activePathNode in this.Nodes)
			{
				if (activePathNode.CurrentStack != null)
				{
					list.AddRange(activePathNode.CurrentStack.Units);
				}
			}
			return list;
		}

		public int ID;

		private List<int> PathingNodes;

		public SpellTargetData SpellEffects;

		public string ImageFile;
	}
}
