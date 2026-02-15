using Godot;
using System;

namespace fm{	
	public partial class MaoJogador : Node2D
	{
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
		[Export] public PackedScene CartaCena;

		public void AtualizarMao(List<int> idsCartasNoDeck)
		{
			// Limpa a mão atual
			foreach (Node child in GetChildren())
			{
				child.QueueFree();
			}

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
			}
		}
	}
}
	
