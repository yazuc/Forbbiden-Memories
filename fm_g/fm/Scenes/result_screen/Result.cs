using fm;
using Godot;
using System;

public partial class Result : Control
{
	public Control Screen {get;set;}
	public List<Control> Screens;
	public Node node;
	public Godot.GridContainer Grid {get;set;}
	public PackedScene CardUI = GD.Load<PackedScene>("res://Menu/Password/card_ui.tscn");

	private int index = 0;
	// Called when the node enters the scene tree for the first time.
	//140/196 custom size pras cartas ficarem bem organizadas
	public override void _Ready()
	{
		Grid = GetNode<Godot.GridContainer>("Screen/SpoilScreen/Spoils");
		Screen = GetNode<Control>("Screen");
		SetProcessInput(false);
		SetProcess(false);
		SetProcessUnhandledInput(false);
		if(Screen != null)
			Screens = Screen.GetChildren().Cast<Control>().ToList();
		if(Grid != null)
		{
			InstanciaMao(new List<int>(){1,2,3,4,5,6,7,8,9,10,11,12,13,14,15});
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _UnhandledInput(InputEvent @event)
    {
		if(@event.IsActionPressed("ui_accept"))
		{
			_ = ReturnNode();
		}
		if (@event.IsActionPressed("ui_right"))
		{
			Screens[index].Visible = false;
			index++;
			
			if(index > Screens.Count - 1)
				index = 0;

			Screens[index].Visible = true;
		}
		if (@event.IsActionPressed("ui_left"))
		{
			Screens[index].Visible = false;
			index--;
			
			if(index < 0)
				index = Screens.Count() - 1;

			Screens[index].Visible = true;
		}
    }
	public async Task ReturnNode()
	{
		await GlobalUsings.Instance.GoBack(from: node);
	}
	public void Setup(Rank rank, Node node)
	{
		this.node = node;
		var nodes = GetTree().GetNodesInGroup("label");
		SetProcessInput(true);
		SetProcess(true);
		SetProcessUnhandledInput(true);
		foreach(Label item in nodes)
		{
			RankMapper.ToNode(item, rank);
		}		
	}

	public void InstanciaMao(List<int> CartasNaMaoLocal)
	{
		foreach(var item in CartasNaMaoLocal)
		{
			var cartaControlada = CardUI.Instantiate<CardUi>();
			cartaControlada.index = item;
			cartaControlada.CustomMinimumSize = new Vector2(140,196);
			cartaControlada.Theme = GD.Load<Theme>("res://Resources/tema_carta_hand.tres");
			Grid.AddChild(cartaControlada);	
		}
	}
	
}
