using System;

namespace SovereigntyTK.Game.Data
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class DataConverterAttribute : Attribute
	{
		public DataConverterAttribute(Type ConverterType)
		{
			this.ConverterType = ConverterType;
		}

		public readonly Type ConverterType;
	}
}
