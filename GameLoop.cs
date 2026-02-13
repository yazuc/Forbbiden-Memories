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

            _ = RunTurn();
        }

        public async Task RunTurn()
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
            await ExecuteMainPhase();
            if (_gameState.IsGameOver()) return;

            // Battle Phase
            ExecuteBattlePhase();
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
            
            if(_gameState.CurrentPlayer.Hand.Count() == HAND_SIZE)
            {
                return;
            }

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

        private async Task ExecuteMainPhase()
        {
            Console.WriteLine($"--- {_gameState.CurrentPlayer.Name}'s {_gameState.CurrentPhase} ---");
            
            foreach(var card in _gameState.CurrentPlayer.Hand)
            {
                Console.WriteLine($"- {_gameState.CurrentPlayer.Name} has {card.Name} in hand.");
            }            
            Console.WriteLine("You summoned:" + await SelectCardIndexFromHand(_gameState.CurrentPlayer, $"Hello {_gameState.CurrentPlayer.Name}, select a card to play (index) or 'b' to pass:"));
            _gameState.AdvancePhase();
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
            Console.WriteLine("Monsters on the field:");
            DisplayCards(_gameState.CurrentPlayer.Field.MonsterZones.Select(x => x.Card).ToList());

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

        // Lists the player's hand with indices and prompts for a selection.
        // Returns the selected index, or null if cancelled/invalid.
        private async Task<string?> SelectCardIndexFromHand(Player player, string prompt = "Select a card (index):")
        {
            if (player.Hand == null || player.Hand.Count == 0)
            {
                Console.WriteLine("No cards in hand.");
                return null;
            }

            Console.WriteLine(prompt);
            for (int i = 0; i < player.Hand.Count; i++)
            {
                var c = player.Hand[i];
                Console.WriteLine($"[{player.Hand[i].Id}] {c.Name} - {c.Type}");
            }

            var input = Console.ReadLine();
            List<string> selectedCardIds = new List<string>();
            if (!string.IsNullOrWhiteSpace(input))            {
                selectedCardIds = input.Split(',').Select(s => s.Trim()).ToList();
            }
            
            foreach (var id in selectedCardIds)
            {
                var card = player.Hand.FirstOrDefault(c => c.Id == int.Parse(id));
                if (card != null)
                {
                    player.Hand.Remove(card);
                }
            }

            if (string.IsNullOrWhiteSpace(input)) return null;
            if (input.Trim().ToLower() == "b") return null;

            //we need to set the result of this into field zones, independent of which
            var endCard = await Function.Fusion(input);
            player.Field.placeCard(endCard);
            //player.Field.DrawFieldState();
            GameDisplay.DisplayGameBoard(_gameState);
            return endCard.Name;            
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

        public void DisplayCards(List<Cards> cards)
        {
            Console.WriteLine("=== Cards ===");
            foreach (var card in cards)
            {
                Console.WriteLine($"- {card.Name} (ID: {card.Id}, Type: {card.Type})");
            }
        }
    }
}
