using fm;
using Godot;
using System;

public partial class Mao : Control
{
	public PackedScene CartaCena = GD.Load<PackedScene>("res://Carta/CartasBase.tscn");
	public PackedScene CartaControl = GD.Load<PackedScene>("res://testeMao/CartaControl.tscn");
	public List<int> CartasNaMao = new List<int>();
	private List<CartaControl> CartasInstanciadas = new();
	[Export] private TextureRect InterfaceDuelo {get;set;}
	public HBoxContainer Hbox {get;set;}
	public InformacaoCarta HboxCardInfo {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Hbox = GetNode<HBoxContainer>("HBoxContainer");
		HboxCardInfo = GetNode<InformacaoCarta>("../InformacaoCarta");
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
	public async void InstanciaMaoAnimated(List<int> CartasNaMaoLocal, bool animate = true)
	{
		if(!animate) 
		{
			return;
		}
		GD.Print(CartasNaMaoLocal.Count());
		LimpaMao(Hbox);
		
		float screenWidth = GetViewportRect().Size.X;
		
		for (int i = 0; i < CartasNaMaoLocal.Count; i++)
		{
			var carta = CartaControl.Instantiate<CartaControl>();
			carta.ID = CartasNaMaoLocal[i];
			
			var mod = carta.Modulate;
			mod.A = 0;
			carta.Modulate = mod;

			Hbox.AddChild(carta);
			CartasInstanciadas.Add(carta);
		}

		if (!animate)
			return;

		// espera o HBox terminar layout
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		for (int i = 0; i < CartasInstanciadas.Count; i++)
		{
			var carta = CartasInstanciadas[i];

			if (!IsInstanceValid(carta))
				continue;

			AnimateCard(carta, i, screenWidth);
		}
	}

	private void AnimateCard(Control carta, int index, float screenWidth)
	{
		Vector2 posFinal = carta.Position;

		carta.Position = posFinal + new Vector2(screenWidth, 0);


		var tween = GetTree().CreateTween();

		tween.Parallel().TweenProperty(carta, "position", posFinal, 0.45f)
			.SetDelay(index * 0.15f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);

		tween.Parallel().TweenProperty(carta, "modulate:a", 1.0f, 0.45f);
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
	public Godot.Vector2 GetHboxPosition()
	{
		return Hbox.GlobalPosition;
	}
	public Godot.Vector2 GetSlotPosition(int index)
	{
		if (index < 0 || index >= CartasInstanciadas.Count)
			return Godot.Vector2.Zero;

		return CartasInstanciadas[index].GlobalPosition;
	}
	
	public void DefineInfo(CartasBase carta)
	{
		HboxCardInfo.DefineRegion(carta.Type, carta.sign, carta.sign1, carta.nome);
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

	public void AnimateInterface(bool sobe = false)
	{		
		Vector2 target = MoveInterface(sobe);

		var tween = GetTree().CreateTween().Parallel();

		tween.TweenProperty(
			InterfaceDuelo,
			"global_position",
			target,
			1.2f
		)
		.SetEase(Tween.EaseType.Out)
		.SetTrans(Tween.TransitionType.Cubic);
		ResetHand();
		//await ToSignal(tween, Tween.SignalName.Finished);
	}

	public void ResetHand()
	{
		LimpaMao(Hbox);
		CartasNaMao.Clear();
		CartasInstanciadas.Clear();
	}


	public Vector2 MoveInterface(bool sobe = false)
	{
		//InterfaceDuelo é um texturerect
		if (sobe)
			return InterfaceDuelo.GlobalPosition - new Vector2(0f, InterfaceDuelo.Size.Y + 500);
				
		return InterfaceDuelo.GlobalPosition + new Vector2(0f, InterfaceDuelo.Size.Y + 500);				
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
