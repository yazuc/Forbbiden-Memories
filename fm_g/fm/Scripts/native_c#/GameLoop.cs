using QuickType;
using Godot;

namespace fm
{
	public class GameLoop
	{
		public MaoJogador MaoDoJogador;
		public Camera3D CameraHand;
		public Camera3D CameraField;
		private GameState _gameState;
		private CardEffectManager _effectManager;
		private const int HAND_SIZE = 5;
		private const int STARTING_HAND = 5;

		public GameLoop(Player player1, Player player2, MaoJogador maoUI, Camera3D CameraHand, Camera3D CameraField)
		{
			_gameState = new GameState(player1, player2);
			_effectManager = new CardEffectManager();
			this.MaoDoJogador = maoUI;
			this.CameraHand = CameraHand;
			this.CameraField = CameraField;
		}

		public void Initialize()
		{
			GD.Print("=== Initializing Game ===");
			if (MaoDoJogador == null) {
				GD.PrintErr("ERRO: MaoDoJogador é nula no Initialize! Verifique a atribuição no construtor.");
				return;
			}
			// Draw starting hands
			for (int i = 0; i < STARTING_HAND; i++)
			{
				GD.Print("started drawing");
				if (_gameState.Player1.HasCards())
				{
					GD.Print("drawd");
					DrawCard(_gameState.Player1);                    
				}                
				if (_gameState.Player2.HasCards())
					DrawCard(_gameState.Player2);
			}
			
			GD.Print($"Game started! {_gameState.Player1.Name} goes first.");
			foreach (var card in _gameState.Player1.Hand)
			{
				GD.Print($"- {_gameState.Player1.Name} has {card.Name} in hand.");
			}
			_ = RunTurn();
		}

		public async Task RunTurn()
		{
			if (_gameState.IsGameOver())
			{
				GD.Print("Game is already over!");
				return;
			}

			GD.Print($"\n=== Turn {_gameState.CurrentTurn}: {_gameState.CurrentPlayer.Name}'s Turn ===");

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
			GD.Print($"{_gameState.CurrentPlayer.Name} draws a card.");
			
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
				GD.Print($"{_gameState.CurrentPlayer.Name} has no cards to draw! Deck out!");
				_gameState.EndGame(_gameState.OpponentPlayer);
			}
		}

		private async Task ExecuteMainPhase()
		{
			GD.Print($"--- {_gameState.CurrentPlayer.Name}'s {_gameState.CurrentPhase} ---");
			
			foreach(var card in _gameState.CurrentPlayer.Hand)
			{
				GD.Print($"- {_gameState.CurrentPlayer.Name} has {card.Name} in hand.");
			}       
			
			var handIds = _gameState.Player1.Hand.Select(x => x.Id).ToList();
 			MaoDoJogador.AtualizarMao(handIds);   
			
			GD.Print("Aguardando jogador selecionar uma carta...");
			int idEscolhido = await EsperarEscolhaDoJogador(); 			
			GD.Print($"O jogador escolheu a carta com ID: {idEscolhido}");
			
			CameraHand.Current = false;
			CameraField.Current = true;  
			
			GD.Print($"Hand: {CameraHand.Current.ToString()} Field: {CameraField.Current.ToString()}");
			//GD.Print("You summoned:" + await SelectCardIndexFromHand(_gameState.CurrentPlayer, $"Hello {_gameState.CurrentPlayer.Name}, select a card to play (index) or 'b' to pass:"));
			_gameState.AdvancePhase();
			// TODO: Implement player actions in main phase
			// - Summon monsters
			// - Activate spell/trap cards
			// - Pass
		}

		private void ExecuteBattlePhase()
		{
			_gameState.AdvancePhase();
			GD.Print($"--- Battle Phase ---");
			// TODO: Implement battle phase
			// - Declare attacks
			// - Resolve battles
			GD.Print("Monsters on the field:");
			BattleSystem.ResetBattleStates(_gameState.CurrentPlayer);
		}

		private void ExecuteEndPhase()
		{
			_gameState.AdvancePhase();
			GD.Print($"--- End Phase ---");
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
				GD.Print($"{player.Name} drew: {card.Name}");
			}
		}

		private void EnforceHandSize(Player player)
		{
			while (player.Hand.Count > HAND_SIZE)
			{
				GD.Print($"{player.Name}'s hand exceeds {HAND_SIZE} cards. Must discard.");
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
				GD.Print("No cards in hand.");
				return null;
			}

			GD.Print(prompt);
			for (int i = 0; i < player.Hand.Count; i++)
			{
				var c = player.Hand[i];
				GD.Print($"[{player.Hand[i].Id}] {c.Name} - {c.Type}");
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
			player.Field.DrawFieldState();
			//GameDisplay.DisplayGameBoard(_gameState);
			return endCard.Name;            
		}

		public bool IsGameOver() => _gameState.IsGameOver();

		public GameState GetGameState() => _gameState;

		public void CheckWinConditions()
		{
			if (_gameState.Player1.LifePoints <= 0)
			{
				_gameState.EndGame(_gameState.Player2);
				GD.Print($"\n{_gameState.Player2.Name} wins! {_gameState.Player1.Name}'s LP reached 0.");
			}
			else if (_gameState.Player2.LifePoints <= 0)
			{
				_gameState.EndGame(_gameState.Player1);
				GD.Print($"\n{_gameState.Player1.Name} wins! {_gameState.Player2.Name}'s LP reached 0.");
			}
		}
		
		private Task<int> EsperarEscolhaDoJogador()
		{
			var tcs = new TaskCompletionSource<int>();

			// Use Godot.ConnectFlags para resolver o erro CS0103
			MaoDoJogador.Connect(MaoJogador.SignalName.CartaSelecionada, Callable.From<int>((id) => {
				tcs.TrySetResult(id);
			}), (uint)GodotObject.ConnectFlags.OneShot); 

			return tcs.Task;
		}
		
		public void DisplayGameState()
		{
			GD.Print($"\n=== Game State ===");
			GD.Print($"{_gameState.Player1.Name} LP: {_gameState.Player1.LifePoints}");
			GD.Print($"{_gameState.Player2.Name} LP: {_gameState.Player2.LifePoints}");
			GD.Print($"Turn: {_gameState.CurrentTurn} | Phase: {_gameState.CurrentPhase}");
			GD.Print($"Current Player: {_gameState.CurrentPlayer.Name}");
		}

		public void DisplayCards(List<Cards> cards)
		{
			GD.Print("=== Cards ===");
			foreach (var card in cards)
			{
				GD.Print($"- {card.Name} (ID: {card.Id}, Type: {card.Type})");
			}
		}
	}
}
