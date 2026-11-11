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
using SovereigntyTK.Game.Battle;
using OpenTK.Input;
using OpenTK;

namespace SovereigntyTK.UI
{
    public class MovingCardData
    {
        public ControlHeroCard Card;
        public RectangleF TargetBounds;
    }

    public class FloatingElement
    {
        public UIControl Control;

        //position on map, will be converted
        public float PositionX;
        public float PositionY;
        public bool Inverted;
    }

    public class TacticalBattleForm: ScriptBase
    {
        TacticalBattleController Battle;

        WorkingUnit MouseoverUnit;
        WorkingUnit SelectedUnit;
        UnitActionData SelectedUnitActions;
        List<FloatingElement> FloatingUIElements;

        ControlUnitCard MouseoverCard;
        ControlUnitCard SelectedCard;

        List<ControlHeroCard> CardImages;
        List<MovingCardData> MovingCards;
        ControlHeroCard ActiveHeroCardImage;
        ControlHeroCard SelectedHeroCard;

        ControlList<GameText> LogControl;

        ControlHeroCard AIHeroCard;

        int LastSelectedIndex;

        List<CardTargetData> CardTargets;
        int CardTargetIndex;

        WorkingUnit CurrentActionUnit;

        public void FormLoaded(UIControl Control)
        {
            FloatingUIElements = new List<FloatingElement>();

            GameSystem.RegisterEvents(GameSystem_OnScriptEvent, "BattleDeployFinished", "BattleActionCompleted", "FullBattleStarted", "TacticalBattleEnded", "BattleTurnChanged", "UnitsUpdated", "BattleCardsChanged");
            GameSystem.RegisterEvent(HandleAICard, "AIBattleCard");
            GameSystem.RegisterEvent(HandleKeyPress, "KeyDown");
            GameSystem.RegisterEvent(HandleLogEvent, "BattleLogEvent");
            GameSystem.Camera.ViewMatrixChanged += Camera_ViewMatrixChanged;

            MouseoverCard = new ControlUnitCard(GameSystem);
            MouseoverCard.SetSize(180, 300, UIUnits.Pixel);
            MouseoverCard.SetPosition(0, 300, UIUnits.Pixel);
            MouseoverCard.SetAnchor(AnchorPoints.BottomLeft);
            MouseoverCard.Visible = false;
            GameForm.AddChild(MouseoverCard);

            SelectedCard = new ControlUnitCard(GameSystem);
            SelectedCard.SetSize(180, 300, UIUnits.Pixel);
            SelectedCard.SetPosition(0, 0, UIUnits.Pixel);
            SelectedCard.SetAnchor(AnchorPoints.BottomLeft);
            SelectedCard.Visible = false;
            GameForm.AddChild(SelectedCard);

            AIHeroCard = new ControlHeroCard(GameSystem);
            AIHeroCard.SetSize(25, 40, UIUnits.Internal);
            AIHeroCard.SetAnchor(AnchorPoints.Middle);
            GameForm.GetControlByName<ControlImage>("PlayedCardBG").AddChild(AIHeroCard);

            GameSystem.OnViewportChanged += GameSystem_OnViewportChanged;
            MovingCards = new List<MovingCardData>();

            LogControl = GameForm.GetControlByName<ControlList<GameText>>("LogList");
            LogControl.HighlightEnabled = false;
            LogControl.SelectionEnabled = false;

            LogControl.ManualUpdate = true;
            LogControl.OnListItemUpdated += LogControl_OnListItemUpdated;
        }

        void LogControl_OnListItemUpdated(ListItem<GameText> Item)
        {
            ControlText Text = new ControlText(GameSystem);
            Text.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
            Text.FontName = "Trebuchet MS";
            Text.SetFontSize(10, UIUnits.Pixel);
            Text.Bold = true;
            Text.Colour = Color.FromArgb(255, 238, 161);
            Text.SetTextData(Item.Data);
            Text.TextAnchor = AnchorPoints.TopLeft;
            Item.Container.AddChild(Text);
        }

        private void HandleLogEvent(string EventName, params object[] Args)
        {
            GameText Text = Args[0] as GameText;

            if (LogControl.Items.Count == 50) LogControl.RemoveItem(LogControl.Items[0].Data);
            LogControl.AddItem(Text);
            LogControl.ScrollToBottom();
        }

        private void HandleAICard(string EventName, params object[] Args)
        {
            CardEffect Card = Args[0] as CardEffect;

            GameForm.GetControlByName<ControlImage>("PlayedCardBG").Visible = true;
            AIHeroCard.SetCard(Card);

            GameText Text = GameText.CreateLocalised("CARDUSEDMSG");
            Text.AddChildText(GameText.CreateLocalised(Battle.ActiveStack.Owner.DisplayName));
            Text.AddChildText(GameText.CreateLocalised(Card.DisplayName));

            GameForm.GetControlByName<ControlText>("AICardTitle").SetTextData(Text);
        }

        void GameSystem_OnViewportChanged()
        {

        }

        void Camera_ViewMatrixChanged()
        {
            if (FloatingUIElements.Count == 0) return;

            foreach (FloatingElement Element in FloatingUIElements.ToList())
            {
                if (Element.Control.Disposed)
                {
                    FloatingUIElements.Remove(Element);
                    continue;
                }

                //convert position from map position to pixel co-ords
                Point ScreenPoint = GameSystem.Map.GetScreenCoord(new Vector3(Element.PositionX, 0, Element.PositionY));

                //update control position
                if (!Element.Inverted) ScreenPoint.Y -= (int)Element.Control.Sprite.Bounds.Height;
                Element.Control.SetPosition(ScreenPoint.X - (Element.Control.Sprite.Bounds.Width / 2), ScreenPoint.Y, UIUnits.PixelScaled);
            }
        }

        void GameSystem_OnScriptEvent(string EventName, params object[] Args)
        {
            if (EventName == "BattleDeployFinished")
            {
                SetBattlePhaseText();
            }

            if (EventName == "BattleActionCompleted" && CurrentActionUnit != null)
            {
                if (CurrentActionUnit.Disabled == false && CurrentActionUnit.BattleData != null)
                {
                    if (CurrentActionUnit.BattleData.CanMove || CurrentActionUnit.BattleData.CanFight)
                    {
                        SelectUnit(CurrentActionUnit);
                    }
                }

                CurrentActionUnit = null;
            }

            if (EventName == "FullBattleStarted")
            {
                Battle = Args[0] as TacticalBattleController;
                SelectedUnitActions = null;

                ControlText BattleTitle = GameForm.GetControlByName<ControlText>("TitleText");

                GameText Text = GameText.CreateLocalised("BATTLETITLE");
                Text.AddChildText(GameText.CreateLocalised(Battle.Node.GetRegion().DisplayName));
                BattleTitle.SetTextData(Text);

                ControlText TitleText = GameForm.GetControlByName<ControlText>("InfoTitle");
                ControlText DescText = GameForm.GetControlByName<ControlText>("InfoText");

                TitleText.SetTextData(GameText.CreateLocalised("TUT_BATTLEDEPLOY_TITLE"));
                DescText.SetTextData(GameText.CreateLocalised("DEPLOYDESC"));

                Battle.OnTileClicked += Battle_OnTileClicked;
                Battle.OnTileMouseover += Battle_OnTileMouseover;
                Battle.OnUnitClicked += Battle_OnUnitClicked;
                Battle.OnUnitMouseover += Battle_OnUnitMouseover;
                Battle.OnMapClicked += Battle_OnMapClicked;

                CreateBattleCards();
                UpdateUnitList();
                LogControl.ClearItems();
            }

            if (EventName == "TacticalBattleEnded")
            {
                ControlImage UnitListBG = GameForm.GetControlByName<ControlImage>("UnitListBG");
                foreach (UIControl Control in UnitListBG.GetChildren().ToList())
                {
                    UnitListBG.RemoveChild(Control);
                    Control.Dispose();
                }

                Battle.OnTileClicked -= Battle_OnTileClicked;
                Battle.OnTileMouseover -= Battle_OnTileMouseover;
                Battle.OnUnitClicked -= Battle_OnUnitClicked;
                Battle.OnUnitMouseover -= Battle_OnUnitMouseover;
                Battle.OnMapClicked -= Battle_OnMapClicked;


            }

            if (EventName == "BattleTurnChanged")
            {
                UpdateTimerText();
            }

            if (EventName == "UnitsUpdated")
            {
                UpdateUnitList();
            }

            if (EventName == "BattleCardsChanged")
            {
                CreateBattleCards();
            }
        }

        private void UpdateUnitList()
        {
            WorkingStack PlayerStack = Battle.Attacker;
            if (Battle.Defender.Owner == GameSystem.CurrentGame.PlayerRealm) PlayerStack = Battle.Defender;

            ControlImage UnitListBG = GameForm.GetControlByName<ControlImage>("UnitListBG");
            int UnitX = 0;
            int UnitY = 0;

            foreach (UIControl Control in UnitListBG.GetChildren().ToList())
            {
                UnitListBG.RemoveChild(Control);
                Control.Dispose();
            }

            foreach (WorkingUnit Unit in PlayerStack.Units)
            {
                if (Unit.Disabled) continue;
                if (Unit.BattleData == null) continue;

                ControlImage UnitImage = new ControlImage(GameSystem);
                UnitListBG.AddChild(UnitImage);
                UnitImage.SetBounds(UnitX, UnitY, 55, 55, UIUnits.Pixel);
                UnitImage.SetImage(Unit.BattleData.GetTexture());
                UnitImage.OnMouseEnter += UnitImage_OnMouseEnter;
                UnitImage.OnMouseLeave += UnitImage_OnMouseLeave;
                UnitImage.OnClick += UnitImage_OnClick;
                UnitImage.CustomData = Unit;

                UnitX += 55;
                if (UnitX >= 715)
                {
                    UnitX = 0;
                    UnitY += 60;
                }
            }
        }

        void UnitImage_OnClick(UIControl Control)
        {
            Battle_OnUnitClicked(Control.CustomData as WorkingUnit, MouseButton.Left);
        }

        void UnitImage_OnMouseLeave(UIControl Control)
        {
            MouseoverUnit = null;
            UpdateUnitCards();
        }

        void UnitImage_OnMouseEnter(UIControl Control)
        {
            Battle_OnUnitMouseover(Control.CustomData as WorkingUnit);
        }

        public void SettingsClicked(UIControl Control)
        {
            if (Battle.TurnCounter.AttackerTurn != (Battle.Attacker.Owner == GameSystem.CurrentGame.PlayerRealm)) return;
            GameSystem.FireEvent("MenuRequested");
        }

        private void CreateBattleCards()
        {
            if (CardImages != null)
            {
                foreach (ControlHeroCard Card in CardImages)
                {
                    Card.ParentControl.RemoveChild(Card);
                    Card.Dispose();
                }

                CardImages.Clear();
            }

            CardImages = new List<ControlHeroCard>();
            int CardPos = 342;

            if (GameSystem.CurrentGame.PlayerRealm.BattleData.ActiveCards.Count == 0) return;

            int Spacing = 300 / GameSystem.CurrentGame.PlayerRealm.BattleData.ActiveCards.Count;
            Spacing = Math.Min(Spacing, 150);

            foreach (CardEffect Card in GameSystem.CurrentGame.PlayerRealm.BattleData.ActiveCards)
            {
                ControlHeroCard CardControl = new ControlHeroCard(GameSystem);
                GameForm.AddChild(CardControl);

                CardControl.SetAnchor(AnchorPoints.BottomMiddle);
                CardControl.SetBounds(CardPos, -100, 150, 255, UIUnits.Pixel);
                CardControl.OnMouseEnter += Image_OnMouseEnter;
                CardControl.OnMouseLeave += Image_OnMouseLeave;
                CardControl.OnClick += CardControl_OnClick;
                CardControl.OnPlayClicked += CardControl_OnPlayClicked;
                CardControl.OnCancelClicked += CardControl_OnCancelClicked;
                CardControl.MouseInputType = MouseInputTypes.Forced;

                CardControl.SetCard(Card);
                CardImages.Add(CardControl);

                CardPos -= Spacing;
            }

            Image_OnMouseLeave(null);
            GameForm.GetControlByName<ControlImage>("UnitListBG").BringToFront();
            GameForm.GetControlByName<ControlImage>("PlayedCardBG").BringToFront();
        }

        void CardControl_OnCancelClicked()
        {
            RectangleF OldBounds = new RectangleF(SelectedHeroCard.GetOriginalX(), SelectedHeroCard.GetOriginalY(), SelectedHeroCard.getOriginalWidth(), SelectedHeroCard.GetOriginalHeight());
            CardImages.Add(SelectedHeroCard);
            SetCardPositions(true);
            Image_OnMouseLeave(null);

            if (SelectedHeroCard != null)
            {
                MovingCardData Data = new MovingCardData();
                Data.Card = SelectedHeroCard;
                Data.TargetBounds = new RectangleF(GetCardX(CardImages.Count+1, CardImages.Count - 1), -100, 150, 255);
                Data.Card.SetTextState(false);
                Data.Card.SetButtonState(false);
                Data.Card.SetBounds(OldBounds.X, OldBounds.Y, OldBounds.Width, OldBounds.Height, UIUnits.Pixel);
                MovingCards.Add(Data);
            }

            SelectedHeroCard = null;

            GameSystem.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\organic_texture_whoosh_30.wav");
        }

        void CardControl_OnPlayClicked()
        {
            if (SelectedHeroCard == null) return;

            CardEffect Effect = SelectedHeroCard.CurrentCard;

            if (Effect.GetTargetData() == null)
            {
                GameSystem.CurrentGame.PlayerRealm.BattleData.UsedCards.Add(Effect);
                GameSystem.CurrentGame.PlayerRealm.BattleData.ActiveCards.Remove(Effect);
                GameSystem.CurrentGame.PlayerRealm.BattleData.CardPlayed = true;

                GameText Text = GameText.CreateLocalised("FORMAT_BATTLELOG_CARD");
                Text.AddChildText(GameText.CreateLocalised(GameSystem.CurrentGame.PlayerRealm.DisplayName));
                Text.AddChildText(GameText.CreateLocalised(Effect.DisplayName));
                GameSystem.FireEvent("BattleLogEvent", Text);

                Effect.CastEffect(null);
                MovingCardData Data = new MovingCardData();
                Data.Card = SelectedHeroCard;
                Data.Card.BringToFront();
                Data.TargetBounds = new RectangleF(-200, -400, 50, 80);
                SelectedHeroCard = null;
                MovingCards.Add(Data);
            }
            else
            {
                CardTargets = Effect.GetTargetData();
                CardTargetIndex = 0;

                SelectUnit(null);
                SetCardTarget();

                MovingCardData Data = new MovingCardData();
                Data.Card = SelectedHeroCard;
                Data.Card.BringToFront();
                Data.Card.SetButtonState(false);
                Data.TargetBounds = new RectangleF(0, GameSystem.GetViewport().Height - 450, 200, 340);
                MovingCards.Add(Data);
            }
        }

        private void SetCardTarget()
        {
            //display text for current card target
            ControlText TitleText = GameForm.GetControlByName<ControlText>("InfoTitle");
            ControlText DescText = GameForm.GetControlByName<ControlText>("InfoText");

            TitleText.SetTextData(GameText.CreateLocalised("CARDCAST_TITLE"));
            DescText.SetTextData(CardTargets[CardTargetIndex].TargetDescription);

            //determine the valid tiles for target
            List<Point> ValidTiles = new List<Point>();
            foreach (Point p in Battle.Map.AllTiles)
            {
                if (SelectedHeroCard.CurrentCard.TileTargetValid(p, CardTargetIndex))
                {
                    ValidTiles.Add(p);
                }
            }

            //put map into card target mode
            Battle.Map.SetCastingMode(ValidTiles);
        }

        void CardControl_OnClick(UIControl Control)
        {
            if (Battle.ActiveStack.Owner.AIPlayer != null) return;
            if (Battle.Map.CurrentMode == Map.BattleMapModes.Deploy) return;
            if (Control == SelectedHeroCard) return;

            RectangleF OldBounds = RectangleF.Empty;

            CardImages.Remove(Control as ControlHeroCard);
            if (SelectedHeroCard != null)
            {
                OldBounds = new RectangleF(SelectedHeroCard.GetOriginalX(), SelectedHeroCard.GetOriginalY(), SelectedHeroCard.getOriginalWidth(), SelectedHeroCard.GetOriginalHeight());
                CardImages.Add(SelectedHeroCard);
            }
            SetCardPositions(true);
            Image_OnMouseLeave(null);

            if (SelectedHeroCard != null)
            {
                MovingCardData Data = new MovingCardData();
                Data.Card = SelectedHeroCard;
                Data.TargetBounds = new RectangleF(GetCardX(CardImages.Count+1, CardImages.Count-1), -100, 150, 255);
                Data.Card.SetTextState(false);
                Data.Card.SetButtonState(false);
                Data.Card.SetBounds(OldBounds.X, OldBounds.Y, OldBounds.Width, OldBounds.Height, UIUnits.Pixel);
                MovingCards.Add(Data);
            }

            SelectedHeroCard = Control as ControlHeroCard;
            SelectedHeroCard.SetTextState(true);
            SelectedHeroCard.SetButtonState(true);

            GameSystem.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\organic_texture_whoosh_30.wav");
        }

        private int GetCardX(int CardCount, int Index)
        {
            int CardPos = 342;

            int Spacing = 300 / CardCount;
            Spacing = Math.Min(Spacing, 150);

            for (int i = 0; i < Index; i++)
            {
                CardPos -= Spacing;
            }

            return CardPos;
        }

        private void SetCardPositions(bool ExtraCard)
        {
            int CardPos = 342;

            int Count = CardImages.Count;
            if (ExtraCard) Count++;

            int Spacing = 300 / Count;
            Spacing = Math.Min(Spacing, 150);

            foreach (ControlHeroCard Card in CardImages)
            {
                Card.SetBounds(CardPos, -100, 150, 255, UIUnits.Pixel);
                CardPos -= Spacing;
            }
        }

        public override void Update(float ElapsedTime)
        {
            base.Update(ElapsedTime);

            if (CardImages == null) return;

            foreach (ControlHeroCard Image in CardImages)
            {
                if (Image == ActiveHeroCardImage)
                {
                    if (MovingCards.Count(x => x.Card == Image) > 0) continue;

                    if (Image.GetOriginalY() < 120)
                    {
                        float NewPos = Image.GetOriginalY() + (ElapsedTime * 1000);
                        if (NewPos > 120) NewPos = 120;
                        Image.SetPositionY(NewPos, UIUnits.Pixel);
                    }
                }
                else
                {
                    if (MovingCards.Count(x => x.Card == Image) > 0) continue;

                    if (Image.GetOriginalY() > -100)
                    {
                        float NewPos = Image.GetOriginalY() - (ElapsedTime * 1000);
                        if (NewPos < -100) NewPos = -100;
                        Image.SetPositionY(NewPos, UIUnits.Pixel);
                    }
                }
            }

            if (MovingCards.Count > 0)
            {
                foreach (MovingCardData Data in MovingCards.ToList())
                {
                    Vector2 Current = new Vector2(Data.Card.GetOriginalX(), Data.Card.GetOriginalY());
                    Vector2 Target = new Vector2(Data.TargetBounds.X, Data.TargetBounds.Y);

                    if (Data.Card.getOriginalWidth() > Data.TargetBounds.Width)
                    {
                        float NewWidth = Data.Card.getOriginalWidth() - (ElapsedTime * 600);
                        if (NewWidth < Data.TargetBounds.Width) NewWidth = Data.TargetBounds.Width;
                        Data.Card.SetWidth(NewWidth, UIUnits.Pixel);
                    }

                    if (Data.Card.GetOriginalHeight() > Data.TargetBounds.Height)
                    {
                        float NewHeight = Data.Card.GetOriginalHeight() - (ElapsedTime * 1000);
                        if (NewHeight < Data.TargetBounds.Height) NewHeight = Data.TargetBounds.Height;
                        Data.Card.SetHeight(NewHeight, UIUnits.Pixel);
                    }

                    Vector2 Direction = Target - Current;

                    if (Direction.Length < 2)
                    {
                        MovingCards.Remove(Data);

                        if (Data.TargetBounds.Y < -200)
                        {
                            Data.Card.ParentControl.RemoveChild(Data.Card);
                            Data.Card.Dispose();
                        }
                    }
                    else
                    {
                        float MaxDist = Direction.Length;
                        Direction.Normalize();
                        Direction *= Math.Min(ElapsedTime * 3000, MaxDist);

                        Current += Direction;

                        Data.Card.SetPosition(Current.X, Current.Y, UIUnits.Pixel);
                    }
                }
            }

            if (SelectedHeroCard != null && Battle.Map.CurrentMode != Map.BattleMapModes.Casting)
            {
                if (SelectedHeroCard.getOriginalWidth() < 265)
                {
                    float NewWidth = SelectedHeroCard.getOriginalWidth() + (ElapsedTime * 500);
                    if (NewWidth > 265) NewWidth = 265;
                    SelectedHeroCard.SetWidth(NewWidth, UIUnits.Pixel);
                }

                if (SelectedHeroCard.GetOriginalHeight() < 433)
                {
                    float NewHeight = SelectedHeroCard.GetOriginalHeight() + (ElapsedTime * 1000);
                    if (NewHeight > 433) NewHeight = 433;
                    SelectedHeroCard.SetHeight(NewHeight, UIUnits.Pixel);
                }

                Rectangle Viewport = GameSystem.GetViewport();
                float CX = Viewport.Width / 2;
                float CY = Viewport.Height / 2;

                float TargetX = 0;
                float TargetY = CY - (SelectedHeroCard.GetOriginalHeight() / 2);

                Vector2 CurrentPos = new Vector2(SelectedHeroCard.GetOriginalX(), SelectedHeroCard.GetOriginalY());
                Vector2 TargetPos = new Vector2(TargetX, TargetY);

                Vector2 Dir = TargetPos - CurrentPos;

                if (Dir.Length < 1) return;

                float MaxDist = Dir.Length;
                Dir.Normalize();
                Dir *= Math.Min(ElapsedTime * 3000, MaxDist);

                CurrentPos += Dir;

                SelectedHeroCard.SetPosition(CurrentPos.X, CurrentPos.Y, UIUnits.Pixel);
            }
        }

        void Image_OnMouseLeave(UIControl Control)
        {
            if (Control == SelectedHeroCard) return;
            if (MovingCards.Count(x => x.Card == Control) > 0) return;
            ActiveHeroCardImage = null;

            for (int i = CardImages.Count - 1; i >= 0; i--)
            {
                CardImages[i].BringToFront();
            }

            GameForm.GetControlByName<ControlImage>("UnitListBG").BringToFront();
            GameForm.GetControlByName<ControlImage>("PlayedCardBG").BringToFront();

        }

        void Image_OnMouseEnter(UIControl Control)
        {
            if (Control == SelectedHeroCard) return;
            if (MovingCards.Count(x => x.Card == Control) > 0) return;
            ActiveHeroCardImage = Control as ControlHeroCard;

            GameSystem.FireEvent("HeroCardMouseover");

            Control.BringToFront();
            GameForm.GetControlByName<ControlImage>("UnitListBG").BringToFront();
            GameForm.GetControlByName<ControlImage>("PlayedCardBG").BringToFront();
        }

        private void SelectUnit(WorkingUnit Unit)
        {
            try
            {
                if (SelectedUnit != null && SelectedUnit.BattleData != null)
                {
                    SelectedUnit.BattleData.Selected = false;
                    SelectedUnit.BattleData.UpdateImage();
                }

                SelectedUnit = Unit;

                if (SelectedUnit != null && SelectedUnit.BattleData != null)
                {
                    SelectedUnit.BattleData.Selected = true;
                    SelectedUnit.BattleData.UpdateImage();
                }

                UpdateUnitCards();

                if (Battle.DeployPhase) return;

                Battle.Map.ClearTargetSprites();

                if (SelectedUnit == null)
                {
                    Battle.Map.SetMode(Map.BattleMapModes.Default);
                    Battle.Map.ClearArrow();
                    ClearPreviewControls();
                }
                else
                {
                    if (GameSystem.Settings.GetBooleanSetting("FollowCamera"))
                    {
                        GameSystem.Camera.BeginAutoMove(new OpenTK.Vector3(Unit.BattleData.Sprite.GetPosition().X, GameSystem.Camera.CamPos.Y, Unit.BattleData.Sprite.GetPosition().Y), 0.25f);
                    }

                    SelectedUnitActions = Battle.GetUnitActions(SelectedUnit);
                    Battle.Map.SetMoveOverlay(SelectedUnitActions.MovementCosts, SelectedUnit);
                    Battle.Map.SetRangeOverlay(SelectedUnitActions.TilesinRange, SelectedUnitActions.MovementCosts);

                    foreach (KeyValuePair<WorkingUnit, List<CombatAction>> Pair in SelectedUnitActions.Targets)
                    {
                        Battle.Map.GetTile(Pair.Key.BattleData.BattleLocation).SetTargetSprite(Pair.Value[0]);
                    }

                    GameSystem.FireEvent("TacticalUnitSelected", Unit);

                    if (SelectedUnit.OwnerRealmID == GameSystem.CurrentGame.PlayerRealm.ID)
                    {
                        LastSelectedIndex = Battle.GetPlayerstack().Units.IndexOf(SelectedUnit);
                    }
                }
            }
            catch (NullReferenceException)
            {
                string Message = "Error selecting unit in battle.\r\n";
                Message += "Battle location: " + Battle.Node.GetRegion().Name + "\r\n";
                Message += "Attacker Province: " + Battle.AttackerNode.GetRegion().Name + "\r\n";
                Message += "Attacker: " + Battle.AttackerRealm.Name + "\r\n";
                Message += "Defender: " + Battle.DefenderRealm.Name + "\r\n";
                Message += "Attacker Units: ";
                foreach (WorkingUnit TestUnit in Battle.Attacker.Units)
                {
                    Message += TestUnit.BaseName + ", ";
                }
                Message += "(Total: " + Battle.Attacker.Units.Count + ")\r\n";
                Message += "Defender Units: ";
                foreach (WorkingUnit TestUnit in Battle.Defender.Units)
                {
                    Message += TestUnit.BaseName + ", ";
                }
                Message += "(Total: " + Battle.Defender.Units.Count + ")\r\n";

                if (SelectedUnit == null)
                {
                    Message += "Affected unit: null\r\n";
                }
                else
                {
                    Message += "Affected unit: " + Unit.BaseName + " (" + Unit.Class.ToString() + ")\r\n";
                }
                
                throw new Exception(Message);
            }
        }

        void Battle_OnMapClicked(MouseButton Button)
        {
            if (GameForm.Disposed) return;

            //click outside of the battle area
            //deselect the active unit no matter which button it was
            if (Battle.Map.CurrentMode == Map.BattleMapModes.Casting) { }
            else SelectUnit(null);
        }

        public void InfoButtonClick(UIControl Control)
        {
            ControlImage BG = GameForm.GetControlByName<ControlImage>("InfoImage");
            BG.Visible = !BG.Visible;

            if (BG.Visible) GameSystem.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\scroll34.wav");
            else GameSystem.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\scrollclose.wav");
        }

        void Battle_OnUnitMouseover(WorkingUnit Unit)
        {
            if (GameForm.Disposed) return;

            if (Unit.OwnerRealm != GameSystem.CurrentGame.PlayerRealm && Battle.DeployPhase) return;

            MouseoverUnit = Unit;
            UpdateUnitCards();

            if (SelectedUnit != null && SelectedUnitActions != null && SelectedUnit.OwnerRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                if (SelectedUnitActions.Targets.ContainsKey(Unit))
                {
                    CombatAction ActionType = SelectedUnitActions.Targets[Unit][0];
                    bool UsePathing = ActionType == CombatAction.ChargeAttack || ActionType == CombatAction.Moveattack;

                    Battle.Map.ShowActionArrow(SelectedUnit.BattleData.BattleLocation, Unit.BattleData.BattleLocation, Unit.OwnerRealmID == SelectedUnit.OwnerRealmID, UsePathing, SelectedUnitActions);

                    CombatPreviewResult Preview = new CombatPreviewResult(GameSystem.CurrentGame, Battle, SelectedUnit, Unit, ActionType, SelectedUnitActions);
                    ShowPreview(Preview);
                }
                else
                {
                    Battle.Map.ClearArrow();
                    ClearPreviewControls();
                }
            }
        }

        private void ClearPreviewControls()
        {
            UIControl Control = GameForm.GetControlByName<UIControl>("CombatPreview");

            while (Control != null)
            {
                GameForm.RemoveChild(Control);
                Control.Dispose();

                Control = GameForm.GetControlByName<UIControl>("CombatPreview");
            }
        }

        private void ShowPreview(CombatPreviewResult Preview)
        {
            ClearPreviewControls();

            //if (Preview.AttackerResult.HasDamage())
            {
                bool Invert = Preview.Attacker.BattleData.BattleY >= Preview.Defender.BattleData.BattleY;
                ShowPreviewResult(Preview.AttackerResult, Invert);
            }

            //if (Preview.DefenderResult.HasDamage())
            {
                bool Invert = Preview.Attacker.BattleData.BattleY < Preview.Defender.BattleData.BattleY;
                ShowPreviewResult(Preview.DefenderResult, Invert);
            }
        }

        public void LogButtonClick(UIControl Control)
        {
            ControlImage BG = GameForm.GetControlByName<ControlImage>("LogImage");
            BG.Visible = !BG.Visible;

            if (BG.Visible) GameSystem.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\scroll34.wav");
            else GameSystem.Utilities.SoundManager.PlaySound("Data\\Sound\\Effects\\scrollclose.wav");

            if (BG.Visible) LogControl.ScrollToBottom();
        }

        private void ShowPreviewResult(PreviewResultData Result, bool Invert)
        {
            List<GameText> Text = new List<GameText>();
            foreach (GameText TextItem in Result.StatusEffects)
            {
                Text.Add(TextItem);
                Text.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
            }

            if (Result.MaxSupportCasualties > 0)
            {
                Text.Add(GameText.CreateLocalised("COMBATPREVIEW_SUPPORT", Result.MinSupportCasualties, Result.MaxSupportCasualties));
                Text.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
            }

            if (Result.Heal > 0)
            {
                Text.Add(GameText.CreateLocalised("COMBATPREVIEWHEAL", Result.Heal));
                Text.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
            }
            else if (Result.MaxCasualties > 0)
            {
                Text.Add(GameText.CreateLocalised("COMBATPREVIEW", Result.MinCasualties, Result.MaxCasualties));
                Text.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
            }
            else
            {
                Text.Add(GameText.CreateLocalised("COMBATNONE"));
                Text.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
            }

            ControlText OutcomeText = new ControlText(GameSystem);
            OutcomeText.SetWidth(126, UIUnits.Pixel);
            OutcomeText.SetPositionX(12, UIUnits.Pixel);
            OutcomeText.FontName = "Trebuchet MS";
            OutcomeText.SetFontSize(14, UIUnits.Pixel);
            OutcomeText.TextAnchor = AnchorPoints.Middle;
            OutcomeText.Colour = Color.FromArgb(255, 238, 161);
            OutcomeText.SetTextData(Text);
            OutcomeText.SetHeight(OutcomeText.GetTextHeight(), UIUnits.PixelScaled);

            ControlContainer MainControl = new ControlContainer(GameSystem);
            MainControl.ControlName = "CombatPreview";
            GameForm.AddChild(MainControl);
            MainControl.SetWidth(150, UIUnits.Pixel);
            MainControl.MouseInputType = MouseInputTypes.None;

            ControlImage MiddleImage = new ControlImage(GameSystem);
            MiddleImage.SetWidth(130, UIUnits.Pixel);
            MiddleImage.SetHeight(OutcomeText.Sprite.Bounds.Height, UIUnits.PixelScaled);
            MiddleImage.SetPositionX(10, UIUnits.Pixel);

            ControlImage BarImage = new ControlImage(GameSystem);
            BarImage.SetWidth(150, UIUnits.Pixel);
            BarImage.SetHeight(30, UIUnits.Pixel);

            ControlImage BottomImage = new ControlImage(GameSystem);
            BottomImage.SetWidth(130, UIUnits.Pixel);
            BottomImage.SetHeight(20, UIUnits.Pixel);
            BottomImage.SetPositionX(10, UIUnits.Pixel);

            GameText Title = GameText.CreateLocalised("COMBATMELEE");
            string TitleImageName = "Icon_Melee";

            if (Result.ActionType == CombatAction.RangedAttack)
            {
                Title = GameText.CreateLocalised("COMBATRANGED");
                TitleImageName = "Icon_Ranged";
            }

            if (Result.ActionType == CombatAction.Heal)
            {
                Title = GameText.CreateLocalised("COMBATHEAL");
                TitleImageName = "Icon_Heal";
            }

            if (Invert)
            {
                float Ypos = 0;

                BottomImage.SetPositionY(Ypos, UIUnits.PixelScaled);
                MainControl.AddChild(BottomImage);
                Ypos += BottomImage.Sprite.Bounds.Height;

                MiddleImage.SetPositionY(Ypos, UIUnits.PixelScaled);
                OutcomeText.SetPositionY(Ypos, UIUnits.PixelScaled);
                MainControl.AddChild(MiddleImage);
                Ypos += MiddleImage.Sprite.Bounds.Height;
                Ypos -= 2;

                BarImage.SetPositionY(Ypos, UIUnits.PixelScaled);
                MainControl.AddChild(BarImage);

                MainControl.SetHeight(Ypos + BarImage.Sprite.Bounds.Height, UIUnits.PixelScaled);
                MainControl.AddChild(OutcomeText);

                ControlText TitleText = new ControlText(GameSystem);
                MainControl.AddChild(TitleText);
                TitleText.SetWidth(109, UIUnits.Pixel);
                TitleText.SetHeight(26, UIUnits.Pixel);
                TitleText.SetAnchor(AnchorPoints.BottomLeft);
                TitleText.SetPosition(31, 3, UIUnits.Pixel);
                TitleText.FontName = "Trebuchet MS";
                TitleText.SetFontSize(14, UIUnits.Pixel);
                TitleText.TextAnchor = AnchorPoints.Middle;
                TitleText.Colour = Color.FromArgb(255, 238, 161);
                TitleText.SetTextData(Title);

                ControlImage TitleImage = new ControlImage(GameSystem);
                MainControl.AddChild(TitleImage);
                TitleImage.SetWidth(25, UIUnits.Pixel);
                TitleImage.SetHeight(25, UIUnits.Pixel);
                TitleImage.SetAnchor(AnchorPoints.BottomLeft);
                TitleImage.SetPosition(3, 3, UIUnits.Pixel);
                TitleImage.SetImageFile("Data\\Images\\HUD\\Battle\\floating\\" + TitleImageName + ".png");
            }
            else
            {
                float Ypos = 0;

                MainControl.AddChild(BarImage);
                Ypos += BarImage.Sprite.Bounds.Height;
                Ypos -= 2;

                MiddleImage.SetPositionY(Ypos, UIUnits.PixelScaled);
                OutcomeText.SetPositionY(Ypos, UIUnits.PixelScaled);
                MainControl.AddChild(MiddleImage);
                Ypos += MiddleImage.Sprite.Bounds.Height;

                BottomImage.SetPositionY(Ypos, UIUnits.PixelScaled);
                MainControl.AddChild(BottomImage);
                Ypos += BottomImage.Sprite.Bounds.Height;

                MainControl.SetHeight(Ypos, UIUnits.PixelScaled);
                MainControl.AddChild(OutcomeText);

                ControlText TitleText = new ControlText(GameSystem);
                MainControl.AddChild(TitleText);
                TitleText.SetWidth(109, UIUnits.Pixel);
                TitleText.SetHeight(26, UIUnits.Pixel);
                TitleText.SetPosition(31, 3, UIUnits.Pixel);
                TitleText.FontName = "Trebuchet MS";
                TitleText.SetFontSize(14, UIUnits.Pixel);
                TitleText.TextAnchor = AnchorPoints.Middle;
                TitleText.Colour = Color.FromArgb(255, 238, 161);
                TitleText.SetTextData(Title);

                ControlImage TitleImage = new ControlImage(GameSystem);
                MainControl.AddChild(TitleImage);
                TitleImage.SetWidth(25, UIUnits.Pixel);
                TitleImage.SetHeight(25, UIUnits.Pixel);
                TitleImage.SetPosition(3, 3, UIUnits.Pixel);
                TitleImage.SetImageFile("Data\\Images\\HUD\\Battle\\floating\\" + TitleImageName + ".png");
            }

            MiddleImage.SetImageFile("Data\\Images\\HUD\\Battle\\floating\\combat_mainbar.png");
            BarImage.SetImageFile("Data\\Images\\HUD\\Battle\\floating\\combat_titlebar.png");

            if (Invert)
            {
                BottomImage.SetImageFile("Data\\Images\\HUD\\Battle\\floating\\combat_bottombar2.png");
            }
            else
            {
                BottomImage.SetImageFile("Data\\Images\\HUD\\Battle\\floating\\combat_bottombar.png");
            }

            FloatingElement Element = new FloatingElement();
            Element.Control = MainControl;
            Element.Inverted = Invert;

            PointF Target = GameSystem.Data.CombatMap.GetScaledTileCoords(Result.Unit.BattleData.BattleX, Result.Unit.BattleData.BattleY);
            if (Invert) Target.Y += 3;
            else Target.Y -= 3;

            Element.PositionX = Target.X;
            Element.PositionY = Target.Y;
            FloatingUIElements.Add(Element);
        }

        void Battle_OnUnitClicked(WorkingUnit Unit, MouseButton Button)
        {
            if (GameForm.Disposed) return;

            if (Unit.OwnerRealm != GameSystem.CurrentGame.PlayerRealm && Battle.DeployPhase) return;

            if (Battle.CurrentAction != null) return;
            if (Unit.Disabled) return;

            if (Battle.Map.CurrentMode == Map.BattleMapModes.Casting)
            {
                HandleCastClick(Unit.BattleData.BattleLocation, Button);
                return;
            }

            if (SelectedUnit == null && Button == MouseButton.Left )
            {
                //if this is a friendy unit, select it
                if (Unit.OwnerRealm == GameSystem.CurrentGame.PlayerRealm)
                {
                    if (GameSystem.UIActionAllowed("BattleSelectUnit"))
                    {
                        SelectUnit(Unit);
                    }
                }
                else
                {
                    if (GameSystem.UIActionAllowed("BattleSelectUnit"))
                    {
                        SelectEnemyUnit(Unit);
                    }
                }

                return;
            }

            if (SelectedUnit != null && Button == MouseButton.Left)
            {
                //if left click is the action button, attempt an action on the selected unit
                //if left click is the select button, select the unit (or deselect if it is an enemy)

                if (GameSystem.Settings.GetBooleanSetting("SwapActionButtons")) PerformSelect(Unit);
                else if (!PerformAction(Unit)) PerformSelect(Unit);
            }

            if (SelectedUnit != null && Button == MouseButton.Right)
            {
                //if right click is the action button, attempt an action on the selected unit
                //if right click is the select button, select the unit (or deselect if it is an enemy)

                if (GameSystem.Settings.GetBooleanSetting("SwapActionButtons")) PerformAction(Unit);
                else SelectUnit(null);
            }
        }

        private void HandleCastClick(Point Tile, MouseButton Button)
        {
            if (Button == MouseButton.Right)
            {
                CancelCasting();
            }
            else if (Button == MouseButton.Left)
            {
                if (SelectedHeroCard.CurrentCard.TileTargetValid(Tile, CardTargetIndex))
                {
                    CardTargets[CardTargetIndex].Tile = Tile;
                    CardTargetIndex++;

                    if (CardTargetIndex >= CardTargets.Count)
                    {
                        FinishCasting();
                    }
                    else
                    {
                        SetCardTarget();
                    }
                }
            }
        }

        private void FinishCasting()
        {
            GameSystem.CurrentGame.PlayerRealm.BattleData.UsedCards.Add(SelectedHeroCard.CurrentCard);
            GameSystem.CurrentGame.PlayerRealm.BattleData.ActiveCards.Remove(SelectedHeroCard.CurrentCard);
            GameSystem.CurrentGame.PlayerRealm.BattleData.CardPlayed = true;

            SelectedHeroCard.CurrentCard.CastEffect(CardTargets);

            GameText Text = GameText.CreateLocalised("FORMAT_BATTLELOG_CARD");
            Text.AddChildText(GameText.CreateLocalised(GameSystem.CurrentGame.PlayerRealm.DisplayName));
            Text.AddChildText(GameText.CreateLocalised(SelectedHeroCard.CurrentCard.DisplayName));
            GameSystem.FireEvent("BattleLogEvent", Text);

            MovingCardData Data = new MovingCardData();
            Data.Card = SelectedHeroCard;
            Data.Card.BringToFront();
            Data.TargetBounds = new RectangleF(0, -400, 50, 80);
            SelectedHeroCard = null;
            MovingCards.Add(Data);

            Battle.Map.SetMode(Map.BattleMapModes.Default);
            SetBattlePhaseText();
            Battle.UpdateShieldWallStatus();
        }

        private void CancelCasting()
        {
            CardControl_OnCancelClicked();
            Battle.Map.SetMode(Map.BattleMapModes.Default);
            SetBattlePhaseText();
        }

        void Battle_OnTileMouseover(Point Tile)
        {
            if (GameForm.Disposed) return;

            MouseoverUnit = null;
            UpdateUnitCards();

            ClearPreviewControls();

            if (SelectedUnit != null && SelectedUnitActions != null && SelectedUnit.OwnerRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                if (SelectedUnitActions.MovementCosts.ContainsKey(Tile))
                {
                    //target tile is a valid move destination
                    //get the battle map to show a movement arrow for it
                    Battle.Map.ShowArrow(SelectedUnit.BattleData.BattleLocation, Tile, SelectedUnitActions);
                }
                else
                {
                    Battle.Map.ClearArrow();
                }
            }
        }

        void Battle_OnTileClicked(Point Tile, MouseButton Button)
        {
            if (GameForm.Disposed) return;

            if (Battle.Map.CurrentMode == Map.BattleMapModes.Casting)
            {
                HandleCastClick(Tile, Button);
                return;
            }

            if (SelectedUnit != null && Button == MouseButton.Left)
            {
                //if left click is the action button, attempt an action on the tile
                //if left click is the select button, deselect active unit

                if (GameSystem.Settings.GetBooleanSetting("SwapActionButtons")) SelectUnit(null);
                else PerformAction(Tile);
            }

            if (SelectedUnit != null && Button == MouseButton.Right)
            {
                //if right click is the action button, attempt an action on the tile
                //if right click is the select button, deselect active unit

                if (GameSystem.Settings.GetBooleanSetting("SwapActionButtons")) PerformAction(Tile);
                else SelectUnit(null);
            }
        }

        private void PerformSelect(WorkingUnit Unit)
        {
            if (GameSystem.UIActionAllowed("BattleSelectUnit") == false)
            {
                SelectUnit(null);
                return;
            }

            if (Unit.OwnerRealm == GameSystem.CurrentGame.PlayerRealm) SelectUnit(Unit);
            else SelectEnemyUnit(Unit);
        }

        /// <summary>
        /// Inspect an enemy: show unit card + ranged range overlay (no move/actions).
        /// </summary>
        private void SelectEnemyUnit(WorkingUnit Unit)
        {
            if (Unit == null || Unit.BattleData == null || Unit.Disabled)
            {
                SelectUnit(null);
                return;
            }

            if (SelectedUnit != null && SelectedUnit.BattleData != null && SelectedUnit.OwnerRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                SelectedUnit.BattleData.Selected = false;
                SelectedUnit.BattleData.UpdateImage();
            }

            SelectedUnit = Unit;
            SelectedUnitActions = null;
            UpdateUnitCards();

            if (Battle.DeployPhase)
            {
                GameSystem.FireEvent("EnemyUnitSelected", Unit);
                return;
            }

            Battle.Map.ClearTargetSprites();
            Battle.Map.ClearArrow();
            Battle.Map.SetMode(Map.BattleMapModes.Default);

            // Same range overlay as friendly selection; skip move overlay (enemy isn't controllable).
            List<Point> RangeTiles = Battle.Map.GetRangeTiles(Unit);
            Battle.Map.SetRangeOverlay(RangeTiles, new Dictionary<Point, float>());

            GameSystem.FireEvent("EnemyUnitSelected", Unit);
        }

        private bool PerformAction(WorkingUnit Unit)
        {
            if (Battle.CurrentAction != null) return false;
            if (Battle.DeployPhase) return false;

            //if an action is possible on the target unit
            //request that action from the battle controller
            if (SelectedUnit != null && SelectedUnitActions != null && SelectedUnit.OwnerRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                if (SelectedUnitActions.Targets.ContainsKey(Unit))
                {
                    CombatAction Action = SelectedUnitActions.Targets[Unit][0];
                    Battle.RequestAttackUnit(SelectedUnit, Unit, SelectedUnitActions, Action);

                    CurrentActionUnit = SelectedUnit;

                    SelectUnit(null);
                    return true;
                }
            }

            return false;
        }

        private void PerformAction(Point Tile)
        {
            if (Battle.CurrentAction != null) return;
            if (Battle.DeployPhase) SelectUnit(null);

            //if the tile is a valid move target, perform move action
            if (SelectedUnit != null && SelectedUnitActions != null && SelectedUnit.OwnerRealm == GameSystem.CurrentGame.PlayerRealm)
            {
                if (SelectedUnitActions.MovementCosts.ContainsKey(Tile))
                {
                    if (SelectedUnit.Class == UnitClasses.Siege && SelectedUnitActions.MovementCosts[Tile] > SelectedUnit.CombatMoves)
                    {
                        SelectedUnit = Battle.PackUnit(SelectedUnit, GameSystem.CurrentGame.PlayerRealm.UnitPurchaseManager.GetUnitByClass(UnitClasses.Wagon));
                    }

                    if (SelectedUnit.CarriedUnit != null && SelectedUnit.Class == UnitClasses.Naval && Battle.Map.GetTile(Tile).Terrain.BaseType.IsAnyType("sea") == false)
                    {
                        SelectedUnit = Battle.UnpackUnit(SelectedUnit);

                        UnitFlag Disorder = UnitFlag.CreateNamedFlag(GameSystem, "Disorder");
                        Disorder.TurnCount = 2;
                        SelectedUnit.GrantFlag(Disorder);
                    }

                    GameSystem.FireEvent("TacticalUnitMoved", SelectedUnit);
                    Battle.RequestMoveUnit(SelectedUnit, Tile, SelectedUnitActions);

                    CurrentActionUnit = SelectedUnit;

                    SelectUnit(null);
                    return;
                }
            }

            //otherwise treat it as a deselect click
            SelectUnit(null);
        }

        private void UpdateUnitCards()
        {
            if (SelectedUnit != null && SelectedUnit.Disabled) SelectedUnit = null;

            if (MouseoverUnit == null || MouseoverUnit.Disabled) MouseoverCard.Visible = false;
            else
            {
                MouseoverCard.Visible = true;
                MouseoverCard.SetUnit(MouseoverUnit, SelectedUnit);
            }

            if (SelectedUnit == null) SelectedCard.Visible = false;
            else
            {
                SelectedCard.Visible = true;
                SelectedCard.SetUnit(SelectedUnit, MouseoverUnit);
            }
        }

        public void AttackClicked(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("EndBattleTurn") == false) return;

            //deselect any selected unit to avoid issues on the following turn
            SelectUnit(null);

            if (Battle.Map.CurrentMode == Map.BattleMapModes.Deploy)
            {
                Battle.EndDeployPhase();

                GameForm.GetControlByName<ControlImage>("VictoryBG").Visible = true;
                UpdateTimerText();
            }
            else
            {
                Battle.RequestEndTurn(GameSystem.CurrentGame.PlayerRealm);
            }
        }

        private void SetBattlePhaseText()
        {
            ControlText TitleText = GameForm.GetControlByName<ControlText>("InfoTitle");
            ControlText DescText = GameForm.GetControlByName<ControlText>("InfoText");

            TitleText.SetTextData(GameText.CreateLocalised("BATTLETEXT"));

            string TextName = "BATTLEINFOTEXT";
            if (Battle.Node.NodeType == PathNodeTypes.Sea) TextName = "BATTLEINFONAVAL";

            if (Battle.AttackerRealm == GameSystem.CurrentGame.PlayerRealm) TextName += "A";
            else TextName += "D";

            DescText.SetTextData(GameText.CreateLocalised(TextName));

            GameForm.GetControlByName<ControlImage>("InfoImage").Visible = true;
        }

        private void UpdateTimerText()
        {
            List<GameText> TimerText = new List<GameText>();
            TimerText.Add(GameText.CreateLocalised("TIMERNAVAL", Battle.TurnCounter.GetWinnerTurns()));

            if (Battle.Node.Province != null)
            {
                TimerText.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TimerText.Add(GameText.CreateLocalised("TIMERMSG", Battle.VictoryPointOwnership.Count(x => x.Value.OwnerID == Battle.AttackerRealm.ID), Battle.VictoryPointOwnership.Count));
            }

            GameForm.GetControlByName<ControlText>("VictoryText").SetTextData(TimerText);
            WorkingRealm WinningRealm = Battle.TurnCounter.GetWinningRealm();
            GameForm.GetControlByName<ControlImage>("VictoryFlag").SetImageFile("Data\\Images\\HUD\\flags\\" + WinningRealm.FlagFilename + ".png");

            GameText TT = GameText.CreateLocalised("BATTLEWINNER_TT");
            TT.AddChildText(GameText.CreateLocalised(WinningRealm.DisplayName));

            GameForm.GetControlByName<ControlImage>("VictoryFlag").SetTooltip(TT);
        }

        public void RetreatClicked(UIControl Control)
        {
            if (GameSystem.UIActionAllowed("BattleRetreat") == false) return;
            //cannot retreat if otherp layer has already decided to retreat
            if (GameSystem.CurrentGame.CurrentTacticalBattle.RetreatingPlayer != null) return;

            //ask player if they are sure,
            //point out that this will allow the enemy to take one more turn
            MessageBoxData Msg = new MessageBoxData();
            Msg.MsgType = MessageType.RetreatConfirm;
            Msg.DisplayType = MessageBoxType.YesNo;

            Msg.CaptionText = GameText.CreateLocalised("MSG_RETREAT_TITLE");
            Msg.MessageText = GameText.CreateLocalised("MSG_RETREAT_TEXT");
            Msg.YesText = GameText.CreateLocalised("MSG_RETREAT_YES");
            Msg.NoText = GameText.CreateLocalised("MSG_RETREAT_NO");

            GameSystem.MessageHandler.ShowMessage(Msg);
        }

        public override void Dispose()
        {
            GameSystem.UnregisterEvents(GameSystem_OnScriptEvent, "BattleDeployFinished", "BattleActionCompleted", "FullBattleStarted", "TacticalBattleEnded", "BattleTurnChanged", "UnitsUpdated", "BattleCardsChanged");
            GameSystem.UnregisterEvent(HandleAICard, "AIBattleCard");
            GameSystem.UnregisterEvent(HandleKeyPress, "KeyDown");
            GameSystem.UnregisterEvent(HandleLogEvent, "BattleLogEvent");
            GameSystem.Camera.ViewMatrixChanged -= Camera_ViewMatrixChanged;
            GameSystem.OnViewportChanged -= GameSystem_OnViewportChanged;
        }

        private void HandleKeyPress(string EventName, params object[] Args)
        {
            if (GameForm.Visible == false || Battle == null) return;

            Key key = (Key)Args[0];

            if (key == Key.Comma) PrevUnitClick(null);
            if (key == Key.Period) NextUnitClick(null);
        }

        public void PrevUnitClick(UIControl Control)
        {
            if (Battle.ActiveStack.Owner != GameSystem.CurrentGame.PlayerRealm) return;

            int Index = LastSelectedIndex;
            int StartIndex = Index;

            if (Index < 0 || Index >= Battle.ActiveStack.Units.Count)
            {
                //select the last unit in the stack
                WorkingUnit Unit = Battle.ActiveStack.Units.LastOrDefault(x => x.Disabled == false && (x.BattleData.CanMove || x.BattleData.CanFight));
                if (Unit != null) SelectUnit(Unit); 
            }
            else
            {
                //select previous
                Index--;
                if (Index < 0) Index = Battle.ActiveStack.Units.Count - 1;

                while (UnitCannotAct(Battle.ActiveStack.Units[Index]))
                {
                    Index--;
                    if (Index < 0) Index = Battle.ActiveStack.Units.Count - 1;
                    if (Index == StartIndex) break;
                }

                SelectUnit(Battle.ActiveStack.Units[Index]);
            }
        }

        public void ContinueClicked(UIControl Control)
        {
            GameForm.GetControlByName<ControlImage>("PlayedCardBG").Visible = false;
            GameSystem.FireEvent("AICardAcknowledged");
        }

        public void NextUnitClick(UIControl Control)
        {
            if (Battle.ActiveStack.Owner != GameSystem.CurrentGame.PlayerRealm) return;

            int Index = LastSelectedIndex;
            int StartIndex = Index;

            if (Index < 0 || Index >= Battle.ActiveStack.Units.Count)
            {
                //select the first unit in the stack
                WorkingUnit Unit = Battle.ActiveStack.Units.FirstOrDefault(x => x.Disabled == false && (x.BattleData.CanMove || x.BattleData.CanFight));
                if (Unit != null) SelectUnit(Unit); 
            }
            else 
            {
                //select next
                Index++;
                if (Index >= Battle.ActiveStack.Units.Count) Index = 0;

                while (UnitCannotAct(Battle.ActiveStack.Units[Index]))
                {
                    Index++;
                    if (Index >= Battle.ActiveStack.Units.Count) Index = 0;
                    if (Index == StartIndex) break;
                }

                SelectUnit(Battle.ActiveStack.Units[Index]);
            }
        }

        private bool UnitCannotAct(WorkingUnit Unit)
        {
            if (Unit.Disabled) return true;
            if (Unit.BattleData == null) return true;
            if (!Unit.BattleData.CanFight && !Unit.BattleData.CanMove) return true;
            return false;
        }
    }
}
