using Godot;
using System;
using System.Threading.Tasks;

namespace fm
{
	public partial class Main : Node
	{
		[Export] public MaoJogador? MaoVisual;
		[Export] public Camera3D CameraHand;
		[Export] public Camera3D CameraField;
		[Export] public Camera3D CameraInimigo;
		[Export] public Node3D CameraPivot;
		[Export] public Godot.Collections.Array<Marker3D> SlotsCampo;
		[Export] public Godot.Collections.Array<Marker3D> SlotsCampoIni;
		private GameLoop gL;
		
		public override async void _Ready()
		{		
			GD.Print("Iniciando Banco de Dados e Jogo...");
			CameraHand.Current = true;
			CameraField.Current = false;
			

			if (MaoVisual == null)
			{
				var root = GetTree().CurrentScene;
				MaoVisual = root.FindChild("MaoJogador", true, false) as MaoJogador;
			}
			
			// 1. Instanciar Database
			var db = CardDatabase.Instance;
			//db.SyncJsonToDatabase("cards.json"); // Load cards from JSON into the database if not already loaded
			//db.MapAtlasCoordinates();
			// 2. Carregar Deck e Cartas
			var deck = new Deck();
			var deckIni = new Deck();
			// Certifique-se que o caminho do arquivo está acessível pelo Godot
			//FmStarterDeckGenerator generator = new FmStarterDeckGenerator();
			//List<QuickType.Cards> starterDeck = generator.GenerateStarterDeck(db.GetAllCards());          
			//Funcoes.WriteCardsToFile(starterDeck, "starter_deck_ini.txt");
			
			var deckList = Funcoes.LoadUserDeck("/mnt/Nvme/fm/fm_g/fm/Scripts/native_c#/starter_deck.txt");
			deck.LoadDeck(deckList);
			GD.Print(deckList.Count());
			
			var deckListIni = Funcoes.LoadUserDeck("/mnt/Nvme/fm/fm_g/fm/Scripts/native_c#/starter_deck_ini.txt");
			deckIni.LoadDeck(deckListIni);
			// 3. Inicializar o GameLoop
			// Passando Alice e Bob como os duelistas
			if (MaoVisual != null)
			{
				GD.Print(SlotsCampo.Count().ToString());
				gL = new GameLoop(
					new Player("Alice", deck.Cards, SlotsCampo, 8000), 
					new Player("Bob", deckIni.Cards, SlotsCampoIni, 8000),
					MaoVisual,
					CameraHand,
					CameraField,
					CameraInimigo,
					CameraPivot
				);
				gL.Initialize();
			}
			else
			{
				GD.PrintErr("CRÍTICO: O nó MaoVisual não foi encontrado na cena!");
			}						
			// Teste de Fusão (se quiser testar agora)
			// string result = await fm.Function.Fusion("177,296,211");
			// GD.Print($"Resultado da Fusão: {result}");
		}
		// Inside your Main Node script (e.g., Main.cs)
		public override void _Input(InputEvent @event)
		{
			// Forward the input to your game loop logic
			gL.HandleInput(@event);
		}
	}
}
