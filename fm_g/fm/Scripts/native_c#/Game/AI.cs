using QuickType;
using System.Collections.Generic;

namespace fm
{
	public class AIPlayer
	{
		private readonly Random _rng = new();
		public string Difficulty { get; private set; }

		public enum DifficultyLevel
		{
			Easy,
			Medium,
			Hard
		}

		public AIPlayer(DifficultyLevel difficulty = DifficultyLevel.Medium)
		{
			Difficulty = difficulty.ToString();
		}

		public AIMove? SelectCardToPlay(Player player, GameState gameState)
		{
			// Get playable cards from hand
			var playableCards = player.Hand
				.Where(c => IsCardPlayable(c, player, gameState))
				.ToList();

			if (!playableCards.Any())
				return null;

			return Difficulty switch
			{
				"Hard" => SelectCardHard(playableCards, player, gameState)
			};
		}

		public int SelectMonsterZone(Player player)
		{
			var availableZones = Enumerable.Range(0, FieldZones.MONSTER_ZONES)
				.Where(i => player.Field.MonsterZones[i] == null)
				.ToList();

			return availableZones.Any() ? availableZones[_rng.Next(availableZones.Count)] : -1;
		}

		public AIMove SelectAttack(Player aiPlayer, Player opponent, GameState gameState)
		{
			var availableAttackers = aiPlayer.Field.MonsterZones.Where(m => m != null).ToList();

			if (!availableAttackers.Any())
				return null;

			string attackerZone = availableAttackers[_rng.Next(availableAttackers.Count)].zoneName;
			AIMove move = new AIMove
			{
				AttackerZone = attackerZone
			};

			// Try to find a target
			var availableDefenders = opponent.Field.MonsterZones.Where(m => m != null).ToList();

			if (availableDefenders.Any())
			{
				string defenderZone = availableDefenders[_rng.Next(availableDefenders.Count)].zoneName;
				move.DefenderZone = defenderZone;
				return move;
			}

			// Direct attack
			return move;
		}

		// --- Selection Strategies ---

		private AIMove SelectCardHard(List<Cards> playableCards, Player player, GameState gameState)
		{
			// Priority: Board clears if opponent has strong board
			var opponent = gameState.OpponentPlayer;
			var opponentMonsterCount = opponent.Field.MonsterZones.Count(m => m != null);
			AIMove aIMove = new AIMove();

			if (opponentMonsterCount >= 3)
			{
				var boardClears = playableCards
					.Where(c => c.Name == "Raigeki" || c.Name == "Dark Hole")
					.ToList();

				if (boardClears.Any())
				{					
					aIMove.CardToPlay.Add(boardClears.First());
					aIMove.FaceUP = true;
					aIMove.IndexCard.Add(playableCards.IndexOf(boardClears.First()));
					return aIMove;
				}
			}

			// Second priority: Monsters that can fuse
			var range = player.Field.MonsterZones.Where(m => m != null).Select(m => m.Card).ToList();
			var possibleFusions = new List<Cards>(playableCards);
			possibleFusions.AddRange(range);
			var idsNaMao = possibleFusions.Select(c => c.Id).ToHashSet();
		
			var bestFusion = possibleFusions
				.Where(c => c.Fusions != null)
				.SelectMany(c => c.Fusions
					.Where(f =>
						f.Card1 == f.Card2
							? idsNaMao.Count(id => id == f.Card1) > 1
							: idsNaMao.Contains(f.Card2)
					)
				)
				.OrderByDescending(f => f.Result)
				.FirstOrDefault();


			var fusionResult = bestFusion?.Card1 != null ? Function.ProcessChain($"{bestFusion?.Card1}, {bestFusion?.Card2}") : null;
			if (fusionResult != null && fusionResult.FusaoAconteceu)
			{								
				aIMove.CardToPlay.Add(GlobalUsings.Instance.db.GetCardById(bestFusion.Card1));
				aIMove.CardToPlay.Add(GlobalUsings.Instance.db.GetCardById(bestFusion.Card2));
				aIMove.IndexCard.Add(playableCards.IndexOf(playableCards.FirstOrDefault(c => c.Id == bestFusion.Card1)));
				aIMove.IndexCard.Add(playableCards.IndexOf(playableCards.FirstOrDefault(c => c.Id == bestFusion.Card2)));
				return aIMove;
			}
			// 	return fusionMonsters.First();

			// Default to highest attack
			var monsters = playableCards
				.Where(c => c.Type != CardTypeEnum.Spell && c.Type != CardTypeEnum.Trap)
				.OrderByDescending(c => c.Attack)
				.ToList();

			if (monsters.Any())
			{
				aIMove.CardToPlay.Add(monsters.First());
				aIMove.IndexCard.Add(playableCards.IndexOf(monsters.First()));
				return aIMove;
			}

			return null;
		}

		private bool IsCardPlayable(Cards card, Player player, GameState gameState)
		{
			// Check if main phase
			if (!gameState.IsMainPhase())
				return false;

			// Monsters can be played if field zone available
			if (card.Type != CardTypeEnum.Spell && card.Type != CardTypeEnum.Trap)
			{
				return player.Field.HasAvailableMonsterZone();
			}

			// Spells and traps need spell/trap zone
			return true;
		}
	}
}
