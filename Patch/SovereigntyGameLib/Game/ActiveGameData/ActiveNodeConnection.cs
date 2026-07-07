using System;
using System.IO;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class ActiveNodeConnection
	{
		public ActiveNodeConnection(SovereigntyGame Game)
		{
			this.Game = Game;
		}

		public ActivePathNode TargetNode
		{
			get
			{
				return this.Game.AllNodes[this.TargetNodeID];
			}
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.TargetNodeID);
			w.Write((short)this.ConnectionType);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.TargetNodeID = r.ReadInt32();
			this.ConnectionType = (ConnectionTypes)r.ReadInt16();
		}

		private SovereigntyGame Game;

		public int TargetNodeID;

		public ConnectionTypes ConnectionType;
	}
}
