using System;
using SovereigntyTK.Game.ActiveGameData;
using SovereigntyTK.Game.Data;

namespace SovereigntyTK.Game.Battle
{
	public class CombatResults
	{
		internal void ApplyDamage()
		{
			string text = null;
			string text2 = null;
			if (this.Defender.BattleData != null)
			{
				bool flag = this.Defender.BattleData.Battle != null;
				if (this.AttackerCasualties > 0)
				{
					if (flag)
					{
						text = this.GetAnimation(this.Attacker, this.Defender.GetDamageType(), this.Defender);
					}
					this.Attacker.ApplyRealDamage(this.AttackerCasualties, this.Defender.GetDamageType(), this.Ranged, this.Defender, text);
				}
				if (this.DefenderCasualties > 0)
				{
					if (flag)
					{
						text2 = this.GetAnimation(this.Defender, this.Attacker.GetDamageType(), this.Attacker);
					}
					this.Defender.ApplyRealDamage(this.DefenderCasualties, this.Attacker.GetDamageType(), this.Ranged, this.Attacker, text2);
				}
			}
		}

		private string GetAnimation(WorkingUnit DamagedUnit, DamageTypes DamageType, WorkingUnit Damager)
		{
			if (DamagedUnit.BattleData.IsStandingOn(new string[] { "sea" }))
			{
				return "attack_water";
			}
			switch (DamageType)
			{
			case DamageTypes.Nature:
				return "attack_nature";
			case DamageTypes.War:
				return "attack_war";
			case DamageTypes.Illusion:
				return "attack_illusion";
			case DamageTypes.Death:
				return "attack_death";
			default:
				return Damager.AttackAnimation;
			}
		}

		public WorkingUnit Attacker;

		public WorkingUnit Defender;

		public int AttackerCasualties;

		public int DefenderCasualties;

		public bool Ranged;
	}
}
