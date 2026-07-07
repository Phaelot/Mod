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
			this.WriteTurnDebugLog("RequestEndTurn: BEGIN index=" + this.CurrentRealmIndex + " turn=" + this.TurnNumber);
			try
			{
				if (this.CurrentRealmIndex >= 0)
				{
					this.WriteTurnDebugLog("RequestEndTurn: leaving realm " + this.DescribeRealmID(this.TurnOrder[this.CurrentRealmIndex]));
				}
				if (this.CurrentRealmIndex >= 0 && this.Game.AllRealms[this.TurnOrder[this.CurrentRealmIndex]] == this.Game.PlayerRealm)
				{
					this.WriteTurnDebugLog("RequestEndTurn: player turn ended, changing map to Default");
					this.Game.GameCore.Map.ChangeMode(MapModes.Default, false);
					this.WriteTurnDebugLog("RequestEndTurn: map mode changed");
				}
				this.CurrentRealmIndex++;
				this.WriteTurnDebugLog("RequestEndTurn: advanced index=" + this.CurrentRealmIndex + " of " + this.TurnOrder.Count);
				if (this.CurrentRealmIndex >= this.TurnOrder.Count)
				{
					this.WriteTurnDebugLog("RequestEndTurn: starting new full game turn");
					this.CurrentRealmIndex = 0;
					this.TurnNumber++;
					this.CurrentDate.Value += new TimeSpan(14, 0, 0, 0, 0);
					this.WriteTurnDebugLog("RequestEndTurn: calling StartGameTurn turn=" + this.TurnNumber);
					this.Game.StartGameTurn();
					this.WriteTurnDebugLog("RequestEndTurn: StartGameTurn finished");
				}
				if (this.Game.IgnoreHumanPlayer && this.Game.AllRealms[this.TurnOrder[this.CurrentRealmIndex]] == this.Game.PlayerRealm)
				{
					this.WriteTurnDebugLog("RequestEndTurn: IgnoreHumanPlayer skips player realm");
					this.CurrentRealmIndex++;
				}
				this.CurrentRealmID = this.TurnOrder[this.CurrentRealmIndex];
				this.WriteTurnDebugLog("RequestEndTurn: current realm set to " + this.DescribeRealmID(this.CurrentRealmID));
				if (this.Game.PendingDispose)
				{
					this.WriteTurnDebugLog("RequestEndTurn: PendingDispose, returning");
					return;
				}
				this.WriteTurnDebugLog("RequestEndTurn: calling StartPlayerTurn for " + this.DescribeRealmID(this.CurrentRealmID));
				this.Game.StartPlayerTurn(this.CurrentRealm);
				this.WriteTurnDebugLog("RequestEndTurn: StartPlayerTurn finished for " + this.DescribeRealmID(this.CurrentRealmID));
				if (this.OnStartTurn != null)
				{
					this.WriteTurnDebugLog("RequestEndTurn: firing OnStartTurn for " + this.DescribeRealmID(this.CurrentRealmID));
					this.OnStartTurn(this.CurrentRealm);
					this.WriteTurnDebugLog("RequestEndTurn: OnStartTurn finished for " + this.DescribeRealmID(this.CurrentRealmID));
				}
				AIPlayer aiplayer = null;
				this.Game.AllAIPlayers.TryGetValue(this.CurrentRealmID, out aiplayer);
				if (aiplayer != null)
				{
					this.WriteTurnDebugLog("RequestEndTurn: AI BeginTurn for " + this.DescribeRealmID(this.CurrentRealmID));
					aiplayer.BeginTurn();
					this.WriteTurnDebugLog("RequestEndTurn: AI BeginTurn finished for " + this.DescribeRealmID(this.CurrentRealmID));
				}
				else
				{
					this.WriteTurnDebugLog("RequestEndTurn: no AI player for " + this.DescribeRealmID(this.CurrentRealmID));
				}
				this.WriteTurnDebugLog("RequestEndTurn: END current=" + this.DescribeRealmID(this.CurrentRealmID));
			}
			catch (Exception ex)
			{
				this.WriteTurnDebugLog("RequestEndTurn: EXCEPTION current=" + this.DescribeRealmID(this.CurrentRealmID) + " index=" + this.CurrentRealmIndex + " turn=" + this.TurnNumber + Environment.NewLine + ex.ToString());
				throw;
			}
		}

		private string DescribeRealmID(int realmID)
		{
			try
			{
				WorkingRealm workingRealm = null;
				if (this.Game != null && this.Game.AllRealms != null && this.Game.AllRealms.TryGetValue(realmID, out workingRealm) && workingRealm != null)
				{
					return workingRealm.Name + " [" + realmID + "]";
				}
			}
			catch
			{
			}
			return "RealmID " + realmID;
		}

		private void WriteTurnDebugLog(string text)
		{
			try
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string path = System.IO.Path.Combine(folderPath, "SovereigntyTurnDebugLogs");
				Directory.CreateDirectory(path);
				string fileName = System.IO.Path.Combine(path, "TurnResolution.log");
				string currentRealm = this.CurrentRealmID.ToString();
				try
				{
					currentRealm = this.DescribeRealmID(this.CurrentRealmID);
				}
				catch
				{
				}
				File.AppendAllText(fileName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " turn=" + this.TurnNumber + " date=" + this.CurrentDate.Value.ToShortDateString() + " realm=" + currentRealm + " :: " + text + Environment.NewLine);
			}
			catch
			{
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
