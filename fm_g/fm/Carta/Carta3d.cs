using Godot;
using System;
namespace fm{	
	public partial class Carta3d : Node3D
	{
		// Arraste o nó CartasBase do Inspetor para esta variável
		[Export] public CartasBase Visual; 
		[Export] public Sprite3D SpriteDaCarta;
		private float _alturaDaCarta = 5f; // Ajuste para o tamanho real da sua carta em metros
		private float _velocidadeDeQueima = 1.5f; // segundos
		public bool SouCarta = true;
		public int carta = -1;
		public string markerName = "";
		public bool Defesa = false;
		public string instance = "";
		public int slotPlaced = -1;
		public bool IsEnemy = false;
		public bool IsFaceDown = false;
		private Tween _activeTween;
		public override void _Ready()
		{
			if (SpriteDaCarta != null)
			{				
				if (SpriteDaCarta.MaterialOverride is ShaderMaterial mat)
				{
					ShaderMaterial novoMat = (ShaderMaterial)mat.Duplicate();
					SpriteDaCarta.MaterialOverride = novoMat;								
					novoMat.SetShaderParameter("albedo_texture", SpriteDaCarta.Texture);
				}
			}
		}
		
		public override void _Process(double delta)
		{
		
		}

		public void Setup(int cardId, int slot, bool IsEnemy, bool Facedown, string markerName)
		{
			if (Visual != null)
			{
				IsFaceDown = Facedown;
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
		
		private void PrepararMaterial()
		{
			if (SpriteDaCarta != null && SpriteDaCarta.MaterialOverride is ShaderMaterial mat)
			{
				// Duplica para que cada carta queime sozinha
				ShaderMaterial novoMat = (ShaderMaterial)mat.Duplicate();
				SpriteDaCarta.MaterialOverride = novoMat;

				// Alimenta o shader com a textura que está no Sprite3D
				if (SpriteDaCarta.Texture != null)
				{
					novoMat.SetShaderParameter("albedo_texture", SpriteDaCarta.Texture);
				}
			}
		}

		public async Task Queimar()
		{
			if (SpriteDaCarta?.MaterialOverride is ShaderMaterial mat)
			{
				Tween tween = GetTree().CreateTween();
				
				// Anima o progresso de 0.0 até 1.1 
				// (Usamos 1.1 para garantir que o ruído passe totalmente do topo da carta)
				tween.TweenProperty(mat, "shader_parameter/progress", 1.1f, _velocidadeDeQueima)
					 .SetTrans(Tween.TransitionType.Quad) // Quad ou Cubic fica mais natural que Linear
					 .SetEase(Tween.EaseType.In);

				await ToSignal(tween, Tween.SignalName.Finished);
				
				// Remove a carta da cena
				QueueFree();
			}
		}
				
	}
}
