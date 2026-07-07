using System;
using System.Collections.Generic;
using System.IO;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Campaign
{
	public abstract class Objective
	{
		protected abstract void HandleEventFired(object[] Args);

		public ObjectiveState State
		{
			get
			{
				return this.m_State;
			}
		}

		public bool Enabled
		{
			get
			{
				return this.m_Enabled;
			}
		}

		public Objective(Sovereignty Game, string Event)
		{
			this.Game = Game;
			this.EventNames = new List<string>();
			this.EventNames.Add(Event);
		}

		public void SetUnlockUnit(string UnitName)
		{
			this.IconName = "Data\\Images\\HUD\\MainHUDNew\\objunit_armies.png";
			this.UnitName = UnitName;
			this.UnlocksUnit = true;
		}

		public void ForceCheck(params object[] Args)
		{
			this.HandleEventFired(Args);
		}

		public void SetCompleted()
		{
			if (this.m_State == ObjectiveState.Complete)
			{
				return;
			}
			this.m_State = ObjectiveState.Complete;
			if (this.UnlocksUnit)
			{
				this.IconName = null;
				this.Game.CurrentGame.UnlockPlayerUnit(this.UnitName);
				this.UnitName = null;
				this.UnlocksUnit = false;
			}
			if (this.OnComplete != null)
			{
				this.OnComplete(this);
			}
		}

		public void SetIncomplete()
		{
			if (this.m_State == ObjectiveState.Incomplete)
			{
				return;
			}
			this.m_State = ObjectiveState.Incomplete;
			if (this.OnIncomplete != null)
			{
				this.OnIncomplete(this);
			}
		}

		public void SetFailed()
		{
			if (this.m_State == ObjectiveState.Failed)
			{
				return;
			}
			this.m_State = ObjectiveState.Failed;
			if (this.OnFailed != null)
			{
				this.OnFailed(this);
			}
		}

		public void Enable()
		{
			if (this.m_Enabled)
			{
				return;
			}
			this.m_Enabled = true;
			foreach (string text in this.EventNames)
			{
				this.Game.RegisterEvent(new GenericDelegate(this.Game_OnScriptEvent), text);
			}
		}

		public void Disable()
		{
			if (!this.m_Enabled)
			{
				return;
			}
			this.m_Enabled = false;
			foreach (string text in this.EventNames)
			{
				this.Game.UnregisterEvent(new GenericDelegate(this.Game_OnScriptEvent), text);
			}
		}

		public void AddEvent(string EventName)
		{
			this.EventNames.Add(EventName);
		}

		private void Game_OnScriptEvent(string EventName, params object[] Args)
		{
			this.HandleEventFired(Args);
		}

		public virtual void SetText(string TitleText, string DescriptionText)
		{
			this.NameText = GameText.CreateLocalised(TitleText, new object[0]);
			this.DescriptionText = GameText.CreateLocalised(DescriptionText, new object[0]);
		}

		public virtual void SetText(GameText TitleText, GameText DescriptionText)
		{
			this.NameText = TitleText;
			this.DescriptionText = DescriptionText;
		}

		internal virtual void Save(BinaryWriter w)
		{
			w.Write((short)this.m_State);
			w.Write(this.m_Enabled);
			w.Write(this.Visible);
			w.Write(this.UnlocksUnit);
			if (this.UnlocksUnit)
			{
				w.Write(this.IconName);
				w.Write(this.UnitName);
			}
		}

		internal virtual void Load(BinaryReader r, int SaveVersion)
		{
			this.m_State = (ObjectiveState)r.ReadInt16();
			if (!r.ReadBoolean())
			{
				this.Disable();
			}
			else
			{
				this.Enable();
			}
			this.Visible = r.ReadBoolean();
			if (SaveVersion >= 31)
			{
				this.UnlocksUnit = r.ReadBoolean();
				if (this.UnlocksUnit)
				{
					this.IconName = r.ReadString();
					this.UnitName = r.ReadString();
					return;
				}
				this.IconName = null;
				this.UnitName = null;
			}
		}

		public void Reset()
		{
			this.m_State = ObjectiveState.Incomplete;
		}

		private ObjectiveState m_State;

		private bool m_Enabled;

		protected Sovereignty Game;

		private List<string> EventNames;

		public ObjectiveDelegate OnComplete;

		public ObjectiveDelegate OnIncomplete;

		public ObjectiveDelegate OnFailed;

		public GameText NameText;

		public GameText DescriptionText;

		public bool Visible;

		public int ID;

		public bool RecentCompletion;

		public bool UnlocksUnit;

		public string IconName;

		public string UnitName;

		public string TipTitle;

		public string TipText;

		public string TargetProvince;

		public string TargetRealm;
	}
}
