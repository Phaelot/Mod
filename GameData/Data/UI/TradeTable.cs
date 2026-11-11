using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Threading;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Game.Data;
using System.Linq;
using SovereigntyTK.Game;
using SovereigntyTK.Utility;
using SovereigntyTK.Game.Campaign;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Trade;
using SovereigntyTK.AI.V2;

namespace SovereigntyTK.UI
{
    public class TradeTable: ScriptBase
    {
        const int CATEGORY_MONEY = 1;
        const int CATEGORY_RESOURCE = 2;
        const int CATEGORY_PROVINCE = 3;
        const int CATEGORY_UNIT = 4;
        const int CATEGORY_TREATY = 5;

        Dictionary<int, bool> OurItemStatuses;
        Dictionary<int, bool> TheirItemStatuses;
        Dictionary<int, bool> OurOfferStatuses;
        Dictionary<int, bool> TheirOfferStatuses;

        WorkingRealm OtherRealm;
        TradeOffer CurrentTrade;

        bool m_AIOffer;

        bool IsAIOffer
        {
            get
            {
                return m_AIOffer;
            }
            set
            {
                m_AIOffer = value;
                UpdateVisibleControls();
            }
        }

        bool CounterOffering;

        ControlList<CategorisedListItem> OurItemsList;
        ControlList<CategorisedListItem> OurOffersList;
        ControlList<CategorisedListItem> TheirOffersList;
        ControlList<CategorisedListItem> TheirItemsList;

        public void FormLoaded(UIControl Control)
        {
            GameSystem.RegisterEvent(HandleAITradeOffer, "PlayerTradeOffer");
            GameSystem.RegisterEvent(HandlePlayerTradeOffer, "OfferTrade");
            GameSystem.RegisterEvent(HandlePlayerTreatyOffer, "OfferTreaty");
            GameSystem.RegisterEvent(HandlePlayerTradeOfferWithItems, "OfferTradeForItems");
            GameSystem.RegisterEvent(HandleCloseRequest, "CloseTradeTable");

            ControlImage BG = GameForm.GetControlByName<ControlImage>("PanelBG");

            switch (GameSystem.CurrentGame.PlayerRealm.Panel)
            {
                case SpellSchools.Death: BG.SetImageFile("Data\\Images\\HUD\\bg_death_square.png"); break;
                case SpellSchools.Illusion: BG.SetImageFile("Data\\Images\\HUD\\bg_illusion_square.png"); break;
                case SpellSchools.Nature: BG.SetImageFile("Data\\Images\\HUD\\bg_nature_square.png"); break;
                case SpellSchools.War: BG.SetImageFile("Data\\Images\\HUD\\bg_war_square.png"); break;
            }

            BG = GameForm.GetControlByName<ControlImage>("OurItemsBG");
            OurItemsList = new ControlList<CategorisedListItem>(GameSystem);
            BG.AddChild(OurItemsList);
            OurItemsList.SetBounds(0, 20, 230, 480, UIUnits.Pixel);
            OurItemsList.SetVisibleItems(24);
            OurItemsList.ManualUpdate = true;
            OurItemsList.SelectionEnabled = false;
            OurItemsList.HighlightEnabled = false;
            OurItemsList.OnListItemUpdated += OnCategorisedListItemUpdated;
            OurItemsList.OnItemClicked += OurItemsList_OnItemClicked;

            BG = GameForm.GetControlByName<ControlImage>("TheirItemsBG");
            TheirItemsList = new ControlList<CategorisedListItem>(GameSystem);
            BG.AddChild(TheirItemsList);
            TheirItemsList.SetBounds(0, 20, 230, 480, UIUnits.Pixel);
            TheirItemsList.SetVisibleItems(24);
            TheirItemsList.ManualUpdate = true;
            TheirItemsList.SelectionEnabled = false;
            TheirItemsList.HighlightEnabled = false;
            TheirItemsList.OnListItemUpdated += OnCategorisedListItemUpdated;
            TheirItemsList.OnItemClicked += TheirItemsList_OnItemClicked;

            BG = GameForm.GetControlByName<ControlImage>("OurOffersBG");
            OurOffersList = new ControlList<CategorisedListItem>(GameSystem);
            BG.AddChild(OurOffersList);
            OurOffersList.SetBounds(0, 20, 230, 230, UIUnits.Pixel);
            OurOffersList.SetVisibleItems(11);
            OurOffersList.ManualUpdate = true;
            OurOffersList.SelectionEnabled = false;
            OurOffersList.HighlightEnabled = false;
            OurOffersList.OnListItemUpdated += OnCategorisedListItemUpdated;
            OurOffersList.OnItemClicked += OurOffersList_OnItemClicked;

            BG = GameForm.GetControlByName<ControlImage>("TheirOffersBG");
            TheirOffersList = new ControlList<CategorisedListItem>(GameSystem);
            BG.AddChild(TheirOffersList);
            TheirOffersList.SetBounds(0, 20, 230, 230, UIUnits.Pixel);
            TheirOffersList.SetVisibleItems(11);
            TheirOffersList.ManualUpdate = true;
            TheirOffersList.SelectionEnabled = false;
            TheirOffersList.HighlightEnabled = false;
            TheirOffersList.OnListItemUpdated += OnCategorisedListItemUpdated;
            TheirOffersList.OnItemClicked += TheirOffersList_OnItemClicked;

            OurItemStatuses = new Dictionary<int, bool>();
            OurItemStatuses.Add(CATEGORY_MONEY, false);
            OurItemStatuses.Add(CATEGORY_PROVINCE, false);
            OurItemStatuses.Add(CATEGORY_RESOURCE, false);
            OurItemStatuses.Add(CATEGORY_TREATY, false);
            OurItemStatuses.Add(CATEGORY_UNIT, false);

            OurOfferStatuses = new Dictionary<int, bool>();
            OurOfferStatuses.Add(CATEGORY_MONEY, false);
            OurOfferStatuses.Add(CATEGORY_PROVINCE, false);
            OurOfferStatuses.Add(CATEGORY_RESOURCE, false);
            OurOfferStatuses.Add(CATEGORY_TREATY, false);
            OurOfferStatuses.Add(CATEGORY_UNIT, false);

            TheirItemStatuses = new Dictionary<int, bool>();
            TheirItemStatuses.Add(CATEGORY_MONEY, false);
            TheirItemStatuses.Add(CATEGORY_PROVINCE, false);
            TheirItemStatuses.Add(CATEGORY_RESOURCE, false);
            TheirItemStatuses.Add(CATEGORY_TREATY, false);
            TheirItemStatuses.Add(CATEGORY_UNIT, false);

            TheirOfferStatuses = new Dictionary<int, bool>();
            TheirOfferStatuses.Add(CATEGORY_MONEY, false);
            TheirOfferStatuses.Add(CATEGORY_PROVINCE, false);
            TheirOfferStatuses.Add(CATEGORY_RESOURCE, false);
            TheirOfferStatuses.Add(CATEGORY_TREATY, false);
            TheirOfferStatuses.Add(CATEGORY_UNIT, false);
        }

        private void HandleCloseRequest(string EventName, params object[] Args)
        {
            CloseButtonClick(null);
        }

        private void UpdateVisibleControls()
        {
            if (IsAIOffer)
            {
                GameForm.GetControlByName<ControlContainer>("AITradeButtons").Visible = true;
                GameForm.GetControlByName<ControlContainer>("PlayerTradeButtons").Visible = false;
                GameForm.GetControlByName<ControlImage>("TheirItemsBG").Visible = false;
                GameForm.GetControlByName<ControlImage>("OurItemsBG").Visible = false;
                GameForm.GetControlByName<ControlButton>("CloseButton").Visible = false;
                GameForm.GetControlByName<ControlImage>("AcceptanceBG").Visible = false;
            }
            else
            {
                GameForm.GetControlByName<ControlContainer>("AITradeButtons").Visible = false;
                GameForm.GetControlByName<ControlContainer>("PlayerTradeButtons").Visible = true;
                GameForm.GetControlByName<ControlImage>("TheirItemsBG").Visible = true;
                GameForm.GetControlByName<ControlImage>("OurItemsBG").Visible = true;
                GameForm.GetControlByName<ControlButton>("CloseButton").Visible = true;
                GameForm.GetControlByName<ControlImage>("AcceptanceBG").Visible = true;
            }
        }

        public void ClearButtonClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("ClearTradeTable") == false) return;

            //remove all items in the current trade
            //redraw all
            CurrentTrade.Clear();
            UpdateAll();
        }

        public void OfferButtonClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("TradeSend") == false) return;

            if (!CounterOffering)
            {
                WorkingAgent Agent = GameSystem.CurrentGame.PlayerRealm.Agents.FirstOrDefault(x => x.CurrentMode == AgentModes.Idle && x.HostRealm == x.OwnerRealm && x.TurnsLeft == 0);

                if (Agent == null)
                {
                    GameSystem.MessageHandler.ShowInfoMessage("MSG_TRADEAGENT_TITLE", "MSG_TRADEAGENT_TEXT");
                    return;
                }
            }

            //get the current feeling from the AI
            //get the accept/refuse state from the AI
            //show the appropriate message (actual send button will be in the message)
            //handle send trade event to close the form
            if (CurrentTrade.OfferIsEmpty())
            {
                GameSystem.MessageHandler.ShowInfoMessage("TRADE_NOITEMS_TITLE", "TRADE_NOITEMS_TEXT");
                return;
            }

            GameSystem.FireEvent("TradeOfferViewed");

            int Tolerance = OtherRealm.TradeManager.GetToleranceLevel();
            GameText ToleranceText = null;

            if (Tolerance >= 75) ToleranceText = GameText.CreateLocalised("TRADERESPONSE_HAPPY");
            else if (Tolerance >= 40) ToleranceText = GameText.CreateLocalised("TRADERESPONSE_NEUTRAL");
            else if (Tolerance >= 0) ToleranceText = GameText.CreateLocalised("TRADERESPONSE_UNHAPPY");
            else if (OtherRealm.TradeManager.LimitMet == false)
            {
                ToleranceText = GameText.CreateLocalised("TRADERESPONSE_FINAL");
                OtherRealm.DiplomacyManager.TriggerEvent(GameSystem.CurrentGame.PlayerRealm, "TradeAnnoyed");
            }
            else
            {
                GameSystem.MessageHandler.ShowInfoMessage("TRADE_NOTOL_TITLE", "TRADE_NOTOL_TEXT");
                return;
            }

            AITradeResponses Response = OtherRealm.AIPlayer.TradeManager.EvaluateTradeOffer(GameSystem.CurrentGame.PlayerRealm, CurrentTrade);

            GameText Status = null;
            bool CanAccept = true;

            switch (Response)
            {
                case AITradeResponses.NotPossible: Status = GameText.CreateLocalised("TRADEREACT_0"); CanAccept = false; break;
                case AITradeResponses.Insult: Status = GameText.CreateLocalised("TRADEREACT_1"); CanAccept = false; break;
                case AITradeResponses.Insufficient: Status = GameText.CreateLocalised("TRADEREACT_2"); CanAccept = false; break;
                case AITradeResponses.Almost: Status = GameText.CreateLocalised("TRADEREACT_3"); CanAccept = false; break;
                case AITradeResponses.Acceptable: Status = GameText.CreateLocalised("TRADEREACT_4"); break;
                case AITradeResponses.Good: Status = GameText.CreateLocalised("TRADEREACT_5"); break;
                case AITradeResponses.Fantastic: Status = GameText.CreateLocalised("TRADEREACT_6"); break;
            }

            GameText TitleText = GameText.CreateLocalised("TRADE_RESPONSE_TITLE");
            GameText ResponseText = GameText.CreateLocalised("FORMAT_TRADERESPONSE");
            ResponseText.AddChildText(GameText.CreateLocalised(OtherRealm.DisplayName));
            ResponseText.AddChildText(ToleranceText);
            ResponseText.AddChildText(Status);

            MessageBoxData Msg = new MessageBoxData();
            Msg.CaptionText = TitleText;
            Msg.MessageText = ResponseText;

            if (CanAccept)
            {
                Msg.DisplayType = MessageBoxType.YesNo;
                Msg.YesText = GameText.CreateLocalised("TRADE_YES");
                Msg.NoText = GameText.CreateLocalised("TRADE_NO");

                Msg.OnYesResponse = TradeOfferConfirm;
            }
            else
            {
                Msg.DisplayType = MessageBoxType.Info;
            }

            GameSystem.MessageHandler.ShowMessage(Msg);
        }

        private void TradeOfferConfirm()
        {
            if (CounterOffering)
            {
                AcceptButtonClick(null);
                CounterOffering = false;
                return;
            }

            WorkingAgent Agent = GameSystem.CurrentGame.PlayerRealm.Agents.FirstOrDefault(x => x.CurrentMode == AgentModes.Idle && x.HostRealm == x.OwnerRealm && x.TurnsLeft == 0);
            if (Agent == null) return;

            MessageBoxData Msg = new MessageBoxData();
            Msg.CaptionText = GameText.CreateLocalised("TRADE_SENT");
            Msg.MessageText = GameText.CreateLocalised("TRADE_STATUS_OK");
            Msg.MessageText.AddChildText(GameText.CreateLocalised(OtherRealm.DisplayName));

            Msg.MsgType = MessageType.GenericInfo;
            Msg.DisplayType = MessageBoxType.Info;

            GameSystem.MessageHandler.ShowMessage(Msg);

            GameSystem.CurrentGame.ClearPendingTrade(OtherRealm);

            CurrentTrade.Cleanup();
            Agent.TradeOffer = CurrentTrade;
            Agent.Send(OtherRealm, null, AgentModes.CarryTradeOffer);

            if (CurrentTrade.SenderOffers.Treaty != TreatyTypes.None)
            {
                GameSystem.CurrentGame.AllianceController.FormTreaty(GameSystem.CurrentGame.PlayerRealm, OtherRealm, CurrentTrade.SenderOffers.Treaty);
            }

            foreach (int ProvinceID in CurrentTrade.SenderOffers.GetProvinces())
            {
                WorkingProvince Province = GameSystem.CurrentGame.AllProvinces[ProvinceID];

                GameSystem.CurrentGame.ChangeProvinceOwner(Province, OtherRealm);
                if (Province.LandNode.CurrentStack != null) GameSystem.CurrentGame.WithdrawStack(Province.LandNode.CurrentStack);
                if (Province.HarbourNode != null && Province.HarbourNode.CurrentStack != null) GameSystem.CurrentGame.WithdrawStack(Province.HarbourNode.CurrentStack);
                GameSystem.FireEvent("ProvinceOccupierChanged", Province);
            }

            foreach (int ProvinceID in CurrentTrade.TargetOffers.GetProvinces())
            {
                WorkingProvince Province = GameSystem.CurrentGame.AllProvinces[ProvinceID];

                GameSystem.CurrentGame.ChangeProvinceOwner(Province, GameSystem.CurrentGame.PlayerRealm);
                if (Province.LandNode.CurrentStack != null) GameSystem.CurrentGame.WithdrawStack(Province.LandNode.CurrentStack);
                if (Province.HarbourNode != null && Province.HarbourNode.CurrentStack != null) GameSystem.CurrentGame.WithdrawStack(Province.HarbourNode.CurrentStack);
                GameSystem.FireEvent("ProvinceOccupierChanged", Province);
            }

            foreach (int UnitID in CurrentTrade.SenderOffers.GetPrisoners())
            {
                WorkingUnit Unit = GameSystem.CurrentGame.AllUnits[UnitID];
                GameSystem.CurrentGame.PlayerRealm.Prison.ReleasePrisoner(Unit);
            }

            foreach (int UnitID in CurrentTrade.TargetOffers.GetPrisoners())
            {
                WorkingUnit Unit = GameSystem.CurrentGame.AllUnits[UnitID];
                OtherRealm.Prison.ReleasePrisoner(Unit);
            }

            GameSystem.FireEvent("TradeOfferSent");
            GameForm.Visible = false;
        }

        public void AcceptButtonClick(UIControl Control)
        {
            //accept the trade offer and close the form
            CurrentTrade.SwitchSides();
            GameSystem.FireEvent("PlayerAcceptedTrade", CurrentTrade);

            GameForm.Visible = false;
        }

        public void RejectButtonClick(UIControl Control)
        {
            //ignore the trade offer and close the form
            CurrentTrade.SwitchSides();
            GameSystem.FireEvent("PlayerRejectedTrade", CurrentTrade);

            GameForm.Visible = false;
        }

        public void CounterButtonClick(UIControl Control)
        {
            //change the buttons but note that this is acounter offer
            //so that we can use the AI agent
            IsAIOffer = false;
            CounterOffering = true;
            UpdateAll();
        }

        void TheirOffersList_OnItemClicked(UIControl Control)
        {
            if (TheirOffersList.ClickedItem == null) return;
            CategorisedListItem Item = TheirOffersList.ClickedItem.Data;

            if (Item.Header)
            {
                TheirOfferStatuses[Item.CategoryID] = !TheirOfferStatuses[Item.CategoryID];
                UpdateAll();
            }
            else
            {

            }
        }

        void OurOffersList_OnItemClicked(UIControl Control)
        {
            if (OurOffersList.ClickedItem == null) return;
            CategorisedListItem Item = OurOffersList.ClickedItem.Data;

            if (Item.Header)
            {
                OurOfferStatuses[Item.CategoryID] = !OurOfferStatuses[Item.CategoryID];
                UpdateAll();
            }
            else
            {

            }
        }

        void TheirItemsList_OnItemClicked(UIControl Control)
        {
            if (TheirItemsList.ClickedItem == null) return;
            CategorisedListItem Item = TheirItemsList.ClickedItem.Data;

            if (Item.Header)
            {
                TheirItemStatuses[Item.CategoryID] = !TheirItemStatuses[Item.CategoryID];
                UpdateAll();
            }
            else
            {
                if (GameSystem.UIActionAllowed("TradeDemandAdd") == false) return;

                TradeItemData Data = Item.Data as TradeItemData;

                if (TradeItemBlocked(Data)) return;

                if (Data.Category == Tradecategories.Treaty) CurrentTrade.SenderOffers.AddItem(Data);
                CurrentTrade.TargetOffers.AddItem(Data);

                UpdateAll();
            }
        }

        private bool TradeItemBlocked(TradeItemData Data)
        {
            switch (Data.Category)
            {
                case Tradecategories.GoldLump: if (GameSystem.TradeRestrictions.GoldBlocked) return true; break;
                case Tradecategories.GoldPerTurn: if (GameSystem.TradeRestrictions.GoldPerTurnBlocked) return true; break;
                case Tradecategories.Prisoner: if (GameSystem.TradeRestrictions.PrisonersBlocked) return true; break;
                case Tradecategories.Province: if (GameSystem.TradeRestrictions.ProvinceBlocked) return true; break;
                case Tradecategories.ResourceLump:
                    {
                        if (GameSystem.TradeRestrictions.ResourceBlocked) return true;

                        if (GameSystem.TradeRestrictions.ResourceWhitelist.Count > 0)
                        {
                            ResourceData Resource = GameSystem.Data.Resources.Values.ElementAt(Data.ID);
                            if (GameSystem.TradeRestrictions.ResourceWhitelist.Contains(Resource.ResourceName) == false) return true;
                        }

                        break;
                    }
                case Tradecategories.ResourcePerTurn:
                    {
                        if (GameSystem.TradeRestrictions.ResourcePerTurnBlocked) return true;

                        if (GameSystem.TradeRestrictions.ResourceWhitelist.Count > 0)
                        {
                            ResourceData Resource = GameSystem.Data.Resources.Values.ElementAt(Data.ID);
                            if (GameSystem.TradeRestrictions.ResourceWhitelist.Contains(Resource.ResourceName) == false) return true;
                        }

                        break;
                    }
                case Tradecategories.Treaty: if (GameSystem.TradeRestrictions.TreatyBlocked) return true; break;
            }

            return false;
        }

        void OurItemsList_OnItemClicked(UIControl Control)
        {
            if (OurItemsList.ClickedItem == null) return;
            CategorisedListItem Item = OurItemsList.ClickedItem.Data;

            if (Item.Header)
            {
                OurItemStatuses[Item.CategoryID] = !OurItemStatuses[Item.CategoryID];
                UpdateAll();
            }
            else
            {
                if (GameSystem.UIActionAllowed("TradeOfferAdd") == false) return;

                TradeItemData Data = Item.Data as TradeItemData;

                if (TradeItemBlocked(Data)) return;

                if (Data.Category == Tradecategories.Treaty) CurrentTrade.TargetOffers.AddItem(Data);
                CurrentTrade.SenderOffers.AddItem(Data);

                UpdateAll();
            }
        }

        private void OnCategorisedListItemUpdated(ListItem<CategorisedListItem> Item)
        {
            //images used for buttons
            string Reduce1 = "Data\\Images\\HUD\\economy\\arrow_down_normal.png";
            string Reduce2 = "Data\\Images\\HUD\\economy\\arrow_down_mouseover.png";
            string Reduce3 = "Data\\Images\\HUD\\economy\\arrow_down_pressed.png";

            string Increase1 = "Data\\Images\\HUD\\economy\\arrow_up_normal.png";
            string Increase2 = "Data\\Images\\HUD\\economy\\arrow_up_mouseover.png";
            string Increase3 = "Data\\Images\\HUD\\economy\\arrow_up_pressed.png";

            string Remove1 = "Data\\Images\\HUD\\purchase\\cancel_normal.png";
            string Remove2 = "Data\\Images\\HUD\\purchase\\cancel_mouseover.png";
            string Remove3 = "Data\\Images\\HUD\\purchase\\cancel_pressed.png";

            //all lines, apply indent
            float IndentLeft = Item.Data.IndentLevel * Item.Container.Sprite.Bounds.Width * 0.05f;

            if (Item.Data.Header)
            {
                //header
                //  coloured background, text, open/close button
                ControlImage Expand = new ControlImage(GameSystem);
                Expand.SetBounds(IndentLeft, Item.Container.Sprite.Bounds.Height * 0.05f, Item.Container.Sprite.Bounds.Height * 0.9f, Item.Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
                Expand.MouseInputType = MouseInputTypes.None;
                Item.Container.AddChild(Expand);

                if (Item.Data.Closed) Expand.SetImageFile("Data\\Images\\HUD\\Economy\\arrow_down_normal.png");
                else Expand.SetImageFile("Data\\Images\\HUD\\Economy\\arrow_up_normal.png");

                ControlImage UnitImage = new ControlImage(GameSystem);
                UnitImage.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                UnitImage.SetImageFile("Data\\Images\\HUD\\ListSelect.png");
                UnitImage.MouseInputType = MouseInputTypes.None;
                Item.Container.AddChild(UnitImage);

                ControlText UnitText = new ControlText(GameSystem);
                UnitText.SetBounds(IndentLeft + Item.Container.Sprite.Bounds.Height * 1.05f, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.65f), UIUnits.PixelScaled);
                UnitText.FontName = "Trebuchet MS";
                UnitText.Bold = true;
                UnitText.TextAnchor = AnchorPoints.Left;
                UnitText.SetTextData(Item.Data.Text);
                Item.Container.AddChild(UnitText);
            }
            else
            {
                //normal line
                //  image on left, text after image
                if (Item.Data.ImageFile != null)
                {
                    ControlImage Image = new ControlImage(GameSystem);
                    Image.SetBounds((Item.Container.Sprite.Bounds.Height * 0.05f) + IndentLeft, Item.Container.Sprite.Bounds.Height * 0.05f, Item.Container.Sprite.Bounds.Height * 0.9f, Item.Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
                    Image.SetImageFile(Item.Data.ImageFile);
                    Image.MouseInputType = MouseInputTypes.None;
                    Item.Container.AddChild(Image);
                }

                ControlText UnitText = new ControlText(GameSystem);
                UnitText.SetBounds((Item.Container.Sprite.Bounds.Height) + IndentLeft, 0, Item.Container.Sprite.Bounds.Width - Item.Container.Sprite.Bounds.Height, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.65f), UIUnits.PixelScaled);
                UnitText.FontName = "Trebuchet MS";
                UnitText.TextAnchor = AnchorPoints.Left;
                UnitText.Colour = Color.FromArgb(255, 238, 161);
                UnitText.SetTextData(Item.Data.Text);
                Item.Container.AddChild(UnitText);

                //if a value is given, add a right aligned text to hold it
                if (Item.Data.HasValue)
                {
                    ControlText RelationText = new ControlText(GameSystem);
                    RelationText.ControlName = "ValueText";
                    RelationText.SetAnchor(AnchorPoints.TopRight);
                    RelationText.SetBounds(Item.Container.Sprite.Bounds.Height * 0.05f, 0, Item.Container.Sprite.Bounds.Height * 2, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                    RelationText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.65f), UIUnits.PixelScaled);
                    RelationText.FontName = "Trebuchet MS";
                    RelationText.TextAnchor = AnchorPoints.Right;
                    RelationText.Colour = Color.FromArgb(255, 238, 161);
                    RelationText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Item.Data.Value));
                    Item.Container.AddChild(RelationText);
                }

                if (Item.Data.IncludeRemoveButton)
                {
                    ControlButton RemoveButton = new ControlButton(GameSystem);
                    RemoveButton.SetAnchor(AnchorPoints.TopRight);
                    RemoveButton.SetBounds(Item.Container.Sprite.Bounds.Height * 1.70f, 0, Item.Container.Sprite.Bounds.Height, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                    RemoveButton.SetImageFiles(Remove1, Remove2, Remove3);
                    RemoveButton.CustomData = Item;
                    RemoveButton.OnClick += RemoveButton_OnClick;
                    Item.Container.AddChild(RemoveButton);
                }

                if (Item.Data.IncludeNumericButtons)
                {
                    ControlButton DownButton = new ControlButton(GameSystem);
                    DownButton.SetAnchor(AnchorPoints.TopRight);
                    DownButton.SetBounds(Item.Container.Sprite.Bounds.Height * 2.70f, 0, Item.Container.Sprite.Bounds.Height, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                    DownButton.SetImageFiles(Reduce1, Reduce2, Reduce3);
                    DownButton.CustomData = Item;
                    DownButton.AutoClick = true;
                    DownButton.OnClick += DownButton_OnClick;
                    Item.Container.AddChild(DownButton);

                    ControlButton UpButton = new ControlButton(GameSystem);
                    UpButton.SetAnchor(AnchorPoints.TopRight);
                    UpButton.SetBounds(Item.Container.Sprite.Bounds.Height * 3.70f, 0, Item.Container.Sprite.Bounds.Height, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                    UpButton.SetImageFiles(Increase1, Increase2, Increase3);
                    UpButton.CustomData = Item;
                    UpButton.AutoClick = true;
                    UpButton.OnClick += UpButton_OnClick;
                    Item.Container.AddChild(UpButton);
                }
            }
        }

        void UpButton_OnClick(UIControl Control)
        {
            CategorisedListItem Item = (Control.CustomData as ListItem<CategorisedListItem>).Data;
            TradeItemData Data = Item.Data as TradeItemData;

            int AdjustAmount = (Control as ControlButton).AutoClickCount / 2;
            if (AdjustAmount < 1) AdjustAmount = 1;
            if (AdjustAmount > 100) AdjustAmount = 100;

            int CurrentValue = Data.Value;
            CurrentValue += AdjustAmount;

            if ((Control.CustomData as ListItem<CategorisedListItem>).Parent == OurOffersList)
            {
                //determine available amount, modify current amount if needed
                int Maximum = GetRealmAvailableQuantity(GameSystem.CurrentGame.PlayerRealm, Data);
                Maximum = Math.Max(0, Maximum);
                CurrentValue = Math.Min(Maximum, CurrentValue);

                //change data.value
                Data.Value = CurrentValue;

                //change value in trade offer
                CurrentTrade.SenderOffers.SetQuantity(Data);

                //change ValueText
                ControlText ValueText = (Control.CustomData as ListItem<CategorisedListItem>).Container.GetControlByName<ControlText>("ValueText");
                ValueText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", CurrentValue));
            }
            else
            {
                //determine available amount, modify current amount if needed
                int Maximum = GetRealmAvailableQuantity(OtherRealm, Data);
                Maximum = Math.Max(0, Maximum);
                CurrentValue = Math.Min(Maximum, CurrentValue);

                //change data.value
                Data.Value = CurrentValue;

                //change value in trade offer
                CurrentTrade.TargetOffers.SetQuantity(Data);

                //change ValueText
                ControlText ValueText = (Control.CustomData as ListItem<CategorisedListItem>).Container.GetControlByName<ControlText>("ValueText");
                ValueText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", CurrentValue));
            }

            UpdateAcceptancePanel();
        }

        private int GetRealmAvailableQuantity(WorkingRealm Realm, TradeItemData Data)
        {
            switch (Data.Category)
            {
                case Tradecategories.GoldLump: return Realm.GetTradeGold();
                case Tradecategories.GoldPerTurn: return GameSystem.CurrentGame.EconomyController.GetRealmTotalIncome(Realm) - GameSystem.CurrentGame.EconomyController.GetTotalExpenses(Realm);
                case Tradecategories.ResourceLump:
                    {
                        ResourceData Resource = GameSystem.Data.Resources.Values.ElementAt(Data.ID);
                        return Realm.GetStockpiledResource(Resource);
                    }
                case Tradecategories.ResourcePerTurn:
                    {
                        ResourceData Resource = GameSystem.Data.Resources.Values.ElementAt(Data.ID);
                        return Realm.GetResourceIncome(Resource) - Realm.GetResourceExpenses(Resource);
                    }
            }

            return 0;
        }

        void DownButton_OnClick(UIControl Control)
        {
            CategorisedListItem Item = (Control.CustomData as ListItem<CategorisedListItem>).Data;
            TradeItemData Data = Item.Data as TradeItemData;

            int AdjustAmount = (Control as ControlButton).AutoClickCount / 2;
            if (AdjustAmount < 1) AdjustAmount = 1;
            if (AdjustAmount > 100) AdjustAmount = 100;

            int CurrentValue = Data.Value;
            CurrentValue -= AdjustAmount;

            int Min = 0;
            if (GameSystem.UIActionAllowed("TradeRemove") == false) Min = 1;

            CurrentValue = Math.Max(Min, CurrentValue);

            if ((Control.CustomData as ListItem<CategorisedListItem>).Parent == OurOffersList)
            {
                //change data.value
                Data.Value = CurrentValue;

                //change value in trade offer
                CurrentTrade.SenderOffers.SetQuantity(Data);

                //change ValueText
                ControlText ValueText = (Control.CustomData as ListItem<CategorisedListItem>).Container.GetControlByName<ControlText>("ValueText");
                ValueText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", CurrentValue));
            }
            else
            {
                //change data.value
                Data.Value = CurrentValue;

                //change value in trade offer
                CurrentTrade.TargetOffers.SetQuantity(Data);

                //change ValueText
                ControlText ValueText = (Control.CustomData as ListItem<CategorisedListItem>).Container.GetControlByName<ControlText>("ValueText");
                ValueText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", CurrentValue));
            }

            UpdateAcceptancePanel();
        }

        void RemoveButton_OnClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("TradeRemove") == false) return;

            CategorisedListItem Item = (Control.CustomData as ListItem<CategorisedListItem>).Data;
            TradeItemData Data = Item.Data as TradeItemData;

            if ((Control.CustomData as ListItem<CategorisedListItem>).Parent == OurOffersList)
            {
                CurrentTrade.SenderOffers.RemoveItem(Data);
                if (Data.Category == Tradecategories.Treaty) CurrentTrade.TargetOffers.RemoveItem(Data);
            }
            else
            {
                CurrentTrade.TargetOffers.RemoveItem(Data);
                if (Data.Category == Tradecategories.Treaty) CurrentTrade.SenderOffers.RemoveItem(Data);
            }

            UpdateAll();
        }

        private void HandlePlayerTradeOfferWithItems(string EventName, params object[] Args)
        {
            OtherRealm = Args[0] as WorkingRealm;
            CurrentTrade = Args[1] as TradeOffer;

            GameForm.Visible = true;
            IsAIOffer = false;
            UpdateAll();
        }

        private void HandlePlayerTreatyOffer(string EventName, params object[] Args)
        {
            OtherRealm = Args[0] as WorkingRealm;
            TreatyTypes Treaty = (TreatyTypes)Args[1];

            GameSystem.CurrentGame.ClearPendingTrade(OtherRealm);
            CurrentTrade = GameSystem.CurrentGame.GetPendingTrade(OtherRealm);
            CurrentTrade.SenderOffers.Treaty = Treaty;
            CurrentTrade.TargetOffers.Treaty = Treaty;

            if (Treaty == TreatyTypes.Peace)
            {
                foreach (WorkingUnit Unit in OtherRealm.Prison.GetRealmPrisoners(GameSystem.CurrentGame.PlayerRealm))
                {
                    CurrentTrade.TargetOffers.AddPrisoner(Unit);
                }

                foreach (WorkingUnit Unit in GameSystem.CurrentGame.PlayerRealm.Prison.GetRealmPrisoners(OtherRealm))
                {
                    CurrentTrade.SenderOffers.AddPrisoner(Unit);
                }
            }

            GameForm.Visible = true;
            IsAIOffer = false;
            UpdateAll();
        }

        private void HandlePlayerTradeOffer(string EventName, params object[] Args)
        {
            OtherRealm = Args[0] as WorkingRealm;
            string Reason;
            if (!RealmTradeManager.CanStartEconomicTrade(GameSystem.CurrentGame.PlayerRealm, OtherRealm, out Reason))
            {
                GameSystem.WriteLog("[TradeRule] Blocked player trade open " + GameSystem.CurrentGame.PlayerRealm.Name + " -> " + OtherRealm.Name + ": " + Reason);
                return;
            }

            CurrentTrade = GameSystem.CurrentGame.GetPendingTrade(OtherRealm);
            GameForm.Visible = true;
            IsAIOffer = false;

            FixCurrentTrade();

            UpdateAll();
        }

        private void FixCurrentTrade()
        {
            CheckTradeValid(CurrentTrade.SenderOffers, GameSystem.CurrentGame.PlayerRealm, OtherRealm);
            CheckTradeValid(CurrentTrade.TargetOffers, OtherRealm, GameSystem.CurrentGame.PlayerRealm);
        }

        private void CheckTradeValid(TradeOfferList Trade, WorkingRealm Realm, WorkingRealm OtherRealm)
        {
            if (Trade.GoldLump > Realm.GetTradeGold()) Trade.GoldLump = Realm.GetTradeGold();
            if (Trade.GoldPerTurn > 0)
            {
                int Maximum = GameSystem.CurrentGame.EconomyController.GetRealmTotalIncome(Realm) - GameSystem.CurrentGame.EconomyController.GetTotalExpenses(Realm);
                if (Trade.GoldPerTurn > Maximum) Trade.GoldPerTurn = Maximum;
            }

            foreach (KeyValuePair<ResourceData, int> Pair in Trade.GetResourceLump())
            {
                if (Pair.Value == 0) continue;

                int Maximum = Realm.GetStockpiledResource(Pair.Key);
                if (Pair.Value > Maximum) Trade.SetLumpResource(Pair.Key, Maximum);
            }

            foreach (KeyValuePair<ResourceData, int> Pair in Trade.GetResourceTurn())
            {
                if (Pair.Value == 0) continue;

                int Maximum = Realm.GetResourceIncome(Pair.Key) - Realm.GetResourceExpenses(Pair.Key);
                if (Pair.Value > Maximum) Trade.SetLumpResource(Pair.Key, Maximum);
            }

            foreach (int ID in Trade.GetProvinces())
            {
                WorkingProvince Province = GameSystem.CurrentGame.AllProvinces[ID];

                if (Province.IsCapitol) Trade.RemoveProvince(Province);
                else if (Province.Occupied) Trade.RemoveProvince(Province);
                else if (Province.OwnerRealm != Realm) Trade.RemoveProvince(Province);
            }

            foreach (int ID in Trade.GetPrisoners())
            {
                WorkingUnit Unit = null;
                GameSystem.CurrentGame.AllUnits.TryGetValue(ID, out Unit);

                if (Unit == null) Trade.RemovePrisoner(ID);
                else if (Unit.IsPrisoner == false) Trade.RemovePrisoner(ID);
                else if (Realm.Prison.ContainsUnit(ID) == false) Trade.RemovePrisoner(ID);
            }

            if (Trade.Treaty != TreatyTypes.None)
            {
                if (GameSystem.CurrentGame.AllianceController.TreatyIsPossible(Realm, OtherRealm, Trade.Treaty) == false)
                {
                    Trade.Treaty = TreatyTypes.None;
                }
            }
        }

        private void HandleAITradeOffer(string EventName, params object[] Args)
        {
            CurrentTrade = Args[0] as TradeOffer;

            OtherRealm = GameSystem.CurrentGame.AllRealms[CurrentTrade.SenderRealmID];
            if (GameSystem.CurrentGame.TradeAllowed(OtherRealm) == false && CurrentTrade.SenderOffers.Treaty != TreatyTypes.Peace)
            {
                GameSystem.FireEvent("PlayerRejectedTrade", CurrentTrade);
                return;
            }

            GameSystem.CurrentGame.TradeOffered(OtherRealm);

            CurrentTrade.SwitchSides();

            GameForm.Visible = true;
            IsAIOffer = true;
            CounterOffering = false;
            UpdateAll();
        }

        private void UpdateAll()
        {
            ControlText TitleText = GameForm.GetControlByName<ControlText>("TradeTitleText");
            ControlText OurItemsTitle = GameForm.GetControlByName<ControlText>("OurItemsTitle");
            ControlText OurOffersTitle = GameForm.GetControlByName<ControlText>("OurOffersTitle");
            ControlText TheirOffersTitle = GameForm.GetControlByName<ControlText>("TheirOffersTitle");
            ControlText TheirItemsTitle = GameForm.GetControlByName<ControlText>("TheirItemsTitle");

            GameText Title = null;
            GameText Title1 = GameText.CreateLocalised("TRADE_TITLE_ITEMS");
            GameText Title2 = GameText.CreateLocalised("TRADE_TITLE_OFFERS");
            GameText Title3 = GameText.CreateLocalised("TRADE_TITLE_OFFERS");
            GameText Title4 = GameText.CreateLocalised("TRADE_TITLE_ITEMS");

            if (IsAIOffer) Title = GameText.CreateLocalised("TRADE_TITLE_FROM");
            else Title = GameText.CreateLocalised("TRADE_TITLE_TO");

            Title.AddChildText(GameText.CreateLocalised(OtherRealm.DisplayName));
            TitleText.SetTextData(Title);

            Title1.AddChildText(GameText.CreateLocalised(GameSystem.CurrentGame.PlayerRealm.DisplayName));
            Title2.AddChildText(GameText.CreateLocalised(GameSystem.CurrentGame.PlayerRealm.DisplayName));
            Title3.AddChildText(GameText.CreateLocalised(OtherRealm.DisplayName));
            Title4.AddChildText(GameText.CreateLocalised(OtherRealm.DisplayName));

            OurItemsTitle.SetTextData(Title1);
            OurOffersTitle.SetTextData(Title2);
            TheirOffersTitle.SetTextData(Title3);
            TheirItemsTitle.SetTextData(Title4);

            FillItems(OurItemsList, OurItemStatuses, GameSystem.CurrentGame.PlayerRealm, OtherRealm, CurrentTrade.SenderOffers);
            FillItems(TheirItemsList, TheirItemStatuses, OtherRealm, GameSystem.CurrentGame.PlayerRealm, CurrentTrade.TargetOffers);

            FillOffers(OurOffersList, OurOfferStatuses, GameSystem.CurrentGame.PlayerRealm, OtherRealm, CurrentTrade.SenderOffers);
            FillOffers(TheirOffersList, TheirOfferStatuses, OtherRealm, GameSystem.CurrentGame.PlayerRealm, CurrentTrade.TargetOffers);

            UpdateAcceptancePanel();
        }

        private void UpdateAcceptancePanel()
        {
            ControlImage AcceptanceBG = GameForm.GetControlByName<ControlImage>("AcceptanceBG");
            ControlText AcceptanceTitle = GameForm.GetControlByName<ControlText>("AcceptanceTitle");
            ControlText AcceptanceStatus = GameForm.GetControlByName<ControlText>("AcceptanceStatusText");
            ControlText AcceptancePos = GameForm.GetControlByName<ControlText>("AcceptancePosText");
            ControlText AcceptanceNeg = GameForm.GetControlByName<ControlText>("AcceptanceNegText");

            if (IsAIOffer || OtherRealm == null || CurrentTrade == null || OtherRealm.AIPlayer == null)
            {
                if (AcceptanceBG != null) AcceptanceBG.Visible = false;
                return;
            }

            AcceptanceBG.Visible = true;
            AcceptanceTitle.SetTextData(GameText.CreateFromLiteral("Offer acceptance"));

            if (CurrentTrade.OfferIsEmpty())
            {
                AcceptanceStatus.SetTextData(GameText.CreateFromLiteral(""));
                AcceptancePos.SetTextData(GameText.CreateFromLiteral(""));
                AcceptanceNeg.SetTextData(GameText.CreateFromLiteral(""));
                UpdateOfferButtonTooltip("");
                return;
            }

            AITradeResponses Response = OtherRealm.AIPlayer.TradeManager.EvaluateTradeOffer(GameSystem.CurrentGame.PlayerRealm, CurrentTrade);
            bool PeaceOffer = CurrentTrade.SenderOffers.Treaty == TreatyTypes.Peace
                || CurrentTrade.TargetOffers.Treaty == TreatyTypes.Peace;
            RelationStates Relation = OtherRealm.DiplomacyManager.GetRelation(GameSystem.CurrentGame.PlayerRealm);

            StringBuilder StatusBuilder = new StringBuilder();
            StatusBuilder.AppendLine(GetAcceptanceStatusLine(Response));

            StringBuilder PosBuilder = new StringBuilder();
            StringBuilder NegBuilder = new StringBuilder();

            if (PeaceOffer && Relation == RelationStates.War)
            {
                AppendPeaceAcceptance(StatusBuilder, PosBuilder, NegBuilder);
                string PeaceHint = GetAcceptanceHint(Response, true);
                if (!string.IsNullOrEmpty(PeaceHint) && Response == AITradeResponses.NotPossible)
                {
                    StatusBuilder.AppendLine(PeaceHint);
                }
            }
            else if (Response == AITradeResponses.NotPossible)
            {
                StatusBuilder.Append("Blocked by AI trade rules.");
            }
            else
            {
                string Hint = GetAcceptanceHint(Response, PeaceOffer);
                if (!string.IsNullOrEmpty(Hint)) StatusBuilder.Append(Hint);
            }

            AcceptanceStatus.SetTextData(GameText.CreateFromLiteral(StatusBuilder.ToString().TrimEnd()));
            AcceptancePos.SetTextData(GameText.CreateFromLiteral(PosBuilder.ToString().TrimEnd()));
            AcceptanceNeg.SetTextData(GameText.CreateFromLiteral(NegBuilder.ToString().TrimEnd()));

            StringBuilder Tip = new StringBuilder();
            Tip.AppendLine(StatusBuilder.ToString().TrimEnd());
            if (PosBuilder.Length > 0 || NegBuilder.Length > 0)
            {
                Tip.AppendLine();
                Tip.AppendLine(PosBuilder.ToString().TrimEnd());
                Tip.Append(NegBuilder.ToString().TrimEnd());
            }
            UpdateOfferButtonTooltip(Tip.ToString().TrimEnd());
        }

        private void UpdateOfferButtonTooltip(string DetailText)
        {
            ControlButton OfferButton = GameForm.GetControlByName<ControlButton>("OfferButton");
            if (OfferButton == null) return;

            if (string.IsNullOrEmpty(DetailText))
            {
                OfferButton.SetTooltip(GameText.CreateLocalised("TT_OFFERTRADE"));
                return;
            }

            OfferButton.SetTooltip(GameText.CreateFromLiteral(DetailText));
        }

        private string GetAcceptanceStatusLine(AITradeResponses Response)
        {
            switch (Response)
            {
                case AITradeResponses.NotPossible: return "Will never accept";
                case AITradeResponses.Insult: return "Finds this insulting";
                case AITradeResponses.Insufficient: return "Not enough for them";
                case AITradeResponses.Almost: return "Close — needs a better offer";
                case AITradeResponses.Acceptable: return "Would accept";
                case AITradeResponses.Good: return "Happy to accept";
                case AITradeResponses.Fantastic: return "Eager to accept";
                default: return "Unknown";
            }
        }

        private string GetAcceptanceHint(AITradeResponses Response, bool PeaceOffer)
        {
            if (PeaceOffer && Response == AITradeResponses.NotPossible)
            {
                return "Gold/goods won't help until peace desire rises.";
            }

            switch (Response)
            {
                case AITradeResponses.Insult:
                case AITradeResponses.Insufficient:
                case AITradeResponses.Almost:
                    return PeaceOffer
                        ? "Once they want peace, gold or land can tip the deal."
                        : "Add gold, goods, or provinces on your side.";
                case AITradeResponses.Acceptable:
                case AITradeResponses.Good:
                case AITradeResponses.Fantastic:
                    return "They are willing to take this deal.";
                default:
                    return "";
            }
        }

        private void AppendPeaceAcceptance(StringBuilder StatusBuilder, StringBuilder PosBuilder, StringBuilder NegBuilder)
        {
            WorkingRealm Player = GameSystem.CurrentGame.PlayerRealm;
            AIWarManager WarManager = OtherRealm.AIPlayer.WarManager;

            int RelationTime = OtherRealm.DiplomacyManager.GetRelationTime(Player);
            int Desire = 0;

            try
            {
                Desire = WarManager.GetPeaceDesire(Player);
            }
            catch
            {
                StatusBuilder.AppendLine("Peace desire unavailable (no war data).");
                return;
            }

            StatusBuilder.AppendLine(string.Format("Peace desire {0}", FormatSigned(Desire)));
            if (RelationTime < 5)
            {
                StatusBuilder.AppendLine(string.Format("Too early: war must last 5 turns ({0} left).", 5 - RelationTime));
            }

            List<Tuple<string, int>> Breakdown = GetPeaceBreakdownWithTraits(WarManager, Player);
            List<Tuple<string, int>> Positives = new List<Tuple<string, int>>();
            List<Tuple<string, int>> Negatives = new List<Tuple<string, int>>();
            int PosTotal = 0;
            int NegTotal = 0;

            foreach (Tuple<string, int> Item in Breakdown)
            {
                if (Item.Item2 > 0)
                {
                    PosTotal += Item.Item2;
                    Positives.Add(Item);
                }
                else if (Item.Item2 < 0)
                {
                    NegTotal += Item.Item2;
                    Negatives.Add(Item);
                }
            }

            PosBuilder.AppendLine(string.Format("For: {0}", FormatSigned(PosTotal)));
            foreach (Tuple<string, int> Item in Positives)
            {
                PosBuilder.AppendLine(string.Format("{0} {1}", FormatSigned(Item.Item2).PadLeft(4), ExplainBreakdownLabel(Item.Item1, Item.Item2)));
            }

            NegBuilder.AppendLine(string.Format("Against: {0}", FormatSigned(NegTotal)));
            foreach (Tuple<string, int> Item in Negatives)
            {
                NegBuilder.AppendLine(string.Format("{0} {1}", FormatSigned(Item.Item2).PadLeft(4), ExplainBreakdownLabel(Item.Item1, Item.Item2)));
            }
        }

        private string ExplainBreakdownLabel(string Label, int Value)
        {
            string Key = Label.TrimEnd(':').Trim();
            switch (Key)
            {
                case "Income":
                    return Value < 0 ? "Healthy treasury" : "Strained treasury";
                case "Damaged provinces":
                    return "Their ruined provinces";
                case "Attrition (Units)":
                    return "Their army losses";
                case "Attrition (Time)":
                    return "War dragging on";
                case "Gains (Province)":
                    return Value < 0 ? "Lost provinces to you" : "Took provinces from you";
                case "Gains (k/d ratio)":
                    return Value < 0 ? "Losing battles" : "Winning battles";
                case "Value of war":
                    return "Wants your lands";
                case "Other wars":
                    return "Busy with other wars";
                case "Expecting to win":
                    // Engine: +10 = DefeatIsLikely (fears you), -10 = VictoryIsPossible (expects to beat you)
                    return Value > 0 ? "Fears losing to you" : "Expects to beat you";
                case "Disposition":
                    return "Opinion of you";
                case "Warmonger":
                    return "Ruler is a warmonger";
                case "Militarist":
                    return "Ruler is a militarist";
                case "Diplomat":
                    return "Ruler is a diplomat";
                default:
                    return Key;
            }
        }

        private List<Tuple<string, int>> GetPeaceBreakdownWithTraits(AIWarManager WarManager, WorkingRealm Player)
        {
            List<Tuple<string, int>> List = new List<Tuple<string, int>>();
            List.AddRange(WarManager.GetPeaceDesireBreakdown(Player));

            // Older DLL builds omit trait lines from the breakdown; add them so the panel matches GetPeaceDesire.
            if (!List.Exists(x => x.Item1 == "Warmonger:"))
            {
                int Warmonger = OtherRealm.Traits[AITraits.Warmonger];
                int Militarist = OtherRealm.Traits[AITraits.Militarist];
                int Diplomat = OtherRealm.Traits[AITraits.Diplomat];
                List.Add(new Tuple<string, int>("Warmonger:", (int)(-Warmonger * 1.5f)));
                List.Add(new Tuple<string, int>("Militarist:", (int)(-Militarist * 0.5f)));
                List.Add(new Tuple<string, int>("Diplomat:", (int)(Diplomat * 0.5f)));
            }
            return List;
        }

        private string FormatSigned(int Value)
        {
            if (Value > 0) return "+" + Value.ToString();
            return Value.ToString();
        }

        private void FillOffers(ControlList<CategorisedListItem> OffersList, Dictionary<int, bool> Statuses, WorkingRealm Realm, WorkingRealm OtherRealm, TradeOfferList Offers)
        {
            OffersList.ClearItems();

            if (Offers.GoldLump > 0 || Offers.GoldPerTurn > 0)
            {
                CategorisedListItem MoneyHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATMONEY"));
                MoneyHeader.CategoryID = CATEGORY_MONEY;
                MoneyHeader.Closed = Statuses[CATEGORY_MONEY];
                OffersList.AddItem(MoneyHeader);

                if (!MoneyHeader.Closed)
                {
                    if (Offers.GoldLump > 0)
                    {
                        CategorisedListItem LumpItem = new CategorisedListItem(false, GameText.CreateLocalised("TRADECATGOLD1"));
                        LumpItem.HasValue = true;
                        LumpItem.Value = Offers.GoldLump;
                        LumpItem.IndentLevel = 1;
                        LumpItem.IncludeNumericButtons = !IsAIOffer || CounterOffering;
                        LumpItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                        LumpItem.Data = new TradeItemData(Tradecategories.GoldLump, 0, Offers.GoldLump);
                        OffersList.AddItem(LumpItem);
                    }

                    if (Offers.GoldPerTurn > 0)
                    {
                        CategorisedListItem TurnItem = new CategorisedListItem(false, GameText.CreateLocalised("TRADECATGOLD2"));
                        TurnItem.HasValue = true;
                        TurnItem.Value = Offers.GoldPerTurn;
                        TurnItem.IndentLevel = 1;
                        TurnItem.IncludeNumericButtons = !IsAIOffer || CounterOffering;
                        TurnItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                        TurnItem.Data = new TradeItemData(Tradecategories.GoldPerTurn, 0, Offers.GoldPerTurn);
                        OffersList.AddItem(TurnItem);
                    }
                }
            }

            if (Offers.HasResources())
            {
                CategorisedListItem ResourceHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATRESOURCES"));
                ResourceHeader.CategoryID = CATEGORY_RESOURCE;
                ResourceHeader.Closed = Statuses[CATEGORY_RESOURCE];
                OffersList.AddItem(ResourceHeader);

                if (!ResourceHeader.Closed)
                {
                    int ID = 0;
                    foreach (ResourceData Resource in GameSystem.Data.Resources.Values)
                    {
                        int ResourceLump = Offers.GetResourceLumpValue(Resource);
                        int ResourceTurn = Offers.GetResourceTurnValue(Resource);

                        if (ResourceLump > 0)
                        {
                            CategorisedListItem LumpItem = new CategorisedListItem(false, GameText.CreateLocalised(Resource.DisplayName));
                            LumpItem.HasValue = true;
                            LumpItem.Value = ResourceLump;
                            LumpItem.IndentLevel = 1;
                            LumpItem.IncludeNumericButtons = !IsAIOffer || CounterOffering; ;
                            LumpItem.IncludeRemoveButton = !IsAIOffer || CounterOffering; ;
                            LumpItem.Data = new TradeItemData(Tradecategories.ResourceLump, ID, ResourceLump);
                            LumpItem.ImageFile = "Data\\Images\\HUD\\tradeicons\\" + Resource.ResourceName + ".png";
                            OffersList.AddItem(LumpItem);
                        }

                        if (ResourceTurn > 0)
                        {
                            GameText Text = GameText.CreateLocalised("TRADETABLE_RPT");
                            Text.AddChildText(GameText.CreateLocalised(Resource.DisplayName));

                            CategorisedListItem TurnItem = new CategorisedListItem(false, Text);
                            TurnItem.HasValue = true;
                            TurnItem.Value = ResourceTurn;
                            TurnItem.IndentLevel = 1;
                            TurnItem.IncludeNumericButtons = !IsAIOffer || CounterOffering;
                            TurnItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                            TurnItem.Data = new TradeItemData(Tradecategories.ResourcePerTurn, ID, ResourceTurn);
                            TurnItem.ImageFile = "Data\\Images\\HUD\\tradeicons\\" + Resource.ResourceName + ".png";
                            OffersList.AddItem(TurnItem);
                        }

                        ID++;
                    }
                }
            }

            if (Offers.GetProvinces().Count > 0)
            {
                CategorisedListItem ProvinceHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATPROVINCES"));
                ProvinceHeader.CategoryID = CATEGORY_PROVINCE;
                ProvinceHeader.Closed = Statuses[CATEGORY_PROVINCE];
                OffersList.AddItem(ProvinceHeader);

                if (!ProvinceHeader.Closed)
                {
                    foreach (int ID in Offers.GetProvinces())
                    {
                        WorkingProvince Province = GameSystem.CurrentGame.AllProvinces[ID];

                        CategorisedListItem ProvinceItem = new CategorisedListItem(false, GameText.CreateLocalised(Province.DisplayName));
                        ProvinceItem.IndentLevel = 1;
                        ProvinceItem.ImageFile = "Data\\Images\\Map\\Province Icons\\" + Province.Name + ".png";
                        ProvinceItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                        ProvinceItem.Data = new TradeItemData(Tradecategories.Province, ID, 0);
                        OffersList.AddItem(ProvinceItem);
                    }
                }
            }


            if (Offers.GetPrisoners().Count > 0)
            {
                CategorisedListItem UnitHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATPRISONERS"));
                UnitHeader.CategoryID = CATEGORY_UNIT;
                UnitHeader.Closed = Statuses[CATEGORY_UNIT];
                OffersList.AddItem(UnitHeader);

                if (!UnitHeader.Closed)
                {
                    foreach (int ID in Offers.GetPrisoners())
                    {
                        WorkingUnit Unit = GameSystem.CurrentGame.AllUnits[ID];

                        CategorisedListItem PrisonerItem = new CategorisedListItem(false, GameText.CreateLocalised(Unit.DisplayName));
                        PrisonerItem.IndentLevel = 1;
                        PrisonerItem.ImageFile = "Data\\Images\\Units\\" + Unit.ImageFile + ".jpg";
                        PrisonerItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                        PrisonerItem.Data = new TradeItemData(Tradecategories.Prisoner, ID, 0);
                        OffersList.AddItem(PrisonerItem);
                    }
                }
            }

            if (Offers.Treaty != TreatyTypes.None)
            {
                CategorisedListItem TreatyHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATTREATIES"));
                TreatyHeader.CategoryID = CATEGORY_TREATY;
                TreatyHeader.Closed = Statuses[CATEGORY_TREATY];
                OffersList.AddItem(TreatyHeader);

                if (!TreatyHeader.Closed)
                {
                    switch (Offers.Treaty)
                    {
                        case TreatyTypes.Alliance:
                            {
                                CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_ALLY"));
                                TreatyItem.IndentLevel = 1;
                                TreatyItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                                TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)Offers.Treaty, 0);
                                OffersList.AddItem(TreatyItem);
                                break;
                            }
                        case TreatyTypes.MutualDefence:
                            {
                                CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_DEFENCE"));
                                TreatyItem.IndentLevel = 1;
                                TreatyItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                                TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)Offers.Treaty, 0);
                                OffersList.AddItem(TreatyItem);
                                break;
                            }
                        case TreatyTypes.NonAggression:
                            {
                                CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_NAP"));
                                TreatyItem.IndentLevel = 1;
                                TreatyItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                                TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)Offers.Treaty, 0);
                                OffersList.AddItem(TreatyItem);
                                break;
                            }
                        case TreatyTypes.Peace:
                            {
                                CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_PEACE"));
                                TreatyItem.IndentLevel = 1;
                                TreatyItem.IncludeRemoveButton = !IsAIOffer || CounterOffering;
                                TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)Offers.Treaty, 0);
                                OffersList.AddItem(TreatyItem);
                                break;
                            }
                    }
                }
            }
        }

        private void FillItems(ControlList<CategorisedListItem> ItemsList, Dictionary<int, bool> Statuses, WorkingRealm Realm, WorkingRealm OtherRealm, TradeOfferList Offers)
        {
            ItemsList.ClearItems();

            CategorisedListItem MoneyHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATMONEY"));
            MoneyHeader.CategoryID = CATEGORY_MONEY;
            MoneyHeader.Closed = Statuses[CATEGORY_MONEY];
            ItemsList.AddItem(MoneyHeader);

            if (!MoneyHeader.Closed)
            {
                int GoldLump = Realm.GetTradeGold();
                int GoldTurn = GameSystem.CurrentGame.EconomyController.GetRealmTotalIncome(Realm) - GameSystem.CurrentGame.EconomyController.GetTotalExpenses(Realm);

                GoldLump = Math.Max(0, GoldLump);
                GoldTurn = Math.Max(0, GoldTurn);

                CategorisedListItem LumpItem = new CategorisedListItem(false, GameText.CreateLocalised("TRADECATGOLD1"));
                LumpItem.HasValue = true;
                LumpItem.Value = GoldLump;
                LumpItem.IndentLevel = 1;
                LumpItem.Data = new TradeItemData(Tradecategories.GoldLump, 0, Math.Min(100, GoldLump));
                ItemsList.AddItem(LumpItem);

                CategorisedListItem TurnItem = new CategorisedListItem(false, GameText.CreateLocalised("TRADECATGOLD2"));
                TurnItem.HasValue = true;
                TurnItem.Value = GoldTurn;
                TurnItem.IndentLevel = 1;
                TurnItem.Data = new TradeItemData(Tradecategories.GoldPerTurn, 0, Math.Min(20, GoldTurn));
                ItemsList.AddItem(TurnItem);
            }

            CategorisedListItem ResourceHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATRESOURCES"));
            ResourceHeader.CategoryID = CATEGORY_RESOURCE;
            ResourceHeader.Closed = Statuses[CATEGORY_RESOURCE];
            ItemsList.AddItem(ResourceHeader);

            if (!ResourceHeader.Closed)
            {
                int ID = 0;
                foreach (ResourceData Resource in GameSystem.Data.Resources.Values)
                {
                    int ResourceLump = Realm.GetStockpiledResource(Resource);
                    int ResourceTurn = Realm.GetResourceIncome(Resource) - Realm.GetResourceExpenses(Resource);

                    ResourceLump = Math.Max(0, ResourceLump);
                    ResourceTurn = Math.Max(0, ResourceTurn);

                    if (ResourceLump > 0)
                    {
                        CategorisedListItem LumpItem = new CategorisedListItem(false, GameText.CreateLocalised(Resource.DisplayName));
                        LumpItem.HasValue = true;
                        LumpItem.Value = ResourceLump;
                        LumpItem.IndentLevel = 1;
                        LumpItem.ImageFile = "Data\\Images\\HUD\\tradeicons\\" + Resource.ResourceName + ".png";
                        LumpItem.Data = new TradeItemData(Tradecategories.ResourceLump, ID, 1);
                        ItemsList.AddItem(LumpItem);
                    }

                    if (ResourceTurn > 0)
                    {
                        GameText Text = GameText.CreateLocalised("TRADETABLE_RPT");
                        Text.AddChildText(GameText.CreateLocalised(Resource.DisplayName));

                        CategorisedListItem TurnItem = new CategorisedListItem(false, Text);
                        TurnItem.HasValue = true;
                        TurnItem.Value = ResourceTurn;
                        TurnItem.IndentLevel = 1;
                        TurnItem.ImageFile = "Data\\Images\\HUD\\tradeicons\\" + Resource.ResourceName + ".png";
                        TurnItem.Data = new TradeItemData(Tradecategories.ResourcePerTurn, ID, 1);
                        ItemsList.AddItem(TurnItem);
                    }

                    ID++;
                }
            }

            CategorisedListItem ProvinceHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATPROVINCES"));
            ProvinceHeader.CategoryID = CATEGORY_PROVINCE;
            ProvinceHeader.Closed = Statuses[CATEGORY_PROVINCE];
            ItemsList.AddItem(ProvinceHeader);

            if (!ProvinceHeader.Closed)
            {
                foreach (WorkingProvince Province in Realm.Provinces)
                {
                    if (Province.IsCapitol) continue;
                    if (Province.Occupied) continue;
                    if (Offers.HasProvince(Province)) continue;

                    CategorisedListItem ProvinceItem = new CategorisedListItem(false, GameText.CreateLocalised(Province.DisplayName));
                    ProvinceItem.IndentLevel = 1;
                    ProvinceItem.ImageFile = "Data\\Images\\Map\\Province Icons\\" + Province.Name + ".png";
                    ProvinceItem.Data = new TradeItemData(Tradecategories.Province, Province.ID, 0);
                    ItemsList.AddItem(ProvinceItem);
                }
            }

            CategorisedListItem UnitHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATPRISONERS"));
            UnitHeader.CategoryID = CATEGORY_UNIT;
            UnitHeader.Closed = Statuses[CATEGORY_UNIT];
            ItemsList.AddItem(UnitHeader);

            if (!UnitHeader.Closed)
            {
                foreach (WorkingUnit Unit in Realm.Prison.GetRealmPrisoners(OtherRealm))
                {
                    if (Offers.HasPrisoner(Unit)) continue;

                    CategorisedListItem PrisonerItem = new CategorisedListItem(false, GameText.CreateLocalised(Unit.DisplayName));
                    PrisonerItem.IndentLevel = 1;
                    PrisonerItem.ImageFile = "Data\\Images\\Units\\" + Unit.ImageFile + ".jpg";
                    PrisonerItem.Data = new TradeItemData(Tradecategories.Prisoner, Unit.ID, 0);
                    ItemsList.AddItem(PrisonerItem);
                }
            }

            CategorisedListItem TreatyHeader = new CategorisedListItem(true, GameText.CreateLocalised("TRADECATTREATIES"));
            TreatyHeader.CategoryID = CATEGORY_TREATY;
            TreatyHeader.Closed = Statuses[CATEGORY_TREATY];
            ItemsList.AddItem(TreatyHeader);

            if (!TreatyHeader.Closed)
            {
                if (Offers.Treaty == TreatyTypes.None)
                {
                    if (GameSystem.CurrentGame.AllianceController.TreatyIsPossible(Realm, OtherRealm, TreatyTypes.Peace))
                    {
                        CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_PEACE"));
                        TreatyItem.IndentLevel = 1;
                        TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)TreatyTypes.Peace, 0);
                        ItemsList.AddItem(TreatyItem);
                    }

                    if (GameSystem.CurrentGame.AllianceController.TreatyIsPossible(Realm, OtherRealm, TreatyTypes.NonAggression))
                    {
                        CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_NAP"));
                        TreatyItem.IndentLevel = 1;
                        TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)TreatyTypes.NonAggression, 0);
                        ItemsList.AddItem(TreatyItem);
                    }

                    if (GameSystem.CurrentGame.AllianceController.TreatyIsPossible(Realm, OtherRealm, TreatyTypes.MutualDefence))
                    {
                        CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_DEFENCE"));
                        TreatyItem.IndentLevel = 1;
                        TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)TreatyTypes.MutualDefence, 0);
                        ItemsList.AddItem(TreatyItem);
                    }

                    if (GameSystem.CurrentGame.AllianceController.TreatyIsPossible(Realm, OtherRealm, TreatyTypes.Alliance))
                    {
                        CategorisedListItem TreatyItem = new CategorisedListItem(false, GameText.CreateLocalised("TREATY_ALLY"));
                        TreatyItem.IndentLevel = 1;
                        TreatyItem.Data = new TradeItemData(Tradecategories.Treaty, (int)TreatyTypes.Alliance, 0);
                        ItemsList.AddItem(TreatyItem);
                    }
                }
            }
        }

        public void CloseButtonClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("CloseTradeOffer") == false) return;
            if (IsAIOffer && !CounterOffering) return;

            if (CounterOffering)
            {
                CounterOffering = false;
                CurrentTrade.SwitchSides();
                GameSystem.FireEvent("PlayerRejectedTrade", CurrentTrade);
            }

            GameForm.Visible = false;
        }

        public override void Dispose()
        {
            GameSystem.UnregisterEvent(HandleAITradeOffer, "PlayerTradeOffer");
            GameSystem.UnregisterEvent(HandlePlayerTradeOffer, "OfferTrade");
            GameSystem.UnregisterEvent(HandlePlayerTreatyOffer, "OfferTreaty");
            GameSystem.UnregisterEvent(HandlePlayerTradeOfferWithItems, "OfferTradeForItems");
            GameSystem.UnregisterEvent(HandleCloseRequest, "CloseTradeTable");
        }
    }
}
