using Godot;
using System;
using System.ComponentModel;

namespace fm;
public partial class DeckEditor : Control
{
	[Export] public Panel selector;
	[Export] public ScrollContainer scroll;    	
	public int j = 0;	
	public bool once = false;
	[Export] public Godot.VBoxContainer decklist;	
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		Setup();		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override async void _Process(double delta)
	{				
		if (Input.IsActionJustReleased("ui_cancel"))
		{
			await GlobalUsings.Instance.GoBack();
		}
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event.IsActionPressed("ui_down"))
		{
			if(j < 39)
				j++;				
		}
		if (@event.IsActionPressed("ui_up"))
		{
			if(j > 0)
				j--;							
		}
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
				slot.Initialize(item, i);				
				i++;				
			}
		}	
	}

	public void MoveSelector(int index)
	{
		GD.Print(index);
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
