using QuickType;
using Godot;
using static fm.Function;

namespace fm
{
	public partial class GameLoop : Node
	{
		public MaoJogador MaoDoJogador;
		public Camera3D CameraHand;
		public Camera3D CameraField;
		public Camera3D CameraInimigo;
		public Node3D CameraPivot;
		public GameState _gameState;
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
				//GlobalUsings.Instance.SceneTransition(GlobalUsings.Instance.Story);		
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
			MaoDoJogador._inputState = InputState.HandSelection;
			
			GD.Print("Aguardando jogador selecionar uma carta...");

			bool acaoConcluida = false;
			FusionResult idEscolhido = null;

			while (!acaoConcluida)
			{
				var acao = await MaoDoJogador.AguardarAcaoAsync();

				if (acao.Type == PlayerActionType.SelectCard)
				{
					if (MaoDoJogador._cartasSelecionadasParaFusao.Count == 0)
					{
						MaoDoJogador._cartasSelecionadasParaFusao.Add(acao.Card);
					}

					var alvo = MaoDoJogador._cartasSelecionadasParaFusao.FirstOrDefault();
					bool isFaceDown = await MaoDoJogador._anim.AnimaCartaParaCentro(MaoDoJogador, alvo.carta.Id, alvo.carta.Name, MaoDoJogador._indiceSelecionado);

					if (MaoDoJogador._cartasSelecionadasParaFusao.Count == 1 && alvo.carta.IsSpell() && !isFaceDown)
					{
						GD.Print("usando spell");
						idEscolhido = await MaoDoJogador.ConfirmarInvocacaoNoCampo(true, alvo);
						acaoConcluida = true;
					}
					else
					{
						MaoDoJogador._inputState = InputState.FieldSelection;
						await MaoDoJogador.EntrarModoSelecaoCampo();

						bool slotSelecionado = false;
						while (!slotSelecionado)
						{
							var acaoCampo = await MaoDoJogador.AguardarAcaoAsync();
							if (acaoCampo.Type == PlayerActionType.SelectSlot)
							{
								idEscolhido = await MaoDoJogador.ConfirmarInvocacaoNoCampo();
								slotSelecionado = true;
								acaoConcluida = true;
							}
							else if (acaoCampo.Type == PlayerActionType.Cancel)
							{
								await MaoDoJogador.Tools.TransitionTo(CameraHand, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
								await MaoDoJogador.SairModoSelecaoCampo();
								MaoDoJogador._inputState = InputState.HandSelection;
								slotSelecionado = true;
							}
						}
					}
				}
				else if (acao.Type == PlayerActionType.Cancel)
				{
					if (MaoDoJogador._cartasSelecionadasParaFusao.Any())
					{
						await MaoDoJogador._anim.AnimaCartaParaMao(MaoDoJogador._cartasSelecionadasParaFusao.FirstOrDefault().carta.Id, MaoDoJogador._cartasSelecionadasParaFusao.FirstOrDefault().carta.Name, MaoDoJogador._indiceSelecionado, true);
						MaoDoJogador._cartasSelecionadasParaFusao.Clear();
					}
				}
			}

			if (idEscolhido != null)
			{
				int i = 1;
				var cardData = idEscolhido.MainCard;
				var car = MaoDoJogador.Tools.PegaSlotByMarker(idEscolhido.WorldPos);
				GD.Print("Logical pos meu monstro: " + idEscolhido.WorldPos);
				_gameState.CurrentPlayer.Field.placeCard(car, cardData, true, idEscolhido.IsFaceDown, _gameState.CurrentPlayer.IsEnemy);
				foreach(var item in idEscolhido.CardsUsed){
					_gameState.CurrentPlayer.DiscardCard(item.Id);
					i++;
				}
				await MaoDoJogador.Tools.TransitionTo(CameraField, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
				_gameState.Player1.Field.DrawFieldState();
				_gameState.Player2.Field.DrawFieldState();
			}
			_gameState.AdvancePhase();
		}
		
		private async Task ExecuteBattlePhaseAsync()
		{
			_gameState.CurrentPhase = TurnPhase.Battle;
			GD.Print("--- Battle Phase Iniciada ---");
			MaoDoJogador.DefineVisibilidade(false);
			MaoDoJogador.MaoControl.AnimateInterface(false);
			bool BP_Ativa = true;
			MaoDoJogador._inputState = InputState.BattleSelection;

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
				MaoDoJogador.PrepararSelecaoSlot(MaoDoJogador.FiltraSlot(inimigo: _gameState.CurrentPlayer.IsEnemy, aliado: true), _gameState.CurrentTurn == 1);
				var acaoAtaque = await MaoDoJogador.AguardarAcaoAsync();
				MaoDoJogador.EsconderSeletor();

				if (acaoAtaque.Type == PlayerActionType.EndTurn || acaoAtaque.Type == PlayerActionType.Cancel)
				{
					BP_Ativa = false;
					continue;
				}

				if (acaoAtaque.Type != PlayerActionType.SelectSlot)
				{
					continue;
				}

				string posAtaque = MaoDoJogador.LogicalPosition;
				var meuMonstro = _gameState.CurrentPlayer.Field.GetMonsterInZone(posAtaque);
				var minhaSpell = _gameState.CurrentPlayer.Field.GetFieldSpellTrap(posAtaque);

				GD.Print("Logical pos meu monstro: " + posAtaque);

				var intentAtacante = MaoDoJogador.Tools.DefineIntentCampo(meuMonstro?.Card ?? minhaSpell?.Card);

				if(meuMonstro != null && meuMonstro.HasAttackedThisTurn && intentAtacante != PlayerIntentEnum.InvalidIntent)
				{
					continue;
				}

				if(minhaSpell != null && intentAtacante == PlayerIntentEnum.SelectSpell)
				{
					GD.Print("no mundo perfeito ativamos spell do campo aqui");
					GD.Print("Selecionando alvo da spell...");

					if (minhaSpell.Card.IsEquip())
					{
						var slotsValidos = MaoDoJogador.FiltraSlot(aliadoM: true);
						MaoDoJogador.PrepararSelecaoSlot(slotsValidos);
						var acaoAlvoSpell = await MaoDoJogador.AguardarAcaoAsync();
						MaoDoJogador.EsconderSeletor();

						if (acaoAlvoSpell.Type == PlayerActionType.SelectSlot)
						{
							GD.Print($"Spell ativada em: {MaoDoJogador.LogicalPosition}");
						}						
					}
					continue;
				}

				await MaoDoJogador.Tools.TransitionTo(CameraInimigo, 0.4f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
				
				GD.Print("Escolha o alvo...");
				MaoDoJogador.PrepararSelecaoSlot(MaoDoJogador.SlotsCampoIni, _gameState.CurrentTurn == 1, true);
				var acaoAlvo = await MaoDoJogador.AguardarAcaoAsync();
				MaoDoJogador.EsconderSeletor();

				if (acaoAlvo.Type == PlayerActionType.SelectSlot)
				{
					string posAlvo = MaoDoJogador.LogicalPosition;
					GD.Print("slotalvo: " + posAlvo + " Logical pos inimigo monstro: " + posAlvo);
					try
					{
						var monstroInimigo = _gameState.OpponentPlayer.Field.GetMonsterInZone(posAlvo);
						await ResolverBatalha(meuMonstro, monstroInimigo);
						
					}catch(Exception e)
					{						
						GD.PrintErr($"Erro na Batalha: {e.Message}");
    					GD.PrintErr(e.StackTrace);
					}
				}

				await MaoDoJogador.Tools.TransitionTo(CameraField, 0.4f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
			}

			GD.Print("--- Battle Phase Encerrada ---");
			_gameState.AdvancePhase();
		}			

		public bool MonsterHasAttacked(string LogicalPos)
		{
			var meuMonstro = _gameState.CurrentPlayer.Field.GetMonsterInZone(LogicalPos);
			return meuMonstro != null ? meuMonstro.HasAttackedThisTurn : false;
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
			MaoDoJogador.MaoControl.AnimateInterface(true);
			MaoDoJogador.Tools.SwitchTurn(MaoDoJogador);
			MaoDoJogador.DefineVisibilidade(true);
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
