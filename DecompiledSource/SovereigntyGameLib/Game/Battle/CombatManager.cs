using System;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.Battle
{
	public class CombatManager
	{
		public static CombatResults PerformCombat(WorkingUnit Attacker, WorkingUnit Defender, CombatType Type, bool Ranged, bool IncludeRetal, bool IsCounterFire)
		{
			int value = Attacker.Initiative.GetValue(Defender);
			int value2 = Defender.Initiative.GetValue(Attacker);
			int num = 0;
			int num2;
			if (value > value2)
			{
				num2 = CombatManager.GetDamage(Attacker, Defender, Ranged, Type);
				num2 = Attacker.GetModifiedDamage(Defender, num2, Ranged, IsCounterFire, Attacker.GetDamageType());
				num2 = Defender.ApplySimulatedDamage(Attacker, num2, Ranged, IsCounterFire, Attacker.GetDamageType());
				if (IncludeRetal)
				{
					num = CombatManager.GetDamage(Defender, Attacker, Ranged, Type);
					num = Defender.GetModifiedDamage(Attacker, num, Ranged, true, Defender.GetDamageType());
					num = Attacker.ApplySimulatedDamage(Defender, num, Ranged, true, Defender.GetDamageType());
				}
			}
			else if (value2 > value)
			{
				if (IncludeRetal)
				{
					num = CombatManager.GetDamage(Defender, Attacker, Ranged, Type);
					num = Defender.GetModifiedDamage(Attacker, num, Ranged, true, Defender.GetDamageType());
					num = Attacker.ApplySimulatedDamage(Defender, num, Ranged, true, Defender.GetDamageType());
				}
				num2 = CombatManager.GetDamage(Attacker, Defender, Ranged, Type);
				num2 = Attacker.GetModifiedDamage(Defender, num2, Ranged, IsCounterFire, Attacker.GetDamageType());
				num2 = Defender.ApplySimulatedDamage(Attacker, num2, Ranged, IsCounterFire, Attacker.GetDamageType());
			}
			else
			{
				if (IncludeRetal)
				{
					num = CombatManager.GetDamage(Defender, Attacker, Ranged, Type);
					num = Defender.GetModifiedDamage(Attacker, num, Ranged, true, Defender.GetDamageType());
				}
				num2 = CombatManager.GetDamage(Attacker, Defender, Ranged, Type);
				num2 = Attacker.GetModifiedDamage(Defender, num2, Ranged, IsCounterFire, Attacker.GetDamageType());
				num2 = Defender.ApplySimulatedDamage(Attacker, num2, Ranged, IsCounterFire, Attacker.GetDamageType());
				if (IncludeRetal)
				{
					num = Attacker.ApplySimulatedDamage(Defender, num, Ranged, true, Defender.GetDamageType());
				}
			}
			if (Type != CombatType.Real)
			{
				Defender.RemoveSimulatedDamage(num2);
				Attacker.RemoveSimulatedDamage(num);
			}
			return new CombatResults
			{
				Attacker = Attacker,
				Defender = Defender,
				AttackerCasualties = num,
				DefenderCasualties = num2,
				Ranged = Ranged
			};
		}

		public static int GetDamage(WorkingUnit Attacker, WorkingUnit Defender, bool Ranged, CombatType Type)
		{
			if (Attacker.Health <= 0)
			{
				return 0;
			}
			int num = Attacker.Attack.GetValue(Defender);
			if (Ranged)
			{
				num = Attacker.RangedAttack.GetValue(Defender);
			}
			int value = Defender.Defence.GetValue(Attacker);
			float num2 = 0f;
			for (int i = 0; i < num; i++)
			{
				num2 += 15f;
			}
			for (int j = 0; j < value; j++)
			{
				num2 -= 5f;
			}
			switch (Type)
			{
			case CombatType.MaxDamage:
			{
				for (int k = 0; k < num; k++)
				{
					num2 += 2f;
				}
				for (int l = 0; l < value; l++)
				{
					num2 -= 0f;
				}
				break;
			}
			case CombatType.MinDamage:
			{
				for (int m = 0; m < num; m++)
				{
					num2 += 0f;
				}
				for (int n = 0; n < value; n++)
				{
					num2 -= 2f;
				}
				break;
			}
			default:
			{
				for (int num3 = 0; num3 < num; num3++)
				{
					num2 += (float)CombatManager.RNG.Next(3);
				}
				for (int num4 = 0; num4 < value; num4++)
				{
					num2 -= (float)CombatManager.RNG.Next(3);
				}
				break;
			}
			}
			if (Attacker.EntityType == EntityType.Group)
			{
				num2 *= 0.01f * (float)Attacker.Health;
			}
			if (Ranged && Defender.EntityType == EntityType.Group)
			{
				num2 *= 0.01f * (float)Defender.Health;
			}
			return Math.Max(0, (int)Math.Ceiling((double)num2));
		}

		private WorkingUnit Attacker;

		private WorkingUnit Defender;

		private static Random RNG = new Random();
	}
}
