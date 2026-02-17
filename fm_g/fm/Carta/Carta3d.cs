using Godot;
using System;
namespace fm{	
	public partial class Carta3d : Node3D
	{
		// Arraste o nó CartasBase do Inspetor para esta variável
		[Export] public CartasBase Visual; 
		public bool SouCarta = true;
		public int carta = -1;

		public void Setup(int cardId)
		{
			if (Visual != null)
			{
				// Chama o seu método existente que carrega do CardDatabase
				Visual.DisplayCard(cardId);
				this.carta = cardId;
			}
		}
	}
}
