using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.UI.Text;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SovereigntyTK.UI
{
    /// <summary>
    /// Research point breakdown for the province card tooltip.
    /// </summary>
    public static class ProvinceResearchTooltip
    {
        // Buildings and spells only hook ResearchPoints.OnRequestModifier (no breakdown list), so the
        // per-source split is recovered by running the handlers one at a time.
        private static readonly FieldInfo ModifierField = typeof(ProvinceStat).GetField("OnRequestModifier", BindingFlags.Instance | BindingFlags.NonPublic);

        public static List<GameText> BuildTooltip(WorkingProvince Province)
        {
            ProvinceStat Stat = Province.ResearchPoints;

            List<GameText> Tip = new List<GameText>();
            Tip.Add(GameText.CreateLocalised("TT_PROVINCE_RESEARCH"));
            Tip.Add(MakeLine(Stat.BaseValue, GameText.CreateLocalised("RESEARCH_BASE")));

            ProvinceStatModifierDelegate Handlers = null;
            if (ModifierField != null) Handlers = ModifierField.GetValue(Stat) as ProvinceStatModifierDelegate;
            if (Handlers == null) return Tip;

            int Running = Stat.BaseValue;
            int Other = 0;

            foreach (ProvinceStatModifierDelegate Handler in Handlers.GetInvocationList())
            {
                int Before = Running;
                try
                {
                    Handler(Province, ref Running);
                }
                catch (Exception)
                {
                    Running = Before;
                    continue;
                }

                int Delta = Running - Before;
                if (Delta == 0) continue;

                GameText Source = GetSourceName(Handler.Target);
                if (Source == null) Other += Delta;
                else Tip.Add(MakeLine(Delta, Source));
            }

            if (Other != 0) Tip.Add(MakeLine(Other, GameText.CreateLocalised("RESEARCH_OTHER")));

            return Tip;
        }

        private static GameText GetSourceName(object Target)
        {
            BuildingEffect Building = Target as BuildingEffect;
            if (Building != null && Building.Data != null) return GameText.CreateLocalised(Building.Data.DisplayName);

            SpellEffect Spell = Target as SpellEffect;
            if (Spell != null && Spell.SpellData != null) return GameText.CreateLocalised(Spell.SpellData.DisplayName);

            return null;
        }

        private static GameText MakeLine(int Value, GameText Caption)
        {
            GameText Line = GameText.CreateLocalised("FORMAT_PRISONERS", Value);
            Line.AddChildText(Caption);
            return Line;
        }
    }
}
