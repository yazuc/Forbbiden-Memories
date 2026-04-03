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
			attackingMonster.HasAttackedThisTurn = true;
			
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
				if(defendingMonster.IsAttackMode)
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
				if(defendingMonster.IsAttackMode)
				{
					result.AttackerDestroyed = true;
					result.DefenderDestroyed = true;
				}
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

		public static (FieldMonster, FieldMonster) DefineVantagem(FieldMonster atacante, FieldMonster defensor)
		{
			if(TemVantagem(atacante.Card.CurrentGuardianStar, defensor.Card.CurrentGuardianStar))
			{
				atacante.Card.Attack += 500;
				atacante.Card.Defense += 500;

				return (atacante, defensor);
			}
			if(TemVantagem(defensor.Card.CurrentGuardianStar, atacante.Card.CurrentGuardianStar))
			{
				defensor.Card.Attack += 500;
				defensor.Card.Defense += 500;

				return (atacante, defensor);
			}
			
			return (atacante, defensor);
		}

		public static bool TemVantagem(GuardianStar atacante, GuardianStar defensor)
		{
			switch (atacante)
			{
				case GuardianStar.Mars:
					return defensor == GuardianStar.Jupiter;
				case GuardianStar.Jupiter:
					return defensor == GuardianStar.Saturn;
				case GuardianStar.Saturn:
					return defensor == GuardianStar.Uranus;
				case GuardianStar.Uranus:
					return defensor == GuardianStar.Pluto;
				case GuardianStar.Pluto:
					return defensor == GuardianStar.Neptune;
				case GuardianStar.Neptune:
					return defensor == GuardianStar.Mars;

				case GuardianStar.Sun:
					return defensor == GuardianStar.Moon;
				case GuardianStar.Moon:
					return defensor == GuardianStar.Venus;
				case GuardianStar.Venus:
					return defensor == GuardianStar.Mercury;
				default:
					return false;
			}
		}
	}
}
