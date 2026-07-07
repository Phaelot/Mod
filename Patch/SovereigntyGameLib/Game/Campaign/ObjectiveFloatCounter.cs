using System;
using System.IO;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Campaign
{
	public class ObjectiveFloatCounter : Objective
	{
		public float CurrentValue
		{
			get
			{
				return this.m_Count;
			}
		}

		private float Count
		{
			get
			{
				return this.m_Count;
			}
			set
			{
				if (this.m_Count == value)
				{
					return;
				}
				if (this.m_Count < this.CounterTarget && value >= this.CounterTarget)
				{
					this.HandlePassedCounter();
				}
				if (this.m_Count >= this.CounterTarget && value < this.CounterTarget)
				{
					this.HandleDroppedCounter();
				}
				this.m_Count = value;
				this.DescriptionText.TextArgs[0] = this.m_Count;
				this.Game.FireEvent("ObjectiveTextUpdated", new object[] { this });
			}
		}

		public ObjectiveFloatCounter(Sovereignty Game, float Count, string Event, ObjectiveFloatCounterDelegate Action)
			: base(Game, Event)
		{
			this.CounterTarget = Count;
			this.Action = Action;
		}

		internal override void Load(BinaryReader r, int SaveVersion)
		{
			base.Load(r, SaveVersion);
			this.m_Count = (float)r.ReadInt32();
			this.DescriptionText.TextArgs[0] = this.m_Count;
		}

		internal override void Save(BinaryWriter w)
		{
			base.Save(w);
			w.Write(this.m_Count);
		}

		private void HandleDroppedCounter()
		{
			base.SetIncomplete();
		}

		private void HandlePassedCounter()
		{
			base.SetCompleted();
		}

		public void IncrementCounter()
		{
			this.Count += 1f;
		}

		public void SetCounter(float Count)
		{
			this.Count = Count;
		}

		protected override void HandleEventFired(object[] Args)
		{
			if (this.Action != null)
			{
				this.SetCounter(this.Action(Args));
			}
		}

		public override void SetText(string TitleText, string DescriptionText)
		{
			this.NameText = GameText.CreateLocalised(TitleText, new object[0]);
			this.DescriptionText = GameText.CreateLocalised("FORMAT_COUNTERFLOAT", new object[] { this.m_Count, this.CounterTarget });
			this.DescriptionText.AddChildText(GameText.CreateLocalised(DescriptionText, new object[0]));
		}

		private float m_Count;

		private float CounterTarget;

		private ObjectiveFloatCounterDelegate Action;
	}
}
