using System;

namespace SovereigntyTK.Game.Data
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class PrimaryKeyAttribute : Attribute
	{
		public PrimaryKeyAttribute(int Index)
		{
			this.Index = Index;
		}

		public readonly int Index;
	}
}
