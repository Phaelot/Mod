using System;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public delegate void UnitDamageDelegate(WorkingUnit Damager, DamageTypes Type, bool Ranged, bool Retal, ref int Damage);
}
