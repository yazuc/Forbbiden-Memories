using Godot;
using System;

public partial class CardUi : Control
{


	public Godot.Label ATK {get;set;}
	public Godot.Label DEF {get;set;}
	public Godot.Label Name {get;set;}
	public int index = 1;
	public int lastIndex = 0;

	public TextureRect CartaArte {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{		
		CartaArte = GetNode<TextureRect>("TextureRect/TextureRect");
		ATK = GetNode<Label>("ATK");
		DEF = GetNode<Label>("DEF");
		Name = GetNode<Label>("Name");
		Display(25);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(Input.IsActionJustReleased("ui_right"))
			index++;
		if(Input.IsActionJustReleased("ui_left"))
			index--;
		if(lastIndex != index)
			Display(index);
	}

	public void Display(int id)
	{
		lastIndex = id;
		var card = GlobalUsings.Instance.db.GetCardById(id);
		if(card != null)
		{
			GD.Print(card.Stars);
			Name.Text = card.Name;
			DEF.Text = "DEF: " + card.Defense.ToString();
			ATK.Text = "ATK: " + card.Attack.ToString();
			CalculaArte(id - 1);
		}
	}

	//atlas +107 x proxima carta na linha, +101y proxima carta na coluna
	//25 x, 29 y
	//caso eu queira index 1, eu preciso do baseline 5.705/5.758, multiplicar indice por 107 e contar qual coluna está
	//no caso a cada 25 soma 1 coluna

	public void CalculaArte(int id)
	{
		if(CartaArte != null && CartaArte.Texture != null)
		{			
			AtlasTexture atlas = CartaArte.Texture as AtlasTexture;
			
			//coluna max 25
			//indice 26 começa linha 2
			int maxCol = 25;
			
			Vector2 baseline = new Vector2(5.705f, 5.758f);
			Vector2 size = new Vector2(100.1f, 94.496f);
			float offSetX = 107;
			float offSetY = 101;

			var linha = id  / maxCol;
			var coluna = (id  % maxCol);
			
			Vector2 cut = new Vector2(baseline.X + (offSetX * coluna) , baseline.Y + (offSetY * linha));						
			
			//GD.Print($"Linha:{linha} Coluna: {coluna} offsetX:{offSetX} offsetY:{offSetY} cutx:{cut.X} cuty:{cut.Y}");			
			
			Rect2 finalRegion = new Rect2(cut, size);

			atlas.Region = finalRegion;
			CartaArte.Texture = atlas;
		}		
	}

}
