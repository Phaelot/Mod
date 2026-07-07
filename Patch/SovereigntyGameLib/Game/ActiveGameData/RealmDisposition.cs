using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Utility;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmDisposition
	{
		public event DispositionChangeDelegate OnDispositionChanged;

		public RealmDisposition(string Realm, string TargetRealm, float BaseValue)
		{
			this.Realm = Realm;
			this.TargetRealm = TargetRealm;
			this.BaseValue = BaseValue;
			this.ActiveEvents = new List<DiplomaticEvent>();
			this.ActiveConditions = new List<DiplomaticCondition>();
			this.LastValue = BaseValue;
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.BaseValue);
			w.Write(this.ActiveEvents.Count);
			foreach (DiplomaticEvent diplomaticEvent in this.ActiveEvents)
			{
				diplomaticEvent.Save(w);
			}
			w.Write(this.ActiveConditions.Count((DiplomaticCondition x) => !x.DoNotSave));
			foreach (DiplomaticCondition diplomaticCondition in this.ActiveConditions.Where((DiplomaticCondition x) => !x.DoNotSave))
			{
				diplomaticCondition.Save(w);
			}
		}

		internal void Load(BinaryReader r, int SaveVersion)
		{
			this.BaseValue = r.ReadSingle();
			int num = r.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				DiplomaticEvent diplomaticEvent = new DiplomaticEvent();
				diplomaticEvent.Load(r);
				this.ActiveEvents.Add(diplomaticEvent);
			}
			num = r.ReadInt32();
			for (int j = 0; j < num; j++)
			{
				DiplomaticCondition diplomaticCondition = new DiplomaticCondition();
				diplomaticCondition.Load(r);
				this.ActiveConditions.Add(diplomaticCondition);
			}
			this.UpdateLastValue();
		}

		private void UpdateLastValue()
		{
			this.LastValue = this.GetDisposition();
		}

		public float GetDisposition()
		{
			float num = this.BaseValue + this.ActiveEvents.Sum((DiplomaticEvent x) => x.CurrentValue) + this.ActiveConditions.Sum((DiplomaticCondition x) => x.CurrentValue);
			if (num < -50f)
			{
				num = -50f;
			}
			if (num > 50f)
			{
				num = 50f;
			}
			return num;
		}

		public void AddEvent(DiplomaticEventData Event)
		{
			DiplomaticEvent diplomaticEvent = this.ActiveEvents.SingleOrDefault((DiplomaticEvent x) => x.Data.EventName == Event.EventName);
			if (diplomaticEvent != null)
			{
				switch (diplomaticEvent.Data.StackMode)
				{
				case DiplomaticStackModes.Refresh:
					diplomaticEvent.CurrentValue = diplomaticEvent.Data.DispositionEffect;
					break;
				case DiplomaticStackModes.Stack:
					diplomaticEvent.CurrentValue += Event.DispositionEffect;
					break;
				}
			}
			else
			{
				this.ActiveEvents.Add(new DiplomaticEvent(Event));
			}
			this.CheckGaleni();
		}

		public void Update()
		{
			foreach (DiplomaticEvent diplomaticEvent in this.ActiveEvents.ToList<DiplomaticEvent>())
			{
				diplomaticEvent.Update();
				if (diplomaticEvent.IsExpired())
				{
					this.ActiveEvents.Remove(diplomaticEvent);
				}
			}
			foreach (DiplomaticCondition diplomaticCondition in this.ActiveConditions.ToList<DiplomaticCondition>())
			{
				diplomaticCondition.Update();
				if (diplomaticCondition.IsExpired())
				{
					this.ActiveConditions.Remove(diplomaticCondition);
				}
			}
			this.CheckGaleni();
		}

		internal void AddCondition(DiplomaticConditionData Condition)
		{
			DiplomaticCondition diplomaticCondition = this.ActiveConditions.SingleOrDefault((DiplomaticCondition x) => x.Data.ConditionName == Condition.ConditionName);
			if (diplomaticCondition != null)
			{
				diplomaticCondition.Enabled = true;
			}
			else
			{
				this.ActiveConditions.Add(new DiplomaticCondition(Condition));
			}
			this.CheckGaleni();
		}

		internal void RemoveCondition(DiplomaticConditionData Condition)
		{
			DiplomaticCondition diplomaticCondition = this.ActiveConditions.SingleOrDefault((DiplomaticCondition x) => x.Data.ConditionName == Condition.ConditionName);
			if (diplomaticCondition == null)
			{
				return;
			}
			diplomaticCondition.Enabled = false;
			this.CheckGaleni();
		}

		private void CheckGaleni()
		{
			if (this.TargetRealm == null)
			{
				return;
			}
			if (this.GetDisposition() != this.LastValue && this.OnDispositionChanged != null)
			{
				this.OnDispositionChanged(this.Realm, this.TargetRealm, this.LastValue, this.GetDisposition());
			}
			this.UpdateLastValue();
		}

		internal List<GameText> GetTooltip()
		{
			List<GameText> list = new List<GameText>();
			GameText gameText = GameText.CreateLocalised("EVENT_BASE", new object[] { this.BaseValue });
			list.Add(gameText);
			foreach (DiplomaticEvent diplomaticEvent in this.ActiveEvents.ToList<DiplomaticEvent>())
			{
				GameText gameText2 = GameText.CreateLocalised("FORMAT_EVENT", new object[]
				{
					diplomaticEvent.CurrentValue,
					diplomaticEvent.Data.DecayRate
				});
				gameText2.AddChildText(GameText.CreateLocalised(diplomaticEvent.Data.DisplayName, new object[0]));
				list.Add(gameText2);
			}
			foreach (DiplomaticCondition diplomaticCondition in this.ActiveConditions.ToList<DiplomaticCondition>())
			{
				float currentValue = diplomaticCondition.CurrentValue;
				float num;
				if (diplomaticCondition.Enabled)
				{
					if (!diplomaticCondition.IsMaxed())
					{
						num = diplomaticCondition.Data.DispositionEffect;
					}
					else
					{
						num = 0f;
					}
				}
				else
				{
					num = diplomaticCondition.Data.DecayRate;
				}
				GameText gameText3 = GameText.CreateLocalised("FORMAT_EVENT", new object[] { currentValue, num });
				gameText3.AddChildText(GameText.CreateLocalised(diplomaticCondition.Data.DisplayName, new object[0]));
				list.Add(gameText3);
			}
			return list;
		}

		public List<DiplomaticEvent> ActiveEvents;

		public List<DiplomaticCondition> ActiveConditions;

		public string TargetRealm;

		public string Realm;

		public float BaseValue;

		private float LastValue;
	}
}
