using System;
using System.Collections.Generic;
using SovereigntyTK.UI.Controls;

namespace SovereigntyTK.UI
{
	public class ControlZComparer : IComparer<UIControl>
	{
		public int Compare(UIControl A, UIControl B)
		{
			if (A.ParentControl == B.ParentControl)
			{
				return A.ZOrder.CompareTo(B.ZOrder);
			}
			if (A.ControlIsAbove(B))
			{
				return 1;
			}
			return -1;
		}
	}
}
