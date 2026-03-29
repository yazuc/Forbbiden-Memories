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
		private TaskCompletionSource<PlayerAction> _inputTcs;
		public TaskCompletionSource<bool> _tcsFaceDown;
		bool IsFaceDown = false;
		private Node3D _instanciaSeletor = null;
		public int _indiceSelecionado = 0;	
		public int _indiceCampoSelecionado = 0;
		public List<CardUi> _cartasSelecionadasParaFusao = new List<CardUi>();
		private List<Node3D> _cartasInstanciadas = new List<Node3D>();
		public List<int> IDFusao = new List<int>();
		public Vector2 lastPos = Vector2.Zero;
		private Godot.Collections.Array<Marker3D> _slots;
		public bool _camIni, PrimeiroTurno;
		public string LogicalPosition {get;set;}
		public Mao MaoControl {get;set;}
		public IndicadorSeta indicadorSetaEsquerda{get;set;}
		public IndicadorSeta indicadorSetaDireita{get;set;}
		public InputState _inputState = InputState.HandSelection;
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

		public override void _Process(double delta)
		{			
			float speed = 8.0f;
			Vector2 newScale = Scale;
			newScale.Y = Mathf.Sin(Time.GetTicksMsec() * 0.001f * speed);
			IndicadorTriangulo.Scale = newScale;
			AtualizarPosicaoIndicador();
		}
		
		public Task<PlayerAction> AguardarAcaoAsync()
		{
			_inputTcs = new TaskCompletionSource<PlayerAction>();
			return _inputTcs.Task;
		}

		public void FinalizarAcao(PlayerAction action)
		{
			if (_inputTcs != null && !_inputTcs.Task.IsCompleted)
			{
				_inputTcs.SetResult(action);
			}
		}

		public async Task EntrarModoSelecaoCampo()
		{			
			if(_cartasSelecionadasParaFusao.Count() == 1)
				await _anim.AnimaCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().carta.Id, _cartasSelecionadasParaFusao.FirstOrDefault().carta.Name, _indiceSelecionado);
			_indiceCampoSelecionado = 0; // Começa no primeiro slot								
			if (_instanciaSeletor != null)
			{				
				AtualizarPosicaoSeletor3D(SlotsCampo, _cartasSelecionadasParaFusao.FirstOrDefault()?.carta?.Type ?? CardTypeEnum.Indefinido);
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
		public void AtualizarPosicaoSeletor3D(Godot.Collections.Array<Marker3D> slots, CardTypeEnum tipo)
		{
			if (_instanciaSeletor == null || slots == null || slots.Count == 0)
			{
				GD.PrintErr("MaoJogador: Tentativa de atualizar seletor sem SlotsCampo configurados!");
				return;
			}
				
			if(_cartasSelecionadasParaFusao.Count() == 1){
				slots = DefineSlotagem(tipo);				
			}
			var slotDestino = slots[_indiceCampoSelecionado];			
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(_instanciaSeletor, "global_position", slotDestino.GlobalPosition + new Vector3(0, 0.05f, 0), 0.05f);
			_instanciaSeletor.GlobalRotation = slotDestino.GlobalRotation;
		}
		
		public async Task<FusionResult> ConfirmarInvocacaoNoCampo(bool ativaDireto = false, CardUi? card = null)
		{			
			
			var slotDestino = SlotsCampo[_indiceCampoSelecionado];
			var carta3dfield = Tools.PegaNodoCarta3d(slotDestino.Name);

			var scene = GD.Load<PackedScene>("res://Menu/Password/card_ui.tscn");
			if(carta3dfield != null)
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
					await CartaSTAction(card, resultadoFusao);
					return resultadoFusao;
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
			}
			return resultadoFusao;
		}

		public async Task CartaSTAction(CardUi card, FusionResult resultadoFusao)
		{
			card = _anim.GetChildCount() > 0 ? _anim.GetChild<CardUi>(0) : card;

			if(card != null)
			{						
				await card.AtivaSpellAnimation(_anim.ScrenCenter());						
				_cartasSelecionadasParaFusao.Clear();
				card.QueueFree();
			}
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

		public void PrepararSelecaoSlot(
			Godot.Collections.Array<Marker3D> slots,
			bool primeiroTurno = false,
			bool camIni = false)
		{
			_slots = slots;
			_indiceCampoSelecionado = 0;
			PrimeiroTurno = primeiroTurno;
			_camIni = camIni;

			_instanciaSeletor.Visible = true;
			AtualizarPosicaoSeletorParaSlots(slots);
		}

		private void HandleHandInput(InputEvent e)
		{
			if (MaoControl.CartasNaMaoCount() == 0) return;

			int anterior = _indiceSelecionado;

			if (e.IsActionPressed("ui_right"))
				_indiceSelecionado = Mathf.Min(_indiceSelecionado + 1, MaoControl.CartasNaMaoCount() - 1);
			else if (e.IsActionPressed("ui_left"))
				_indiceSelecionado = Mathf.Max(_indiceSelecionado - 1, 0);

			if (_indiceSelecionado > 4) _indiceSelecionado = 0;
			if (_indiceSelecionado < 0) _indiceSelecionado = 4;

			if (anterior != _indiceSelecionado)
				AtualizarPosicaoIndicador();

			var carta = MaoControl.GetCarta(_indiceSelecionado);
			if (e.IsActionPressed("ui_up"))
			{
				_anim.AlternarSelecaoFusao(carta);
			}

			if (e.IsActionPressed("ui_accept"))
			{
				FinalizarAcao(new PlayerAction
				{
					Type = PlayerActionType.SelectCard,
					Card = carta
				});
			}
			else if (e.IsActionPressed("ui_cancel"))
			{
				FinalizarAcao(new PlayerAction
				{
					Type = PlayerActionType.Cancel
				});
			}
			else if (e.IsActionPressed("ui_end_phase"))
			{
				FinalizarAcao(new PlayerAction
				{
					Type = PlayerActionType.EndTurn
				});
			}
		}

		private void HandleFieldInput(InputEvent e)
		{
			int anterior = _indiceCampoSelecionado;
			var slotsParaUsar = SlotsCampo;
			int dir = 1;

			if (e.IsActionPressed("ui_right"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado + dir, 0, slotsParaUsar.Count - 1);
			if (e.IsActionPressed("ui_left"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado - dir, 0, slotsParaUsar.Count - 1);
			if (e.IsActionPressed("ui_up"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado - 5, 0, slotsParaUsar.Count - 1);
			if (e.IsActionPressed("ui_down"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado + 5, 0, slotsParaUsar.Count - 1);

			if (anterior != _indiceCampoSelecionado)
			{
				AtualizarPosicaoSeletor3D(slotsParaUsar, _cartasSelecionadasParaFusao.FirstOrDefault()?.carta?.Type ?? CardTypeEnum.Indefinido);
			}

			if (e.IsActionPressed("ui_accept"))
			{
				FinalizarAcao(new PlayerAction
				{
					Type = PlayerActionType.SelectSlot,
					SlotIndex = _indiceCampoSelecionado
				});
			}
			else if (e.IsActionPressed("ui_cancel"))
			{
				FinalizarAcao(new PlayerAction
				{
					Type = PlayerActionType.Cancel
				});
			}
		}

		private void HandleBattleInput(InputEvent e)
		{
			if (_slots == null || _slots.Count == 0) return;

			int anterior = _indiceCampoSelecionado;
			int dir = _camIni ? -1 : 1;

			if (e.IsActionPressed("ui_right"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado + dir, 0, _slots.Count - 1);
			if (e.IsActionPressed("ui_left"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado - dir, 0, _slots.Count - 1);
			if (e.IsActionPressed("ui_up"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado - 5, 0, _slots.Count - 1);
			if (e.IsActionPressed("ui_down"))
				_indiceCampoSelecionado = Mathf.Clamp(_indiceCampoSelecionado + 5, 0, _slots.Count - 1);

			if (anterior != _indiceCampoSelecionado)
			{
				AtualizarPosicaoSeletorParaSlots(_slots);
			}

			if (e.IsActionPressed("ui_lb") || e.IsActionPressed("ui_rb"))
				AlternarDefesa();

			if (e.IsActionPressed("ui_accept"))
			{
				var slotDestino = _camIni ? SlotsCampoIni[_indiceCampoSelecionado] : _slots[_indiceCampoSelecionado];
				var nodo = Tools.PegaNodoCarta3d(slotDestino.Name);
				var Intent = Tools.DefineIntentCampo(nodo?.carta);

				if (PrimeiroTurno && Intent != PlayerIntentEnum.SelectSpell) return;

				if (Tools.PegaNodoNoSlot(slotDestino))
				{
					if (!_camIni && !nodo.Defesa)
					{
						LogicalPosition = slotDestino.Name;
						FinalizarAcao(new PlayerAction { Type = PlayerActionType.SelectSlot, SlotIndex = _indiceCampoSelecionado });
					}
					else if (_camIni)
					{
						LogicalPosition = slotDestino.Name;
						FinalizarAcao(new PlayerAction { Type = PlayerActionType.SelectSlot, SlotIndex = _indiceCampoSelecionado });
					}
				}
				else if (Tools.PodeBate(SlotsCampoIni.ToList()))
				{
					if (!_camIni && nodo != null && !nodo.Defesa)
					{
						FinalizarAcao(new PlayerAction { Type = PlayerActionType.SelectSlot, SlotIndex = _indiceCampoSelecionado });
					}
					else if (_camIni)
					{
						FinalizarAcao(new PlayerAction { Type = PlayerActionType.SelectSlot, SlotIndex = _indiceCampoSelecionado });
					}
				}
			}
			else if (e.IsActionPressed("ui_cancel"))
			{
				FinalizarAcao(new PlayerAction { Type = PlayerActionType.Cancel });
			}
			else if (e.IsActionPressed("ui_end_phase"))
			{
				FinalizarAcao(new PlayerAction { Type = PlayerActionType.EndTurn });
			}
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (STOP) return;

			switch (_inputState)
			{
				case InputState.HandSelection:
					HandleHandInput(@event);
					break;
				case InputState.FieldSelection:
					HandleFieldInput(@event);
					break;
				case InputState.BattleSelection:
					HandleBattleInput(@event);
					break;
			}
		}

		public void AtualizarPosicaoSeletorParaSlots(Godot.Collections.Array<Marker3D> slots)
		{
			if (slots != null && slots.Count > 0 && _indiceCampoSelecionado >= 0 && _indiceCampoSelecionado < slots.Count)
			{
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
		
		public void EsconderSeletor()
		{
			if (_instanciaSeletor != null)
				_instanciaSeletor.Visible = false;
		}
		

			
	}
}
