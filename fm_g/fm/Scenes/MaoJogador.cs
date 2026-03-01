using Godot;
using System;

namespace fm{	
	public partial class MaoJogador : Node2D
	{
		[Export] public PackedScene CartaCena;
		[Export] public Node2D IndicadorTriangulo;
		public Node2D IndicadorSeta;
		[Export] public PackedScene Carta3d;
		[Export] public Camera3D CameraHand;
		[Export] public Camera3D CameraField;
		[Export] public Camera3D CameraInimigo;
		[Export] public PackedScene Seletor;
		
		private Camera3D _transitionCam;
		public Godot.Collections.Array<Marker3D> SlotsCampo = new();
		public Godot.Collections.Array<Marker3D> SlotsCampoST = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoIni = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoSTIni = new ();
		public Godot.Collections.Array<Marker3D> Slots = new ();	
		public bool STOP {get;set;}
		
		private TaskCompletionSource<Godot.Collections.Array<int>> _tcsCarta;
		private TaskCompletionSource<int> _tcsSlot;
		private TaskCompletionSource<int> _tcsCampo = null;
		private TaskCompletionSource<bool> _tcsFaceDown;
		bool IsFaceDown = false;
		private bool _bloquearNavegaçãoManual = false;
		private Node3D _instanciaSeletor = null;
		public int _indiceSelecionado = 0;	
		public int _indiceCampoSelecionado = 0;		
		private bool _selecionandoLocal = false; // Estado para saber se estamos escolhendo onde colocar a carta
		private List<CartasBase> _cartasNaMao = new List<CartasBase>();
		private List<CartasBase> _cartasSelecionadasParaFusao = new List<CartasBase>();
		private List<Node3D> _cartasInstanciadas = new List<Node3D>();
		private bool _processandoInput = false;		
		private List<int> IDFusao = new List<int>();
		private Vector2 lastPos = Vector2.Zero;
		public Sprite2D ComActive;
		public Sprite2D YouActive;
		public override void _Ready()
		{
			_transitionCam = new Camera3D();
			AddChild(_transitionCam);
			if (Seletor != null)
			{
				_instanciaSeletor = Seletor.Instantiate<Node3D>();
				GetTree().CurrentScene.CallDeferred("add_child", _instanciaSeletor);
				_instanciaSeletor.Visible = false;
			}
			ComActive = GetNode<Sprite2D>($"../CameraPivot/ComActive");
			YouActive = GetNode<Sprite2D>($"../CameraPivot/YouActive");
			ComActive.Visible = false;			
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
		}
		
		private async Task HandleNavigation()
		{
			if (_bloquearNavegaçãoManual) return;
			if (_cartasNaMao.Count == 0) return;

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
					if (Input.IsActionJustPressed("ui_right")) _indiceSelecionado = Mathf.Min(_indiceSelecionado + 1, _cartasNaMao.Count - 1);
					else if (Input.IsActionJustPressed("ui_left")) _indiceSelecionado = Mathf.Max(_indiceSelecionado - 1, 0);					
				}

				if (anterior != _indiceSelecionado) AtualizarPosicaoIndicador();
				
				// MECÂNICA DE FUSÃO (Cima/Baixo)
				if(!STOP){
					if (Input.IsActionJustPressed("ui_up")) 
					{
						AlternarSelecaoFusao(_cartasNaMao[_indiceSelecionado]);
					}					
					if (Input.IsActionJustPressed("ui_accept")) 
					{
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						if (_cartasSelecionadasParaFusao.Count == 0)
						{
							_cartasSelecionadasParaFusao.Add(_cartasNaMao[_indiceSelecionado]);
						}
						try 
						{
							// O await vai "explodir" aqui se TrySetCanceled for chamado
							IsFaceDown = await MoveCartaParaCentro(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID);
							
							GD.Print("Facedown confirmado: " + IsFaceDown);
							EntrarModoSelecaoCampo();
						}
						catch (OperationCanceledException) 
						{
							// O código cai aqui IMEDIATAMENTE quando aperta ui_cancel
							GD.Print("Ação cancelada pelo usuário.");
							
							if (_cartasSelecionadasParaFusao.Any()) {
								DevolveCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID, true);
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
						await TransitionTo(CameraHand, 0.5f);
						SairModoSelecaoCampo();
					}					
				}
			}
			return;
		}
		
		private void AlternarSelecaoFusao(CartasBase carta)
		{
			if (_cartasSelecionadasParaFusao.Contains(carta))
			{
				// Se já estava selecionada, removemos (Desmarcar)
				_cartasSelecionadasParaFusao.Remove(carta);
				carta.SetNumeroFusao(0); // 0 ou ocultar o label
			}
			else
			{
				// Se não estava, adicionamos à lista de fusão
				_cartasSelecionadasParaFusao.Add(carta);
			}
			
			// Atualiza visualmente os números de todas as selecionadas para manter a ordem 1, 2, 3...
			for (int i = 0; i < _cartasSelecionadasParaFusao.Count; i++)
			{
				_cartasSelecionadasParaFusao[i].SetNumeroFusao(i + 1);
			}
		}

		private async Task EntrarModoSelecaoCampo()
		{
			if(_cartasSelecionadasParaFusao.Count() == 1)
				DevolveCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID);
			_selecionandoLocal = true;
			_indiceCampoSelecionado = 0; // Começa no primeiro slot								
			if (_instanciaSeletor != null)
			{				
				AtualizarPosicaoSeletor3D(SlotsCampo, _cartasSelecionadasParaFusao.FirstOrDefault().CurrentID);
				_instanciaSeletor.Visible = true;
				await TransitionTo(CameraField, 0.5f);
			}
		}
		
		public void CancelarSelecaoNoCampo()
		{
			if (_tcsCampo != null && !_tcsCampo.Task.IsCompleted)
			{
				// Resolvemos com -1 para indicar que a seleção foi abortada visualmente
				_cartasSelecionadasParaFusao = new List<CartasBase>();
				_tcsCampo.TrySetResult(-1); 
			}
						
			if (_cartasSelecionadasParaFusao.Any() && _cartasSelecionadasParaFusao.Count() == 1) {
				DevolveCartaParaMao(_cartasSelecionadasParaFusao.FirstOrDefault().CurrentID, true);
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
		
		private async Task AnimaFusao()
		{
			if (_cartasSelecionadasParaFusao.Count < 2) return;

			var viewport = GetViewport();
			Vector2 screenCenter = viewport.GetVisibleRect().Size / 2f;
	
			var selecionadasOrdenadas = _cartasSelecionadasParaFusao
				.OrderBy(x => int.Parse(x.label.Text)) 
				.ToList();

			var idsOrdenados = selecionadasOrdenadas.Select(x => x.CurrentID).ToList();
			IDFusao = idsOrdenados;
			var list3d = idsOrdenados
				.Select(id => _cartasNaMao.First(c => c.CurrentID == id))
				.ToList();

			var cartaPrincipal = list3d[0];
			float sideOffset = 250f; 
			float stackOffset = 30f;  

			var taskPrincipal = MoverParaPosicao(cartaPrincipal, screenCenter + new Vector2(-sideOffset, 0), 0f);
			List<Task> tarefasIniciais = new List<Task> { taskPrincipal };
			
			list3d.FirstOrDefault().EscondeLabel();
			for (int i = 1; i < list3d.Count; i++)
			{				
				Vector2 posPilha = screenCenter + new Vector2(sideOffset + (i * stackOffset), 0);
				tarefasIniciais.Add(MoverParaPosicao(list3d[i], posPilha, 0f));
				list3d[i].EscondeLabel();
			}

			await Task.WhenAll(tarefasIniciais);
			await Task.Delay(100); 
						
			for (int i = 1; i < list3d.Count; i++)
			{
				var cartaSacrificio = list3d[i];

				await MoverParaPosicao(cartaSacrificio, screenCenter + new Vector2(sideOffset, 0), 0f);
				string idsString = $"{cartaPrincipal.CurrentID},{cartaSacrificio.CurrentID}";							
				var resultadoFusao = Function.Fusion(idsString);						
				await Task.Delay(200);

				Node2D pivot = new Node2D();
				AddChild(pivot);
				pivot.GlobalPosition = screenCenter;

				Reparentar(cartaPrincipal, pivot);
				Reparentar(cartaSacrificio, pivot);

				cartaPrincipal.RotationDegrees = 0;
				cartaSacrificio.RotationDegrees = 0;
				cartaPrincipal.Position = new Vector2(-sideOffset, 0);
				cartaSacrificio.Position = new Vector2(sideOffset, 0);

				if(cartaSacrificio.CurrentID != resultadoFusao.Id)
				{
					Tween spiralTween = CreateTween().SetParallel(true);
					float duration = 1.2f;
					float voltas = 1080f; 

					spiralTween.TweenProperty(pivot, "rotation_degrees", voltas, duration)
						.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);								
					spiralTween.TweenProperty(cartaPrincipal, "rotation_degrees", -voltas, duration)
						.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
					spiralTween.TweenProperty(cartaSacrificio, "rotation_degrees", -voltas, duration)
						.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
					spiralTween.TweenProperty(cartaPrincipal, "position", Vector2.Zero, duration);
					spiralTween.TweenProperty(cartaSacrificio, "position", Vector2.Zero, duration);
					
					await ToSignal(spiralTween, "finished");				
					cartaSacrificio.Visible = false;
									
					Tween impact = CreateTween();
					impact.TweenProperty(cartaPrincipal, "scale", new Vector2(1.5f, 1.5f), 0.1f);
					impact.TweenProperty(cartaPrincipal, "scale", new Vector2(1.0f, 1.0f), 0.1f);				
					cartaPrincipal.DisplayCard(resultadoFusao.Id);					
				}else
				{
					float durationSaida = 0.5f;
					Vector2 foraDaTela = new Vector2(-500, 500); 					
					Tween yeetTween = CreateTween().SetParallel(true);
					
					yeetTween.TweenProperty(cartaPrincipal, "position", foraDaTela, durationSaida)
						.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
					
					yeetTween.TweenProperty(cartaSacrificio, "position", Vector2.Zero, durationSaida)
						.SetTrans(Tween.TransitionType.Quad);
					
					await ToSignal(yeetTween, "finished");
					
					// Cleanup
					cartaPrincipal.Position = Vector2.Zero; // Reseta pro futuro					
					
					// Agora a carta de sacrifício assume o posto de principal visualmente
					cartaPrincipal.DisplayCard(resultadoFusao.Id);
				}
				
				Vector2 globalPos = cartaPrincipal.GlobalPosition;
				Reparentar(cartaPrincipal, this);
				cartaPrincipal.GlobalPosition = globalPos;
				cartaPrincipal.RotationDegrees = 0; 
				
				pivot.QueueFree();

				if (i < list3d.Count - 1)
				{
					await MoverParaPosicao(cartaPrincipal, screenCenter + new Vector2(-sideOffset, 0), 0f);
					await Task.Delay(200);
				}
			}

			await MoverParaPosicao(cartaPrincipal, screenCenter, 0f);
		}

		// Método auxiliar atualizado para aceitar rotação
		private async Task MoverParaPosicao(Node2D node, Vector2 targetPos, float targetRotation = 0f)
		{
			Tween t = CreateTween().SetParallel(true);
			t.TweenProperty(node, "global_position", targetPos, 0.5f)
			 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			t.TweenProperty(node, "rotation_degrees", targetRotation, 0.5f)
			 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			await ToSignal(t, "finished");
		}

		private void Reparentar(Node2D node, Node novoPai)
		{
			if (node.GetParent() != null) 
				node.GetParent().RemoveChild(node);
			novoPai.AddChild(node);
		}
		
		private async Task<bool> MoveCartaParaCentro(int ID)
		{
			//await AnimaFusao();
			if(_cartasSelecionadasParaFusao.Count() > 1) return false;
			bool IsFaceDown = true;
			_tcsFaceDown = new TaskCompletionSource<bool>();
			
			var viewport = GetViewport();			
			Vector2 screenCenter = viewport.GetVisibleRect().Size / 2f;
			
			var nodoAlvo = _cartasNaMao.Where(x => x.CurrentID == ID).FirstOrDefault();
			lastPos = nodoAlvo.GlobalPosition;
			
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(nodoAlvo, "global_position", screenCenter, 0.2f)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.Out);
			nodoAlvo.FlipCard(IsFaceDown);
			
			var instancia = CriarSetaPersonalizada(screenCenter + new Vector2(90,-20));
			var instancia2 = CriarSetaPersonalizada(screenCenter + new Vector2(-90,-20));
			
			while(!_tcsFaceDown.Task.IsCompleted) 
			{
				await ToSignal(GetTree(), "process_frame");
				if(!STOP){	
					if(Input.IsActionJustPressed("ui_left")  || Input.IsActionJustPressed("ui_right")){
						IsFaceDown = !IsFaceDown;
						nodoAlvo.FlipCard(IsFaceDown);
					}
					if(Input.IsActionJustPressed("ui_accept")){			
						IDFusao = _cartasSelecionadasParaFusao.Select(x => x.CurrentID).ToList();												
						_tcsFaceDown?.TrySetResult(IsFaceDown);
						instancia.Visible = false;
						instancia2.Visible = false;				
					}
					if(Input.IsActionJustPressed("ui_cancel")){
						instancia.Visible = false;
						instancia2.Visible = false;
						_tcsFaceDown?.TrySetCanceled();		
					}
				}
			}			
			instancia.Visible = false;
			instancia2.Visible = false;
			return await _tcsFaceDown.Task;
		}
		
		private void DevolveCartaParaMao(int ID, bool cancel = false)
		{			
			var nodoAlvo = _cartasNaMao.Where(x => x.CurrentID == ID).FirstOrDefault();
			if(cancel)
				nodoAlvo.FlipCard(false);
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(nodoAlvo, "global_position", lastPos, 0.2f)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.Out);
			if(cancel)
				_cartasSelecionadasParaFusao.Clear();
			//lastPos = Vector2.Zero;
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
				await AnimaFusao();
			
			string idsString = string.Join(",", IDFusao);									
			var resultadoFusao = Function.Fusion(idsString);		
			bool summon = true;	
			
			if (resultadoFusao != null)
			{				
				var idsMateriais = IDFusao;
				var retorno = new Godot.Collections.Array<int>(idsMateriais);
				retorno.Add(resultadoFusao.Id);
				
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
				
				if(summon)
					Instancia3D(slotDestino, (int)resultadoFusao.Id);			
				
				_cartasSelecionadasParaFusao.Clear();
				SairModoSelecaoCampo();
				_bloquearNavegaçãoManual = false;
				_tcsCarta?.TrySetResult(retorno);
			}
		}
		
		public async Task Instancia3D(Marker3D slotDestino, int fusao){
			bool IsEnemy = slotDestino.Name.ToString().Contains("Ini");
			Node3D novaCarta3d = PegaNodo(slotDestino);			
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
				_cartasNaMao.Remove(carta);
				carta.QueueFree();
			}		
		}

		public void SairModoSelecaoCampo()
		{
			_selecionandoLocal = false;			
			CancelarSelecaoNoCampo();
			if (_instanciaSeletor != null) _instanciaSeletor.Visible = false;
		}

		public void AtualizarMao(List<int> idsCartasNoDeck)
		{
			// Limpa a mão atual
			foreach (var carta in _cartasNaMao)
			{
				if (GodotObject.IsInstanceValid(carta)) 
				{
					carta.QueueFree();
				}
			}
			_cartasNaMao.Clear();

			float espacamentoHorizontal = 150.0f; // Ajuste para as cartas ficarem lado a lado
			Vector2 posicaoInicial = new Vector2(200, 500); // Posição da primeira carta na tela
			float larguraTela = GetViewportRect().Size.X;
			Vector2 posicaoOffScreen = new Vector2(larguraTela + 200, 500);
			
			for (int i = 0; i < idsCartasNoDeck.Count; i++)
			{
				int id = idsCartasNoDeck[i];				
				var novaCarta = CartaCena.Instantiate<CartasBase>();
				AddChild(novaCarta);

				// Define a posição manualmente (i * espaçamento faz o alinhamento)
				// Isso não interfere no código interno da sua carta (DisplayCard)
				novaCarta.Position = posicaoOffScreen;// + new Vector2(i * espacamentoHorizontal, 0);
				novaCarta.DisplayCard(id);
				_cartasNaMao.Add(novaCarta);
				
				// 2. Calcula a posição final dela na mão
				Vector2 posicaoFinal = posicaoInicial + new Vector2(i * espacamentoHorizontal, 0);

				// 3. Animação de entrada
				Tween tween = GetTree().CreateTween();
				float delay = i * 0.1f; 
				
				tween.TweenProperty(novaCarta, "position", posicaoFinal, 0.5f)
					 .SetTrans(Tween.TransitionType.Cubic) 
					 .SetEase(Tween.EaseType.Out)
					 .SetDelay(delay);
					if (i == 0 && IndicadorTriangulo != null)
					{
						IndicadorTriangulo.Visible = false;
						tween.Finished += () => 
						{
							if (GodotObject.IsInstanceValid(IndicadorTriangulo))
							{
								IndicadorTriangulo.Visible = true;
								AtualizarPosicaoIndicador();								
							}
						};
					}
			}
			_indiceSelecionado = 0;
			if (IndicadorTriangulo != null)
			{			
				IndicadorTriangulo.Visible = true;				
				AtualizarPosicaoIndicador(); 
			}
		}
		
		private void AtualizarPosicaoIndicador()
		{
			if (_cartasNaMao.Count > 0 && IndicadorTriangulo != null)
			{				
				Vector2 cardPos = _cartasNaMao[_indiceSelecionado].GlobalPosition;
				Vector2 targetPos = cardPos + new Vector2(-90, 50);
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
				if(Input.IsActionJustPressed("ui_lb") || Input.IsActionJustPressed("ui_rb"))
				{
					var slotDestino = slots[_indiceCampoSelecionado];									
					var isEnemy = slotDestino.Name.ToString().Contains("Ini");
					var pegou = PegaNodo(slotDestino, false);
					if(pegou != null)
					{						
						var rotacao = pegou.Rotation;			
						if(pegou is Carta3d nodo)
						{
							nodo.Defesa = !nodo.Defesa;
							GD.Print($"defesa: {nodo.Defesa}");
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
				
				if(!STOP){
					if (Input.IsActionJustPressed("ui_accept") && !PrimeiroTurno)
					{
						var slotDestino = camIni ? SlotsCampoIni[_indiceCampoSelecionado] : SlotsCampo[_indiceCampoSelecionado];
						GD.Print($"{slotDestino.Name} Slot confirmado: " + _indiceCampoSelecionado);
						if(PegaNodoNoSlot(slotDestino))
						{							
							_tcsSlot.TrySetResult(_indiceCampoSelecionado);
						}
						else if(PodeBate())
						{
							_tcsSlot.TrySetResult(_indiceCampoSelecionado);
						}
					}
									
					if (Input.IsActionJustPressed("ui_cancel"))
					{
						CancelarSelecaoNoCampo();						
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
		
		public void FinalizaNodoByCard(string CardID){
			var nodes = GetTree().GetNodesInGroup("cartas").Cast<Carta3d>().ToArray();		
							
			foreach(var item in nodes){				
				if(CardID == item.markerName){
					item.QueueFree();										
				}				
			}
		}
		
		public int PegaSlot(int CardID){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					if(CardID == meuNode.carta){
						return meuNode.slotPlaced;
					}
				}
			}
			//nao achou
			return -1;
		}
		
		public void Flipa(string ID)
		{
			var cartaInstanciada = PegaNodoCarta3d(ID);
			if(cartaInstanciada.IsFaceDown){
				cartaInstanciada.SetFaceDown(!cartaInstanciada.IsFaceDown);				
			}
		}
		
		public Carta3d PegaNodoCarta3d(string ID)
		{
			return _cartasInstanciadas.OfType<Carta3d>().FirstOrDefault(x => x.markerName == ID);
		}	
		
		public bool PodeBate(){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					foreach(var inimigo in SlotsCampoIni){
						if(inimigo.Name == meuNode.markerName){
							return false;
						}						
					}
				}
			}			
			return true;
		}
		
		public bool PegaNodoNoSlot(Marker3D slotDestino){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					if(slotDestino.Name == meuNode.markerName){
						return true;
					}
				}
			}			
			return false;
		}
		
		public Node3D PegaNodo(Marker3D slotDestino, bool criarNovo = true){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					if(slotDestino.Name == meuNode.markerName){
						return meuNode;
					}
				}
			}
			
			if(!criarNovo)
				return null;
			
			//nao achou
			var novaCarta3d = Carta3d.Instantiate<Node3D>();
			novaCarta3d.AddToGroup("cartas");
			GetTree().CurrentScene.AddChild(novaCarta3d);
			novaCarta3d.GlobalPosition = slotDestino.GlobalPosition;
			novaCarta3d.GlobalRotation = slotDestino.GlobalRotation;
			_cartasInstanciadas.Add(novaCarta3d);
			
			return novaCarta3d;
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
		
		public void PrintTodosNodos3D(){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode)
					GD.Print($"Aqui temos o nodo {meuNode.carta}");
			}
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
		
		public List<(string, bool)> DevolvePosicoes()
		{
			var tuple = new List<(string carta,bool defesa)>();	
			foreach(var item in _cartasInstanciadas)
			{
				if(item is Carta3d nodo)	
					tuple.Add((nodo.markerName, nodo.Defesa));
			}
			
			return tuple;
		}	
		
		public void PrintTodasInstancias(){
			foreach(var item in _cartasInstanciadas)
				PrintInstancia(item);
		}
		
		public void PrintInstancia(Node3D instancia)
		{
			if(instancia is Carta3d cartaInstanciada)
			{
				GD.Print($"carta {cartaInstanciada.carta.ToString()} - fieldzone {cartaInstanciada.markerName.ToString()} - posicao {cartaInstanciada.Defesa.ToString()}");
			}
		}
		
		public IndicadorSeta CriarSetaPersonalizada(Vector2 alvo)
		{
			var cenaSeta = GD.Load<PackedScene>("res://HUD/IndicadorSeta.tscn");
			var instancia = cenaSeta.Instantiate<IndicadorSeta>();

			instancia.PosicaoDesejada = alvo;
			instancia.OlharParaDireita = (alvo.X > GetViewportRect().Size.X / 2f); 

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
			return resultado;
		}
		
		public void SwitchTurn()
		{
			YouActive.Visible = !YouActive.Visible;
			ComActive.Visible = !ComActive.Visible;
		}
		
		public async Task TransitionTo(Camera3D targetCam, double duration, bool MainPhase = false)
		{			
			Viewport viewport = GetViewport();
			Camera3D currentCam = viewport.GetCamera3D();			
			if (currentCam == null || currentCam == targetCam) return;        
			STOP = true;
			_transitionCam.GlobalTransform = currentCam.GlobalTransform;
			_transitionCam.Fov = currentCam.Fov;
			_transitionCam.MakeCurrent();
			
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.SetEase(Tween.EaseType.InOut);

			tween.TweenProperty(_transitionCam, "global_transform", targetCam.GlobalTransform, duration);
			tween.TweenProperty(_transitionCam, "fov", targetCam.Fov, duration);
			await ToSignal(tween, Tween.SignalName.Finished);

			targetCam.MakeCurrent();
			STOP = false;
		}
		
		public async Task AnimateBattle(FieldMonster meuMonstro, FieldMonster? monstroInimigo, BattleSystem.BattleResult br, bool IsEnemy)
		{
			await TransitionTo(CameraHand, 0.5f);
			var viewport = GetViewport();			
			Vector2 screenCenter = viewport.GetVisibleRect().Size / 2f;
			
			float distancia = 5.0f; 
			Vector3 rayOrigin = CameraHand.ProjectRayOrigin(screenCenter);
			Vector3 rayNormal = CameraHand.ProjectRayNormal(screenCenter);			
			Vector3 position3D = rayOrigin + rayNormal * distancia;
			
			Carta3d meuMonstro3d = PegaNodoCarta3d(meuMonstro.zoneName);
			Carta3d monstroInimigo3d = null;
			
			if(monstroInimigo != null)
				monstroInimigo3d = PegaNodoCarta3d(monstroInimigo.zoneName);
			
			int diffEnemy = IsEnemy ? 1 : -1;
			
			var originalPos = meuMonstro3d.GlobalPosition;
			var originalPosRot = meuMonstro3d.Rotation;
			var taskMe = meuMonstro3d.TransitionCardTo(position3D + new Vector3(0, 0, (diffEnemy * -2)), 0.5f);
			if(monstroInimigo3d != null)
			{
				monstroInimigo3d.Rotation = new Vector3(-0, (diffEnemy * -1.5707964f), 0);
				var taskIni = monstroInimigo3d.TransitionCardTo(position3D + new Vector3(0,0,( diffEnemy * 2)), 0.5f);				
			}
								
			await Task.Delay(800);
			
			taskMe = meuMonstro3d.TransitionCardTo(originalPos, 0.5f, originalPosRot);
		}	
	}
}
