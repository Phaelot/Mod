using System;
using SovereigntyTK.Game;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;
using SovereigntyTK.UI.Text;

namespace SovereigntyTK.Utility
{
	public class TooltipFactory
	{
		public GameText GetClassTooltip(UnitClasses Class, bool Ranged)
		{
			switch (Class)
			{
			case UnitClasses.Infantry:
				return GameText.CreateLocalised("CLASS_INFANTRY", new object[0]);
			case UnitClasses.Cavalry:
				return GameText.CreateLocalised("CLASS_CAVALRY", new object[0]);
			case UnitClasses.Archer:
				return GameText.CreateLocalised("CLASS_ARCHER", new object[0]);
			case UnitClasses.Siege:
				return GameText.CreateLocalised("CLASS_SIEGE", new object[0]);
			case UnitClasses.Naval:
				return GameText.CreateLocalised("CLASS_NAVAL", new object[0]);
			case UnitClasses.Fort:
				return GameText.CreateLocalised("CLASS_FORT", new object[0]);
			case UnitClasses.Irregular:
				return GameText.CreateLocalised("CLASS_IRREGULAR", new object[0]);
			}
			return null;
		}

		public GameText GetRankTooltip(UnitRanks Rank)
		{
			switch (Rank)
			{
			case UnitRanks.Standard:
				return GameText.CreateLocalised("UNITSTANDARDTOOLTIP", new object[0]);
			case UnitRanks.Elite:
				return GameText.CreateLocalised("UNITELITETOOLTIP", new object[0]);
			case UnitRanks.Unique:
				return GameText.CreateLocalised("UNITUNIQUETOOLTIP", new object[0]);
			case UnitRanks.Mercenary:
				return GameText.CreateLocalised("UNITMERCENARYTOOLTIP", new object[0]);
			default:
				return null;
			}
		}

		public GameText GetRaceTooltip(Races Race)
		{
			switch (Race)
			{
			case Races.Human:
				return GameText.CreateLocalised("RACE_HUMAN", new object[0]);
			case Races.Elf:
				return GameText.CreateLocalised("RACE_ELF", new object[0]);
			case Races.Dwarf:
				return GameText.CreateLocalised("RACE_DWARF", new object[0]);
			case Races.Undead:
				return GameText.CreateLocalised("RACE_UNDEAD", new object[0]);
			case Races.Orc:
				return GameText.CreateLocalised("RACE_ORC", new object[0]);
			case Races.Monster:
				return GameText.CreateLocalised("RACE_MONSTER", new object[0]);
			case Races.Giant:
				return GameText.CreateLocalised("RACE_GIANT", new object[0]);
			case Races.Dragon:
				return GameText.CreateLocalised("RACE_DRAGON", new object[0]);
			case Races.Outcast:
				return GameText.CreateLocalised("RACE_OUTCAST", new object[0]);
			default:
				return null;
			}
		}

		public GameText GetGroupTooltip(bool IsGroup)
		{
			if (IsGroup)
			{
				return GameText.CreateLocalised("UNITGROUPTOOLTIP", new object[0]);
			}
			return GameText.CreateLocalised("UNITSINGLETOOLTIP", new object[0]);
		}

		public GameText GetUnitPurchaseStatusTooltip(UnitTrainStates Status)
		{
			switch (Status)
			{
			case UnitTrainStates.OK:
				return null;
			case UnitTrainStates.TooManyElites:
				return GameText.CreateLocalised("TT_PURCHASE_ELITE", new object[0]);
			case UnitTrainStates.TooManyUniques:
				return GameText.CreateLocalised("TT_PURCHASE_UNIQUE", new object[0]);
			case UnitTrainStates.QueueFull:
				return GameText.CreateLocalised("TT_PURCHASE_FULL", new object[0]);
			case UnitTrainStates.CannotAfford:
				return GameText.CreateLocalised("TT_PURCHASE_NOGOLD", new object[0]);
			case UnitTrainStates.NoResources:
				return GameText.CreateLocalised("TT_PURCHASE_NORESOURCE", new object[0]);
			case UnitTrainStates.NoHarbour:
				return GameText.CreateLocalised("TT_PURCHASE_HARBOUR", new object[0]);
			case UnitTrainStates.TutorialBlock:
				return null;
			case UnitTrainStates.TooManyAux:
				return GameText.CreateLocalised("TT_PURCHASE_AUX", new object[0]);
			default:
				return null;
			}
		}

		public GameText GetUnitMoveStatusText(UnitMoveResult Status)
		{
			switch (Status)
			{
			case UnitMoveResult.NotEnoughMoves:
				return GameText.CreateLocalised("TT_MOVE_MOVES", new object[0]);
			case UnitMoveResult.InvalidTerrain:
				return GameText.CreateLocalised("TT_MOVE_TERRAIN", new object[0]);
			case UnitMoveResult.AttackNotPossible:
				return GameText.CreateLocalised("TT_MOVE_RESTRICTED", new object[0]);
			case UnitMoveResult.NoUnitsSelected:
				return GameText.CreateLocalised("TT_MOVE_NOUNITS", new object[0]);
			case UnitMoveResult.ProvinceLocked:
				return GameText.CreateLocalised("TT_MOVE_NOIN", new object[0]);
			case UnitMoveResult.ProvinceLocked2:
				return GameText.CreateLocalised("TT_MOVE_NOOUT", new object[0]);
			case UnitMoveResult.ProvinceFull:
				return GameText.CreateLocalised("TT_MOVE_FULL", new object[0]);
			case UnitMoveResult.LandmarkBlocked:
				return GameText.CreateLocalised("TT_MOVE_LANDMARK", new object[0]);
			case UnitMoveResult.NotOwned:
				return GameText.CreateLocalised("TT_MOVE_NOTOWNED", new object[0]);
			case UnitMoveResult.FlagBlocked:
				return GameText.CreateLocalised("TT_MOVE_FLAG", new object[0]);
			case UnitMoveResult.Auxilliary:
				return GameText.CreateLocalised("TT_MOVE_AUXILIARY", new object[0]);
			case UnitMoveResult.AlliedProvince:
				return GameText.CreateLocalised("TT_MOVE_ALLIED", new object[0]);
			case UnitMoveResult.AlreadyOccupying:
				return GameText.CreateLocalised("TT_MOVE_SAMELOCATION", new object[0]);
			case UnitMoveResult.TooManyHeroes:
				return GameText.CreateLocalised("TT_MOVE_HEROES", new object[0]);
			case UnitMoveResult.HeroNeedsUnit:
				return GameText.CreateLocalised("TT_MOVE_HEROUNIT", new object[0]);
			case UnitMoveResult.NoHarbour:
				return GameText.CreateLocalised("TT_MOVE_HARBOUR", new object[0]);
			case UnitMoveResult.TransportInvalid:
				return GameText.CreateLocalised("TT_MOVE_TRANSPORT", new object[0]);
			case UnitMoveResult.NoTransportAttack:
				return GameText.CreateLocalised("TT_MOVE_NOATTACK", new object[0]);
			case UnitMoveResult.HeroBlocked:
				return GameText.CreateLocalised("TT_MOVE_HEROBLOCK", new object[0]);
			default:
				return null;
			}
		}
	}
}
