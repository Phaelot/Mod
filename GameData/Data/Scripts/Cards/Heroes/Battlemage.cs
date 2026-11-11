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
    public class Battlemage: CardEffect
    {
        public Battlemage(TacticalBattleController Battle, WorkingHero Hero)
            : base("Fireball", Hero, Battle)
        {
        }

        public override List<CardTargetData> GetTargetData()
        {
            List<CardTargetData> Result = new List<CardTargetData>();

            CardTargetData Target1 = new CardTargetData();
            Target1.TargetType = CardTargetTypes.Tile;
            Target1.TargetDescription = GameText.CreateLocalised("CARD_UNIT_ENEMY");
            Result.Add(Target1);

            return Result;
        }

        public override bool TileTargetValid(Point Tile, int TargetIndex)
        {
            if (TargetIndex == 0)
            {
                BattleTile MapTile = Battle.Map.GetTile(Tile);
                if (MapTile == null) return false;

                WorkingUnit Unit = MapTile.Unit;
                if (Unit == null) return false;
                if (Unit.OwnerRealmID == Battle.ActiveStack.OwnerID) return false;

                return true;
            }

            return false;
        }

        public override void CastEffect(List<CardTargetData> Targets)
        {
            if (Targets == null || Targets.Count == 0) return;

            BattleTile MapTile = Battle.Map.GetTile(Targets[0].Tile);
            if (MapTile == null) return;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return;

            int Damage = Unit.ApplySimulatedDamage(null, 35, false, false, Data.DamageTypes.War);
            Unit.RemoveSimulatedDamage(Damage);
            Unit.ApplyRealDamage(Damage, Data.DamageTypes.War, false, null, "attack_war");
        }

        public override int GetTargetWeight(Point Target)
        {
            BattleTile MapTile = Battle.Map.GetTile(Target);
            if (MapTile == null) return 0;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return 0;

            if (Unit.HasAnyNamedFlag("Immune_War")) return 0;

            int Result = 2;

            Result += Math.Min(0, Unit.Defence.GetValue() - 2);
            Result += Math.Min(0, Unit.Attack.GetValue() - 2);

            int RemainingLife = Unit.Health;
            RemainingLife -= 35;
            if (Unit.HasAnyNamedFlag("Vulnerable_War")) RemainingLife -= 35;

            if (RemainingLife <= 20)
            {
                Result += 2;
                if (Unit.HasAnyNamedFlag("Suicide")) Result += 2;
            }

            if (Unit.HasAnyNamedFlag("Vulnerable_War")) Result *= 2;

            // The AI picks targets by weighted random over these values; negatives corrupt the pick.
            return Math.Max(0, Result);
        }

        public override int GetAICastChance()
        {
            return 30;
        }

        public override CastTimes GetAICastTime()
        {
            return CastTimes.TurnStart;
        }
    }
}
