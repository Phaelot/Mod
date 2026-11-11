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
    public class ControlUnitCard: UIControl
    {
        ControlImage CardBGImage;
        ControlText TitleText;
        ControlImage FlagImage;
        ControlImage UnitImage;

        ControlImage StatAttackBG;
        ControlImage StatRangedBG;
        ControlText StatAttackText;
        ControlText StatDefenceText;
        ControlText StatRangedText;
        ControlText StatRangeText;

        ControlText StatMoveText;
        ControlText StatDisciplineText;
        ControlText StatInitiativeText;
        ControlText StatHealText;
        ControlText StatUpkeepText;

        ControlImage RankImage;
        ControlImage ClassImage;

        ControlImage XPImage;
        ControlImage MedalImage1;
        ControlImage MedalImage2;
        ControlImage MedalImage3;
        ControlImage MedalImage4;

        ControlImage HealthBar;
        ControlImage MoraleBar;

        ControlButton DisbandButton;

        float HealthBarMaxWidth;

        ControlList<UnitFlag> FlagList;

        List<ControlImage> SpellImages;

        public WorkingUnit CurrentUnit;

        public ControlUnitCard(GameBase Game)
            :base(Game)
        {
            CreateControls();
        }

        private void CreateControls()
        {
            UnitImage = new ControlImage(Game);
            AddChild(UnitImage);

            CardBGImage = new ControlImage(Game);
            AddChild(CardBGImage);

            FlagImage = new ControlImage(Game);
            AddChild(FlagImage);

            RankImage = new ControlImage(Game);
            RankImage.MouseInputType = MouseInputTypes.Forced;
            AddChild(RankImage);

            ClassImage = new ControlImage(Game);
            ClassImage.MouseInputType = MouseInputTypes.Forced;
            AddChild(ClassImage);

            HealthBar = new ControlImage(Game);
            HealthBar.MouseInputType = MouseInputTypes.Forced;
            AddChild(HealthBar);
            HealthBar.SetImageFile("Data\\Images\\Combat\\health_10.png");

            MoraleBar = new ControlImage(Game);
            MoraleBar.MouseInputType = MouseInputTypes.Forced;
            AddChild(MoraleBar);
            MoraleBar.SetImageFile("Data\\Images\\Combat\\morale_10.png");

            MedalImage1 = new ControlImage(Game);
            MedalImage1.SetImageFile("Data\\Images\\HUD\\medal.png");
            AddChild(MedalImage1);

            MedalImage2 = new ControlImage(Game);
            MedalImage2.SetImageFile("Data\\Images\\HUD\\medal.png");
            AddChild(MedalImage2);

            MedalImage3 = new ControlImage(Game);
            MedalImage3.SetImageFile("Data\\Images\\HUD\\medal.png");
            AddChild(MedalImage3);

            MedalImage4 = new ControlImage(Game);
            MedalImage4.SetImageFile("Data\\Images\\HUD\\medal.png");
            AddChild(MedalImage4);

            XPImage = new ControlImage(Game);
            AddChild(XPImage);

            TitleText = new ControlText(Game);
            AddChild(TitleText);
            TitleText.FontName = "Trebuchet MS";
            TitleText.Colour = Color.FromArgb(0, 0, 0);
            TitleText.TextAnchor = AnchorPoints.Middle;

            StatAttackBG = new ControlImage(Game);
            AddChild(StatAttackBG);
            StatAttackBG.MouseInputType = MouseInputTypes.None;

            StatRangedBG = new ControlImage(Game);
            AddChild(StatRangedBG);
            StatRangedBG.MouseInputType = MouseInputTypes.None;

            StatAttackText = new ControlText(Game);
            AddChild(StatAttackText);
            StatAttackText.FontName = "Trebuchet MS";
            StatAttackText.Colour = Color.FromArgb(0, 0, 0);
            StatAttackText.TextAnchor = AnchorPoints.Middle;
            StatAttackText.MouseInputType = MouseInputTypes.Forced;

            StatDefenceText = new ControlText(Game);
            AddChild(StatDefenceText);
            StatDefenceText.FontName = "Trebuchet MS";
            StatDefenceText.Colour = Color.FromArgb(0, 0, 0);
            StatDefenceText.TextAnchor = AnchorPoints.Middle;
            StatDefenceText.MouseInputType = MouseInputTypes.Forced;
            StatDefenceText.SetTooltip(GameText.CreateLocalised("UNITDEFENSETOOLTIP"));

            StatRangedText = new ControlText(Game);
            AddChild(StatRangedText);
            StatRangedText.FontName = "Trebuchet MS";
            StatRangedText.Colour = Color.FromArgb(0, 0, 0);
            StatRangedText.TextAnchor = AnchorPoints.Middle;
            StatRangedText.MouseInputType = MouseInputTypes.Forced;

            StatRangeText = new ControlText(Game);
            AddChild(StatRangeText);
            StatRangeText.FontName = "Trebuchet MS";
            StatRangeText.Colour = Color.FromArgb(0, 0, 0);
            StatRangeText.TextAnchor = AnchorPoints.Middle;
            StatRangeText.MouseInputType = MouseInputTypes.Forced;
            StatRangeText.SetTooltip(GameText.CreateLocalised("UNITRANGETOOLTIP"));

            StatMoveText = new ControlText(Game);
            AddChild(StatMoveText);
            StatMoveText.FontName = "Trebuchet MS";
            StatMoveText.Colour = Color.FromArgb(0, 0, 0);
            StatMoveText.TextAnchor = AnchorPoints.Middle;
            StatMoveText.MouseInputType = MouseInputTypes.Forced;
            StatMoveText.SetTooltip(GameText.CreateLocalised("UNITMOVESTOOLTIP"));

            StatDisciplineText = new ControlText(Game);
            AddChild(StatDisciplineText);
            StatDisciplineText.FontName = "Trebuchet MS";
            StatDisciplineText.Colour = Color.FromArgb(0, 0, 0);
            StatDisciplineText.TextAnchor = AnchorPoints.Middle;
            StatDisciplineText.MouseInputType = MouseInputTypes.Forced;
            StatDisciplineText.SetTooltip(GameText.CreateLocalised("UNITMORALETOOLTIP"));

            StatUpkeepText = new ControlText(Game);
            AddChild(StatUpkeepText);
            StatUpkeepText.FontName = "Trebuchet MS";
            StatUpkeepText.Colour = Color.FromArgb(0, 0, 0);
            StatUpkeepText.TextAnchor = AnchorPoints.Middle;
            StatUpkeepText.MouseInputType = MouseInputTypes.Forced;

            StatInitiativeText = new ControlText(Game);
            AddChild(StatInitiativeText);
            StatInitiativeText.FontName = "Trebuchet MS";
            StatInitiativeText.Colour = Color.FromArgb(0, 0, 0);
            StatInitiativeText.TextAnchor = AnchorPoints.Middle;
            StatInitiativeText.MouseInputType = MouseInputTypes.Forced;
            StatInitiativeText.SetTooltip(GameText.CreateLocalised("UNITSPEEDTOOLTIP"));

            StatHealText = new ControlText(Game);
            AddChild(StatHealText);
            StatHealText.FontName = "Trebuchet MS";
            StatHealText.Colour = Color.FromArgb(0, 0, 0);
            StatHealText.TextAnchor = AnchorPoints.Middle;
            StatHealText.MouseInputType = MouseInputTypes.Forced;
            StatHealText.SetTooltip(GameText.CreateLocalised("UNITHEALTOOLTIP"));

            FlagList = new ControlList<UnitFlag>(Game);
            AddChild(FlagList);
            FlagList.SetFontName("Trebuchet MS");
            FlagList.SetFontColour(Color.FromArgb(0, 0, 0));
            FlagList.SetVisibleItems(4);
            FlagList.ManualUpdate = true;
            FlagList.OnListItemUpdated += FlagList_OnListItemUpdated;

            DisbandButton = new ControlButton(Game);
            AddChild(DisbandButton);
            DisbandButton.SetImageFiles("Data\\Images\\HUD\\Purchase\\cancel_normal.png", "Data\\Images\\HUD\\Purchase\\cancel_mouseover.png", "Data\\Images\\HUD\\Purchase\\cancel_pressed.png");
            DisbandButton.SetTooltip(GameText.CreateLocalised("TT_DISBAND"));
            DisbandButton.OnClick += DisbandButton_OnClick;

            foreach (UIControl Control in GetChildren())
            {
                if (Control == XPImage) continue;
                if (Control == MedalImage1) continue;
                if (Control == MedalImage2) continue;
                if (Control == MedalImage3) continue;
                if (Control == MedalImage4) continue;
                if (Control == RankImage) continue;
                if (Control == ClassImage) continue;
                if (Control == HealthBar) continue;
                if (Control == MoraleBar) continue;
                if (Control == StatAttackText) continue;
                if (Control == StatDefenceText) continue;
                if (Control == StatDisciplineText) continue;
                if (Control == StatUpkeepText) continue;
                if (Control == StatHealText) continue;
                if (Control == StatInitiativeText) continue;
                if (Control == StatMoveText) continue;
                if (Control == StatRangedText) continue;
                if (Control == StatRangeText) continue;
                if (Control == DisbandButton) continue;

                Control.BubbleMouseovers = true;
            }

            SpellImages = new List<ControlImage>();
            for (int i = 0; i < 6; i++)
            {
                ControlImage SpellImage = new ControlImage(Game);
                SpellImages.Add(SpellImage);
                AddChild(SpellImage);
            }
        }

        void DisbandButton_OnClick(UIControl Control)
        {
            if (CurrentUnit.Game.GameCore.UIActionAllowed("DisbandUnit") == false) return;

            UnitQueueItem Item = Control.CustomData as UnitQueueItem;

            //offer to remove the unit
            MessageBoxData Msg = new MessageBoxData();

            Msg.CaptionText = GameText.CreateLocalised("DISBAND_CAPTION");
            Msg.MessageText = GameText.CreateLocalised("DISBAND_MESSAGE");
            Msg.YesText = GameText.CreateLocalised("DISBAND_YES");
            Msg.NoText = GameText.CreateLocalised("DISBAND_NO");

            if (CurrentUnit.OwnerStack.Hero != null && CurrentUnit.OwnerStack.Units.Count == 1)
            {
                Msg.MessageText = GameText.CreateLocalised("DISBAND_MESSAGE3");
            }

            Msg.DisplayType = MessageBoxType.YesNo;
            Msg.MsgType = MessageType.Disband;

            Msg.Unit = CurrentUnit;
            CurrentUnit.Game.GameCore.MessageHandler.ShowMessage(Msg);
        }

        void FlagList_OnListItemUpdated(ListItem<UnitFlag> Item)
        {
            ControlText UnitText = new ControlText(Game);
            UnitText.SetBounds(0, 0, Item.Container.Sprite.Bounds.Width, Item.Container.Sprite.Bounds.Height, UIUnits.PixelScaled);
            UnitText.SetFontSize((int)(Item.Container.Sprite.Bounds.Height * 0.8f), UIUnits.PixelScaled);
            UnitText.FontName = "Trebuchet MS";
            UnitText.TextAnchor = AnchorPoints.Left;
            UnitText.Colour = Color.FromArgb(0, 0, 0);
            UnitText.SetTextData(GameText.CreateLocalised(Item.Data.DisplayName));
            
            Item.Container.SetTooltip(GameText.CreateLocalised(Item.Data.DisplayDesc));
            Item.Container.AddChild(UnitText);
        }

        private void SetControlPositions()
        {
            CardBGImage.SetBounds(0, 0, Sprite.Bounds.Width, Sprite.Bounds.Height, UIUnits.PixelScaled);
            TitleText.SetBounds(Sprite.Bounds.Width * 0.101f, Sprite.Bounds.Height * 0.054f, Sprite.Bounds.Width * 0.645f, Sprite.Bounds.Height * 0.074f, UIUnits.PixelScaled);
            FlagImage.SetBounds(Sprite.Bounds.Width * 0.755f, Sprite.Bounds.Height * 0.054f, Sprite.Bounds.Width * 0.140f, Sprite.Bounds.Height * 0.074f, UIUnits.PixelScaled);
            UnitImage.SetBounds(Sprite.Bounds.Width * 0.177f, Sprite.Bounds.Height * 0.156f, Sprite.Bounds.Width * 0.645f, Sprite.Bounds.Height * 0.386f, UIUnits.PixelScaled);

            XPImage.SetBounds(Sprite.Bounds.Width * 0.168f, Sprite.Bounds.Height * 0.460f, Sprite.Bounds.Width * 0.283f, Sprite.Bounds.Height * 0.045f, UIUnits.PixelScaled);

            MedalImage1.SetBounds(Sprite.Bounds.Width * 0.168f, Sprite.Bounds.Height * 0.494f, Sprite.Bounds.Width * 0.070f, Sprite.Bounds.Height * 0.045f, UIUnits.PixelScaled);
            MedalImage2.SetBounds(Sprite.Bounds.Width * 0.235f, Sprite.Bounds.Height * 0.494f, Sprite.Bounds.Width * 0.070f, Sprite.Bounds.Height * 0.045f, UIUnits.PixelScaled);
            MedalImage3.SetBounds(Sprite.Bounds.Width * 0.302f, Sprite.Bounds.Height * 0.494f, Sprite.Bounds.Width * 0.070f, Sprite.Bounds.Height * 0.045f, UIUnits.PixelScaled);
            MedalImage4.SetBounds(Sprite.Bounds.Width * 0.369f, Sprite.Bounds.Height * 0.494f, Sprite.Bounds.Width * 0.070f, Sprite.Bounds.Height * 0.045f, UIUnits.PixelScaled);

            HealthBar.SetBounds(Sprite.Bounds.Width * 0.188f, Sprite.Bounds.Height * 0.564f, 0, Sprite.Bounds.Height * 0.020f, UIUnits.PixelScaled);
            MoraleBar.SetBounds(Sprite.Bounds.Width * 0.188f, Sprite.Bounds.Height * 0.544f, 0, Sprite.Bounds.Height * 0.020f, UIUnits.PixelScaled);

            HealthBarMaxWidth = Sprite.Bounds.Width * 0.622f;

            // Top row keeps the original 4 wells: Attack, Defense, Ranged, Discipline
            // (Discipline occupies the old Range well; Range is shown on the ranged tooltip.)
            StatAttackBG.SetBounds(Sprite.Bounds.Width * 0.177f, Sprite.Bounds.Height * 0.605f, Sprite.Bounds.Width * 0.117f, Sprite.Bounds.Height * 0.068f, UIUnits.PixelScaled);
            StatRangedBG.SetBounds(Sprite.Bounds.Width * 0.532f, Sprite.Bounds.Height * 0.605f, Sprite.Bounds.Width * 0.117f, Sprite.Bounds.Height * 0.068f, UIUnits.PixelScaled);

            StatAttackText.SetBounds(Sprite.Bounds.Width * 0.177f, Sprite.Bounds.Height * 0.605f, Sprite.Bounds.Width * 0.117f, Sprite.Bounds.Height * 0.068f, UIUnits.PixelScaled);
            StatDefenceText.SetBounds(Sprite.Bounds.Width * 0.355f, Sprite.Bounds.Height * 0.605f, Sprite.Bounds.Width * 0.117f, Sprite.Bounds.Height * 0.068f, UIUnits.PixelScaled);
            StatRangedText.SetBounds(Sprite.Bounds.Width * 0.532f, Sprite.Bounds.Height * 0.605f, Sprite.Bounds.Width * 0.117f, Sprite.Bounds.Height * 0.068f, UIUnits.PixelScaled);
            StatDisciplineText.SetBounds(Sprite.Bounds.Width * 0.709f, Sprite.Bounds.Height * 0.605f, Sprite.Bounds.Width * 0.117f, Sprite.Bounds.Height * 0.068f, UIUnits.PixelScaled);

            // Range text is hidden; value is folded into the ranged tooltip.
            StatRangeText.SetBounds(0, 0, 1, 1, UIUnits.PixelScaled);
            StatRangeText.Visible = false;

            // Bottom row: Move, Initiative, Upkeep, Heal
            StatMoveText.SetBounds(Sprite.Bounds.Width * 0.264f, Sprite.Bounds.Height * 0.693f, Sprite.Bounds.Width * 0.106f, Sprite.Bounds.Height * 0.052f, UIUnits.PixelScaled);
            StatInitiativeText.SetBounds(Sprite.Bounds.Width * 0.385f, Sprite.Bounds.Height * 0.693f, Sprite.Bounds.Width * 0.106f, Sprite.Bounds.Height * 0.052f, UIUnits.PixelScaled);
            // Keep width aligned with Move/Initiative so the well border isn't covered.
            StatUpkeepText.SetBounds(Sprite.Bounds.Width * 0.506f, Sprite.Bounds.Height * 0.693f, Sprite.Bounds.Width * 0.106f, Sprite.Bounds.Height * 0.052f, UIUnits.PixelScaled);
            StatHealText.SetBounds(Sprite.Bounds.Width * 0.630f, Sprite.Bounds.Height * 0.693f, Sprite.Bounds.Width * 0.106f, Sprite.Bounds.Height * 0.052f, UIUnits.PixelScaled);

            RankImage.SetBounds(Sprite.Bounds.Width * 0.121f, Sprite.Bounds.Height * 0.682f, Sprite.Bounds.Width * 0.125f, Sprite.Bounds.Height * 0.074f, UIUnits.PixelScaled);
            ClassImage.SetBounds(Sprite.Bounds.Width * 0.758f, Sprite.Bounds.Height * 0.682f, Sprite.Bounds.Width * 0.125f, Sprite.Bounds.Height * 0.074f, UIUnits.PixelScaled);

            FlagList.SetBounds(Sprite.Bounds.Width * 0.113f, Sprite.Bounds.Height * 0.767f, Sprite.Bounds.Width * 0.694f, Sprite.Bounds.Height * 0.181f, UIUnits.PixelScaled);

            DisbandButton.SetBounds(Sprite.Bounds.Width * 0.803f, Sprite.Bounds.Height * 0.884f, Sprite.Bounds.Width * 0.192f, Sprite.Bounds.Height * 0.108f, UIUnits.PixelScaled);

            TitleText.SetFontSize((int)(TitleText.Sprite.Bounds.Height * 0.5f), UIUnits.PixelScaled);

            StatAttackText.SetFontSize((int)(StatAttackText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            StatDefenceText.SetFontSize((int)(StatDefenceText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            StatRangedText.SetFontSize((int)(StatRangedText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            StatRangeText.SetFontSize((int)(StatRangeText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);

            StatMoveText.SetFontSize((int)(StatMoveText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            StatInitiativeText.SetFontSize((int)(StatInitiativeText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            StatDisciplineText.SetFontSize((int)(StatDisciplineText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            StatUpkeepText.SetFontSize((int)(StatUpkeepText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);
            StatHealText.SetFontSize((int)(StatHealText.Sprite.Bounds.Height * 0.9f), UIUnits.PixelScaled);

            for (int i = 0; i < 6; i++)
            {
                float XDist = 0.078f;
                float XPos = 0.177f + (i * XDist);

                SpellImages[i].SetBounds(Sprite.Bounds.Width * XPos, Sprite.Bounds.Height * 0.156f, Sprite.Bounds.Width * 0.078f, Sprite.Bounds.Height * 0.045f, UIUnits.PixelScaled);
            }
        }

        public void DisplayUnit(WorkingUnit Unit, WorkingUnit OtherUnit, bool Carried)
        {
            // Preview units never show Disband (also hidden again at end of this method).
            if (Unit.ID < 0 || Unit.OwnerStack == null) DisbandButton.Visible = false;

            switch (Unit.OwnerRealm.Panel)
            {
                case SovereigntyTK.Game.Data.SpellSchools.Death: CardBGImage.SetImageFile("Data\\Images\\HUD\\Battle\\deathcard_unit.png"); break;
                case SovereigntyTK.Game.Data.SpellSchools.Illusion: CardBGImage.SetImageFile("Data\\Images\\HUD\\Battle\\illusioncard_unit.png"); break;
                case SovereigntyTK.Game.Data.SpellSchools.Nature: CardBGImage.SetImageFile("Data\\Images\\HUD\\Battle\\naturecard_unit.png"); break;
                case SovereigntyTK.Game.Data.SpellSchools.War: CardBGImage.SetImageFile("Data\\Images\\HUD\\Battle\\warcard_unit.png"); break;
            }

            StatAttackBG.Visible = true;
            StatRangedBG.Visible = true;

            GameText TT1 = GameText.CreateLocalised("UNITATTACKTOOLTIP");
            string DamageTypeKey = "DAMPHYS";

            DamageTypes DmgType = DamageTypes.Physical;
            try { DmgType = Unit.GetDamageType(); }
            catch { DmgType = DamageTypes.Physical; }

            switch (DmgType)
            {
                case DamageTypes.None:
                case DamageTypes.Physical:
                    {
                        StatAttackBG.Visible = false;
                        StatRangedBG.Visible = false;
                        DamageTypeKey = "DAMPHYS";
                        TT1.AddChildText(GameText.CreateLocalised(DamageTypeKey));
                        break;
                    }
                case DamageTypes.Death:
                    {
                        StatAttackBG.SetImageFile("Data\\Images\\Combat\\alignment_death.png");
                        StatRangedBG.SetImageFile("Data\\Images\\Combat\\alignment_death.png");
                        DamageTypeKey = "DAMDEA";
                        TT1.AddChildText(GameText.CreateLocalised(DamageTypeKey));
                        break;
                    }
                case DamageTypes.Illusion:
                    {
                        StatAttackBG.SetImageFile("Data\\Images\\Combat\\alignment_illusion.png");
                        StatRangedBG.SetImageFile("Data\\Images\\Combat\\alignment_illusion.png");
                        DamageTypeKey = "DAMILL";
                        TT1.AddChildText(GameText.CreateLocalised(DamageTypeKey));
                        break;
                    }
                case DamageTypes.Nature:
                    {
                        StatAttackBG.SetImageFile("Data\\Images\\Combat\\alignment_nature.png");
                        StatRangedBG.SetImageFile("Data\\Images\\Combat\\alignment_nature.png");
                        DamageTypeKey = "DAMNAT";
                        TT1.AddChildText(GameText.CreateLocalised(DamageTypeKey));
                        break;
                    }
                case DamageTypes.War:
                    {
                        StatAttackBG.SetImageFile("Data\\Images\\Combat\\alignment_war.png");
                        StatRangedBG.SetImageFile("Data\\Images\\Combat\\alignment_war.png");
                        DamageTypeKey = "DAMWAR";
                        TT1.AddChildText(GameText.CreateLocalised(DamageTypeKey));
                        break;
                    }
            }

            StatAttackText.SetTooltip(TT1);
            // Icon shows Range; tooltip lists Range + Ranged Damage on separate lines.
            StatRangedText.SetTooltip(BuildRangedTooltip(Unit, OtherUnit, DamageTypeKey));
            StatRangeText.Visible = false;

            if (Unit.Game.GameCore.UnitIDsEnabled)
            {
                GameText Text = GameText.CreateLocalised("UNIT_CARRYTEXT");
                Text.AddChildText(GameText.CreateLocalised(Unit.DisplayName));
                Text.AddChildText(GameText.CreateLocalised("FORMAT_NUMBER", Unit.ID));
                TitleText.SetTextData(Text);
            }
            else
            {
                TitleText.SetTextData(GameText.CreateLocalised(Unit.DisplayName));
            }

            int CurrentLevel = Unit.Medals;
            int NextLevel = Unit.Medals + 1;

            if (CurrentLevel == 4)
            {
                XPImage.SetImageFile("Data\\Images\\HUD\\Magic\\xpbar_10.png");
                XPImage.SetTooltip(GameText.CreateLocalised("TT_UNITXPFULL"));
            }
            else
            {
                int LastLevelXP = Unit.GetXPForMedal(CurrentLevel);
                int XPNeeded = Unit.GetXPForMedal(NextLevel);
                int XPGained = Unit.XP;

                XPNeeded -= LastLevelXP;
                XPGained -= LastLevelXP;

                float Pct = (float)XPGained / (float)XPNeeded;
                Pct *= 10;

                int ImageNumber = (int)Pct + 1;
                if (ImageNumber > 10) ImageNumber = 10;
                if (ImageNumber < 0) ImageNumber = 10;

                string Num = ImageNumber.ToString();
                if (Num.Length < 2) Num = "0" + Num;

                XPImage.SetImageFile("Data\\Images\\HUD\\Magic\\xpbar_" + Num + ".png");
                XPImage.SetTooltip(GameText.CreateLocalised("TT_UNITXP", XPGained, XPNeeded));
            }

            MedalImage1.Visible = false;
            MedalImage2.Visible = false;
            MedalImage3.Visible = false;
            MedalImage4.Visible = false;

            if (Unit.Medals >= 1)
            {
                MedalImage1.Visible = true;
                UnitFlag Flag = UnitFlag.CreateNamedFlag(Unit.Game.GameCore, Unit.MedalNames[0]);

                List<GameText> TT = new List<GameText>();
                TT.Add(GameText.CreateLocalised(Flag.DisplayName));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised(Flag.DisplayDesc));

                MedalImage1.SetTooltip(TT);
            }

            if (Unit.Medals >= 2)
            {
                MedalImage2.Visible = true;
                UnitFlag Flag = UnitFlag.CreateNamedFlag(Unit.Game.GameCore, Unit.MedalNames[1]);

                List<GameText> TT = new List<GameText>();
                TT.Add(GameText.CreateLocalised(Flag.DisplayName));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised(Flag.DisplayDesc));

                MedalImage2.SetTooltip(TT);
            }

            if (Unit.Medals >= 3)
            {
                MedalImage3.Visible = true;
                UnitFlag Flag = UnitFlag.CreateNamedFlag(Unit.Game.GameCore, Unit.MedalNames[2]);

                List<GameText> TT = new List<GameText>();
                TT.Add(GameText.CreateLocalised(Flag.DisplayName));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised(Flag.DisplayDesc));

                MedalImage3.SetTooltip(TT);
            }

            if (Unit.Medals >= 4)
            {
                MedalImage4.Visible = true;
                UnitFlag Flag = UnitFlag.CreateNamedFlag(Unit.Game.GameCore, Unit.MedalNames[3]);

                List<GameText> TT = new List<GameText>();
                TT.Add(GameText.CreateLocalised(Flag.DisplayName));
                TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                TT.Add(GameText.CreateLocalised(Flag.DisplayDesc));

                MedalImage4.SetTooltip(TT);
            }

            FlagImage.SetImageFile("Data\\Images\\HUD\\flags\\" + Unit.OwnerRealm.FlagFilename + ".png");
            UnitImage.SetImageFile("Data\\Images\\Units\\" + Unit.ImageFile + ".jpg");

            StatAttackText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", GetUnitStat(Unit.Attack, Unit, OtherUnit)));
            StatDefenceText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", GetUnitStat(Unit.Defence, Unit, OtherUnit)));
            // Bow icon shows Range; melee / no ranged attack displays as 0.
            int DisplayRange = GetDisplayRange(Unit, OtherUnit);
            StatRangedText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", DisplayRange));
            StatRangeText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", DisplayRange));

            StatMoveText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", GetUnitStat(Unit.MaxCombatMoves, Unit, OtherUnit)));
            StatInitiativeText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", GetUnitStat(Unit.Initiative, Unit, OtherUnit)));
            StatDisciplineText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", GetUnitStat(Unit.Discipline, Unit, OtherUnit)));
            StatHealText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", GetUnitStat(Unit.HealRate, Unit, OtherUnit)));

            int CurrentUpkeep = GetUnitStat(Unit.Upkeep, Unit, null);
            StatUpkeepText.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", CurrentUpkeep));
            // 2–3 digit upkeep (e.g. Ballista ~130) must shrink or digits overlap.
            int UpkeepDigits = Math.Abs(CurrentUpkeep).ToString().Length;
            float UpkeepFontScale = UpkeepDigits >= 3 ? 0.52f : (UpkeepDigits == 2 ? 0.68f : 0.9f);
            StatUpkeepText.SetFontSize((int)(StatUpkeepText.Sprite.Bounds.Height * UpkeepFontScale), UIUnits.PixelScaled);
            StatUpkeepText.SetTooltip(BuildUpkeepTooltip(Unit));

            float HealthPct = (float)Unit.Health * 0.01f;
            HealthBar.SetWidth(HealthBarMaxWidth * HealthPct, UIUnits.PixelScaled);
            HealthBar.SetTooltip(GameText.CreateLocalised("TT_HEALTH", Unit.Health));

            float MoralePCt = (float)Unit.Morale * 0.01f;
            MoraleBar.SetWidth(HealthBarMaxWidth * MoralePCt, UIUnits.PixelScaled);
            MoraleBar.SetTooltip(GameText.CreateLocalised("TT_MORALE", Unit.Morale));

            GameText Tooltip = GameText.CreateLocalised("TT_UNITCLASS");

            switch (Unit.Class)
            {
                case UnitClasses.Fort:
                    {
                        ClassImage.SetImageFile("Data\\Images\\HUD\\icons_siege.png");
                        Tooltip.AddChildText(GameText.CreateLocalised("CLASS_FORT"));
                        break;
                    }
                case UnitClasses.Archer:
                    {
                        ClassImage.SetImageFile("Data\\Images\\HUD\\icons_archer.png");
                        Tooltip.AddChildText(GameText.CreateLocalised("CLASS_ARCHER"));
                        break;
                    }
                case UnitClasses.Cavalry:
                    {
                        ClassImage.SetImageFile("Data\\Images\\HUD\\icons_cavalry.png");
                        Tooltip.AddChildText(GameText.CreateLocalised("CLASS_CAVALRY"));
                        break;
                    }
                case UnitClasses.Infantry:
                    {
                        ClassImage.SetImageFile("Data\\Images\\HUD\\icons_infantry.png");
                        Tooltip.AddChildText(GameText.CreateLocalised("CLASS_INFANTRY"));
                        break;
                    }
                case UnitClasses.Irregular:
                    {
                        ClassImage.SetImageFile("Data\\Images\\HUD\\icons_irregular.png");
                        Tooltip.AddChildText(GameText.CreateLocalised("CLASS_IRREGULAR"));
                        break;
                    }
                case UnitClasses.Siege:
                    {
                        ClassImage.SetImageFile("Data\\Images\\HUD\\icons_siege.png");
                        Tooltip.AddChildText(GameText.CreateLocalised("CLASS_SIEGE"));
                        break;
                    }
                case UnitClasses.Naval:
                    {
                        ClassImage.SetImageFile("Data\\Images\\HUD\\icons_naval.png");
                        Tooltip.AddChildText(GameText.CreateLocalised("CLASS_NAVAL"));
                        break;
                    }
            }

            ClassImage.SetTooltip(Tooltip);

            Tooltip = GameText.CreateLocalised("TT_UNITRANK");


            switch (Unit.Rank)
            {
                case UnitRanks.Standard:
                    {
                        RankImage.SetImageFile("Data\\Images\\HUD\\unit_standard.png");
                        RankImage.SetTooltip(GameText.CreateLocalised("UNITSTANDARDTOOLTIP"));
                        break;
                    }
                case UnitRanks.Mercenary:
                    {
                        RankImage.SetImageFile("Data\\Images\\HUD\\unit_merc.png");
                        RankImage.SetTooltip(GameText.CreateLocalised("UNITMERCENARYTOOLTIP"));
                        break;
                    }
                case UnitRanks.Elite:
                    {
                        RankImage.SetImageFile("Data\\Images\\HUD\\unit_elite.png");
                        RankImage.SetTooltip(GameText.CreateLocalised("UNITELITETOOLTIP"));
                        break;
                    }
                case UnitRanks.Unique:
                    {
                        RankImage.SetImageFile("Data\\Images\\HUD\\unit_unique.png");
                        RankImage.SetTooltip(GameText.CreateLocalised("UNITUNIQUETOOLTIP"));
                        break;
                    }
            }

            // Battle-only flags must never linger on the strategic map.
            if (Unit.BattleData == null && Unit.HasAnyNamedFlag("Shield Wall"))
            {
                Unit.RemoveNamedFlags("Shield Wall", 0);
            }

            FlagList.ClearItems();
            foreach (UnitFlag Flag in Unit.ActiveFlags)
            {
                if (Flag.Visible == false) continue;
                FlagList.AddItem(Flag);
            }

            UpdateSpells();

            DisbandButton.Visible = true;
            if (Unit.IsPrisoner) DisbandButton.Visible = false;
            if (Unit.OwnerRealm != Unit.Game.PlayerRealm) DisbandButton.Visible = false;
            if (Unit.BattleData != null) DisbandButton.Visible = false;
            if (Carried) DisbandButton.Visible = false;
            // Preview / unstacked units (e.g. FA auxiliaries list) must never show Disband.
            if (Unit.OwnerStack == null) DisbandButton.Visible = false;
        }

        public void SetUnit(WorkingUnit Unit, WorkingUnit OtherUnit)
        {
            if (CurrentUnit != null) CurrentUnit.SpellEffects.SpellsChanged -= SpellEffects_SpellsChanged;
            Unit.SpellEffects.SpellsChanged += SpellEffects_SpellsChanged;

            CurrentUnit = Unit;

            if (Unit.CarriedUnit != null && Unit.Class == UnitClasses.Naval && Unit.BattleData != null && Unit.BattleData.Battle != null && Unit.BattleData.Battle.DeployPhase)
            {
                try
                {
                    DisplayUnit(Unit.CarriedUnit, OtherUnit, true);
                }
                catch (Exception e)
                {
                    Game.FireEvent("UnitDisplayFailed", Unit);
                }
            }
            else
            {
                try
                {
                    DisplayUnit(Unit, OtherUnit, false);
                }
                catch (Exception e)
                {
                    Game.FireEvent("UnitDisplayFailed", Unit);
                }
            }
        }

        void SpellEffects_SpellsChanged()
        {
            UpdateSpells();
        }

        public void UpdateSpells()
        {
            for (int i = 0; i < 6; i++)
            {
                if (CurrentUnit == null || i >= CurrentUnit.SpellEffects.ActiveSpells.Count) SpellImages[i].Visible = false;
                else
                {
                    SpellEffect Spell = CurrentUnit.SpellEffects.ActiveSpells[i];

                    SpellImages[i].Visible = true;
                    SpellImages[i].SetImageFile("Data\\Images\\HUD\\spell\\" + Spell.SpellData.SchoolName + "\\" + Spell.SpellData.ImageFileName + ".png");
                    SpellImages[i].SetTooltip(GetSpellEffectTooltip(Spell));
                }
            }
        }

        public GameText GetSpellEffectTooltip(SpellEffect Effect)
        {
            GameText Text = GameText.CreateLocalised("TT_ACTIVESPELL");
            Text.AddChildText(GameText.CreateLocalised(Effect.SpellData.DisplayName));
            Text.AddChildText(GameText.CreateLocalised(Effect.SpellData.DisplayText));

            if (Effect.Timer >= 0) Text.AddChildText(GameText.CreateLocalised("FORMAT_NUMBER", Effect.Timer));
            else Text.AddChildText(GameText.CreateLocalised("SPELL_PERM"));

            Text.AddChildText(GameText.CreateLocalised(Effect.Caster.DisplayName));

            return Text;
        }

        // Preview units (e.g. FA auxiliaries) are constructed with ID -1 and are not in the
        // game unit table. UnitStat.GetValue looks up by ID and fails — use BaseValue instead.
        private int GetUnitStat(UnitStat Stat, WorkingUnit Unit, WorkingUnit OtherUnit)
        {
            if (Stat == null) return 0;
            if (Unit != null && Unit.ID < 0) return Stat.BaseValue;
            if (OtherUnit != null) return Stat.GetValue(OtherUnit);
            return Stat.GetValue();
        }

        private int GetDisplayRange(WorkingUnit Unit, WorkingUnit OtherUnit)
        {
            int Range = GetUnitStat(Unit.Range, Unit, OtherUnit);
            // Game data treats Range 1 as melee-only; show 0 when the unit cannot shoot.
            if (GetUnitStat(Unit.RangedAttack, Unit, OtherUnit) <= 0 || Range <= 1)
                return 0;
            return Range;
        }

        private GameText BuildRangedTooltip(WorkingUnit Unit, WorkingUnit OtherUnit, string DamageTypeKey)
        {
            // One literal line per entry — List tooltips collapse onto a single line here.
            StringBuilder SB = new StringBuilder();
            SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_CARD_RANGED_INFO")));
            SB.AppendLine();
            SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_CARD_RANGE", GetDisplayRange(Unit, OtherUnit))));
            SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_CARD_RANGED_DAMAGE", GetUnitStat(Unit.RangedAttack, Unit, OtherUnit))));
            SB.Append(ResolveText(GameText.CreateLocalised("TT_CARD_DAMAGE_TYPE", ResolveText(GameText.CreateLocalised(DamageTypeKey)))));
            return GameText.CreateFromLiteral(SB.ToString());
        }

        private GameText BuildUpkeepTooltip(WorkingUnit Unit)
        {
            // One literal line per entry — List/child-text tooltips collapse onto a single line here.
            System.Text.StringBuilder SB = new System.Text.StringBuilder();
            SB.AppendLine(ResolveText(GameText.CreateLocalised("UNITUPKEEPTOOLTIP")));
            SB.AppendLine();

            int Base = Unit.Upkeep.BaseValue;
            int Current = GetUnitStat(Unit.Upkeep, Unit, null);
            SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_UPKEEP_BASE", Base)));

            bool InCapitol = Unit.OwnerStack != null
                && Unit.OwnerStack.Node != null
                && Unit.OwnerStack.Node.Province != null
                && Unit.OwnerStack.Node.Province.IsCapitol
                && Unit.OwnerStack.Node.Province.OwnerRealm == Unit.OwnerRealm;

            if (InCapitol)
            {
                SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_UPKEEP_CAPITOL")));
            }

            bool ForcesZero = false;
            int Explained = Base;

            foreach (UnitFlag Flag in Unit.ActiveFlags)
            {
                if (Flag == null) continue;

                if (Flag.Name == "UpkeepBonus")
                {
                    int Amount = Flag.GetVariable("Amount");
                    if (Amount == 0) continue;
                    Explained -= Amount;
                    SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_UPKEEP_BONUS")) + ": " + (-Amount).ToString("+0;-0;0"));
                }
                else if (Flag.Name == "Champion" || Flag.Name == "Dormant")
                {
                    ForcesZero = true;
                    string Name = ResolveText(GameText.CreateLocalised(Flag.DisplayName));
                    SB.AppendLine(Name + ": " + ResolveText(GameText.CreateLocalised("TT_UPKEEP_NONE")));
                }
            }

            if (Unit.OwnerStack != null)
            {
                int StackMod = Unit.OwnerStack.GetStackModifier(UnitStatNames.Upkeep);
                if (StackMod != 0)
                {
                    Explained += StackMod;
                    SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_UPKEEP_ARMY")) + ": " + StackMod.ToString("+0;-0;0"));
                }
            }

            if (InCapitol || ForcesZero)
            {
                Explained = 0;
            }
            else
            {
                Explained = Math.Max(0, Explained);
                int Remainder = Current - Explained;
                if (Remainder != 0)
                {
                    SB.AppendLine(ResolveText(GameText.CreateLocalised("TT_UPKEEP_OTHER")) + ": " + Remainder.ToString("+0;-0;0"));
                }
            }

            SB.AppendLine();
            SB.Append(ResolveText(GameText.CreateLocalised("TT_UPKEEP_CURRENT", Current)));
            return GameText.CreateFromLiteral(SB.ToString());
        }

        private string ResolveText(GameText Text)
        {
            if (Text == null) return "";
            try { return Text.GetActualText(Game) ?? ""; }
            catch { return ""; }
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
