using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using System;
using System.Reflection;

namespace SovereigntyTK.UI
{
    /// <summary>
    /// Revolt chance helpers for the province card: current (clamped) vs base without army.
    /// </summary>
    public static class ProvinceRevoltDisplay
    {
        private static readonly FieldInfo ModifierField = typeof(ProvinceStat).GetField("OnRequestModifier", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int GetArmySuppression(WorkingProvince Province)
        {
            if (Province.LandNode == null || Province.LandNode.CurrentStack == null) return 0;

            WorkingStack Stack = Province.LandNode.CurrentStack;
            int Suppression = 0;
            foreach (WorkingUnit Unit in Stack.Units)
            {
                Suppression += GetUnitRevoltReduction(Unit);
            }
            if (Stack.Hero != null) Suppression += 3;
            return Suppression;
        }

        // Mirrors WorkingUnit.GetRevoltReduction (internal in the engine).
        private static int GetUnitRevoltReduction(WorkingUnit Unit)
        {
            int Amount = 1;
            if (Unit.Rank == UnitRanks.Elite) Amount = 2;
            if (Unit.Rank == UnitRanks.Unique) Amount = 3;
            if (Unit.HasStatus("Police", new object[0])) Amount *= 2;
            return Amount;
        }

        public static int GetUnclampedChance(WorkingProvince Province)
        {
            int Value = Province.RevoltChance.BaseValue;
            ProvinceStatModifierDelegate Handlers = null;
            if (ModifierField != null) Handlers = ModifierField.GetValue(Province.RevoltChance) as ProvinceStatModifierDelegate;
            if (Handlers != null)
            {
                try
                {
                    Handlers(Province, ref Value);
                }
                catch (Exception)
                {
                    return Province.RevoltChance.GetValue();
                }
            }
            return Value;
        }

        /// <summary>
        /// Revolt chance if the garrison (units + hero) left — everything else kept.
        /// </summary>
        public static int GetChanceWithoutArmy(WorkingProvince Province)
        {
            return GetUnclampedChance(Province) + GetArmySuppression(Province);
        }
    }
}
