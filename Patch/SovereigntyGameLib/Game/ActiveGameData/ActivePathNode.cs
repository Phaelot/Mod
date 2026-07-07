using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class ActivePathNode
	{
		public int RegionID
		{
			get
			{
				if (this.ProvinceID != -1)
				{
					return this.Province.RegionID;
				}
				return this.Zone.RegionID;
			}
		}

		public WorkingStack CurrentStack
		{
			get
			{
				WorkingStack workingStack = null;
				this.Game.AllStacks.TryGetValue(this.CurrentStackID, out workingStack);
				return workingStack;
			}
		}

		public WorkingProvince Province
		{
			get
			{
				WorkingProvince workingProvince = null;
				this.Game.AllProvinces.TryGetValue(this.ProvinceID, out workingProvince);
				return workingProvince;
			}
		}

		public WorkingZone Zone
		{
			get
			{
				WorkingZone workingZone = null;
				this.Game.AllZones.TryGetValue(this.SeaZoneID, out workingZone);
				return workingZone;
			}
		}

		public ActivePathNode(SovereigntyGame Game, int ID, PathingNodeData Node)
		{
			this.Game = Game;
			this.ID = ID;
			this.BaseNodeID = Node.ID;
			this.MapCoords = Node.MapCoords;
			this.NodeType = Node.NodeType;
			this.ProvinceID = -1;
			this.SeaZoneID = -1;
			object obj = Game.GameCore.Data.ProvincesByID[Node.ProvinceID];
			if (obj is ProvinceData)
			{
				this.ProvinceID = Game.GetProvince((obj as ProvinceData).Name).ID;
			}
			else
			{
				this.SeaZoneID = Game.GetZone((obj as SeaZoneData).Name).ID;
			}
			if (this.ProvinceID != -1)
			{
				if (this.NodeType == PathNodeTypes.Harbour || this.NodeType == PathNodeTypes.RiverHarbour)
				{
					this.Province.HarbourNodeID = ID;
				}
				if (this.NodeType == PathNodeTypes.Land)
				{
					this.Province.LandNodeID = ID;
				}
			}
			if (this.SeaZoneID != -1)
			{
				this.Zone.AddNode(ID);
			}
			this.CurrentStackID = -1;
			this.AllyStacks = new List<int>();
			this.ConnectedNodes = new List<ActiveNodeConnection>();
		}

		public ActivePathNode(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.ID = r.ReadInt32();
			this.BaseNodeID = r.ReadInt32();
			this.MapCoords.X = r.ReadInt32();
			this.MapCoords.Y = r.ReadInt32();
			this.NodeType = (PathNodeTypes)r.ReadInt16();
			this.ProvinceID = r.ReadInt32();
			this.SeaZoneID = r.ReadInt32();
			this.CurrentStackID = r.ReadInt32();
			int num = r.ReadInt32();
			this.AllyStacks = new List<int>();
			for (int i = 0; i < num; i++)
			{
				this.AllyStacks.Add(r.ReadInt32());
			}
			num = r.ReadInt32();
			this.ConnectedNodes = new List<ActiveNodeConnection>();
			for (int j = 0; j < num; j++)
			{
				ActiveNodeConnection activeNodeConnection = new ActiveNodeConnection(Game);
				activeNodeConnection.Load(r, SaveVersion);
				this.ConnectedNodes.Add(activeNodeConnection);
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ID);
			w.Write(this.BaseNodeID);
			w.Write(this.MapCoords.X);
			w.Write(this.MapCoords.Y);
			w.Write((short)this.NodeType);
			w.Write(this.ProvinceID);
			w.Write(this.SeaZoneID);
			w.Write(this.CurrentStackID);
			w.Write(this.AllyStacks.Count);
			foreach (int num in this.AllyStacks)
			{
				w.Write(num);
			}
			w.Write(this.ConnectedNodes.Count);
			foreach (ActiveNodeConnection activeNodeConnection in this.ConnectedNodes)
			{
				activeNodeConnection.Save(w);
			}
		}

		public void CreateConnections(PathingNodeData Node)
		{
			using (List<NodeConnection>.Enumerator enumerator = Node.ConnectedNodes.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					NodeConnection Connection = enumerator.Current;
					ActiveNodeConnection activeNodeConnection = new ActiveNodeConnection(this.Game);
					activeNodeConnection.ConnectionType = Connection.ConnectionType;
					activeNodeConnection.TargetNodeID = this.Game.AllNodes.Values.FirstOrDefault((ActivePathNode x) => x.BaseNodeID == Connection.NodeID).ID;
					this.ConnectedNodes.Add(activeNodeConnection);
				}
			}
		}

		public GameRegion GetRegion()
		{
			if (this.Province != null)
			{
				return this.Province;
			}
			if (this.Zone != null)
			{
				return this.Zone;
			}
			return null;
		}

		internal WorkingStack GetRealmStack(WorkingRealm Realm)
		{
			if (this.CurrentStack != null && this.CurrentStack.Owner == Realm)
			{
				return this.CurrentStack;
			}
			foreach (int num in this.AllyStacks)
			{
				WorkingStack workingStack = this.Game.AllStacks[num];
				if (workingStack.Owner == Realm)
				{
					return workingStack;
				}
			}
			return null;
		}

		public bool HasLandmark()
		{
			return this.Province != null && this.Province.Landmark != null && !(this.Province.Landmark == "");
		}

		public bool HasLandmark(string Landmark)
		{
			return this.Province != null && this.Province.Landmark == Landmark;
		}

		internal List<ActivePathNode> GetConnectedNodes()
		{
			List<ActivePathNode> list = new List<ActivePathNode>();
			foreach (ActiveNodeConnection activeNodeConnection in this.ConnectedNodes)
			{
				list.Add(activeNodeConnection.TargetNode);
			}
			return list;
		}

		public Point GetStackCoords(int StackID)
		{
			if (StackID == this.CurrentStackID)
			{
				return this.MapCoords;
			}
			switch (this.AllyStacks.IndexOf(StackID))
			{
			case 0:
			{
				Point mapCoords = this.MapCoords;
				mapCoords.X -= 48;
				return mapCoords;
			}
			case 1:
			{
				Point mapCoords2 = this.MapCoords;
				mapCoords2.X += 48;
				return mapCoords2;
			}
			case 2:
			{
				Point mapCoords3 = this.MapCoords;
				mapCoords3.Y += 48;
				return mapCoords3;
			}
			case 3:
			{
				Point mapCoords4 = this.MapCoords;
				mapCoords4.Y -= 48;
				return mapCoords4;
			}
			default:
				return this.MapCoords;
			}
		}

		internal void RemoveStack(int StackID)
		{
			if (this.CurrentStackID == StackID)
			{
				this.CurrentStackID = -1;
			}
			int num = this.AllyStacks.IndexOf(StackID);
			if (num >= 0)
			{
				this.AllyStacks.RemoveAt(num);
			}
		}

		private int BaseNodeID;

		public int ID;

		public Point MapCoords;

		public PathNodeTypes NodeType;

		public List<ActiveNodeConnection> ConnectedNodes;

		public int ProvinceID;

		public int SeaZoneID;

		public int CurrentStackID;

		public List<int> AllyStacks;

		private SovereigntyGame Game;

		public List<int> ReachableNodes;
	}
}
