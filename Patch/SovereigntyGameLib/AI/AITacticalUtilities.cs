// SovereigntyGameLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// SovereigntyTK.AI.AITacticalUtilities
using System;
using System.Drawing;
using OpenTK;
using SovereigntyTK.AI;
using SovereigntyTK.UI.Map;

namespace SovereigntyTK.AI
{
	public class AITacticalUtilities
	{
		public static Facings RotateFacing(Facings Facing, int Amount)
		{
			int num = (int)Facing;
			num += Amount;
			if (num < 0)
			{
				num += 8;
			}
			if (num > 7)
			{
				num -= 8;
			}
			return (Facings)num;
		}

		public static Facings ConvertDirectionToFacing(Vector2 Direction)
		{
			Vector2 vector = new Vector2(0f, -1f);
			double radians = Math.Atan2(Direction.Y, Direction.X) - Math.Atan2(vector.Y, vector.X);
			double num = RadiansToDegrees(radians);
			if (num <= 15.0)
			{
				return Facings.North;
			}
			if (num <= 60.0)
			{
				return Facings.NorthEast;
			}
			if (num <= 120.0)
			{
				return Facings.East;
			}
			if (num <= 165.0)
			{
				return Facings.SouthEast;
			}
			if (num <= 195.0)
			{
				return Facings.South;
			}
			if (num <= 240.0)
			{
				return Facings.SouthWest;
			}
			if (num <= 300.0)
			{
				return Facings.West;
			}
			if (num <= 345.0)
			{
				return Facings.NorthWest;
			}
			return Facings.North;
		}

		private static double RadiansToDegrees(double Radians)
		{
			return Radians * (180.0 / Math.PI);
		}

		public static Point MoveInDirection(Point Location, Facings Direction, int Distance, BattleMap Map)
		{
			Point point = Location;
			for (int i = 0; i < Distance; i++)
			{
				Point point2 = point;
				switch (Direction)
				{
					case Facings.North:
						point2.Y--;
						break;
					case Facings.NorthEast:
						if (point.X % 2 == 0)
						{
							point2.X++;
							point2.Y--;
						}
						else
						{
							point2.X++;
						}
						break;
					case Facings.East:
						point2.X++;
						break;
					case Facings.SouthEast:
						if (point2.X % 2 == 0)
						{
							point2.X++;
							break;
						}
						point2.X++;
						point2.Y++;
						break;
					case Facings.South:
						point2.Y++;
						break;
					case Facings.SouthWest:
						if (point2.X % 2 == 0)
						{
							point2.X--;
							break;
						}
						point2.X--;
						point2.Y++;
						break;
					case Facings.West:
						point2.X--;
						break;
					case Facings.NorthWest:
						if (point2.X % 2 == 0)
						{
							point2.X--;
							point2.Y--;
						}
						else
						{
							point2.X--;
						}
						break;
				}
				if (Map != null && !Map.TileInsideBounds(point2))
				{
					return new Point(-1, -1);
				}
				if (Map == null || Map.AllTiles.Contains(point2))
				{
					point = point2;
				}
			}
			return point;
		}
	}
}