using System;

namespace SovereigntyTK.Game.ActiveGameData
{
	public delegate void RealmstatusDelegate(string StatusName, ref bool Value, WorkingRealm Realm, params object[] Args);
}
