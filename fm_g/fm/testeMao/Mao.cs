using fm;
using Godot;
using System;

public partial class Mao : Control
{
	public PackedScene CartaCena = GD.Load<PackedScene>("res://Carta/CartasBase.tscn");
	public PackedScene CartaControl = GD.Load<PackedScene>("res://testeMao/CartaControl.tscn");
	public PackedScene CardUI = GD.Load<PackedScene>("res://Menu/Password/card_ui.tscn");
	public List<int> CartasNaMao = new List<int>();
	private List<CardUi> CartasInstanciadas = new();
	[Export] private TextureRect InterfaceDuelo {get;set;}
	[Export] private AnimationP animationPlayer {get;set;}
	public HBoxContainer Hbox {get;set;}
	public InformacaoCarta HboxCardInfo {get;set;}
	public CartasBase PreloadedCard;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PreloadedCard = CartaCena.Instantiate<CartasBase>();
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
			var cartaControlada = CardUI.Instantiate<CardUi>();
			cartaControlada.index = item;
			cartaControlada.Theme = GD.Load<Theme>("res://Resources/tema_carta_hand.tres");
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
			var carta = CardUI.Instantiate<CardUi>();
			carta.index = CartasNaMaoLocal[i];
			
			var mod = carta.Modulate;
			mod.A = 0;
			carta.Modulate = mod;

			Hbox.AddChild(carta);
			CartasInstanciadas.Add(carta);
			GD.Print(carta.Size);
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

			animationPlayer.AnimateCard(carta, i, screenWidth);
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
	public Godot.Vector2 GetHboxPosition()
	{
		return Hbox.GlobalPosition;
	}
	public Godot.Vector2 GetSlotPosition(int index)
	{
		if (index < 0 || index >= CartasInstanciadas.Count)
			return Vector2.Zero;

		return CartasInstanciadas[index].GlobalPosition;
	}
	
	public void DefineInfo(QuickType.Cards carta)
	{
		HboxCardInfo.DefineRegion(carta.Type, (int)carta.GuardianStarA, (int)carta.GuardianStarB, carta.Name);
	}

	public CardUi? GetCarta(int index)
    {
        if (index < 0 || index >= CartasInstanciadas.Count) return null;
        return CartasInstanciadas[index];
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
