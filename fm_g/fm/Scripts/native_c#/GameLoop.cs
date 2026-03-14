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
			_ = RunTurn();
		}

		public async Task RunTurn()
		{
			while(!_gameState.IsGameOver()){				
				await MaoDoJogador.Tools.TransitionTo(CameraHand, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
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
				await ExecuteBattlePhaseAsync();
				if (_gameState.IsGameOver()) break;

				// End Phase
				ExecuteEndPhase();
				if (_gameState.IsGameOver()) break;

				// Switch player
				_gameState.SwitchPlayer();							
				await RotateCameraPivot180();
			}
			
			if (_gameState.IsGameOver())
			{
				GD.Print("Game is already over after while loop!");
				await MaoDoJogador.Tools.TransitionTo(CameraHand, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
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
			GD.Print($"--- {_gameState.CurrentPlayer.Name}'s {_gameState.CurrentPhase} Enemy? {_gameState.CurrentPlayer.IsEnemy}---");					
 			MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());   
			
			GD.Print("Aguardando jogador selecionar uma carta...");
			Godot.Collections.Array<int> idEscolhido = await MaoDoJogador.AguardarConfirmacaoJogadaAsync(); 			
			int i = 1;
			foreach(var item in idEscolhido){
				var cardData = CardDatabase.Instance.GetCardById((int)item);	
				if(i == idEscolhido.Count()){
					//arrumar quando colocar um nodo por cima de outro, deletar o anterior sempre
					var car = MaoDoJogador.Tools.PegaSlotByMarker(MaoDoJogador.LogicalPosition);
					_gameState.CurrentPlayer.Field.placeCard(car, cardData, true, false, _gameState.CurrentPlayer.IsEnemy);					
				}
				_gameState.CurrentPlayer.DiscardCard(cardData.Id);
				i++;
			}					
			MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());  
			await MaoDoJogador.Tools.TransitionTo(CameraField, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);			
			_gameState.Player1.Field.DrawFieldState();
			_gameState.Player2.Field.DrawFieldState();	
			SincronizaField();
			MaoDoJogador.Tools.PrintTodasInstancias();
			_gameState.AdvancePhase();
		}
		
		private async Task ExecuteBattlePhaseAsync()
		{
			_gameState.CurrentPhase = TurnPhase.Battle;
			GD.Print("--- Battle Phase Iniciada ---");
			MaoDoJogador.DefineVisibilidade(false);
			bool BP_Ativa = true;
			while (BP_Ativa)
			{
				if(_gameState.OpponentPlayer.LifePoints <= 0){
					GD.Print("fim de jogo");
					BP_Ativa = false;
					_gameState.EndGame(_gameState.CurrentPlayer);
					_gameState.AdvancePhase();
					break;
				}
				if(_gameState.CurrentPlayer.LifePoints <= 0)
				{
					GD.Print("fim de jogo");
					BP_Ativa = false;
					_gameState.EndGame(_gameState.Player2);
					_gameState.AdvancePhase();
					break;
				}
				
				GD.Print("Escolha um atacante...");
				int slotAtacante = await MaoDoJogador.SelecionarSlotAsync(MaoDoJogador.FiltraSlot(_gameState.CurrentPlayer.IsEnemy, true), _gameState.CurrentTurn == 1);								
				var meuMonstro = _gameState.CurrentPlayer.Field.GetMonsterInZone(MaoDoJogador.LogicalPosition);
				GD.Print("Logical pos meu monstro: " + MaoDoJogador.LogicalPosition);
				if(meuMonstro != null && meuMonstro.HasAttackedThisTurn && slotAtacante != -2)
				{
					continue;
				}
				if (slotAtacante == -2 || slotAtacante == -1) 
				{
					BP_Ativa = false; // Sai do loop se apertar V ou Cancelar na seleção de ataque
					continue;
				}				

				await MaoDoJogador.Tools.TransitionTo(CameraInimigo, 0.4f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
				
				GD.Print("Escolha o alvo...");
				int slotAlvo = await MaoDoJogador.SelecionarSlotAsync(MaoDoJogador.SlotsCampoIni, _gameState.CurrentTurn == 1, true);
				GD.Print("slotalvo: "+ slotAlvo +" Logical pos inimigo monstro: " + MaoDoJogador.LogicalPosition);
				if (slotAlvo != -1 && slotAlvo != -2)
				{
					try
					{
						var monstroInimigo = _gameState.OpponentPlayer.Field.GetMonsterInZone(MaoDoJogador.LogicalPosition);
						await ResolverBatalha(meuMonstro, monstroInimigo);
						
					}catch(Exception e)
					{						
						GD.PrintErr($"Erro na Batalha: {e.Message}");
    					GD.PrintErr(e.StackTrace);
					}
				}

				await MaoDoJogador.Tools.TransitionTo(CameraField, 0.4f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
				SincronizaField();
			}

			GD.Print("--- Battle Phase Encerrada ---");
			_gameState.AdvancePhase();
		}			

		private async Task<bool> ResolverBatalha(FieldMonster meuMonstro, FieldMonster? monstroInimigo)
		{
			
			MaoDoJogador.Tools.Flipa(meuMonstro.zoneName);

			if (meuMonstro?.Card == null)
			{
				GD.PrintErr("[GameLoop] Batalha abortada: SlotAtacante vazio.");
				return false;
			}
			
			var battleResult = _battleSystem.ResolveBattle(meuMonstro, monstroInimigo, _gameState.OpponentPlayer);		
			if(monstroInimigo != null){
				MaoDoJogador.Tools.Flipa(monstroInimigo.zoneName);
				if(!battleResult.AttackerDestroyed && !battleResult.DefenderDestroyed)
				{
					GD.Print("caiu no empate, ninguem destruido");
					_gameState.CurrentPlayer.TakeDamage(battleResult.DamageDealt);		
				}
				if(battleResult.AttackerDestroyed && battleResult.DefenderDestroyed)
				{
					//descobrir pq draw ta bugado
					GD.Print("caiu no empate, ambos destruídos");
					await MaoDoJogador._anim.AnimaBattle(MaoDoJogador, meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy);
					_gameState.OpponentPlayer.Field.RemoveMonster(monstroInimigo.zoneName);	
					_gameState.CurrentPlayer.Field.RemoveMonster(meuMonstro.zoneName);	
					return false;		
				}
				if(battleResult.DefenderDestroyed){	
					GD.Print("caiu no defensor destruído");				
					await MaoDoJogador._anim.AnimaBattle(MaoDoJogador, meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy);					
					_gameState.OpponentPlayer.Field.RemoveMonster(monstroInimigo.zoneName);
					_gameState.OpponentPlayer.TakeDamage(battleResult.DamageDealt);									
				}
				if(battleResult.AttackerDestroyed){
					GD.Print("caiu no atacante destruído");
					//ajustar para o meuMonstro ser destruido
					await MaoDoJogador._anim.AnimaBattle(MaoDoJogador, meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy);
					_gameState.CurrentPlayer.Field.RemoveMonster(meuMonstro.zoneName);							
					_gameState.CurrentPlayer.TakeDamage(battleResult.DamageDealt);
				}
			}else
			{
				GD.Print("caiu no ataque direto");
				await MaoDoJogador._anim.AnimaBattle(MaoDoJogador, meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy);
			}
			
			
			MaoDoJogador.STOP = false;
			_gameState.CurrentPlayer.Field.DrawFieldState();
			_gameState.OpponentPlayer.Field.DrawFieldState();
			return false;			
		}
			
		private void ExecuteEndPhase()
		{
			_gameState.AdvancePhase();
			GD.Print($"--- End Phase ---");			
		}

		private void DrawCard(Player player)
		{
			if (player.Deck.Count > 0)
			{
				var card = player.Deck.First();
				player.Hand.Add(card);
				player.Deck.RemoveAt(0);
				player.DeckNro.Text = player.Deck.Count().ToString();				
			}
		}

		public bool IsGameOver() => _gameState.IsGameOver();

		public GameState GetGameState() => _gameState;					
		
		public async Task RotateCameraPivot180()
		{			
			MaoDoJogador.DefineVisibilidade(false);
			await MaoDoJogador.Tools.TransitionTo(CameraHand, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
			var tween = CameraPivot.CreateTween();
			
			tween.TweenProperty(
				CameraPivot,
				"rotation",
				CameraPivot.Rotation + new Vector3(0, Mathf.DegToRad(180), 0),
				1f
			);
			await ToSignal(tween, Tween.SignalName.Finished);
			MaoDoJogador.SwitchTurn();
			MaoDoJogador.DefineVisibilidade(true);
		}
		
		public void SincronizaField()
		{
			List<(string carta,bool defesa)> tuple = MaoDoJogador.Tools.DevolvePosicoes();
			foreach(var item in tuple)
			{
				_gameState.Player1.Field.BotaDeLadinho(item.carta, item.defesa);
				_gameState.Player2.Field.BotaDeLadinho(item.carta, item.defesa);
				GD.Print($"carta: {item.carta} - de ladinho? {item.defesa}");
			}
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
	}
}
