using QuickType;

namespace fm
{
	public interface ICardEffect
	{
		string EffectName { get; }
		void Activate(GameState gameState, Player caster, Cards card);
		bool CanActivate(GameState gameState, Player caster, Cards card);
	}

	public abstract class BaseCardEffect : ICardEffect
	{
		public abstract string EffectName { get; }
		public abstract void Activate(GameState gameState, Player caster, Cards card);
		public abstract bool CanActivate(GameState gameState, Player caster, Cards card);
	}

	// Example effect: Board wipe (Raigeki, Dark Hole)
	public class BoardWipeEffect : BaseCardEffect
	{
		public override string EffectName => "Board Wipe";

		public override bool CanActivate(GameState gameState, Player caster, Cards card)
		{
			return gameState.IsMainPhase();
		}

		public override void Activate(GameState gameState, Player caster, Cards card)
		{
			if (!CanActivate(gameState, caster, card))
				return;

			// Destroy all monsters on opponent's field
			var opponent = gameState.OpponentPlayer;
			for (int i = 0; i < opponent.Field.MonsterZones.Length; i++)
			{
				var monster = opponent.Field.MonsterZones[i];
				if (monster != null)
				{
					opponent.SendToGraveyard(monster.Card);
					opponent.Field.RemoveMonster(monster.zoneName);
				}
			}
		}
	}
	public class DestroyByFilterEffect : BaseCardEffect
	{
		public override string EffectName => "Destroy By Filter";

		private Func<FieldMonster, bool> _filter;
		private bool _onlyOpponent;

		public DestroyByFilterEffect(Func<FieldMonster, bool> filter, bool onlyOpponent = true)
		{
			_filter = filter;
			_onlyOpponent = onlyOpponent;
		}

		public override bool CanActivate(GameState gameState, Player caster, Cards card)
		{
			return gameState.IsMainPhase();
		}

		public override void Activate(GameState gameState, Player caster, Cards card)
		{
			var players = _onlyOpponent
				? new[] { gameState.OpponentPlayer }
				: new[] { gameState.CurrentPlayer, gameState.OpponentPlayer };

			foreach (var player in players)
			{
				foreach (var monster in player.Field.MonsterZones)
				{
					if (monster != null && _filter(monster))
					{
						player.SendToGraveyard(monster.Card);
						player.Field.RemoveMonster(monster.zoneName);
					}
				}
			}
		}
	}
	public class LifePointEffect : BaseCardEffect
	{
		public override string EffectName => "Life Change";

		private int _amount;
		private bool _targetOpponent;

		public LifePointEffect(int amount, bool targetOpponent = false)
		{
			_amount = amount;
			_targetOpponent = targetOpponent;
		}

		public override bool CanActivate(GameState gameState, Player caster, Cards card)
		{
			return true;
		}

		public override void Activate(GameState gameState, Player caster, Cards card)
		{
			var target = _targetOpponent ? gameState.OpponentPlayer : caster;
			target.LifePoints += _amount;
		}
	}
	public class ModifyAttackEffect : BaseCardEffect
	{
		public override string EffectName => "Modify Attack";

		private int _amount;
		private bool _onlyOpponent;

		public ModifyAttackEffect(int amount, bool onlyOpponent = true)
		{
			_amount = amount;
			_onlyOpponent = onlyOpponent;
		}

		public override bool CanActivate(GameState gameState, Player caster, Cards card)
		{
			return true;
		}

		public override void Activate(GameState gameState, Player caster, Cards card)
		{
			var player = _onlyOpponent ? gameState.OpponentPlayer : caster;

			foreach (var monster in player.Field.MonsterZones)
			{
				if (monster != null)
					monster.Card.Attack += _amount;
			}
		}
	}
	public class ChangePositionEffect : BaseCardEffect
	{
		public override string EffectName => "Change Position";

		public override bool CanActivate(GameState gameState, Player caster, Cards card)
		{
			return true;
		}

		public override void Activate(GameState gameState, Player caster, Cards card)
		{
			foreach (var monster in gameState.CurrentPlayer.Field.MonsterZones)
			{
				if (monster != null && !monster.IsAttackMode)
					monster.IsAttackMode = !monster.IsAttackMode;
			}
		}
	}

	public static class CardEffectFactory
	{
		public static ICardEffect CreateEffect(Cards card)
		{
			return card.Name switch
			{
				"Dark Hole" => new DestroyByFilterEffect(m => true, false),
				"Raigeki" => new DestroyByFilterEffect(m => true, true),
				"Crush Card" => new DestroyByFilterEffect(m => m.Card.Attack >= 1500, true),
				"Warrior Elimination" => new DestroyByFilterEffect(m => m.Card.Type == CardTypeEnum.Warrior, true),
				"Stain Storm" => new DestroyByFilterEffect(m => m.Card.Type == CardTypeEnum.Machine, true),
				"Eradicating Aerosol" => new DestroyByFilterEffect(m => m.Card.Type == CardTypeEnum.Insect, true),
				"Breath of Light" => new DestroyByFilterEffect(m => m.Card.Type == CardTypeEnum.Rock, true),
				"Dragon Capture Jar" => new DestroyByFilterEffect(m => m.Card.Type == CardTypeEnum.Dragon, true),
				"Eternal Draught" => new DestroyByFilterEffect(m => m.Card.Type == CardTypeEnum.Fish, true),
				"Harpie's Feather Duster" => new DestroyByFilterEffect(m => m.Card.IsSpellTrap(), true),

				"Mooyan Curry" => new LifePointEffect(200),
				"Red Medicine" => new LifePointEffect(500),
				"Goblin Secret Remedy" => new LifePointEffect(1000),
				"Soul of the Pure" => new LifePointEffect(2000),
				"Dian Keto" => new LifePointEffect(5000),

				"Sparks" => new LifePointEffect(-50, true),
				"Hinotama" => new LifePointEffect(-100, true),
				"Final Flame" => new LifePointEffect(-200, true),
				"Ookazi" => new LifePointEffect(-500, true),
				"Tremendous Fire" => new LifePointEffect(-1000, true),

				"Spellbinding Circle" => new ModifyAttackEffect(-1000),
				"Shadow Spell" => new ModifyAttackEffect(-1500),


				_ => throw new Exception($"Effect not defined for card {card.Name}")
			};
		}
	}

	public class CardEffectManager
	{
		private Dictionary<string, ICardEffect> _effects = new();

		public CardEffectManager()
		{	
			RegisterEffect(new DestroyByFilterEffect(m => true)); // Dark Hole
			RegisterEffect(new DestroyByFilterEffect(m => true, onlyOpponent: true)); // Raigeki
			RegisterEffect(new DestroyByFilterEffect(m => m.Card.Type == CardTypeEnum.Dragon, false)); // Dragon Capture Jar
			
			RegisterEffect(new LifePointEffect(+200)); // Mooyan Curry
			RegisterEffect(new LifePointEffect(+500)); // Red Medicine
			RegisterEffect(new LifePointEffect(+1000)); // Goblin Remedy
			RegisterEffect(new LifePointEffect(+2000)); // Soul of the Pure
			RegisterEffect(new LifePointEffect(+5000)); // Dian Keto

			RegisterEffect(new LifePointEffect(-50, true)); // Sparks
			RegisterEffect(new LifePointEffect(-100, true)); // Hinotama
			RegisterEffect(new LifePointEffect(-200, true)); // Final Flame
			RegisterEffect(new LifePointEffect(-500, true)); // Ookazi
			RegisterEffect(new LifePointEffect(-1000, true)); // Tremendous Fire

			RegisterEffect(new ModifyAttackEffect(-1000)); // Spellbinding Circle
			RegisterEffect(new ModifyAttackEffect(-1500)); // Shadow Spell

			RegisterEffect(new DestroyByFilterEffect(m => m.Card.Attack >= 1500, false)); // Crush Card

		}

		public void RegisterEffect(ICardEffect effect)
		{
			_effects[effect.EffectName] = effect;
		}

		public ICardEffect? GetEffect(string effectName)
		{
			return _effects.ContainsKey(effectName) ? _effects[effectName] : null;
		}

		public bool TryActivateEffect(GameLoop gameLoop, Cards card)
		{
			var effect = card.cardEffect;
			if (effect != null && effect.CanActivate(gameLoop._gameState, gameLoop._gameState.CurrentPlayer, card))
			{
				effect.Activate(gameLoop._gameState, gameLoop._gameState.CurrentPlayer, card);
				return true;
			}
			return false;
		}
	}
}
