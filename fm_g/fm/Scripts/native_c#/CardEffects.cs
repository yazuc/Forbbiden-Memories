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
					opponent.Field.RemoveMonster(i);
				}
			}
		}
	}

	// Example effect: Ritual Summon
	public class RitualSummonEffect : BaseCardEffect
	{
		public override string EffectName => "Ritual Summon";

		public override bool CanActivate(GameState gameState, Player caster, Cards card)
		{
			// Check if player has ritual components
			return gameState.IsMainPhase() && card.Ritual != null && card.Ritual.Length > 0;
		}

		public override void Activate(GameState gameState, Player caster, Cards card)
		{
			if (!CanActivate(gameState, caster, card))
				return;

			// TODO: Implement ritual summon logic
			// 1. Select ritual card from hand
			// 2. Select tribute monsters from hand matching ritual requirements
			// 3. Summon the monster
		}
	}

	// Example effect: Fusion Summon
	public class FusionEffect : BaseCardEffect
	{
		public override string EffectName => "Fusion";

		public override bool CanActivate(GameState gameState, Player caster, Cards card)
		{
			return card.Fusions != null && card.Fusions.Length > 0;
		}

		public override void Activate(GameState gameState, Player caster, Cards card)
		{
			if (!CanActivate(gameState, caster, card))
				return;

			// TODO: Implement fusion logic using Function.TryGetFusion()
		}
	}

	public class CardEffectManager
	{
		private Dictionary<string, ICardEffect> _effects = new();

		public CardEffectManager()
		{
			RegisterEffect(new BoardWipeEffect());
			RegisterEffect(new RitualSummonEffect());
			RegisterEffect(new FusionEffect());
		}

		public void RegisterEffect(ICardEffect effect)
		{
			_effects[effect.EffectName] = effect;
		}

		public ICardEffect? GetEffect(string effectName)
		{
			return _effects.ContainsKey(effectName) ? _effects[effectName] : null;
		}

		public bool TryActivateEffect(string effectName, GameState gameState, Player caster, Cards card)
		{
			var effect = GetEffect(effectName);
			if (effect != null && effect.CanActivate(gameState, caster, card))
			{
				effect.Activate(gameState, caster, card);
				return true;
			}
			return false;
		}
	}
}
