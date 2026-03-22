using Godot;
using System.Threading.Tasks;

namespace fm{
	
	public partial class AnimationP : Node
	{
		[Export] public Camera3D CameraHand;
		[Export] public Mao MaoControl;
		public List<CardUi> _cartasSelecionadasParaFusao {get;set;}

		public override void _Ready()
		{
		}

		public async Task AnimaBattle(
			MaoJogador mao,
			FieldMonster meuMonstro,
			FieldMonster monstroInimigo,
			BattleSystem.BattleResult br,
			bool IsEnemy
		)
		{
			mao.STOP = true;
			await mao.Tools.TransitionTo(CameraHand, 0.5f, mao._transitionCam, mao.STOP);

			var viewport = GetViewport();
			Vector2 screenCenter = viewport.GetVisibleRect().Size / 2f;

			float distancia = 5.0f;
			Vector3 rayOrigin = CameraHand.ProjectRayOrigin(screenCenter);
			Vector3 rayNormal = CameraHand.ProjectRayNormal(screenCenter);
			Vector3 position3D = rayOrigin + rayNormal * distancia;

			Carta3d meuMonstro3d = mao.Tools.PegaNodoCarta3d(meuMonstro.zoneName);
			if (!IsInstanceValid(meuMonstro3d))
    			return;

			Carta3d monstroInimigo3d = null;

			if (monstroInimigo != null)
			{
				monstroInimigo3d = mao.Tools.PegaNodoCarta3d(monstroInimigo.zoneName);
				if (!IsInstanceValid(monstroInimigo3d))
    				return;
			}

			int diffEnemy = IsEnemy ? 1 : -1;

			var originalPos = meuMonstro3d.GlobalPosition;
			var originalRot = meuMonstro3d.Rotation;

			Vector3 originalEnemyPos = Vector3.Zero;
			Vector3 originalEnemyRot = Vector3.Zero;

			var taskMe = meuMonstro3d.TransitionCardTo(position3D + new Vector3(0,0,(diffEnemy*-2)),0.5f);

			if(monstroInimigo3d != null)
			{
				originalEnemyPos = monstroInimigo3d.GlobalPosition;
				originalEnemyRot = monstroInimigo3d.Rotation;

				monstroInimigo3d.Rotation = new Vector3(0,(diffEnemy*-1.5707964f),0);

				var taskIni = monstroInimigo3d.TransitionCardTo(position3D + new Vector3(0,0,(diffEnemy*2)),0.5f);
			}

			await Task.Delay(600);

			if(br.DefenderDestroyed && br.AttackerDestroyed)
			{
				if(monstroInimigo3d != null)
				{
					await monstroInimigo3d.Queimar();
					mao.Tools.RemoveDasInstanciadas(monstroInimigo3d);
				}

				await meuMonstro3d.Queimar();
				mao.Tools.RemoveDasInstanciadas(meuMonstro3d);
				return;
			}

			if(!br.DefenderDestroyed && br.AttackerDestroyed)
			{
				await meuMonstro3d.Queimar();
				mao.Tools.RemoveDasInstanciadas(meuMonstro3d);				

				if(monstroInimigo3d != null)
                    _ = monstroInimigo3d.TransitionCardTo(originalEnemyPos, 0.5f, originalEnemyRot);

				return;
			}

			if(br.DefenderDestroyed && !br.AttackerDestroyed)
			{
				if(monstroInimigo3d != null)
				{
					await monstroInimigo3d.Queimar();
					mao.Tools.RemoveDasInstanciadas(monstroInimigo3d);
				}

				  taskMe = meuMonstro3d.TransitionCardTo(originalPos,0.5f,originalRot);
			}

			if(!br.DefenderDestroyed && !br.AttackerDestroyed)
			{
				if(monstroInimigo3d != null)
					monstroInimigo3d.TransitionCardTo(originalEnemyPos,0.5f,originalEnemyRot);

				 taskMe = meuMonstro3d.TransitionCardTo(originalPos,0.5f,originalRot);
			}

			GD.Print("finalizou");
		}

		public async Task AnimaCartaParaMao(int ID, string name, int _indiceSelecionado, bool cancel = false)
		{			
			var nodoAlvo = MaoControl.GetCarta(_indiceSelecionado); 
			var nodoMao = MaoControl.GetCarta(_indiceSelecionado);
			if(nodoAlvo == null || nodoMao == null) 
			{
				GD.PrintErr("Não foi possível encontrar a carta para devolver à mão!");
				return;
			}

			nodoAlvo.Scale = new Vector2(1.0f, 1.0f);			
			if(cancel)
				nodoAlvo.FlipCard(false, 0.3f, 1.0f);
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(nodoAlvo, "global_position", nodoMao.GlobalPosition, 0.2f)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.Out);
			await ToSignal(tween, "finished");
			nodoAlvo.Reparent(MaoControl.Hbox);
			MaoControl.Hbox.MoveChild(nodoAlvo, _indiceSelecionado);
			if(cancel)
				_cartasSelecionadasParaFusao.Clear();
			//lastPos = Vector2.Zero;
		}

		public async Task<bool> AnimaCartaParaCentro(MaoJogador maoJogador, int ID, string name, int _indiceSelecionado)
		{
			if(_cartasSelecionadasParaFusao.Count() > 1) return false;
			bool IsFaceDown = true;
			maoJogador._tcsFaceDown = new TaskCompletionSource<bool>();
			
			var viewport = GetViewport();			
			Vector2 screenCenter = viewport.GetVisibleRect().Size / 2f;
			
			var nodoAlvo = MaoControl.GetCarta(_indiceSelecionado);// _cartasNaMao.FirstOrDefault(x => x.Name == name);			
			if(nodoAlvo == null) 
			{
				GD.PrintErr("Não foi possível encontrar a carta selecionada na mão!");
				return false;
			}
			maoJogador.lastPos = MaoControl.GetCarta(_indiceSelecionado).GlobalPosition;
			nodoAlvo.Visible = true;
			nodoAlvo.Reparent(this,true);
			nodoAlvo.Position = maoJogador.lastPos;
			Vector2 halfSize = nodoAlvo.Size * nodoAlvo.Scale / 2f;
			Vector2 targetGlobalPos = screenCenter - halfSize;
			
			Tween tween = GetTree().CreateTween();
			tween.TweenProperty(nodoAlvo, "global_position", targetGlobalPos, 0.2f)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.Out);
			maoJogador.STOP = true;
			nodoAlvo.FlipCard(IsFaceDown);
			maoJogador.STOP = false;
			var instancia = maoJogador.CriarSetaPersonalizada(screenCenter + new Vector2(100,-20));
			var instancia2 = maoJogador.CriarSetaPersonalizada(screenCenter + new Vector2(-100,-20), true);
			
			while(!maoJogador._tcsFaceDown.Task.IsCompleted) 
			{
				await ToSignal(GetTree(), "process_frame");
				if(!maoJogador.STOP){	
					if(Input.IsActionJustPressed("ui_left")  || Input.IsActionJustPressed("ui_right")){
						IsFaceDown = !IsFaceDown;
						nodoAlvo.FlipCard(IsFaceDown);
					}
					if(Input.IsActionJustPressed("ui_accept")){			
						maoJogador.IDFusao = _cartasSelecionadasParaFusao.Select(x => x.carta.Id).ToList();												
						maoJogador._tcsFaceDown?.TrySetResult(IsFaceDown);
						instancia.Visible = false;
						instancia2.Visible = false;			
						nodoAlvo.Reparent(maoJogador.MaoControl.Hbox);	
					}
					if(Input.IsActionJustPressed("ui_cancel")){
						instancia.Visible = false;
						instancia2.Visible = false;						
						maoJogador._tcsFaceDown?.TrySetCanceled();		
					}
				}
			}			
			instancia.Visible = false;
			instancia2.Visible = false;
			return await maoJogador._tcsFaceDown.Task;
		}	
		public void AlternarSelecaoFusao(CardUi carta)
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

		public async Task AnimaFusao(MaoJogador maoJogador)
		{
			maoJogador.STOP = true;
			if (_cartasSelecionadasParaFusao.Count < 2) return;

			var viewport = GetViewport();
			Vector2 screenCenter = viewport.GetVisibleRect().Size / 2f;
	
			var selecionadasOrdenadas = _cartasSelecionadasParaFusao
				.OrderBy(x => int.Parse(x.label.Text)) 
				.ToList();

			var list3d = selecionadasOrdenadas.ToList();

			var idsOrdenados = list3d.Select(x => x.carta.Id).ToList();
			maoJogador.IDFusao = idsOrdenados;

			var cartaPrincipal = list3d[0];
			float sideOffset = 250f; 
			float stackOffset = 30f;  

			Vector2 halfSize = cartaPrincipal.Size * cartaPrincipal.Scale / 2f;
			Vector2 targetGlobalPos = screenCenter - halfSize;

			var taskPrincipal = MoverParaPosicao(cartaPrincipal, targetGlobalPos + new Vector2(-sideOffset, 0), 0f);
			List<Task> tarefasIniciais = new List<Task> { taskPrincipal };
			
			list3d.FirstOrDefault().EscondeLabel();
			for (int i = 1; i < list3d.Count; i++)
			{				
				Vector2 posPilha = targetGlobalPos + new Vector2(sideOffset + (i * stackOffset), 0);
				tarefasIniciais.Add(MoverParaPosicao(list3d[i], posPilha, 0f));
				list3d[i].EscondeLabel();
			}

			await Task.WhenAll(tarefasIniciais);
			await Task.Delay(100); 
						
			for (int i = 1; i < list3d.Count; i++)
			{
				var cartaSacrificio = list3d[i];

				await MoverParaPosicao(cartaSacrificio, targetGlobalPos + new Vector2(sideOffset, 0), 0f);
				string idsString = $"{cartaPrincipal.carta.Id},{cartaSacrificio.carta.Id}";							
				var resultadoFusao = Function.Fusion(idsString);						
				await Task.Delay(200);

				Node2D pivot = new Node2D();
				AddChild(pivot);
				pivot.GlobalPosition = targetGlobalPos;

				Reparentar(cartaPrincipal, pivot);
				Reparentar(cartaSacrificio, pivot);

				cartaPrincipal.RotationDegrees = 0;
				cartaSacrificio.RotationDegrees = 0;
				cartaPrincipal.Position = new Vector2(-sideOffset, 0);
				cartaSacrificio.Position = new Vector2(sideOffset, 0);

				if(cartaSacrificio.carta.Id != resultadoFusao.Id)
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
					cartaPrincipal.Display(resultadoFusao.Id);					
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
					cartaPrincipal.Display(resultadoFusao.Id);
				}
				
				Vector2 globalPos = cartaPrincipal.GlobalPosition;
				Reparentar(cartaPrincipal, this);
				cartaPrincipal.GlobalPosition = globalPos;
				cartaPrincipal.RotationDegrees = 0; 
				
				pivot.QueueFree();

				if (i < list3d.Count - 1)
				{
					await MoverParaPosicao(cartaPrincipal, targetGlobalPos + new Vector2(-sideOffset, 0), 0f);
					await Task.Delay(200);
				}
			}

			await MoverParaPosicao(cartaPrincipal, targetGlobalPos, 0f);
			maoJogador.STOP = false;
		}

		private async Task MoverParaPosicao(Control node, Vector2 targetPos, float targetRotation = 0f)
		{
			// var glob = node.GlobalPosition;
			// node.GlobalPosition = MaoControl.GetHboxPosition(); 
			node.Reparent(this);
			node.Visible = true;
			Tween t = CreateTween().SetParallel(true);
			t.TweenProperty(node, "global_position", targetPos, 0.5f)
			 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			t.TweenProperty(node, "rotation_degrees", targetRotation, 0.5f)
			 .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			await ToSignal(t, "finished");
		}
		private void Reparentar(Control node, Node novoPai)
		{
			if (node.GetParent() != null) 
				node.GetParent().RemoveChild(node);
			novoPai.AddChild(node);
		}

		public void AnimateCard(Control carta, int index, float screenWidth)
		{
			Vector2 posFinal = carta.Position;

			carta.Position = posFinal + new Vector2(screenWidth, 0);


			var tween = GetTree().CreateTween();

			tween.Parallel().TweenProperty(carta, "position", posFinal, 0.45f)
				.SetDelay(index * 0.15f)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Cubic);

			tween.Parallel().TweenProperty(carta, "modulate:a", 1.0f, 0.45f);
		}
	}
}
