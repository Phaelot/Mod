using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SovereigntyTK.Game.Data;
using SovereigntyTK.Game.Trade;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class WorkingAgent
	{
		public event AgentStatDelegate OnTravelTimeModifierRequested;

		public WorkingRealm OwnerRealm
		{
			get
			{
				return this.Game.AllRealms.Values.FirstOrDefault((WorkingRealm x) => x.ID == this.OwnerID);
			}
		}

		public WorkingRealm HostRealm
		{
			get
			{
				return this.Game.AllRealms.Values.FirstOrDefault((WorkingRealm x) => x.ID == this.HostID);
			}
		}

		public WorkingRealm TargetRealm
		{
			get
			{
				return this.Game.AllRealms.Values.FirstOrDefault((WorkingRealm x) => x.ID == this.TargetID);
			}
		}

		public WorkingAgent(int ID, SovereigntyGame Game, int OwnerID)
		{
			this.ID = ID;
			this.OwnerID = OwnerID;
			this.Game = Game;
			this.HostID = OwnerID;
			this.TargetID = -1;
			this.CurrentMode = AgentModes.Idle;
			this.RNG = new Random();
		}

		public WorkingAgent(SovereigntyGame Game, BinaryReader r, int SaveVersion)
		{
			this.Game = Game;
			this.ID = r.ReadInt32();
			this.OwnerID = r.ReadInt32();
			this.HostID = r.ReadInt32();
			this.TargetID = r.ReadInt32();
			this.TurnsTravelled = r.ReadInt32();
			this.TurnsLeft = r.ReadInt32();
			this.CurrentMode = (AgentModes)r.ReadInt16();
			if (r.ReadBoolean())
			{
				this.TradeOffer = new TradeOffer(Game, 0, 0);
				this.TradeOffer.Load(r, SaveVersion);
			}
			if (SaveVersion >= 58)
			{
				this.RebelProvinceID = r.ReadInt32();
			}
			this.RNG = new Random();
		}

		internal void Save(BinaryWriter w)
		{
			w.Write(this.ID);
			w.Write(this.OwnerID);
			w.Write(this.HostID);
			w.Write(this.TargetID);
			w.Write(this.TurnsTravelled);
			w.Write(this.TurnsLeft);
			w.Write((short)this.CurrentMode);
			if (this.TradeOffer == null)
			{
				w.Write(false);
			}
			else
			{
				w.Write(true);
				this.TradeOffer.Save(w);
			}
			w.Write(this.RebelProvinceID);
		}

		public void Update()
		{
			if (this.TurnsLeft > 0)
			{
				this.TurnsLeft--;
				this.TurnsTravelled++;
				if (this.TurnsLeft == 0)
				{
					this.HandleArrival();
				}
			}
			if (this.TurnsLeft < 0)
			{
				this.TurnsLeft = 1;
			}
			if (this.TurnsLeft == 0 && this.CurrentMode == AgentModes.Military)
			{
				this.UpdateMilitary();
			}
			if (this.TurnsLeft == 0 && this.CurrentMode == AgentModes.Incite)
			{
				this.UpdateRebellion();
			}
		}

		private void HandleArrival()
		{
			if (this.HostRealm == this.OwnerRealm)
			{
				this.CurrentMode = AgentModes.Idle;
				return;
			}
			switch (this.CurrentMode)
			{
			case AgentModes.ImproveRelations:
			{
				DiplomaticConditionData diplomaticConditionData = null;
				this.Game.GameCore.Data.DiplomaticConditions.TryGetValue("SpyBoost", out diplomaticConditionData);
				diplomaticConditionData = diplomaticConditionData.Clone();
				diplomaticConditionData.DispositionEffect *= 0.01f * (float)this.OwnerRealm.AgentEffectModifier.GetValue();
				this.HostRealm.DiplomacyManager.EnableCondition(this.OwnerRealm, diplomaticConditionData);
				return;
			}
			case AgentModes.ImproveForeignRelations:
			{
				DiplomaticConditionData diplomaticConditionData2 = null;
				this.Game.GameCore.Data.DiplomaticConditions.TryGetValue("SpyImproveActive", out diplomaticConditionData2);
				diplomaticConditionData2 = diplomaticConditionData2.Clone();
				diplomaticConditionData2.DispositionEffect *= 0.01f * (float)this.OwnerRealm.AgentEffectModifier.GetValue();
				this.HostRealm.DiplomacyManager.EnableCondition(this.TargetRealm, diplomaticConditionData2);
				return;
			}
			case AgentModes.DamageForeignRelations:
			{
				DiplomaticConditionData diplomaticConditionData3 = null;
				this.Game.GameCore.Data.DiplomaticConditions.TryGetValue("SpyDamageActive", out diplomaticConditionData3);
				diplomaticConditionData3 = diplomaticConditionData3.Clone();
				diplomaticConditionData3.DispositionEffect *= 0.01f * (float)this.OwnerRealm.AgentEffectModifier.GetValue();
				this.HostRealm.DiplomacyManager.EnableCondition(this.TargetRealm, diplomaticConditionData3);
				return;
			}
			case AgentModes.CarryTradeOffer:
			{
				this.HostRealm.Gold.Value += this.TradeOffer.SenderOffers.GoldLump;
				this.OwnerRealm.Gold.Value += this.TradeOffer.TargetOffers.GoldLump;
				foreach (KeyValuePair<ResourceData, int> keyValuePair in this.TradeOffer.SenderOffers.GetResourceLump())
				{
					this.HostRealm.GrantResource(keyValuePair.Key, keyValuePair.Value);
					this.Game.GameCore.FireEvent("TradeGoodGiven", new object[] { this.OwnerRealm, this.HostRealm, keyValuePair.Key, keyValuePair.Value });
				}
				if (this.HostRealm == this.Game.PlayerRealm)
				{
					this.Game.GameCore.FireEvent("TradeArrived", new object[]
					{
						this.TradeOffer.SenderRealmID,
						this.TradeOffer.SenderOffers
					});
				}
				WorkingRealm workingRealm = this.Game.AllRealms[this.TradeOffer.TargetRealmID];
				foreach (KeyValuePair<ResourceData, int> keyValuePair2 in this.TradeOffer.TargetOffers.GetResourceLump())
				{
					this.OwnerRealm.GrantResource(keyValuePair2.Key, keyValuePair2.Value);
					this.Game.GameCore.FireEvent("TradeGoodGiven", new object[] { workingRealm, this.OwnerRealm, keyValuePair2.Key, keyValuePair2.Value });
				}
				foreach (int num in this.TradeOffer.SenderOffers.GetPrisoners())
				{
					WorkingUnit workingUnit = this.Game.AllUnits[num];
					workingRealm.QueueUnit(workingUnit, true, false);
				}
				foreach (int num2 in this.TradeOffer.TargetOffers.GetPrisoners())
				{
					WorkingUnit workingUnit2 = this.Game.AllUnits[num2];
					this.OwnerRealm.QueueUnit(workingUnit2, true, false);
				}
				if (this.OwnerRealm == this.Game.PlayerRealm)
				{
					this.Game.GameCore.FireEvent("TradeArrived", new object[]
					{
						this.TradeOffer.TargetRealmID,
						this.TradeOffer.TargetOffers
					});
				}
				OngoingTrade ongoingTrade = this.HostRealm.TradeManager.InitTrade(this.TradeOffer.TargetOffers, this.OwnerRealm);
				OngoingTrade ongoingTrade2 = this.OwnerRealm.TradeManager.InitTrade(this.TradeOffer.SenderOffers, this.HostRealm);
				if (ongoingTrade != null && ongoingTrade2 != null)
				{
					ongoingTrade.AssociatedTradeID = ongoingTrade2.TradeID;
					ongoingTrade2.AssociatedTradeID = ongoingTrade.TradeID;
				}
				this.TradeOffer = null;
				this.Recall();
				return;
			}
			case AgentModes.Military:
				this.UpdateMilitary();
				return;
			case AgentModes.Idle:
				break;
			case AgentModes.Incite:
				this.UpdateRebellion();
				break;
			default:
				return;
			}
		}

		private bool ProvinceOKForUnrest(WorkingProvince Province)
		{
			if (Province.IsCapitol)
			{
				return false;
			}
			if (Province.Occupied)
			{
				return false;
			}
			if (this.RebelProvinceID != 0)
			{
				List<GameRegion> allConnectedRegions = Province.GetAllConnectedRegions();
				bool flag = false;
				foreach (GameRegion gameRegion in allConnectedRegions)
				{
					if (!(gameRegion is WorkingZone) && gameRegion.RegionID == this.RebelProvinceID)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		private void UpdateRebellion()
		{
			List<WorkingProvince> list = this.TargetRealm.Provinces.Where((WorkingProvince x) => this.ProvinceOKForUnrest(x)).ToList<WorkingProvince>();
			if (list.Count == 0)
			{
				if (this.OwnerRealm.AIPlayer == null)
				{
					MessageBoxData messageBoxData = new MessageBoxData();
					messageBoxData.MsgType = MessageType.GenericInfo;
					messageBoxData.DisplayType = MessageBoxType.Info;
					messageBoxData.CaptionText = GameText.CreateLocalised("MSG_SPYRETURN_TITLE", new object[0]);
					messageBoxData.MessageText = GameText.CreateLocalised("MSG_SPYRETURN_TEXT", new object[0]);
					messageBoxData.MessageText.AddChildText(GameText.CreateLocalised(this.TargetRealm.DisplayName, new object[0]));
					this.Game.GameCore.MessageHandler.ShowMessage(messageBoxData);
				}
				this.Recall();
				return;
			}
			WorkingProvince workingProvince = list[this.RNG.Next(list.Count)];
			WorkingProvince workingProvince2 = null;
			this.Game.AllProvinces.TryGetValue(this.RebelProvinceID, out workingProvince2);
			if (this.TargetRealm.AIPlayer == null)
			{
				if (workingProvince2 != null)
				{
					workingProvince2.ReduceInciteCount();
				}
				workingProvince.IncreaseInciteCount();
			}
			workingProvince.IncreaseInciteAmount();
			this.RebelProvinceID = workingProvince.ID;
			this.Game.GameCore.FireEvent("ProvinceIncited", new object[] { workingProvince });
		}

		private void UpdateMilitary()
		{
			int num = 10;
			num = (int)((float)num * 0.01f * (float)this.OwnerRealm.AgentEffectModifier.GetValue());
			foreach (WorkingStack workingStack in this.HostRealm.Stacks)
			{
				if (this.RNG.Next(100) < num)
				{
					workingStack.ForceScout = true;
				}
			}
		}

		public void Recall()
		{
			if (this.CurrentMode == AgentModes.Idle)
			{
				return;
			}
			this.HandleLeaving();
			this.HostID = this.OwnerID;
			this.TargetID = -1;
			this.TurnsLeft = this.TurnsTravelled;
			this.TurnsTravelled = 0;
			this.CurrentMode = AgentModes.Idle;
		}

		private void HandleLeaving()
		{
			if (this.HostRealm != null && this.TurnsLeft == 0)
			{
				switch (this.CurrentMode)
				{
				case AgentModes.ImproveRelations:
					this.HostRealm.DiplomacyManager.DisableCondition(this.OwnerRealm, "SpyBoost");
					return;
				case AgentModes.ImproveForeignRelations:
					this.HostRealm.DiplomacyManager.DisableCondition(this.TargetRealm, "SpyImproveActive");
					return;
				case AgentModes.DamageForeignRelations:
					this.HostRealm.DiplomacyManager.DisableCondition(this.TargetRealm, "SpyDamageActive");
					return;
				case AgentModes.CarryTradeOffer:
				case AgentModes.Military:
				case AgentModes.Idle:
					break;
				case AgentModes.Incite:
					this.RebelProvinceID = 0;
					break;
				default:
					return;
				}
			}
		}

		public void Send(WorkingRealm Host, WorkingRealm Target, AgentModes Mode)
		{
			this.HostID = Host.ID;
			if (Target == null)
			{
				this.TargetID = -1;
			}
			else
			{
				this.TargetID = Target.ID;
			}
			this.CurrentMode = Mode;
			WorkingProvince capitolProvince = this.OwnerRealm.CapitolProvince;
			WorkingProvince capitolProvince2 = Host.CapitolProvince;
			Path path = this.Game.PathManager.GetPath(capitolProvince.LandNode, capitolProvince2.LandNode, null, false, this.OwnerRealm, false);
			this.TurnsTravelled = 0;
			this.TurnsLeft = (int)(path.TotalMoveCost / 2f);
			this.TurnsLeft += this.OwnerRealm.AgentSpeedModifier.GetValue();
			this.TurnsLeft = Math.Max(this.TurnsLeft, 1);
			if (this.OnTravelTimeModifierRequested != null)
			{
				this.OnTravelTimeModifierRequested(this, ref this.TurnsLeft);
			}
			if (Mode == AgentModes.CarryTradeOffer)
			{
				this.OwnerRealm.SpendTradeGold(this.TradeOffer.SenderOffers.GoldLump);
				foreach (KeyValuePair<ResourceData, int> keyValuePair in this.TradeOffer.SenderOffers.GetResourceLump())
				{
					this.OwnerRealm.RemoveResource(keyValuePair.Key, keyValuePair.Value, false);
				}
				this.HostRealm.SpendTradeGold(this.TradeOffer.TargetOffers.GoldLump);
				foreach (KeyValuePair<ResourceData, int> keyValuePair2 in this.TradeOffer.TargetOffers.GetResourceLump())
				{
					this.HostRealm.RemoveResource(keyValuePair2.Key, keyValuePair2.Value, false);
				}
			}
			this.Game.GameCore.FireEvent("AgentSent", new object[] { this });
		}

		public GameText GetShortText()
		{
			switch (this.CurrentMode)
			{
			case AgentModes.ImproveRelations:
				return GameText.CreateLocalised("SPY_IMPROVE_SHORT", new object[0]);
			case AgentModes.ImproveForeignRelations:
				return GameText.CreateLocalised("SPY_IMPROVEFOREIGN_SHORT", new object[0]);
			case AgentModes.DamageForeignRelations:
				return GameText.CreateLocalised("SPY_DAMAGEFOREIGN_SHORT", new object[0]);
			case AgentModes.Military:
				return GameText.CreateLocalised("SPY_MILITARY_SHORT", new object[0]);
			}
			return GameText.CreateLocalised("", new object[0]);
		}

		public string GetLongText()
		{
			switch (this.CurrentMode)
			{
			case AgentModes.ImproveRelations:
				return "SPY_IMPROVE_LONG";
			case AgentModes.ImproveForeignRelations:
				return "SPY_IMPROVEFOREIGN_LONG";
			case AgentModes.DamageForeignRelations:
				return "SPY_DAMAGEFOREIGN_LONG";
			case AgentModes.Military:
				return "SPY_MILITARY_LONG";
			}
			return "";
		}

		internal void RecallImmediate()
		{
			this.HandleLeaving();
			this.HostID = this.OwnerID;
			this.TargetID = -1;
			this.TurnsLeft = 0;
			this.CurrentMode = AgentModes.Idle;
		}

		public int GetTurnsToHome()
		{
			if (this.CurrentMode == AgentModes.Idle)
			{
				return this.TurnsLeft;
			}
			if (this.CurrentMode == AgentModes.CarryTradeOffer)
			{
				return this.TurnsLeft + (this.TurnsTravelled + this.TurnsLeft);
			}
			return this.TurnsLeft;
		}

		public void ModifyTravelTime(int Modifier)
		{
			this.TurnsLeft += Modifier;
			if (this.TurnsLeft <= 1)
			{
				this.TurnsLeft = 1;
			}
		}

		public void Dispose()
		{
			this.OwnerID = -1;
			this.HostID = -1;
			this.TargetID = -1;
			this.TradeOffer = null;
		}

		public int ID;

		public int OwnerID;

		public int HostID;

		public int TargetID;

		public SovereigntyGame Game;

		public int TurnsTravelled;

		public int TurnsLeft;

		public int RebelProvinceID;

		public Random RNG;

		public AgentModes CurrentMode;

		public TradeOffer TradeOffer;
	}
}
