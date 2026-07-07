using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI.V2
{
	public class AIEspionageManager
	{
		public IList<WorkingAgent> Agents
		{
			get
			{
				return this.AI.Game.AllAgents.Values.Where((WorkingAgent x) => this.AgentIDs.Contains(x.ID)).ToList<WorkingAgent>().AsReadOnly();
			}
		}

		public AIEspionageManager(AIPlayer AI)
		{
			this.AI = AI;
			this.Funds = new AIFundData();
			this.AgentIDs = new List<int>();
			this.ActiveMissions = new List<CovertMissionData>();
			this.RecentSpies = new List<int>();
			this.RNG = new Random();
		}

		internal void Dispose()
		{
		}

		internal void Save(BinaryWriter w)
		{
			this.Funds.Save(w);
			w.Write(this.AgentIDs.Count);
			foreach (int num in this.AgentIDs)
			{
				w.Write(num);
			}
			w.Write(this.ActiveMissions.Count);
			foreach (CovertMissionData covertMissionData in this.ActiveMissions)
			{
				covertMissionData.Save(w);
			}
			w.Write(this.RecentSpies.Count);
			foreach (int num2 in this.RecentSpies)
			{
				w.Write(num2);
			}
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.Funds.Load(r, SaveVersion);
			int num = r.ReadInt32();
			this.AgentIDs = new List<int>();
			for (int i = 0; i < num; i++)
			{
				this.AgentIDs.Add(r.ReadInt32());
			}
			num = r.ReadInt32();
			this.ActiveMissions = new List<CovertMissionData>();
			for (int j = 0; j < num; j++)
			{
				this.ActiveMissions.Add(new CovertMissionData(this.AI.Game, r, SaveVersion));
			}
			num = r.ReadInt32();
			this.RecentSpies = new List<int>();
			for (int k = 0; k < num; k++)
			{
				this.RecentSpies.Add(r.ReadInt32());
			}
		}

		public void Update()
		{
			this.AI.Log("");
			this.AI.Log("Espionage Manager Updating");
			foreach (CovertMissionData covertMissionData in this.ActiveMissions.ToList<CovertMissionData>())
			{
				covertMissionData.TurnsLeft--;
				if (covertMissionData.TurnsLeft == 0)
				{
					WorkingAgent workingAgent = this.AI.Game.AllAgents[covertMissionData.ActiveAgentID];
					if (workingAgent.CurrentMode != AgentModes.Idle)
					{
						workingAgent.Recall();
					}
					this.ActiveMissions.Remove(covertMissionData);
				}
			}
			List<WorkingAgent> list = new List<WorkingAgent>();
			using (IEnumerator<WorkingAgent> enumerator2 = this.Agents.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					WorkingAgent Agent = enumerator2.Current;
					if (this.ActiveMissions.Count((CovertMissionData x) => x.ActiveAgentID == Agent.ID) <= 0 && (Agent.CurrentMode != AgentModes.Idle || Agent.TurnsLeft <= 0))
					{
						list.Add(Agent);
					}
				}
			}
			this.StrongerRealms = new List<int>();
			foreach (WorkingRealm workingRealm in this.AI.Game.AllRealms.Values)
			{
				if (this.AI.WarManager.DefeatIsLikely(workingRealm, true))
				{
					this.StrongerRealms.Add(workingRealm.ID);
				}
			}
			foreach (WorkingAgent workingAgent2 in list)
			{
				List<CovertMissionData> missionList = this.GetMissionList(workingAgent2);
				if (missionList.Count == 0)
				{
					break;
				}
				int num = missionList.Sum((CovertMissionData x) => x.Weight);
				int num2 = this.RNG.Next(num);
				int num3 = 0;
				for (int i = 0; i < missionList.Count; i++)
				{
					num3 += missionList[i].Weight;
					if (num3 >= num2)
					{
						this.ActiveMissions.Add(missionList[i]);
						missionList[i].Start(this.RNG.Next(5) + 8);
						this.AI.Log("  Agent " + workingAgent2.ID + " on mission:");
						this.AI.Log("    " + missionList[i].GetDescription());
						break;
					}
				}
			}
		}

		private List<CovertMissionData> GetMissionList(WorkingAgent Agent)
		{
			List<CovertMissionData> list = new List<CovertMissionData>();
			List<WorkingRealm> list2 = this.AI.Game.AllRealms.Values.Where((WorkingRealm x) => x != this.AI.Game.RebelRealm && x.DiplomacyManager.GetDisposition(this.AI.Realm) <= -15f).ToList<WorkingRealm>();
			int num;
			foreach (WorkingRealm workingRealm in list2)
			{
				if (!workingRealm.RealmIsDead && workingRealm.DiplomacyManager.GetRelation(this.AI.Realm) != RelationStates.NAP && workingRealm.DiplomacyManager.GetRelation(this.AI.Realm) != RelationStates.Defence && workingRealm.DiplomacyManager.GetRelation(this.AI.Realm) != RelationStates.Alliance)
				{
					num = 5;
					if (this.StrongerRealms.Contains(workingRealm.ID))
					{
						num = 10;
					}
					CovertMissionData covertMissionData = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.InciteRebellion, workingRealm.ID, workingRealm.ID, num);
					list.Add(covertMissionData);
				}
			}
			List<WorkingRealm> list3 = this.AI.Game.AllRealms.Values.Where((WorkingRealm x) => x != this.AI.Game.RebelRealm && x.DiplomacyManager.GetDisposition(this.AI.Realm) <= -10f).ToList<WorkingRealm>();
			foreach (WorkingRealm workingRealm2 in list3)
			{
				if (!workingRealm2.RealmIsDead && workingRealm2.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Peace && this.StrongerRealms.Contains(workingRealm2.ID) && this.AI.WarManager.WarLimitReached())
				{
					CovertMissionData covertMissionData2 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.AvoidWar, workingRealm2.ID, workingRealm2.ID, 5);
					list.Add(covertMissionData2);
				}
			}
			int num2 = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => x != this.AI.Game.RebelRealm && !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.NAP);
			int num3 = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => x != this.AI.Game.RebelRealm && !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Defence);
			int num4 = this.AI.Game.AllRealms.Values.Count((WorkingRealm x) => x != this.AI.Game.RebelRealm && !x.RealmIsDead && x.DiplomacyManager.GetRelation(this.AI.Realm) == RelationStates.Alliance);
			int num5 = num2 + num3 + num4;
			if (num5 < 6)
			{
				foreach (WorkingRealm workingRealm3 in this.AI.Game.AllRealms.Values)
				{
					if (workingRealm3 != this.AI.Game.RebelRealm && !workingRealm3.RealmIsDead)
					{
						if (num2 < 3 && this.AI.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm3, TreatyTypes.NonAggression) && this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm3) > -5f)
						{
							CovertMissionData covertMissionData3 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.PushForTreaty, workingRealm3.ID, workingRealm3.ID, 5);
							list.Add(covertMissionData3);
						}
						int distance = this.AI.RelationsManager.GetDistance(this.AI.Realm, workingRealm3);
						if (num3 < 3 && this.AI.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm3, TreatyTypes.MutualDefence) && this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm3) > 10f && distance < 3)
						{
							CovertMissionData covertMissionData4 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.PushForTreaty, workingRealm3.ID, workingRealm3.ID, 5);
							list.Add(covertMissionData4);
						}
						if (num4 < 3 && this.AI.Game.AllianceController.TreatyIsPossible(this.AI.Realm, workingRealm3, TreatyTypes.Alliance) && this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm3) > 20f && distance < 3)
						{
							CovertMissionData covertMissionData5 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.PushForTreaty, workingRealm3.ID, workingRealm3.ID, 5);
							list.Add(covertMissionData5);
						}
					}
				}
			}
			foreach (WorkingRealm workingRealm4 in this.AI.Game.AllRealms.Values)
			{
				if (workingRealm4 != this.AI.Game.RebelRealm && !workingRealm4.RealmIsDead)
				{
					foreach (WorkingRealm workingRealm5 in this.AI.Realm.Allies)
					{
						float disposition = workingRealm4.DiplomacyManager.GetDisposition(workingRealm5);
						if (disposition >= -10f && disposition <= 10f)
						{
							CovertMissionData covertMissionData6 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.StrengthenAlliance, workingRealm5.ID, workingRealm4.ID, 5);
							list.Add(covertMissionData6);
						}
					}
				}
			}
			foreach (WorkingRealm workingRealm6 in this.AI.Game.AllRealms.Values)
			{
				if (workingRealm6 != this.AI.Game.RebelRealm && !workingRealm6.RealmIsDead)
				{
					foreach (WorkingRealm workingRealm7 in this.AI.Realm.Enemies)
					{
						float disposition2 = workingRealm6.DiplomacyManager.GetDisposition(workingRealm7);
						if (disposition2 >= -10f && disposition2 <= 10f)
						{
							CovertMissionData covertMissionData7 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.DamageAlliance, workingRealm7.ID, workingRealm6.ID, 5);
							list.Add(covertMissionData7);
						}
					}
				}
			}
			foreach (WorkingRealm workingRealm8 in this.AI.Game.AllRealms.Values)
			{
				if (workingRealm8 != this.AI.Game.RebelRealm && !workingRealm8.RealmIsDead)
				{
					foreach (WorkingRealm workingRealm9 in this.AI.Game.AllRealms.Values.Where((WorkingRealm x) => this.AI.Realm.DiplomacyManager.GetDisposition(x) < -10f))
					{
						float disposition3 = workingRealm8.DiplomacyManager.GetDisposition(workingRealm9);
						if (disposition3 >= -10f && disposition3 <= 10f)
						{
							CovertMissionData covertMissionData8 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.DamageAlliance, workingRealm9.ID, workingRealm8.ID, 5);
							list.Add(covertMissionData8);
						}
					}
				}
			}
			foreach (WorkingRealm workingRealm10 in this.AI.Game.AllRealms.Values)
			{
				if (workingRealm10 != this.AI.Game.RebelRealm && !workingRealm10.RealmIsDead && this.AI.Realm.DiplomacyManager.GetDisposition(workingRealm10) <= 0f && this.RNG.Next(100) >= 75)
				{
					CovertMissionData covertMissionData9 = new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.Spy, workingRealm10.ID, workingRealm10.ID, 5);
					list.Add(covertMissionData9);
				}
			}
			num = 25;
			if (this.ActiveMissions.Count((CovertMissionData x) => x.MissionType == CovertMissionTypes.CounterSpy) > 0)
			{
				num = 1;
			}
			list.Add(new CovertMissionData(this.AI.Game, Agent.ID, CovertMissionTypes.CounterSpy, this.AI.Realm.ID, this.AI.Realm.ID, num));
			return list;
		}

		public void AssignAgent(WorkingAgent Agent)
		{
			this.AgentIDs.Add(Agent.ID);
		}

		public WorkingAgent GetFreeAgent()
		{
			return this.Agents.FirstOrDefault((WorkingAgent x) => x.CurrentMode == AgentModes.Idle && x.HostRealm == x.OwnerRealm && x.TurnsLeft == 0);
		}

		internal void RespondToEspionage(WorkingAgent Agent)
		{
			WorkingRealm ownerRealm = Agent.OwnerRealm;
			float disposition = this.AI.Realm.DiplomacyManager.GetDisposition(ownerRealm);
			if (disposition < -10f)
			{
				this.WarnAboutSpying(Agent, ownerRealm);
				return;
			}
			if (disposition < 0f && this.RNG.Next(100) < 75)
			{
				this.WarnAboutSpying(Agent, ownerRealm);
				return;
			}
			if (disposition < 10f && this.RNG.Next(100) < 25)
			{
				this.WarnAboutSpying(Agent, ownerRealm);
				return;
			}
			this.ForgiveSpying(ownerRealm);
		}

		private void ForgiveSpying(WorkingRealm Realm)
		{
			if (Realm.AIPlayer == null)
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.MsgType = MessageType.GenericInfo;
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.CaptionText = GameText.CreateLocalised("MSG_SPYFAIL_TITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("MSG_SPYFAIL_IGNORE", new object[0]);
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
				this.AI.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
		}

		private void WarnAboutSpying(WorkingAgent Agent, WorkingRealm Realm)
		{
			this.RecentSpies.Add(Realm.ID);
			if (Realm.AIPlayer == null)
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				messageBoxData.MsgType = MessageType.GenericInfo;
				messageBoxData.DisplayType = MessageBoxType.Info;
				messageBoxData.CaptionText = GameText.CreateLocalised("MSG_SPYFAIL_TITLE", new object[0]);
				messageBoxData.MessageText = GameText.CreateLocalised("MSG_SPYFAIL_WARN", new object[0]);
				messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(this.AI.Realm.DisplayName, new object[0]));
				this.AI.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
			}
			this.AI.Realm.DiplomacyManager.TriggerEvent(Realm, "SpyCaught");
			Realm.DiplomacyManager.TriggerEvent(this.AI.Realm, "SpyCaught");
			Agent.Recall();
			if (Agent.OwnerRealm.AIPlayer != null)
			{
				Agent.OwnerRealm.AIPlayer.EspionageManager.CancelAgentMission(Agent);
			}
		}

		internal void CancelAgentMission(WorkingAgent Agent)
		{
			this.ActiveMissions.RemoveAll((CovertMissionData x) => x.ActiveAgentID == Agent.ID);
		}

		public AIPlayer AI;

		public AIFundData Funds;

		public List<int> AgentIDs;

		public List<CovertMissionData> ActiveMissions;

		public List<int> RecentSpies;

		public Random RNG;

		private List<int> StrongerRealms;
	}
}
