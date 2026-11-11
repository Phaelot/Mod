using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SovereigntyTK.Game.Battle;
using SovereigntyTK.UI.Text;
using SovereigntyTK.Game.ActiveGameData.Flags;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.Game.ActiveGameData.Cards
{
    public class Sorcerer: CardEffect
    {
        public Sorcerer(TacticalBattleController Battle, WorkingHero Hero)
            : base("Ethereal Touch", Hero, Battle)
        {
        }

        public override List<CardTargetData> GetTargetData()
        {
            List<CardTargetData> Result = new List<CardTargetData>();

            CardTargetData Target1 = new CardTargetData();
            Target1.TargetType = CardTargetTypes.Tile;
            Target1.TargetDescription = GameText.CreateLocalised("CARD_STD_FRIEND");
            Result.Add(Target1);

            return Result;
        }

        public override bool TileTargetValid(Point Tile, int TargetIndex)
        {
            BattleTile MapTile = Battle.Map.GetTile(Tile);
            if (MapTile == null) return false;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return false;
            if (Unit.OwnerStack != Battle.ActiveStack) return false;
            if (Unit.Rank != Data.UnitRanks.Standard) return false;

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

            UnitFlag Flag = new Phantom();
            Flag.TurnCount = 1;
            Unit.GrantFlag(Flag);
        }

        public override int GetTargetWeight(Point Target)
        {
            BattleTile MapTile = Battle.Map.GetTile(Target);
            if (MapTile == null) return 0;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return 0;

            switch (Unit.Class)
            {
                case UnitClasses.Infantry: return 10;
                case UnitClasses.Archer: return 8;
                case UnitClasses.Siege: return 6;
                case UnitClasses.Cavalry: return 4;
                default: return 0;
            }
        }

        public override int GetAICastChance()
        {
            return 25;
        }

        public override CastTimes GetAICastTime()
        {
            return CastTimes.TurnStart;
        }
    }
}
