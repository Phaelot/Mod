using System;
using System.IO;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;

namespace SovereigntyTK.AI.V2
{
	public class CovertMissionData
	{
		public CovertMissionData(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.ActiveAgentID = r.ReadInt32();
			this.MissionType = (CovertMissionTypes)r.ReadInt16();
			this.TargetRealmID = r.ReadInt32();
			this.HostRealmID = r.ReadInt32();
			this.Weight = r.ReadInt32();
			this.TurnsLeft = r.ReadInt32();
		}

		public CovertMissionData(SovereigntyGame Game, int AgentID, CovertMissionTypes MissionType, int TargetID, int HostID, int Weight)
		{
			this.Game = Game;
			this.ActiveAgentID = AgentID;
			this.MissionType = MissionType;
			this.TargetRealmID = TargetID;
			this.HostRealmID = HostID;
			this.Weight = Weight;
		}

		public void Start(int MissionLength)
		{
			this.TurnsLeft = MissionLength;
			WorkingAgent workingAgent = this.Game.AllAgents[this.ActiveAgentID];
			WorkingRealm workingRealm = this.Game.AllRealms[this.HostRealmID];
			WorkingRealm workingRealm2 = this.Game.AllRealms[this.TargetRealmID];
			switch (this.MissionType)
			{
			case CovertMissionTypes.AvoidWar:
				workingAgent.Send(workingRealm, workingRealm2, AgentModes.ImproveRelations);
				return;
			case CovertMissionTypes.PushForTreaty:
				workingAgent.Send(workingRealm, workingRealm2, AgentModes.ImproveRelations);
				return;
			case CovertMissionTypes.StrengthenAlliance:
				workingAgent.Send(workingRealm, workingRealm2, AgentModes.ImproveForeignRelations);
				break;
			case CovertMissionTypes.DamageAlliance:
				workingAgent.Send(workingRealm, workingRealm2, AgentModes.DamageForeignRelations);
				return;
			case CovertMissionTypes.DamageRelations:
			case CovertMissionTypes.InciteRebellion:
			case CovertMissionTypes.CounterSpy:
				break;
			case CovertMissionTypes.Spy:
				workingAgent.Send(workingRealm, workingRealm2, AgentModes.Military);
				return;
			default:
				return;
			}
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.ActiveAgentID);
			w.Write((short)this.MissionType);
			w.Write(this.TargetRealmID);
			w.Write(this.HostRealmID);
			w.Write(this.Weight);
			w.Write(this.TurnsLeft);
		}

		internal string GetDescription()
		{
			string text = "";
			WorkingRealm workingRealm = this.Game.AllRealms[this.HostRealmID];
			WorkingRealm workingRealm2 = this.Game.AllRealms[this.TargetRealmID];
			switch (this.MissionType)
			{
			case CovertMissionTypes.AvoidWar:
				return "Avoid war with " + workingRealm2.Name;
			case CovertMissionTypes.PushForTreaty:
				return "Improve relations with " + workingRealm2.Name;
			case CovertMissionTypes.StrengthenAlliance:
			{
				string text2 = text;
				return string.Concat(new string[] { text2, "Improve relations between ", workingRealm.Name, " and ", workingRealm2.Name });
			}
			case CovertMissionTypes.DamageAlliance:
				return "Damage relations between " + workingRealm.Name + " and " + workingRealm2.Name;
			case CovertMissionTypes.Spy:
				return text + "Spy on " + workingRealm2.Name;
			case CovertMissionTypes.CounterSpy:
				return "Counter Spy";
			}
			return "Unknown mission type";
		}

		public int ActiveAgentID;

		public CovertMissionTypes MissionType;

		public int TargetRealmID;

		public int HostRealmID;

		public int Weight;

		public int TurnsLeft;

		public SovereigntyGame Game;
	}
}
