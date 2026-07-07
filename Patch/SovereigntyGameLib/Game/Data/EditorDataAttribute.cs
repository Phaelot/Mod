using System;

namespace SovereigntyTK.Game.Data
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class EditorDataAttribute : Attribute
	{
		public EditorDataAttribute(string EditorName, EditorTypes EditorType)
		{
			this.EditorName = EditorName;
			this.EditorType = EditorType;
		}

		public EditorDataAttribute(string EditorName)
		{
			this.EditorName = EditorName;
			this.EditorType = EditorTypes.Text;
		}

		public readonly string EditorName;

		public readonly EditorTypes EditorType;
	}
}
