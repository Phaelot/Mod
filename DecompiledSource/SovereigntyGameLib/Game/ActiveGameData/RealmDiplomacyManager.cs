using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmDiplomacyManager
	{
		public RealmDiplomacyManager(string Realm, SovereigntyGame Game)
		{
			this.Realm = Realm;
			this.Game = Game;
			this.CurrentDispositions = new Dictionary<string, RealmDisposition>();
			this.CurrentRelations = new Dictionary<string, RelationStates>();
			this.RelationTimes = new Dictionary<string, int>();
			this.IgnoreDestroyPenalties = new List<string>();
		}

		internal void Dispose()
		{
			foreach (KeyValuePair<string, RealmDisposition> keyValuePair in this.CurrentDispositions)
			{
				keyValuePair.Value.OnDispositionChanged -= this.Data_OnDispositionChanged;
			}
			this.CurrentDispositions.Clear();
		}

		public List<GameText> GetTooltipList(WorkingRealm Target)
		{
			if (!this.CurrentDispositions.ContainsKey(Target.Name))
			{
				return null;
			}
			List<GameText> list = new List<GameText>();
			int num = (int)this.GetDisposition(Target);
			string text;
			if (num <= -36)
			{
				text = "DISP_HOSTILE";
			}
			else if (num <= -21)
			{
				text = "DISP_COLD";
			}
			else if (num <= -6)
			{
				text = "DISP_SUSPICIOUS";
			}
			else if (num <= 5)
			{
				text = "DISP_NEUTRAL";
			}
			else if (num <= 20)
			{
				text = "DISP_POLITE";
			}
			else if (num <= 35)
			{
				text = "DISP_CORDIAL";
			}
			else
			{
				text = "DISP_FRIENDLY";
			}
			WorkingRealm workingRealm = this.Game.AllRealms.Values.FirstOrDefault((WorkingRealm x) => x.Name == this.Realm);
			GameText gameText = GameText.CreateLocalised("FORMAT_DISPNAME", new object[0]);
			gameText.AddChildText(GameText.CreateLocalised(workingRealm.DisplayName, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(text, new object[0]));
			gameText.AddChildText(GameText.CreateLocalised(Target.DisplayName, new object[0]));
			list.Add(gameText);
			list.AddRange(this.CurrentDispositions[Target.Name].GetTooltip());
			return list;
		}

		public void PopulateRealms()
		{
			foreach (WorkingRealm workingRealm in this.Game.AllRealms.Values)
			{
				DiplomacyOffsetData diplomacyOffsetData = null;
				this.Game.GameCore.Data.DiplomaticOffsets.TryGetValue(this.Realm, out diplomacyOffsetData);
				if (diplomacyOffsetData == null)
				{
					this.AddRealm(workingRealm, 0f);
				}
				else if (!diplomacyOffsetData.NaturalOffsets.ContainsKey(workingRealm.Name))
				{
					this.AddRealm(workingRealm, 0f);
				}
				else
				{
					this.AddRealm(workingRealm, diplomacyOffsetData.NaturalOffsets[workingRealm.Name]);
				}
			}
		}

		public void AddRealm(WorkingRealm Realm, float BaseOffset)
		{
			RealmDisposition realmDisposition = new RealmDisposition(this.Realm, Realm.Name, BaseOffset);
			this.CurrentDispositions.Add(Realm.Name, realmDisposition);
			realmDisposition.OnDispositionChanged += this.Data_OnDispositionChanged;
			if (Realm.Name == "Rebels")
			{
				this.SetRelation(Realm, RelationStates.War);
				return;
			}
			this.SetRelation(Realm, RelationStates.Peace);
		}

		private void Data_OnDispositionChanged(string Realm, string TargetRealm, float OldValue, float NewValue)
		{
			this.Game.HandleDispositionChanged(Realm, TargetRealm, OldValue, NewValue);
		}

		public void TriggerEvent(WorkingRealm Target, DiplomaticEventData Event)
		{
			if (!this.CurrentDispositions.ContainsKey(Target.Name))
			{
				return;
			}
			if (Target.HasStatus("IgnoreDiplomacy", new object[] { Event }))
			{
				return;
			}
			this.CurrentDispositions[Target.Name].AddEvent(Event);
		}

		public void TriggerEvent(WorkingRealm Target, string EventName)
		{
			if (EventName == "None")
			{
				return;
			}
			DiplomaticEventData diplomaticEventData = null;
			this.Game.GameCore.Data.DiplomaticEvents.TryGetValue(EventName, out diplomaticEventData);
			if (diplomaticEventData == null)
			{
				throw new Exception("Diplomatic event named " + EventName + " does not exist.");
			}
			this.TriggerEvent(Target, diplomaticEventData);
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.CurrentDispositions.Count);
			foreach (KeyValuePair<string, RealmDisposition> keyValuePair in this.CurrentDispositions)
			{
				w.Write(keyValuePair.Key);
				keyValuePair.Value.Save(w);
			}
			w.Write(this.CurrentRelations.Count);
			foreach (KeyValuePair<string, RelationStates> keyValuePair2 in this.CurrentRelations)
			{
				w.Write(keyValuePair2.Key);
				w.Write((short)keyValuePair2.Value);
			}
			w.Write(this.RelationTimes.Count);
			foreach (KeyValuePair<string, int> keyValuePair3 in this.RelationTimes)
			{
				w.Write(keyValuePair3.Key);
				w.Write(keyValuePair3.Value);
			}
			w.Write(this.IgnoreDestroyPenalties.Count);
			foreach (string text in this.IgnoreDestroyPenalties)
			{
				w.Write(text);
			}
		}

		public void Load(BinaryReader r, int SaveVersion)
		{
			this.CurrentDispositions.Clear();
			this.CurrentRelations.Clear();
			this.RelationTimes.Clear();
			this.IgnoreDestroyPenalties.Clear();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string text = r.ReadString();
				RealmDisposition realmDisposition = new RealmDisposition(this.Realm, text, 0f);
				realmDisposition.Load(r, SaveVersion);
				this.CurrentDispositions.Add(text, realmDisposition);
				realmDisposition.OnDispositionChanged += this.Data_OnDispositionChanged;
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				this.CurrentRelations.Add(r.ReadString(), (RelationStates)r.ReadInt16());
			}
			num = r.ReadInt32();
			for (int k = 0; k < num; k++)
			{
				this.RelationTimes.Add(r.ReadString(), r.ReadInt32());
			}
			if (SaveVersion >= 50)
			{
				num = r.ReadInt32();
				for (int l = 0; l < num; l++)
				{
					this.IgnoreDestroyPenalties.Add(r.ReadString());
				}
			}
		}

		public float GetNaturalDisposition(WorkingRealm TargetRealm)
		{
			if (!this.CurrentDispositions.ContainsKey(TargetRealm.Name))
			{
				return 0f;
			}
			return this.CurrentDispositions[TargetRealm.Name].BaseValue;
		}

		public float GetDisposition(WorkingRealm TargetRealm)
		{
			if (!this.CurrentDispositions.ContainsKey(TargetRealm.Name))
			{
				return 0f;
			}
			return this.CurrentDispositions[TargetRealm.Name].GetDisposition();
		}

		public bool NamedConditionExists(string ConditionName)
		{
			DiplomaticConditionData diplomaticConditionData = null;
			this.Game.GameCore.Data.DiplomaticConditions.TryGetValue(ConditionName, out diplomaticConditionData);
			return diplomaticConditionData != null;
		}

		public DiplomaticConditionData GetNamedCondition(string ConditionName)
		{
			DiplomaticConditionData diplomaticConditionData = null;
			this.Game.GameCore.Data.DiplomaticConditions.TryGetValue(ConditionName, out diplomaticConditionData);
			if (diplomaticConditionData == null)
			{
				throw new Exception("Diplomatic condition named " + ConditionName + " does not exist.");
			}
			return diplomaticConditionData;
		}

		public void EnableCondition(WorkingRealm Target, DiplomaticConditionData Condition)
		{
			if (!this.CurrentDispositions.ContainsKey(Target.Name))
			{
				return;
			}
			if (Target.HasStatus("IgnoreDiplomacy", new object[] { Condition }))
			{
				return;
			}
			this.CurrentDispositions[Target.Name].AddCondition(Condition);
		}

		public void DisableCondition(WorkingRealm Target, DiplomaticConditionData Condition)
		{
			if (!this.CurrentDispositions.ContainsKey(Target.Name))
			{
				return;
			}
			this.CurrentDispositions[Target.Name].RemoveCondition(Condition);
		}

		public void EnableCondition(WorkingRealm Target, string ConditionName)
		{
			this.EnableCondition(Target, this.GetNamedCondition(ConditionName));
		}

		public void DisableCondition(WorkingRealm Target, string ConditionName)
		{
			this.DisableCondition(Target, this.GetNamedCondition(ConditionName));
		}

		public void AdjustBaseValue(WorkingRealm Target, float Value)
		{
			if (!this.CurrentDispositions.ContainsKey(Target.Name))
			{
				return;
			}
			this.CurrentDispositions[Target.Name].BaseValue += Value;
		}

		internal void UpdateDispositions()
		{
			foreach (KeyValuePair<string, RealmDisposition> keyValuePair in this.CurrentDispositions)
			{
				keyValuePair.Value.Update();
			}
		}

		public int GetRelationTime(WorkingRealm TargetRealm)
		{
			if (!this.RelationTimes.ContainsKey(TargetRealm.Name))
			{
				return 10;
			}
			return this.RelationTimes[TargetRealm.Name];
		}

		public bool HasTreaty(WorkingRealm TargetRealm)
		{
			return TargetRealm != null && this.CurrentRelations.ContainsKey(TargetRealm.Name) && (this.CurrentRelations[TargetRealm.Name] == RelationStates.Alliance || this.CurrentRelations[TargetRealm.Name] == RelationStates.Defence || this.CurrentRelations[TargetRealm.Name] == RelationStates.NAP);
		}

		public RelationStates GetRelation(WorkingRealm TargetRealm)
		{
			if (this.Realm == this.Game.RebelRealm.Name)
			{
				return RelationStates.War;
			}
			if (TargetRealm == this.Game.RebelRealm)
			{
				return RelationStates.War;
			}
			if (TargetRealm == null)
			{
				return RelationStates.War;
			}
			if (!this.CurrentRelations.ContainsKey(TargetRealm.Name))
			{
				return RelationStates.War;
			}
			return this.CurrentRelations[TargetRealm.Name];
		}

		internal void SetRelation(WorkingRealm TargetRealm, RelationStates Relation)
		{
			this.CurrentRelations[TargetRealm.Name] = Relation;
			this.RelationTimes[TargetRealm.Name] = 0;
		}

		internal void AgeRelations()
		{
			List<string> list = new List<string>();
			foreach (string text in this.CurrentRelations.Keys)
			{
				Dictionary<string, int> relationTimes;
				string text2;
				(relationTimes = this.RelationTimes)[text2 = text] = relationTimes[text2] + 1;
				if (this.CurrentRelations[text] == RelationStates.ForcedPeace && this.RelationTimes[text] >= 5)
				{
					list.Add(text);
				}
			}
			foreach (string text3 in list)
			{
				this.CurrentRelations[text3] = RelationStates.Peace;
			}
		}

		internal void SetBaseValue(WorkingRealm TargetRealm, int Value)
		{
			if (!this.CurrentDispositions.ContainsKey(TargetRealm.Name))
			{
				return;
			}
			this.CurrentDispositions[TargetRealm.Name].BaseValue = (float)Value;
		}

		public string Realm;

		public SovereigntyGame Game;

		public Dictionary<string, RealmDisposition> CurrentDispositions;

		public Dictionary<string, RelationStates> CurrentRelations;

		public Dictionary<string, int> RelationTimes;

		public List<string> IgnoreDestroyPenalties;
	}
}
