using QuickType;
using Godot;
using static fm.Function;

namespace fm
{
	public partial class GameLoop : Node
	{
		public MaoJogador MaoDoJogador;
		public MaoInimigo MaoDoInimigo;
		public Camera3D CameraHand;
		public Camera3D CameraField;
		public Camera3D CameraInimigo;
		public Node3D CameraPivot;
		public GameState _gameState;
		private CardEffectManager _effectManager;
		private AIPlayer _aiPlayer;
		private BattleSystem _battleSystem;
		private const int HAND_SIZE = 5;
		private bool _isBattlePhaseActive = false;

		public GameLoop(Player player1, Player player2, MaoJogador maoUI, MaoInimigo maoInimigo, Camera3D CameraHand, Camera3D CameraField, Camera3D CameraInimigo, Node3D CameraPivot)
		{
			_gameState = new GameState(player1, player2, maoUI, maoInimigo);
			_effectManager = new CardEffectManager();
			_battleSystem = new BattleSystem();
			this.MaoDoJogador = maoUI;
			this.MaoDoInimigo = maoInimigo;
			this.CameraHand = CameraHand;
			this.CameraField = CameraField;
			this.CameraInimigo = CameraInimigo;
			this.CameraPivot = CameraPivot;
			this._aiPlayer = new AIPlayer(AIPlayer.DifficultyLevel.Hard);
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
 			await MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());   
			_gameState.CurrentPhase = TurnPhase.Main1;
			if(_gameState.CurrentPlayer.IsEnemy)
			{
				await MaoDoJogador.ChangeState(InputState.None);
				MaoDoJogador._selecionandoLocal = true;				
				if(_aiPlayer.SelectCardToPlay(_gameState.CurrentPlayer, _gameState) is AIMove cardToPlay)
				{
					var result = await MaoDoInimigo.RealizarJogadaIA(cardToPlay, cardToPlay.FaceUP);
					_gameState.RealizaMainPhase(result);
					await MaoDoJogador.Tools.TransitionTo(CameraField, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);			
				}		
				MaoDoJogador._selecionandoLocal = false;
			}
			else
			{
				FusionResult idEscolhido = await MaoDoJogador.AguardarConfirmacaoJogadaAsync();
				_gameState.RealizaMainPhase(idEscolhido); 															
				await MaoDoJogador.Tools.TransitionTo(CameraField, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);			
			}
			_gameState.AdvancePhase();
		}
		
		private async Task ExecuteBattlePhaseAsync()
		{
			bool BP_Ativa = true;
			if(_gameState.CurrentPlayer.IsEnemy)
			{
				MaoDoJogador.STOP = BP_Ativa;
				while (_gameState.CurrentPlayer.Field.HasBattaleReadyMonster())
				{
					GD.Print("Vez da AI. Realizando jogada de batalha...");
					var ret = _aiPlayer.SelectAttack(_gameState.CurrentPlayer, _gameState.OpponentPlayer, _gameState);
					await MaoDoInimigo.AtualizarPosicaoSeletor3DInimigo(MaoDoInimigo.SlotsCampoIni, ret.DefenderZone);
					var monstroAliado = _gameState.CurrentPlayer.Field.GetMonsterInZone(ret.AttackerZone);
					if(monstroAliado == null) continue;
					await MaoDoInimigo.AtualizarPosicaoSeletor3DInimigo(MaoDoInimigo.SlotsCampo, ret.AttackerZone);
					await MaoDoInimigo.Tools.TransitionTo(CameraInimigo, 0.4f, MaoDoInimigo._transitionCam, false);			
					Task.Delay(500).Wait();
					var monstroInimigo = _gameState.OpponentPlayer.Field.GetMonsterInZone(ret.DefenderZone);
					if (!monstroAliado.HasAttackedThisTurn)
					{
						await ResolverBatalha(monstroAliado, monstroInimigo);
						await MaoDoInimigo.Tools.TransitionTo(CameraField, 0.4f, MaoDoInimigo._transitionCam, false);						
					}
				}
				BP_Ativa = false;		
				MaoDoInimigo.Seletor3D.Visible = false;			
			}
			_gameState.CurrentPhase = TurnPhase.Battle;
			GD.Print("--- Battle Phase Iniciada ---");
			await MaoDoJogador.MaoControl.AnimateInterface(false);
			MaoDoJogador.DefineVisibilidade(false);
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
				PlayerIntention slotAtacante = await MaoDoJogador.SelecionarSlotTAsync(MaoDoJogador.FiltraSlot(inimigo: _gameState.CurrentPlayer.IsEnemy, aliado: true), _gameState.CurrentTurn == 1);

				var meuMonstro = _gameState.CurrentPlayer.Field.GetMonsterInZone(slotAtacante.WorldPos);
				var minhaSpell = _gameState.CurrentPlayer.Field.GetFieldSpellTrap(slotAtacante.WorldPos);

				GD.Print("Logical pos meu monstro: " + slotAtacante.WorldPos);
				if (slotAtacante.EndTurn()) 
				{
					BP_Ativa = false; // Sai do loop se apertar V ou Cancelar na seleção de ataque
					continue;
				}				
				if(!slotAtacante.ValidIntention()) continue;
				if(meuMonstro != null && meuMonstro.HasAttackedThisTurn && slotAtacante.ValidIntention())
				{
					continue;
				}
				if(minhaSpell != null && slotAtacante.SelectSpell())
				{
					GD.Print("no mundo perfeito ativamos spell do campo aqui");
					GD.Print("Selecionando alvo da spell...");

					var equip3d = MaoDoJogador.Tools.PegaNodoCarta3d(slotAtacante.WorldPos);
					if (minhaSpell.Card != null && minhaSpell.Card.IsEquip())
					{
						var slotsValidos = MaoDoJogador.FiltraSlot(aliadoM: true);
						if(equip3d != null && equip3d.carta.IsEquip())
						{						
							var alvoSpell = await MaoDoJogador.SelecionarSlotTAsync(slotsValidos);
							if (alvoSpell.ValidIntention())
							{
								GD.Print($"Spell ativada em: {alvoSpell.WorldPos}");				
								if(equip3d != null && equip3d.carta.IsEquip())
								{
									var equipSelecionado = MaoDoJogador.CriarCartaFusao(equip3d.CardUI);
									await MaoDoJogador.ConfirmarSpellNoCampo(card:equipSelecionado);							
								}
							}						
						}
					}
					if(equip3d != null && equip3d.carta.IsSpell())
					{
						await MaoDoJogador.Tools.TransitionTo(CameraHand, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
						await equip3d.CardUI.AtivaSpellAnimation(MaoDoJogador._anim.ScrenCenter());
						await MaoDoJogador.Tools.TransitionTo(CameraField, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
					}
					equip3d.QueueFree();
					continue;
				}

				await MaoDoJogador.Tools.TransitionTo(CameraInimigo, 0.4f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);
				
				GD.Print("Escolha o alvo...");
				PlayerIntention slotAlvo = await MaoDoJogador.SelecionarSlotTAsync(MaoDoJogador.SlotsCampoIni, _gameState.CurrentTurn == 1, true);
				GD.Print("slotalvo: "+ slotAlvo +" Logical pos inimigo monstro: " + slotAlvo.WorldPos);
				if (slotAlvo.ValidIntention())
				{
					try
					{
						var monstroInimigo = _gameState.OpponentPlayer.Field.GetMonsterInZone(slotAlvo.WorldPos);
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
			MaoDoJogador.STOP = true;

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
					await MaoDoJogador._anim.AnimaBattle(meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy, MaoDoJogador._transitionCam);
					_gameState.OpponentPlayer.Field.RemoveMonster(monstroInimigo.zoneName);	
					_gameState.CurrentPlayer.Field.RemoveMonster(meuMonstro.zoneName);	
					return false;		
				}
				if(battleResult.DefenderDestroyed){	
					GD.Print("caiu no defensor destruído");				
					await MaoDoJogador._anim.AnimaBattle(meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy, MaoDoJogador._transitionCam);					
					_gameState.OpponentPlayer.Field.RemoveMonster(monstroInimigo.zoneName);
					_gameState.OpponentPlayer.TakeDamage(battleResult.DamageDealt);									
				}
				if(battleResult.AttackerDestroyed){
					GD.Print("caiu no atacante destruído");
					//ajustar para o meuMonstro ser destruido
					await MaoDoJogador._anim.AnimaBattle(meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy, MaoDoJogador._transitionCam);
					_gameState.CurrentPlayer.Field.RemoveMonster(meuMonstro.zoneName);							
					_gameState.CurrentPlayer.TakeDamage(battleResult.DamageDealt);
				}
			}else
			{
				GD.Print("caiu no ataque direto");
				await MaoDoJogador._anim.AnimaBattle(meuMonstro, monstroInimigo, battleResult, _gameState.CurrentPlayer.IsEnemy, MaoDoJogador._transitionCam);
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
			await MaoDoJogador.MaoControl.AnimateInterface(true);
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
