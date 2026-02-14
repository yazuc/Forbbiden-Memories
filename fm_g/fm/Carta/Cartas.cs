using Godot;
using System;

namespace fm{
	
	public partial class Cartas : Node2D
	{
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
		
		public void DisplayCard(int id)
		{
			// 1. Busca a carta no banco (usando o método que você já criou)
			var card = CardDatabase.Instance.GetCardById(id);
			
			if (card == null) return;

			// 2. Acessa o Sprite2D do Godot
			var sprite = GetNode<Sprite2D>("SpriteCardArt");

			// 3. Ativa o modo de "Recorte" (Region)
			sprite.RegionEnabled = true;

			// 4. Diz ao Godot exatamente qual pedaço do PNG gigante desenhar
			// Rect2(X, Y, Largura, Altura)
			sprite.RegionRect = new Rect2(card.AtlasX, card.AtlasY, 92.41f, 117.36f);
		}
		
		public void SetHighlight(bool active)
		{
			var tween = GetTree().CreateTween();
			// Se active for true, a carta sobe 50 pixels (no Godot 2D, Y negativo é para cima)
			float targetY = active ? -50f : 0f; 
			
			tween.TweenProperty(this, "position:y", targetY, 0.15f)
				 .SetTrans(Tween.TransitionType.Quad)
				 .SetEase(Tween.EaseType.Out);
		}
	}
}
