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
		
		private Camera3D _transitionCam;
		public Godot.Collections.Array<Marker3D> SlotsCampo = new();
		public Godot.Collections.Array<Marker3D> SlotsCampoST = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoIni = new ();
		public Godot.Collections.Array<Marker3D> SlotsCampoSTIni = new ();	
		public bool STOP {get;set;}
		
		private TaskCompletionSource<Godot.Collections.Array<int>> _tcsCarta;
		private TaskCompletionSource<int> _tcsSlot;
		private TaskCompletionSource<int> _tcsCampo = null;
		private bool _bloquearNavegaçãoManual = false;
		private Node3D _instanciaSeletor = null;
		public int _indiceSelecionado = 0;	
		public int _indiceCampoSelecionado = 0;		
		private bool _selecionandoLocal = false; // Estado para saber se estamos escolhendo onde colocar a carta
		private List<CartasBase> _cartasNaMao = new List<CartasBase>();
		private List<CartasBase> _cartasSelecionadasParaFusao = new List<CartasBase>();
		private bool _processandoInput = false;
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
		}

		public async override void _Process(double delta)
		{			
			 if (!_processandoInput)
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
						
						await EntrarModoSelecaoCampo();
					}
				}

			}
			else 
			{
				// SELEÇÃO NO CAMPO (3D)
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
				_selecionandoLocal = true;
				_indiceCampoSelecionado = 0; // Começa no primeiro slot
								

				if (_instanciaSeletor != null)
				{
					AtualizarPosicaoSeletor3D();
					_instanciaSeletor.Visible = true;
					await TransitionTo(CameraField, 0.5f);
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
			if(!STOP){			
				if (Input.IsActionJustPressed("ui_right"))
					_indiceCampoSelecionado = Mathf.Min(_indiceCampoSelecionado + 1, SlotsCampo.Count - 1);
				
				if (Input.IsActionJustPressed("ui_left"))
					_indiceCampoSelecionado = Mathf.Max(_indiceCampoSelecionado - 1, 0);
			}

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
				bool IsEnemy = slotDestino.Name.ToString().Contains("Ini");
				if(IsEnemy){
					GD.Print(slotDestino.GlobalRotation.ToString());
					Vector3 rota = new Vector3(-0, 1.5707964f, 0);
					novaCarta3d.GlobalRotation += slotDestino.GlobalRotation + rota;
				}

				if (novaCarta3d.HasMethod("Setup")){
					novaCarta3d.Call("Setup", (int)resultadoFusao.Id, (int)_indiceCampoSelecionado, IsEnemy);
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
				_tcsCarta?.TrySetResult(retorno);
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
				// Adicionamos um pequeno atraso (delay) baseado no índice para as cartas entrarem uma por uma
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
								GD.Print("Primeira carta chegou, indicador posicionado.");
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
				// Position above the card
				Vector2 cardPos = _cartasNaMao[_indiceSelecionado].GlobalPosition;
				Vector2 targetPos = cardPos + new Vector2(-90, 50);
				IndicadorTriangulo.ZIndex = 10;
				// Add a smooth Tween so it "slides" to the card
				Tween tween = GetTree().CreateTween();
				tween.TweenProperty(IndicadorTriangulo, "position", targetPos, 0.1f)
					 .SetTrans(Tween.TransitionType.Quad)
					 .SetEase(Tween.EaseType.Out);
			}
		}
		
		// Em MaoJogador.cs
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
				
				if(!STOP){
					if (Input.IsActionJustPressed("ui_accept") && !PrimeiroTurno)
					{
						GD.Print("Slot confirmado: " + _indiceCampoSelecionado);
						_tcsSlot.TrySetResult(_indiceCampoSelecionado);
					}
									
					if (Input.IsActionJustPressed("ui_cancel"))
					{
						GD.Print("Seleção cancelada.");
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
		
		public void FinalizaNodoByCard(int CardID, bool IsEnemy = false){
			var nodes = GetTree().GetNodesInGroup("cartas").Cast<Carta3d>().ToArray();		
							
			if(IsEnemy){
				nodes = nodes.Where(x => x.IsEnemy == IsEnemy).ToArray();
				GD.Print("temos nodos:" + nodes.Count());
			}
				
			foreach(var item in nodes){				
				if(CardID == item.carta){
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
					_indiceCampoSelecionado = Mathf.Min(_indiceCampoSelecionado + 1 * dir, slots.Count - 1);											
				if (Input.IsActionJustPressed("ui_left"))
					_indiceCampoSelecionado = Mathf.Max(_indiceCampoSelecionado - 1 * dir , 0);					
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
		
		public async Task<Godot.Collections.Array<int>> AguardarConfirmacaoJogadaAsync()
		{
			_tcsCarta = new TaskCompletionSource<Godot.Collections.Array<int>>();
			
			// O código aqui fica "parado" até que ConfirmarInvocacaoNoCampo() seja chamado
			var resultado = await _tcsCarta.Task;
			return resultado;
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
			GD.Print($"Câmera {targetCam.Name} assumiu o controle.");
			STOP = false;
		}
	}
}
