using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;
using System.Collections.Generic;
using System.Linq;

namespace SovereigntyTK.UI
{
    /// <summary>
    /// Shared income breakdown for province/economy card tooltips (mirrors TopPanel economy list).
    /// </summary>
    public static class ProvinceIncomeTooltip
    {
        public static List<GameText> BuildTooltip(WorkingProvince Province)
        {
            int Income = Province.Game.EconomyController.GetProvinceIncome(Province, Province.OwnerRealm.CapitolProvince, 1f);

            List<GameText> Tip = new List<GameText>();

            GameText Header = GameText.CreateLocalised("TT_BUILDING_ECON");
            Header.AddChildText(GameText.CreateLocalised("FORMAT_PROVINCEINCOME", Income, Province.BaseIncome.GetValue()));
            Tip.Add(Header);
            Tip.Add(GameText.CreateLocalised("FORMAT_NEWLINE"));

            if (Province.Cradle == ArtScienceTypes.Alchemy)
            {
                int CradleValue = 75;
                if (Province.OwnerRealm.MagicData.SpellCast == false) CradleValue = 0;
                Tip.Add(MakeLine(CradleValue, "CRADLE_ALCHEMY"));
            }

            Tip.Add(MakeLine(Province.CurrentEconomy * 150, "TEXT_ECONOMY"));

            foreach (BuildingEffect Building in Province.Buildings)
            {
                int Value = Building.GetGoldIncome();
                if (Value == 0) continue;

                GameText Line = GameText.CreateLocalised("FORMAT_PRISONERS", Value);
                Line.AddChildText(GameText.CreateLocalised(Building.Data.DisplayName));
                Tip.Add(Line);
            }

            foreach (SpellEffect Spell in Province.SpellEffects.ActiveSpells)
            {
                int Value = Spell.GetGoldIncome(Province);
                if (Value == 0) continue;

                GameText Line = GameText.CreateLocalised("FORMAT_PRISONERS", Value);
                Line.AddChildText(GameText.CreateLocalised(Spell.SpellData.DisplayName));
                Tip.Add(Line);
            }

            int Distance;
            int DistanceValue = GetDistancePenalty(Province, Province.OwnerRealm.CapitolProvince, out Distance);
            if (DistanceValue < 0)
            {
                GameText Line = GameText.CreateLocalised("FORMAT_PRISONERS", DistanceValue);
                Line.AddChildText(GetDistanceCaption(Distance));
                Tip.Add(Line);
            }

            return Tip;
        }

        // Same rules as EconomyController.GetProvinceIncome (the engine's GetProvinceDistanceEffect uses a
        // different route and can disagree with the real income). Returns the income lost to distance (<= 0).
        // Distance is provinces/sea zones away after modifiers such as Tax Collector, or -1 with no route.
        public static int GetDistancePenalty(WorkingProvince Province, WorkingProvince Capitol, out int Distance)
        {
            Distance = 0;

            float Value = (float)Province.BaseIncome.GetValue() * ((float)Province.IncomeMultiplier.GetValue() * 0.01f);
            int Undiminished = (int)Value;

            if (Province.HasStatus("IgnoreDistancePenalty")) return 0;
            if (Capitol == null) return 0;

            var Route = Province.Game.PathManager.GetPath(Province.LandNode, Capitol.LandNode, null, false, Province.OwnerRealm, true);
            int Regions = Route.PathPoints.Select(x => x.Node.RegionID).Distinct().Count();

            if (Regions == 0 && !Province.IsCapitol)
            {
                Distance = -1;
                return -Undiminished;
            }

            int Steps = Regions + Province.CapitolDistanceModifier.GetValue();
            Distance = System.Math.Max(0, Steps - 1);

            for (; Steps > 2; Steps--)
            {
                Value *= 0.78f;
            }

            return (int)Value - Undiminished;
        }

        public static GameText GetDistanceCaption(int Distance)
        {
            if (Distance < 0) return GameText.CreateLocalised("TEXT_DISTANCE_NOROUTE");
            return GameText.CreateLocalised("TEXT_DISTANCE_STEPS", Distance);
        }

        private static GameText MakeLine(int Value, string CaptionKey)
        {
            GameText Line = GameText.CreateLocalised("FORMAT_PRISONERS", Value);
            Line.AddChildText(GameText.CreateLocalised(CaptionKey));
            return Line;
        }
    }
}
