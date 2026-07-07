using System;
using System.Collections.Generic;
using System.Drawing;
using SovereigntyTK.Game.Battle;

namespace SovereigntyTK.AI
{
	public class AIFormationManager
	{
		public AIFormationManager(TacticalBattleController Battle)
		{
			this.Battle = Battle;
		}

		public List<Point> GetFlankPositions(int UnitCount, Point Location, Facings Facing, List<Point> LinePositions)
		{
			List<Point> list = new List<Point>();
			Facings facings = AITacticalUtilities.RotateFacing(Facing, -2);
			Facings facings2 = AITacticalUtilities.RotateFacing(Facing, 2);
			Facings facings3 = AITacticalUtilities.RotateFacing(Facing, 4);
			Point point = Location;
			for (int i = 0; i < 2; i++)
			{
				point = AITacticalUtilities.MoveInDirection(point, facings3, i, this.Battle.Map);
				if (point.X == -1)
				{
					break;
				}
				while (LinePositions.Contains(point))
				{
					point = AITacticalUtilities.MoveInDirection(point, facings, 1, null);
				}
				if (this.Battle.Map.AllTiles.Contains(point))
				{
					list.Add(point);
					point = AITacticalUtilities.MoveInDirection(point, facings, 1, null);
				}
				if (this.Battle.Map.AllTiles.Contains(point))
				{
					list.Add(point);
				}
				point = AITacticalUtilities.MoveInDirection(point, facings3, i, null);
				if (point.X == -1)
				{
					break;
				}
				while (LinePositions.Contains(point))
				{
					point = AITacticalUtilities.MoveInDirection(point, facings2, 1, null);
				}
				if (this.Battle.Map.AllTiles.Contains(point))
				{
					list.Add(point);
					point = AITacticalUtilities.MoveInDirection(point, facings2, 1, null);
				}
				if (this.Battle.Map.AllTiles.Contains(point))
				{
					list.Add(point);
				}
			}
			return list;
		}

		public List<Point> GetMainLinePositions(int UnitCount, Point Location, Facings Facing)
		{
			Facings facings = AITacticalUtilities.RotateFacing(Facing, -2);
			Facings facings2 = AITacticalUtilities.RotateFacing(Facing, 2);
			Facings facings3 = AITacticalUtilities.RotateFacing(Facing, 4);
			Point point = Location;
			List<Point> list = new List<Point>();
			bool flag = true;
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			while (num3 < UnitCount && this.Battle.Map.AllTiles.Contains(point))
			{
				list.Add(point);
				if (flag)
				{
					num++;
				}
				flag = !flag;
				if (flag)
				{
					point = AITacticalUtilities.MoveInDirection(Location, facings, num, null);
				}
				else
				{
					point = AITacticalUtilities.MoveInDirection(Location, facings2, num, null);
				}
				bool flag2 = false;
				if (num > 4)
				{
					flag2 = true;
				}
				if (!this.Battle.Map.AllTiles.Contains(point))
				{
					flag2 = true;
				}
				if (flag2)
				{
					num2++;
					num = 0;
					flag = true;
					Location = AITacticalUtilities.MoveInDirection(Location, facings3, 1, null);
					point = Location;
				}
				num3++;
			}
			return list;
		}

		private TacticalBattleController Battle;
	}
}
