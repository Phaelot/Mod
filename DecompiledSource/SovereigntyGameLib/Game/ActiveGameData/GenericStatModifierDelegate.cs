using System;

namespace SovereigntyTK.Game.ActiveGameData
{
	public delegate void GenericStatModifierDelegate(WorkingUnit Unit, WorkingUnit EnemyUnit, UnitStatNames StatName, ref int Value);
}
