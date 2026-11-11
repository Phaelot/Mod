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

namespace SovereigntyTK.UI
{
    public class StackPanel: ScriptBase
    {
        WorkingStack Stack;
        ControlImage BG;

        List<ControlUnitIcon> UnitIcons;
        ControlUnitIcon HeroIcon;

        public void FormLoaded(UIControl Control)
        {
            GameSystem.RegisterEvent(GameSystem_OnScriptEvent, "StackSelectionChanged");
            GameSystem.RegisterEvent(HandleUnitDisbanded, "UnitDisbanded");
            GameSystem.RegisterEvent(HandleHeroDisbanded, "HeroDisbanded");
            GameSystem.RegisterEvent(HandleUnitPromoted, "UnitPromoted");
            BG = GameForm.GetControlByName<ControlImage>("StackBG");

            int Xpos = 34;
            int Ypos = 5;
            UnitIcons = new List<ControlUnitIcon>();

            for (int i = 0; i < 20; i++)
            {
                ControlUnitIcon Icon = new ControlUnitIcon(GameSystem);
                Icon.SetSize(48, 48, UIUnits.Pixel);
                Icon.SetPosition(Xpos, Ypos, UIUnits.Pixel);
                Icon.Visible = false;
                Icon.MouseInputType = MouseInputTypes.Forced;
                Icon.OnMouseEnter += Icon_OnMouseEnter;
                Icon.OnMouseLeave += Icon_OnMouseLeave;
                Icon.OnClick += Icon_OnClick;
                Icon.OnDoubleClick += Icon_OnDoubleClick;
                BG.AddChild(Icon);
                UnitIcons.Add(Icon);

                Xpos += 52;
                if (Xpos > 242)
                {
                    Xpos = 34;
                    Ypos += 52;
                }
            }

            HeroIcon = new ControlUnitIcon(GameSystem);
            HeroIcon.SetSize(53, 53, UIUnits.Pixel);
            HeroIcon.SetPosition(37, 217, UIUnits.Pixel);
            HeroIcon.Visible = false;
            HeroIcon.MouseInputType = MouseInputTypes.Forced;
            HeroIcon.OnMouseEnter += Icon_OnMouseEnter;
            HeroIcon.OnMouseLeave += Icon_OnMouseLeave;
            HeroIcon.OnClick += Icon_OnClick;
            BG.AddChild(HeroIcon);
        }

        private void HandleHeroDisbanded(string EventName, params object[] Args)
        {
            if (BG.Visible) UpdateStack();
        }

        private void HandleUnitPromoted(string EventName, params object[] Args)
        {
            if (BG.Visible && Stack != null) UpdateStack();
        }

        void Icon_OnDoubleClick(UIControl Control)
        {
            WorkingUnit Unit = (Control as ControlUnitIcon).Unit;
            if (Unit == null) return;
            GameSystem.FireEvent("UnitClicked", Unit);
        }

        void Icon_OnClick(UIControl Control)
        {
            if ((Control as ControlUnitIcon).Unit == null) return;
            if (GameSystem.UIActionAllowed("ToggleUnits") == false) return;
            if (Stack.Owner != GameSystem.CurrentGame.PlayerRealm) return;

            (Control as ControlUnitIcon).Unit.ToggleSelected();
            (Control as ControlUnitIcon).SetUnit((Control as ControlUnitIcon).Unit);

            GameSystem.Map.RefreshMode();
            GameSystem.FireEvent("UnitSelectionToggled");
        }

        void Icon_OnMouseLeave(UIControl Control)
        {
            
        }

        void Icon_OnMouseEnter(UIControl Control)
        {
            if ((Control as ControlUnitIcon).Unit == null) return;
            if ((Control as ControlUnitIcon).Unit is WorkingHero)
            {
                GameSystem.FireEvent("MouseoverHero", (Control as ControlUnitIcon).Unit as WorkingHero);
            }
            else
            {
                GameSystem.FireEvent("MouseoverUnit", (Control as ControlUnitIcon).Unit);
            }
        }

        void GameSystem_OnScriptEvent(string EventName, params object[] Args)
        {
            if (EventName == "StackSelectionChanged")
            {
                Stack = Args[0] as WorkingStack;

                if (Stack == null) BG.Visible = false;
                else
                {
                    if (Stack.Owner == GameSystem.CurrentGame.PlayerRealm)
                    {
                        foreach (WorkingUnit Unit in Stack.Units) Unit.AutoSelect();
                        if (Stack.Hero != null) Stack.Hero.AutoSelect();

                        GameForm.GetControlByName<ControlButton>("SelectAllButton").Visible = true;
                        GameForm.GetControlByName<ControlButton>("SelectNoneButton").Visible = true;
                    }
                    else
                    {
                        foreach (WorkingUnit Unit in Stack.Units) Unit.Selected = true;
                        if (Stack.Hero != null) Stack.Hero.Selected = true;

                        GameForm.GetControlByName<ControlButton>("SelectAllButton").Visible = false;
                        GameForm.GetControlByName<ControlButton>("SelectNoneButton").Visible = false;
                    }

                    BG.Visible = true;
                    UpdateStack();
                }
            }
        }

        public void SelectAllClick(UIControl Control)
        {
            foreach (WorkingUnit Unit in Stack.Units) Unit.AutoSelect();
            if (Stack.Hero != null) Stack.Hero.AutoSelect();
            UpdateStack();
        }

        public void SelectNoneClick(UIControl Control)
        {
            foreach (WorkingUnit Unit in Stack.Units) Unit.Selected = false;
            if (Stack.Hero != null) Stack.Hero.Selected = false;
            UpdateStack();
        }

        private bool UseLimitedEnemyInspection()
        {
            if (Stack == null || Stack.Owner == GameSystem.CurrentGame.PlayerRealm) return false;
            if (GameSystem.Map == null || GameSystem.Map.SimpleFogOfWarEnabled == false) return false;

            WorkingProvince province = Stack.Node == null ? null : Stack.Node.Province;
            if (province != null)
            {
                if (province.OwnerRealm == GameSystem.CurrentGame.PlayerRealm) return false;
                if (province.OwnerRealm != null && province.OwnerRealm.DiplomacyManager.GetRelation(GameSystem.CurrentGame.PlayerRealm) == RelationStates.Alliance) return false;
            }

            return true;
        }

        private int GetIntelRevealCount(int UnitCount)
        {
            if (UnitCount <= 0) return 0;
            return Math.Max(1, (UnitCount + 4) / 5);
        }

        private long GetIntelRevealOrderValue(WorkingUnit Unit)
        {
            int turn = 0;
            if (GameSystem.CurrentGame != null && GameSystem.CurrentGame.TurnController != null)
            {
                turn = GameSystem.CurrentGame.TurnController.TurnNumber;
            }

            long value = (long)Stack.ID * 1103515245L;
            value ^= (long)Unit.ID * 12345L;
            value ^= (long)turn * 2654435761L;
            value ^= ((long)Unit.Class + 1L) * 97531L;
            return value & 0x7fffffffL;
        }

        private HashSet<int> GetLimitedInspectionRevealIDs(List<WorkingUnit> SortedUnits)
        {
            int revealCount = GetIntelRevealCount(SortedUnits.Count);
            HashSet<int> result = new HashSet<int>();

            foreach (WorkingUnit unit in SortedUnits
                .OrderBy(x => GetIntelRevealOrderValue(x))
                .ThenBy(x => x.ID)
                .Take(revealCount))
            {
                result.Add(unit.ID);
            }

            return result;
        }

        private void UpdateStack()
        {
            List<WorkingUnit> SortedUnits = Stack.Units.OrderBy(x => x.Formation).ThenBy(x => x.BaseName).ToList();

            bool limitedInspection = UseLimitedEnemyInspection();
            HashSet<int> revealedIDs = limitedInspection ? GetLimitedInspectionRevealIDs(SortedUnits) : null;

            for (int i = 0; i < 20; i++)
            {
                if (i >= SortedUnits.Count)
                {
                    UnitIcons[i].Visible = false;
                }
                else
                {
                    UnitIcons[i].Visible = true;
                    if (!limitedInspection || revealedIDs.Contains(SortedUnits[i].ID))
                    {
                        UnitIcons[i].SetUnit(SortedUnits[i]);
                    }
                    else
                    {
                        UnitIcons[i].SetUnknownUnit("Unknown unit");
                    }
                }
            }

            if (Stack.Hero == null) HeroIcon.Visible = false;
            else
            {
                HeroIcon.Visible = true;
                HeroIcon.SetUnit(Stack.Hero);
            }

            GameSystem.Map.RefreshMode();
        }

        public override void Dispose()
        {
            GameSystem.UnregisterEvent(GameSystem_OnScriptEvent, "StackSelectionChanged");
            GameSystem.UnregisterEvent(HandleUnitDisbanded, "UnitDisbanded");
            GameSystem.UnregisterEvent(HandleHeroDisbanded, "HeroDisbanded");
            GameSystem.UnregisterEvent(HandleUnitPromoted, "UnitPromoted");
        }

        private void HandleUnitDisbanded(string EventName, params object[] Args)
        {
            if (BG.Visible)
            {
                if (Stack.Units.Count == 0)
                {
                    Stack = null;
                    BG.Visible = false;
                }
                else UpdateStack();
            }
        }
    }
}
