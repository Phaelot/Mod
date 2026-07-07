using System;
using System.Collections.Generic;

namespace SovereigntyTK.Game.Data
{
	public class SpellOwnerData
	{
		public SpellOwnerData(string Realm, int Column, List<string> Required)
		{
			this.RealmName = Realm;
			this.Column = Column;
			this.PrerequisiteSpells = Required;
		}

		public string RealmName;

		public int Column;

		public List<string> PrerequisiteSpells;
	}
}
