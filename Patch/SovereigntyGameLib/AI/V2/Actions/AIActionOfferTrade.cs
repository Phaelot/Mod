using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Trade;
using SovereigntyTK.UI;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.AI.V2.Actions
{
	public class AIActionOfferTrade : AIAction
	{
		public AIActionOfferTrade(AIActionManager Manager, SovereigntyGame Game)
			: base(Manager, Game)
		{
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleTradeAccepted), "PlayerAcceptedTrade");
			Game.GameCore.RegisterEvent(new GenericDelegate(this.HandleTradeRejected), "PlayerRejectedTrade");
		}

		private void HandleTradeRejected(string EventName, params object[] Args)
		{
			this.AI.Log("    Trade rejected");
			TradeOffer tradeOffer = Args[0] as TradeOffer;
			if (tradeOffer != this.Offer)
			{
				return;
			}
			this.State = AiActionStates.Finished;
		}

		private void HandleTradeAccepted(string EventName, params object[] Args)
		{
			this.AI.Log("    Trade accepted, sending agent");
			TradeOffer tradeOffer = Args[0] as TradeOffer;
			if (tradeOffer != this.Offer)
			{
				return;
			}
			WorkingAgent tradeAgent = this.AI.Realm.GetTradeAgent();
			tradeAgent.TradeOffer = tradeOffer;
			tradeAgent.Send(this.TargetRealm, null, AgentModes.CarryTradeOffer);
			WorkingRealm realm = this.AI.Realm;
			WorkingRealm targetRealm = this.TargetRealm;
			if (tradeOffer.SenderOffers.Treaty != TreatyTypes.None)
			{
				this.Game.AllianceController.FormTreaty(realm, targetRealm, tradeOffer.SenderOffers.Treaty);
			}
			foreach (int num in tradeOffer.SenderOffers.GetProvinces())
			{
				WorkingProvince workingProvince = this.Game.AllProvinces[num];
				this.Game.ChangeProvinceOwner(workingProvince, targetRealm);
				if (workingProvince.LandNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince.LandNode.CurrentStack);
				}
				if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince.HarbourNode.CurrentStack);
				}
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince });
			}
			foreach (int num2 in tradeOffer.TargetOffers.GetProvinces())
			{
				WorkingProvince workingProvince2 = this.Game.AllProvinces[num2];
				this.Game.ChangeProvinceOwner(workingProvince2, realm);
				if (workingProvince2.LandNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince2.LandNode.CurrentStack);
				}
				if (workingProvince2.HarbourNode != null && workingProvince2.HarbourNode.CurrentStack != null)
				{
					this.Game.WithdrawStack(workingProvince2.HarbourNode.CurrentStack);
				}
				this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince2 });
			}
			foreach (int num3 in tradeOffer.SenderOffers.GetPrisoners())
			{
				WorkingUnit workingUnit = this.Game.AllUnits[num3];
				realm.Prison.ReleasePrisoner(workingUnit);
			}
			foreach (int num4 in tradeOffer.TargetOffers.GetPrisoners())
			{
				WorkingUnit workingUnit2 = this.Game.AllUnits[num4];
				targetRealm.Prison.ReleasePrisoner(workingUnit2);
			}
			this.State = AiActionStates.Finished;
		}

		public override void Execute()
		{
			if (this.Game.GameCore.DebugMessagesEnabled)
			{
				this.Game.GameCore.FireEvent("TickerMessage", new object[]
				{
					new TickerMessage(GameText.CreateFromLiteral("AI " + this.AI.Realm.Name + " has offered trade to " + this.TargetRealm.Name), TickerMessageType.Debug, 1)
				});
			}
			if (this.TargetRealm.AIPlayer == null)
			{
				this.Game.GameCore.FireEvent("PlayerTradeOffer", new object[] { this.Offer });
				if (this.State != AiActionStates.Finished)
				{
					this.State = AiActionStates.Executing;
					return;
				}
			}
			else
			{
				this.AI.Log("    Trade accepted, sending agent");
				WorkingAgent tradeAgent = this.AI.Realm.GetTradeAgent();
				tradeAgent.TradeOffer = this.Offer;
				tradeAgent.Send(this.TargetRealm, null, AgentModes.CarryTradeOffer);
				WorkingRealm realm = this.AI.Realm;
				WorkingRealm targetRealm = this.TargetRealm;
				if (this.Offer.SenderOffers.Treaty != TreatyTypes.None)
				{
					this.Game.AllianceController.FormTreaty(realm, targetRealm, this.Offer.SenderOffers.Treaty);
				}
				foreach (int num in this.Offer.SenderOffers.GetProvinces())
				{
					WorkingProvince workingProvince = this.Game.AllProvinces[num];
					this.Game.ChangeProvinceOwner(workingProvince, targetRealm);
					if (workingProvince.LandNode.CurrentStack != null)
					{
						this.Game.WithdrawStack(workingProvince.LandNode.CurrentStack);
					}
					if (workingProvince.HarbourNode != null && workingProvince.HarbourNode.CurrentStack != null)
					{
						this.Game.WithdrawStack(workingProvince.HarbourNode.CurrentStack);
					}
					this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince });
				}
				foreach (int num2 in this.Offer.TargetOffers.GetProvinces())
				{
					WorkingProvince workingProvince2 = this.Game.AllProvinces[num2];
					this.Game.ChangeProvinceOwner(workingProvince2, realm);
					if (workingProvince2.LandNode.CurrentStack != null)
					{
						this.Game.WithdrawStack(workingProvince2.LandNode.CurrentStack);
					}
					if (workingProvince2.HarbourNode != null && workingProvince2.HarbourNode.CurrentStack != null)
					{
						this.Game.WithdrawStack(workingProvince2.HarbourNode.CurrentStack);
					}
					this.Game.GameCore.FireEvent("ProvinceOccupierChanged", new object[] { workingProvince2 });
				}
				this.State = AiActionStates.Finished;
			}
		}

		public override void Dispose()
		{
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleTradeAccepted), "PlayerAcceptedTrade");
			this.Game.GameCore.UnregisterEvent(new GenericDelegate(this.HandleTradeRejected), "PlayerRejectedTrade");
			base.Dispose();
		}

		public TradeOffer Offer;

		public WorkingRealm TargetRealm;
	}
}
