using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Game.ActiveGameData.Flags;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.Game.ActiveGameData.Cards
{
    public class Spymaster: CardEffect
    {
        public Spymaster(TacticalBattleController Battle, WorkingHero Hero)
            : base("Poison Vial", Hero, Battle)
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
            BattleTile MapTile = Battle.Map.GetTile(Tile);
            if (MapTile == null) return false;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return false;
            if (Unit.OwnerStack != Battle.InactiveStack) return false;

            if (Unit.Class == Data.UnitClasses.Fort) return false;
            if (Unit.MoveType == MoveTypes.Phantom) return false;
            if (Unit.Race == Data.Races.Undead) return false;

            return true;
        }

        public override void CastEffect(List<CardTargetData> Targets)
        {
            base.CastEffect(Targets);

            if (Targets == null || Targets.Count == 0) return;

            BattleTile MapTile = Battle.Map.GetTile(Targets[0].Tile);
            if (MapTile == null) return;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return;
            if (Unit.OwnerStack != Battle.InactiveStack) return;

            UnitFlag Flag = new VenomTarget();
            Flag.TurnCount = 100;
            Unit.GrantFlag(Flag);
        }

        public override int GetTargetWeight(Point Target)
        {
            BattleTile MapTile = Battle.Map.GetTile(Target);
            if (MapTile == null) return 0;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return 0;

            return GetUnitValue(Unit);
        }

        public override int GetAICastChance()
        {
            return 25;
        }

        public int GetUnitValue(WorkingUnit Unit)
        {
            if (Unit.Disabled) return 0;

            int Result = 0;

            Result += Unit.Defence.GetValue();
            Result += Unit.Attack.GetValue();
            Result += Unit.Health / 10;
            Result += Unit.ActiveFlagCount;

            return Result;
        }

        public override CastTimes GetAICastTime()
        {
            return CastTimes.TurnStart;
        }
    }
}
