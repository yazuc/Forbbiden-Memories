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
	public void Setup(RankEnum resultado, Rank rank, Rank rankEne, Node node)
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
		foreach(Label item in nodes)
		{
			RankMapper.ToNodeEnemy(item, rankEne);
		}
		List<DropPool> dropPool = new List<DropPool>();
		for(int i = 1; i <= 15; i++)
		{			
			dropPool.Add(GlobalUsings.Instance.db.ScanDropPool(1, Converter(resultado)));
		}
		InstanciaMao(dropPool.Select(x => x.CardId).ToList());
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

	public string Converter(RankEnum rank)
	{
		return rank switch
		{
			RankEnum.SPOW => "SAPow",
			RankEnum.APOW => "SAPow",
			RankEnum.STEC => "SATec",
			RankEnum.ATEC => "SATec",
			RankEnum.BPOW => "BCD",
			RankEnum.CPOW => "BCD",
			RankEnum.DPOW => "BCD",
			RankEnum.BTEC => "BCD",
			RankEnum.CTEC => "BCD",
			RankEnum.DTEC => "BCD",

			_ => throw new ArgumentException("Invalid RankEnum value", nameof(rank)),
		};
	}
	
}
