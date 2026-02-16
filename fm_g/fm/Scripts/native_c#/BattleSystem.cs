using QuickType;

namespace fm
{
	public class BattleSystem
	{
		public class BattleResult
		{
			public int DamageDealt { get; set; }
			public bool AttackerDestroyed { get; set; }
			public bool DefenderDestroyed { get; set; }
			public string? Description { get; set; }
		}

		public static BattleResult ResolveBattle(
			FieldMonster attackingMonster,
			FieldMonster? defendingMonster,
			Player defender)
		{
			var result = new BattleResult();

			// If no defending monster, direct attack
			if (defendingMonster == null)
			{
				result.DamageDealt = (int)attackingMonster.Card.Attack;
				result.Description = $"Direct attack! {attackingMonster.Card.Name} deals {result.DamageDealt} damage.";
				defender.LifePoints -= result.DamageDealt;
				return result;
			}

			// Monster-to-Monster battle
			int attackPower = (int)attackingMonster.Card.Attack;
			int defensePower = defendingMonster.IsAttackMode 
				? (int)defendingMonster.Card.Attack 
				: (int)defendingMonster.Card.Defense;

			if (attackPower > defensePower)
			{
				// Attacker wins
				result.DefenderDestroyed = true;
				result.DamageDealt = attackPower - defensePower;
				result.Description = $"{attackingMonster.Card.Name} destroys {defendingMonster.Card.Name}! " +
									$"Damage dealt: {result.DamageDealt}";
				defender.LifePoints -= result.DamageDealt;
			}
			else if (attackPower < defensePower)
			{
				// Defender wins (attacker destroyed if attacking)
				if (defendingMonster.IsAttackMode)
				{
					result.AttackerDestroyed = true;
					result.DamageDealt = defensePower - attackPower;
					result.Description = $"{defendingMonster.Card.Name} destroys {attackingMonster.Card.Name}! " +
										$"Damage to attacker's LP: {result.DamageDealt}";
				}
				else
				{
					result.Description = $"{attackingMonster.Card.Name} fails to destroy {defendingMonster.Card.Name}!";
				}
			}
			else
			{
				// Draw
				result.Description = $"Battle is a draw! Both monsters remain.";
			}

			return result;
		}

		public static bool CanAttack(FieldMonster monster, int turnNumber)
		{
			// Monster can't attack on turn it was summoned (unless it's turn 1 with 1st player)
			if (monster.TurnsOnField == 0 && turnNumber > 1)
				return false;

			return !monster.HasAttackedThisTurn;
		}

		public static void ResetBattleStates(Player player)
		{
			foreach (var monster in player.Field.MonsterZones)
			{
				if (monster != null)
				{
					monster.HasAttackedThisTurn = false;
					monster.TurnsOnField++;
				}
			}
		}
	}
}
