using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.AI.V2;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Map;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game
{
	public class TurnController
	{
		public event RealmDelegate OnStartTurn;

		public WorkingRealm CurrentRealm
		{
			get
			{
				WorkingRealm workingRealm = null;
				this.Game.AllRealms.TryGetValue(this.CurrentRealmID, out workingRealm);
				return workingRealm;
			}
		}

		public TurnController(SovereigntyGame Game)
		{
			this.Game = Game;
			this.CurrentDate = new ActiveStat<DateTime>(new DateTime(1392, 4, 4, 0, 0, 0));
			WorkingRealm realm = Game.GetRealm("Boruvian Empire");
			this.TurnOrder = new List<int>();
			foreach (WorkingRealm workingRealm in Game.AllRealms.Values)
			{
				if (workingRealm != Game.PlayerRealm && workingRealm != Game.RebelRealm && workingRealm != realm)
				{
					this.TurnOrder.Add(workingRealm.ID);
				}
			}
			this.Shuffle<int>(this.TurnOrder);
			this.TurnOrder.Insert(0, realm.ID);
			this.TurnOrder.Insert(0, Game.PlayerRealm.ID);
			this.TurnOrder.Add(Game.RebelRealm.ID);
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.CurrentRealmID = r.ReadInt32();
			this.CurrentRealmIndex = r.ReadInt32();
			this.CurrentDate.Value = DateTime.FromBinary(r.ReadInt64());
			int num = r.ReadInt32();
			this.TurnOrder = new List<int>();
			for (int i = 0; i < num; i++)
			{
				this.TurnOrder.Add(r.ReadInt32());
			}
			this.TurnNumber = r.ReadInt32();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.CurrentRealmID);
			w.Write(this.CurrentRealmIndex);
			w.Write(this.CurrentDate.Value.ToBinary());
			w.Write(this.TurnOrder.Count);
			foreach (int num in this.TurnOrder)
			{
				w.Write(num);
			}
			w.Write(this.TurnNumber);
		}

		public void Shuffle<T>(List<T> list)
		{
			Random random = new Random();
			int i = list.Count;
			while (i > 1)
			{
				i--;
				int num = random.Next(i + 1);
				T t = list[num];
				list[num] = list[i];
				list[i] = t;
			}
		}

		public void StartFirstTurn()
		{
			this.CurrentRealmIndex = -1;
			this.Game.StartGameTurn();
			this.RequestEndTurn();
		}

		public void RedoTurnStart()
		{
			this.Game.StartPlayerTurn(this.CurrentRealm);
			if (this.OnStartTurn != null)
			{
				this.OnStartTurn(this.CurrentRealm);
			}
		}

		public void RequestEndTurn()
		{
			if (this.CurrentRealmIndex >= 0 && this.Game.AllRealms[this.TurnOrder[this.CurrentRealmIndex]] == this.Game.PlayerRealm)
			{
				this.Game.GameCore.Map.ChangeMode(MapModes.Default, false);
			}
			this.CurrentRealmIndex++;
			if (this.CurrentRealmIndex >= this.TurnOrder.Count)
			{
				this.CurrentRealmIndex = 0;
				this.TurnNumber++;
				this.CurrentDate.Value += new TimeSpan(14, 0, 0, 0, 0);
				this.Game.StartGameTurn();
			}
			if (this.Game.IgnoreHumanPlayer && this.Game.AllRealms[this.TurnOrder[this.CurrentRealmIndex]] == this.Game.PlayerRealm)
			{
				this.CurrentRealmIndex++;
			}
			this.CurrentRealmID = this.TurnOrder[this.CurrentRealmIndex];
			if (this.Game.PendingDispose)
			{
				return;
			}
			this.Game.StartPlayerTurn(this.CurrentRealm);
			if (this.OnStartTurn != null)
			{
				this.OnStartTurn(this.CurrentRealm);
			}
			AIPlayer aiplayer = null;
			this.Game.AllAIPlayers.TryGetValue(this.CurrentRealmID, out aiplayer);
			if (aiplayer != null)
			{
				aiplayer.BeginTurn();
			}
		}

		public void Dispose()
		{
			this.CurrentDate.Dispose();
			this.OnStartTurn = null;
		}

		public int GetRealmCount()
		{
			return this.CurrentRealmIndex;
		}

		public int CurrentRealmID;

		private List<int> TurnOrder;

		private int CurrentRealmIndex;

		private SovereigntyGame Game;

		public ActiveStat<DateTime> CurrentDate;

		public int TurnNumber;
	}
}
