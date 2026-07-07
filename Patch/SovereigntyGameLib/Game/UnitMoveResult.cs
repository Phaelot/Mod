using System;

namespace SovereigntyTK.Game
{
	public enum UnitMoveResult
	{
		OK,
		NotEnoughMoves,
		InvalidTerrain,
		AttackNotPossible,
		NoUnitsSelected,
		ProvinceLocked,
		ProvinceLocked2,
		ProvinceFull,
		LandmarkBlocked,
		NotOwned,
		FlagBlocked,
		Auxilliary,
		AlliedProvince,
		AlreadyOccupying,
		TooManyHeroes,
		HeroNeedsUnit,
		NoHarbour,
		TransportInvalid,
		NoTransportAttack,
		HeroBlocked
	}
}
