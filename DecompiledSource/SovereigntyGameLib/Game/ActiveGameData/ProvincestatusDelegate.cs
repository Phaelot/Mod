using System;

namespace SovereigntyTK.Game.ActiveGameData
{
	public delegate void ProvincestatusDelegate(string StatusName, ref bool Value, WorkingProvince Province, params object[] Args);
}
