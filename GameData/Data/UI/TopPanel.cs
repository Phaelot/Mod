using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace SovereigntyTK.UI
{
    public enum EconomyCategories
    {
        Provinces,
        Units,
        Landmarks,
        Magic,
        Prisoners,
        Buildings,
        Treasury,
        Trade
    }

    public class EconomyListItem
    {
        public int IndentLevel;
        public bool HeaderItem;
        public bool Closed;
        public bool OddItem;
        public EconomyCategories Category;
        public GameText Caption;
        public GameText Tooltip;
        public int Value;
        public List<EconomyListItem> Children;

        public EconomyListItem()
        {
            Children = new List<EconomyListItem>();
        }
    }

    public class TopPanel : ScriptBase
    {
        public PanelStates CurrentState = PanelStates.Closed;
        ControlContainer MainControl;

        ControlList<EconomyListItem> IncomeList;
        ControlList<EconomyListItem> ExpensesList;

        List<EconomyListItem> IncomeSourceList;
        List<EconomyListItem> ExpenseSourceList;

        public void FormLoaded(UIControl Control)
        {
            UpdateGold();
            UpdateDate();

            GameSystem.RegisterEvent(HandleCloseRequest, "CloseFinancePanel");

            GameSystem.CurrentGame.PlayerRealm.Gold.OnStatChanged += UpdateGold;
            GameSystem.CurrentGame.TurnController.CurrentDate.OnStatChanged += UpdateDate;

            MainControl = GameForm.GetControlByName<ControlContainer>("TopPanelMain");

            GameText Title = GameText.CreateLocalised("FINANCE_TITLE");
            Title.AddChildText(GameText.CreateLocalised(GameSystem.CurrentGame.PlayerRealm.DisplayName));
            GameForm.GetControlByName<ControlText>("FinanceTitle").SetTextData(Title);

            ControlImage IncomeBG = GameForm.GetControlByName<ControlImage>("IncomeBG");
            ControlImage ExpenseBG = GameForm.GetControlByName<ControlImage>("ExpenseBG");

            IncomeList = new ControlList<EconomyListItem>(GameSystem);
            IncomeBG.AddChild(IncomeList);
            IncomeList.SetPosition(10, 30, UIUnits.Pixel);
            IncomeList.SetSize(280, 400, UIUnits.Pixel);
            IncomeList.SetVisibleItems(30);
            IncomeList.ManualUpdate = true;
            IncomeList.OnListItemUpdated += EconomyListItemUpdated;
            IncomeList.HighlightEnabled = false;
            IncomeList.SelectionEnabled = false;
            IncomeList.OnItemClicked += IncomeList_OnItemClicked;

            ExpensesList = new ControlList<EconomyListItem>(GameSystem);
            ExpenseBG.AddChild(ExpensesList);
            ExpensesList.SetPosition(10, 30, UIUnits.Pixel);
            ExpensesList.SetSize(280, 400, UIUnits.Pixel);
            ExpensesList.SetVisibleItems(30);
            ExpensesList.ManualUpdate = true;
            ExpensesList.OnListItemUpdated += EconomyListItemUpdated;
            ExpensesList.HighlightEnabled = false;
            ExpensesList.SelectionEnabled = false;
            ExpensesList.OnItemClicked += IncomeList_OnItemClicked;
        }

        private void HandleCloseRequest(string EventName, params object[] Args)
        {
            if (CurrentState == PanelStates.Open || CurrentState == PanelStates.Opening)
            {
                ClosePanel();
            }
        }

        void IncomeList_OnItemClicked(UIControl Control)
        {
            ControlList<EconomyListItem> List = (Control as ControlList<EconomyListItem>);
            EconomyListItem Item = List.ClickedItem.Data;

            Item.Closed = !Item.Closed;
            UpdateLists();
        }

        private void EconomyListItemUpdated(ListItem<EconomyListItem> Item)
        {
            //header draws with yellow background and bold text, value and tooltip are ignored
            //normal draws with light/dark shaded background (based on odditem) with value right aligned, and tooltip applied if present
            //header rows also include up/down arrow based on open/closed status
            //category element used to determine which category to collapse or expand when button clicked

            float TextLeft = (Item.Data.IndentLevel + 1) * Item.Container.Sprite.Bounds.Width * 0.05f;
            float ButtonLeft = Item.Data.IndentLevel * Item.Container.Sprite.Bounds.Width * 0.05f;

            if (Item.Data.Children.Count > 0)
            {
                if (Item.Data.Closed)
                {
                    ControlImage Expand = new ControlImage(GameSystem);
                    Expand.SetBounds(ButtonLeft, 0, Item.Container.Sprite.Bounds.Width * 0.05f, Item.Container.Sprite.Bounds.Width * 0.05f, UIUnits.PixelScaled);
                    Expand.SetImageFile("Data\\Images\\HUD\\Economy\\arrow_down_normal.png");
                    Expand.MouseInputType = MouseInputTypes.None;
                    Item.Container.AddChild(Expand);
                }
                else
                {
                    ControlImage Expand = new ControlImage(GameSystem);
                    Expand.SetBounds(ButtonLeft, 0, Item.Container.Sprite.Bounds.Width * 0.05f, Item.Container.Sprite.Bounds.Width * 0.05f, UIUnits.PixelScaled);
                    Expand.SetImageFile("Data\\Images\\HUD\\Economy\\arrow_up_normal.png");
                    Expand.MouseInputType = MouseInputTypes.None;
                    Item.Container.AddChild(Expand);
                }
            }

            if (Item.Data.HeaderItem)
            {
                ControlImage UnitImage = new ControlImage(GameSystem);
                UnitImage.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                UnitImage.SetImageFile("Data\\Images\\HUD\\ListSelect.png");
                UnitImage.MouseInputType = MouseInputTypes.None;
                Item.Container.AddChild(UnitImage);

                ControlText UnitText = new ControlText(GameSystem);
                UnitText.SetBounds(TextLeft, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
                UnitText.FontName = "Trebuchet MS";
                UnitText.Bold = true;
                UnitText.TextAnchor = AnchorPoints.Left;
                UnitText.SetTextData(Item.Data.Caption);
                Item.Container.AddChild(UnitText);
            }
            else
            {
                if (Item.Data.OddItem)
                {
                    ControlImage UnitImage = new ControlImage(GameSystem);
                    UnitImage.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                    UnitImage.SetImageFile("Data\\Images\\HUD\\ListHighlight.png");
                    UnitImage.Sprite.SetAlpha(0.5f);
                    UnitImage.MouseInputType = MouseInputTypes.None;
                    Item.Container.AddChild(UnitImage);
                }

                ControlText UnitText = new ControlText(GameSystem);
                UnitText.SetBounds(TextLeft, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
                UnitText.FontName = "Trebuchet MS";
                UnitText.TextAnchor = AnchorPoints.Left;
                UnitText.SetTextData(Item.Data.Caption);
                Item.Container.AddChild(UnitText);

                ControlText ValueText = new ControlText(GameSystem);
                ValueText.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
                ValueText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
                ValueText.FontName = "Trebuchet MS";
                ValueText.TextAnchor = AnchorPoints.Right;
                ValueText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Item.Data.Value));
                Item.Container.AddChild(ValueText);

                if (Item.Data.IndentLevel == 2)
                {
                    UnitText.Colour = Color.CornflowerBlue;
                    ValueText.Colour = Color.CornflowerBlue;
                }
            }
        }

        void UpdateLedger()
        {
            ControlImage BG = GameForm.GetControlByName<ControlImage>("PanelBG");
            switch (GameSystem.CurrentGame.PlayerRealm.Panel)
            {
                case SpellSchools.Death: BG.SetImageFile("Data\\Images\\HUD\\bg_death_square.png"); break;
                case SpellSchools.Illusion: BG.SetImageFile("Data\\Images\\HUD\\bg_illusion_square.png"); break;
                case SpellSchools.Nature: BG.SetImageFile("Data\\Images\\HUD\\bg_nature_square.png"); break;
                case SpellSchools.War: BG.SetImageFile("Data\\Images\\HUD\\bg_war_square.png"); break;
            }

            //list all of the details in a tree structure
            //then call another function to create the flat lists for display
            //this second function is also called when a +/- button is clicked

            IncomeList.ClearItems();
            ExpensesList.ClearItems();

            IncomeSourceList = new List<EconomyListItem>();
            ExpenseSourceList = new List<EconomyListItem>();

            //landmark income
            EconomyListItem Header = new EconomyListItem();
            Header.HeaderItem = true;
            Header.Category = EconomyCategories.Landmarks;
            Header.Caption = GameText.CreateLocalised("HEADER_LANDMARK");
            IncomeSourceList.Add(Header);

            if (GameSystem.CurrentGame.PlayerRealm.HasLandmark("Nourel's Mine"))
            {
                EconomyListItem Item = new EconomyListItem();
                Item.IndentLevel = 1;
                Item.Caption = GameText.CreateLocalised("LANDMARKNAME18");
                Item.Value = 150;
                Header.Children.Add(Item);
            }

            //province income
            EconomyListItem ProvinceHeader = new EconomyListItem();
            ProvinceHeader.HeaderItem = true;
            ProvinceHeader.Category = EconomyCategories.Provinces;
            ProvinceHeader.Caption = GameText.CreateLocalised("HEADER_PROVINCE");
            IncomeSourceList.Add(ProvinceHeader);

            foreach (WorkingProvince Province in GameSystem.CurrentGame.PlayerRealm.Provinces)
            {
                EconomyListItem Item = new EconomyListItem();
                Item.Caption = GameText.CreateLocalised(Province.DisplayName);
                Item.Value = GameSystem.CurrentGame.EconomyController.GetProvinceIncome(Province, GameSystem.CurrentGame.PlayerRealm.CapitolProvince, 1f);
                Item.Closed = true;
                Item.IndentLevel = 1;
                ProvinceHeader.Children.Add(Item);

                if (Province.Cradle == ArtScienceTypes.Alchemy)
                {
                    int Value = 75;
                    if (GameSystem.CurrentGame.PlayerRealm.MagicData.SpellCast == false) Value = 0;

                    EconomyListItem CradleItem = new EconomyListItem();
                    CradleItem.Caption = GameText.CreateLocalised("CRADLE_ALCHEMY");
                    CradleItem.Value = Value;
                    CradleItem.IndentLevel = 2;
                    Item.Children.Add(CradleItem);
                }

                //line for base income from economy level
                EconomyListItem BaseItem = new EconomyListItem();
                BaseItem.Caption = GameText.CreateLocalised("TEXT_ECONOMY");
                BaseItem.Value = Province.CurrentEconomy * 150;
                BaseItem.IndentLevel = 2;
                Item.Children.Add(BaseItem);
                
                //one line for each building which generates base income
                foreach (BuildingEffect Building in Province.Buildings)
                {
                    int Value = Building.GetGoldIncome();
                    if (Value == 0) continue;

                    EconomyListItem BuildItem = new EconomyListItem();
                    BuildItem.Caption = GameText.CreateLocalised(Building.Data.DisplayName);
                    BuildItem.Value = Value;
                    BuildItem.IndentLevel = 2;
                    Item.Children.Add(BuildItem);
                }

                //one line for each spell which is affecting this province
                foreach (SpellEffect Spell in Province.SpellEffects.ActiveSpells)
                {
                    int Value = Spell.GetGoldIncome(Province);
                    if (Value == 0) continue;

                    EconomyListItem BuildItem = new EconomyListItem();
                    BuildItem.Caption = GameText.CreateLocalised(Spell.SpellData.DisplayName);
                    BuildItem.Value = Value;
                    BuildItem.IndentLevel = 2;
                    Item.Children.Add(BuildItem);
                }

                //line for distance penalty
                int Distance;
                int DistanceValue = ProvinceIncomeTooltip.GetDistancePenalty(Province, GameSystem.CurrentGame.PlayerRealm.CapitolProvince, out Distance);
                if (DistanceValue < 0)
                {
                    EconomyListItem DistanceItem = new EconomyListItem();
                    DistanceItem.Caption = ProvinceIncomeTooltip.GetDistanceCaption(Distance);
                    DistanceItem.Value = DistanceValue;
                    DistanceItem.IndentLevel = 2;
                    Item.Children.Add(DistanceItem);
                }
            }

            //building income (direct to treasury)
            EconomyListItem TreasuryHeader = new EconomyListItem();
            TreasuryHeader.HeaderItem = true;
            TreasuryHeader.Category = EconomyCategories.Treasury;
            TreasuryHeader.Caption = GameText.CreateLocalised("HEADER_TREASURY");
            IncomeSourceList.Add(TreasuryHeader);

            foreach (WorkingProvince Province in GameSystem.CurrentGame.PlayerRealm.Provinces)
            {
                foreach (BuildingEffect Building in Province.Buildings)
                {
                    int Value = Building.GetTreasuryIncome();
                    if (Value == 0) continue;

                    EconomyListItem BuildItem = new EconomyListItem();
                    BuildItem.Caption = GameText.CreateLocalised("HEADER_BUILDING");
                    BuildItem.Caption.AddChildText(GameText.CreateLocalised(Building.Data.DisplayName));
                    BuildItem.Caption.AddChildText(GameText.CreateLocalised(Province.DisplayName));
                    BuildItem.Value = Value;
                    BuildItem.IndentLevel = 2;
                    TreasuryHeader.Children.Add(BuildItem);
                }
            }

            //trade income
            EconomyListItem TradeHeader = new EconomyListItem();
            TradeHeader.HeaderItem = true;
            TradeHeader.Category = EconomyCategories.Trade;
            TradeHeader.Caption = GameText.CreateLocalised("HEADER_TRADE");
            IncomeSourceList.Add(TradeHeader);

            foreach (OngoingTrade Trade in GameSystem.CurrentGame.GetOngoingTrades(GameSystem.CurrentGame.PlayerRealm))
            {
                if (Trade.TargetRealm == GameSystem.CurrentGame.PlayerRealm && Trade.Gold > 0)
                {
                    EconomyListItem TradeItem = new EconomyListItem();
                    TradeItem.Caption = GameText.CreateLocalised(Trade.Realm.DisplayName);
                    TradeItem.Value = Trade.Gold;
                    TradeItem.IndentLevel = 1;
                    TradeHeader.Children.Add(TradeItem);
                }
            }

            //unit expenses
            EconomyListItem UnitHeader = new EconomyListItem();
            UnitHeader.HeaderItem = true;
            UnitHeader.Category = EconomyCategories.Units;
            UnitHeader.Caption = GameText.CreateLocalised("HEADER_UNITS");
            ExpenseSourceList.Add(UnitHeader);

            foreach (WorkingStack Stack in GameSystem.CurrentGame.PlayerRealm.Stacks)
            {
                EconomyListItem Item = new EconomyListItem();
                Item.Caption = GameText.CreateLocalised("HEADER_ARMY", Stack.Units.Count);
                Item.Caption.AddChildText(GameText.CreateLocalised(Stack.Node.GetRegion().DisplayName));
                Item.Value = Stack.Units.Sum(x => x.Upkeep);
                Item.IndentLevel = 1;
                Item.Closed = true;
                UnitHeader.Children.Add(Item);

                foreach (WorkingUnit Unit in Stack.Units)
                {
                    EconomyListItem UnitItem = new EconomyListItem();
                    UnitItem.Caption = GameText.CreateLocalised(Unit.DisplayName);
                    UnitItem.Value = Unit.Upkeep;
                    UnitItem.IndentLevel = 2;
                    Item.Children.Add(UnitItem);
                }
            }

            EconomyListItem PrisonerHeader = new EconomyListItem();
            PrisonerHeader.HeaderItem = true;
            PrisonerHeader.Category = EconomyCategories.Prisoners;
            PrisonerHeader.Caption = GameText.CreateLocalised("HEADER_PRISONERS");
            ExpenseSourceList.Add(PrisonerHeader);

            //prisoner expenses
            List<WorkingUnit> Units = GameSystem.CurrentGame.PlayerRealm.Prison.AllPrisoners.ToList();

            foreach (WorkingUnit Unit in Units)
            {
                EconomyListItem Item = new EconomyListItem();
                Item.Caption = GameText.CreateLocalised(Unit.DisplayName);
                Item.Value = (int)(Unit.Upkeep * 0.3f);
                Item.IndentLevel = 1;
                PrisonerHeader.Children.Add(Item);
            }
            
            //magic expenses (research investment)
            EconomyListItem MagicHeader = new EconomyListItem();
            MagicHeader.HeaderItem = true;
            MagicHeader.Category = EconomyCategories.Magic;
            MagicHeader.Caption = GameText.CreateLocalised("HEADER_MAGIC");
            ExpenseSourceList.Add(MagicHeader);

            int MagicValue = GameSystem.CurrentGame.PlayerRealm.MagicData.GetInvestmentCost();

            EconomyListItem MagicItem = new EconomyListItem();
            MagicItem.Caption = GameText.CreateLocalised("RESEARCH_TEXT");
            MagicItem.Value = MagicValue;
            MagicItem.IndentLevel = 1;
            MagicHeader.Children.Add(MagicItem);

            //trade expenses
            TradeHeader = new EconomyListItem();
            TradeHeader.HeaderItem = true;
            TradeHeader.Category = EconomyCategories.Trade;
            TradeHeader.Caption = GameText.CreateLocalised("HEADER_TRADE");
            ExpenseSourceList.Add(TradeHeader);

            foreach (OngoingTrade Trade in GameSystem.CurrentGame.GetOngoingTrades(GameSystem.CurrentGame.PlayerRealm))
            {
                if (Trade.Realm == GameSystem.CurrentGame.PlayerRealm && Trade.Gold > 0)
                {
                    EconomyListItem TradeItem = new EconomyListItem();
                    TradeItem.Caption = GameText.CreateLocalised(Trade.TargetRealm.DisplayName);
                    TradeItem.Value = Trade.Gold;
                    TradeItem.IndentLevel = 1;
                    TradeHeader.Children.Add(TradeItem);
                }
            }

            int Income = GameSystem.CurrentGame.EconomyController.GetRealmTotalIncome(GameSystem.CurrentGame.PlayerRealm);
            int Expense = GameSystem.CurrentGame.EconomyController.GetTotalExpenses(GameSystem.CurrentGame.PlayerRealm);

            GameForm.GetControlByName<ControlText>("TotalIncome").SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Income));
            GameForm.GetControlByName<ControlText>("TotalExpense").SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Expense));
            GameForm.GetControlByName<ControlText>("TotalBalance").SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Income - Expense));

            UpdateLists();
        }

        void UpdateLists()
        {
            IncomeList.ClearItems();
            ExpensesList.ClearItems();

            foreach (EconomyListItem Item in IncomeSourceList) AddToList(Item, IncomeList);
            foreach (EconomyListItem Item in ExpenseSourceList) AddToList(Item, ExpensesList);
        }

        private void AddToList(EconomyListItem Item, ControlList<EconomyListItem> List)
        {
            Item.OddItem = List.Items.Count % 2 == 0;
            List.AddItem(Item);

            if (Item.Closed) return;

            foreach (EconomyListItem Child in Item.Children)
            {
                AddToList(Child, List);
            }
        }

        public void LedgerToggleClick(UIControl Control)
        {
            if (CurrentState == PanelStates.Closed || CurrentState == PanelStates.Closing)
            {
                UpdateLedger();
                CurrentState = PanelStates.Opening;
                ControlButton Btn = Control as ControlButton;

                Btn.SetImageFiles("Data\\Images\\HUD\\Economy\\arrow_up_normal.png", "Data\\Images\\HUD\\Economy\\arrow_up_mouseover.png", "Data\\Images\\HUD\\Economy\\arrow_up_pressed.png");
            }
            else
            {
                ClosePanel();
            }
        }

        void ClosePanel()
        {
            CurrentState = PanelStates.Closing;
            ControlButton Btn = GameForm.GetControlByName<ControlButton>("LedgerToggle");

            Btn.SetImageFiles("Data\\Images\\HUD\\Economy\\arrow_down_normal.png", "Data\\Images\\HUD\\Economy\\arrow_down_mouseover.png", "Data\\Images\\HUD\\Economy\\arrow_down_pressed.png");
        }

        private void UpdateDate()
        {
            // Force English month names so OS culture (e.g. Polish) does not localise the HUD date.
            string DateText = GameSystem.CurrentGame.TurnController.CurrentDate.Value.ToString("d MMM yyyy", CultureInfo.GetCultureInfo("en-GB"));
            GameForm.GetControlByName<ControlText>("TurnText").SetTextData(GameText.CreateFromLiteral(DateText));
            GameForm.GetControlByName<ControlText>("TurnText").SetTooltip(GameText.CreateLocalised("TT_DATE", GameSystem.CurrentGame.TurnController.TurnNumber + 1));
        }

        private void UpdateGold()
        {
            GameSystem.FireEvent("PlayerGoldChanged");
            GameForm.GetControlByName<ControlText>("GoldText").SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", GameSystem.CurrentGame.PlayerRealm.Gold));
        }

        public void GoldMouseEnter(UIControl Control)
        {
            if (GameSystem == null || GameSystem.CurrentGame == null || GameSystem.CurrentGame.EconomyController == null) return;

            //Treasury: {0}\n\nIncome:\n  Provinces: {1}\n  Spells: {2}\n  Landmarks: {3}\n  
            //Trade: {4}\n\nExpenses:\n  Units: {5}\n  Prisoners: {6}\n  Magic: {7}\n\nTotal: {8}

            int ProvinceIncome = GameSystem.CurrentGame.EconomyController.GetRealmProvinceIncome(GameSystem.CurrentGame.PlayerRealm);
            int SpellIncome = GameSystem.CurrentGame.EconomyController.GetRealmSpellIncome(GameSystem.CurrentGame.PlayerRealm);
            int LandmarkIncome = GameSystem.CurrentGame.EconomyController.GetRealmLandmarkIncome(GameSystem.CurrentGame.PlayerRealm);
            int TradeIncome = GameSystem.CurrentGame.EconomyController.GetRealmTradeIncome(GameSystem.CurrentGame.PlayerRealm);
            int TotalIncome = GameSystem.CurrentGame.EconomyController.GetRealmTotalIncome(GameSystem.CurrentGame.PlayerRealm);

            int UnitExpenses = GameSystem.CurrentGame.EconomyController.GetUnitUpkeep(GameSystem.CurrentGame.PlayerRealm);
            int PrisonerExpenses = GameSystem.CurrentGame.EconomyController.GetPrisonerUpkeep(GameSystem.CurrentGame.PlayerRealm);
            int MagicExpenses = GameSystem.CurrentGame.EconomyController.GetMagicExpenses(GameSystem.CurrentGame.PlayerRealm);
            int TotalExpenses = GameSystem.CurrentGame.EconomyController.GetTotalExpenses(GameSystem.CurrentGame.PlayerRealm);

            int Surplus = TotalIncome - TotalExpenses;

            GameText Text = GameText.CreateLocalised("CURRENTTREASURYTOOLTIP", GameSystem.CurrentGame.PlayerRealm.Gold, ProvinceIncome, SpellIncome, LandmarkIncome, TradeIncome, UnitExpenses, PrisonerExpenses, MagicExpenses, Surplus);
            Control.SetTooltip(Text);
        }

        public override void Update(float ElapsedTime)
        {
            base.Update(ElapsedTime);
            float Speed = 2000;

            if (CurrentState == PanelStates.Closing)
            {
                float PosY = MainControl.GetOriginalY();
                PosY -= (Speed * ElapsedTime);
                if (PosY <= -592)
                {
                    PosY = -592;
                    CurrentState = PanelStates.Closed;
                }

                MainControl.SetPositionY(PosY, UIUnits.Pixel);
            }

            if (CurrentState == PanelStates.Opening)
            {
                float PosY = MainControl.GetOriginalY();
                PosY += (Speed * ElapsedTime);
                if (PosY >= -15)
                {
                    PosY = -15;
                    CurrentState = PanelStates.Open;
                }

                MainControl.SetPositionY(PosY, UIUnits.Pixel);
            }
        }

        public override void Dispose()
        {
            GameSystem.UnregisterEvent(HandleCloseRequest, "CloseFinancePanel");
        }
    }
}
