using QuickType;

namespace fm
{
    public class GameLoop
    {
        private GameState _gameState;
        private CardEffectManager _effectManager;
        private const int HAND_SIZE = 5;
        private const int STARTING_HAND = 5;

        public GameLoop(Player player1, Player player2)
        {
            _gameState = new GameState(player1, player2);
            _effectManager = new CardEffectManager();
        }

        public void Initialize()
        {
            Console.WriteLine("=== Initializing Game ===");
            // Draw starting hands
            for (int i = 0; i < STARTING_HAND; i++)
            {
                Console.WriteLine("started drawing");
                if (_gameState.Player1.HasCards())
                {
                    Console.WriteLine("drawd");
                    DrawCard(_gameState.Player1);                    
                }                
                if (_gameState.Player2.HasCards())
                    DrawCard(_gameState.Player2);
            }
            
            Console.WriteLine($"Game started! {_gameState.Player1.Name} goes first.");
            foreach (var card in _gameState.Player1.Hand)
            {
                Console.WriteLine($"- {_gameState.Player1.Name} has {card.Name} in hand.");
            }            
        }

        public void RunTurn()
        {
            if (_gameState.IsGameOver())
            {
                Console.WriteLine("Game is already over!");
                return;
            }

            Console.WriteLine($"\n=== Turn {_gameState.CurrentTurn}: {_gameState.CurrentPlayer.Name}'s Turn ===");

            // Draw Phase
            ExecuteDrawPhase();
            if (_gameState.IsGameOver()) return;

            // Main Phase 1
            ExecuteMainPhase();
            if (_gameState.IsGameOver()) return;

            // Battle Phase
            ExecuteBattlePhase();
            if (_gameState.IsGameOver()) return;

            // Main Phase 2
            ExecuteMainPhase();
            if (_gameState.IsGameOver()) return;

            // End Phase
            ExecuteEndPhase();

            // Switch player
            _gameState.SwitchPlayer();
        }

        private void ExecuteDrawPhase()
        {
            _gameState.CurrentPhase = TurnPhase.Draw;
            Console.WriteLine($"{_gameState.CurrentPlayer.Name} draws a card.");
            
            if (_gameState.CurrentPlayer.HasCards() && _gameState.CurrentPlayer.Hand.Count < HAND_SIZE)
            {
                DrawCard(_gameState.CurrentPlayer);
            }
            else
            {
                Console.WriteLine($"{_gameState.CurrentPlayer.Name} has no cards to draw! Deck out!");
                _gameState.EndGame(_gameState.OpponentPlayer);
            }
        }

        private void ExecuteMainPhase()
        {
            _gameState.AdvancePhase();
            Console.WriteLine($"--- {_gameState.CurrentPlayer.Name}'s {_gameState.CurrentPhase} ---");
            // TODO: Implement player actions in main phase
            // - Summon monsters
            // - Activate spell/trap cards
            // - Pass
        }

        private void ExecuteBattlePhase()
        {
            _gameState.AdvancePhase();
            Console.WriteLine($"--- Battle Phase ---");
            // TODO: Implement battle phase
            // - Declare attacks
            // - Resolve battles
            BattleSystem.ResetBattleStates(_gameState.CurrentPlayer);
        }

        private void ExecuteEndPhase()
        {
            _gameState.AdvancePhase();
            Console.WriteLine($"--- End Phase ---");
            // TODO: Implement end phase effects
            // - Card effects that trigger at end of turn
            // - Hand size check (max 6 cards)
            EnforceHandSize(_gameState.CurrentPlayer);
        }

        private void DrawCard(Player player)
        {
            if (player.Deck.Count > 0)
            {
                var card = player.Deck.First();
                player.Hand.Add(card);
                player.Deck.RemoveAt(0);
                Console.WriteLine($"{player.Name} drew: {card.Name}");
            }
        }

        private void EnforceHandSize(Player player)
        {
            while (player.Hand.Count > HAND_SIZE)
            {
                Console.WriteLine($"{player.Name}'s hand exceeds {HAND_SIZE} cards. Must discard.");
                // TODO: Implement UI for card selection
                player.Hand.RemoveAt(0); // Placeholder
            }
        }

        public bool IsGameOver() => _gameState.IsGameOver();

        public GameState GetGameState() => _gameState;

        public void CheckWinConditions()
        {
            if (_gameState.Player1.LifePoints <= 0)
            {
                _gameState.EndGame(_gameState.Player2);
                Console.WriteLine($"\n{_gameState.Player2.Name} wins! {_gameState.Player1.Name}'s LP reached 0.");
            }
            else if (_gameState.Player2.LifePoints <= 0)
            {
                _gameState.EndGame(_gameState.Player1);
                Console.WriteLine($"\n{_gameState.Player1.Name} wins! {_gameState.Player2.Name}'s LP reached 0.");
            }
        }

        public void DisplayGameState()
        {
            Console.WriteLine($"\n=== Game State ===");
            Console.WriteLine($"{_gameState.Player1.Name} LP: {_gameState.Player1.LifePoints}");
            Console.WriteLine($"{_gameState.Player2.Name} LP: {_gameState.Player2.LifePoints}");
            Console.WriteLine($"Turn: {_gameState.CurrentTurn} | Phase: {_gameState.CurrentPhase}");
            Console.WriteLine($"Current Player: {_gameState.CurrentPlayer.Name}");
        }
    }
}
