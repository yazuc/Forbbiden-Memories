global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Newtonsoft.Json;
global using Newtonsoft.Json.Converters;
global using System.Text.Json;
using fm;
public partial class GlobalUsings : Node
{
	public static GlobalUsings Instance { get; private set; }
	public int DeckIndex = 0;
	public int BoardIndex = 0;
	public int currentNpc = 0;
	public int currentBackGround;	
	public string LastLocation = "Mundo";
	public string Mundo = "res://world.tscn";
	public string Duelo = "res://Scenes/game.tscn";
	public string Story = "res://Menu/Story/Story_Control.tscn";
	public string Freeduel = "res://Menu/FreeDuel/FreeDuel.tscn";
	public string Deckeditor = "res://Menu/DeckEditor/DeckEditor.tscn";
	public string Password = "res://Menu/Password/Password.tscn";
	public string UserDeck = "res://starter_deck.txt";
	public Deck Deck = new Deck();
	public List<string> Dialogue = new List<string>();
	public CardDatabase db = CardDatabase.Instance;
	public bool stop = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		DeckIndex = 8;
		PopulateDialogue();		
		Deck.LoadDeck(Funcoes.LoadUserDeck(ProjectSettings.GlobalizePath(UserDeck)));
		GD.Print(Deck.Cards.Count());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void FadeToBlack(float tempo, string path, Node obj)
	{
		var tween = CreateTween();
			tween.TweenProperty(obj,"modulate", Colors.Black, tempo);		
			tween.Finished += () =>
			{
				if(obj is Control menu)
				{
					
					obj.SetProcess(false);
					obj.SetProcessInput(false);
					obj.SetProcessUnhandledInput(false);
					menu.Visible = false;					
				}
				SceneTransition(path);
			};
	}
	public void FadeToWhite(float tempo, Node obj)
	{
		// obj.SetProcess(true);
		// obj.SetProcessInput(true);
		// obj.SetProcessUnhandledInput(true);
		if(obj is MainMenu menu)
		{
			obj.SetProcess(true);
			obj.SetProcessInput(true);
			obj.SetProcessUnhandledInput(true);
			menu.Visible = true;
			menu.textureButtons[0].GrabFocus();
		}
		var tween = CreateTween();
			tween.TweenProperty(obj,"modulate", Colors.White, tempo);		
	}

	public void SceneTransition(string path)
	{
		var scene = GD.Load<PackedScene>(path);
		var instance = scene.Instantiate();

		GetTree().Root.AddChild(instance);
	}


	public void PopulateDialogue()
	{
		Dialogue.Add("My dear prince! Are you going to the city to play cards again!?");		
		Dialogue.Add("You are of royal blood! Walking the city streets dressed as a commoner......Have you no shame!?");
		Dialogue.Add("Quite frankly, I'm embarrassed!");
		Dialogue.Add("<Run away>");
		Dialogue.Add("<Keep listening>");
		Dialogue.Add("The Pharaoh has gotten wind of your activities...And he's quite concerned!");
		Dialogue.Add("<Run away>");
		Dialogue.Add("<Keep listening>");
		Dialogue.Add("I realize I'm to blame for teaching you the card game...But you overindulge, my prince!");
		Dialogue.Add("<Run away>");
		Dialogue.Add("<Keep listening>");
		Dialogue.Add("It is high time you put aside this ridiculous pastime and focus on your studies.");
		Dialogue.Add("<Run away>");
		Dialogue.Add("<Keep listening>");
		Dialogue.Add("It is wrong to worry the Pharaoh and the Queen so much! Please, dear Prince... Return to your room.");
		Dialogue.Add("Wait! Stop, my prince!");
		Dialogue.Add("Drat!");
		Dialogue.Add("He's gone...");
		Dialogue.Add("Prince Username.");
		Dialogue.Add("You've returned...");
		Dialogue.Add("It is well into the night...");
		Dialogue.Add("Please return to the palace.");
		Dialogue.Add("Dear prince...");
		Dialogue.Add("If you still wish to");
		Dialogue.Add("play cards...");
		Dialogue.Add("...then try your hand");
		Dialogue.Add("against me.");
		Dialogue.Add("If you lose, you must");
		Dialogue.Add("return to your room.");
		Dialogue.Add("I'm sure you'll find");
		Dialogue.Add("me a worthy opponent.");
		Dialogue.Add("<Duel>");
		Dialogue.Add("<Pass>");		
	}


}
