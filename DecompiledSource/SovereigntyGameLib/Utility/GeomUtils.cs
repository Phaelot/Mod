using System;
using System.Drawing;

namespace SovereigntyTK.Utility
{
	internal class GeomUtils
	{
		public static bool IsPointInPolygon(PointF p, PointF[] polygon)
		{
			double num = (double)polygon[0].X;
			double num2 = (double)polygon[0].X;
			double num3 = (double)polygon[0].Y;
			double num4 = (double)polygon[0].Y;
			for (int i = 1; i < polygon.Length; i++)
			{
				PointF pointF = polygon[i];
				num = Math.Min((double)pointF.X, num);
				num2 = Math.Max((double)pointF.X, num2);
				num3 = Math.Min((double)pointF.Y, num3);
				num4 = Math.Max((double)pointF.Y, num4);
			}
			if ((double)p.X < num || (double)p.X > num2 || (double)p.Y < num3 || (double)p.Y > num4)
			{
				return false;
			}
			bool flag = false;
			int j = 0;
			int num5 = polygon.Length - 1;
			while (j < polygon.Length)
			{
				if (polygon[j].Y > p.Y != polygon[num5].Y > p.Y && p.X < (polygon[num5].X - polygon[j].X) * (p.Y - polygon[j].Y) / (polygon[num5].Y - polygon[j].Y) + polygon[j].X)
				{
					flag = !flag;
				}
				num5 = j++;
			}
			return flag;
		}
	}
}
