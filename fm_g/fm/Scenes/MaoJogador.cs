using Godot;
using System;

namespace fm{	
	public partial class MaoJogador : Node2D
	{
		[Export] public PackedScene CartaCena;
		[Export] public Node2D IndicadorTriangulo;
		[Export] public Marker3D Carta1;		
		[Export] public PackedScene Carta3d;
		[Export] public Camera3D CameraHand;
		[Export] public Camera3D CameraField;
		[Export] public Camera3D CameraInimigo;
		[Export] public PackedScene Seletor;
		[Export] public bool InvertInput = false;
		
		private Camera3D _transitionCam;
		public Godot.Collections.Array<Marker3D> SlotsCampo = new();
		public Godot.Collections.Array<Marker3D> SlotsCampoST = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoIni = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoSTIni = new ();
		
		
		[Signal] public delegate void SlotConfirmadoEventHandler(int index);
		[Signal] public delegate void CartaSelecionadaEventHandler(Godot.Collections.Array<int> ids);
		[Signal] public delegate void AlvoSelecionadoEventHandler(int index);
		
		private TaskCompletionSource<int> _tcsCampo = null;
		private bool _bloquearNavegaçãoManual = false;
		private Node3D _instanciaSeletor = null;
		public int _indiceSelecionado = 0;	
		public int _indiceCampoSelecionado = 0;		
		private bool _selecionandoLocal = false; // Estado para saber se estamos escolhendo onde colocar a carta
		private List<CartasBase> _cartasNaMao = new List<CartasBase>();
		private List<CartasBase> _cartasSelecionadasParaFusao = new List<CartasBase>();
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			_transitionCam = new Camera3D();
			AddChild(_transitionCam);
			if (Seletor != null)
			{
				_instanciaSeletor = Seletor.Instantiate<Node3D>();
				// Adicionamos à cena principal para ele não herdar transformações do Node2D
				GetTree().CurrentScene.CallDeferred("add_child", _instanciaSeletor);
				_instanciaSeletor.Visible = false;
			}
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{			
			HandleNavigation();				
		}
		private async Task<bool> HandleNavigation()
		{
			if (_bloquearNavegaçãoManual) return false;
			if (_cartasNaMao.Count == 0) return false;

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
				if (Input.IsActionJustPressed("ui_right")) _indiceSelecionado = Mathf.Min(_indiceSelecionado + 1, _cartasNaMao.Count - 1);
				else if (Input.IsActionJustPressed("ui_left")) _indiceSelecionado = Mathf.Max(_indiceSelecionado - 1, 0);

				if (anterior != _indiceSelecionado) AtualizarPosicaoIndicador();
				
				// MECÂNICA DE FUSÃO (Cima/Baixo)
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
					EntrarModoSelecaoCampo();
				}
			}
			else 
			{
				// SELEÇÃO NO CAMPO (3D)
				ControlarSelecaoDeCampo();

				if (Input.IsActionJustPressed("ui_accept")) 
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					ConfirmarInvocacaoNoCampo();
				}
				
				if (Input.IsActionJustPressed("ui_cancel")) 
				{					
					TransitionTo(CameraHand, 0.5f);
					SairModoSelecaoCampo();
				}
			}
			return true;
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

			private void EntrarModoSelecaoCampo()
			{
				_selecionandoLocal = true;
				_indiceCampoSelecionado = 0; // Começa no primeiro slot
								
				TransitionTo(CameraField, 0.5f);

				if (_instanciaSeletor != null)
				{
					_instanciaSeletor.Visible = true;
					AtualizarPosicaoSeletor3D();
				}
			}
		public void CancelarSelecaoNoCampo()
		{
			if (_tcsCampo != null && !_tcsCampo.Task.IsCompleted)
			{
				// Resolvemos com -1 para indicar que a seleção foi abortada visualmente
				_tcsCampo.TrySetResult(-1); 
			}
			
			// Desative aqui os highlights ou colisores que você ativou para a seleção
			GD.Print("Seleção de campo cancelada manualmente.");
		}
		private void ControlarSelecaoDeCampo()
		{
			int anterior = _indiceCampoSelecionado;
			int direcao = InvertInput ? -1 : 1;
			if (Input.IsActionJustPressed("ui_right"))
				_indiceCampoSelecionado = Mathf.Min(_indiceCampoSelecionado + direcao, SlotsCampo.Count - 1);
			
			if (Input.IsActionJustPressed("ui_left"))
				_indiceCampoSelecionado = Mathf.Max(_indiceCampoSelecionado - direcao, 0);

			if (anterior != _indiceCampoSelecionado)
			{
				AtualizarPosicaoSeletor3D();
			}
		}

		private void AtualizarPosicaoSeletor3D()
		{
			if (_instanciaSeletor == null || SlotsCampo == null || SlotsCampo.Count == 0)
			{
				GD.PrintErr("MaoJogador: Tentativa de atualizar seletor sem SlotsCampo configurados!");
				return;
			}

			var slotDestino = SlotsCampo[_indiceCampoSelecionado];
			
			// Usamos Tween para um movimento suave como no PS1
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(_instanciaSeletor, "global_position", slotDestino.GlobalPosition + new Vector3(0, 0.05f, 0), 0.05f);
			_instanciaSeletor.GlobalRotation = slotDestino.GlobalRotation;
		}

		private async void ConfirmarInvocacaoNoCampo()
		{			
			// Aqui você enviaria a LISTA de IDs para o seu sistema de fusão
			string idsString = string.Join(",", _cartasSelecionadasParaFusao.Select(c => c.CurrentID));									
			//precisa retornar os ids dos que foram descartados
			var resultadoFusao = await Function.Fusion(idsString);			
			if (resultadoFusao != null)
			{
				var resultid = resultadoFusao.Id;
				var idsMateriais = _cartasSelecionadasParaFusao.Select(c => c.CurrentID);
				var retorno = new Godot.Collections.Array<int>(idsMateriais);
				retorno.Add(resultid);
				var slotDestino = SlotsCampo[_indiceCampoSelecionado];

				// 3. Instancia a carta 3D do resultado final
				Node3D novaCarta3d = Carta3d.Instantiate<Node3D>();
				novaCarta3d.AddToGroup("cartas");
				GetTree().CurrentScene.AddChild(novaCarta3d);

				novaCarta3d.GlobalPosition = slotDestino.GlobalPosition;
				novaCarta3d.GlobalRotation = slotDestino.GlobalRotation;

				if (novaCarta3d.HasMethod("Setup")){
					novaCarta3d.Call("Setup", (int)resultadoFusao.Id);
				} 

				// 4. Remove todas as cartas usadas da mão
				foreach (var carta in _cartasSelecionadasParaFusao)
				{
					_cartasNaMao.Remove(carta);
					carta.QueueFree();
				}

				// 5. Limpa a lista de seleção e atualiza a interface
				_cartasSelecionadasParaFusao.Clear();
				AtualizarMao(_cartasNaMao.Select(x => x.CurrentID).ToList());
				SairModoSelecaoCampo();
				_bloquearNavegaçãoManual = false;
				//retorno.Add(resultid);
				EmitSignal(SignalName.CartaSelecionada, retorno);
			}
		}

		public void SairModoSelecaoCampo()
		{
			_selecionandoLocal = false;			
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

			for (int i = 0; i < idsCartasNoDeck.Count; i++)
			{
				int id = idsCartasNoDeck[i];				
				var novaCarta = CartaCena.Instantiate<CartasBase>();
				AddChild(novaCarta);

				// Define a posição manualmente (i * espaçamento faz o alinhamento)
				// Isso não interfere no código interno da sua carta (DisplayCard)
				novaCarta.Position = posicaoInicial + new Vector2(i * espacamentoHorizontal, 0);

				novaCarta.DisplayCard(id);
				_cartasNaMao.Add(novaCarta);
			}
			_indiceSelecionado = 0;
			if (IndicadorTriangulo != null)
			{
				GD.Print("Indicador ta vivo");
				// Make sure it's actually visible!
				IndicadorTriangulo.Visible = true;
				// Force the first position update
				AtualizarPosicaoIndicador(); 
			}
		}
		
		private void AtualizarPosicaoIndicador()
		{
			if (_cartasNaMao.Count > 0 && IndicadorTriangulo != null)
			{
				// Position above the card
				Vector2 cardPos = _cartasNaMao[_indiceSelecionado].Position;
				Vector2 targetPos = cardPos + new Vector2(0, 70);
				IndicadorTriangulo.ZIndex = 10;
				// Add a smooth Tween so it "slides" to the card
				Tween tween = GetTree().CreateTween();
				tween.TweenProperty(IndicadorTriangulo, "position", targetPos, 0.1f)
					 .SetTrans(Tween.TransitionType.Quad)
					 .SetEase(Tween.EaseType.Out);
			}
		}
		
// Método genérico para selecionar um slot no campo (Aliado ou Inimigo)
		public async Task<int> SelecionarSlotNoCampo(Godot.Collections.Array<Marker3D> slots, bool PrimeiroTurno = false)
		{			
			_bloquearNavegaçãoManual = true; // TRAVA O SELETOR 2D IMEDIATAMENTE
			_selecionandoLocal = true;
			if (!IsInsideTree()) 
			{
				GD.PrintErr("[MaoJogador] ERRO: Tentativa de selecionar slot enquanto MaoJogador está fora da SceneTree!");
				return -1;
			}

			if (_tcsCampo != null && !_tcsCampo.Task.IsCompleted) 
			{
				GD.Print("[MaoJogador] Resetando Task anterior não finalizada.");
				_tcsCampo.TrySetResult(-1);
			}
			
			_tcsCampo = new TaskCompletionSource<int>();
			
			if (slots == null || slots.Count == 0) 
			{
				GD.PrintErr("[MaoJogador] ERRO: Lista de slots vazia ou nula.");
				return -1;
			}
			
			_indiceCampoSelecionado = 0; 
			_instanciaSeletor.Visible = true;
			
			// FORÇAR ATUALIZAÇÃO IMEDIATA SEM TWEEN (para não ter delay no primeiro frame)
			var slotInicial = slots[_indiceCampoSelecionado];
			_instanciaSeletor.GlobalPosition = slotInicial.GlobalPosition + new Vector3(0, 0.05f, 0);
			_instanciaSeletor.GlobalRotation = slotInicial.GlobalRotation;

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			
			_bloquearNavegaçãoManual = true; 
			_selecionandoLocal = true;
			_indiceCampoSelecionado = 0;
			_instanciaSeletor.Visible = true;		

			while (!_tcsCampo.Task.IsCompleted)
			{
				if (!IsInsideTree()) 
				{
					GD.PrintErr("[MaoJogador] MaoJogador saiu da árvore durante a seleção!");
					break;
				}
				
				int direcao = InvertInput ? -1 : 1;
				
				int anterior = _indiceCampoSelecionado;
				if (Input.IsActionJustPressed("ui_right")) 
					_indiceCampoSelecionado = Mathf.Min(_indiceCampoSelecionado + 1, slots.Count - 1);
				if (Input.IsActionJustPressed("ui_left")) 
					_indiceCampoSelecionado = Mathf.Max(_indiceCampoSelecionado - 1 , 0);

				if (anterior != _indiceCampoSelecionado)
				{					
					AtualizarPosicaoSeletorParaSlots(slots);
				}

				if (Input.IsActionJustPressed("ui_accept"))
				{				
					if(!PrimeiroTurno)
						_tcsCampo.TrySetResult(_indiceCampoSelecionado);
				}

				if (Input.IsActionJustPressed("ui_cancel"))
				{				
					_tcsCampo.TrySetResult(-1);
				}

				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			int resultado = await _tcsCampo.Task;			

			_instanciaSeletor.Visible = false;
			_selecionandoLocal = false;
			_bloquearNavegaçãoManual = false;
			if(resultado > -1){				
				TransitionTo(CameraField, 0.5f);				
			}
			return resultado;
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
		
		public void FinalizaNodoByCard(int CardID){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					if(CardID == meuNode.carta){
						meuNode.QueueFree();										
					}
				}
			}
		}
		
		public void PrintTodosNodos3D(){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode)
					GD.Print($"Aqui temos o nodo {meuNode.carta}");
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
			
			GD.Print("MaoJogador: Slots redefinidos com sucesso via GameLoop.");
		}
		
		public void TransitionTo(Camera3D targetCam, double duration, bool MainPhase = false)
		{
			// 1. Identifica a câmera que está ativa no momento
			Viewport viewport = GetViewport();
			Camera3D currentCam = viewport.GetCamera3D();

			if (currentCam == null || currentCam == targetCam) return;
			GD.Print(targetCam.Name);
			
			//if(targetCam.Name == "CameraField" && MainPhase){
				//this.Visible = false;
			//}
			
			// 2. Prepara a câmera de transição na posição exata da origem
			_transitionCam.GlobalTransform = currentCam.GlobalTransform;
			_transitionCam.Fov = currentCam.Fov;
			_transitionCam.MakeCurrent();

			// 3. Cria o Tween
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.SetEase(Tween.EaseType.InOut);

			// Move a transição para o destino (posição e rotação)
			tween.TweenProperty(_transitionCam, "global_transform", 
				targetCam.GlobalTransform, duration);
			
			// Ajusta o FOV caso as câmeras tenham lentes diferentes
			tween.TweenProperty(_transitionCam, "fov", 
				targetCam.Fov, duration);

			// 4. Finalização: Entrega o controle para a câmera alvo real
			tween.Chain().TweenCallback(Callable.From(() => 
			{
				targetCam.MakeCurrent();
				GD.Print("Troca de câmera concluída.");
			}));
		}
	}
}
	
