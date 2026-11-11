using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SovereigntyTK.UI
{
    public class ControlEconomyCard: UIControl
    {
        ControlImage CardBGImage;
        private WorkingProvince CurrentProvince;

        ControlText TitleText;
        
        ControlImage EconomyIcon;
        ControlImage EconomyBG;
        ControlText EconomyText;

        ControlImage ResistIcon;
        ControlImage ResistBG;
        ControlText ResistText;

        ControlText BuildingsText;

        List<ControlImage> BuildingImages;
        List<ControlButton> BuildingButtons;

        ControlText PurchaseText;

        ControlList<List<BuildingEffect>> AvailableList;

        public ControlEconomyCard(GameBase Game)
            :base(Game)
        {
            Game.RegisterEvent(HandleBuildingsChanged, "BuildingsChanged");

            CreateControls();
        }
        public override void Dispose()
        {
            Game.UnregisterEvent(HandleBuildingsChanged, "BuildingsChanged");
            base.Dispose();
        }

        private void HandleBuildingsChanged(string EventName, params object[] Args)
        {
            WorkingProvince Province = Args[0] as WorkingProvince;
            if (Visible && CurrentProvince == Province) SetProvince(Province);
        }

        private void CreateControls()
        {
            CardBGImage = new ControlImage(Game);
            AddChild(CardBGImage);
            CardBGImage.MouseInputType = MouseInputTypes.None;
            CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\building_panel.png");

            TitleText = new ControlText(Game);
            AddChild(TitleText);
            TitleText.FontName = "Trebuchet MS";
            TitleText.Colour = Color.FromArgb(255, 238, 161);
            TitleText.TextAnchor = AnchorPoints.Middle;
            TitleText.MouseInputType = MouseInputTypes.None;
            TitleText.BubbleMouseovers = true;

            EconomyIcon = new ControlImage(Game);
            AddChild(EconomyIcon);
            EconomyIcon.MouseInputType = MouseInputTypes.Forced;
            EconomyIcon.SetImageFile("Data\\Images\\HUD\\Info\\economyicon.png");

            EconomyBG = new ControlImage(Game);
            AddChild(EconomyBG);
            EconomyBG.MouseInputType = MouseInputTypes.Forced;
            EconomyBG.SetImageFile("Data\\Images\\HUD\\Info\\value.png");

            EconomyText = new ControlText(Game);
            AddChild(EconomyText);
            EconomyText.FontName = "Trebuchet MS";
            EconomyText.Colour = Color.FromArgb(71, 42, 28);
            EconomyText.TextAnchor = AnchorPoints.Middle;
            EconomyText.MouseInputType = MouseInputTypes.None;
            EconomyText.BubbleMouseovers = true;

            ResistIcon = new ControlImage(Game);
            AddChild(ResistIcon);
            ResistIcon.MouseInputType = MouseInputTypes.Forced;
            ResistIcon.SetImageFile("Data\\Images\\HUD\\Info\\resistanceicon.png");
            ResistIcon.SetTooltip(GameText.CreateLocalised("TT_BUILDING_RESIST"));

            ResistBG = new ControlImage(Game);
            AddChild(ResistBG);
            ResistBG.MouseInputType = MouseInputTypes.Forced;
            ResistBG.SetImageFile("Data\\Images\\HUD\\Info\\value.png");
            ResistBG.SetTooltip(GameText.CreateLocalised("TT_BUILDING_RESIST"));

            ResistText = new ControlText(Game);
            AddChild(ResistText);
            ResistText.FontName = "Trebuchet MS";
            ResistText.Colour = Color.FromArgb(71, 42, 28);
            ResistText.TextAnchor = AnchorPoints.Middle;
            ResistText.MouseInputType = MouseInputTypes.None;
            ResistText.BubbleMouseovers = true;

            BuildingsText = new ControlText(Game);
            AddChild(BuildingsText);
            BuildingsText.FontName = "Trebuchet MS";
            BuildingsText.Colour = Color.FromArgb(255, 238, 161);
            BuildingsText.TextAnchor = AnchorPoints.Middle;
            BuildingsText.MouseInputType = MouseInputTypes.None;
            BuildingsText.BubbleMouseovers = true;
            BuildingsText.SetTextData(GameText.CreateLocalised("BUILDING_TITLE"));

            BuildingImages = new List<ControlImage>();
            BuildingButtons = new List<ControlButton>();

            for (int i = 0; i < 10; i++)
            {
                ControlButton BuildingButton = new ControlButton(Game);
                AddChild(BuildingButton);
                BuildingButton.OnClick += DemolishButton_OnClick;
                BuildingButton.OnMouseEnter += BuildingButton_OnMouseEnter;
                BuildingButton.OnMouseLeave += BuildingButton_OnMouseLeave;
                BuildingButtons.Add(BuildingButton);

                ControlImage BuildingImage = new ControlImage(Game);
                AddChild(BuildingImage);
                BuildingImages.Add(BuildingImage);
            }

            PurchaseText = new ControlText(Game);
            AddChild(PurchaseText);
            PurchaseText.FontName = "Trebuchet MS";
            PurchaseText.Colour = Color.FromArgb(255, 238, 161);
            PurchaseText.TextAnchor = AnchorPoints.Middle;
            PurchaseText.MouseInputType = MouseInputTypes.None;
            PurchaseText.BubbleMouseovers = true;
            PurchaseText.SetTextData(GameText.CreateLocalised("BUILDING_PURCHASE"));

            AvailableList = new ControlList<List<BuildingEffect>>(Game);
            AvailableList.ControlName = "BuildingAvailableList";
            AddChild(AvailableList);
            AvailableList.VisibleItems = 2;
            AvailableList.ManualUpdate = true;
            AvailableList.OnListItemUpdated += AvailableList_OnListItemUpdated;
            AvailableList.HighlightEnabled = false;
            AvailableList.SelectionEnabled = false;
        }

        void AvailableList_OnListItemUpdated(ListItem<List<BuildingEffect>> Item)
        {
            float ItemWidth = Item.Container.Sprite.Bounds.Width * 0.2f;
            float ItemHeight = Item.Container.Sprite.Bounds.Height;
            float X = 0;

            foreach (BuildingEffect Building in Item.Data)
            {
                ControlButton BuildingButton = new ControlButton(Game);
                Item.Container.AddChild(BuildingButton);
                BuildingButton.SetBounds(X + ItemWidth * 0.05f, ItemWidth * 0.05f, ItemWidth * 0.9f, ItemWidth * 0.9f, UIUnits.PixelScaled);
                BuildingButton.MouseInputType = MouseInputTypes.Forced;

                string Img1 = "Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_normal.png";
                string Img2 = "Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_highlight.png";
                string Img3 = "Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_pressed.png";

                BuildingButton.SetImageFiles(Img1, Img2, Img3);
                BuildingButton.CustomData = Building;
                BuildingButton.OnMouseEnter += BuildingButton_OnMouseEnter;
                BuildingButton.OnMouseLeave += BuildingButton_OnMouseLeave;

                if (CurrentProvince.CanBuild(Building) && CurrentProvince.OwnerRealm.AIPlayer == null)
                {
                    BuildingButton.OnClick += BuildingButton_OnClick;
                    BuildingButton.CustomData = Building;
                }
                else
                {
                    BuildingButton.Disabled = true;

                    Img1 = "Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_disabled.png";
                    BuildingButton.SetImageFiles(Img1, Img1, Img1);
                }

                ControlImage Frame = new ControlImage(Game);
                Item.Container.AddChild(Frame);
                Frame.SetImageFile("Data\\Images\\HUD\\Info\\building_built.png");
                Frame.SetBounds(X + ItemWidth * 0.05f, ItemWidth * 0.05f, ItemWidth * 0.9f, ItemWidth * 0.9f, UIUnits.PixelScaled);
                Frame.MouseInputType = MouseInputTypes.None;

                ControlImage Tier = new ControlImage(Game);
                Frame.AddChild(Tier);
                Tier.SetImageFile("Data\\Images\\HUD\\Economy\\Tier" + Building.Data.Tier + ".png");
                Tier.SetAnchor(AnchorPoints.TopRight);
                Tier.SetBounds(ItemWidth * 0.05f, ItemWidth * 0.05f, ItemWidth * 0.3f, ItemWidth * 0.3f, UIUnits.PixelScaled);
                Tier.MouseInputType = MouseInputTypes.None;

                List<GameText> TT = new List<GameText>();
                TT.Add(GameText.CreateLocalised(Building.Data.DisplayName));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised("FORMAT_BUILDTIER", Building.Data.Tier));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised("FORMAT_BUILDLIMIT", Building.Data.MaxNumber));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised(Building.Data.DisplayDesc));
                BuildingButton.SetTooltip(TT);

                GameText CostTT = GameText.CreateLocalised("FORMAT_COST", CurrentProvince.GetBuildingGoldCost(Building));

                ControlImage CostImage = new ControlImage(Game);
                Item.Container.AddChild(CostImage);
                CostImage.SetBounds(X + ItemWidth * 0.05f, ItemWidth, ItemWidth * 0.25f, ItemWidth * 0.25f, UIUnits.PixelScaled);
                CostImage.SetImageFile("Data\\Images\\HUD\\Purchase\\stat_purchasecost.png");
                CostImage.MouseInputType = MouseInputTypes.Forced;
                CostImage.SetTooltip(CostTT);

                ControlText CostText = new ControlText(Game);
                Item.Container.AddChild(CostText);
                CostText.FontName = "Trebuchet MS";
                CostText.Colour = Color.FromArgb(255, 238, 161);
                CostText.TextAnchor = AnchorPoints.Left;
                CostText.SetBounds(X + ItemWidth * 0.30f, ItemWidth, ItemWidth * 0.75f, ItemWidth * 0.25f, UIUnits.PixelScaled);
                CostText.SetFontSize((int)(CostText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
                CostText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", CurrentProvince.GetBuildingGoldCost(Building)));
                CostText.MouseInputType = MouseInputTypes.Forced;
                CostText.SetTooltip(CostTT);

                if (CurrentProvince.Game.PlayerRealm.Gold < CurrentProvince.GetBuildingGoldCost(Building)) CostText.Colour = Color.FromArgb(255, 63, 0);

                Dictionary<string, int> ResourceCosts = CurrentProvince.GetBuildingResourceCosts(Building);

                if (ResourceCosts.Count > 0)
                {
                    ResourceData Resource = Building.Game.GameCore.Data.Resources[ResourceCosts.ElementAt(0).Key];

                    GameText ResTT = GameText.CreateLocalised("FORMAT_COSTRES", ResourceCosts.ElementAt(0).Value);
                    ResTT.AddChildText(GameText.CreateLocalised(Resource.DisplayName));

                    ControlImage ResImage = new ControlImage(Game);
                    Item.Container.AddChild(ResImage);
                    ResImage.SetBounds(X + ItemWidth * 0.05f, ItemWidth + (ItemWidth * 0.25f), ItemWidth * 0.25f, ItemWidth * 0.25f, UIUnits.PixelScaled);
                    ResImage.SetImageFile("Data\\Images\\HUD\\Purchase\\currency_" + Resource.ResourceName + ".png");
                    ResImage.MouseInputType = MouseInputTypes.Forced;
                    ResImage.SetTooltip(ResTT);

                    ControlText ResCostText = new ControlText(Game);
                    Item.Container.AddChild(ResCostText);
                    ResCostText.FontName = "Trebuchet MS";
                    ResCostText.Colour = Color.FromArgb(255, 238, 161);
                    ResCostText.TextAnchor = AnchorPoints.Left;
                    ResCostText.SetBounds(X + ItemWidth * 0.30f, ItemWidth + (ItemWidth * 0.25f), ItemWidth * 0.75f, ItemWidth * 0.25f, UIUnits.PixelScaled);
                    ResCostText.SetFontSize((int)(ResCostText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
                    ResCostText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", ResourceCosts.ElementAt(0).Value));
                    ResCostText.MouseInputType = MouseInputTypes.Forced;
                    ResCostText.SetTooltip(ResTT);

                    if (CurrentProvince.Game.PlayerRealm.GetStockpiledResource(Resource) < ResourceCosts.ElementAt(0).Value) ResCostText.Colour = Color.FromArgb(255, 63, 0);
                }

                if (ResourceCosts.Count > 1)
                {
                    ResourceData Resource = Building.Game.GameCore.Data.Resources[ResourceCosts.ElementAt(1).Key];

                    GameText ResTT = GameText.CreateLocalised("FORMAT_COSTRES", ResourceCosts.ElementAt(1).Value);
                    ResTT.AddChildText(GameText.CreateLocalised(Resource.DisplayName));

                    ControlImage ResImage = new ControlImage(Game);
                    Item.Container.AddChild(ResImage);
                    ResImage.SetBounds(X + ItemWidth * 0.05f, ItemWidth + (ItemWidth * 0.50f), ItemWidth * 0.25f, ItemWidth * 0.25f, UIUnits.PixelScaled);
                    ResImage.SetImageFile("Data\\Images\\HUD\\Purchase\\currency_" + Resource.ResourceName + ".png");
                    ResImage.MouseInputType = MouseInputTypes.Forced;
                    ResImage.SetTooltip(ResTT);

                    ControlText ResCostText = new ControlText(Game);
                    Item.Container.AddChild(ResCostText);
                    ResCostText.FontName = "Trebuchet MS";
                    ResCostText.Colour = Color.FromArgb(255, 238, 161);
                    ResCostText.TextAnchor = AnchorPoints.Left;
                    ResCostText.SetBounds(X + ItemWidth * 0.30f, ItemWidth + (ItemWidth * 0.50f), ItemWidth * 0.75f, ItemWidth * 0.25f, UIUnits.PixelScaled);
                    ResCostText.SetFontSize((int)(ResCostText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
                    ResCostText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", ResourceCosts.ElementAt(1).Value));
                    ResCostText.MouseInputType = MouseInputTypes.Forced;
                    ResCostText.SetTooltip(ResTT);

                    if (CurrentProvince.Game.PlayerRealm.GetStockpiledResource(Resource) < ResourceCosts.ElementAt(1).Value) ResCostText.Colour = Color.FromArgb(255, 63, 0);
                }

                X += ItemWidth;
            }
        }

        private void BuildingButton_OnMouseEnter(UIControl Control)
        {
            BuildingEffect Building = Control.CustomData as BuildingEffect;
            if (Building == null || CurrentProvince == null) return;

            bool CanBuild = CurrentProvince.CanBuild(Building) && CurrentProvince.OwnerRealm.AIPlayer == null;
            bool IsConstructed = CurrentProvince.Buildings.Contains(Building);
            Game.FireEvent("BuildingMouseover", new BuildingCardData(Building, CurrentProvince, CanBuild, IsConstructed));
        }

        private void BuildingButton_OnMouseLeave(UIControl Control)
        {
            Game.FireEvent("BuildingMouseover", (BuildingCardData)null);
        }

        private void DemolishButton_OnClick(UIControl Control)
        {
            BuildingEffect Building = Control.CustomData as BuildingEffect;
            if (Building.Game.GameCore.UIActionAllowed("DemolishBuilding") == false) return;

            if (CurrentProvince.ConstructionState != ConstructionStates.Free || CurrentProvince.OwnerRealm.AIPlayer != null)
            {
                return;
            }

            List<BuildingEffect> SupportedBuildings = CurrentProvince.GetSupportedBuildings(Building);
            if (SupportedBuildings.Count > 0)
            {
                GameText TitleText = GameText.CreateLocalised("MSG_NODEMOLISH_TITLE");
                GameText IntroText = GameText.CreateLocalised("MSG_NODEMOLISH_TEXT");
                IntroText.AddChildText(GameText.CreateLocalised(Building.Data.DisplayName));
                List<GameText> MsgText = new List<GameText>();

                MsgText.Add(IntroText);
                MsgText.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                MsgText.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));

                foreach (BuildingEffect Supported in SupportedBuildings)
                {
                    MsgText.Add(GameText.CreateLocalised(Supported.Data.DisplayName));
                    MsgText.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                }

                MessageBoxData Msg = new MessageBoxData();
                Msg.CaptionText = TitleText;
                Msg.MessageTextList = MsgText;
                Msg.DisplayType = MessageBoxType.Info;
                Msg.MsgType = MessageType.GenericInfo;
                CurrentProvince.Game.GameCore.MessageHandler.ShowMessage(Msg);

                return;
            }

            MessageBoxData Data = new MessageBoxData();
            Data.CaptionText = GameText.CreateLocalised("MSG_DEMOLISH_TITLE");
            Data.MessageText = GameText.CreateLocalised("MSG_DEMOLISH_TEXT");
            Data.MessageText.AddChildText(GameText.CreateLocalised(Building.Data.DisplayName));
            Data.DisplayType = MessageBoxType.YesNo;
            Data.MsgType = MessageType.Demolish;
            Data.YesText = GameText.CreateLocalised("MSG_DEMOLISH_YES");
            Data.NoText = GameText.CreateLocalised("MSG_DEMOLISH_NO");
            Data.Building = Building;
            Data.Province = CurrentProvince;
            CurrentProvince.Game.GameCore.MessageHandler.ShowMessage(Data);
        }

        void BuildingButton_OnClick(UIControl Control)
        {
            BuildingEffect Building = Control.CustomData as BuildingEffect;
            if (Building.Game.GameCore.UIActionAllowed("ImproveProvince") == false) return;

            BuildingEffect ConstructedBuilding = BuildingEffect.CreateEffect(CurrentProvince.Game, Building.Data, CurrentProvince);
            ConstructedBuilding.Construct(CurrentProvince.OwnerRealm, CurrentProvince, true);
            CurrentProvince.ConstructionState = ConstructionStates.Building;

            Game.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\upgrade_economy.wav");

            Game.FireEvent("BuildingsChanged", CurrentProvince);
        }

        private void SetControlPositions()
        {
            float W = Sprite.Bounds.Width;
            float H = Sprite.Bounds.Height;

            CardBGImage.SetBounds(0, 0, W, H, UIUnits.PixelScaled);

            TitleText.SetBounds(W * 0.022f, H * 0.011f, W * 0.954f, H * 0.051f, UIUnits.PixelScaled);

            EconomyIcon.SetBounds(W * 0.181f, H * 0.101f, W * 0.086f, H * 0.051f, UIUnits.PixelScaled);
            EconomyBG.SetBounds(W * 0.275f, H * 0.081f, W * 0.158f, H * 0.094f, UIUnits.PixelScaled);
            EconomyText.SetBounds(W * 0.275f, H * 0.081f, W * 0.158f, H * 0.094f, UIUnits.PixelScaled);

            ResistIcon.SetBounds(W * 0.562f, H * 0.101f, W * 0.086f, H * 0.051f, UIUnits.PixelScaled);
            ResistBG.SetBounds(W * 0.656f, H * 0.081f, W * 0.158f, H * 0.094f, UIUnits.PixelScaled);
            ResistText.SetBounds(W * 0.656f, H * 0.081f, W * 0.158f, H * 0.094f, UIUnits.PixelScaled);

            BuildingsText.SetBounds(W * 0.332f, H * 0.191f, W * 0.335f, H * 0.045f, UIUnits.PixelScaled);

            float Y = 0.243f;
            float X = 0.026f;

            for (int i = 0; i < 10; i++)
            {
                if (i == 5)
                {
                    Y = 0.354f;
                    X = 0.026f;
                }

                BuildingImages[i].SetBounds(W * X, H * Y, W * 0.177f, H * 0.097f, UIUnits.PixelScaled);
                BuildingButtons[i].SetBounds(W * X, H * Y, W * 0.177f, H * 0.097f, UIUnits.PixelScaled);

                X += 0.192f;
            }

            PurchaseText.SetBounds(W * 0.018f, H * 0.460f, W * 0.962f, H * 0.051f, UIUnits.PixelScaled);
            AvailableList.SetBounds(W * 0.022f, H * 0.523f, W * 0.954f, H * 0.462f, UIUnits.PixelScaled);

            TitleText.SetFontSize((int)(TitleText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            EconomyText.SetFontSize((int)(EconomyText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            ResistText.SetFontSize((int)(ResistText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            BuildingsText.SetFontSize((int)(BuildingsText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            PurchaseText.SetFontSize((int)(PurchaseText.Sprite.Bounds.Height * 0.5f), UIUnits.PixelScaled);
        }

        public void SetProvince(WorkingProvince Province)
        {
            CurrentProvince = Province;

            TitleText.SetTextData(GameText.CreateLocalised(Province.DisplayName));

            switch (Province.ConstructionState)
            {
                case ConstructionStates.Free: PurchaseText.SetTextData(GameText.CreateLocalised("BUILDING_PURCHASE")); break;
                case ConstructionStates.Building: PurchaseText.SetTextData(GameText.CreateLocalised("BUILDING_NOPURCHASE1")); break;
                case ConstructionStates.Demolishing: PurchaseText.SetTextData(GameText.CreateLocalised("BUILDING_NOPURCHASE2")); break;
            }

            if (CurrentProvince.Occupied)
            {
                ResistBG.Visible = true;
                ResistIcon.Visible = true;
                ResistText.Visible = true;
                ResistText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Province.ActiveResistance));
            }
            else
            {
                ResistBG.Visible = false;
                ResistIcon.Visible = false;
                ResistText.Visible = false;
            }

            EconomyText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Province.CurrentEconomy));

            List<GameText> IncomeTip = ProvinceIncomeTooltip.BuildTooltip(Province);
            EconomyIcon.SetTooltip(IncomeTip);
            EconomyBG.SetTooltip(IncomeTip);

            int i = 0;
            foreach (BuildingEffect Building in Province.Buildings.OrderBy(b => b.Data.Tier).ThenBy(b => b.Data.DisplayName))
            {
                BuildingImages[i].Visible = true;
                BuildingImages[i].SetImageFile("Data\\Images\\HUD\\Info\\building_built.png");

                BuildingButtons[i].Visible = true;

                string Img1 = "Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_normal.png";
                string Img2 = "Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_highlight.png";
                string Img3 = "Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_pressed.png";

                BuildingButtons[i].SetImageFiles(Img1, Img2, Img3);
                BuildingButtons[i].CustomData = Building;

                List<GameText> TT = new List<GameText>();
                TT.Add(GameText.CreateLocalised(Building.Data.DisplayName));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised(Building.Data.DisplayDesc));
                BuildingButtons[i].SetTooltip(TT);

                i++;
            }

            while (i < Province.CurrentEconomy)
            {
                BuildingImages[i].Visible = true;
                BuildingImages[i].SetImageFile("Data\\Images\\HUD\\Info\\building_planned.png");
                BuildingImages[i].SetTooltip(GameText.CreateLocalised("TT_PROVINCE_CANBUILD", Province.MaxBuildableTier()));

                BuildingButtons[i].Visible = false;

                i++;
            }

            while (i < 10)
            {
                BuildingImages[i].Visible = false;
                BuildingButtons[i].Visible = false;

                i++;
            }

            List<BuildingEffect> AvailableBuildings = BuildingEffect.GetAllEffects(Province.Game);
            AvailableBuildings = AvailableBuildings.Where(x => ShouldDisplay(x)).OrderBy(x => CurrentProvince.Game.GameCore.Data.BuildingAffinities[x.BuildingName].GetAffinity(CurrentProvince.OwnerRealm.Name)).Take(10).ToList();
            AvailableBuildings.Sort(BuildingComparer);
            AvailableList.ClearItems();

            while (AvailableBuildings.Count > 0)
            {
                int Count = Math.Min(5, AvailableBuildings.Count);

                List<BuildingEffect> TempList = AvailableBuildings.Take(Count).ToList();
                AvailableBuildings.RemoveRange(0, Count);

                AvailableList.AddItem(TempList);
            }
        }

        private int BuildingComparer(BuildingEffect x, BuildingEffect y)
        {
            if (x.Data.Tier != y.Data.Tier) return x.Data.Tier.CompareTo(y.Data.Tier);
            return x.Data.GoldCost.CompareTo(y.Data.GoldCost);
        }

        private bool ShouldDisplay(BuildingEffect Building)
        {
            //Display only buildings if the building has a positive value in the Sov_charts, BuildRealm sheet.
            if (CurrentProvince.Game.GameCore.Data.BuildingAffinities[Building.BuildingName].GetAffinity(CurrentProvince.OwnerRealm.Name) == 0) return false;

            //If a Building Tier > than the Province Economy value, do not display that Building.
            if (Building.Data.Tier > CurrentProvince.CurrentEconomy) return false;

            //If the Building Requirements are not met, do not display that Building.
            if (Building.CanBuildInProvince(CurrentProvince.OwnerRealm, CurrentProvince) == false) return false;

            return true;
        }

        protected override void ReclaculateBounds()
        {
            RectangleF OldBounds = Sprite.Bounds;

            base.ReclaculateBounds();

            if (OldBounds.Size == Sprite.Bounds.Size) return;
            if (CardBGImage == null) return;

            //if the size of the control has changed
            //the internal controls need to have their positions/sizes updated to reflect this
            SetControlPositions();
        }
    }
}
