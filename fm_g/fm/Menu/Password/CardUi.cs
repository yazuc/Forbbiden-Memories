using fm;
using Godot;
using QuickType;
using System;

public partial class CardUi : Control
{

	public PackedScene Cartasbase = GD.Load<PackedScene>("res://Carta/CartasBase.tscn");
	public Godot.Label ATK {get;set;}
	public Godot.Label DEF {get;set;}
	public Godot.Label Name {get;set;}
	public Label label {get;set;}
	public int index = -1;
	public int lastIndex = 0;
	int framePos = 1;
	int maxCol = 25;			
	Vector2 baseline = new Vector2(5.705f, 5.758f);
	Vector2 size = new Vector2(100.1f, 94.496f);
	Vector2 baselineFrame = new Vector2(11.297f, 41.5f);
	Vector2 sizeFrame = new Vector2(139.0f, 197.0f);
	float offSetX = 107;
	float offSetY = 101;
	public Cards carta {get;set;}
	public TextureRect FusionUp {get;set;}
	public Vector2 PositionInHand {get;set;}
	public TextureRect CartaArte {get;set;}
	public TextureRect CartaFrame {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{		
		CartaArte = GetNode<TextureRect>("TextureRect/TextureRect");
		CartaFrame = GetNode<TextureRect>("TextureRect");
		//instanciaBase = Cartasbase.Instantiate<CartasBase>();
		ATK = GetNode<Label>("ATK");
		DEF = GetNode<Label>("DEF");
		Name = GetNode<Label>("Name");
		label = GetNode<Label>("LabelNumero");
		Display(index);
		GD.Print("Ready da silva");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void DisplayByCode(string code)
	{
		GD.Print(code);
		var card = GlobalUsings.Instance.db.GetCardByCode(code);
		if(card != null && card.CardCode != "00000000")
		{
			carta = card;
			lastIndex = card.Id;					
			PivotOffset = Size / 2;
			FlipCard(false);
			Display(card.Id);			
		}
		else
			Display(-1);
	}

	public void StatusUp(Cards card)
	{
		ATK.Text = "ATK " + card.Attack.ToString();
		DEF.Text = "DEF " + card.Defense.ToString();
	}

	public void DisplayCard(Cards card, string labelT = null)
	{
		if(card != null)
		{
			if(labelT != null)
				label.Text = labelT;
			carta = card;			
			lastIndex = card.Id;
			framePos = TipoFrame(card.Type);			
			Name.Text = card.Name;
			//GenerateBase();
			if(framePos == 1)
			{
				DEF.Text = "DEF " + card.Defense.ToString();
				ATK.Text = "ATK " + card.Attack.ToString();				
			}

			CartaArte.Visible = true;
			CalculaArte(framePos, 9, baselineFrame, sizeFrame, 145f, 0f, CartaFrame);
			CalculaArte(card.Id - 1, maxCol, baseline, size, offSetX, offSetY, CartaArte);
			
		}		
	}


	public void Display(int id)
	{
		if(id < 0)
		{			
			CartaArte.Visible = false;
			Name.Text = "";
			DEF.Text = "";
			ATK.Text = "";
			CalculaArte(0, 9, baselineFrame, sizeFrame, 145f, 0f, CartaFrame);
		}

		lastIndex = id;
		var card = GlobalUsings.Instance.db.GetCardById(id);
		DisplayCard(card);
	}

	//atlas +107 x proxima carta na linha, +101y proxima carta na coluna
	//25 x, 29 y
	//caso eu queira index 1, eu preciso do baseline 5.705/5.758, multiplicar indice por 107 e contar qual coluna está
	//no caso a cada 25 soma 1 coluna

	public void CalculaArte(int id, int maxCol, Vector2 baseline, Vector2 size, float offSetX, float offSetY, TextureRect CartaArteLocal)
	{
		if(CartaArteLocal != null && CartaArteLocal.Texture != null)
		{			
			AtlasTexture atlas = CartaArteLocal.Texture.Duplicate() as AtlasTexture;			
			var linha = id  / maxCol;
			var coluna = id  % maxCol;			
			Vector2 cut = new Vector2(baseline.X + (offSetX * coluna) , baseline.Y + (offSetY * linha));						
			
			//GD.Print($"Linha:{linha} Coluna: {coluna} offsetX:{offSetX} offsetY:{offSetY} cutx:{cut.X} cuty:{cut.Y}");			
			
			Rect2 finalRegion = new Rect2(cut, size);

			atlas.Region = finalRegion;
			CartaArteLocal.Texture = atlas;
		}		
	}

	public void SetNumeroFusao(int numero) 
	{
		label = GetNode<Label>("LabelNumero"); 
		FusionUp = GetNode<TextureRect>("FusionUp");
		label.Text = numero > 0 ? numero.ToString() : "";
		label.Visible = numero > 0;			
		FusionUp.Visible = label.Visible;	
	}
	public void EscondeLabel()
	{
		label.Visible = false;
		if(FusionUp != null)
			FusionUp.Visible = false;
	}

	public void CalculaFlip(bool flip)
	{
		var framelocal = !flip ? framePos : 0;		
		CartaArte.Visible = !flip;
		ATK.Visible = !flip;
		DEF.Visible = !flip;
		Name.Visible = !flip;
		CalculaArte(framelocal, 9, baselineFrame, sizeFrame, 145f, 0f, CartaFrame);
		
	}

	public async Task FlipCard(bool targetFaceDown, float duration = 0.3f, float customScale = 1.0f)
	{
		// 1. Criamos o Tween
		Tween tween = GetTree().CreateTween();
		PivotOffset = new Vector2(Size.X / 2, Size.Y / 2);
		// Dividimos a duração por 2 (metade para fechar, metade para abrir)
		float halfDuration = duration / 2.0f;

		// 2. Primeiro Passo: "Fecha" a carta (achata no eixo X)
		tween.TweenProperty(this, "scale:x", 0.0f, halfDuration)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.In);

		// 3. O Callback: Troca os dados e a moldura quando a carta está invisível
		tween.TweenCallback(Callable.From(() => {
			CalculaFlip(targetFaceDown);
		}));

		// 4. Segundo Passo: "Abre" a carta (volta ao scale normal)
		tween.TweenProperty(this, "scale:x", customScale, halfDuration)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
		
		await ToSignal(tween, Tween.SignalName.Finished);

		GD.Print("estou flipandooooooo");
	}

	public async Task AtivaSpellAnimation(Vector2 Screencenter)
	{	
		// Create a tween that runs on this node
		Tween tween = GetTree().CreateTween();
		Vector2 targetScale = Scale * 2f;
		
		// Set the transition type (Cubic/Expo look "magical")
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.SetEase(Tween.EaseType.Out);	
		tween.Parallel().TweenProperty(this, "global_position", Screencenter - this.Size, 0.5f);				
		tween.Parallel().TweenProperty(this, "scale", targetScale, 0.6f);	
		tween.TweenProperty(this, "modulate:a", 0f, 0.45f);			

		// Wait for the animation to finish before proceeding
		await ToSignal(tween, Tween.SignalName.Finished);
		
		GD.Print("Spell animation completed!");
	}

	public int TipoFrame(CardTypeEnum tipo)
	{
		if(tipo == CardTypeEnum.Spell || tipo == CardTypeEnum.Equipment)
			return 4;
		if(tipo == CardTypeEnum.Trap)
			return 6;
		
		return 1;
	}

}
