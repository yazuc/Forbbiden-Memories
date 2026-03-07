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
			string srcGodot = "res://starter_deck.txt";
			string srcPath = ProjectSettings.GlobalizePath(srcGodot);	
			var deck = new Deck();					
			var deckList = Funcoes.LoadUserDeck(srcPath);
			deck.LoadDeck(deckList);
			var scene1 = "res://Menu/DeckEditor/SlotCarta.tscn";
			var scene = GD.Load<PackedScene>(scene1);
			int i = 1;
			foreach(var item in deck.Cards)
			{
				var cell = scene.Instantiate();				
				if(cell is SlotCarta slot)
				{
					slot.FillLabel(
						i.ToString(),
						item.Id.ToString(),
						item.Name,
						item.Attack.ToString(),
						item.Type.ToString(),
						item.GuardianStarA.ToString()
					);					
					AddChild(slot);
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
				j++;				
			if(Input.IsActionJustPressed("ui_up"))
				j--;							
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
