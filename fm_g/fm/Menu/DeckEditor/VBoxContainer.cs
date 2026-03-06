using Godot;
using System;

namespace fm
{	
	[Tool]
	public partial class VBoxContainer : Godot.VBoxContainer
	{
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			string srcGodot = "res://starter_deck.txt";
			string srcPath = ProjectSettings.GlobalizePath(srcGodot);	
			var deck = new Deck();					
			var deckList = Funcoes.LoadUserDeck(srcPath);
			deck.LoadDeck(deckList);
			var scene1 = "res://Menu/DeckEditor/SlotCarta.tscn";
			// var scene1 = "res://Menu/FreeDuelCell2.tscn";
			var scene = GD.Load<PackedScene>(scene1);
			int i = 1;
			foreach(var item in deck.Cards)
			{
				var cell = scene.Instantiate();
				cell.Call("FillLabel", i.ToString(), item.Id.ToString(), item.Name, item.Attack.ToString(), item.Type.ToString(), item.GuardianStarA.ToString());
				AddChild(cell);
				i++;
			}
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}
}
}
