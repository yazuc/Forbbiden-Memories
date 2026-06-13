using Godot;
using System;
using System.ComponentModel;

namespace fm;
public partial class DeckEditor : Control
{
	[Export] public Panel selector;
	[Export] public ScrollContainer scroll;    	
	public DeckBuildEnum DeckBuild {get;set;} = DeckBuildEnum.Trunk;
	public int j = 0;	
	public bool once = false, SetupDone = false, SetupDoneDeck = false;
	[Export] public Godot.VBoxContainer decklist;	
	private List<SlotCarta> slotCartas = new List<SlotCarta>();
	public List<TextureButton> textureButtons;
	// Called when the node enters the scene tree for the first time.
	public override  void _Ready()
	{
		SetupTrunk();
		HideSlotInGroup(DeckBuild.ToString(), true);
		HideSlotInGroup(DeckBuildEnum.Deck.ToString(), false);
		// textureButtons = GetTree().GetNodesInGroup("button").Cast<TextureButton>().ToList();	
		// textureButtons[0].GrabFocus();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{				
		MoveSelector(j);		
	}
	public override async void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_left") || @event.IsActionPressed("ui_right"))
		{
			if (@event.IsActionPressed("ui_right"))
			{
				GD.Print("called");				
				DeckBuild = DeckBuildEnum.Deck;
				SetupDeck();	
			}
			if (@event.IsActionPressed("ui_left"))
			{
				GD.Print("called left");				
				DeckBuild = DeckBuildEnum.Trunk;
				SetupTrunk();
			}
			GetViewport().SetInputAsHandled();
		}
		if (Input.IsActionJustReleased("ui_cancel"))
		{
			await GlobalUsings.Instance.GoBack();
		}
	}

    public override void _UnhandledInput(InputEvent @event)
    {
		// var index = textureButtons.IndexOf(textureButtons.FirstOrDefault(x => x.HasFocus()));			
		
		// if (@event.IsActionPressed("ui_lb"))
		// {			
		// 	if(index <= 0) index = textureButtons.Count;		
		// 	textureButtons[index - 1].GrabFocus(); 
		// 	Filter((TipoFiltro)index - 1);

		// }
		// if (@event.IsActionPressed("ui_rb"))
		// {
		// 	if(index >= textureButtons.Count - 1) index = -1;			
		// 	textureButtons[index + 1].GrabFocus(); 
		// 	Filter((TipoFiltro)index + 1);
		// }		
        if(@event.IsActionPressed("ui_down"))
		{
			if(j < decklist.GetChildren().OfType<SlotCarta>().Count(x => x.Visible) - 1)
				j++;				
		}
		if (@event.IsActionPressed("ui_up"))
		{
			if(j > 0)
				j--;							
		}
    }

	public void Filter(TipoFiltro tipo)
	{
		if(tipo == TipoFiltro.Numero)
			slotCartas = slotCartas.OrderBy(x => x.item.Id).ToList();					
		if(tipo == TipoFiltro.Monstro)
			slotCartas = slotCartas.OrderByDescending(x => !x.item.IsSpellTrap()).ToList();
		if(tipo == TipoFiltro.Ataque)
			slotCartas = slotCartas.OrderByDescending(x => x.item.Attack).ToList();
		if(tipo == TipoFiltro.Defesa)
			slotCartas = slotCartas.OrderByDescending(x => x.item.Defense).ToList();
		if(tipo == TipoFiltro.Tipo)
			slotCartas = slotCartas.OrderBy(x => x.item.Type).ToList();		

		for (int i = 0; i < slotCartas.Count; i++)
		{
			decklist.MoveChild(slotCartas[i], i);
		}
		j = 0;
		MoveSelector(j);
	}

	public void Setup()
	{
		var scene1 = "res://Menu/DeckEditor/slot_carta.scn";
		var scene = GD.Load<PackedScene>(scene1);
		int i = 1;
		foreach(var item in GlobalUsings.Instance.Deck.Cards)
		{
			var cell = scene.Instantiate();				
			if(cell is SlotCarta slot)
			{
				decklist.AddChild(slot);
				slot.DeckBuild = DeckBuild;
				slot.Initialize(item, i);		
				slot.AddToGroup(DeckBuild.ToString());
				slotCartas.Add(slot);		
				i++;				
			}
		}	
	}

	public void SetupTrunk()
	{
		if (SetupDone)
		{
			HideSlotInGroup(DeckBuildEnum.Deck.ToString(), false);			
			HideSlotInGroup(DeckBuildEnum.Trunk.ToString(), true);
			return;
		}

		var scene1 = "res://Menu/DeckEditor/slot_carta.scn";
		var scene = GD.Load<PackedScene>(scene1);
		int i = 1;
		foreach(var item in GlobalUsings.Instance.db.GetAllCards().OrderBy(x => x.Id))
		{
			var cell = scene.Instantiate();				
			if(cell is SlotCarta slot)
			{
				decklist.AddChild(slot);
				slot.DeckBuild = DeckBuildEnum.Trunk;
				slot.Initialize(item, i);		
				slot.AddToGroup(DeckBuild.ToString());
				slotCartas.Add(slot);		
				i++;				
			}
		}	
		SetupDone = true;
	}

	public void SetupDeck()
	{
		if (SetupDoneDeck)
		{
			HideSlotInGroup(DeckBuildEnum.Deck.ToString(), true);
			HideSlotInGroup(DeckBuildEnum.Trunk.ToString(), false);
			return;
		}

		var scene1 = "res://Menu/DeckEditor/slot_carta.scn";
		var scene = GD.Load<PackedScene>(scene1);
		int i = 1;
		foreach(var item in GlobalUsings.Instance.Deck.Cards)
		{
			var cell = scene.Instantiate();				
			if(cell is SlotCarta slot)
			{
				decklist.AddChild(slot);
				slot.DeckBuild = DeckBuildEnum.Deck;
				slot.Initialize(item, i);		
				slot.AddToGroup(DeckBuild.ToString());
				slotCartas.Add(slot);		
				i++;				
			}
		}	
		SetupDoneDeck = true;	
		HideSlotInGroup(DeckBuildEnum.Deck.ToString(), true);
		HideSlotInGroup(DeckBuildEnum.Trunk.ToString(), false);
	}

	public void HideSlotInGroup(string group, bool visible = false)
	{
		if(!visible)
			GetTree().CallGroup(group, "hide");
		if(visible)
			GetTree().CallGroup(group, "show");
		// var nodes = GetTree().GetNodesInGroup(group).Cast<Control>().ToList();
		// foreach (var node in nodes)
		// {
		// 	node.Visible = visible;
		// }
	}

	public void MoveSelector(int index)
	{
		if (decklist.GetChildCount() == 0)
			return;

		if (index < 0 || index >= decklist.GetChildCount())
			return;

		var slot = decklist.GetChild(index) as HBoxContainer;
		if (slot == null)
		{
			return;
		}
		
		scroll.EnsureControlVisible(slot);
		//scroll.ScrollVertical = (int)slot.Position.Y;
		slot.ForceUpdateTransform();

		var rect = slot.GetGlobalRect();			
		selector.GlobalPosition = rect.Position;
		selector.Size = rect.Size;			
	}
}
