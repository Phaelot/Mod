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
    public class Ranger: CardEffect
    {
        public Ranger(TacticalBattleController Battle, WorkingHero Hero)
            : base("Wood Lore", Hero, Battle)
        {

        }

        public override List<CardTargetData> GetTargetData()
        {
            List<CardTargetData> Result = new List<CardTargetData>();

            CardTargetData Target1 = new CardTargetData();
            Target1.TargetType = CardTargetTypes.Tile;
            Target1.TargetDescription = GameText.CreateLocalised("CARD_FRIEND_FORESTER");
            Result.Add(Target1);

            return Result;
        }

        public override bool TileTargetValid(Point Tile, int TargetIndex)
        {
            BattleTile t = Battle.Map.GetTile(Tile);
            if (t == null) return false;
            WorkingUnit Unit = t.Unit;

            if (Unit == null) return false;
            if (Unit.OwnerStack != Battle.ActiveStack) return false;
            if (Unit.HasAnyNamedFlag("Forester") == false) return false;

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

            UnitFlag Flag = new Evasion();
            Flag.TurnCount = 100;
            Unit.GrantFlag(Flag);

            if (MapTile.Terrain.BaseType.IsAnyType("old forest", "lt forest"))
            {
                Unit.ResetBattleMoves();

                UnitFlag Flag2 = new Mobility();
                Flag2.TurnCount = 100;
                Unit.GrantFlag(Flag2);

                Unit.ApplyHealing(30, false, null);
            }
        }

        public override int GetTargetWeight(Point Target)
        {
            BattleTile MapTile = Battle.Map.GetTile(Target);
            if (MapTile == null) return 0;

            WorkingUnit Unit = MapTile.Unit;
            if (Unit == null) return 0;

            return 100 - Unit.Health;
        }

        public override int GetAICastChance()
        {
            foreach (WorkingUnit Unit in Battle.ActiveStack.Units)
            {
                if (Unit.Health < 80) return 100;
            }

            return 0;
        }

        public override CastTimes GetAICastTime()
        {
            return CastTimes.TurnEnd;
        }
    }
}
