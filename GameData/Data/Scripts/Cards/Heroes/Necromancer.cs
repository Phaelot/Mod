using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.UI.Text;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.Game.ActiveGameData.Cards
{
    public class Necromancer: CardEffect
    {
        public Necromancer(TacticalBattleController Battle, WorkingHero Hero)
            : base("Blood Ritual", Hero, Battle)
        {
        }

        public override List<CardTargetData> GetTargetData()
        {
            List<CardTargetData> Result = new List<CardTargetData>();

            CardTargetData Target1 = new CardTargetData();
            Target1.TargetType = CardTargetTypes.Tile;
            Target1.TargetDescription = GameText.CreateLocalised("CARD_ENEMY_NOMEDAL");
            Result.Add(Target1);

            CardTargetData Target2 = new CardTargetData();
            Target2.TargetType = CardTargetTypes.Tile;
            Target2.TargetDescription = GameText.CreateLocalised("CARD_FRIEND_NOMEDAL");
            Result.Add(Target2);

            return Result;
        }

        public override bool TileTargetValid(Point Tile, int TargetIndex)
        {
            BattleTile MapTile = Battle.Map.GetTile(Tile);
            if (MapTile == null) return false;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return false;

            if (Unit.Rank != Data.UnitRanks.Standard && Unit.Rank != Data.UnitRanks.Mercenary) return false;
            if (Unit.Medals > 0) return false;

            if (TargetIndex == 0 && Unit.OwnerRealm == Battle.ActiveStack.Owner) return false;
            if (TargetIndex == 1 && Unit.OwnerRealm == Battle.InactiveStack.Owner) return false;

            return true;
        }

        public override void CastEffect(List<CardTargetData> Targets)
        {
            base.CastEffect(Targets);

            if (Targets == null || Targets.Count < 2) return;

            BattleTile TileA = Battle.Map.GetTile(Targets[0].Tile);
            BattleTile TileB = Battle.Map.GetTile(Targets[1].Tile);
            if (TileA == null || TileB == null) return;

            WorkingUnit UnitA = TileA.Unit;
            WorkingUnit UnitB = TileB.Unit;
            if (UnitA == null || UnitB == null) return;

            UnitA.ApplyRealDamage(100, Data.DamageTypes.None, false, null, "attack_death");
            UnitB.ApplyRealDamage(100, Data.DamageTypes.None, false, null, "attack_death");
        }

        public override int GetTargetWeight(Point Target)
        {
            BattleTile MapTile = Battle.Map.GetTile(Target);
            if (MapTile == null) return 0;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return 0;

            if (Unit.OwnerStack == Battle.ActiveStack) return 1000 - GetUnitValue(Unit);
            if (Unit.OwnerStack == Battle.InactiveStack) return GetUnitValue(Unit);

            return 0;
        }

        public override int GetAICastChance()
        {
            //do not kill last unit, ever
            if (Battle.ActiveStack.Units.Count(x => x.Disabled == false) == 1) return 0;

            KeyValuePair<WorkingUnit, int> BestEnemy = GetBestEnemyUnit();
            KeyValuePair<WorkingUnit, int> WorstFriendly = GetWorstFriendlyUnit();

            if (BestEnemy.Key == null || WorstFriendly.Key == null) return 0;

            //only cast if your weakest unit is damaged enough
            if (WorstFriendly.Key.Health > 33) return 0;

            float Ratio = (float)BestEnemy.Value / (float)Math.Max(1, WorstFriendly.Value);

            if (Ratio >= 3) return 100;
            else if (Ratio >= 2) return 70;
            else return 0;
        }

        public KeyValuePair<WorkingUnit, int> GetBestEnemyUnit()
        {
            int HighValue = int.MinValue;
            WorkingUnit HighUnit = null;

            foreach (WorkingUnit Unit in Battle.InactiveStack.Units)
            {
                if (!CanBeSacrificed(Unit)) continue;

                int Value = GetUnitValue(Unit);

                if (Value > HighValue)
                {
                    HighValue = Value;
                    HighUnit = Unit;
                }
            }

            return new KeyValuePair<WorkingUnit, int>(HighUnit, HighValue);
        }

        public KeyValuePair<WorkingUnit, int> GetWorstFriendlyUnit()
        {
            int LowValue = int.MaxValue;
            WorkingUnit LowUnit = null;

            foreach (WorkingUnit Unit in Battle.ActiveStack.Units)
            {
                if (!CanBeSacrificed(Unit)) continue;

                int Value = GetUnitValue(Unit);

                if (Value < LowValue)
                {
                    LowValue = Value;
                    LowUnit = Unit;
                }
            }

            return new KeyValuePair<WorkingUnit, int>(LowUnit, LowValue);
        }

        // Same unit rules as TileTargetValid, so the cast decision only weighs legal targets.
        private bool CanBeSacrificed(WorkingUnit Unit)
        {
            if (Unit.Disabled || Unit.Health <= 0) return false;
            if (Unit.Rank != Data.UnitRanks.Standard && Unit.Rank != Data.UnitRanks.Mercenary) return false;
            return Unit.Medals == 0;
        }

        public int GetUnitValue(WorkingUnit Unit)
        {
            int Result = 0;

            Result += Unit.Defence.GetValue();
            Result += Unit.Attack.GetValue();
            Result += Unit.Health / 10;
            Result += Unit.ActiveFlagCount;

            return Result;
        }

        public override CastTimes GetAICastTime()
        {
            return CastTimes.TurnEnd;
        }
    }
}
