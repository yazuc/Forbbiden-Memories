using Godot;
using System;
using System.Reflection.Emit;

namespace fm
{
	public partial class SlotCarta : HBoxContainer
	{
		[Export] public Godot.Label DeckNumber;
		[Export] public Godot.Label CardNumber;
		[Export] public Godot.Label CardName;
		[Export] public Godot.Label CardStats;
		[Export] public Godot.Label CardType;
		[Export] public Godot.Label CardSign;		
		
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			string srcGodot = "res://starter_deck.txt";
			string srcPath = ProjectSettings.GlobalizePath(srcGodot);	
			var deck = new Deck();					
			var deckList = Funcoes.LoadUserDeck(srcPath);
			deck.LoadDeck(deckList);						
		
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}

		public void FillLabel(string DeckNumber, string CardNumber, string CardName, string CardStats, string CardType, string CardSign)
		{
			this.DeckNumber.Text = DeckNumber;
			this.CardNumber.Text = CardNumber;
			this.CardName.Text = CardName;
			this.CardStats.Text = CardStats;
			this.CardType.Text = CardType;
			this.CardSign.Text = CardSign;
		}
	}	
}
