global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Newtonsoft.Json;
global using Newtonsoft.Json.Converters;
global using System.Text.Json;
using fm;
[GlobalClass]
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
	public string MainMenu = "res://Menu/Principal/MainMenu.tscn";
	public string GameOver = "res://Scenes/gameover_screen/GameOver.tscn";
	public string UserDeck =  "d002";
	public ActiveScene activeScene;	

	public Deck Deck = new Deck();
	public List<string> Dialogue = new List<string>();
	public CardDatabase db = CardDatabase.Instance;
	public DialogicSingleton dialogic;
	public bool stop = false;
	private static bool _dueloIniciado = false;
	private Stack<Node> _sceneStack = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		dialogic = new DialogicSingleton();
    	AddChild(dialogic);
		DeckIndex = 1;
		#if DEBUG
			UserDeck = "d002";
		#endif
		Deck.LoadDeck(db.GetUserDeck(UserDeck));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public async Task FadeToBlack(float tempo, string path, Node obj)
	{
		if(path == "introseq")
		{
			IniciarDialogoNoMundo("res://Resources/timelines/introseq.dtl");
			return;
		}
		await ScreenTransition.Instance.FadeOut(0.5f);
		SceneTransition(path, obj);
		await ScreenTransition.Instance.FadeIn(0.5f);
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

	public void SceneTransition(string path, Node from = null)
	{		
		var current = GetTree().CurrentScene;
		
		var scene = GD.Load<PackedScene>(path);
		Control? storyControl = GetNodeOrNull<Control>("../StoryUI");
		Node? instance = storyControl;
		if(storyControl == null || path != Story)
		{
			instance = scene.Instantiate();
		}

	
		// Hide current scene and push to stack
		if (from != null)
		{
			_sceneStack.Push(from);

			from.SetProcess(false);
			from.SetProcessInput(false);
			from.SetProcessUnhandledInput(false);

			if (from is Control c)
				c.Visible = false;
		}
		PrintStackState();
		if(path != Story)
		{
			GetTree().Root.AddChild(instance);
			GetTree().CurrentScene = instance;			
		}
		
	}

	public void ChangeSceneToMainMenu()
	{
		var scene = GetNode<MainMenu>("../MainMenu");
		if(scene != null)
		{
			scene.Visible = true;
			scene.SetProcess(true);
			scene.SetProcessInput(true);
			scene.SetProcessUnhandledInput(true);
			scene.textureButtons[0].GrabFocus();		
			_sceneStack = new();	
		}
	}

	//goback precisa de algumas parametrizações para quando o duelo inicia de um dialogo
	public async Task GoBack(bool pop = false, Node from = null)
	{				
		AdjustToWorld();
		if (_sceneStack.Count == 0)
			return;

		await ScreenTransition.Instance.FadeOut(0.5f);
		var currentTree = GetTree();
		var current = currentTree.CurrentScene;

		if(current == null)
		{
			current = from;
		}

		if (current != null)
			current.Free();

		PrintStackState();
		var previous = pop ? _sceneStack.Peek() : _sceneStack.Pop();
		GD.Print(previous.Name);

		if (previous != null)
		{
			previous.SetProcess(true);
			previous.SetProcessInput(true);
			previous.SetProcessUnhandledInput(true);

			if (previous is Control c)
				c.Visible = true;
			if (previous is MainMenu menu)
				menu.textureButtons[0].GrabFocus();
			
			await ScreenTransition.Instance.FadeIn(0.5f);
		}
	}

	public void AdjustToWorld()
	{
		if(activeScene == ActiveScene.Duel)
		{
			var world = GetNodeOrNull<World>("../World");
			if(world == null)
				return;
			world.SetProcess(true);
			world.Visible = true;
			activeScene = ActiveScene.World;
		}		
	}

	public async void GameOverTransition()
	{
		await FadeToBlack(0.5f, GameOver, this);
		await Task.Delay(500);
		await ScreenTransition.Instance.FadeOut(0.5f);
		
		var gameover = GetNodeOrNull<Control>("../GameOver");
		var world = GetNodeOrNull<World>("../World");
		
		if(gameover != null)		
			gameover.QueueFree();
		if(world != null)		
			world.QueueFree();

		await Task.Delay(500);
		ChangeSceneToMainMenu();
		await ScreenTransition.Instance.FadeIn(0.9f);

	}

	public void PrintStackState()
	{
		GD.Print("--- ESTADO DA PILHA (Topo para Fundo) ---");
		if (_sceneStack.Count == 0)
		{
			GD.Print("Pilha Vazia");
			return;
		}

		int index = 0;
		foreach (Node node in _sceneStack)
		{
			// Imprime o índice, o nome do nó e o tipo da classe
			GD.Print($"{index}: Nome: {node.Name} | Tipo: {node.GetType().Name}");
			index++;
		}
		GD.Print("---------------------------------------");
	}

	public void IniciarDialogoNoMundo(string timelinePath)
	{
		// 1. Pegamos o World (Cena de exploração)
		var worldNode = GetTree().CurrentScene;
		
		// 2. Salvamos o World na Stack e carregamos a cena de Story/Dialogic
		// Isso garante que o 'World' esteja no topo da pilha
		SceneTransition(Story, worldNode);

		// 3. Iniciamos a conversa dentro da nova cena carregada
		dialogic.StartConversation(timelinePath);
	}

	public void IniciarDialogoNoMundo(string timelinePath, string Label)
	{
		//timelines que precisam de label, precisam ser definidos estilo e bg para que não falte na hora de apresentar
		var worldNode = GetTree().CurrentScene;					
		SceneTransition(Story, worldNode);
		dialogic.StartConversation(timelinePath, Label);
	}

	public async void IniciarDuelo()
	{
		if (!_dueloIniciado)
		{
			int index = (int)dialogic.GetVariable("DeckIndex");
			activeScene = ActiveScene.Duel;
			GD.Print(index);
			DeckIndex = index;
			await FadeToBlack(2.5f, Duelo, this);			
			_dueloIniciado = true;
		}
	}
	public void PrintTree(Node node = null, string indent = "")
	{
		if (node == null)
		{
			node = ((SceneTree)Engine.GetMainLoop()).Root;
		}

		GD.Print($"{indent}- {node.Name} ({node.GetType()})");

		foreach (Node child in node.GetChildren())
		{
			PrintTree(child, indent + "  ");
		}
	}


	public async void GoBackOverworld(float tempo)
	{
		await FadeToBlack(0.5f, Mundo, this);		
	}

}
