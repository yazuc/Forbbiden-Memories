using Godot;
using System;
namespace fm{	
	public partial class Carta3d : Node3D
	{
		// Arraste o nó CartasBase do Inspetor para esta variável
		[Export] public CartasBase Visual; 
		public bool SouCarta = true;
		public int carta = -1;
		public string markerName = "";
		public bool Defesa = false;
		public string instance = "";
		public int slotPlaced = -1;
		public bool IsEnemy = false;
		public bool IsFaceDown = false;

		public void Setup(int cardId, int slot, bool IsEnemy, bool Facedown, string markerName)
		{
			if (Visual != null)
			{
				IsFaceDown = Facedown;
				// Chama o seu método existente que carrega do CardDatabase
				Visual.DisplayCard(cardId, IsFaceDown);
				this.carta = cardId;
				this.slotPlaced = slot;
				this.IsEnemy = IsEnemy;
				this.markerName = markerName;
				if(IsEnemy)
					GD.Print("o inimigo invocou uma cartinha tehee");
			}
		}
		public void SetFaceDown(bool faceDown)
		{
			IsFaceDown = faceDown;
			if (Visual != null)
			{
				// Apenas atualiza a parte visual sem mexer no restante dos dados
				Visual.DisplayCard(this.carta, IsFaceDown);
			}
		}
		
		public SignalAwaiter TransitionCardTo(Vector3 targetGlobalPosition, float duration = 0.6f, Vector3? targetRotation = null)
		{
			Tween tween = GetTree().CreateTween();
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.SetEase(Tween.EaseType.Out);
			
			// Se targetRotation for nulo, usa o padrão de 70 graus no X
			Vector3 finalRot = targetRotation ?? new Vector3(Mathf.DegToRad(70), Rotation.Y, Rotation.Z);

			tween.TweenProperty(this, "global_position", targetGlobalPosition, duration);
			tween.Parallel().TweenProperty(this, "rotation", finalRot, duration);
			
			return ToSignal(tween, Tween.SignalName.Finished);
		}
				
	}
}
