using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Controls;
using SovereigntyTK.UI.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SovereigntyTK.UI
{
    public class ControlProvinceCard: UIControl
    {
        const int MaxBuildingSlots = 10;

        ControlImage CardBGImage;
        private WorkingProvince CurrentProvince;

        ControlImage FlagImage;
        ControlImage ProvinceImage;

        ControlText NameText;
        ControlText DescriptionText;

        List<ControlImage> SpellImages;
        ControlImage PlagueIcon;

        ControlImage EconomyBG;
        ControlText EconomyText2;
        ControlText RevoltText;
        ControlText IncomeText;
        ControlText ResearchText;
        ControlText TerrainText;

        List<ControlImage> BuildingIcons;
        List<ControlImage> BuildingFrames;
        int BuildingSlotCount;

        public ControlProvinceCard(GameBase Game)
            :base(Game)
        {
            CreateControls();
        }

        private void CreateControls()
        {
            CardBGImage = new ControlImage(Game);
            CardBGImage.MouseInputType = MouseInputTypes.None;
            AddChild(CardBGImage);

            FlagImage = new ControlImage(Game);
            AddChild(FlagImage);

            ProvinceImage = new ControlImage(Game);
            AddChild(ProvinceImage);

            NameText = new ControlText(Game);
            AddChild(NameText);
            NameText.FontName = "Trebuchet MS";
            NameText.Colour = Color.FromArgb(0, 0, 0);
            NameText.TextAnchor = AnchorPoints.Middle;

            EconomyBG = new ControlImage(Game);
            AddChild(EconomyBG);

            EconomyText2 = new ControlText(Game);
            EconomyBG.AddChild(EconomyText2);
            EconomyText2.FontName = "Trebuchet MS";
            EconomyText2.Colour = Color.FromArgb(72, 30, 27);
            EconomyText2.TextAnchor = AnchorPoints.Middle;
            EconomyText2.MouseInputType = MouseInputTypes.Forced;

            RevoltText = CreateStatText();
            IncomeText = CreateStatText();
            ResearchText = CreateStatText();
            TerrainText = CreateStatText();

            DescriptionText = new ControlText(Game);
            AddChild(DescriptionText);
            DescriptionText.FontName = "Trebuchet MS";
            DescriptionText.Colour = Color.FromArgb(0, 0, 0);
            DescriptionText.TextAnchor = AnchorPoints.TopLeft;
            // Same class of bug as the ticker: scroll chrome at default size stretches until text/layout settles.
            DescriptionText.AdjustTextureCoords = false;
            DescriptionText.ScrollingEnabled = false;
            DescriptionText.AcceptMouseWheel = true;
            DescriptionText.MouseInputType = MouseInputTypes.Forced;

            // Controls with their own tooltip must not bubble: the card's (empty) tooltip would replace theirs.
            foreach (UIControl Control in GetChildren())
            {
                if (Control == RevoltText || Control == IncomeText || Control == ResearchText || Control == TerrainText
                    || Control == FlagImage || Control == NameText) continue;
                Control.BubbleMouseovers = true;
            }
            FlagImage.MouseInputType = MouseInputTypes.Forced;
            NameText.MouseInputType = MouseInputTypes.Forced;

            SpellImages = new List<ControlImage>();
            for (int i = 0; i < 6; i++)
            {
                ControlImage SpellImage = new ControlImage(Game);
                SpellImages.Add(SpellImage);
                AddChild(SpellImage);
            }

            PlagueIcon = new ControlImage(Game);
            AddChild(PlagueIcon);
            PlagueIcon.SetImageFile("Data\\Images\\HUD\\Info\\plague_icon.png");
            PlagueIcon.SetTooltip(GameText.CreateLocalised("TTPLAGUE"));

            BuildingIcons = new List<ControlImage>();
            BuildingFrames = new List<ControlImage>();
            for (int i = 0; i < MaxBuildingSlots; i++)
            {
                ControlImage Icon = new ControlImage(Game);
                Icon.MouseInputType = MouseInputTypes.Forced;
                Icon.Visible = false;
                Icon.SetBounds(0, 0, 1, 1, UIUnits.PixelScaled);
                AddChild(Icon);
                BuildingIcons.Add(Icon);

                ControlImage Frame = new ControlImage(Game);
                Frame.MouseInputType = MouseInputTypes.None;
                Frame.SetImageFile("Data\\Images\\HUD\\Info\\building_built.png");
                Frame.Visible = false;
                Frame.SetBounds(0, 0, 1, 1, UIUnits.PixelScaled);
                AddChild(Frame);
                BuildingFrames.Add(Frame);
            }

            OnVisibleChanged += ControlProvinceCard_OnVisibleChanged;
        }

        void ControlProvinceCard_OnVisibleChanged(UIControl Control)
        {
            if (Visible && CurrentProvince != null) SetControlPositions();
        }

        private ControlText CreateStatText()
        {
            ControlText Text = new ControlText(Game);
            AddChild(Text);
            Text.FontName = "Trebuchet MS";
            Text.Colour = Color.FromArgb(0, 0, 0);
            Text.TextAnchor = AnchorPoints.TopLeft;
            Text.ScrollingEnabled = false;
            Text.MouseInputType = MouseInputTypes.Forced;
            return Text;
        }

        private void SetControlPositions()
        {
            CardBGImage.SetBounds(0, 0, Sprite.Bounds.Width, Sprite.Bounds.Height, UIUnits.PixelScaled);

            FlagImage.SetBounds(Sprite.Bounds.Width * 0.751f, Sprite.Bounds.Height * 0.056f, Sprite.Bounds.Width * 0.143f, Sprite.Bounds.Height * 0.074f, UIUnits.PixelScaled);
            ProvinceImage.SetBounds(Sprite.Bounds.Width * 0.143f, Sprite.Bounds.Height * 0.153f, Sprite.Bounds.Width * 0.713f, Sprite.Bounds.Height * 0.282f, UIUnits.PixelScaled);

            NameText.SetBounds(Sprite.Bounds.Width * 0.102f, Sprite.Bounds.Height * 0.054f, Sprite.Bounds.Width * 0.645f, Sprite.Bounds.Height * 0.077f, UIUnits.PixelScaled);
            NameText.SetFontSize((int)(NameText.Sprite.Bounds.Height * 0.5f), UIUnits.PixelScaled);

            EconomyBG.SetBounds(Sprite.Bounds.Width * 0.05f, Sprite.Bounds.Width * 0.05f, Sprite.Bounds.Height * 0.060f, Sprite.Bounds.Height * 0.060f, UIUnits.PixelScaled);

            EconomyText2.SetBounds(0, 0, Sprite.Bounds.Height * 0.060f, Sprite.Bounds.Height * 0.060f, UIUnits.PixelScaled);
            EconomyText2.SetFontSize((int)(EconomyText2.Sprite.Bounds.Height * 0.75f), UIUnits.PixelScaled);

            for (int i = 0; i < 6; i++)
            {
                float XDist = 0.118f;
                float XPos = 0.143f + (i * XDist);

                SpellImages[i].SetBounds(Sprite.Bounds.Width * XPos, Sprite.Bounds.Height * 0.153f, Sprite.Bounds.Width * 0.118f, Sprite.Bounds.Height * 0.072f, UIUnits.PixelScaled);
            }

            PlagueIcon.SetBounds(Sprite.Bounds.Width * 0.143f, Sprite.Bounds.Height * 0.363f, Sprite.Bounds.Width * 0.118f, Sprite.Bounds.Height * 0.072f, UIUnits.PixelScaled);

            LayoutTextPanel();
        }

        // Building row at the top of the text panel; the stat lines and description flow below it.
        private void LayoutTextPanel()
        {
            if (BuildingIcons == null) return;

            float W = Sprite.Bounds.Width;
            float H = Sprite.Bounds.Height;
            // After load the card can briefly report 0-size; applying slots then leaves icons at default stretch.
            if (W < 32f || H < 32f) return;

            float Left = W * 0.115f;
            float Width = W * 0.792f;
            float LineH = H * 0.038f;
            float Y = H * 0.515f;

            int Slots = Math.Max(0, Math.Min(MaxBuildingSlots, BuildingSlotCount));
            if (Slots > 0)
            {
                float Step = Math.Min(W * 0.125f, Width / Slots);
                float SlotW = Step * 0.94f;
                float SlotH = SlotW * (37f / 42f);
                float RowY = H * 0.523f;
                float X = Left;

                for (int i = 0; i < MaxBuildingSlots; i++)
                {
                    if (i < Slots)
                    {
                        BuildingIcons[i].SetBounds(X, RowY, SlotW, SlotH, UIUnits.PixelScaled);
                        BuildingFrames[i].SetBounds(X, RowY, SlotW, SlotH, UIUnits.PixelScaled);
                        X += Step;
                    }
                    else
                    {
                        BuildingIcons[i].SetBounds(0, 0, 1, 1, UIUnits.PixelScaled);
                        BuildingFrames[i].SetBounds(0, 0, 1, 1, UIUnits.PixelScaled);
                    }
                }

                Y = RowY + SlotH + H * 0.006f;
            }
            else
            {
                for (int i = 0; i < MaxBuildingSlots; i++)
                {
                    BuildingIcons[i].SetBounds(0, 0, 1, 1, UIUnits.PixelScaled);
                    BuildingFrames[i].SetBounds(0, 0, 1, 1, UIUnits.PixelScaled);
                }
            }

            RevoltText.SetBounds(Left, Y, Width, LineH, UIUnits.PixelScaled);
            Y += LineH;
            IncomeText.SetBounds(Left, Y, Width, LineH, UIUnits.PixelScaled);
            Y += LineH;
            ResearchText.SetBounds(Left, Y, Width, LineH, UIUnits.PixelScaled);
            Y += LineH;
            TerrainText.SetBounds(Left, Y, Width, LineH, UIUnits.PixelScaled);
            Y += LineH + H * 0.015f;
            float DescH = Math.Max(LineH, H * 0.948f - Y);
            DescriptionText.SetBounds(Left, Y, Width, DescH, UIUnits.PixelScaled);

            int FontSize = (int)(H * 0.383f * 0.075f);
            RevoltText.SetFontSize(FontSize, UIUnits.PixelScaled);
            IncomeText.SetFontSize(FontSize, UIUnits.PixelScaled);
            ResearchText.SetFontSize(FontSize, UIUnits.PixelScaled);
            TerrainText.SetFontSize(FontSize, UIUnits.PixelScaled);
            DescriptionText.SetFontSize(FontSize, UIUnits.PixelScaled);

            UpdateDescriptionScrolling();
        }

        private void UpdateDescriptionScrolling()
        {
            if (DescriptionText == null) return;

            bool HasText = CurrentProvince != null
                && !string.IsNullOrEmpty(CurrentProvince.Description)
                && CurrentProvince.Description != "0";

            if (!HasText)
            {
                DescriptionText.ScrollingEnabled = false;
                return;
            }

            // Only reserve scrollbar chrome when the flavour text actually overflows.
            float TextH = DescriptionText.GetTextHeight();
            float BoxH = DescriptionText.Sprite.Bounds.Height;
            DescriptionText.ScrollingEnabled = TextH > BoxH + 1f;
        }

        private void UpdateBuildings(WorkingProvince Province)
        {
            // Construction order is arbitrary; show lowest tier first, then by name.
            List<BuildingEffect> Built = Province.Buildings
                .OrderBy(b => b.Data.Tier)
                .ThenBy(b => b.Data.DisplayName)
                .Take(MaxBuildingSlots)
                .ToList();
            BuildingSlotCount = Math.Min(MaxBuildingSlots, Math.Max(Built.Count, Province.CurrentEconomy));
            bool PlayerOwned = Province.OwnerRealm == Province.Game.PlayerRealm;

            for (int i = 0; i < MaxBuildingSlots; i++)
            {
                ControlImage Icon = BuildingIcons[i];
                ControlImage Frame = BuildingFrames[i];

                if (i < Built.Count)
                {
                    BuildingEffect Building = Built[i];
                    Icon.SetImageFile("Data\\Images\\HUD\\Buildings\\" + Building.Data.ArtName + "_normal.png");

                    List<GameText> TT = new List<GameText>();
                    TT.Add(GameText.CreateLocalised(Building.Data.DisplayName));
                    TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                    TT.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));
                    TT.Add(GameText.CreateLocalised(Building.Data.DisplayDesc));
                    Icon.SetTooltip(TT);

                    Icon.Visible = true;
                    Frame.Visible = true;
                }
                else if (i < BuildingSlotCount)
                {
                    Icon.SetImageFile("Data\\Images\\HUD\\Info\\building_planned.png");
                    if (PlayerOwned) Icon.SetTooltip(GameText.CreateLocalised("TT_PROVINCE_CANBUILD", Province.MaxBuildableTier()));
                    else Icon.SetTooltip(GameText.CreateLocalised("PROVCARD_EMPTYSLOT"));

                    Icon.Visible = true;
                    Frame.Visible = false;
                }
                else
                {
                    Icon.Visible = false;
                    Frame.Visible = false;
                }
            }

            LayoutTextPanel();
        }

        // Landmark descriptions hardcode the base value as an "RP  22" line; the live value has its own line now.
        private GameText GetFlavourText(WorkingProvince Province)
        {
            string Text = GameText.CreateLocalised(Province.Description).GetActualText(Game);
            Text = Regex.Replace(Text, @"^[ \t]*RP:?[ \t]*\d+[ \t]*\r?\n?", "", RegexOptions.Multiline);
            return GameText.CreateFromLiteral(Text);
        }

        public void SetProvince(WorkingProvince Province)
        {
            if (CurrentProvince != null) CurrentProvince.SpellEffects.SpellsChanged -= SpellEffects_SpellsChanged;
            Province.SpellEffects.SpellsChanged += SpellEffects_SpellsChanged;

            this.CurrentProvince = Province;

            switch (Province.OwnerRealm.Panel)
            {
                case SovereigntyTK.Game.Data.SpellSchools.Death:
                    {
                        CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_death.png");
                        EconomyBG.SetImageFile("Data\\Images\\HUD\\Info\\economyvalue_death.png");
                        break;
                    }
                case SovereigntyTK.Game.Data.SpellSchools.Illusion:
                    {
                        CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_illusion.png");
                        EconomyBG.SetImageFile("Data\\Images\\HUD\\Info\\economyvalue_illusion.png");
                        break;
                    }
                case SovereigntyTK.Game.Data.SpellSchools.Nature:
                    {
                        CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_nature.png");
                        EconomyBG.SetImageFile("Data\\Images\\HUD\\Info\\economyvalue_nature.png");
                        break;
                    }
                case SovereigntyTK.Game.Data.SpellSchools.War:
                    {
                        CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_war.png");
                        EconomyBG.SetImageFile("Data\\Images\\HUD\\Info\\economyvalue_war.png");
                        break;
                    }
            }

            FlagImage.SetImageFile("Data\\Images\\HUD\\flags\\" + Province.OwnerRealm.FlagFilename + ".png");
            ProvinceImage.SetImageFile("Data\\Images\\HUD\\landmarks\\" + Province.ImageFile + ".png");

            NameText.SetTextData(GameText.CreateLocalised(Province.DisplayName));
            List<GameText> OwnerTip = ProvinceOwnerTooltip.BuildTooltip(Province);
            FlagImage.SetTooltip(OwnerTip);
            NameText.SetTooltip(OwnerTip);

            string IconName = Province.CurrentEconomy + ".png";
            
            EconomyText2.Visible = true;

            EconomyText2.SetTextData(GameText.CreateLocalised("FORMAT_NUMBER", Province.CurrentEconomy));

            UpdateBuildings(Province);

            int Income = Province.Game.EconomyController.GetProvinceIncome(Province, Province.OwnerRealm.CapitolProvince, 1f);

            List<GameText> IncomeTip = ProvinceIncomeTooltip.BuildTooltip(Province);
            EconomyText2.SetTooltip(IncomeTip);

            if (Province.IsCapitol)
            {
                RevoltText.SetTextData(GameText.CreateLocalised("REVOLT_CHANCE_CAPITAL", Province.RevoltChance.GetValue()));
            }
            else
            {
                int RevoltNow = Province.RevoltChance.GetValue();
                int RevoltBase = ProvinceRevoltDisplay.GetChanceWithoutArmy(Province);
                RevoltText.SetTextData(GameText.CreateLocalised("REVOLT_CHANCE_BASE", RevoltNow, RevoltBase));
            }
            RevoltText.SetTooltip(Province.RevoltChance.GetBreakdown());

            IncomeText.SetTextData(GameText.CreateLocalised("PROVCARD_INCOME", Income, Province.BaseIncome.GetValue()));
            IncomeText.SetTooltip(IncomeTip);

            ResearchText.SetTextData(GameText.CreateLocalised("PROVCARD_RESEARCH", Province.ResearchPoints.GetValue()));
            ResearchText.SetTooltip(ProvinceResearchTooltip.BuildTooltip(Province));

            GameText Terrain = GameText.CreateLocalised("PROVCARD_TERRAIN");
            Terrain.AddChildText(GameText.CreateLocalised(Province.Terrain.DisplayName));
            TerrainText.SetTextData(Terrain);
            TerrainText.SetTooltip(ProvinceTerrainTooltip.BuildTooltip(Province));

            if (Province.Description != "" && Province.Description != "0") DescriptionText.SetTextData(GetFlavourText(Province));
            else DescriptionText.SetTextData((GameText)null);

            PlagueIcon.Visible = Province.PlagueTurns > 0;

            UpdateSpells();

            // Relayout after all content is set — catches post-load frames where the card size was not ready yet.
            SetControlPositions();
        }

        void SpellEffects_SpellsChanged()
        {
            UpdateSpells();
        }

        public void UpdateSpells()
        {
            for (int i = 0; i < 6; i++)
            {
                if (i >= CurrentProvince.SpellEffects.ActiveSpells.Count) SpellImages[i].Visible = false;
                else
                {
                    SpellEffect Spell = CurrentProvince.SpellEffects.ActiveSpells[i];

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

        protected override void ReclaculateBounds()
        {
            RectangleF OldBounds = Sprite.Bounds;

            base.ReclaculateBounds();

            if (CardBGImage == null) return;
            if (OldBounds.Size == Sprite.Bounds.Size && OldBounds.Width >= 32f) return;

            // Relayout on size change, and also when growing out of a 0-size post-load frame.
            SetControlPositions();
        }
    }
}
