using Godot;
using System;
namespace fm{	
	public partial class Carta3d : Node3D
	{
		// Arraste o nó CartasBase do Inspetor para esta variável
		[Export] public CartasBase Visual; 
		[Export] public Sprite3D SpriteDaCarta;
		[Export] public GpuParticles3D SistemaDeFogo; // O nó criado no passo 2
		private float _alturaDaCarta = 2.0f; // Ajuste para o tamanho real da sua carta em metros
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
				// Garante material único
				if (SpriteDaCarta.MaterialOverride is ShaderMaterial mat)
				{
					ShaderMaterial novoMat = (ShaderMaterial)mat.Duplicate();
					SpriteDaCarta.MaterialOverride = novoMat;
					
					// Atribui a textura do Viewport ao shader. 
					// Em Sprite3D configurado com Viewport, a textura vem do ViewportTexture.
					novoMat.SetShaderParameter("albedo_texture", SpriteDaCarta.Texture);
				}
			}
			if (SistemaDeFogo != null) SistemaDeFogo.Emitting = false;
		}

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
			if (SpriteDaCarta?.MaterialOverride is ShaderMaterial mat && SistemaDeFogo != null)
			{
				// Liga o fogo
				SistemaDeFogo.Emitting = true;
				
				// Posição inicial do emissor (na base da carta, localmente)
				SistemaDeFogo.Position = new Vector3(0, -_alturaDaCarta / 2.0f, 0.01f); // 0.01f para ficar levemente na frente

				Tween tween = GetTree().CreateTween();
				
				// Anima o progresso de baixo para cima no Shader (0.0 -> 1.0)
				tween.Parallel().TweenProperty(mat, "shader_parameter/progress", 1.0f, _velocidadeDeQueima)
					 .SetTrans(Tween.TransitionType.Linear);

				// Anima o EMISSOR de partículas para cima (sincronizado com o corte)
				Vector3 posicaoFinal = new Vector3(0, _alturaDaCarta / 2.0f, 0.01f);
				tween.Parallel().TweenProperty(SistemaDeFogo, "position", posicaoFinal, _velocidadeDeQueima)
					 .SetTrans(Tween.TransitionType.Linear);

				await ToSignal(tween, Tween.SignalName.Finished);
				
				// Deixa as últimas partículas sumirem antes de deletar
				SistemaDeFogo.Emitting = false;
				await ToSignal(GetTree().CreateTimer(SistemaDeFogo.Lifetime), SceneTreeTimer.SignalName.Timeout);
				
				// Remove a carta
				QueueFree();
			}
		}
				
	}
}
