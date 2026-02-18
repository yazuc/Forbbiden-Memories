using QuickType;
using Godot;

namespace fm
{
	public partial class GameLoop : Node
	{
		public MaoJogador MaoDoJogador;
		public Camera3D CameraHand;
		public Camera3D CameraField;
		public Camera3D CameraInimigo;
		public Node3D CameraPivot;
		private GameState _gameState;
		private CardEffectManager _effectManager;
		private BattleSystem _battleSystem;
		private const int HAND_SIZE = 5;
		private const int STARTING_HAND = 5;
		private bool _isBattlePhaseActive = false;
		private TaskCompletionSource<bool> _battlePhaseEndSignal;

		public GameLoop(Player player1, Player player2, MaoJogador maoUI, Camera3D CameraHand, Camera3D CameraField, Camera3D CameraInimigo, Node3D CameraPivot)
		{
			_gameState = new GameState(player1, player2, maoUI);
			_effectManager = new CardEffectManager();
			_battleSystem = new BattleSystem();
			this.MaoDoJogador = maoUI;
			this.CameraHand = CameraHand;
			this.CameraField = CameraField;
			this.CameraInimigo = CameraInimigo;
			this.CameraPivot = CameraPivot;
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
				//GD.Print("started drawing");
				if (_gameState.Player1.HasCards())
				{
					//GD.Print("drawd");
					DrawCard(_gameState.Player1);                    
				}                
				if (_gameState.Player2.HasCards())
					DrawCard(_gameState.Player2);
			}
								
			_ = RunTurn();
		}

		public async Task RunTurn()
		{
			while(!_gameState.IsGameOver()){				
				MaoDoJogador.TransitionTo(CameraHand, 0.5f, true);
				if (_gameState.IsGameOver())
				{
					GD.Print("Game is already over!");
					return;
				}

				GD.Print($"\n=== Turn {_gameState.CurrentTurn}: {_gameState.CurrentPlayer.Name}'s Turn ===");

				// Draw Phase
				ExecuteDrawPhase();
				if (_gameState.IsGameOver()) break;

				// Main Phase 1
				await ExecuteMainPhase();				
				if (_gameState.IsGameOver()) break;

				// Battle Phase
				await ExecuteBattlePhase();
				if (_gameState.IsGameOver()) break;

				// End Phase
				ExecuteEndPhase();
				if (_gameState.IsGameOver()) break;

				// Switch player
				_gameState.SwitchPlayer();				
				MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());
				//CameraPivot.RotateY(Mathf.DegToRad(180));
				RotateCameraPivot180();
			}
			
			if (_gameState.IsGameOver())
			{
				GD.Print("Game is already over after while loop!");
				RotateCameraPivot180Slow();
				//return;
			}
		}

		private void ExecuteDrawPhase()
		{
			_gameState.CurrentPhase = TurnPhase.Draw;
			
			if(_gameState.CurrentPlayer.Hand.Count() == HAND_SIZE)
			{
				return;
			}
			while(_gameState.CurrentPlayer.Hand.Count < HAND_SIZE){
				if (_gameState.CurrentPlayer.HasCards() && _gameState.CurrentPlayer.Hand.Count < HAND_SIZE)
				{
					DrawCard(_gameState.CurrentPlayer);
					MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());
				}
				else
				{
					_gameState.EndGame(_gameState.OpponentPlayer);
				}											
			}
		}

		private async Task ExecuteMainPhase()
		{
			GD.Print($"--- {_gameState.CurrentPlayer.Name}'s {_gameState.CurrentPhase} ---");
			
			
			var handIds = _gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList();
 			MaoDoJogador.AtualizarMao(handIds);   
			
			GD.Print("Aguardando jogador selecionar uma carta...");
			Godot.Collections.Array<int> idEscolhido = await EsperarEscolhaDoJogadorArray(); 
			int i = 1;
			foreach(var item in idEscolhido){
				var cardData = CardDatabase.Instance.GetCardById((int)item);	
				if(i == idEscolhido.Count()){
					_gameState.CurrentPlayer.Field.placeCard(MaoDoJogador.PegaSlot(cardData.Id), cardData);					
				}
				_gameState.CurrentPlayer.DiscardCard(cardData.Id);
				GD.Print("PLAYER DO TURNO ATUAL: " + _gameState.CurrentPlayer.Name);
				i++;
			}		
			  			
			MaoDoJogador.TransitionTo(CameraField, 0.5f);				
			_gameState.AdvancePhase();
		}

		private async Task ExecuteBattlePhase()
		{
			_gameState.CurrentPhase = TurnPhase.Battle;
			GD.Print($"--- Battle Phase ---");
			_isBattlePhaseActive = true;

			try 
			{
				while (_isBattlePhaseActive)
				{												
					_battlePhaseEndSignal = new TaskCompletionSource<bool>();
					GD.Print("Aguardando ataque ou fim de fase (V)...");

					// 2. Preparação de Slots (Monstros + Magias se necessário)
					// Nota: Cuidado ao adicionar itens repetidamente em um loop while
					var slotsAtacantes = new Godot.Collections.Array<Marker3D>(MaoDoJogador.SlotsCampo);
					foreach(var item in MaoDoJogador.SlotsCampoST) {
						if (!slotsAtacantes.Contains(item)) slotsAtacantes.Add(item);
					}

					// 3. Inicia a seleção do monstro atacante
					Task<int> tarefaSelecao = MaoDoJogador.SelecionarSlotNoCampo(slotsAtacantes, _gameState.CurrentTurn == 1);

					// 4. Espera o primeiro evento: Seleção OU Pressão de 'V'
					await Task.WhenAny(tarefaSelecao, _battlePhaseEndSignal.Task);

					// Se o sinal de fechar a fase (V) foi disparado
					if (!_isBattlePhaseActive) break;

					int indexAtacante = await tarefaSelecao; 

					if (indexAtacante != -1)
					{
						// 5. Transição para o campo inimigo
						MaoDoJogador.TransitionTo(CameraInimigo, 0.3f);						

						GD.Print("Selecione o alvo inimigo...");
						Task<int> tarefaAlvo = MaoDoJogador.SelecionarSlotNoCampo(MaoDoJogador.SlotsCampoIni, _gameState.CurrentTurn == 1);
						await Task.WhenAny(tarefaAlvo, _battlePhaseEndSignal.Task);
						
						if (!_isBattlePhaseActive) break;

						int indexAlvo = await tarefaAlvo;
						if(indexAlvo == -1){
							MaoDoJogador.TransitionTo(CameraField, 0.2f);
						}
						
						if (indexAlvo != -1)
						{
							ResolverBatalha(indexAtacante, indexAlvo);							
						}
					}					
					// Limpa inputs para evitar que um 'Enter' confirme o próximo ataque sem querer
					Input.FlushBufferedEvents();
				}
			}
			catch (Exception e)
			{
				GD.PrintErr($"Erro crítico na Battle Phase: {e.Message}");
			}
			finally 
			{
				// 7. LIMPEZA TOTAL: Garante que o jogo não trave mesmo em caso de erro
				GD.Print("saimos da bp");
				_isBattlePhaseActive = false;
				MaoDoJogador.SairModoSelecaoCampo(); // Esconde o seletor 3D
				MaoDoJogador.CancelarSelecaoNoCampo(); // Resolve qualquer Task pendente
				
				MaoDoJogador.TransitionTo(CameraHand, 0.5f);				
				GD.Print("--- Battle Phase Ended & State Reset ---");
				_gameState.AdvancePhase();
				GD.Print($"--- {_gameState.CurrentPlayer.Name}'s {_gameState.CurrentPhase} ---");
			}
			return;
		}
		

		private bool ResolverBatalha(int atacanteIdx, int alvoIdx)
		{
			var meuMonstro = _gameState.CurrentPlayer.Field.GetMonsterInZone(atacanteIdx);
			var monstroInimigo = _gameState.OpponentPlayer.Field.GetMonsterInZone(alvoIdx);

			// Validação de segurança antes de acessar propriedades
			if (meuMonstro?.Card == null)
			{
				GD.PrintErr("[GameLoop] Batalha abortada: Um dos slots está vazio.");
				return false;
			}
			//_gameState.CurrentPlayer.Field.DrawFieldState();
			//_gameState.OpponentPlayer.Field.DrawFieldState();
			var battleResult = _battleSystem.ResolveBattle(meuMonstro, monstroInimigo, _gameState.OpponentPlayer);
			
			if(monstroInimigo != null){
				if(battleResult.AttackerDestroyed && battleResult.DefenderDestroyed)
				{
					MaoDoJogador.FinalizaNodoByCard(monstroInimigo.Card.Id);
					_gameState.OpponentPlayer.Field.RemoveMonster(alvoIdx);	
					MaoDoJogador.FinalizaNodoByCard(meuMonstro.Card.Id, _gameState.CurrentPlayer.IsEnemy);
					_gameState.CurrentPlayer.Field.RemoveMonster(atacanteIdx);			
				}
				if(battleResult.DefenderDestroyed){
					MaoDoJogador.FinalizaNodoByCard(monstroInimigo.Card.Id);
					_gameState.OpponentPlayer.Field.RemoveMonster(alvoIdx);		
					_gameState.OpponentPlayer.TakeDamage(battleResult.DamageDealt);				
				}
				if(battleResult.AttackerDestroyed){
					MaoDoJogador.FinalizaNodoByCard(meuMonstro.Card.Id, _gameState.CurrentPlayer.IsEnemy);
					_gameState.CurrentPlayer.Field.RemoveMonster(atacanteIdx);							
					_gameState.CurrentPlayer.TakeDamage(battleResult.DamageDealt);
				}
			}
			if(_gameState.OpponentPlayer.LifePoints <= 0){
				GD.Print("fim de jogo");
				_isBattlePhaseActive = false;
				_gameState.EndGame(_gameState.CurrentPlayer);
				_gameState.AdvancePhase();
				return true;
			}
			
			_gameState.CurrentPlayer.Field.DrawFieldState();
			_gameState.OpponentPlayer.Field.DrawFieldState();
			return false;			
		}
		
		public void HandleInput(InputEvent @event)
		{
			// Only care about input if we are in the Battle Phase
			if (!_isBattlePhaseActive) return;

			if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.V)
			{
				//GD.Print("'V' pressed: Ending Battle Phase.");
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
		
		private Task<Godot.Collections.Array<int>> EsperarEscolhaDoJogadorArray()
		{
			var tcs = new TaskCompletionSource<Godot.Collections.Array<int>>();

			// Use Godot.ConnectFlags para resolver o erro CS0103
			MaoDoJogador.Connect(MaoJogador.SignalName.CartaSelecionada, Callable.From<Godot.Collections.Array<int>>((id) => {
				tcs.TrySetResult(id);
			}), (uint)GodotObject.ConnectFlags.OneShot); 		
			return tcs.Task;
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
		
		
		public void RotateCameraPivot180()
		{
			var tween = CameraPivot.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Sine);

			tween.TweenProperty(
				CameraPivot,
				"rotation",
				CameraPivot.Rotation + new Vector3(0, Mathf.DegToRad(180), 0),
				0.8f
			);
		}
		
		public void RotateCameraPivot180Slow()
		{
			var tween = CameraPivot.CreateTween();
			tween.SetLoops();
			
			//tween.SetEase(Tween.EaseType.Linear);
			tween.SetTrans(Tween.TransitionType.Linear);
			
			tween.TweenProperty(
				CameraPivot,
				"rotation",
				CameraPivot.Rotation + new Vector3(0, Mathf.DegToRad(360), 0),
				30.0f
			).AsRelative();
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
