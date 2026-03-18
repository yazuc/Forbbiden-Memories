using Godot;
using System;

namespace fm
{	
	
	public partial class VBoxContainer : Godot.VBoxContainer
	{
		public Panel selector;
		public ScrollContainer scroll;    		
		public int j = 0;
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			var scene1 = "res://Menu/DeckEditor/slot_carta.scn";
			var scene = GD.Load<PackedScene>(scene1);
			int i = 1;
			GD.Print("cards " + GlobalUsings.Instance.Deck.Cards.Count());
			foreach(var item in GlobalUsings.Instance.Deck.Cards)
			{
				var cell = scene.Instantiate();				
				if(cell is SlotCarta slot)
				{
					AddChild(slot);
					slot.Initialize(item, i);				
					i++;
				}
			}		
			
						
			selector = GetParent().GetParent().GetNode<Panel>("CardSeletor");
			scroll = GetParent<ScrollContainer>();		
			MoveSelector(j);
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			MoveSelector(j);
			if(Input.IsActionJustPressed("ui_down"))
			{
				if(j < 39)
					j++;				
			}
			if (Input.IsActionJustPressed("ui_up"))
			{
				if(j > 0)
					j--;							
			}
			if (Input.IsActionJustReleased("ui_cancel"))
			{
				GlobalUsings.Instance.FadeToWhite(0.3f, GetTree().CurrentScene);
				var node = GetTree().Root.FindChild("DeckEditor", true, false);

				if (node != null)
					node.QueueFree();
			}
		}	

		public void MoveSelector(int index)
		{
			if (GetChildCount() == 0)
				return;

			if (index < 0 || index >= GetChildCount())
				return;

			var slot = GetChild(index) as HBoxContainer;
			if (slot == null)
			{
				GD.Print("Slot inválido no índice: ", index); 
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
}
