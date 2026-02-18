using Godot;
using System;
namespace fm{	
	public partial class Carta3d : Node3D
	{
		// Arraste o nó CartasBase do Inspetor para esta variável
		[Export] public CartasBase Visual; 
		public bool SouCarta = true;
		public int carta = -1;
		public string instance = "";
		public int slotPlaced = -1;
		public bool IsEnemy = false;

		public void Setup(int cardId, int slot, bool IsEnemy)
		{
			if (Visual != null)
			{
				// Chama o seu método existente que carrega do CardDatabase
				Visual.DisplayCard(cardId);
				this.carta = cardId;
				this.slotPlaced = slot;
				this.IsEnemy = IsEnemy;
				if(IsEnemy)
					GD.Print("o inimigo invocou uma cartinha tehee");
			}
		}
	}
}
