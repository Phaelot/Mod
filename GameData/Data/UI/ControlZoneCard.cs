using SovereigntyTK.Game.ActiveGameData;
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
    public class ControlZoneCard: UIControl
    {
        ControlImage CardBGImage;
        private WorkingZone CurrentZone;

        ControlImage FlagImage;
        ControlImage ProvinceImage;

        ControlText NameText;
        ControlText DescriptionText;

        List<ControlImage> SpellImages;

        public ControlZoneCard(GameBase Game)
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
            FlagImage.MouseInputType = MouseInputTypes.Forced;
            AddChild(FlagImage);

            ProvinceImage = new ControlImage(Game);
            AddChild(ProvinceImage);

            NameText = new ControlText(Game);
            AddChild(NameText);
            NameText.FontName = "Trebuchet MS";
            NameText.Colour = Color.FromArgb(0, 0, 0);
            NameText.TextAnchor = AnchorPoints.Middle;
            NameText.MouseInputType = MouseInputTypes.Forced;

            DescriptionText = new ControlText(Game);
            AddChild(DescriptionText);
            DescriptionText.FontName = "Trebuchet MS";
            DescriptionText.Colour = Color.FromArgb(0, 0, 0);
            DescriptionText.TextAnchor = AnchorPoints.TopLeft;

            foreach (UIControl Control in GetChildren())
            {
                if (Control == FlagImage || Control == NameText) continue;
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

        private void SetControlPositions()
        {
            CardBGImage.SetBounds(0, 0, Sprite.Bounds.Width, Sprite.Bounds.Height, UIUnits.PixelScaled);

            FlagImage.SetBounds(Sprite.Bounds.Width * 0.751f, Sprite.Bounds.Height * 0.056f, Sprite.Bounds.Width * 0.143f, Sprite.Bounds.Height * 0.074f, UIUnits.PixelScaled);
            ProvinceImage.SetBounds(Sprite.Bounds.Width * 0.143f, Sprite.Bounds.Height * 0.153f, Sprite.Bounds.Width * 0.713f, Sprite.Bounds.Height * 0.282f, UIUnits.PixelScaled);

            NameText.SetBounds(Sprite.Bounds.Width * 0.102f, Sprite.Bounds.Height * 0.054f, Sprite.Bounds.Width * 0.645f, Sprite.Bounds.Height * 0.077f, UIUnits.PixelScaled);
            NameText.SetFontSize((int)(NameText.Sprite.Bounds.Height * 0.5f), UIUnits.PixelScaled);

            DescriptionText.SetBounds(Sprite.Bounds.Width * 0.106f, Sprite.Bounds.Height * 0.515f, Sprite.Bounds.Width * 0.792f, Sprite.Bounds.Height * 0.433f, UIUnits.PixelScaled);
            DescriptionText.SetFontSize((int)(DescriptionText.Sprite.Bounds.Height * 0.075f), UIUnits.PixelScaled);

            for (int i = 0; i < 6; i++)
            {
                float XDist = 0.118f;
                float XPos = 0.143f + (i * XDist);

                SpellImages[i].SetBounds(Sprite.Bounds.Width * XPos, Sprite.Bounds.Height * 0.153f, Sprite.Bounds.Width * 0.118f, Sprite.Bounds.Height * 0.072f, UIUnits.PixelScaled);
            }
        }

        public void SetZone(WorkingZone Zone)
        {
            if (CurrentZone != null) CurrentZone.SpellEffects.SpellsChanged -= SpellEffects_SpellsChanged;
            Zone.SpellEffects.SpellsChanged += SpellEffects_SpellsChanged;

            this.CurrentZone = Zone;

            switch (Zone.Game.PlayerRealm.Panel)
            {
                case SovereigntyTK.Game.Data.SpellSchools.Death: CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_death.png"); break;
                case SovereigntyTK.Game.Data.SpellSchools.Illusion: CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_illusion.png"); break;
                case SovereigntyTK.Game.Data.SpellSchools.Nature: CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_nature.png"); break;
                case SovereigntyTK.Game.Data.SpellSchools.War: CardBGImage.SetImageFile("Data\\Images\\HUD\\Info\\provincecard_war.png"); break;
            }

            ProvinceImage.SetImageFile("Data\\Images\\HUD\\landmarks\\" + Zone.ImageFile + ".png");

            FlagImage.SetImageFile("Data\\Images\\HUD\\Flags\\seaflag.png");
            FlagImage.SetTooltip(GameText.CreateLocalised("TT_SEA_ZONE"));

            NameText.SetTextData(GameText.CreateLocalised(Zone.DisplayName));
            NameText.SetTooltip(GameText.CreateLocalised("TT_SEA_ZONE"));

            DescriptionText.SetTextData((GameText)null);
            UpdateSpells();
        }

        void SpellEffects_SpellsChanged()
        {
            UpdateSpells();
        }

        public void UpdateSpells()
        {
            for (int i = 0; i < 6; i++)
            {
                if (i >= CurrentZone.SpellEffects.ActiveSpells.Count) SpellImages[i].Visible = false;
                else
                {
                    SpellEffect Spell = CurrentZone.SpellEffects.ActiveSpells[i];

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

            if (OldBounds.Size == Sprite.Bounds.Size) return;
            if (CardBGImage == null) return;

            //if the size of the control has changed
            //the internal controls need to have their positions/sizes updated to reflect this
            SetControlPositions();
        }
    }
}
