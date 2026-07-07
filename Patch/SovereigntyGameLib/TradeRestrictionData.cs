using System;
using System.Collections.Generic;

namespace SovereigntyTK
{
	public class TradeRestrictionData
	{
		public TradeRestrictionData()
		{
			this.ResourceWhitelist = new List<string>();
		}

		public bool GoldBlocked;

		public bool GoldPerTurnBlocked;

		public bool ResourceBlocked;

		public bool ResourcePerTurnBlocked;

		public bool ProvinceBlocked;

		public bool TreatyBlocked;

		public List<string> ResourceWhitelist;

		public bool PrisonersBlocked;
	}
}
