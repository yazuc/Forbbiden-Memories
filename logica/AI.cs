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

        public Cards? SelectCardToPlay(Player player, GameState gameState)
        {
            // Get playable cards from hand
            var playableCards = player.Hand
                .Where(c => IsCardPlayable(c, player, gameState))
                .ToList();

            if (!playableCards.Any())
                return null;

            return Difficulty switch
            {
                "Easy" => SelectCardEasy(playableCards, player, gameState),
                "Medium" => SelectCardMedium(playableCards, player, gameState),
                "Hard" => SelectCardHard(playableCards, player, gameState),
                _ => playableCards[_rng.Next(playableCards.Count)]
            };
        }

        public int SelectMonsterZone(Player player)
        {
            var availableZones = Enumerable.Range(0, FieldZones.MONSTER_ZONES)
                .Where(i => player.Field.MonsterZones[i] == null)
                .ToList();

            return availableZones.Any() ? availableZones[_rng.Next(availableZones.Count)] : -1;
        }

        public (int attackerZone, int defenderZone)? SelectAttack(Player aiPlayer, Player opponent, GameState gameState)
        {
            var availableAttackers = Enumerable.Range(0, FieldZones.MONSTER_ZONES)
                .Where(i => 
                {
                    var monster = aiPlayer.Field.MonsterZones[i];
                    return monster != null && 
                           BattleSystem.CanAttack(monster, gameState.CurrentTurn) &&
                           monster.IsAttackMode;
                })
                .ToList();

            if (!availableAttackers.Any())
                return null;

            int attackerZone = availableAttackers[_rng.Next(availableAttackers.Count)];

            // Try to find a target
            var availableDefenders = Enumerable.Range(0, FieldZones.MONSTER_ZONES)
                .Where(i => opponent.Field.MonsterZones[i] != null)
                .ToList();

            if (availableDefenders.Any())
            {
                int defenderZone = availableDefenders[_rng.Next(availableDefenders.Count)];
                return (attackerZone, defenderZone);
            }

            // Direct attack
            return (attackerZone, -1);
        }

        // --- Selection Strategies ---

        private Cards SelectCardEasy(List<Cards> playableCards, Player player, GameState gameState)
        {
            // Random card selection
            return playableCards[_rng.Next(playableCards.Count)];
        }

        private Cards SelectCardMedium(List<Cards> playableCards, Player player, GameState gameState)
        {
            // Prefer monsters with highest attack
            var monsters = playableCards
                .Where(c => c.Type != CardTypeEnum.Spell && c.Type != CardTypeEnum.Trap)
                .OrderByDescending(c => c.Attack)
                .ToList();

            if (monsters.Any())
                return monsters.First();

            return playableCards[_rng.Next(playableCards.Count)];
        }

        private Cards SelectCardHard(List<Cards> playableCards, Player player, GameState gameState)
        {
            // Priority: Board clears if opponent has strong board
            var opponent = gameState.OpponentPlayer;
            var opponentMonsterCount = opponent.Field.MonsterZones.Count(m => m != null);

            if (opponentMonsterCount >= 3)
            {
                var boardClears = playableCards
                    .Where(c => c.Name == "Raigeki" || c.Name == "Dark Hole")
                    .ToList();

                if (boardClears.Any())
                    return boardClears.First();
            }

            // Second priority: Monsters that can fuse
            var fusionMonsters = playableCards
                .Where(c => c.Fusions != null && c.Fusions.Length > 0)
                .OrderByDescending(c => c.Attack)
                .ToList();

            if (fusionMonsters.Any())
                return fusionMonsters.First();

            // Default to highest attack
            var monsters = playableCards
                .Where(c => c.Type != CardTypeEnum.Spell && c.Type != CardTypeEnum.Trap)
                .OrderByDescending(c => c.Attack)
                .ToList();

            if (monsters.Any())
                return monsters.First();

            return playableCards[_rng.Next(playableCards.Count)];
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
