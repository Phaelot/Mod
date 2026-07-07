using System;
using System.Collections.Generic;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.ActiveGameData
{
	public class RealmSpell
	{
		public RealmSpell(RealmMagicData Data, List<string> RequiredSpells)
		{
			this.Data = Data;
			this.RequiredSpells = RequiredSpells;
		}

		public RealmMagicData Data;

		public bool Learned;

		public List<string> RequiredSpells;
	}
}
