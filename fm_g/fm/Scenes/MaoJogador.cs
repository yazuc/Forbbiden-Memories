using Godot;
using QuickType;
using System;
using static fm.Function;

namespace fm{	
	public partial class MaoJogador : Node2D
	{
		[Export] public PackedScene CartaCena;
		[Export] public Node2D IndicadorTriangulo;
		public Node2D IndicadorSeta;
		[Export] public Camera3D CameraHand;
		[Export] public Camera3D CameraField;
		[Export] public Camera3D CameraInimigo;
		[Export] public PackedScene Seletor;		
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
		private bool _selecionandoLocal = false; // Estado para saber se estamos escolhendo onde colocar a carta
		public List<CardUi> _cartasSelecionadasParaFusao = new List<CardUi>();
		private List<Node3D> _cartasInstanciadas = new List<Node3D>();
		private bool _processandoInput = false;		
		public List<int> IDFusao = new List<int>();
		public Vector2 lastPos = Vector2.Zero;
		private Godot.Collections.Array<Marker3D> _slots;
		public bool _camIni, PrimeiroTurno;
		public string LogicalPosition {get;set;}
		public Mao MaoControl {get;set;}
		public IndicadorSeta indicadorSetaEsquerda{get;set;}
		public IndicadorSeta indicadorSetaDireita{get;set;}
		public GameLoop gameLoop {get;set;}
		public AnimationP _anim;
		public Helper Tools;
		public CardUi RefFusao;
		public override void _Ready()
		{
			_transitionCam = new Camera3D();
			MaoControl = GetNode<Mao>("../CameraPivot/CameraHand/Control/InterfaceDuelo/Mao");		
			AddChild(_transitionCam);
			if (Seletor != null)
			{
				_instanciaSeletor = Seletor.Instantiate<Node3D>();
				GetParent().CallDeferred("add_child", _instanciaSeletor);
				//GetTree().CurrentScene.CallDeferred("add_child", _instanciaSeletor);
				_instanciaSeletor.Visible = false;
			}
			_anim = GetNode<AnimationP>("../AnimationP");
			_anim._cartasSelecionadasParaFusao = _cartasSelecionadasParaFusao;
			Tools = GetNode<Helper>("../Helper");
			Tools._cartasInstanciadas = _cartasInstanciadas;
		}

		public async override void _Process(double delta)
		{			
			float speed = 8.0f;
			Vector2 newScale = Scale;
			newScale.Y = Mathf.Sin(Time.GetTicksMsec() * 0.001f * speed);
			IndicadorTriangulo.Scale = newScale;
			if (!_processandoInput && !STOP)
			{
				ExecutarNavegacao();
			}			
		}
		
		private async void ExecutarNavegacao()
		{
			_processandoInput = true;
			await HandleNavigation();
			_processandoInput = false;
			AtualizarPosicaoIndicador();
		}
		private async Task HandleNavigation()
		{
			if (_bloquearNavegaçãoManual) return;
			if (MaoControl.CartasNaMaoCount() == 0) return;

			if (!_selecionandoLocal) 
			{				
				// SELEÇÃO NA MÃO (2D)
				int anterior = _indiceSelecionado;
				if(_indiceSelecionado > 4){
					_indiceSelecionado = 0;
				}
				if(_indiceSelecionado < 0){
					_indiceSelecionado = 4;
				}
				if(!STOP){
					if (Input.IsActionJustPressed("ui_right")) _indiceSelecionado = Mathf.Min(_indiceSelecionado + 1, MaoControl.CartasNaMaoCount() - 1);
					else if (Input.IsActionJustPressed("ui_left")) _indiceSelecionado = Mathf.Max(_indiceSelecionado - 1, 0);					
				}

				if (anterior != _indiceSelecionado) AtualizarPosicaoIndicador();
				
				// MECÂNICA DE FUSÃO (Cima/Baixo)
				if(!STOP){
					var carta = MaoControl.GetCarta(_indiceSelecionado);
					if (Input.IsActionJustPressed("ui_up")) 
					{
						_anim.AlternarSelecaoFusao(carta);
					}					
					if (Input.IsActionJustPressed("ui_accept")) 
					{
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						if (_cartasSelecionadasParaFusao.Count == 0)
						{
							_cartasSelecionadasParaFusao.Add(carta);
						}
						try 
						{
							// O await vai "explodir" aqui se TrySetCanceled for chamado
							var alvo = _cartasSelecionadasParaFusao.FirstOrDefault();
							IsFaceDown = await _anim.AnimaCartaParaCentro(this, alvo.carta.Id, alvo.carta.Name, _indiceSelecionado);							
							if(_cartasSelecionadasParaFusao.Count() == 1 && alvo.carta.IsSpell() && !IsFaceDown)
							{
								GD.Print("usando spell");		
								ConfirmarInvocacaoNoCampo(true, alvo);		
								return;
							}								
							else			
								await EntrarModoSelecaoCampo();
						}
						catch (OperationCanceledException) 
						{
							// O código cai aqui IMEDIATAMENTE quando aperta ui_cancel
							GD.Print("Ação cancelada pelo usuário.");
							
							if (_cartasSelecionadasParaFusao.Any()) {
								await _anim.AnimaCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().carta.Id, _cartasSelecionadasParaFusao.FirstOrDefault().carta.Name, _indiceSelecionado, true);
							}
						}
					}
				}
			}
			else 
			{				
				ControlarSelecaoDeCampo();
				if(!STOP){
					if (Input.IsActionJustPressed("ui_accept")) 
					{
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						ConfirmarInvocacaoNoCampo();						
					}
					
					if (Input.IsActionJustPressed("ui_cancel")) 
					{					
						await Tools.TransitionTo(CameraHand, 0.5f, _transitionCam, STOP);
						await SairModoSelecaoCampo();
					}					
				}
			}
			return;
		}		

		private async Task EntrarModoSelecaoCampo()
		{			
			if(_cartasSelecionadasParaFusao.Count() == 1)
				await _anim.AnimaCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().carta.Id, _cartasSelecionadasParaFusao.FirstOrDefault().carta.Name, _indiceSelecionado);
			_selecionandoLocal = true;
			_indiceCampoSelecionado = 0; // Começa no primeiro slot								
			if (_instanciaSeletor != null)
			{				
				AtualizarPosicaoSeletor3D(SlotsCampo, _cartasSelecionadasParaFusao.FirstOrDefault().carta.Id);
				_instanciaSeletor.Visible = true;
				await Tools.TransitionTo(CameraField, 0.5f, _transitionCam, STOP);
			}
		}
		
		public async Task CancelarSelecaoNoCampo()
		{
						
			if (_cartasSelecionadasParaFusao.Any() && _cartasSelecionadasParaFusao.Count() == 1) {
				await _anim.AnimaCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().carta.Id, _cartasSelecionadasParaFusao.FirstOrDefault().carta.Name, _indiceSelecionado, true);
			}			
						
			_cartasSelecionadasParaFusao.Clear();
			// Desative aqui os highlights ou colisores que você ativou para a seleção
			//GD.Print("Seleção de campo cancelada manualmente.");
		}
		private void ControlarSelecaoDeCampo()
		{
			int anterior = _indiceCampoSelecionado;
			if(!STOP){			
				if (Input.IsActionJustPressed("ui_right"))
					_indiceCampoSelecionado = Mathf.Min(_indiceCampoSelecionado + 1, SlotsCampo.Count - 1);
				
				if (Input.IsActionJustPressed("ui_left"))
					_indiceCampoSelecionado = Mathf.Max(_indiceCampoSelecionado - 1, 0);
			}

			if (anterior != _indiceCampoSelecionado)
			{
				AtualizarPosicaoSeletor3D(SlotsCampo, _cartasSelecionadasParaFusao.FirstOrDefault().carta.Id);
			}
		}		
						

		private void AtualizarPosicaoSeletor3D(Godot.Collections.Array<Marker3D> slots, int Id)
		{
			if (_instanciaSeletor == null || slots == null || slots.Count == 0)
			{
				GD.PrintErr("MaoJogador: Tentativa de atualizar seletor sem SlotsCampo configurados!");
				return;
			}
				
			if(_cartasSelecionadasParaFusao.Count() == 1){
				slots = DefineSlotagem(PegaTipoPorId(Id));				
			}
			var slotDestino = slots[_indiceCampoSelecionado];			
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(_instanciaSeletor, "global_position", slotDestino.GlobalPosition + new Vector3(0, 0.05f, 0), 0.05f);
			_instanciaSeletor.GlobalRotation = slotDestino.GlobalRotation;
		}
		
		public async void ConfirmarInvocacaoNoCampo(bool ativaDireto = false, CardUi? card = null)
		{			
			
			var slotDestino = SlotsCampo[_indiceCampoSelecionado];
			var carta3dfield = Tools.PegaNodoCarta3d(slotDestino.Name);

			var scene = GD.Load<PackedScene>("res://Menu/Password/card_ui.tscn");
			if(carta3dfield != null && _selecionandoLocal)
			{
				RefFusao = CriarCartaFusao(carta3dfield);
				_cartasSelecionadasParaFusao.Insert(0, RefFusao);
			}

			var ids = new List<int>();												
			ids.AddRange(_cartasSelecionadasParaFusao.Select(x => x.carta.Id));
			var resultadoFusao = ProcessChain(string.Join(",", ids), carta3dfield?.carta);		
			
			if(_cartasSelecionadasParaFusao.Count() > 1)
			{
				await _anim.AnimaFusao(this);
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
						carta3dfield.UpdateCard(resultadoFusao.MainCard);
						gameLoop._gameState.CurrentPlayer.Field.UpdateMonster(resultadoFusao.MainCard, slotDestino.Name);
					}
					else
					{
						await Instancia3D(slotDestino, resultadoFusao.MainCard);			
						LogicalPosition = slotDestino.Name.ToString();						
					}
					CleanUpCrew();
				}
				
				_cartasSelecionadasParaFusao.Clear();
				await SairModoSelecaoCampo();
				_bloquearNavegaçãoManual = false;
				_tcsCarta?.TrySetResult(resultadoFusao);
			}
		}

		public async void CartaSTAction(CardUi card, FusionResult resultadoFusao)
		{
			_bloquearNavegaçãoManual = true;

			card = _anim.GetChildCount() > 0 ? _anim.GetChild<CardUi>(0) : card;

			if(card != null)
			{						
				await card.AtivaSpellAnimation(_anim.ScrenCenter());						
				_bloquearNavegaçãoManual = false;
				_selecionandoLocal = false;
				_cartasSelecionadasParaFusao.Clear();
				card.QueueFree();
				_tcsCarta?.TrySetResult(resultadoFusao);
			}
			_bloquearNavegaçãoManual = false;
		}

		public CardUi CriarCartaFusao(Carta3d carta3dfield)
		{
			if (carta3dfield == null)
				return null;

			var scene = GD.Load<PackedScene>("res://Menu/Password/card_ui.tscn");
			var cartaUi = scene.Instantiate<CardUi>();

			// Garantir anchors corretos (evita override de size)
			cartaUi.AnchorLeft = 0;
			cartaUi.AnchorTop = 0;
			cartaUi.AnchorRight = 0;
			cartaUi.AnchorBottom = 0;

			AddChild(cartaUi);

			var cartaOriginal = _cartasSelecionadasParaFusao.First();
			cartaOriginal.FlipCard(false);
			cartaUi.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
			// Evita warning do Godot
			cartaUi.Size = cartaOriginal.Size;

			cartaUi.Scale = Vector2.One;
			cartaUi.Theme = GD.Load<Theme>("res://Resources/tema_carta_hand.tres");

			GD.Print(carta3dfield.carta.Name);

			cartaUi.DisplayCard(carta3dfield.carta, "1");
			AtualizarNumerosFusao();

			return cartaUi;
		}

		
		public async Task Instancia3D(Marker3D slotDestino, Cards fusao){
			bool IsEnemy = slotDestino.Name.ToString().Contains("Ini");
			var novaCarta3d = Tools.InstanciaNodo(slotDestino);			
			if(IsEnemy){
				GD.Print(slotDestino.GlobalRotation.ToString());
				Vector3 rota = new Vector3(-0, 1.5707964f, 0);
				novaCarta3d.GlobalRotation += slotDestino.GlobalRotation + rota;
			}			
			novaCarta3d.Setup(fusao, (int)_indiceCampoSelecionado, IsEnemy, IsFaceDown, slotDestino.Name);			
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

		public void AtualizarMao(List<int> idsCartasNoDeck, bool animate = true)
		{
			GD.Print(idsCartasNoDeck.Count());	
			MaoControl.InstanciaMaoAnimated(idsCartasNoDeck, animate);
			_indiceSelecionado = 0;
			if (IndicadorTriangulo != null)
			{			
				IndicadorTriangulo.Visible = true;				
				AtualizarPosicaoIndicador(); 
			}			
		}
		
		private void AtualizarPosicaoIndicador()
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
				}
			}
		}

		public Task<PlayerIntention> SelecionarSlotTAsync(
			Godot.Collections.Array<Marker3D> slots,
			bool primeiroTurno = false,
			bool camIni = false)
		{
			_slots = slots;
			_indiceCampoSelecionado = 0;
			PrimeiroTurno = primeiroTurno;
			_camIni = camIni;

			_tcsSlot = new TaskCompletionSource<PlayerIntention>();

			_instanciaSeletor.Visible = true;
			_bloquearNavegaçãoManual = true;

			AtualizarPosicaoSeletorParaSlots(slots);

			return _tcsSlot.Task;
		}
		public override void _UnhandledInput(InputEvent @event)
		{
			if (_tcsSlot == null || _tcsSlot.Task.IsCompleted)
				return;

			ProcessarNavegacao();

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
		
		public CardTypeEnum PegaTipoPorId(int id)
		{
			var db = CardDatabase.Instance;
			var card = db.GetCardById(id);
			if(card != null)
				return card.Type;
				
			return CardTypeEnum.Indefinido;
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
						LogicalPosition = slotDestino.Name;
						FinalizarSelecao(Intent);
					}
				}
				else
				{
					LogicalPosition = slotDestino.Name;
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


		void ProcessarNavegacao()
		{
			int anterior = _indiceCampoSelecionado;

			ProcessarNavegacao3D(_slots, _camIni);

			if (anterior != _indiceCampoSelecionado)
				AtualizarPosicaoSeletorParaSlots(_slots);
		}
				
		public void ProcessarNavegacao3D(Godot.Collections.Array<Marker3D> slots, bool camIni){
			int dir = camIni ? -1 : 1;		
			if(!STOP){
				if (Input.IsActionJustPressed("ui_right"))
				{
					_indiceCampoSelecionado = Mathf.Clamp(
								_indiceCampoSelecionado + dir,
								0,
								slots.Count - 1
							);
				}
				if (Input.IsActionJustPressed("ui_left"))
				{
					_indiceCampoSelecionado = Mathf.Clamp(
							_indiceCampoSelecionado - dir,
							0,
							slots.Count - 1
						);
				}
				if(Input.IsActionJustPressed("ui_up"))
				{					
					 _indiceCampoSelecionado = Mathf.Clamp(
							_indiceCampoSelecionado - 5,
							0,
							slots.Count - 1
						);
				}
				if(Input.IsActionJustPressed("ui_down"))
				{
					 _indiceCampoSelecionado = Mathf.Clamp(
								_indiceCampoSelecionado + 5,
								0,
								slots.Count - 1
							);												
				}
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
			Visible = sinal;
			IndicadorTriangulo.Visible = sinal;
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
			
			// O código aqui fica "parado" até que ConfirmarInvocacaoNoCampo() seja chamado
			var resultado = await _tcsCarta.Task;
			//depois de confirmado, setamos a task, e aqui precisamos começar as animações de mover para o centro novamente e em sequência definir qual a guardian star
			return resultado;
		}
		

			
	}
}
