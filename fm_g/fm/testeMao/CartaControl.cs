using fm;
using Godot;
using System;

public partial class CartaControl : Godot.Control
{
	public CartasBase Carta {get;set;}
	public SubViewport Viewport {get;set;}
	public int ID {get;set;}
	public Vector2 originalPosition;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		InstanciaCarta();
	}

	public void InstanciaCarta()
	{
		Carta = GetNode<CartasBase>("AspectRatioContainer/SubViewportContainer/SubViewport/CartasBase");
		originalPosition = Carta.Position;
		Viewport = GetNode<SubViewport>("AspectRatioContainer/SubViewportContainer/SubViewport");
		//Carta.Scale = new Vector2(1.35f, 1.35f);
		Carta.DisplayCard(ID);
	}

	public void ReparentCard(CartasBase cartaLocal)
	{
		cartaLocal.Position = originalPosition;
		cartaLocal.Reparent(Viewport);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
