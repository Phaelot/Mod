using System;
using System.Collections.Generic;
using System.Drawing;

namespace SovereigntyTK.Game.Data
{
	public class VertexPoint
	{
		public VertexPoint()
		{
			this.AttachedProvinces = new List<int>();
		}

		public PointF Point;

		public List<int> AttachedProvinces;

		public VertexPoint NextPoint;

		public VertexPoint PrevPoint;

		public int ProvinceID;

		public int PolygonID;
	}
}
