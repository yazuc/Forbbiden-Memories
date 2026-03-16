using Godot;
using System;

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
		private TaskCompletionSource<Godot.Collections.Array<int>> _tcsCarta;
		private TaskCompletionSource<int> _tcsSlot;
		public TaskCompletionSource<bool> _tcsFaceDown;
		bool IsFaceDown = false;
		private bool _bloquearNavegaçãoManual = false;
		private Node3D _instanciaSeletor = null;
		public int _indiceSelecionado = 0;	
		public int _indiceCampoSelecionado = 0;		
		private bool _selecionandoLocal = false; // Estado para saber se estamos escolhendo onde colocar a carta
		private List<CartasBase> _cartasSelecionadasParaFusao = new List<CartasBase>();
		private List<Node3D> _cartasInstanciadas = new List<Node3D>();
		private bool _processandoInput = false;		
		public List<int> IDFusao = new List<int>();
		public Vector2 lastPos = Vector2.Zero;
		public string LogicalPosition {get;set;}
		public Mao MaoControl {get;set;}
		public IndicadorSeta indicadorSetaEsquerda{get;set;}
		public IndicadorSeta indicadorSetaDireita{get;set;}
		public AnimationP _anim;
		public Helper Tools;

		public override void _Ready()
		{
			_transitionCam = new Camera3D();
			MaoControl = GetNode<Mao>("../CameraPivot/CameraHand/Control/InterfaceDuelo/Mao");		
			AddChild(_transitionCam);
			if (Seletor != null)
			{
				_instanciaSeletor = Seletor.Instantiate<Node3D>();
				GetTree().CurrentScene.CallDeferred("add_child", _instanciaSeletor);
				_instanciaSeletor.Visible = false;
			}
			_anim = GetNode<AnimationP>("../AnimationP");
			_anim._cartasSelecionadasParaFusao = _cartasSelecionadasParaFusao;
			Tools = GetNode<Helper>("../Helper");
			Tools._cartasInstanciadas = _cartasInstanciadas;
		}

		public async override void _Process(double delta)
		{			
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
					if (Input.IsActionJustPressed("ui_up")) 
					{
						_anim.AlternarSelecaoFusao(MaoControl.GetCartaBase(_indiceSelecionado));
					}					
					if (Input.IsActionJustPressed("ui_accept")) 
					{
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						if (_cartasSelecionadasParaFusao.Count == 0)
						{
							_cartasSelecionadasParaFusao.Add(MaoControl.GetCartaBase(_indiceSelecionado));
						}
						try 
						{
							// O await vai "explodir" aqui se TrySetCanceled for chamado
							var alvo = _cartasSelecionadasParaFusao.FirstOrDefault();
							IsFaceDown = await _anim.AnimaCartaParaCentro(this,alvo.CurrentID, alvo.Name, _indiceSelecionado);							
							GD.Print("Facedown confirmado: " + IsFaceDown);
							await EntrarModoSelecaoCampo();
						}
						catch (OperationCanceledException) 
						{
							// O código cai aqui IMEDIATAMENTE quando aperta ui_cancel
							GD.Print("Ação cancelada pelo usuário.");
							
							if (_cartasSelecionadasParaFusao.Any()) {
								await _anim.AnimaCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID, _cartasSelecionadasParaFusao.FirstOrDefault().Name, _indiceSelecionado, true);
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
				await _anim.AnimaCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID, _cartasSelecionadasParaFusao.FirstOrDefault().Name, _indiceSelecionado);
			_selecionandoLocal = true;
			_indiceCampoSelecionado = 0; // Começa no primeiro slot								
			if (_instanciaSeletor != null)
			{				
				AtualizarPosicaoSeletor3D(SlotsCampo, _cartasSelecionadasParaFusao.FirstOrDefault().CurrentID);
				_instanciaSeletor.Visible = true;
				await Tools.TransitionTo(CameraField, 0.5f, _transitionCam, STOP);
			}
		}
		
		public async Task CancelarSelecaoNoCampo()
		{
						
			if (_cartasSelecionadasParaFusao.Any() && _cartasSelecionadasParaFusao.Count() == 1) {
				await _anim.AnimaCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID, _cartasSelecionadasParaFusao.FirstOrDefault().Name, _indiceSelecionado, true);
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
				AtualizarPosicaoSeletor3D(SlotsCampo, _cartasSelecionadasParaFusao.FirstOrDefault().CurrentID);
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
		
		private async void ConfirmarInvocacaoNoCampo()
		{			
			if(_cartasSelecionadasParaFusao.Count() > 1)
				await _anim.AnimaFusao(this);
			
			string idsString = string.Join(",", IDFusao);									
			var resultadoFusao = Function.Fusion(idsString);		
			bool summon = true;	
			
			if (resultadoFusao != null)
			{				
				var idsMateriais = IDFusao;
				var retorno = new Godot.Collections.Array<int>(idsMateriais)
                {
                    resultadoFusao.Id
                };
				
				var slotDestino = SlotsCampo[_indiceCampoSelecionado];
				if(_cartasSelecionadasParaFusao.Count() == 1){
					slotDestino = DefineSlotagem(PegaTipoPorId(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID))[_indiceCampoSelecionado];				
				}				
				if(_cartasSelecionadasParaFusao.Count() > 1)
				{
					var tipo = PegaTipoPorId(_cartasSelecionadasParaFusao.LastOrDefault().CurrentID);
					slotDestino = DefineSlotagem(tipo)[_indiceCampoSelecionado];				
					summon = tipo != CardTypeEnum.Spell && tipo != CardTypeEnum.Trap && tipo != CardTypeEnum.Equipment;
				}

				if (summon)
				{
					await Instancia3D(slotDestino, (int)resultadoFusao.Id);			
					LogicalPosition = slotDestino.Name.ToString();
				}
				
				_cartasSelecionadasParaFusao.Clear();
				await SairModoSelecaoCampo();
				_bloquearNavegaçãoManual = false;
				_tcsCarta?.TrySetResult(retorno);
			}
		}
		
		public async Task Instancia3D(Marker3D slotDestino, int fusao){
			bool IsEnemy = slotDestino.Name.ToString().Contains("Ini");
			Node3D novaCarta3d = Tools.InstanciaNodo(slotDestino);			
			if(IsEnemy){
				GD.Print(slotDestino.GlobalRotation.ToString());
				Vector3 rota = new Vector3(-0, 1.5707964f, 0);
				novaCarta3d.GlobalRotation += slotDestino.GlobalRotation + rota;
			}

			if (novaCarta3d.HasMethod("Setup")){
				novaCarta3d.Call("Setup", fusao, (int)_indiceCampoSelecionado, IsEnemy, IsFaceDown, slotDestino.Name);
			} 
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
				Vector2 cardPos = MaoControl.GetCarta(_indiceSelecionado)?.GlobalPosition ?? Vector2.Zero;
				Vector2 targetPos = cardPos + new Vector2(-10, 210);
				IndicadorTriangulo.ZIndex = 10;			
				Tween tween = GetTree().CreateTween();
				tween.TweenProperty(IndicadorTriangulo, "position", targetPos, 0.01f)
					 .SetTrans(Tween.TransitionType.Quad)
					 .SetEase(Tween.EaseType.Out);
			}
		}
		
		public async Task<int> SelecionarSlotAsync(Godot.Collections.Array<Marker3D> slots, bool PrimeiroTurno = false, bool camIni = false)
		{
			_tcsSlot = new TaskCompletionSource<int>();
			_indiceCampoSelecionado = 0;
			_instanciaSeletor.Visible = true;
			_bloquearNavegaçãoManual = true; // Impede que o seletor 2D da mão se mova junto

			// Primeiro posicionamento sem delay
			AtualizarPosicaoSeletorParaSlots(slots);
			while (!_tcsSlot.Task.IsCompleted)
			{
				int anterior = _indiceCampoSelecionado;			
				ProcessarNavegacao3D(slots, camIni);

				if (anterior != _indiceCampoSelecionado)
				{
					AtualizarPosicaoSeletorParaSlots(slots);
				}
				if (!STOP)
				{
					if(Input.IsActionJustPressed("ui_lb") || Input.IsActionJustPressed("ui_rb"))
					{
						if (!camIni)
						{
							var slotDestino = slots[_indiceCampoSelecionado];									
							var isEnemy = slotDestino.Name.ToString().Contains("Ini");
							var pegou = Tools.PegaNodoCarta3d(slotDestino.Name);
							if(pegou != null)
							{						
								var rotacao = pegou.Rotation;			
								if(pegou != null && pegou is Carta3d nodo)
								{
									nodo.Defesa = !nodo.Defesa;
								}		
								if(!isEnemy){
									if(rotacao == new Vector3(0,0,0)){
										pegou.Rotation = new Vector3(-0, 1.5707964f, 0);
									}else{
										pegou.Rotation = new Vector3(-0, 0.0f,0);						
									}						
								}else{
									if(rotacao == new Vector3(0,3.14f,0)){
										pegou.Rotation = new Vector3(-0, -1.5707964f, 0);
									}else{							
										pegou.Rotation = new Vector3(0,3.14f,0);						
									}	
								}						
								
							}						
						}
					}				
				}
				
				if(!STOP){
					if (Input.IsActionJustPressed("ui_accept") && !PrimeiroTurno)
					{
						var slotDestino = camIni ? SlotsCampoIni[_indiceCampoSelecionado] : SlotsCampo[_indiceCampoSelecionado];
						GD.Print($"{slotDestino.Name} Slot confirmado: " + _indiceCampoSelecionado);
						var nodo = Tools.PegaNodoCarta3d(slotDestino.Name);
						if(Tools.PegaNodoNoSlot(slotDestino))
						{
							if (!camIni)
							{
								if (!nodo.Defesa)
								{									
									LogicalPosition = slotDestino.Name;
									_tcsSlot.TrySetResult(_indiceCampoSelecionado);
								}
							}
							else
							{
								LogicalPosition = slotDestino.Name;
								_tcsSlot.TrySetResult(_indiceCampoSelecionado);
							}
						}
						else if(Tools.PodeBate(SlotsCampoIni.ToList()))
						{
							if (!camIni)
							{
								if (nodo != null && !nodo.Defesa)
								{									
									_tcsSlot.TrySetResult(_indiceCampoSelecionado);
								}
							}
							else
							{
								_tcsSlot.TrySetResult(_indiceCampoSelecionado);
							}
						}
					}
									
					if (Input.IsActionJustPressed("ui_cancel"))
					{
						await CancelarSelecaoNoCampo();						
						_tcsSlot.TrySetResult(-1);
					}
					
					if (Input.IsActionJustPressed("ui_end_phase")) // Mapeie a tecla 'V' no Input Map como "ui_end_phase"
					{
						_tcsSlot.TrySetResult(-2); // Usamos -2 para indicar "Sair da Fase"					
					}				
				}
				// Aguarda o próximo frame para o Godot não travar
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			_instanciaSeletor.Visible = false;
			_bloquearNavegaçãoManual = false;
			
			return await _tcsSlot.Task;
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
			if(tipo == CardTypeEnum.Equipment || tipo == CardTypeEnum.Spell || 
			tipo == CardTypeEnum.Trap || tipo == CardTypeEnum.Ritual){
				return SlotsCampoST;
			}
			
			return SlotsCampo;
		}
		
		public Godot.Collections.Array<Marker3D> FiltraSlot(bool inimigo = false, bool aliado = false, bool aliadoM = false, bool inimigoM = false, bool spell = false, bool trap = false)
		{
			var markers = new List<Marker3D>();
			if(inimigo)	
			{				
				markers.AddRange(Slots.Where(x => x.Name.ToString().Contains("Ini")).ToList());				
			}			
			if(inimigoM)
				markers.AddRange(Slots.Where(x => x.Name.ToString().Contains("Ini") && x.Name.ToString().Contains("M")).ToList());			
			if(aliado)				
				markers.AddRange(Slots.Where(x => !x.Name.ToString().Contains("Ini")).ToList());
			if(aliadoM)
				markers.AddRange(Slots.Where(x => !x.Name.ToString().Contains("Ini") && x.Name.ToString().Contains("M")).ToList());
				
			return new Godot.Collections.Array<Marker3D>(markers);
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
		
		public async Task<Godot.Collections.Array<int>> AguardarConfirmacaoJogadaAsync()
		{
			_tcsCarta = new TaskCompletionSource<Godot.Collections.Array<int>>();
			
			// O código aqui fica "parado" até que ConfirmarInvocacaoNoCampo() seja chamado
			var resultado = await _tcsCarta.Task;
			//depois de confirmado, setamos a task, e aqui precisamos começar as animações de mover para o centro novamente e em sequência definir qual a guardian star
			return resultado;
		}
		

			
	}
}
