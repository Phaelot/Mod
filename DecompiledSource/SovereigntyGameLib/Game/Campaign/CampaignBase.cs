using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.Campaign
{
	public abstract class CampaignBase
	{
		protected abstract void SaveData(BinaryWriter w);

		protected abstract void LoadData(BinaryReader r, int SaveVersion);

		protected abstract bool GetCampaignAvailability(RealmData Realm);

		protected abstract void CampaignStarted();

		protected abstract void CampaignDisposed();

		protected abstract void CampaignRestored();

		protected abstract void CreateObjectives();

		protected abstract void CampaignInit();

		protected abstract void HandleMessageClosed(MessageBoxData Msg, MessageCloseType CloseType);

		protected abstract void HandleObjectiveStatusChanged(Objective Obj, ObjectiveStatuses Status);

		public CampaignBase(Sovereignty Game, string ID, int Order, string RealmName)
		{
			this.Game = Game;
			this.ID = ID;
			this.CampaignOrder = Order;
			this.Objectives = new List<Objective>();
			this.Plugins = new List<CampaignPlugin>();
			this.RNG = new Random();
		}

		public void InitCampaign()
		{
			this.CampaignInit();
		}

		protected CampaignPlugin GetPluginOfType(string PluginType)
		{
			return this.Plugins.FirstOrDefault((CampaignPlugin x) => x.GetType().Name == PluginType);
		}

		protected void SetCampaignVictory(bool AllowContinue = true)
		{
			if (this.CompleteVictory || !AllowContinue)
			{
				this.Game.CurrentGame.HandleGameVictory();
				return;
			}
			this.CompleteVictory = true;
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.CaptionText = GameText.CreateLocalised("MSG_CONTINUE_TITLE", new object[0]);
			messageBoxData.MessageText = GameText.CreateLocalised("MSG_CONTINUE_TEXT", new object[0]);
			messageBoxData.YesText = GameText.CreateLocalised("MSG_CONTINUE_YES", new object[0]);
			messageBoxData.NoText = GameText.CreateLocalised("MSG_CONTINUE_NO", new object[0]);
			messageBoxData.MsgType = MessageType.Campaign;
			messageBoxData.DisplayType = MessageBoxType.YesNo;
			messageBoxData.CustomData = "CONTINUE";
			this.Game.MessageHandler.ShowMessage(messageBoxData);
			this.Game.FireEvent("CampaignVictory", new object[0]);
		}

		protected void SetCampaignDefeat()
		{
			this.Game.CurrentGame.HandleGameDefeat();
		}

		public void BeginCampaign()
		{
			this.FinalObjective = new ObjectiveCounter(this.Game, this.Game.CurrentGame.AllProvinces.Count, "ProvinceOwnerChanged", (object[] Params) => this.Game.CurrentGame.PlayerRealm.Provinces.Count);
			this.FinalObjective.SetText("OBJ_FINAL_TITLE", "OBJ_FINAL_TEXT");
			this.AddObjective(this.FinalObjective, false, false);
			this.Game.CurrentGame.OnProvinceOwnerChanged += this.Game_OnProvinceOwnerChanged;
			this.CreateObjectives();
			this.CampaignStarted();
		}

		private void Game_OnProvinceOwnerChanged(WorkingProvince Province, WorkingRealm OldRealm, WorkingRealm Realm)
		{
			if (this.Game.CurrentGame.PlayerRealm.RealmIsDead)
			{
				this.SetCampaignDefeat();
			}
		}

		public void RestoreCampaign()
		{
			this.Game.CurrentGame.OnProvinceOwnerChanged += this.Game_OnProvinceOwnerChanged;
			this.Game.FireEvent("ObjectivesUpdated", new object[0]);
			this.CampaignRestored();
		}

		public void Dispose()
		{
			this.CampaignDisposed();
			if (this.Objectives != null)
			{
				foreach (Objective objective in this.Objectives)
				{
					if (objective != null)
					{
						objective.Disable();
					}
				}
				this.Objectives.Clear();
			}
			if (this.Plugins != null)
			{
				foreach (CampaignPlugin campaignPlugin in this.Plugins)
				{
					if (campaignPlugin != null)
					{
						campaignPlugin.Dispose();
					}
				}
				this.Plugins.Clear();
			}
			if (this.Game != null && this.Game.CurrentGame != null)
			{
				this.Game.CurrentGame.OnProvinceOwnerChanged -= this.Game_OnProvinceOwnerChanged;
			}
			this.Game = null;
		}

		public bool CampaignAvailable(RealmData Realm)
		{
			return this.GetCampaignAvailability(Realm);
		}

		protected bool ObjectivesCompleted(params Objective[] Objectives)
		{
			foreach (Objective objective in Objectives)
			{
				if (objective.State != ObjectiveState.Complete)
				{
					return false;
				}
			}
			return true;
		}

		public List<GameText> GetDescription(RealmData Realm)
		{
			List<GameText> list = new List<GameText>();
			list.Add(this.DescriptionText);
			list.Add(GameText.CreateLocalised("OBJECTIVES_TITLE", new object[0]));
			foreach (GameText gameText in this.DescriptionObjectives)
			{
				GameText gameText2 = GameText.CreateLocalised("FORMAT_NEWLINETEXT", new object[0]);
				gameText2.AddChildText(gameText);
				list.Add(gameText2);
			}
			return list;
		}

		public void HandleMessageYesClick(MessageBoxData Msg)
		{
			if (Msg.CustomData == "CONTINUE")
			{
				foreach (Objective objective in this.Objectives.ToList<Objective>())
				{
					this.DeactivateObjective(objective);
				}
				this.ActivateObjective(this.FinalObjective);
				return;
			}
			this.HandleMessageClosed(Msg, MessageCloseType.Yes);
		}

		public void HandleMessageNoClick(MessageBoxData Msg)
		{
			if (Msg.CustomData == "CONTINUE")
			{
				this.Game.CurrentGame.HandleGameVictory();
				return;
			}
			this.HandleMessageClosed(Msg, MessageCloseType.No);
		}

		public void HandleMessageCancelClick(MessageBoxData Msg)
		{
			this.HandleMessageClosed(Msg, MessageCloseType.Close);
		}

		public void Save(BinaryWriter w)
		{
			w.Write(this.Objectives.Count);
			foreach (Objective objective in this.Objectives)
			{
				objective.Save(w);
			}
			w.Write(this.Plugins.Count);
			foreach (CampaignPlugin campaignPlugin in this.Plugins)
			{
				w.Write(campaignPlugin.GetType().Name);
				int num = (int)w.BaseStream.Position;
				w.Write(0);
				int num2 = campaignPlugin.Save(w);
				w.Seek(num, SeekOrigin.Begin);
				w.Write(num2);
				w.Seek(num2, SeekOrigin.Current);
			}
			this.SaveData(w);
		}

		public CampaignPlugin GetPlugin(string TypeName)
		{
			Type type = this.Game.Utilities.ScriptManager.CampaignAssembly.GetTypes().FirstOrDefault((Type t) => t.IsSubclassOf(typeof(CampaignPlugin)) && t.Name == TypeName);
			if (type == null)
			{
				throw new Exception("Plugin " + TypeName + " does not exist");
			}
			return Activator.CreateInstance(type, new object[] { this.Game, true }) as CampaignPlugin;
		}

		public void Load(BinaryReader r, int SaveVersion)
		{
			try
			{
				this.FinalObjective = new ObjectiveCounter(this.Game, this.Game.CurrentGame.AllProvinces.Count, "ProvinceOwnerChanged", (object[] Params) => this.Game.CurrentGame.PlayerRealm.Provinces.Count);
				this.FinalObjective.SetText("OBJ_FINAL_TITLE", "OBJ_FINAL_TEXT");
				this.AddObjective(this.FinalObjective, false, false);
				this.CreateObjectives();
				int num = r.ReadInt32();
				for (int i = 0; i < num; i++)
				{
					this.Objectives[i].Load(r, SaveVersion);
				}
				num = r.ReadInt32();
				for (int j = 0; j < num; j++)
				{
					string PluginName = r.ReadString();
					int num2 = r.ReadInt32();
					Type type = this.Game.Utilities.ScriptManager.CampaignAssembly.GetTypes().FirstOrDefault((Type t) => t.IsSubclassOf(typeof(CampaignPlugin)) && t.Name == PluginName);
					if (type == null)
					{
						r.BaseStream.Seek((long)num2, SeekOrigin.Current);
					}
					else
					{
						CampaignPlugin campaignPlugin = Activator.CreateInstance(type, new object[] { this.Game, false }) as CampaignPlugin;
						this.AddPlugin(campaignPlugin);
						campaignPlugin.Load(r);
					}
				}
				this.LoadData(r, SaveVersion);
			}
			catch (Exception ex)
			{
				string text = "Error loading campaign: " + base.GetType().Name;
				text = text + "\r\n" + ex.Message + "\r\n";
				throw new Exception(text);
			}
		}

		public void PlayDecisionSound()
		{
			this.Game.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\timpani_hit_01.wav");
		}

		public void ShowTickerMessage()
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.DisplayType = MessageBoxType.Info;
			messageBoxData.MsgType = MessageType.GenericInfo;
			messageBoxData.CaptionText = GameText.CreateLocalised("MSG_NEWOBJECTIVES_TITLE", new object[0]);
			messageBoxData.MessageText = GameText.CreateLocalised("MSG_NEWOBJECTIVES_TEXT", new object[0]);
			this.Game.MessageHandler.ShowMessage(messageBoxData);
		}

		public void CreateNormalAlliance(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			Realm1.DiplomacyManager.GetDisposition(Realm2);
			Realm2.DiplomacyManager.GetDisposition(Realm1);
			this.Game.CurrentGame.AllianceController.FormTreaty(Realm1, Realm2, TreatyTypes.Alliance);
		}

		public void CreateDiplomaticModifer(string OriginRealm, string TargetRealm, string ModifierName)
		{
			WorkingRealm realm = this.Game.CurrentGame.GetRealm(OriginRealm);
			WorkingRealm realm2 = this.Game.CurrentGame.GetRealm(TargetRealm);
			realm.DiplomacyManager.TriggerEvent(realm2, ModifierName);
		}

		public void ModifyBaseDisposition(string OriginRealm, string TargetRealm, float Modifier)
		{
			WorkingRealm realm = this.Game.CurrentGame.GetRealm(OriginRealm);
			WorkingRealm realm2 = this.Game.CurrentGame.GetRealm(TargetRealm);
			realm.DiplomacyManager.AdjustBaseValue(realm2, Modifier);
		}

		public void CreateNormalWar(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			this.Game.CurrentGame.AllianceController.EstablishWar(Realm1, Realm2);
		}

		public void CreatePermenantWar(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			this.Game.CurrentGame.AllianceController.EstablishWar(Realm1, Realm2);
			Realm1.Restrictions.PermaWar.Add(Realm2.Name);
			Realm2.Restrictions.PermaWar.Add(Realm1.Name);
		}

		public void CreatePermenantAlliance(WorkingRealm Realm1, WorkingRealm Realm2)
		{
			this.CreateNormalAlliance(Realm1, Realm2);
			Realm1.Restrictions.PermaAllies.Add(Realm2.Name);
			Realm2.Restrictions.PermaAllies.Add(Realm1.Name);
		}

		public void AddPlugin(CampaignPlugin Plugin)
		{
			this.Plugins.Add(Plugin);
		}

		public void ActivateObjective(Objective Obj)
		{
			Obj.Visible = true;
			if (!Obj.Enabled)
			{
				Obj.Enable();
			}
			this.Game.FireEvent("ObjectivesUpdated", new object[0]);
		}

		public void DeactivateObjective(Objective Obj)
		{
			Obj.Visible = false;
			if (Obj.Enabled)
			{
				Obj.Disable();
			}
			this.Game.FireEvent("ObjectivesUpdated", new object[0]);
		}

		public void AddObjective(Objective Obj, bool Visible = true, bool Enable = true)
		{
			if (Enable)
			{
				Obj.Enable();
			}
			Obj.Visible = Visible;
			this.Objectives.Add(Obj);
			Obj.OnComplete = (ObjectiveDelegate)Delegate.Combine(Obj.OnComplete, new ObjectiveDelegate(this.Objective_Completed));
			Obj.OnFailed = (ObjectiveDelegate)Delegate.Combine(Obj.OnFailed, new ObjectiveDelegate(this.Objective_Failed));
			Obj.OnIncomplete = (ObjectiveDelegate)Delegate.Combine(Obj.OnIncomplete, new ObjectiveDelegate(this.Objective_Incomplete));
			this.Game.FireEvent("ObjectivesUpdated", new object[0]);
		}

		public void RemoveObjective(Objective Obj)
		{
			if (Obj == null)
			{
				return;
			}
			Obj.Disable();
			this.Objectives.Remove(Obj);
			Obj.OnComplete = (ObjectiveDelegate)Delegate.Remove(Obj.OnComplete, new ObjectiveDelegate(this.Objective_Completed));
			Obj.OnFailed = (ObjectiveDelegate)Delegate.Remove(Obj.OnFailed, new ObjectiveDelegate(this.Objective_Failed));
			Obj.OnIncomplete = (ObjectiveDelegate)Delegate.Remove(Obj.OnIncomplete, new ObjectiveDelegate(this.Objective_Incomplete));
			this.Game.FireEvent("ObjectivesUpdated", new object[0]);
		}

		private void Objective_Incomplete(Objective Obj)
		{
			this.HandleObjectiveStatusChanged(Obj, ObjectiveStatuses.Incomplete);
			if (Obj.Visible)
			{
				this.Game.FireEvent("OpenPanel", new object[] { "Objectives" });
				this.Game.FireEvent("ObjectiveStatusChanged", new object[]
				{
					Obj,
					ObjectiveStatuses.Incomplete
				});
			}
		}

		private void Objective_Failed(Objective Obj)
		{
			this.HandleObjectiveStatusChanged(Obj, ObjectiveStatuses.Failed);
			if (Obj.Visible)
			{
				this.Game.FireEvent("OpenPanel", new object[] { "Objectives" });
				this.Game.FireEvent("ObjectiveStatusChanged", new object[]
				{
					Obj,
					ObjectiveStatuses.Failed
				});
			}
		}

		private void Objective_Completed(Objective Obj)
		{
			this.HandleObjectiveStatusChanged(Obj, ObjectiveStatuses.Complete);
			this.Game.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\sharpie_check_box_05.wav");
			if (Obj.Visible)
			{
				this.Game.FireEvent("OpenPanel", new object[] { "Objectives" });
				this.Game.FireEvent("ObjectiveStatusChanged", new object[]
				{
					Obj,
					ObjectiveStatuses.Complete
				});
			}
			if (Obj == this.FinalObjective)
			{
				this.SetCampaignVictory(false);
			}
		}

		public IEnumerable<Objective> GetVisibleObjectives()
		{
			return this.Objectives.Where((Objective x) => x.Visible);
		}

		public Sovereignty Game;

		public string ID;

		public int CampaignOrder;

		public bool CompleteVictory;

		private List<Objective> Objectives;

		protected List<CampaignPlugin> Plugins;

		public GameText NameText;

		public GameText DescriptionText;

		public List<GameText> DescriptionObjectives;

		private Objective FinalObjective;

		public Random RNG;
	}
}
