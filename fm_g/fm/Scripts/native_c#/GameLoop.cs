using QuickType;
using Godot;

namespace fm
{
	public class GameLoop
	{
		public MaoJogador MaoDoJogador;
		public Camera3D CameraHand;
		public Camera3D CameraField;
		public Camera3D CameraInimigo;
		private GameState _gameState;
		private CardEffectManager _effectManager;
		private const int HAND_SIZE = 5;
		private const int STARTING_HAND = 5;
		private bool _isBattlePhaseActive = false;
		private TaskCompletionSource<bool> _battlePhaseEndSignal;

		public GameLoop(Player player1, Player player2, MaoJogador maoUI, Camera3D CameraHand, Camera3D CameraField, Camera3D CameraInimigo)
		{
			_gameState = new GameState(player1, player2);
			_effectManager = new CardEffectManager();
			this.MaoDoJogador = maoUI;
			this.CameraHand = CameraHand;
			this.CameraField = CameraField;
			this.CameraInimigo = CameraInimigo;
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
			while(true){
				CameraHand.Current = true;
				CameraField.Current = false;
				CameraInimigo.Current = false;
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
				await ExecuteBattlePhase();
				if (_gameState.IsGameOver()) return;

				// End Phase
				ExecuteEndPhase();

				// Switch player
				_gameState.SwitchPlayer();				
				MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());
			}
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
				MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());
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
			
			var handIds = _gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList();
 			MaoDoJogador.AtualizarMao(handIds);   
			
			GD.Print("Aguardando jogador selecionar uma carta...");
			int idEscolhido = await EsperarEscolhaDoJogador(); 		
			var cardData = CardDatabase.Instance.GetCardById(idEscolhido);	
			_gameState.CurrentPlayer.Field.placeCard(cardData);
			_gameState.CurrentPlayer.Field.DrawFieldState();
			
			CameraHand.Current = false;
			CameraField.Current = true;  
					
			_gameState.AdvancePhase();
		}

		private async Task ExecuteBattlePhase()
		{
			_gameState.CurrentPhase = TurnPhase.Battle; // Ou AdvancePhase()
			GD.Print($"--- Battle Phase ---");
			_isBattlePhaseActive = true;

			while (_isBattlePhaseActive)
			{
				CameraHand.Current = false;
				CameraField.Current = true;
				CameraInimigo.Current = false;
				// 1. Criamos o sinal para o fim da fase
				_battlePhaseEndSignal = new TaskCompletionSource<bool>();

				GD.Print("Aguardando ataque ou fim de fase...");

				// 2. Criamos a tarefa de seleção de monstro
				Task<int> tarefaSelecao = MaoDoJogador.SelecionarSlotNoCampo(MaoDoJogador.SlotsCampo);

				// 3. A mágica: Esperamos o PRIMEIRO que terminar: 
				// Ou o jogador seleciona um monstro, ou ele aperta V.
				Task tarefaTerminada = await Task.WhenAny(tarefaSelecao, _battlePhaseEndSignal.Task);

				// Se o sinal de fechar a fase (V) ganhou a corrida
				if (!_isBattlePhaseActive) 
				{
					MaoDoJogador.CancelarSelecaoNoCampo();
					break; 
				}

				// Se a seleção ganhou a corrida, processamos o ataque
				int indexAtacante = await tarefaSelecao; 

				if (indexAtacante != -1)
				{
					CameraHand.Current = false;
					CameraField.Current = false;
					CameraInimigo.Current = true;
					// Lógica de seleção de alvo inimigo (mesmo padrão)
					GD.Print("Selecione o alvo inimigo...");
					Task<int> tarefaAlvo = MaoDoJogador.SelecionarSlotNoCampo(MaoDoJogador.SlotsCampoIni);
					
					// Também permitimos cancelar o alvo apertando V
					await Task.WhenAny(tarefaAlvo, _battlePhaseEndSignal.Task);
					
					if (!_isBattlePhaseActive) break;

					int indexAlvo = await tarefaAlvo;
					if (indexAlvo != -1)
					{
						ResolverBatalha(indexAtacante, indexAlvo);
						await Task.Delay(500);
					}
				}
			}
			GD.Print("--- Battle Phase Ended ---");
		}

		private void ResolverBatalha(int atacanteIdx, int alvoIdx)
		{
			GD.Print($"Batalha: Meu monstro no slot {atacanteIdx} vs Inimigo no slot {alvoIdx}");
			// Aqui tu chamas o BattleSystem para comparar ATK/DEF e subtrair LifePoints
		}
		
		public void HandleInput(InputEvent @event)
		{
			// Only care about input if we are in the Battle Phase
			if (!_isBattlePhaseActive) return;

			if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.V)
			{
				GD.Print("'V' pressed: Ending Battle Phase.");
				_isBattlePhaseActive = false;
				
				// 1. Interrompe qualquer loop de seleção visual que esteja rodando na UI
				MaoDoJogador.CancelarSelecaoNoCampo();
				
				// Resolve the task so the 'await' in the while loop finishes
				_battlePhaseEndSignal?.TrySetResult(true);
			}
		}

		private void ExecuteEndPhase()
		{
			_gameState.AdvancePhase();
			GD.Print($"--- End Phase ---");
			// TODO: Implement end phase effects
			// - Card effects that trigger at end of turn
			// - Hand size check (max 6 cards)
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
			GD.Print("retornou");
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
