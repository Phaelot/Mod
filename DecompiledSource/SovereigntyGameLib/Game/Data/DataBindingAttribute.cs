using System;

namespace SovereigntyTK.Game.Data
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class DataBindingAttribute : Attribute
	{
		public DataBindingAttribute(string TableName, string ColumnName, bool IncludeNull = false)
		{
			this.BindTable = TableName;
			this.BindColumn = ColumnName;
			this.IncludeNull = IncludeNull;
		}

		public readonly string BindTable;

		public readonly string BindColumn;

		public readonly bool IncludeNull;
	}
}
