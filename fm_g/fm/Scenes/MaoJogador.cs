using Godot;
using QuickType;
using System;
using static fm.Function;

namespace fm{	
	public partial class MaoJogador : Node2D
	{
		[Export] public PackedScene CartaCena;
		public Node2D IndicadorTriangulo;
		public Node2D IndicadorSeta;
		[Export] public Camera3D CameraHand;
		[Export] public Camera3D CameraField;
		[Export] public Camera3D CameraInimigo;
		public Camera3D _transitionCam;
		public Godot.Collections.Array<Marker3D> SlotsCampo = new();
		public Godot.Collections.Array<Marker3D> SlotsCampoST = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoIni = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoSTIni = new ();
		public Godot.Collections.Array<Marker3D> Slots = new ();	
		public bool STOP {get;set;}		
		private TaskCompletionSource<FusionResult> _tcsCarta;
		private TaskCompletionSource<PlayerIntention> _tcsSlot;
		public TaskCompletionSource<bool> _tcsFaceDown;
		bool IsFaceDown = false;
		private bool _bloquearNavegaçãoManual = false;
		private Node3D _instanciaSeletor = null;
		public int _indiceSelecionado = 0;	
		public int _indiceCampoSelecionado = 0;
		public bool _selecionandoLocal = false; // Estado para saber se estamos escolhendo onde colocar a carta
		public List<CardUi> _cartasSelecionadasParaFusao = new List<CardUi>();
		private List<Node3D> _cartasInstanciadas = new List<Node3D>();
		private bool _processandoInput = false;		
		public List<int> IDFusao = new List<int>();
		public Vector2 lastPos = Vector2.Zero;
		private Godot.Collections.Array<Marker3D> _slots;
		public bool _camIni, PrimeiroTurno;
		public Mao MaoControl {get;set;}
		public IndicadorSeta indicadorSetaEsquerda{get;set;}
		public IndicadorSeta indicadorSetaDireita{get;set;}
		private InputState _inputState = InputState.HandSelection;
		public GameLoop gameLoop {get;set;}
		public AnimationP _anim;
		public Helper Tools;
		public CardUi RefFusao;
		public Campo Campo;
		public override void _Ready()
		{
			_transitionCam = new Camera3D();
			AddChild(_transitionCam);
			IndicadorTriangulo = GetNode<Node2D>("../IndicadorTriangulo");
			MaoControl = GetNode<Mao>("../CameraPivot/CameraHand/Control/InterfaceDuelo/Mao");	
			Campo = GetNode<Campo>("../Board");	
			_instanciaSeletor = GetNode<Node3D>("../Seletor");
			_anim = GetNode<AnimationP>("../AnimationP");
			_anim._cartasSelecionadasParaFusao = _cartasSelecionadasParaFusao;
			Tools = GetNode<Helper>("../Helper");
			Tools._cartasInstanciadas = _cartasInstanciadas;
		}

		public override void _Process(double delta)
		{			
			float speed = 8.0f;
			Vector2 newScale = Scale;
			newScale.Y = Mathf.Sin(Time.GetTicksMsec() * 0.001f * speed);
			if(IsInstanceValid(IndicadorTriangulo))
			IndicadorTriangulo.Scale = newScale;		
		}			

		public async Task CancelarSelecaoNoCampo()
		{
						
			if (_cartasSelecionadasParaFusao.Any() && _cartasSelecionadasParaFusao.Count() == 1) {
				await _anim.AnimaCartaParaMao(_indiceSelecionado, true);
			}			
			foreach(var item in _cartasSelecionadasParaFusao)
			{
				if(IsInstanceValid(item))
				item.EscondeLabel();
			}						
			_cartasSelecionadasParaFusao.Clear();
		}
								
		public async Task ConfirmarInvocacaoNoCampo(bool ativaDireto = false, CardUi? card = null)
		{			
			await ChangeState(InputState.None);
			var slotDestino = _slots[_indiceCampoSelecionado];
			var carta3dfield = Tools.PegaNodoCarta3d(slotDestino.Name);

			if(carta3dfield != null && _inputState.Equals(InputState.None))
			{
				RefFusao = CriarCartaFusao(carta3dfield.CardUI);	
				var camera = GetViewport().GetCamera3D();
				Vector2 screenPos = camera.UnprojectPosition(carta3dfield.GlobalPosition);
				RefFusao.Position = screenPos;						
				_cartasSelecionadasParaFusao.Insert(0, RefFusao);
			}

			var ids = new List<int>();												
			ids.AddRange(_cartasSelecionadasParaFusao.Select(x => x.carta.Id));
			var resultadoFusao = ProcessChain(string.Join(",", ids), carta3dfield?.carta);		
			
			if(_cartasSelecionadasParaFusao.Count() > 1)
			{
				await Tools.TransitionTo(CameraHand, 0.5f, _transitionCam, STOP);
				await _anim.AnimaFusao(resultadoFusao);
			}
				
			resultadoFusao.IsFaceDown = IsFaceDown;
			bool summon = true;	
			
			if (resultadoFusao != null)
			{								
				var tipo = resultadoFusao.MainCard.Type;
				if(ativaDireto || resultadoFusao.MainCard.IsSpellTrap() && !ativaDireto && !IsFaceDown)
				{
					CartaSTAction(card, resultadoFusao);
					return;
				}

				if(_cartasSelecionadasParaFusao.Count() == 1){
					slotDestino = DefineSlotagem(tipo)[_indiceCampoSelecionado];				
				}				
				if(_cartasSelecionadasParaFusao.Count() > 1)
				{
					slotDestino = DefineSlotagem(tipo)[_indiceCampoSelecionado];				
					summon = tipo != CardTypeEnum.Spell && tipo != CardTypeEnum.Trap && tipo != CardTypeEnum.Equipment;
				}

				resultadoFusao.WorldPos = slotDestino.Name;

				if (summon)
				{
					if(carta3dfield != null)
					{
						MaoControl.SeletorGuardian.Setup(resultadoFusao.MainCard);
						carta3dfield.UpdateCard(resultadoFusao.MainCard);
						STOP = true;
						await MaoControl.AllowGuardian(resultadoFusao.MainCard);
						STOP = false;
						gameLoop._gameState.CurrentPlayer.Field.UpdateMonster(resultadoFusao.MainCard, slotDestino.Name);
					}
					else
					{
						await Tools.TransitionTo(CameraHand, 0.5f, _transitionCam, STOP);						
						await Instancia3D(slotDestino, resultadoFusao.MainCard);			
					}
					CleanUpCrew();
				}
				
				_cartasSelecionadasParaFusao.Clear();
				await SairModoSelecaoCampo();
				_bloquearNavegaçãoManual = false;
				_tcsCarta?.TrySetResult(resultadoFusao);
			}
		}
		//não ativa uma spell do campo ainda, só equipa um monstro
		public async Task ConfirmarSpellNoCampo(CardUi? card = null)
		{			
			
			var slotDestino = _slots[_indiceCampoSelecionado];
			var carta3dfield = Tools.PegaNodoCarta3d(slotDestino.Name, gameLoop._gameState.CurrentPlayer.Field.GetCardInZone(slotDestino.Name));

			var scene = GD.Load<PackedScene>("res://Menu/Password/card_ui.tscn");
			if(carta3dfield != null)
			{
				RefFusao = CriarCartaFusao(carta3dfield.CardUI);
				_cartasSelecionadasParaFusao.Insert(0, RefFusao);
			}
			if(card != null)
			{
				_cartasSelecionadasParaFusao.Insert(1, card);
			}

			var ids = new List<int>();												
			ids.AddRange(_cartasSelecionadasParaFusao.Select(x => x.carta.Id));
			var resultadoFusao = ProcessChain(string.Join(",", ids), carta3dfield?.carta);		
			
			if(_cartasSelecionadasParaFusao.Count() > 1)
			{
				await Tools.TransitionTo(CameraHand, 0.5f, _transitionCam, STOP);
				await _anim.AnimaFusao(resultadoFusao);
				await Tools.TransitionTo(CameraField, 0.5f, _transitionCam, STOP);
			}
				
			resultadoFusao.IsFaceDown = IsFaceDown;
			bool summon = true;	
			
			if (resultadoFusao != null)
			{								
				var tipo = resultadoFusao.MainCard.Type;

				if(_cartasSelecionadasParaFusao.Count() == 1){
					slotDestino = DefineSlotagem(tipo)[_indiceCampoSelecionado];				
				}				
				if(_cartasSelecionadasParaFusao.Count() > 1)
				{
					slotDestino = DefineSlotagem(tipo)[_indiceCampoSelecionado];				
					summon = tipo != CardTypeEnum.Spell && tipo != CardTypeEnum.Trap && tipo != CardTypeEnum.Equipment;
				}

				resultadoFusao.WorldPos = slotDestino.Name;

				if (summon)
				{
					if(carta3dfield != null)
					{
						carta3dfield.UpdateCard(resultadoFusao.MainCard);
						gameLoop._gameState.CurrentPlayer.Field.UpdateMonster(resultadoFusao.MainCard, slotDestino.Name);
					}
					CleanUpCrew();
				}
				
				_cartasSelecionadasParaFusao.Clear();
				await SairModoSelecaoCampo();
				_bloquearNavegaçãoManual = false;
				_tcsCarta?.TrySetResult(resultadoFusao);
			}
		}

		public async Task CartaSTAction(CardUi card, FusionResult resultadoFusao)
		{
			_bloquearNavegaçãoManual = true;

			card = _anim.GetChildCount() > 0 ? _anim.GetChild<CardUi>(0) : card;

			if(card != null)
			{						
				await card.AtivaSpellAnimation(_anim.ScrenCenter());				
				if (card.carta.IsField())
				{
					Campo.SetEstadoCampo(card.carta.Name);
				}								
				_bloquearNavegaçãoManual = false;
				_selecionandoLocal = false;
				_cartasSelecionadasParaFusao.Clear();
				card.QueueFree();
				_tcsCarta?.TrySetResult(resultadoFusao);
			}
			_bloquearNavegaçãoManual = false;
		}

		public CardUi CriarCartaFusao(CardUi carta)
		{
			if (carta == null)
				return null;
			
			var cartaUi = carta.Duplicate() as CardUi;
			if(cartaUi != null)
			{				
				// Garantir anchors corretos (evita override de size)
				cartaUi.AnchorLeft = 0;
				cartaUi.AnchorTop = 0;
				cartaUi.AnchorRight = 0;
				cartaUi.AnchorBottom = 0;				

				AddChild(cartaUi);

				var cartaOriginal = _cartasSelecionadasParaFusao.FirstOrDefault();
				cartaOriginal?.FlipCard(false);
				cartaUi.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
				// Evita warning do Godot
				cartaUi.Size = new Vector2(140f, 213f);

				cartaUi.Scale = Vector2.One;
				cartaUi.Theme = GD.Load<Theme>("res://Resources/tema_carta_hand.tres");				

				GD.Print(carta);

				cartaUi.DisplayCard(carta.carta, "1");
				AtualizarNumerosFusao();

				return cartaUi;
			}
			return null;
		}

		
		public async Task Instancia3D(Marker3D slotDestino, Cards fusao){
			MaoControl.SeletorGuardian.Setup(fusao);
			bool IsEnemy = slotDestino.Name.ToString().Contains("Ini");
			CleanUpCrew();
			var novaCarta3d = await Tools.InstanciaNodo(slotDestino, CameraHand: CameraHand);				
			novaCarta3d.Setup(fusao, _indiceCampoSelecionado, IsEnemy, IsFaceDown, slotDestino.Name);	
			//chamada do guardian, provavelmente devo implementar uma task que escolhe entre A ou B
			STOP = true;			
			await MaoControl.AllowGuardian(novaCarta3d.carta);
			DefineVisibilidade(false);
			STOP = false;	
			await _anim.AnimaCarta3DParaCampo(novaCarta3d);
		}

		private void AtualizarNumerosFusao()
		{
			for (int i = 0; i < _cartasSelecionadasParaFusao.Count; i++)
			{
				var carta = _cartasSelecionadasParaFusao[i];
				if (!IsInstanceValid(carta)) continue;

				int numero = i + 1; // começa em 1

				carta.SetNumeroFusao(numero);
			}
		}


		public void CleanUpCrew()
		{
			foreach (var carta in _cartasSelecionadasParaFusao)
			{
				if(IsInstanceValid(carta))
					carta.QueueFree();
			}	
		}

		public async Task SairModoSelecaoCampo()
		{
			_selecionandoLocal = false;			
			await CancelarSelecaoNoCampo();
			if (_instanciaSeletor != null) _instanciaSeletor.Visible = false;
		}

		public async Task AtualizarMao(List<int> idsCartasNoDeck, bool animate = true)
		{
			GD.Print(idsCartasNoDeck.Count());	
			STOP = true;
			await MaoControl.InstanciaMaoAnimated(idsCartasNoDeck, animate);
			STOP = false;
			_indiceSelecionado = 0;
			if (IndicadorTriangulo != null)
			{			
				IndicadorTriangulo.Visible = true;				
				AtualizarPosicaoIndicador(); 
			}			
		}
		
		private CardUi AtualizarPosicaoIndicador()
		{
			if (IndicadorTriangulo != null)
			{				
				var carta = MaoControl.GetCarta(_indiceSelecionado);
				if (IsInstanceValid(carta))
				{
					Vector2 cardPos = carta?.GlobalPosition ?? Vector2.Zero;
					Vector2 targetPos = cardPos + new Vector2(-10, 180);
					IndicadorTriangulo.ZIndex = 10;			
					Tween tween = GetTree().CreateTween();
					tween.TweenProperty(IndicadorTriangulo, "position", targetPos, 0.01f)
						.SetTrans(Tween.TransitionType.Quad)
						.SetEase(Tween.EaseType.Out);
					if(carta != null && carta.carta != null)
						MaoControl.DefineInfo(carta.carta);					
					
					return carta;
				}
			}
			return null;
		}

		public async Task<PlayerIntention> SelecionarSlotTAsync(
			Godot.Collections.Array<Marker3D> slots,
			bool primeiroTurno = false,
			bool camIni = false)
		{
			await ChangeState(InputState.BattleSelection);
			_slots = slots;
			_indiceCampoSelecionado = 0;
			PrimeiroTurno = primeiroTurno;
			_camIni = camIni;

			_tcsSlot = new TaskCompletionSource<PlayerIntention>();

			_instanciaSeletor.Visible = true;
			_bloquearNavegaçãoManual = true;

			AtualizarPosicaoSeletorParaSlots(slots);

			return await _tcsSlot.Task;
		}

		private async Task HandleFieldInput(InputEvent e)
		{
			_instanciaSeletor.Visible = true;
			await Tools.TransitionTo(CameraField, 0.5f, _transitionCam, STOP);		
			STOP = false;		
			if (e.IsActionPressed("ui_right"))
			{
				if(_indiceCampoSelecionado < _slots.Count - 1)
				_indiceCampoSelecionado++;
			}
			if (e.IsActionPressed("ui_left"))
			{
				if(_indiceCampoSelecionado > 0)
					_indiceCampoSelecionado--;					
			}
			if(e.IsActionPressed("ui_up"))
			{					
					_indiceCampoSelecionado = Mathf.Clamp(
						_indiceCampoSelecionado - 5,
						0,
						_slots.Count - 1
					);
			}
			if(e.IsActionPressed("ui_down"))
			{
					_indiceCampoSelecionado = Mathf.Clamp(
							_indiceCampoSelecionado + 5,
							0,
							_slots.Count - 1
						);												
			}
			AtualizarPosicaoSeletorParaSlots(_slots[_indiceCampoSelecionado].Position);

			if (e.IsActionReleased("ui_accept"))
			{
				GetViewport().SetInputAsHandled();
				await ConfirmarInvocacaoNoCampo();				
			}

			if (e.IsActionPressed("ui_cancel"))
			{
				await ChangeState(InputState.HandSelection);
				return;
			}
		}

		private async Task SairDoCampo()
		{
			
			await Tools.TransitionTo(CameraHand, 0.5f, _transitionCam, STOP);

			await CancelarSelecaoNoCampo();
		}

		public override async void _UnhandledInput(InputEvent @event)
		{
			GD.Print($"Input recebido no estado: {_inputState}");
			if(_processandoInput) return;

			_processandoInput = true;

			switch (_inputState)
			{
				case InputState.HandSelection:
					await HandSelectionInputHandler(@event);
					break;
				case InputState.FaceSelection:
					await FaceSelectionInputHandler(@event);
					break;
				case InputState.FieldSelection:
					await HandleFieldInput(@event);	
					break;
				case InputState.BattleSelection:
					HandleBattlePhaseInput(@event);
					break;
			}

			_processandoInput = false;
		}
		public async Task FaceSelectionInputHandler(InputEvent @event)
		{
			var alvo = _cartasSelecionadasParaFusao.FirstOrDefault();
			if(@event.IsActionReleased("ui_left") || @event.IsActionReleased("ui_right"))
			{
				if (IsInstanceValid(alvo))
				{
					await alvo.FlipCard(!alvo.IsFaceDown);
				}
			}
			if (@event.IsActionReleased("ui_accept"))
			{
				_slots = DefineSlotagem(alvo.carta.Type);
				DefineVisibildadeIndicadores(false);
				IsFaceDown = alvo.IsFaceDown;
				if (alvo.carta.IsSpell() && alvo.carta.IsFaceDown)
				{
					GetViewport().SetInputAsHandled();
					await ConfirmarInvocacaoNoCampo(true, alvo);
				}
				else
				{
					await ChangeState(InputState.FieldSelection);	
					return;			
				}
			}
			if (@event.IsActionReleased("ui_cancel"))
			{
				await _anim.AnimaCartaParaMao(_indiceSelecionado, true);
				_cartasSelecionadasParaFusao.Clear();
				DefineVisibilidade(true);
				await ChangeState(InputState.HandSelection);
				return;
			}
		}

		public async Task HandSelectionInputHandler(InputEvent @event)
		{
			GD.Print("Input na mão");
			
			if (@event.IsActionPressed("ui_right")) _indiceSelecionado = Mathf.Min(_indiceSelecionado + 1, MaoControl.CartasNaMaoCount() - 1);
			else if (@event.IsActionPressed("ui_left")) _indiceSelecionado = Mathf.Max(_indiceSelecionado - 1, 0);		

			var carta = AtualizarPosicaoIndicador();

			if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("ui_down"))
			{
				_anim.AlternarSelecaoFusao(carta);
			}

			if (@event.IsActionPressed("ui_accept"))
			{
				if(_cartasSelecionadasParaFusao.Count() == 0)
				{
					await _anim.AnimaCartaParaCentro(carta, _indiceSelecionado, this);
					_cartasSelecionadasParaFusao.Add(carta);
					DefineVisibilidade(false);
					DefineVisibildadeIndicadores(true);
					await ChangeState(InputState.FaceSelection);		
					return;	
				}
				if(_cartasSelecionadasParaFusao.Count() > 1)
				{
					GetViewport().SetInputAsHandled();
					await ChangeState(InputState.FieldSelection);
					return;
				}
			}
		}

		public void HandleBattlePhaseInput(InputEvent @event)
		{			
			if (_tcsSlot == null || _tcsSlot.Task.IsCompleted)
				return;

			ProcessarNavegacao(@event);

			if (STOP)
				return;

			if (@event.IsActionPressed("ui_lb") || @event.IsActionPressed("ui_rb"))
				AlternarDefesa();

			if (@event.IsActionPressed("ui_accept"))
				ConfirmarSlot();

			if (@event.IsActionPressed("ui_cancel"))
				FinalizarSelecao(PlayerIntentEnum.InvalidIntent);

			if (@event.IsActionPressed("ui_end_phase"))
				FinalizarSelecao(PlayerIntentEnum.EndTurn);
		}				
						
		// Método auxiliar para mover o seletor entre diferentes arrays de markers
		private void AtualizarPosicaoSeletorParaSlots(Godot.Collections.Array<Marker3D> slots)
		{
			if (slots.Count > 0 && _indiceCampoSelecionado >= 0 && _indiceCampoSelecionado < slots.Count){
				var slotDestino = slots[_indiceCampoSelecionado];
				Tween tween = GetTree().CreateTween();
				tween.TweenProperty(_instanciaSeletor, "global_position", slotDestino.GlobalPosition + new Vector3(0, 0.05f, 0), 0.05f);
				_instanciaSeletor.GlobalRotation = slotDestino.GlobalRotation;				
			}
		}		
		private void AtualizarPosicaoSeletorParaSlots(Vector3 position)
		{
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(_instanciaSeletor, "global_position", position + new Vector3(0, 0.05f, 0), 0.05f);			
		}				
		
		public Godot.Collections.Array<Marker3D> DefineSlotagem(CardTypeEnum tipo)
		{
			if(tipo == CardTypeEnum.Equipment && IsFaceDown || tipo == CardTypeEnum.Spell || 
			tipo == CardTypeEnum.Trap || tipo == CardTypeEnum.Ritual){
				return SlotsCampoST;
			}
			
			return SlotsCampo;
		}
		
		public Godot.Collections.Array<Marker3D> FiltraSlot(bool inimigo = false, bool aliado = false, bool aliadoM = false, bool inimigoM = false, bool spell = false, bool trap = false)
		{
			var markers = new List<Marker3D>();
			if(inimigo)								
				markers.AddRange(Slots.Where(x => x.Name.ToString().Contains("Ini")).ToList());								
			if(inimigoM)
				markers.AddRange(Slots.Where(x => x.Name.ToString().Contains("Ini") && x.Name.ToString().Contains("M")).ToList());			
			if(aliado)				
				markers.AddRange(Slots.Where(x => !x.Name.ToString().Contains("Ini")).ToList());
			if(aliadoM)
				markers.AddRange(Slots.Where(x => !x.Name.ToString().Contains("Ini") && x.Name.ToString().Contains("M")).ToList());
				
			return new Godot.Collections.Array<Marker3D>(markers);
		}

		void ConfirmarSlot()
		{
			var slotDestino = _camIni
				? SlotsCampoIni[_indiceCampoSelecionado]
				: _slots[_indiceCampoSelecionado];

			var nodo = Tools.PegaNodoCarta3d(slotDestino.Name);
			var Intent = Tools.DefineIntentCampo(nodo?.carta);

			if (PrimeiroTurno && Intent != PlayerIntentEnum.SelectSpell)
			{
				return;
			}

			GD.Print($"{slotDestino.Name} Slot confirmado: {_indiceCampoSelecionado}");

			if (Tools.PegaNodoNoSlot(slotDestino))
			{
				if (!_camIni)
				{
					if (!nodo.Defesa)
					{
						FinalizarSelecao(Intent);
					}
				}
				else
				{
					FinalizarSelecao(Intent);
				}
			}
			else if (Tools.PodeBate(SlotsCampoIni.ToList()))
			{
				if (!_camIni)
				{
					if (nodo != null && !nodo.Defesa)
						FinalizarSelecao(Intent);
				}
				else
				{
					FinalizarSelecao(Intent);
				}
			}
		}
		void FinalizarSelecao(PlayerIntentEnum intent)
		{
			_instanciaSeletor.Visible = false;
			_bloquearNavegaçãoManual = false;
			PlayerIntention np = new PlayerIntention(_slots[_indiceCampoSelecionado].Name, intent);

			_tcsSlot.TrySetResult(np);
		}

		void AlternarDefesa()
		{
			if (_camIni)
				return;

			var slotDestino = _slots[_indiceCampoSelecionado];

			if(gameLoop.MonsterHasAttacked(slotDestino.Name)) return;

			var isEnemy = slotDestino.Name.ToString().Contains("Ini");

			var pegou = Tools.PegaNodoCarta3d(slotDestino.Name);

			if (pegou == null)
				return;

			var rotacao = pegou.Rotation;

			if (pegou is Carta3d nodo){
				nodo.Defesa = !nodo.Defesa;
				gameLoop._gameState.CurrentPlayer.Field.BotaDeLadinho(nodo.markerName, nodo.Defesa);
			}

			if (!isEnemy)
			{
				if (rotacao == new Vector3(0,0,0))
					pegou.Rotation = new Vector3(0, 1.5707964f, 0);
				else
					pegou.Rotation = Vector3.Zero;
			}
			else
			{
				if (rotacao == new Vector3(0,3.14f,0))
					pegou.Rotation = new Vector3(0, -1.5707964f, 0);
				else
					pegou.Rotation = new Vector3(0,3.14f,0);
			}
		}


		void ProcessarNavegacao(InputEvent e)
		{
			int anterior = _indiceCampoSelecionado;

			ProcessarNavegacao3D(_slots, e);

			if (anterior != _indiceCampoSelecionado)
				AtualizarPosicaoSeletorParaSlots(_slots);
		}
				
		public void ProcessarNavegacao3D(Godot.Collections.Array<Marker3D> slots, InputEvent e){
			if(!STOP){
				if (e.IsActionPressed("ui_right"))
				{
					if(_indiceCampoSelecionado < slots.Count - 1)
						_indiceCampoSelecionado++;
					GD.Print("up is called");
				}
				if (e.IsActionPressed("ui_left"))
				{
					if(_indiceCampoSelecionado > 0)
						_indiceCampoSelecionado--;	
				}
				if(e.IsActionPressed("ui_up"))
				{					
					 _indiceCampoSelecionado = Mathf.Clamp(
							_indiceCampoSelecionado - 5,
							0,
							slots.Count - 1
						);
				}
				if(e.IsActionPressed("ui_down"))
				{
					 _indiceCampoSelecionado = Mathf.Clamp(
								_indiceCampoSelecionado + 5,
								0,
								slots.Count - 1
							);												
				}
				AtualizarPosicaoSeletorParaSlots(slots[_indiceCampoSelecionado].Position);
			}								
		}			
			
		public IndicadorSeta CriarSetaPersonalizada(Vector2 alvo, bool direita = false)
		{
			if(indicadorSetaEsquerda != null && !direita)
			{
				indicadorSetaEsquerda.Visible = true;
				return indicadorSetaEsquerda;
			}
			if(indicadorSetaDireita != null && direita)
			{
				indicadorSetaDireita.Visible = true;
				return indicadorSetaDireita;
			}
			
			var cenaSeta = GD.Load<PackedScene>("res://HUD/IndicadorSeta.tscn");
			var instancia = cenaSeta.Instantiate<IndicadorSeta>();

			if(!direita)
				indicadorSetaEsquerda = instancia;
			else
				indicadorSetaDireita = instancia;

			instancia.PosicaoDesejada = alvo;
			instancia.OlharParaDireita = alvo.X > GetViewportRect().Size.X / 2f; 

			AddChild(instancia);
			return instancia;
		}
		
		public void DefineVisibilidade(bool sinal)
		{
			//Visible = sinal;
			IndicadorTriangulo.Visible = sinal;	
		}
		public void DefineVisibildadeIndicadores(bool sinal)
		{
			if(indicadorSetaDireita != null && indicadorSetaEsquerda != null)
			{
				indicadorSetaDireita.Visible = sinal;
				indicadorSetaEsquerda.Visible = sinal;
			}
		}
		
		public void ConfigurarSlots(
			Godot.Collections.Array<Marker3D> monstrosAliados, 
			Godot.Collections.Array<Marker3D> monstrosInimigos,
			Godot.Collections.Array<Marker3D> magiasAliados,
			Godot.Collections.Array<Marker3D> magiasInimigos)
		{
			this.SlotsCampo = monstrosAliados;
			this.SlotsCampoIni = monstrosInimigos;			
			this.SlotsCampoST = magiasAliados;
			this.SlotsCampoSTIni = magiasInimigos;			
			
			if(this.Slots.Count() < 20){
				this.Slots.AddRange(SlotsCampo);
				this.Slots.AddRange(SlotsCampoIni);
				this.Slots.AddRange(SlotsCampoST);
				this.Slots.AddRange(SlotsCampoSTIni);				
			}
			
			GD.Print(Slots.Count());
			
			GD.Print("MaoJogador: Slots redefinidos com sucesso via GameLoop.");
		}
		
		public async Task<FusionResult> AguardarConfirmacaoJogadaAsync()
		{
			_tcsCarta = new TaskCompletionSource<FusionResult>();
			await ChangeState(InputState.HandSelection);
			_slots = FiltraSlot(aliado: true);
			
			// O código aqui fica "parado" até que ConfirmarInvocacaoNoCampo() seja chamado
			var resultado = await _tcsCarta.Task;
			//depois de confirmado, setamos a task, e aqui precisamos começar as animações de mover para o centro novamente e em sequência definir qual a guardian star
			return resultado;
		}					

		public async Task ChangeState(InputState novoEstado)
		{
 			await ToSignal(GetTree(), "process_frame");
			if (_inputState == novoEstado)
				return;

			// EXIT do estado atual (opcional futuramente)

			GetViewport().SetInputAsHandled();
			_inputState = novoEstado;

			switch (novoEstado)
			{	
				case InputState.HandSelection:
					await SairDoCampo();
					DefineVisibildadeIndicadores(false);
					break;
			}
		}

	}
}
