using fm;
using Godot;
using System;

public partial class Mao : Control
{
	public PackedScene CartaCena = GD.Load<PackedScene>("res://Carta/CartasBase.tscn");
	public PackedScene CartaControl = GD.Load<PackedScene>("res://testeMao/CartaControl.tscn");
	public List<int> CartasNaMao = new List<int>();
	private List<CartaControl> CartasInstanciadas = new();
	public HBoxContainer Hbox {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Hbox = GetNode<HBoxContainer>("HBoxContainer");
		CartasNaMao.Add(1);
		CartasNaMao.Add(1);
		CartasNaMao.Add(1);
		CartasNaMao.Add(1);
		CartasNaMao.Add(1);
		if(CartasNaMao.Count > 0)
			InstanciaMao(CartasNaMao);	
	}

	public void InstanciaMao(List<int> CartasNaMaoLocal)
	{
		GD.Print(CartasNaMaoLocal.Count());
		LimpaMao(Hbox);
		foreach(var item in CartasNaMaoLocal)
		{
			var cartaControlada = CartaControl.Instantiate<CartaControl>();
			cartaControlada.ID = item;
			Hbox.AddChild(cartaControlada);	
			CartasInstanciadas.Add(cartaControlada);
		}
	}

	public void LimpaMao(HBoxContainer Hbox)
	{
		foreach (Node child in Hbox.GetChildren())
		{
			child.QueueFree();
		}
		CartasInstanciadas.Clear();
	}

	public int CartasNaMaoCount()
	{
		return CartasInstanciadas.Count;
	}

	public CartaControl? GetCarta(int index)
    {
        if (index < 0 || index >= CartasInstanciadas.Count) return null;
        return CartasInstanciadas[index];
    }
	public CartasBase? GetCartaBase(int index)
    {
        if (index < 0 || index >= CartasInstanciadas.Count) return null;
        return CartasInstanciadas[index].Carta;
    }


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
