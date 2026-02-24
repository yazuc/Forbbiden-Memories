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
		
		public void TypeResults(BattleResult br){
			GD.Print($"Damage Dealt:{br.DamageDealt.ToString()}");
			GD.Print($"AttackerDestroyed:{br.AttackerDestroyed.ToString()}");
			GD.Print($"DefenderDestroyed:{br.DefenderDestroyed.ToString()}");
			GD.Print($"Description:{br.Description}");
		}

		public BattleResult ResolveBattle(
			FieldMonster attackingMonster,
			FieldMonster? defendingMonster,
			Player defender)
		{
			var result = new BattleResult();
			
			if(defender.Field.HasMonster() && defendingMonster == null)
				return result;

			// If no defending monster, direct attack
			if (defendingMonster == null)
			{
				result.DamageDealt = (int)attackingMonster.Card.Attack;
				result.Description = $"Direct attack! {attackingMonster.Card.Name} deals {result.DamageDealt} damage.";
				defender.TakeDamage(result.DamageDealt);
				TypeResults(result);
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
					result.AttackerDestroyed = false;
					result.DefenderDestroyed = false;
					result.DamageDealt = defensePower - attackPower;
					result.Description = $"{attackingMonster.Card.Name} fails to destroy {defendingMonster.Card.Name}!";
				}
			}
			else
			{
				// Draw
				result.Description = $"Battle is a draw! Both monsters remain.";
			}
			
			if(attackPower == defensePower){
				result.AttackerDestroyed = true;
				result.DefenderDestroyed = true;
			}
			
			TypeResults(result);
			return result;
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
