using Godot;
using System;
namespace fm
{
	[Tool]
	public partial class CartasBase : Node2D
	{
		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
		public int CurrentID;
		private Node? _currentFrameNode;
		private AnimatedSprite2D? _arteSprite;
		private Node2D? _frameAnchor;
		public Label? _nome, _atk, _def, label;
		public CardTypeEnum Type;
		public Sprite2D FusionUp;
		public bool Facedown;

		// Caminhos para as suas cenas de frame
		private readonly Dictionary<string, string> _framePaths = new()
		{
			{ "Monstro", "res://Carta/Monstro.tscn" },
			{ "Spell",  "res://Carta/Spell.tscn" },
			{ "Trap",  "res://Carta/Trap.tscn" },
			{"Facedown", "res://Carta/Fundo.tscn"}
		};

		public override void _Ready()
		{
			_frameAnchor = GetNode<Node2D>("FrameAnchor");
			_arteSprite = GetNode<AnimatedSprite2D>("ArteRecortada");
			_nome = GetNode<Label>("Nome");
			_atk = GetNode<Label>("Ataque");
			_def = GetNode<Label>("Defesa");

			// TESTE: Carrega a carta de ID 1 assim que der Play na cena
			// Se o seu banco estiver vazio, certifique-se de rodar o SyncJson antes!
			//DisplayCard(1);
		}

		public void DisplayCard(int id, bool isFaceDown = false)
		{
			Facedown = isFaceDown;
			var cardData = CardDatabase.Instance.GetCardById(id);
			if (cardData == null) return;
			
			CurrentID = id;

			// Atualiza o frame da moldura (Monster, Spell, Trap, Equip, Ritual)
			UpdateFrame(FixType(cardData.Type));

			// Como o ID 1 é o frame 0 no AnimatedSprite2D:
			int frameIndex = cardData.Id - 1; 

			// Define a animação (geralmente "default") e o frame correto
			_arteSprite.Animation = "default";
			_arteSprite.Frame = frameIndex;
			if(!Facedown){
				_nome.Text = cardData.Name;
				if(FixType(cardData.Type) == "Monstro"){
					_atk.Text = "ATK " + cardData.Attack.ToString();
					_def.Text = "DEF " + cardData.Defense.ToString();				
				}
			}else{
				_nome.Text = "";			
				_atk.Text = "";
				_def.Text = "";				
			}
			_arteSprite.Visible = !Facedown;

			_arteSprite.Position = new Vector2(1.0f, -0.5f);
			_arteSprite.Scale = new Vector2(1.02f, 1.054f);
		}
		
		public void FlipCard(bool targetFaceDown, float duration = 0.3f)
		{
			// 1. Criamos o Tween
			Tween tween = GetTree().CreateTween();

			// Dividimos a duração por 2 (metade para fechar, metade para abrir)
			float halfDuration = duration / 2.0f;

			// 2. Primeiro Passo: "Fecha" a carta (achata no eixo X)
			tween.TweenProperty(this, "scale:x", 0.0f, halfDuration)
				 .SetTrans(Tween.TransitionType.Quad)
				 .SetEase(Tween.EaseType.In);

			// 3. O Callback: Troca os dados e a moldura quando a carta está invisível
			tween.TweenCallback(Callable.From(() => {
				DisplayCard(CurrentID, targetFaceDown);
			}));

			// 4. Segundo Passo: "Abre" a carta (volta ao scale normal)
			tween.TweenProperty(this, "scale:x", 1.0f, halfDuration)
				 .SetTrans(Tween.TransitionType.Quad)
				 .SetEase(Tween.EaseType.Out);
		}
		
		private string FixType(CardTypeEnum type){
			if(Facedown) return "Facedown";
			switch (type)
			{	
				case CardTypeEnum.Equipment:
					return "Spell";			
				case CardTypeEnum.Spell:
					return "Spell";
				case CardTypeEnum.Trap:
					return "Trap";
				default:
					// Caso existam outros tipos ou erro, retorna Monster por segurança					
					return "Monstro";
			}
		}
		
		public void SetNumeroFusao(int numero) 
		{
			label = GetNode<Label>("LabelNumero"); 
			FusionUp = GetNode<Sprite2D>("FusionUp");
			label.Text = numero > 0 ? numero.ToString() : "";
			label.Visible = numero > 0;			
			FusionUp.Visible = label.Visible;	
		}
		
		public void EscondeLabel()
		{
			label.Visible = false;
			FusionUp.Visible = false;
		}
		
		private void UpdateFrame(string tipo)
		{
			// Remove o frame antigo se existir
			if (_currentFrameNode != null)
			{
				_currentFrameNode.QueueFree();
				_currentFrameNode = null;
			}

			// Carrega e instancia a nova .tscn de frame
			if (_framePaths.TryGetValue(tipo, out string path))
			{
				var frameScene = GD.Load<PackedScene>(path);
				_currentFrameNode = frameScene.Instantiate();
				if(_currentFrameNode != null)
					_frameAnchor.AddChild(_currentFrameNode);
			}
		}
	}
}
