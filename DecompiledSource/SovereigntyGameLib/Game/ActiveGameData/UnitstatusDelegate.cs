using System;

namespace SovereigntyTK.Game.ActiveGameData
{
	public delegate void UnitstatusDelegate(string StatusName, ref bool Value, WorkingUnit Unit, params object[] Args);
}
