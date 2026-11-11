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
    public delegate void AgentDelegate(WorkingAgent Agent);
    public delegate void ControlIntDelegate(UIControl Control, int Value);

    public class RelationsForm : ScriptBase
    {
        const int CATEGORY_ALLIES = 1;
        const int CATEGORY_ENEMIES = 2;
        const int CATEGORY_STOCKPILE = 3;
        const int CATEGORY_INCOME = 4;

        class ColouredTextData
        {
            public GameText Text;
            public Color TextColour;
            public int TurnCount;

            public ColouredTextData(GameText Text, Color TextColour)
            {
                this.Text = Text;
                this.TextColour = TextColour;
            }
        }

        class TradeData
        {
            public OngoingTrade Trade;
            public WorkingAgent Agent;
        }

        class AgentOverviewData
        {
            public ControlImage BGImage;
            public ControlImage FaceImage;
            public ControlImage FlagImage;
            public ControlButton CancelButton;
            public ControlText TurnText;
            public ControlImage TurnBG;
            public ControlImage FreeBG;
            public ControlText FreeText;
            public Sovereignty Gamesystem;
            public WorkingAgent Agent;

            public event AgentDelegate OnAgentCancel;

            public AgentOverviewData(Sovereignty Gamesystem, ControlImage BGImage)
            {
                this.BGImage = BGImage;
                this.Gamesystem = Gamesystem;

                FaceImage = new ControlImage(Gamesystem);
                FaceImage.SetBounds(0, 0, BGImage.Sprite.Bounds.Width, BGImage.Sprite.Bounds.Height, UIUnits.PixelScaled);
                FaceImage.SetImageFile("Data\\Images\\Units\\herohire\\hero13frame.png");
                FaceImage.MouseInputType = MouseInputTypes.Forced;
                BGImage.AddChild(FaceImage);

                FlagImage = new ControlImage(Gamesystem);
                FlagImage.SetBounds(0, 0, BGImage.Sprite.Bounds.Width * 0.3f, BGImage.Sprite.Bounds.Height * 0.3f, UIUnits.PixelScaled);
                FaceImage.AddChild(FlagImage);

                CancelButton = new ControlButton(Gamesystem);
                CancelButton.SetBounds(0, 0, BGImage.Sprite.Bounds.Width * 0.3f, BGImage.Sprite.Bounds.Height * 0.3f, UIUnits.PixelScaled);
                CancelButton.SetAnchor(AnchorPoints.TopRight);
                CancelButton.SetImageFiles("Data\\Images\\HUD\\purchase\\cancel_normal.png", "Data\\Images\\HUD\\purchase\\cancel_mouseover.png", "Data\\Images\\HUD\\purchase\\cancel_pressed.png");
                CancelButton.OnClick += CancelButton_OnClick;
                FaceImage.AddChild(CancelButton);

                // Shared lower badge band for turn count / FREE (chin area, not forehead)
                float BadgeY = BGImage.Sprite.Bounds.Height * 0.68f;
                float BadgeH = BGImage.Sprite.Bounds.Height * 0.28f;
                float BadgeW = BGImage.Sprite.Bounds.Width * 0.72f;

                TurnBG = new ControlImage(Gamesystem);
                TurnBG.SetBounds((BGImage.Sprite.Bounds.Width - BadgeW) * 0.5f, BadgeY, BadgeW, BadgeH, UIUnits.PixelScaled);
                TurnBG.SetImageFile("Data\\Images\\HUD\\bg.png");
                TurnBG.Sprite.SetAlpha(0.75f);
                FaceImage.AddChild(TurnBG);

                TurnText = new ControlText(Gamesystem);
                TurnText.SetBounds(0, 0, TurnBG.Sprite.Bounds.Width, TurnBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
                TurnText.SetFontSize((int)(BGImage.Sprite.Bounds.Height * 0.2f), UIUnits.PixelScaled);
                TurnText.FontName = "Trebuchet MS";
                TurnText.Bold = true;
                TurnText.TextAnchor = AnchorPoints.Middle;
                TurnText.Colour = Color.FromArgb(255, 238, 161);
                TurnBG.AddChild(TurnText);

                FreeBG = new ControlImage(Gamesystem);
                FreeBG.SetBounds((BGImage.Sprite.Bounds.Width - BadgeW) * 0.5f, BadgeY, BadgeW, BadgeH, UIUnits.PixelScaled);
                FreeBG.SetImageFile("Data\\Images\\HUD\\bg.png");
                FreeBG.MouseInputType = MouseInputTypes.None;
                FreeBG.Sprite.SetAlpha(0.75f);
                FaceImage.AddChild(FreeBG);

                FreeText = new ControlText(Gamesystem);
                FreeText.SetBounds(0, 0, FreeBG.Sprite.Bounds.Width, FreeBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
                FreeText.SetFontSize((int)(BGImage.Sprite.Bounds.Height * 0.18f), UIUnits.PixelScaled);
                FreeText.FontName = "Trebuchet MS";
                FreeText.Bold = true;
                FreeText.TextAnchor = AnchorPoints.Middle;
                FreeText.Colour = Color.FromArgb(180, 255, 180);
                FreeText.SetTextData(GameText.CreateFromLiteral("FREE"));
                FreeText.MouseInputType = MouseInputTypes.None;
                FreeBG.AddChild(FreeText);
            }

            void CancelButton_OnClick(UIControl Control)
            {
                if (OnAgentCancel != null) OnAgentCancel(Agent);
            }

            public void Update(WorkingAgent Agent)
            {
                this.Agent = Agent;

                if (Agent == null)
                {
                    BGImage.Visible = false;
                    return;
                }

                BGImage.Visible = true;

                int Turns = Agent.GetTurnsToHome();
                bool IsFree = Turns == 0 && Agent.HostRealm == Agent.OwnerRealm;

                CancelButton.Visible = true;
                TurnBG.Visible = true;
                FreeBG.Visible = false;
                FlagImage.Visible = true;
                FlagImage.SetImageFile("Data\\Images\\HUD\\Flags\\" + Agent.HostRealm.FlagFilename + ".png");

                if (IsFree)
                {
                    TurnBG.Visible = false;
                    CancelButton.Visible = false;
                    FreeBG.Visible = true;
                }
                else if (Turns == 0 && Agent.HostRealm != Agent.OwnerRealm)
                {
                    TurnText.SetTextData(GameText.CreateLocalised("FORMAT_INFINITE"));
                }
                else if (Turns == 0)
                {
                    TurnBG.Visible = false;
                    CancelButton.Visible = false;
                }
                else
                {
                    TurnText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Turns));
                }

                GameText Tip = BuildAgentAssignmentTooltip(Gamesystem, Agent, IsFree);
                FaceImage.SetTooltip(Tip);
                BGImage.SetTooltip(Tip);
            }
        }

        class NewAgentBoxData
        {
            public Sovereignty GameSystem;
            public ControlImage BGImage;
            public ControlImage BarImage;
            public ControlText TitleText;
            public ControlText MessageText;
            public ControlButton NewActionbutton1;
            public ControlButton NewActionbutton2;
            public ControlButton NewActionbutton3;
            public ControlButton NewActionbutton4;
            public ControlButton NewActionButton5;

            public event ControlIntDelegate OnButtonClicked;

            public NewAgentBoxData(Sovereignty GameSystem, ControlImage BGImage)
            {
                this.GameSystem = GameSystem;
                this.BGImage = BGImage;

                float BarH = Math.Max(16f, BGImage.Sprite.Bounds.Height * 0.22f);
                // Keep icon size close to the old ~230px-wide card look (was width * 0.15 ≈ 34px).
                float ButtonSize = Math.Min(36f, Math.Max(28f, (BGImage.Sprite.Bounds.Height - BarH) * 0.7f));
                float ButtonGap = ButtonSize * 1.2f;
                float ButtonsTop = BarH + Math.Max(2f, (BGImage.Sprite.Bounds.Height - BarH - ButtonSize) * 0.5f);
                float ButtonsLeft = (BGImage.Sprite.Bounds.Width - ButtonGap * 4f - ButtonSize) * 0.5f;

                BarImage = new ControlImage(GameSystem);
                BarImage.SetBounds(0, 0, BGImage.Sprite.Bounds.Width, BarH, UIUnits.PixelScaled);
                BarImage.SetImageFile("Data\\Images\\HUD\\listselect.png");
                BGImage.AddChild(BarImage);

                TitleText = new ControlText(GameSystem);
                TitleText.SetBounds(BarImage.Sprite.Bounds.Width * 0.01f, 0, BarImage.Sprite.Bounds.Width, BarH, UIUnits.PixelScaled);
                TitleText.SetFontSize((int)(BarH * 0.7f), UIUnits.PixelScaled);
                TitleText.FontName = "Trebuchet MS";
                TitleText.Bold = true;
                TitleText.Colour = Color.FromArgb(255, 238, 161);
                TitleText.SetTextData(GameText.CreateLocalised("COVERT_NEW"));
                BarImage.AddChild(TitleText);

                MessageText = new ControlText(GameSystem);
                MessageText.SetBounds(0, BarH, BGImage.Sprite.Bounds.Width, BGImage.Sprite.Bounds.Height - BarH, UIUnits.PixelScaled);
                MessageText.SetFontSize((int)Math.Max(11f, BGImage.Sprite.Bounds.Height * 0.14f), UIUnits.PixelScaled);
                MessageText.FontName = "Trebuchet MS";
                MessageText.Bold = true;
                MessageText.TextAnchor = AnchorPoints.Middle;
                MessageText.Colour = Color.FromArgb(255, 238, 161);
                BGImage.AddChild(MessageText);

                NewActionbutton1 = CreateCovertActionButton(ButtonsLeft, ButtonsTop, ButtonSize, 1,
                    "button_sendambassador", "TT_COVERT1");
                NewActionbutton2 = CreateCovertActionButton(ButtonsLeft + ButtonGap, ButtonsTop, ButtonSize, 2,
                    "button_propaganda", "TT_COVERT2");
                NewActionbutton3 = CreateCovertActionButton(ButtonsLeft + ButtonGap * 2f, ButtonsTop, ButtonSize, 3,
                    "button_wormtongue", "TT_COVERT3");
                NewActionbutton4 = CreateCovertActionButton(ButtonsLeft + ButtonGap * 3f, ButtonsTop, ButtonSize, 4,
                    "button_foreignaid", "TT_COVERT4");
                NewActionButton5 = CreateCovertActionButton(ButtonsLeft + ButtonGap * 4f, ButtonsTop, ButtonSize, 5,
                    "button_provocateur", "TT_COVERT5");
            }

            private ControlButton CreateCovertActionButton(float X, float Y, float Size, int Id, string ImageBase, string TooltipKey)
            {
                ControlButton Button = new ControlButton(GameSystem);
                Button.SetBounds(X, Y, Size, Size, UIUnits.PixelScaled);
                Button.SetImageFiles(
                    "Data\\Images\\HUD\\Info\\" + ImageBase + "_normal.png",
                    "Data\\Images\\HUD\\Info\\" + ImageBase + "_mouseover.png",
                    "Data\\Images\\HUD\\Info\\" + ImageBase + "_pressed.png");
                Button.SetTooltip(GameText.CreateLocalised(TooltipKey));
                Button.OnMouseEnter += HandleButtonEnter;
                Button.OnClick += HandleButtonClick;
                Button.CustomData = Id;
                BGImage.AddChild(Button);
                return Button;
            }

            private void HandleButtonClick(UIControl Control)
            {
                int ID = (int)Control.CustomData;
                if (OnButtonClicked != null) OnButtonClicked(Control, ID);
            }

            private void HandleButtonEnter(UIControl Control)
            {
                int ID = (int)Control.CustomData;
                GameSystem.FireEvent("CovertButtonMouseover", ID);
            }

            public void Update(WorkingRealm Realm)
            {
                if (Realm == GameSystem.CurrentGame.PlayerRealm)
                {
                    NewActionbutton1.Visible = false;
                    NewActionbutton2.Visible = false;
                    NewActionbutton3.Visible = false;
                    NewActionbutton4.Visible = false;
                    NewActionButton5.Visible = false;
                    MessageText.Visible = true;
                    MessageText.SetTextData(GameText.CreateLocalised("COVERT_SELF"));
                }
                else if (GameSystem.CurrentGame.PlayerRealm.GetCovertAgent() == null)
                {
                    NewActionbutton1.Visible = false;
                    NewActionbutton2.Visible = false;
                    NewActionbutton3.Visible = false;
                    NewActionbutton4.Visible = false;
                    NewActionButton5.Visible = false;
                    MessageText.Visible = true;
                    MessageText.SetTextData(GameText.CreateLocalised("TT_NOSPY"));
                }
                else
                {
                    //improve relations - not available if already doing this action
                    NewActionbutton1.Visible = GameSystem.CurrentGame.PlayerRealm.Agents.Count(x => x.TargetRealm == Realm && x.CurrentMode == AgentModes.ImproveRelations) == 0;
                    
                    NewActionbutton2.Visible = true;
                    NewActionbutton3.Visible = true;
                    NewActionbutton4.Visible = true;
                    NewActionButton5.Visible = true;
                    MessageText.Visible = false;
                }
            }
        }

        class ActiveAgentBoxData
        {
            public Sovereignty GameSystem;
            public ControlImage BGImage;
            public ControlImage BarImage;
            public ControlText TitleText;
            public ControlText MessageText;
            public ControlImage FlagImage;
            public ControlButton CancelButton;
            public WorkingAgent Agent;

            public event AgentDelegate OnAgentCancel;

            public ActiveAgentBoxData(Sovereignty GameSystem, ControlImage BGImage)
            {
                this.GameSystem = GameSystem;
                this.BGImage = BGImage;

                float BarH = Math.Max(18f, BGImage.Sprite.Bounds.Height * 0.28f);

                BarImage = new ControlImage(GameSystem);
                BarImage.SetBounds(0, 0, BGImage.Sprite.Bounds.Width, BarH, UIUnits.PixelScaled);
                BarImage.SetImageFile("Data\\Images\\HUD\\listselect.png");
                BGImage.AddChild(BarImage);

                TitleText = new ControlText(GameSystem);
                TitleText.SetBounds(BarImage.Sprite.Bounds.Width * 0.02f, 0, BarImage.Sprite.Bounds.Width * 0.55f, BarH, UIUnits.PixelScaled);
                TitleText.SetFontSize((int)(BarH * 0.55f), UIUnits.PixelScaled);
                TitleText.FontName = "Trebuchet MS";
                TitleText.Bold = true;
                TitleText.TextAnchor = AnchorPoints.Left;
                TitleText.Colour = Color.FromArgb(255, 238, 161);
                BarImage.AddChild(TitleText);

                MessageText = new ControlText(GameSystem);
                MessageText.SetBounds(4f, BarH + 2f, BGImage.Sprite.Bounds.Width - 8f, BGImage.Sprite.Bounds.Height - BarH - 4f, UIUnits.PixelScaled);
                MessageText.SetFontSize((int)Math.Max(10f, BGImage.Sprite.Bounds.Height * 0.14f), UIUnits.PixelScaled);
                MessageText.FontName = "Trebuchet MS";
                MessageText.Bold = true;
                MessageText.TextAnchor = AnchorPoints.TopLeft;
                MessageText.Colour = Color.FromArgb(255, 238, 161);
                BGImage.AddChild(MessageText);

                float IconSize = BarH * 0.85f;
                FlagImage = new ControlImage(GameSystem);
                FlagImage.SetBounds(IconSize * 1.15f, (BarH - IconSize) * 0.5f, IconSize, IconSize, UIUnits.PixelScaled);
                FlagImage.SetAnchor(AnchorPoints.TopRight);
                BarImage.AddChild(FlagImage);

                CancelButton = new ControlButton(GameSystem);
                CancelButton.SetBounds(2f, (BarH - IconSize) * 0.5f, IconSize, IconSize, UIUnits.PixelScaled);
                CancelButton.SetAnchor(AnchorPoints.TopRight);
                CancelButton.SetImageFiles("Data\\Images\\HUD\\purchase\\cancel_normal.png", "Data\\Images\\HUD\\purchase\\cancel_mouseover.png", "Data\\Images\\HUD\\purchase\\cancel_pressed.png");
                CancelButton.OnClick += CancelButton_OnClick;
                BarImage.AddChild(CancelButton);
            }

            private void CancelButton_OnClick(UIControl Control)
            {
                if (OnAgentCancel != null) OnAgentCancel(Agent);
            }

            public void Update(WorkingAgent Agent)
            {
                this.Agent = Agent;

                if (Agent == null)
                {
                    BGImage.Visible = false;
                }
                else
                {
                    BGImage.Visible = true;

                    FlagImage.SetImageFile("Data\\Images\\HUD\\Flags\\" + Agent.HostRealm.FlagFilename + ".png");

                    GameText Text = null;

                    if (Agent.TurnsLeft == 0)
                    {
                        Text = GameText.CreateLocalised("AGENT_DETAILINF");
                    }
                    else
                    {
                        Text = GameText.CreateLocalised("AGENT_DETAIL", Agent.TurnsLeft);
                    }

                    MessageText.SetTextData(Text);
                    
                    switch (Agent.CurrentMode)
                    {
                        case AgentModes.CarryTradeOffer:
                            {
                                CancelButton.Visible = false;
                                TitleText.SetTextData(GameText.CreateLocalised("AGENT_TRADE"));
                                Text.AddChildText(GameText.CreateLocalised("MISSION_TRADE"));
                                Text.AddChildText(GameText.CreateLocalised("ACTION_TRAVEL"));
                                break;
                            }
                        case AgentModes.DamageForeignRelations:
                            {
                                CancelButton.Visible = true;
                                TitleText.SetTextData(GameText.CreateLocalised("AGENT_DAMAGE"));

                                GameText MissionText = GameText.CreateLocalised("MISSION_DAMAGE");
                                MissionText.AddChildText(GameText.CreateLocalised(Agent.TargetRealm.DisplayName));
                                Text.AddChildText(MissionText);

                                if (Agent.TurnsLeft > 0) Text.AddChildText(GameText.CreateLocalised("ACTION_TRAVEL"));
                                else Text.AddChildText(GameText.CreateLocalised("ACTION_ACTIVE"));
                                
                                break;
                            }
                        case AgentModes.Idle:
                            {
                                CancelButton.Visible = false;
                                TitleText.SetTextData(GameText.CreateLocalised("AGENT_IDLE"));

                                if (Agent.TurnsLeft > 0)
                                {
                                    Text.AddChildText(GameText.CreateLocalised("MISSION_RETURN"));
                                    Text.AddChildText(GameText.CreateLocalised("ACTION_TRAVEL"));
                                }
                                else
                                {
                                    Text.AddChildText(GameText.CreateLocalised("AGENT_IDLE"));
                                    Text.AddChildText(GameText.CreateLocalised("AGENT_IDLE"));
                                }
                                break;
                            }
                        case AgentModes.ImproveForeignRelations:
                            {
                                CancelButton.Visible = true;
                                TitleText.SetTextData(GameText.CreateLocalised("AGENT_IMPROVE"));

                                GameText MissionText = GameText.CreateLocalised("MISSION_IMPROVE");
                                MissionText.AddChildText(GameText.CreateLocalised(Agent.TargetRealm.DisplayName));
                                Text.AddChildText(MissionText);

                                if (Agent.TurnsLeft > 0) Text.AddChildText(GameText.CreateLocalised("ACTION_TRAVEL"));
                                else Text.AddChildText(GameText.CreateLocalised("ACTION_ACTIVE"));

                                break;
                            }
                        case AgentModes.ImproveRelations:
                            {
                                CancelButton.Visible = true;
                                TitleText.SetTextData(GameText.CreateLocalised("AGENT_DIPLOMAT"));

                                Text.AddChildText(GameText.CreateLocalised("MISSION_DIPLOMAT"));

                                if (Agent.TurnsLeft > 0) Text.AddChildText(GameText.CreateLocalised("ACTION_TRAVEL"));
                                else Text.AddChildText(GameText.CreateLocalised("ACTION_ACTIVE"));

                                break;
                            }
                        case AgentModes.Military:
                            {
                                CancelButton.Visible = true;
                                TitleText.SetTextData(GameText.CreateLocalised("AGENT_SPY"));

                                Text.AddChildText(GameText.CreateLocalised("MISSION_SPY"));

                                if (Agent.TurnsLeft > 0) Text.AddChildText(GameText.CreateLocalised("ACTION_TRAVEL"));
                                else Text.AddChildText(GameText.CreateLocalised("ACTION_ACTIVE"));

                                break;
                            }
                        case AgentModes.Incite:
                            {
                                CancelButton.Visible = true;
                                TitleText.SetTextData(GameText.CreateLocalised("AGENT_INCITE"));

                                Text.AddChildText(GameText.CreateLocalised("MISSION_INCITE"));

                                if (Agent.TurnsLeft > 0) Text.AddChildText(GameText.CreateLocalised("ACTION_TRAVEL"));
                                else Text.AddChildText(GameText.CreateLocalised("ACTION_ACTIVE"));

                                break;
                            }
                    }
                }
            }
        }

        private WorkingRealm m_DiplomacyRealm;
        private WorkingRealm DiplomacyRealm
        {
            get { return m_DiplomacyRealm; }
            set
            {
                m_DiplomacyRealm = value;

                GameSystem.FireEvent("DiplomacyRealmChanged", value);
            }
        }

        ControlList<WorkingRealm> RealmList;
        ControlList<WorkingRealm> RelationsList;
        ControlList<CategorisedListItem> AlliesList;
        ControlList<CategorisedListItem> ResourcesList;
        ControlList<TradeData> TradeList;
        ControlList<WorkingRealm> CovertList;
        ControlRealmCard SelectedRealmCard;
        ControlList<WorkingUnit> AuxiliariesList;
        ControlUnitCard AuxPreviewCard;
        List<WorkingUnit> AuxPreviewUnits = new List<WorkingUnit>();
        // Click-to-pin (same idea as InfoCardPanel / tactical SelectedCard): stays until click again or realm change.
        WorkingUnit AuxPinnedUnit;

        bool AlliesClosed;
        bool EnemiesClosed;
        bool StockpileClosed;
        bool IncomeClosed;

        List<AgentOverviewData> AgentOverviewControls;
        NewAgentBoxData NewAgentBox;
        List<ActiveAgentBoxData> ActiveAgentBoxes;
        private AgentModes CovertMode;

        public void FormLoaded(UIControl Control)
        {
            GameSystem.RegisterEvent(HandleOpenRequest, "OpenRelations");
            GameSystem.RegisterEvent(HandleTradeSent, "TradeOfferSent");
            GameSystem.RegisterEvent(HandleCloseRequest, "CloseRelations");

            ControlImage MenuBG = GameForm.GetControlByName<ControlImage>("PurchaseBG");

            switch (GameSystem.CurrentGame.PlayerRealm.Panel)
            {
                case SpellSchools.Death: MenuBG.SetImageFile("Data\\Images\\HUD\\Purchase\\bg_death_16x9.png"); break;
                case SpellSchools.Illusion: MenuBG.SetImageFile("Data\\Images\\HUD\\Purchase\\bg_illusion_16x9.png"); break;
                case SpellSchools.Nature: MenuBG.SetImageFile("Data\\Images\\HUD\\Purchase\\bg_nature_16x9.png"); break;
                case SpellSchools.War: MenuBG.SetImageFile("Data\\Images\\HUD\\Purchase\\bg_war_16x9.png"); break;
            }

            RealmList = GameForm.GetControlByName<ControlList<WorkingRealm>>("TradeOfferRealmList");
            RealmList.ManualUpdate = true;
            RealmList.SelectionEnabled = true;
            RealmList.OnListItemUpdated += RealmList_OnListItemUpdated;
            GameForm.GetControlByName<ControlImage>("TradeOfferRealmsBG").Visible = true;
            
            CovertList = GameForm.GetControlByName<ControlList<WorkingRealm>>("CovertTargetsList");
            CovertList.ManualUpdate = true;
            CovertList.OnListItemUpdated += RealmList_OnListItemUpdated;

            // Bottom-left: realm card + auxiliaries list for the selected FA realm.
            ControlImage RealmInfoBG = GameForm.GetControlByName<ControlImage>("BGImageRealmInfo");
            if (RealmInfoBG != null)
            {
                SelectedRealmCard = new ControlRealmCard(GameSystem);
                RealmInfoBG.AddChild(SelectedRealmCard);

                AuxiliariesList = new ControlList<WorkingUnit>(GameSystem);
                AuxiliariesList.SetVisibleItems(1);
                AuxiliariesList.ManualUpdate = true;
                AuxiliariesList.SelectionEnabled = true;
                AuxiliariesList.HighlightEnabled = true;
                AuxiliariesList.OnListItemUpdated += AuxiliariesList_OnListItemUpdated;
                AuxiliariesList.OnHoverItemChanged += AuxiliariesList_OnHoverItemChanged;
                AuxiliariesList.OnItemClicked += AuxiliariesList_OnItemClicked;
                RealmInfoBG.AddChild(AuxiliariesList);
                LayoutRealmInfoPanel();

                // Local unit card: RelationsForm is fullscreen above InfoCardPanel, so HUD hover cards are hidden.
                // Center of FA screen (between diplomacy/realm-info and trade/covert columns).
                AuxPreviewCard = new ControlUnitCard(GameSystem);
                AuxPreviewCard.SetSize(265, 443, UIUnits.Pixel);
                AuxPreviewCard.SetAnchor(AnchorPoints.Middle);
                AuxPreviewCard.SetPosition(0, 0, UIUnits.Pixel);
                AuxPreviewCard.Visible = false;
                AuxPreviewCard.MouseInputType = MouseInputTypes.None;
                AuxPreviewCard.OnMouseEnter += AuxPreviewCard_OnMouseEnter;
                GameForm.AddChild(AuxPreviewCard);
            }

            ResetRealmList();

            ControlImage AlliesBG = GameForm.GetControlByName<ControlImage>("DiploBG1");
            ControlImage ResourceBG = GameForm.GetControlByName<ControlImage>("TradeBG1");
            ControlImage TradeBG = GameForm.GetControlByName<ControlImage>("TradeBG2");

            AlliesList = new ControlList<CategorisedListItem>(GameSystem);
            AlliesBG.AddChild(AlliesList);
            AlliesList.SetBounds(0, 0, AlliesBG.Sprite.Bounds.Width, AlliesBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            AlliesList.SetVisibleItems(12);
            AlliesList.ManualUpdate = true;
            AlliesList.SelectionEnabled = false;
            AlliesList.HighlightEnabled = false;
            AlliesList.OnListItemUpdated += OnCategorisedListItemUpdated;
            AlliesList.OnItemClicked += AlliesList_OnItemClicked;

            ResourcesList = new ControlList<CategorisedListItem>(GameSystem);
            ResourceBG.AddChild(ResourcesList);
            ResourcesList.SetBounds(0, 0, ResourceBG.Sprite.Bounds.Width, ResourceBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            ResourcesList.SetVisibleItems(12);
            ResourcesList.ManualUpdate = true;
            ResourcesList.SelectionEnabled = false;
            ResourcesList.HighlightEnabled = false;
            ResourcesList.OnListItemUpdated += OnCategorisedListItemUpdated;
            ResourcesList.OnItemClicked += ResourcesList_OnItemClicked;

            TradeList = new ControlList<TradeData>(GameSystem);
            TradeBG.AddChild(TradeList);
            TradeList.SetBounds(0, 0, TradeBG.Sprite.Bounds.Width, TradeBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            TradeList.SetVisibleItems(4);
            TradeList.ManualUpdate = true;
            TradeList.SelectionEnabled = false;
            TradeList.HighlightEnabled = false;
            TradeList.OnListItemUpdated += TradeList_OnListItemUpdated;

            RelationsList = GameForm.GetControlByName<ControlList<WorkingRealm>>("RelationList");
            RelationsList.SelectionEnabled = false;
            RelationsList.ManualUpdate = true;
            RelationsList.OnListItemUpdated += RelationsList_OnListItemUpdated;
            RelationsList.OnItemClicked += RelationsList_OnItemClicked;

            AgentOverviewControls = new List<AgentOverviewData>();
            for (int i = 1; i <= 7; i++)
            {
                AgentOverviewData Data = new AgentOverviewData(GameSystem, GameForm.GetControlByName<ControlImage>("AgentOverviewBG" + i));
                Data.OnAgentCancel += Data_OnAgentCancel;
                AgentOverviewControls.Add(Data);
            }

            NewAgentBox = new NewAgentBoxData(GameSystem, GameForm.GetControlByName<ControlImage>("AgentBG1"));
            NewAgentBox.OnButtonClicked += NewAgentBox_OnButtonClicked;

            ActiveAgentBoxes = new List<ActiveAgentBoxData>();
            for (int i = 2; i <= 6; i++)
            {
                ActiveAgentBoxData Data = new ActiveAgentBoxData(GameSystem, GameForm.GetControlByName<ControlImage>("AgentBG" + i));
                Data.OnAgentCancel += Data_OnAgentCancel;
                ActiveAgentBoxes.Add(Data);
            }

            GameSystem.OnViewportChanged += GameSystem_OnViewportChanged;
            SetBGSizes();
        }

        void GameSystem_OnViewportChanged()
        {
            SetBGSizes();
        }

        private void SetBGSizes()
        {
            ControlImage BGImage = GameForm.GetControlByName<ControlImage>("PurchaseBG");

            Rectangle Viewport = GameSystem.GetViewport();

            float ScreenWidth = ((float)Viewport.Width / (float)Viewport.Height) * 100;
            float ScreenHeight = ScreenWidth / 1.6f;

            if (ScreenWidth > 160)
            {
                BGImage.SetSize(ScreenWidth, ScreenHeight, UIUnits.Internal);
            }
            else
            {
                BGImage.SetSize(160, 100, UIUnits.Internal);
            }
        }

        private void HandleCloseRequest(string EventName, params object[] Args)
        {
            CloseButtonClick(null);
        }

        void RelationsList_OnItemClicked(UIControl Control)
        {
            WorkingRealm Realm = RelationsList.ClickedItem.Data;
            if (Realm != null)
            {
                DiplomacyRealm = Realm;
                UpdateAll();
                GameSystem.FireEvent("DiplomacyRealmChanged", DiplomacyRealm);
            }
        }

        private void HandleTradeSent(string EventName, params object[] Args)
        {
            UpdateAll();
        }

        void NewAgentBox_OnButtonClicked(UIControl Control, int Value)
        {
            switch (Value)
            {
                case 1:
                    {
                        if (!GameSystem.UIActionAllowed("CovertNew1")) return;

                        //improve relations
                        WorkingAgent Agent = GetFreeAgent();
                        if (Agent == null) return;

                        Agent.Send(DiplomacyRealm, DiplomacyRealm, AgentModes.ImproveRelations);
                        GameSystem.FireEvent("CovertSpySent");
                        UpdateAll();
                        break;
                    }
                case 2:
                    {
                        if (!GameSystem.UIActionAllowed("CovertNew2")) return;

                        ControlImage ListBG = GameForm.GetControlByName<ControlImage>("CovertTargetsBG");
                        ControlList<WorkingRealm> List = GameForm.GetControlByName<ControlList<WorkingRealm>>("CovertTargetsList");

                        if (ListBG.Visible && CovertMode == AgentModes.ImproveForeignRelations)
                        {
                            ListBG.Visible = false;
                        }
                        else
                        {
                            List.ClearItems();
                            foreach (WorkingRealm Realm in GameSystem.CurrentGame.AllRealms.Values.Where(x => !x.RealmIsDead && x != DiplomacyRealm && x.Name != "Rebels"))
                            {
                                if (GameSystem.CurrentGame.PlayerRealm.Agents.Count(x => x.HostRealm == DiplomacyRealm && x.TargetRealm == Realm && x.CurrentMode == AgentModes.ImproveForeignRelations) == 0)
                                {
                                    List.AddItem(Realm);
                                }
                            }

                            CovertMode = AgentModes.ImproveForeignRelations;

                            ListBG.Visible = true;
                            ListBG.SetPosition(Control.Sprite.Bounds.X, Control.Sprite.Bounds.Y + 25 - ListBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
                        }

                        break;
                    }
                case 3:
                    {
                        if (!GameSystem.UIActionAllowed("CovertNew3")) return;

                        ControlImage ListBG = GameForm.GetControlByName<ControlImage>("CovertTargetsBG");
                        ControlList<WorkingRealm> List = GameForm.GetControlByName<ControlList<WorkingRealm>>("CovertTargetsList");

                        if (ListBG.Visible && CovertMode == AgentModes.DamageForeignRelations)
                        {
                            ListBG.Visible = false;
                        }
                        else
                        {
                            List.ClearItems();
                            foreach (WorkingRealm Realm in GameSystem.CurrentGame.AllRealms.Values.Where(x => !x.RealmIsDead && x != DiplomacyRealm && x.Name != "Rebels"))
                            {
                                if (GameSystem.CurrentGame.PlayerRealm.Agents.Count(x => x.HostRealm == DiplomacyRealm && x.TargetRealm == Realm && x.CurrentMode == AgentModes.DamageForeignRelations) == 0)
                                {
                                    List.AddItem(Realm);
                                }
                            }

                            CovertMode = AgentModes.DamageForeignRelations;

                            ListBG.Visible = true;
                            ListBG.SetPosition(Control.Sprite.Bounds.X, Control.Sprite.Bounds.Y + 25 - ListBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
                        }

                        break;
                    }
                case 4:
                    {
                        if (!GameSystem.UIActionAllowed("CovertNew4")) return;

                        //spy on military
                        WorkingAgent Agent = GetFreeAgent();
                        if (Agent == null) return;

                        Agent.Send(DiplomacyRealm, DiplomacyRealm, AgentModes.Military);
                        GameSystem.FireEvent("CovertSpySent");
                        UpdateAll();

                        break;
                    }
                case 5:
                    {
                        if (!GameSystem.UIActionAllowed("CovertNew5")) return;

                        //spy on military
                        WorkingAgent Agent = GetFreeAgent();
                        if (Agent == null) return;

                        Agent.Send(DiplomacyRealm, DiplomacyRealm, AgentModes.Incite);
                        GameSystem.FireEvent("CovertSpySent");
                        UpdateAll();

                        break;
                    }
            }
        }

        public void CovertTargetClicked(UIControl Control)
        {
            ControlImage ListBG = GameForm.GetControlByName<ControlImage>("CovertTargetsBG");
            ControlList<WorkingRealm> List = GameForm.GetControlByName<ControlList<WorkingRealm>>("CovertTargetsList");

            WorkingRealm TargetRealm = List.ClickedItem.Data;

            ListBG.Visible = false;

            WorkingAgent Agent = GetFreeAgent();
            if (Agent == null) return;

            Agent.Send(DiplomacyRealm, TargetRealm, CovertMode);
            GameSystem.FireEvent("CovertSpySent");
            UpdateAll();
        }

        private WorkingAgent GetFreeAgent()
        {
            WorkingAgent Result = GameSystem.CurrentGame.PlayerRealm.GetTradeAgent();

            if (Result == null)
            {
                MessageBoxData Msg = new MessageBoxData();
                Msg.DisplayType = MessageBoxType.Info;
                Msg.MsgType = MessageType.GenericInfo;

                Msg.CaptionText = GameText.CreateLocalised("AMBASSADOR_FAIL_TITLE");
                Msg.MessageText = GameText.CreateLocalised("AMBASSADOR_FAIL_TEXT");

                GameSystem.MessageHandler.ShowMessage(Msg);
            }

            return Result;
        }

        void Data_OnAgentCancel(WorkingAgent Agent)
        {
            if (GameSystem.UIActionAllowed("RecallAgent") == false) return;

            Agent.Recall();
            UpdateAll();
        }

        void TradeList_OnListItemUpdated(ListItem<RelationsForm.TradeData> Item)
        {
            //background image
            ControlImage BGImage = new ControlImage(GameSystem);
            BGImage.SetBounds(Item.Container.Sprite.Bounds.Height * 0.02f, Item.Container.Sprite.Bounds.Width * 0.02f, Item.Container.Sprite.Bounds.Width * 0.96f, Item.Container.Sprite.Bounds.Height * 0.96f, UIUnits.PixelScaled);
            BGImage.SetImageFile("Data\\Images\\HUD\\bg.png");
            Item.Container.AddChild(BGImage);

            //title bar image
            ControlImage TitleBarBG = new ControlImage(GameSystem);
            TitleBarBG.SetBounds(Item.Container.Sprite.Bounds.Height * 0.02f, Item.Container.Sprite.Bounds.Width * 0.02f, Item.Container.Sprite.Bounds.Width * 0.96f, Item.Container.Sprite.Bounds.Height * 0.20f, UIUnits.PixelScaled);
            TitleBarBG.SetImageFile("Data\\Images\\HUD\\listselect.png");
            Item.Container.AddChild(TitleBarBG);

            //title bar left text
            ControlText LeftText = new ControlText(GameSystem);
            LeftText.SetBounds(0, 0, TitleBarBG.Sprite.Bounds.Width, TitleBarBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            LeftText.SetFontSize((int)(TitleBarBG.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
            LeftText.FontName = "Trebuchet MS";
            LeftText.TextAnchor = AnchorPoints.Left;
            LeftText.Colour = Color.FromArgb(255, 238, 161);
            LeftText.SetTextData(GameText.CreateLocalised(GameSystem.CurrentGame.PlayerRealm.DisplayName));
            TitleBarBG.AddChild(LeftText);

            //title bar right text
            ControlText RightText = new ControlText(GameSystem);
            RightText.SetBounds(0, 0, TitleBarBG.Sprite.Bounds.Width, TitleBarBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            RightText.SetFontSize((int)(TitleBarBG.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
            RightText.FontName = "Trebuchet MS";
            RightText.TextAnchor = AnchorPoints.Right;
            RightText.Colour = Color.FromArgb(255, 238, 161);
            TitleBarBG.AddChild(RightText);

            //list for our items
            ControlList<ColouredTextData> OurItemsList = new ControlList<ColouredTextData>(GameSystem);
            OurItemsList.SetBounds(BGImage.Sprite.Bounds.Width * 0.01f, TitleBarBG.Sprite.Bounds.Height, BGImage.Sprite.Bounds.Width * 0.45f, BGImage.Sprite.Bounds.Height - TitleBarBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            OurItemsList.SetVisibleItems(5);
            OurItemsList.ManualUpdate = true;
            OurItemsList.SelectionEnabled = false;
            OurItemsList.HighlightEnabled = false;
            OurItemsList.OnListItemUpdated += TradeListItemUpdated;
            OurItemsList.SetAnchor(AnchorPoints.TopLeft);
            BGImage.AddChild(OurItemsList);

            //list for their items
            ControlList<ColouredTextData> TheirItemsList = new ControlList<ColouredTextData>(GameSystem);
            TheirItemsList.SetBounds(BGImage.Sprite.Bounds.Width * 0.01f, TitleBarBG.Sprite.Bounds.Height, BGImage.Sprite.Bounds.Width * 0.45f, BGImage.Sprite.Bounds.Height - TitleBarBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            TheirItemsList.SetVisibleItems(5);
            TheirItemsList.ManualUpdate = true;
            TheirItemsList.SelectionEnabled = false;
            TheirItemsList.HighlightEnabled = false;
            TheirItemsList.OnListItemUpdated += TradeListItemUpdated;
            TheirItemsList.SetAnchor(AnchorPoints.TopRight);
            BGImage.AddChild(TheirItemsList);

            //separator bar
            ControlImage BarImage = new ControlImage(GameSystem);
            BarImage.SetBounds(0, TitleBarBG.Sprite.Bounds.Height, BGImage.Sprite.Bounds.Width * 0.01f, BGImage.Sprite.Bounds.Height - TitleBarBG.Sprite.Bounds.Height, UIUnits.PixelScaled);
            BarImage.SetImageFile("Data\\Images\\HUD\\listselect.png");
            BarImage.SetAnchor(AnchorPoints.TopMiddle);
            BGImage.AddChild(BarImage);

            if (Item.Data.Agent != null)
            {
                WorkingRealm OtherRealm;
                TradeOfferList OurItems;
                TradeOfferList TheirItems;

                if (Item.Data.Agent.TradeOffer.SenderRealmID == GameSystem.CurrentGame.PlayerRealm.ID)
                {
                    OtherRealm = GameSystem.CurrentGame.AllRealms[Item.Data.Agent.TradeOffer.TargetRealmID];
                    OurItems = Item.Data.Agent.TradeOffer.SenderOffers;
                    TheirItems = Item.Data.Agent.TradeOffer.TargetOffers;
                }
                else
                {
                    OtherRealm = GameSystem.CurrentGame.AllRealms[Item.Data.Agent.TradeOffer.SenderRealmID];
                    OurItems = Item.Data.Agent.TradeOffer.TargetOffers;
                    TheirItems = Item.Data.Agent.TradeOffer.SenderOffers;
                }

                RightText.SetTextData(GameText.CreateLocalised(OtherRealm.DisplayName));

                FillPendingTradeList(OurItemsList, OurItems, Item.Data.Agent);
                FillPendingTradeList(TheirItemsList, TheirItems, Item.Data.Agent);
            }

            if (Item.Data.Trade != null)
            {
                WorkingRealm OtherRealm;
                TradeOfferList OurItems = null;
                TradeOfferList TheirItems = null;

                if (Item.Data.Trade.TargetRealm == GameSystem.CurrentGame.PlayerRealm)
                {
                    OtherRealm = Item.Data.Trade.Realm;
                    TheirItems = Item.Data.Trade.OriginalTrade;
                    if (Item.Data.Trade.AssociatedTrade != null)
                    {
                        OurItems = Item.Data.Trade.AssociatedTrade.OriginalTrade;
                    }
                }
                else
                {
                    OtherRealm = Item.Data.Trade.TargetRealm;
                    OurItems = Item.Data.Trade.OriginalTrade;
                    if (Item.Data.Trade.AssociatedTrade != null)
                    {
                        TheirItems = Item.Data.Trade.AssociatedTrade.OriginalTrade;
                    }
                }

                RightText.SetTextData(GameText.CreateLocalised(OtherRealm.DisplayName));

                FillActiveTradeList(OurItemsList, OurItems, Item.Data.Trade);
                FillActiveTradeList(TheirItemsList, TheirItems, Item.Data.Trade);
            }
        }

        private void FillActiveTradeList(ControlList<ColouredTextData> ItemsList, TradeOfferList Items, OngoingTrade Trade)
        {
            if (Items.GoldLump > 0)
            {
                //lump gold, completed
                ColouredTextData Data = new ColouredTextData(GameText.CreateLocalised("TRADELIST_GOLD", Items.GoldLump), Color.CornflowerBlue);
                ItemsList.AddItem(Data);
            }

            foreach (KeyValuePair<ResourceData, int> Pair in Items.GetResourceLump())
            {
                if (Pair.Value == 0) continue;

                //lump resources, completed
                GameText Text = GameText.CreateLocalised("TRADELIST_RESOURCE", Pair.Value);
                Text.AddChildText(GameText.CreateLocalised(Pair.Key.DisplayName));

                ColouredTextData Data = new ColouredTextData(Text, Color.CornflowerBlue);
                ItemsList.AddItem(Data);
            }

            foreach (int ID in Items.GetPrisoners())
            {
                //prisoners, completed
                WorkingUnit Unit = null;
                GameSystem.CurrentGame.AllUnits.TryGetValue(ID, out Unit);

                if (Unit != null)
                {
                    ColouredTextData Data = new ColouredTextData(GameText.CreateLocalised(Unit.DisplayName), Color.CornflowerBlue);
                    ItemsList.AddItem(Data);
                }
                else
                {
                    ColouredTextData Data = new ColouredTextData(GameText.CreateFromLiteral(ID.ToString()), Color.CornflowerBlue);
                    ItemsList.AddItem(Data);
                }
                
            }

            if (Items.GoldPerTurn > 0)
            {
                //turn gold, completes when trade runs out
                ColouredTextData Data = new ColouredTextData(GameText.CreateLocalised("TRADE_GPT", Items.GoldPerTurn), Color.GreenYellow);
                Data.TurnCount = Trade.TimeLeft;
                ItemsList.AddItem(Data);
            }

            foreach (KeyValuePair<ResourceData, int> Pair in Items.GetResourceTurn())
            {
                if (Pair.Value == 0) continue;

                //turn resources, completes when trade runs out
                GameText Text = GameText.CreateLocalised("TRADE_RPT", Pair.Value);
                Text.AddChildText(GameText.CreateLocalised(Pair.Key.DisplayName));

                ColouredTextData Data = new ColouredTextData(Text, Color.GreenYellow);
                Data.TurnCount = Trade.TimeLeft;
                ItemsList.AddItem(Data);
            }

            foreach (int ID in Items.GetProvinces())
            {
                //provinces, completed
                WorkingProvince Province = GameSystem.CurrentGame.AllProvinces[ID];

                GameText Text = GameText.CreateLocalised("TRADE_PROVINCE");
                Text.AddChildText(GameText.CreateLocalised(Province.DisplayName));

                ColouredTextData Data = new ColouredTextData(Text, Color.CornflowerBlue);
                ItemsList.AddItem(Data);
            }

            if (Items.Treaty != TreatyTypes.None)
            {
                //treaty, completed
                ColouredTextData Data = new ColouredTextData(GetTreatyName(Items.Treaty), Color.CornflowerBlue);
                ItemsList.AddItem(Data);
            }
        }

        private void FillPendingTradeList(ControlList<ColouredTextData> ItemsList, TradeOfferList Items, WorkingAgent Agent)
        {
            if (Items.GoldLump > 0)
            {
                //lump gold, pending (completes in agent travel time)
                ColouredTextData Data = new ColouredTextData(GameText.CreateLocalised("TRADELIST_GOLD", Items.GoldLump), Color.Orange);
                Data.TurnCount = Agent.TurnsLeft;
                ItemsList.AddItem(Data);
            }

            foreach (KeyValuePair<ResourceData,int> Pair in Items.GetResourceLump())
            {
                if (Pair.Value == 0) continue;

                //lump resources, pending (completes in agent travel time)
                GameText Text = GameText.CreateLocalised("TRADELIST_RESOURCE", Pair.Value);
                Text.AddChildText(GameText.CreateLocalised(Pair.Key.DisplayName));
                
                ColouredTextData Data = new ColouredTextData(Text, Color.Orange);
                Data.TurnCount = Agent.TurnsLeft;
                ItemsList.AddItem(Data);
            }

            foreach (int ID in Items.GetPrisoners())
            {
                //prisoners, pending, completes in agent travel time
                WorkingUnit Unit = GameSystem.CurrentGame.AllUnits[ID];
                ColouredTextData Data = new ColouredTextData(GameText.CreateLocalised(Unit.DisplayName), Color.Orange);
                Data.TurnCount = Agent.TurnsLeft;
                ItemsList.AddItem(Data);
            }

            if (Items.GoldPerTurn > 0)
            {
                //turn gold, pending (starts in agent travel time)
                ColouredTextData Data = new ColouredTextData(GameText.CreateLocalised("TRADE_GPT", Items.GoldPerTurn), Color.Orange);
                Data.TurnCount = Agent.TurnsLeft;
                ItemsList.AddItem(Data);
            }

            foreach (KeyValuePair<ResourceData, int> Pair in Items.GetResourceTurn())
            {
                if (Pair.Value == 0) continue;

                //turn resources, pending (starts in agent travel time)
                GameText Text = GameText.CreateLocalised("TRADE_RPT", Pair.Value);
                Text.AddChildText(GameText.CreateLocalised(Pair.Key.DisplayName));

                ColouredTextData Data = new ColouredTextData(Text, Color.Orange);
                Data.TurnCount = Agent.TurnsLeft;
                ItemsList.AddItem(Data);
            }

            foreach (int ID in Items.GetProvinces())
            {
                //provinces, completed
                WorkingProvince Province = GameSystem.CurrentGame.AllProvinces[ID];

                GameText Text = GameText.CreateLocalised("TRADE_PROVINCE");
                Text.AddChildText(GameText.CreateLocalised(Province.DisplayName));

                ColouredTextData Data = new ColouredTextData(Text, Color.CornflowerBlue);
                ItemsList.AddItem(Data);
            }

            if (Items.Treaty != TreatyTypes.None)
            {
                //treaty, completed
                ColouredTextData Data = new ColouredTextData(GetTreatyName(Items.Treaty), Color.CornflowerBlue);
                ItemsList.AddItem(Data);
            }
        }

        public GameText GetTreatyName(TreatyTypes Treaty)
        {
            switch (Treaty)
            {
                case TreatyTypes.Alliance: return GameText.CreateLocalised("TREATY_ALLY");
                case TreatyTypes.MutualDefence: return GameText.CreateLocalised("TREATY_DEFENCE");
                case TreatyTypes.NonAggression: return GameText.CreateLocalised("TREATY_NAP");
                case TreatyTypes.None: return GameText.CreateLocalised("TREATY_FORCED");
                case TreatyTypes.Peace: return GameText.CreateLocalised("TREATY_PEACE");
                default: throw new Exception("Unknown treaty type: " + Treaty.ToString());
            }
        }

        private void TradeListItemUpdated(ListItem<ColouredTextData> Item)
        {
            ControlText LineText = new ControlText(GameSystem);
            LineText.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
            LineText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
            LineText.FontName = "Trebuchet MS";
            LineText.TextAnchor = AnchorPoints.Left;
            LineText.Colour = Item.Data.TextColour;
            LineText.SetTextData(Item.Data.Text);
            Item.Container.AddChild(LineText);

            if (Item.Data.TurnCount != 0)
            {
                ControlText TurnText = new ControlText(GameSystem);
                TurnText.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                TurnText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
                TurnText.FontName = "Trebuchet MS";
                TurnText.TextAnchor = AnchorPoints.Right;
                TurnText.Colour = Item.Data.TextColour;
                TurnText.SetTextData(GameText.CreateLocalised("FORMAT_TRADETURNS", Item.Data.TurnCount));
                Item.Container.AddChild(TurnText);
            }
        }

        void ResourcesList_OnItemClicked(UIControl Control)
        {
            if (ResourcesList.ClickedItem == null) return;

            CategorisedListItem Item = ResourcesList.ClickedItem.Data;
            if (Item.Header == false) return;

            if (Item.CategoryID == CATEGORY_STOCKPILE)
            {
                StockpileClosed = !StockpileClosed;
                UpdateResourcesList();
            }
            if (Item.CategoryID == CATEGORY_INCOME)
            {
                IncomeClosed = !IncomeClosed;
                UpdateResourcesList();
            }
        }

        void RelationsList_OnListItemUpdated(ListItem<WorkingRealm> Item)
        {
            ControlImage FlagImage = new ControlImage(GameSystem);
            FlagImage.SetBounds(Item.Container.Sprite.Bounds.Height * 0.05f, Item.Container.Sprite.Bounds.Height * 0.05f, Item.Container.Sprite.Bounds.Height * 0.9f, Item.Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
            FlagImage.SetImageFile("Data\\Images\\HUD\\flags\\" + Item.Data.FlagFilename + ".png");
            Item.Container.AddChild(FlagImage);

            ControlText UnitText = new ControlText(GameSystem);
            UnitText.SetBounds(Item.Container.Sprite.Bounds.Height, 0, Item.Container.Sprite.Bounds.Width - Item.Container.Sprite.Bounds.Height, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
            UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
            UnitText.FontName = "Trebuchet MS";
            UnitText.TextAnchor = AnchorPoints.Left;
            UnitText.Colour = Color.FromArgb(255, 238, 161);
            UnitText.SetTextData(GameText.CreateLocalised(Item.Data.DisplayName));
            Item.Container.AddChild(UnitText);

            //if at war, text should be red
            if (DiplomacyRealm.DiplomacyManager.GetRelation(Item.Data) == RelationStates.War) UnitText.Colour = Color.FromArgb(255, 0, 0);

            //if allied, text should be blue
            if (DiplomacyRealm.DiplomacyManager.GetRelation(Item.Data) == RelationStates.Alliance) UnitText.Colour = Color.FromArgb(51, 255, 255);

            //right aligned list of icons for the realm
            //if not relevant, simply skip and leave blank space to maintain alignments
            ControlText RelationText = new ControlText(GameSystem);
            RelationText.SetAnchor(AnchorPoints.TopRight);
            RelationText.SetBounds(Item.Container.Sprite.Bounds.Height * 0.05f, 0, Item.Container.Sprite.Bounds.Height * 2, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
            RelationText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
            RelationText.FontName = "Trebuchet MS";
            RelationText.TextAnchor = AnchorPoints.Right;
            RelationText.Colour = Color.FromArgb(255, 238, 161);
            RelationText.SetTextData(GameText.CreateLocalised("FORMAT_WHOLENUMBER", Item.Data.DiplomacyManager.GetDisposition(DiplomacyRealm)));
            Item.Container.AddChild(RelationText);

            ControlImage RaceImage = new ControlImage(GameSystem);
            RaceImage.SetAnchor(AnchorPoints.TopRight);
            RaceImage.SetBounds(Item.Container.Sprite.Bounds.Height * 2.1f, Item.Container.Sprite.Bounds.Height * 0.05f, Item.Container.Sprite.Bounds.Height * 0.9f, Item.Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
            RaceImage.SetAnchor(AnchorPoints.Right);
            RaceImage.SetImageFile(GetRaceIcon(Item.Data));
            RaceImage.MouseInputType = MouseInputTypes.Forced;
            RaceImage.SetTooltip(GameSystem.Utilities.TooltipFactory.GetRaceTooltip(Item.Data.Race));
            Item.Container.AddChild(RaceImage);

            Item.Container.SetTooltip(Item.Data.DiplomacyManager.GetTooltipList(DiplomacyRealm));
        }

        private string GetRaceIcon(WorkingRealm Realm)
        {
            switch (Realm.Race)
            {
                case Races.Dragon: return "Data\\Images\\HUD\\Purchase\\stat_dragon.png";
                case Races.Dwarf: return "Data\\Images\\HUD\\Purchase\\stat_dwarf.png";
                case Races.Elf: return "Data\\Images\\HUD\\Purchase\\stat_elf.png";
                case Races.Giant: return "Data\\Images\\HUD\\Purchase\\stat_giant.png";
                case Races.Human: return "Data\\Images\\HUD\\Purchase\\stat_human.png";
                case Races.Monster: return "Data\\Images\\HUD\\Purchase\\stat_monster.png";
                case Races.Orc: return "Data\\Images\\HUD\\Purchase\\stat_orc.png";
                case Races.Undead: return "Data\\Images\\HUD\\Purchase\\stat_undead.png";
            }

            return "";
        }

        void AlliesList_OnItemClicked(UIControl Control)
        {
            if (AlliesList.ClickedItem == null) return;

            CategorisedListItem Item = AlliesList.ClickedItem.Data;
            if (Item.Header == false) return;

            if (Item.CategoryID == CATEGORY_ALLIES)
            {
                AlliesClosed = !AlliesClosed;
                UpdateAlliesList();
            }
            if (Item.CategoryID == CATEGORY_ENEMIES)
            {
                EnemiesClosed = !EnemiesClosed;
                UpdateAlliesList();
            }
        }

        void OnCategorisedListItemUpdated(ListItem<CategorisedListItem> Item)
        {
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
                UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
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
                    Item.Container.AddChild(Image);
                }

                ControlText UnitText = new ControlText(GameSystem);
                UnitText.SetBounds((Item.Container.Sprite.Bounds.Height) + IndentLeft, 0, Item.Container.Sprite.Bounds.Width - Item.Container.Sprite.Bounds.Height, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
                UnitText.FontName = "Trebuchet MS";
                UnitText.TextAnchor = AnchorPoints.Left;
                UnitText.Colour = Color.FromArgb(255, 238, 161);
                UnitText.SetTextData(Item.Data.Text);
                Item.Container.AddChild(UnitText);

                //if a value is given, add a right aligned text to hold it
                if (Item.Data.HasValue)
                {
                    ControlText RelationText = new ControlText(GameSystem);
                    RelationText.SetAnchor(AnchorPoints.TopRight);
                    RelationText.SetBounds(Item.Container.Sprite.Bounds.Height * 0.05f, 0, Item.Container.Sprite.Bounds.Height * 2, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                    RelationText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
                    RelationText.FontName = "Trebuchet MS";
                    RelationText.TextAnchor = AnchorPoints.Right;
                    RelationText.Colour = Color.FromArgb(255, 238, 161);
                    RelationText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Item.Data.Value));
                    Item.Container.AddChild(RelationText);
                }
            }
        }

        private void RealmList_OnListItemUpdated(ListItem<WorkingRealm> Item)
        {
            ControlImage FlagImage = new ControlImage(GameSystem);
            FlagImage.SetBounds(Item.Container.Sprite.Bounds.Height * 0.05f, Item.Container.Sprite.Bounds.Height * 0.05f, Item.Container.Sprite.Bounds.Height * 0.9f, Item.Container.Sprite.Bounds.Height * 0.9f, UIUnits.PixelScaled);
            FlagImage.SetImageFile("Data\\Images\\HUD\\flags\\" + Item.Data.FlagFilename + ".png");
            Item.Container.AddChild(FlagImage);

            ControlText UnitText = new ControlText(GameSystem);
            UnitText.SetBounds(Item.Container.Sprite.Bounds.Height, 0, Item.Container.Sprite.Bounds.Width - Item.Container.Sprite.Bounds.Height, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
            UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
            UnitText.FontName = "Trebuchet MS";
            UnitText.TextAnchor = AnchorPoints.Left;
            UnitText.Colour = Color.FromArgb(255, 238, 161);
            UnitText.SetTextData(GameText.CreateLocalised(Item.Data.DisplayName));
            Item.Container.AddChild(UnitText);
        }

        public override void Dispose()
        {
            GameSystem.UnregisterEvent(HandleOpenRequest, "OpenRelations");
            GameSystem.UnregisterEvent(HandleTradeSent, "TradeOfferSent");
            GameSystem.UnregisterEvent(HandleCloseRequest, "CloseRelations");
        }

        private void HandleOpenRequest(string EventName, params object[] Args)
        {
            if (DiplomacyRealm == null || DiplomacyRealm.RealmIsDead) DiplomacyRealm = GameSystem.CurrentGame.PlayerRealm;

            GameForm.Visible = true;
            UpdateAll();
        }

        private void UpdateAll()
        {
            if (GameForm.Visible == false) return;
            if (DiplomacyRealm == null) return;

            GameText Text = GameText.CreateLocalised(DiplomacyRealm.DisplayName);
            
            GameForm.GetControlByName<ControlText>("TradeOfferRealmText").SetTextData(Text);
            GameForm.GetControlByName<ControlImage>("TradeOfferRealmFlag").SetImageFile("Data\\Images\\HUD\\flags\\" + DiplomacyRealm.FlagFilename + ".png");
            GameForm.GetControlByName<ControlImage>("TradeOfferRealmsBG").Visible = true;
            SelectRealmInList(DiplomacyRealm);

            WorkingRealm Player = GameSystem.CurrentGame.PlayerRealm;
            if (Player != null)
            {
                ControlImage HomeFlag = GameForm.GetControlByName<ControlImage>("PlayerRealmHomeFlag");
                if (HomeFlag != null)
                {
                    HomeFlag.SetImageFile("Data\\Images\\HUD\\flags\\" + Player.FlagFilename + ".png");
                    HomeFlag.SetTooltip(GameText.CreateLocalised("TT_DIPLOMACY_HOME"));
                }
            }

            UpdateDiplomacy();
            UpdateSelectedRealmCard();
            UpdateTrade();
            UpdateAgents();
            UpdateAgentsOverview();
        }

        private void LayoutRealmInfoPanel(int AuxCount = -1)
        {
            ControlImage RealmInfoBG = GameForm.GetControlByName<ControlImage>("BGImageRealmInfo");
            if (RealmInfoBG == null || SelectedRealmCard == null || AuxiliariesList == null) return;

            // Sprite.Bounds are already screen-scaled — must use PixelScaled (Pixel double-sizes).
            float BW = RealmInfoBG.Sprite.Bounds.Width;
            float BH = RealmInfoBG.Sprite.Bounds.Height;
            if (BW < 10f || BH < 10f) return;

            float CardX = BW * 0.02f;
            float CardY = BH * 0.02f;
            // Leave margin inside the ornate border; keep left column under ~48% width.
            float CardH = BH * 0.96f;
            float CardW = CardH * (265f / 443f);
            float MaxCardW = BW * 0.48f;
            if (CardW > MaxCardW)
            {
                CardW = MaxCardW;
                CardH = CardW * (443f / 265f);
            }
            SelectedRealmCard.SetSize(CardW, CardH, UIUnits.PixelScaled);
            SelectedRealmCard.SetPosition(CardX, CardY, UIUnits.PixelScaled);

            float AuxX = CardX + CardW + BW * 0.03f;
            float AuxY = BH * 0.16f;
            float AuxW = BW - AuxX - BW * 0.03f;

            // Size the list to the unit rows only so status text can sit directly under them.
            int Visible = AuxCount < 0 ? AuxiliariesList.VisibleItems : Math.Max(1, Math.Min(5, AuxCount == 0 ? 1 : AuxCount));
            AuxiliariesList.SetVisibleItems(Visible);
            float RowH = BH * 0.105f;
            float AuxH = Math.Min(BH * 0.55f, RowH * Visible);
            AuxiliariesList.SetBounds(AuxX, AuxY, AuxW, AuxH, UIUnits.PixelScaled);

            ControlText AuxTitle = GameForm.GetControlByName<ControlText>("AuxiliariesTitle");
            if (AuxTitle != null)
            {
                AuxTitle.SetBounds(AuxX, BH * 0.09f, AuxW, BH * 0.07f, UIUnits.PixelScaled);
            }

            ControlText AuxStatus = GameForm.GetControlByName<ControlText>("AuxiliariesStatusText");
            float StatusY = AuxY + AuxH + BH * 0.015f;
            if (AuxStatus != null)
            {
                AuxStatus.SetBounds(AuxX, StatusY, AuxW, BH * 0.10f, UIUnits.PixelScaled);
            }

            float RelY = StatusY + BH * 0.11f;
            float RelLineH = BH * 0.055f;
            float RelGap = BH * 0.035f;
            ControlText RelOurs = GameForm.GetControlByName<ControlText>("RealmRelationsOursText");
            if (RelOurs != null)
            {
                RelOurs.SetBounds(AuxX, RelY, AuxW, RelLineH, UIUnits.PixelScaled);
            }
            ControlText RelTheirs = GameForm.GetControlByName<ControlText>("RealmRelationsTheirsText");
            if (RelTheirs != null)
            {
                RelTheirs.SetBounds(AuxX, RelY + RelLineH + RelGap, AuxW, RelLineH, UIUnits.PixelScaled);
            }
        }

        private void UpdateSelectedRealmCard()
        {
            if (SelectedRealmCard == null || DiplomacyRealm == null) return;
            SelectedRealmCard.SetRealm(DiplomacyRealm);
            SelectedRealmCard.UpdateSpells();
            ClearAuxPin();
            UpdateAuxiliariesList();
            UpdateRealmRelationsText();
        }

        private void ClearAuxPreviewUnits()
        {
            AuxPreviewUnits.Clear();
        }

        private void UpdateAuxiliariesList()
        {
            if (AuxiliariesList == null || DiplomacyRealm == null) return;

            ClearAuxPreviewUnits();
            AuxiliariesList.ClearItems();

            List<UnitData> AuxTypes = GameSystem.Data.Units.Values
                .Where(x => x.Realm == DiplomacyRealm.Name
                    && x.Cost > 0
                    && x.Abilities.Contains("Auxiliary"))
                .OrderBy(x => GameText.CreateLocalised(x.DisplayName).GetActualText(GameSystem))
                .ToList();

            foreach (UnitData Type in AuxTypes)
            {
                WorkingUnit Preview = new WorkingUnit(GameSystem.CurrentGame, -1, Type);
                Preview.OwnerRealmID = DiplomacyRealm.ID;
                // OwnerRealm is resolved from OwnerRealmID; bail if lookup fails so hover SetUnit won't NRE.
                if (Preview.OwnerRealm == null) continue;
                AuxPreviewUnits.Add(Preview);
                AuxiliariesList.AddItem(Preview);
            }

            LayoutRealmInfoPanel(AuxPreviewUnits.Count);

            ControlText Status = GameForm.GetControlByName<ControlText>("AuxiliariesStatusText");
            if (Status == null) return;

            if (AuxPreviewUnits.Count == 0)
            {
                Status.SetTextData(GameText.CreateLocalised("REALMINFO_AUX_NONE"));
            }
            else if (DiplomacyRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                Status.SetTextData(GameText.CreateLocalised("REALMINFO_AUX_SELF"));
            }
            else if (GameSystem.CurrentGame.PlayerRealm.Allies.Contains(DiplomacyRealm))
            {
                Status.SetTextData(GameText.CreateLocalised("REALMINFO_AUX_ALLIED"));
            }
            else
            {
                Status.SetTextData(GameText.CreateLocalised("REALMINFO_AUX_NOT_ALLIED"));
            }
        }

        private void UpdateRealmRelationsText()
        {
            ControlText RelOurs = GameForm.GetControlByName<ControlText>("RealmRelationsOursText");
            ControlText RelTheirs = GameForm.GetControlByName<ControlText>("RealmRelationsTheirsText");
            if (RelOurs == null && RelTheirs == null) return;

            WorkingRealm Player = GameSystem.CurrentGame.PlayerRealm;
            bool Hide = DiplomacyRealm == null || Player == null || DiplomacyRealm == Player;
            if (Hide)
            {
                if (RelOurs != null)
                {
                    RelOurs.Visible = false;
                    RelOurs.SetTooltip((List<GameText>)null);
                }
                if (RelTheirs != null)
                {
                    RelTheirs.Visible = false;
                    RelTheirs.SetTooltip((List<GameText>)null);
                }
                return;
            }

            float Ours = Player.DiplomacyManager.GetDisposition(DiplomacyRealm);
            float Theirs = DiplomacyRealm.DiplomacyManager.GetDisposition(Player);

            if (RelOurs != null)
            {
                RelOurs.Visible = true;
                RelOurs.MouseInputType = MouseInputTypes.Forced;
                RelOurs.SetTextData(GameText.CreateLocalised("REALMINFO_RELATIONS_OURS", Ours));
                RelOurs.SetTooltip(Player.DiplomacyManager.GetTooltipList(DiplomacyRealm));
            }
            if (RelTheirs != null)
            {
                RelTheirs.Visible = true;
                RelTheirs.MouseInputType = MouseInputTypes.Forced;
                RelTheirs.SetTextData(GameText.CreateLocalised("REALMINFO_RELATIONS_THEIRS", Theirs));
                RelTheirs.SetTooltip(DiplomacyRealm.DiplomacyManager.GetTooltipList(Player));
            }
        }

        private void AuxiliariesList_OnListItemUpdated(ListItem<WorkingUnit> Item)
        {
            float H = Item.Container.Sprite.Bounds.Height;
            float W = Item.Container.Sprite.Bounds.Width;

            ControlImage UnitImage = new ControlImage(GameSystem);
            UnitImage.SetBounds(H * 0.05f, H * 0.05f, H * 0.9f, H * 0.9f, UIUnits.PixelScaled);
            UnitImage.SetImageFile("Data\\Images\\Units\\" + Item.Data.ImageFile + ".jpg");
            UnitImage.BubbleMouseovers = true;
            Item.Container.AddChild(UnitImage);

            ControlText NameText = new ControlText(GameSystem);
            NameText.SetBounds(H * 1.05f, 0, W - H * 1.1f, H, UIUnits.PixelScaled);
            // Cap font so long names stay readable inside the aux column.
            int FontPx = (int)Math.Max(11f, Math.Min(15f, H * 0.42f));
            NameText.SetFontSize(FontPx, UIUnits.PixelScaled);
            NameText.FontName = "Trebuchet MS";
            NameText.TextAnchor = AnchorPoints.Left;
            NameText.Colour = Color.FromArgb(255, 238, 161);
            NameText.SetTextData(GameText.CreateLocalised(Item.Data.DisplayName));
            NameText.BubbleMouseovers = true;
            Item.Container.AddChild(NameText);
        }

        private void AuxiliariesList_OnHoverItemChanged(UIControl Control)
        {
            RefreshAuxPreviewCard();
        }

        private void AuxiliariesList_OnItemClicked(UIControl Control)
        {
            if (AuxiliariesList == null || AuxiliariesList.ClickedItem == null || AuxiliariesList.ClickedItem.Data == null) return;

            WorkingUnit Clicked = AuxiliariesList.ClickedItem.Data;
            // Toggle pin on the same unit; clicking another unit re-pins.
            if (AuxPinnedUnit == Clicked) ClearAuxPin();
            else PinAuxUnit(Clicked);

            RefreshAuxPreviewCard();
        }

        private void AuxPreviewCard_OnMouseEnter(UIControl Control)
        {
            // Keep pinned card up while inspecting tooltips on it (InfoCardPanel does the same).
            if (AuxPinnedUnit != null) RefreshAuxPreviewCard();
        }

        private void PinAuxUnit(WorkingUnit Unit)
        {
            AuxPinnedUnit = Unit;
            if (AuxPreviewCard != null)
            {
                // Allow hovering stats/tooltips on the pinned card.
                AuxPreviewCard.MouseInputType = MouseInputTypes.Forced;
            }
        }

        private void ClearAuxPin()
        {
            AuxPinnedUnit = null;
            if (AuxPreviewCard != null)
            {
                AuxPreviewCard.MouseInputType = MouseInputTypes.None;
                AuxPreviewCard.Visible = false;
            }
        }

        private void RefreshAuxPreviewCard()
        {
            if (AuxiliariesList == null || AuxPreviewCard == null) return;

            // Pinned card wins over hover, matching InfoCardPanel's pinned-vs-mouseover priority.
            WorkingUnit Show = AuxPinnedUnit;
            if (Show == null && AuxiliariesList.HoverItem != null)
                Show = AuxiliariesList.HoverItem.Data;

            if (Show != null)
            {
                AuxPreviewCard.SetUnit(Show, null);
                AuxPreviewCard.Visible = true;
                AuxPreviewCard.BringToFront();
            }
            else
            {
                AuxPreviewCard.Visible = false;
            }
        }

        private void SelectRealmInList(WorkingRealm Realm)
        {
            if (RealmList == null || Realm == null) return;
            for (int i = 0; i < RealmList.Items.Count; i++)
            {
                if (RealmList.Items[i].Data == Realm)
                {
                    RealmList.SetSelectedIndex(i);
                    return;
                }
            }
        }

        public void PlayerRealmHomeClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("RelationsRealmChange") == false) return;

            DiplomacyRealm = GameSystem.CurrentGame.PlayerRealm;
            UpdateAll();
            GameSystem.FireEvent("DiplomacyRealmChanged", DiplomacyRealm);
        }

        private void UpdateAgentsOverview()
        {
            int i = 0;
            foreach (WorkingAgent Agent in GameSystem.CurrentGame.PlayerRealm.Agents)
            {
                if (i >= AgentOverviewControls.Count) continue;

                AgentOverviewControls[i].Update(Agent);
                i++;
            }

            while (i < AgentOverviewControls.Count)
            {
                AgentOverviewControls[i].Update(null);
                i++;
            }
        }

        private void UpdateAgents()
        {
            //first box shows new action buttons
            //  if no agents, show message
            //  if player, show message
            NewAgentBox.Update(DiplomacyRealm);

            //next 7 boxes show each agent assigned to realm by player
            //  if player, show status of all agents
            if (DiplomacyRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                int i = 0;

                foreach (WorkingAgent Agent in GameSystem.CurrentGame.PlayerRealm.Agents)
                {
                    if (i >= ActiveAgentBoxes.Count) continue;

                    ActiveAgentBoxes[i].Update(Agent);
                    i++;
                }

                while (i < ActiveAgentBoxes.Count)
                {
                    ActiveAgentBoxes[i].Update(null);
                    i++;
                }
            }
            else
            {
                int i = 0;

                foreach (WorkingAgent Agent in GameSystem.CurrentGame.PlayerRealm.Agents.Where(x => x.HostRealm == DiplomacyRealm))
                {
                    if (i >= ActiveAgentBoxes.Count) continue;

                    ActiveAgentBoxes[i].Update(Agent);
                    i++;
                }

                while (i < ActiveAgentBoxes.Count)
                {
                    ActiveAgentBoxes[i].Update(null);
                    i++;
                }
            }
        }

        private void UpdateTrade()
        {
            //list of available resources (stockpile plus per turn, categorised)
            UpdateResourcesList();

            //list of current trades (if player, list all trades)
            UpdateTradesList();

            ControlButton TradeOfferButton = GameForm.GetControlByName<ControlButton>("TradeOfferButton");

            //if not player, offer trade button (if agent free)
            if (DiplomacyRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                GameForm.GetControlByName<ControlText>("TradeOfferSelfText").Visible = true;
                TradeOfferButton.Visible = false;
            }
            else
            {
                GameForm.GetControlByName<ControlText>("TradeOfferSelfText").Visible = false;
                TradeOfferButton.Visible = true;
                string Reason;
                if (RealmTradeManager.CanStartEconomicTrade(GameSystem.CurrentGame.PlayerRealm, DiplomacyRealm, out Reason))
                {
                    TradeOfferButton.Disabled = false;
                    TradeOfferButton.SetTooltip(GameText.CreateLocalised("RELATIONBTN_TRADE"));
                }
                else
                {
                    TradeOfferButton.Disabled = true;
                    TradeOfferButton.SetTooltip(GameText.CreateFromLiteral("Trade unavailable: " + Reason));
                }
            }
        }

        private void UpdateTradesList()
        {
            List<OngoingTrade> AllTrades = GameSystem.CurrentGame.GetOngoingTrades(DiplomacyRealm);
            List<WorkingAgent> PendingTrades = new List<WorkingAgent>();

            //if this is not the player, only trades connected to the player should be show in the list
            if (DiplomacyRealm != GameSystem.CurrentGame.PlayerRealm)
            {
                AllTrades = AllTrades.Where(x => x.Realm == GameSystem.CurrentGame.PlayerRealm || x.TargetRealm == GameSystem.CurrentGame.PlayerRealm).ToList();
            }

            //make sure that only one half of each trade is added to the list to prevent duplication
            List<OngoingTrade> OngoingTrades = new List<OngoingTrade>();
            foreach (OngoingTrade Trade in AllTrades)
            {
                if (OngoingTrades.Contains(Trade)) continue;
                if (Trade.AssociatedTrade != null && OngoingTrades.Contains(Trade.AssociatedTrade)) continue;
                OngoingTrades.Add(Trade);
            }

            foreach (WorkingAgent Agent in GameSystem.CurrentGame.AllAgents.Values)
            {
                if (Agent.TradeOffer == null) continue;

                if (DiplomacyRealm == GameSystem.CurrentGame.PlayerRealm)
                {
                    if (Agent.OwnerRealm == DiplomacyRealm || Agent.HostRealm == DiplomacyRealm)
                    {
                        PendingTrades.Add(Agent);
                    }
                }
                else
                {
                    if (Agent.OwnerRealm == GameSystem.CurrentGame.PlayerRealm && Agent.HostRealm == DiplomacyRealm)
                    {
                        PendingTrades.Add(Agent);
                    }

                    if (Agent.HostRealm == GameSystem.CurrentGame.PlayerRealm && Agent.OwnerRealm == DiplomacyRealm)
                    {
                        PendingTrades.Add(Agent);
                    }
                }
            }

            TradeList.ClearItems();

            foreach (WorkingAgent Agent in PendingTrades)
            {
                TradeData Data = new TradeData();
                Data.Agent = Agent;
                TradeList.AddItem(Data);
            }

            foreach (OngoingTrade Trade in OngoingTrades)
            {
                TradeData Data = new TradeData();
                Data.Trade = Trade;
                TradeList.AddItem(Data);
            }
        }

        public void TradeOfferClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("CreateNewTrade") == false) return;
            string Reason;
            if (!RealmTradeManager.CanStartEconomicTrade(GameSystem.CurrentGame.PlayerRealm, DiplomacyRealm, out Reason)) return;
            GameSystem.FireEvent("OfferTrade", DiplomacyRealm);
            GameSystem.FireEvent("TradeOfferClicked");
        }

        private void UpdateResourcesList()
        {
            ResourcesList.ClearItems();

            CategorisedListItem StockpileHeader = new CategorisedListItem(true, GameText.CreateLocalised("RESOURCE_STOCKPILE"));
            StockpileHeader.Closed = StockpileClosed;
            StockpileHeader.CategoryID = CATEGORY_STOCKPILE;
            ResourcesList.AddItem(StockpileHeader);

            if (StockpileHeader.Closed == false)
            {
                Dictionary<ResourceData, int> Stockpiles = new Dictionary<ResourceData, int>();
                foreach (ResourceData Resource in GameSystem.Data.Resources.Values)
                {
                    int Stockpile = DiplomacyRealm.GetStockpiledResource(Resource);
                    if (Stockpile == 0) continue;
                    Stockpiles.Add(Resource, Stockpile);
                }

                if (Stockpiles.Count == 0)
                {
                    CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised("RESOURCES_NONE"));
                    Item.IndentLevel = 1;
                    ResourcesList.AddItem(Item);
                }
                else
                {
                    foreach (KeyValuePair<ResourceData, int> Pair in Stockpiles)
                    {
                        CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised(Pair.Key.DisplayName));
                        Item.IndentLevel = 1;
                        Item.HasValue = true;
                        Item.Value = Pair.Value;
                        Item.ImageFile = "Data\\Images\\HUD\\tradeicons\\" + Pair.Key.ResourceName + ".png";
                        ResourcesList.AddItem(Item);
                    }
                }
            }

            CategorisedListItem IncomeHeader = new CategorisedListItem(true, GameText.CreateLocalised("RESOURCE_INCOME"));
            IncomeHeader.Closed = IncomeClosed;
            IncomeHeader.CategoryID = CATEGORY_INCOME;
            ResourcesList.AddItem(IncomeHeader);

            if (IncomeHeader.Closed == false)
            {
                Dictionary<ResourceData, int> Incomes = new Dictionary<ResourceData, int>();
                foreach (ResourceData Resource in GameSystem.Data.Resources.Values)
                {
                    int Income = DiplomacyRealm.GetResourceIncome(Resource);
                    Income -= DiplomacyRealm.GetResourceExpenses(Resource);
                    if (Income <= 0) continue;
                    Incomes.Add(Resource, Income);
                }

                if (Incomes.Count == 0)
                {
                    CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised("RESOURCES_NONE"));
                    Item.IndentLevel = 1;
                    ResourcesList.AddItem(Item);
                }
                else
                {
                    foreach (KeyValuePair<ResourceData, int> Pair in Incomes)
                    {
                        CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised(Pair.Key.DisplayName));
                        Item.IndentLevel = 1;
                        Item.HasValue = true;
                        Item.Value = Pair.Value;
                        Item.ImageFile = "Data\\Images\\HUD\\tradeicons\\" + Pair.Key.ResourceName + ".png";
                        ResourcesList.AddItem(Item);
                    }
                }
            }
        }

        private void UpdateDiplomacy()
        {
            //list of allies and enemies (categorised list)   
            UpdateAlliesList();

            //list of current relations
            RelationsList.StorePosition();
            RelationsList.ClearItems();
            foreach (WorkingRealm Realm in GameSystem.CurrentGame.AllRealms.Values.OrderBy(x => GameText.CreateLocalised(x.DisplayName).GetActualText(GameSystem)))
            {
                if (Realm == DiplomacyRealm) continue;
                if (Realm == GameSystem.CurrentGame.RebelRealm) continue;
                if (Realm.RealmIsDead) continue;
                RelationsList.AddItem(Realm);
            }
            RelationsList.RestorePosition();

            //if not player, interaction buttons and current status text
            if (DiplomacyRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                GameForm.GetControlByName<ControlText>("RelationStatusText").SetTextData(GameText.CreateLocalised("TREATIES_SELF"));
                GameForm.GetControlByName<ControlButton>("TreatyBreakButton").Visible = false;
                GameForm.GetControlByName<ControlButton>("TreatyOfferButton").Visible = false;
            }
            else
            {
                GameText Text = GameText.CreateLocalised("TREATYDESC");
                Text.AddChildText(GameText.CreateLocalised(DiplomacyRealm.DisplayName));
                Text.AddChildText(GetTreatyDesc());

                GameForm.GetControlByName<ControlText>("RelationStatusText").SetTextData(Text);

                UpodateTreatyButtons();
            }
        }

        private void UpodateTreatyButtons()
        {
            ControlButton TreatyBreakButton = GameForm.GetControlByName<ControlButton>("TreatyBreakButton");
            ControlButton TreatyOfferButton = GameForm.GetControlByName<ControlButton>("TreatyOfferButton");

            ControlText TreatyBreakText = GameForm.GetControlByName<ControlText>("TreatyBreakText");
            ControlText TreatyOfferText = GameForm.GetControlByName<ControlText>("TreatyOfferText");

            RelationStates Relation = GameSystem.CurrentGame.PlayerRealm.DiplomacyManager.GetRelation(DiplomacyRealm);

            switch (Relation)
            {
                case RelationStates.Alliance:
                    {
                        TreatyBreakButton.Visible = true;
                        TreatyBreakText.SetTextData(GameText.CreateLocalised("BREAKTREATY"));

                        TreatyOfferButton.Visible = false;

                        break;
                    }
                case RelationStates.Defence:
                    {
                        TreatyBreakButton.Visible = true;
                        TreatyBreakText.SetTextData(GameText.CreateLocalised("BREAKTREATY"));

                        TreatyOfferButton.Visible = GameSystem.CurrentGame.AllianceController.TreatyIsPossible(GameSystem.CurrentGame.PlayerRealm, DiplomacyRealm, TreatyTypes.Alliance);
                        TreatyOfferText.SetTextData(GameText.CreateLocalised("OFFERALLIANCE"));
                        TreatyOfferButton.SetTooltip(GameText.CreateLocalised("OFFERALLIANCE"));

                        break;
                    }
                case RelationStates.NAP:
                    {
                        TreatyBreakButton.Visible = true;
                        TreatyBreakText.SetTextData(GameText.CreateLocalised("BREAKTREATY"));

                        TreatyOfferButton.Visible = GameSystem.CurrentGame.AllianceController.TreatyIsPossible(GameSystem.CurrentGame.PlayerRealm, DiplomacyRealm, TreatyTypes.MutualDefence);
                        TreatyOfferText.SetTextData(GameText.CreateLocalised("OFFERDEFENCE"));
                        TreatyOfferButton.SetTooltip(GameText.CreateLocalised("OFFERDEFENCE"));

                        break;
                    }
                case RelationStates.ForcedPeace:
                    {
                        TreatyBreakButton.Visible = false;
                        TreatyOfferButton.Visible = false;

                        break;
                    }
                case RelationStates.Peace:
                    {
                        TreatyBreakButton.Visible = true;
                        TreatyBreakText.SetTextData(GameText.CreateLocalised("DECLAREWAR"));

                        TreatyOfferButton.Visible = GameSystem.CurrentGame.AllianceController.TreatyIsPossible(GameSystem.CurrentGame.PlayerRealm, DiplomacyRealm, TreatyTypes.NonAggression);
                        TreatyOfferText.SetTextData(GameText.CreateLocalised("OFFERNAP"));
                        TreatyOfferButton.SetTooltip(GameText.CreateLocalised("OFFERNAP"));

                        break;
                    }
                case RelationStates.War:
                    {
                        TreatyBreakButton.Visible = false;

                        TreatyOfferButton.Visible = true;
                        TreatyOfferText.SetTextData(GameText.CreateLocalised("OFFERPEACE"));
                        TreatyOfferButton.SetTooltip(GameText.CreateFromLiteral(BuildPeaceAcceptanceTooltip(DiplomacyRealm)));

                        break;
                    }
            }
        }

        private string BuildPeaceAcceptanceTooltip(WorkingRealm Realm)
        {
            if (Realm == null || Realm.AIPlayer == null)
            {
                return "Offer peace";
            }

            WorkingRealm Player = GameSystem.CurrentGame.PlayerRealm;
            StringBuilder Builder = new StringBuilder();
            Builder.AppendLine("Peace acceptance");

            try
            {
                int Desire = Realm.AIPlayer.WarManager.GetPeaceDesire(Player);
                int Threshold = -5 - Realm.Traits[AITraits.Diplomat] + Realm.Traits[AITraits.Warmonger];
                int RelationTime = Realm.DiplomacyManager.GetRelationTime(Player);

                Builder.AppendLine(string.Format("Desire: {0} (need >= {1})", Desire > 0 ? "+" + Desire : Desire.ToString(), Threshold));
                if (RelationTime < 5)
                {
                    Builder.AppendLine(string.Format("Too early: {0} turn(s) left", 5 - RelationTime));
                }
                else if (Desire < Threshold)
                {
                    Builder.AppendLine("Below threshold — will hard-refuse");
                }
                else
                {
                    Builder.AppendLine("Threshold met — open trade table for details");
                }
            }
            catch
            {
                Builder.AppendLine("Open trade table for details");
            }

            return Builder.ToString().TrimEnd();
        }

        public void TreatyBreakClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("BreakTreaty") == false) return;

            RelationStates Relation = GameSystem.CurrentGame.PlayerRealm.DiplomacyManager.GetRelation(DiplomacyRealm);

            switch (Relation)
            {
                case RelationStates.Alliance:
                case RelationStates.Defence:
                case RelationStates.NAP:
                    {
                        GameSystem.FireEvent("ShowTreatyBreak", DiplomacyRealm);
                        break;
                    }
                case RelationStates.Peace:
                    {
                        GameSystem.FireEvent("ShowWarDialog", DiplomacyRealm, (WorkingStack)null, (ActivePathNode)null);
                        break;
                    }
            }
        }

        public void TreatyOfferClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("OfferTreaty") == false) return;

            RelationStates Relation = GameSystem.CurrentGame.PlayerRealm.DiplomacyManager.GetRelation(DiplomacyRealm);

            switch (Relation)
            {
                case RelationStates.Defence:
                    {
                        GameSystem.FireEvent("OfferTreaty", DiplomacyRealm, TreatyTypes.Alliance);
                        break;
                    }
                case RelationStates.NAP:
                    {
                        GameSystem.FireEvent("OfferTreaty", DiplomacyRealm, TreatyTypes.MutualDefence);
                        break;
                    }
                case RelationStates.Peace:
                    {
                        GameSystem.FireEvent("OfferTreaty", DiplomacyRealm, TreatyTypes.NonAggression);
                        break;
                    }
                case RelationStates.War:
                    {
                        GameSystem.FireEvent("OfferTreaty", DiplomacyRealm, TreatyTypes.Peace);
                        break;
                    }
            }
        }

        private GameText GetTreatyDesc()
        {
            RelationStates Relation = GameSystem.CurrentGame.PlayerRealm.DiplomacyManager.GetRelation(DiplomacyRealm);

            switch (Relation)
            {
                case RelationStates.Alliance: return GameText.CreateLocalised("RELATION_ALLIANCE");
                case RelationStates.Defence: return GameText.CreateLocalised("RELATION_DEFENCE");
                case RelationStates.NAP: return GameText.CreateLocalised("RELATION_NAP");
                case RelationStates.Peace: return GameText.CreateLocalised("RELATION_PEACE");
                case RelationStates.ForcedPeace: return GameText.CreateLocalised("RELATION_FORCEDPEACE", 5 - GameSystem.CurrentGame.PlayerRealm.DiplomacyManager.GetRelationTime(DiplomacyRealm));
                case RelationStates.War: return GameText.CreateLocalised("RELATION_WAR");
                default: return GameText.CreateLocalised("");
            }
        }

        private void UpdateAlliesList()
        {
            AlliesList.ClearItems();

            CategorisedListItem AlliesHeader = new CategorisedListItem(true, GameText.CreateLocalised("BATTLES_ALLY"));
            AlliesHeader.Closed = AlliesClosed;
            AlliesHeader.CategoryID = CATEGORY_ALLIES;
            AlliesList.AddItem(AlliesHeader);

            if (AlliesHeader.Closed == false)
            {
                IList<WorkingRealm> Allies = DiplomacyRealm.Allies;
                if (Allies.Count == 0)
                {
                    CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised("ALLIES_NONE"));
                    Item.IndentLevel = 1;
                    AlliesList.AddItem(Item);
                }
                else
                {
                    foreach (WorkingRealm Ally in Allies)
                    {
                        CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised(Ally.DisplayName));
                        Item.IndentLevel = 1;
                        Item.ImageFile = "Data\\Images\\HUD\\flags\\" + Ally.FlagFilename + ".png";
                        AlliesList.AddItem(Item);
                    }
                }
            }

            CategorisedListItem EnemiesHeader = new CategorisedListItem(true, GameText.CreateLocalised("BATTLES_ENEMY"));
            EnemiesHeader.Closed = EnemiesClosed;
            EnemiesHeader.CategoryID = CATEGORY_ENEMIES;
            AlliesList.AddItem(EnemiesHeader);

            if (EnemiesHeader.Closed == false)
            {
                IList<WorkingRealm> Enemies = DiplomacyRealm.Enemies;
                if (Enemies.Count == 0)
                {
                    CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised("ENEMIES_NONE"));
                    Item.IndentLevel = 1;
                    AlliesList.AddItem(Item);
                }
                else
                {
                    foreach (WorkingRealm Enemy in Enemies)
                    {
                        CategorisedListItem Item = new CategorisedListItem(false, GameText.CreateLocalised(Enemy.DisplayName));
                        Item.IndentLevel = 1;
                        Item.ImageFile = "Data\\Images\\HUD\\flags\\" + Enemy.FlagFilename + ".png";
                        AlliesList.AddItem(Item);
                    }
                }
            }
        }

        public void TradeOfferRealmsClick(UIControl Control)
        {
            // Realm list is always visible; keep handler for compatibility.
            if (GameSystem.UIActionAllowed("RelationsRealmChange") == false) return;
            GameForm.GetControlByName<ControlImage>("TradeOfferRealmsBG").Visible = true;
        }

        public void TradeOfferRealmClicked(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("RelationsRealmChange") == false) return;
            if (RealmList.ClickedItem == null) return;

            WorkingRealm Realm = RealmList.ClickedItem.Data;
            if (Realm != null)
            {
                DiplomacyRealm = Realm;
                UpdateAll();
                GameSystem.FireEvent("DiplomacyRealmChanged", DiplomacyRealm);
            }
        }

        private static GameText BuildAgentAssignmentTooltip(Sovereignty Game, WorkingAgent Agent, bool IsFree)
        {
            if (Agent == null) return GameText.CreateFromLiteral("");

            if (IsFree)
            {
                return GameText.CreateFromLiteral("Free\nAvailable for a new assignment.");
            }

            StringBuilder Builder = new StringBuilder();
            string Role = "Diplomat";
            string Mission = "";
            string Status = Agent.TurnsLeft > 0 ? "Travelling" : "On assignment";
            string Location = GameText.CreateLocalised(Agent.HostRealm.DisplayName).GetActualText(Game);

            switch (Agent.CurrentMode)
            {
                case AgentModes.CarryTradeOffer:
                    Role = "Trader";
                    Mission = "Delivering a trade offer";
                    Status = "Travelling";
                    break;
                case AgentModes.DamageForeignRelations:
                    Role = "Provocateur";
                    Mission = "Damaging relations with " + GameText.CreateLocalised(Agent.TargetRealm.DisplayName).GetActualText(Game);
                    break;
                case AgentModes.ImproveForeignRelations:
                    Role = "Emissary";
                    Mission = "Improving relations with " + GameText.CreateLocalised(Agent.TargetRealm.DisplayName).GetActualText(Game);
                    break;
                case AgentModes.ImproveRelations:
                    Role = "Diplomat";
                    Mission = "Improving relations with " + Location;
                    break;
                case AgentModes.Military:
                    Role = "Spy";
                    Mission = "Gathering military intelligence";
                    break;
                case AgentModes.Incite:
                    Role = "Provocateur";
                    Mission = "Inciting unrest";
                    break;
                case AgentModes.Idle:
                    Role = "Diplomat";
                    Mission = Agent.TurnsLeft > 0 ? "Returning home" : "Idle";
                    Status = Agent.TurnsLeft > 0 ? "Travelling" : "Idle";
                    break;
            }

            Builder.AppendLine(Role);
            Builder.AppendLine("Location: " + Location);
            if (!string.IsNullOrEmpty(Mission)) Builder.AppendLine("Mission: " + Mission);
            Builder.AppendLine("Status: " + Status);
            if (Agent.TurnsLeft > 0) Builder.Append("Turns left: " + Agent.TurnsLeft);
            else if (Agent.HostRealm != Agent.OwnerRealm) Builder.Append("Turns left: ∞");

            return GameText.CreateFromLiteral(Builder.ToString().TrimEnd());
        }

        private void ResetRealmList()
        {
            RealmList.ClearItems();
            foreach (WorkingRealm Realm in GameSystem.CurrentGame.AllRealms.Values.OrderBy(x => GetLocalName(x)))
            {
                if (Realm == GameSystem.CurrentGame.RebelRealm) continue;
                if (Realm.RealmIsDead) continue;

                RealmList.AddItem(Realm);
                CovertList.AddItem(Realm);
            }
        }

        private string GetLocalName(WorkingRealm x)
        {
            GameText Text = GameText.CreateLocalised(x.DisplayName);
            return Text.GetActualText(GameSystem);
        }

        public void CloseButtonClick(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("RelationsClose") == false) return;

            ClearAuxPin();
            GameForm.Visible = false;
            GameSystem.FireEvent("RelationsFormClosed");
        }
    }
}
