using QuickType;

namespace fm
{
    public class WinConditions
    {
        public enum VictoryType
        {
            None,
            LifePointsZero,
            DeckOut,
            Surrender,
            SpecialCondition
        }

        public static (bool gameOver, Player? winner, VictoryType victoryType) CheckWinConditions(GameState gameState)
        {
            // Check if either player's LP is 0 or below
            if (gameState.Player1.LifePoints <= 0)
            {
                return (true, gameState.Player2, VictoryType.LifePointsZero);
            }

            if (gameState.Player2.LifePoints <= 0)
            {
                return (true, gameState.Player1, VictoryType.LifePointsZero);
            }

            // Check if either player ran out of deck cards
            if (!gameState.Player1.HasCards())
            {
                return (true, gameState.Player2, VictoryType.DeckOut);
            }

            if (!gameState.Player2.HasCards())
            {
                return (true, gameState.Player1, VictoryType.DeckOut);
            }

            // No win condition met
            return (false, null, VictoryType.None);
        }

        public static void DisplayVictoryMessage(Player winner, VictoryType victoryType)
        {
            string message = victoryType switch
            {
                VictoryType.LifePointsZero => $"{winner.Name} wins! {winner.Name} reduced opponent's LP to 0.",
                VictoryType.DeckOut => $"{winner.Name} wins! Opponent ran out of deck cards.",
                VictoryType.Surrender => $"{winner.Name} wins! Opponent surrendered.",
                VictoryType.SpecialCondition => $"{winner.Name} wins! Special win condition met.",
                _ => $"{winner.Name} wins!"
            };

            Console.WriteLine($"\n{'=',30}");
            Console.WriteLine($"VICTORY!");
            Console.WriteLine(message);
            Console.WriteLine($"{'=',30}");
        }

        public static bool PlayerCanSurrender(GameState gameState) => true;

        public static void PlayerSurrenders(GameState gameState, Player surrenderingPlayer)
        {
            var winner = gameState.Player1 == surrenderingPlayer ? gameState.Player2 : gameState.Player1;
            gameState.EndGame(winner);
            DisplayVictoryMessage(winner, VictoryType.Surrender);
        }
    }
}
