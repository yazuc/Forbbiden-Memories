using Godot;
using System;
namespace fm{	
	public partial class Carta3d : Node3D
	{
		// Arraste o nó CartasBase do Inspetor para esta variável
		[Export] public CartasBase Visual; 

		public void Setup(int cardId)
		{
			if (Visual != null)
			{
				// Chama o seu método existente que carrega do CardDatabase
				Visual.DisplayCard(cardId);
			}
		}
	}
}
