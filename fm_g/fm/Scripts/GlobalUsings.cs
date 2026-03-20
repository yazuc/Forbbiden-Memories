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
		Dialogue.Add("A wasted effort, boy!");
		Dialogue.Add("You lack the power to defeat me!");
		Dialogue.Add("<Duel>");
	}


}
