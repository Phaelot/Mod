using System;

namespace SovereigntyTK.Game.Data
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class DataNameAttribute : Attribute
	{
		public DataNameAttribute(string DataName)
		{
			this.DataName = DataName;
		}

		public readonly string DataName;
	}
}
