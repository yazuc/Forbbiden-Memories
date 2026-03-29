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
		private bool _isBattlePhaseActive = false;

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
			await MaoDoJogador.AtualizarMao(_gameState.CurrentPlayer.Hand.Select(x => x.Id).ToList());

			FusionResult idEscolhido = null;

			if (_gameState.CurrentPlayer.IsEnemy)
			{
				AIEngine aiEngine = new AIEngine();
				AIDecision decision = aiEngine.GetBestMove(_gameState.CurrentPlayer.Hand, _gameState.OpponentPlayer.Field, _gameState.CurrentPlayer.Field);

				if (decision != null && decision.FusionResult != null)
				{
					idEscolhido = decision.FusionResult;
					idEscolhido.IsFaceDown = decision.IsFaceDown;

					var tipo = idEscolhido.MainCard.Type;
					bool isSpellTrap = tipo == CardTypeEnum.Spell || tipo == CardTypeEnum.Trap || tipo == CardTypeEnum.Equipment || tipo == CardTypeEnum.Ritual;

					var slots = MaoDoJogador.DefineSlotagem(tipo);
					var slotDestino = slots[decision.TargetZoneIndex];

					idEscolhido.WorldPos = slotDestino.Name.ToString();

					MaoDoJogador._cartasSelecionadasParaFusao.Clear();

					foreach (var cardId in decision.CardIds)
					{
						var handIndex = _gameState.CurrentPlayer.Hand.FindIndex(c => c.Id == cardId);
						if (handIndex >= 0)
						{
							var uiCard = MaoDoJogador.MaoControl.GetCarta(handIndex);
							if (uiCard != null)
							{
								MaoDoJogador._cartasSelecionadasParaFusao.Add(uiCard);
							}
						}
					}

					if (MaoDoJogador._cartasSelecionadasParaFusao.Count > 0)
					{
						var firstCard = MaoDoJogador._cartasSelecionadasParaFusao.First();

						// Find index using a simpler method or loop
						int handIdx = 0;
						for (int j = 0; j < MaoDoJogador.MaoControl.CartasNaMaoCount(); j++)
						{
							if (MaoDoJogador.MaoControl.GetCarta(j) == firstCard)
							{
								handIdx = j;
								break;
							}
						}

						await MaoDoJogador._anim.AnimaCartaParaCentro(MaoDoJogador, firstCard.carta.Id, firstCard.carta.Name, handIdx, _gameState.CurrentPlayer.IsEnemy);
						MaoDoJogador._tcsFaceDown?.TrySetResult(idEscolhido.IsFaceDown);

						if (MaoDoJogador._cartasSelecionadasParaFusao.Count > 1)
						{
							await MaoDoJogador._anim.AnimaFusao(MaoDoJogador, idEscolhido);
						}
					}

					var carta3dfield = MaoDoJogador.Tools.PegaNodoCarta3d(slotDestino.Name);

					if (carta3dfield == null && !isSpellTrap)
					{
						await MaoDoJogador.Instancia3D(slotDestino, idEscolhido.MainCard);
					}
					else if (carta3dfield != null)
					{
						carta3dfield.UpdateCard(idEscolhido.MainCard);
					}

					MaoDoJogador.CleanUpCrew();
				}
				else
				{
					GD.Print("AI could not make a decision.");
					_gameState.AdvancePhase();
					return;
				}
			}
			else
			{
				GD.Print("Aguardando jogador selecionar uma carta...");
				idEscolhido = await MaoDoJogador.AguardarConfirmacaoJogadaAsync();
			}

			int i = 1;
			var cardData = idEscolhido.MainCard;	
			//arrumar quando colocar um nodo por cima de outro, deletar o anterior sempre
			var car = MaoDoJogador.Tools.PegaSlotByMarker(idEscolhido.WorldPos);
			GD.Print("Logical pos meu monstro: " + idEscolhido.WorldPos);
			_gameState.CurrentPlayer.Field.placeCard(car, cardData, true, idEscolhido.IsFaceDown, _gameState.CurrentPlayer.IsEnemy);								

			if (idEscolhido.CardsUsed != null)
			{
				foreach(var item in idEscolhido.CardsUsed){
					_gameState.CurrentPlayer.DiscardCard(item.Id);
					i++;
				}
			}

			await MaoDoJogador.Tools.TransitionTo(CameraField, 0.5f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);			

			_gameState.Player1.Field.DrawFieldState();
			_gameState.Player2.Field.DrawFieldState();	
			_gameState.AdvancePhase();
		}
		
		private async Task ExecuteBattlePhaseAsync()
		{
			_gameState.CurrentPhase = TurnPhase.Battle;
			GD.Print("--- Battle Phase Iniciada ---");
			MaoDoJogador.DefineVisibilidade(false);
			await MaoDoJogador.MaoControl.AnimateInterface(false);
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
				
				if (_gameState.CurrentPlayer.IsEnemy)
				{
					if (_gameState.CurrentTurn == 1)
					{
						BP_Ativa = false;
						continue;
					}

					AIEngine aiEngine = new AIEngine();
					BattleDecision decision = aiEngine.GetBestBattleDecision(_gameState.CurrentPlayer, _gameState.OpponentPlayer);

					if (decision.EndTurn)
					{
						BP_Ativa = false;
						continue;
					}

					if (decision.SwitchToDefense)
					{
						var slots = MaoDoJogador.FiltraSlot(inimigo: true, inimigoM: true);
						var slotDestino = slots[decision.DefenseZoneIndex];
						var pegou = MaoDoJogador.Tools.PegaNodoCarta3d(slotDestino.Name);
						if (pegou is Carta3d nodo)
						{
							nodo.Defesa = true;
							_gameState.CurrentPlayer.Field.BotaDeLadinho(nodo.markerName, true);
							pegou.Rotation = new Vector3(0, -1.5707964f, 0); // Defesa (inimigo)
						}
						continue;
					}

					if (decision.HasAttack)
					{
						var mySlots = MaoDoJogador.FiltraSlot(inimigo: true, inimigoM: true);
						var attSlot = mySlots[decision.AttackerZoneIndex];
						var meuMonstro = _gameState.CurrentPlayer.Field.GetMonsterInZone(attSlot.Name);

						FieldMonster monstroInimigo = null;
						if (!decision.IsDirectAttack)
						{
							var opSlots = MaoDoJogador.FiltraSlot(aliado: true, aliadoM: true);
							var defSlot = opSlots[decision.DefenderZoneIndex];
							monstroInimigo = _gameState.OpponentPlayer.Field.GetMonsterInZone(defSlot.Name);
						}

						await Task.Delay(1000); // Wait for simulation effect

						try
						{
							await ResolverBatalha(meuMonstro, monstroInimigo);
						}
						catch (Exception e)
						{
							GD.PrintErr($"Erro na Batalha da IA: {e.Message}");
							GD.PrintErr(e.StackTrace);
						}
						continue;
					}
				}
				else
				{
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

						if (minhaSpell.Card != null && minhaSpell.Card.IsEquip())
						{
							var slotsValidos = MaoDoJogador.FiltraSlot(aliadoM: true);
							var alvoSpell = await MaoDoJogador.SelecionarSlotTAsync(slotsValidos);

							if (alvoSpell.ValidIntention())
							{
								GD.Print($"Spell ativada em: {alvoSpell.WorldPos}");
								var equip3d = MaoDoJogador.Tools.PegaNodoCarta3d(slotAtacante.WorldPos);
								var equipSelecionado = MaoDoJogador.CriarCartaFusao(equip3d.carta);
								await MaoDoJogador.ConfirmarSpellNoCampo(card:equipSelecionado);
								equip3d.QueueFree();
							}
						}
						continue;
					}

					await MaoDoJogador.Tools.TransitionTo(CameraInimigo, 0.4f, MaoDoJogador._transitionCam, MaoDoJogador.STOP);

					GD.Print("Escolha o alvo...");
					PlayerIntention slotAlvo = await MaoDoJogador.SelecionarSlotTAsync(MaoDoJogador.SlotsCampoIni, _gameState.CurrentTurn == 1, true);
					GD.Print("slotalvo: "+ slotAlvo +" Logical pos inimigo monstro: " + MaoDoJogador.LogicalPosition);
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
