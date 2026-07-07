using System;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public delegate void UnitDamagedDelegate(int Damage, DamageTypes Type, bool Ranged, WorkingUnit OtherUnit);
}
