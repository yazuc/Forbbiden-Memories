using Godot;
using System;

namespace fm{	
	public partial class MaoJogador : Node2D
	{
		[Export] public PackedScene CartaCena;
		[Export] public Node2D IndicadorTriangulo;
		private int _indiceSelecionado = 0;
		private List<CartasBase> _cartasNaMao = new List<CartasBase>();
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			HandleNavigation();
		}
		private void HandleNavigation()
		{
			if (_cartasNaMao.Count == 0) return;

			int anterior = _indiceSelecionado;

			if (Input.IsActionJustPressed("ui_right"))
			{
				_indiceSelecionado = Mathf.Min(_indiceSelecionado + 1, _cartasNaMao.Count - 1);
				GD.Print($"right pressed: {_indiceSelecionado}");
			}
			else if (Input.IsActionJustPressed("ui_left"))
			{
				GD.Print($"left pressed: {_indiceSelecionado}");
				_indiceSelecionado = Mathf.Max(_indiceSelecionado - 1, 0);
			}

			if (anterior != _indiceSelecionado)
			{
				AtualizarPosicaoIndicador();
			}

			if (Input.IsActionJustPressed("ui_accept")) // Usually Space or Enter
			{
				GD.Print("Invocando: " + _cartasNaMao[_indiceSelecionado]._nome.Text);
				// Trigger your summon logic here
			}
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
	}
}
	
