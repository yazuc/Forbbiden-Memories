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
		public Godot.Collections.Array<Marker3D> SlotsCampo = new();
		public Godot.Collections.Array<Marker3D> SlotsCampoIni = new();
		public Godot.Collections.Array<Marker3D> SlotsCampoST = new();
		public Godot.Collections.Array<Marker3D> SlotsCampoSTIni = new();
		[Export] public Label LP_You;
		[Export] public Label LP_Com;
		private GameLoop gL;
		
		public override async void _Ready()
		{		
			GD.Print("Iniciando Banco de Dados e Jogo...");
			CameraHand.Current = true;
			CameraField.Current = false;
			SlotsCampo = GetSlotsFromGroup("player_monster_slots");
			SlotsCampoIni = GetSlotsFromGroup("enemy_monster_slots");
			SlotsCampoST = GetSlotsFromGroup("player_spell_slot");
			SlotsCampoSTIni = GetSlotsFromGroup("enemy_spell_slot");
			
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
			string srcGodot = "res://Scripts/native_c#/starter_deck.txt";
			string srcGodot2 = "res://Scripts/native_c#/starter_deck_ini.txt";
			string srcPath = ProjectSettings.GlobalizePath(srcGodot);
			string srcPath2 = ProjectSettings.GlobalizePath(srcGodot2);
						
			var deckList = Funcoes.LoadUserDeck(srcPath);
			deck.LoadDeck(deckList);
			GD.Print(deckList.Count());
			
			var deckListIni = Funcoes.LoadUserDeck(srcPath2);
			deckIni.LoadDeck(deckListIni);
			// 3. Inicializar o GameLoop
			// Passando Alice e Bob como os duelistas
			if (MaoVisual != null)
			{
				GD.Print(SlotsCampo.Count().ToString());
				gL = new GameLoop(
					new Player("Alice", deck.Cards, SlotsCampo, SlotsCampoST, LP_You, 8000), 
					new Player("Bob", deckIni.Cards, SlotsCampoIni, SlotsCampoSTIni, LP_Com, 8000),
					MaoVisual,
					CameraHand,
					CameraField,
					CameraInimigo,
					CameraPivot
				);
				gL.Initialize();
			}							
			// Teste de Fusão (se quiser testar agora)
			// string result = await fm.Function.Fusion("177,296,211");
			// GD.Print($"Resultado da Fusão: {result}");
		}
		
		private Godot.Collections.Array<Marker3D> GetSlotsFromGroup(string groupName)
		{
			var nodes = GetTree().GetNodesInGroup(groupName);
			var array = new Godot.Collections.Array<Marker3D>();
						
			var sorted = nodes.Cast<Marker3D>().OrderBy(n => n.Name.ToString());
			foreach(var item in sorted){
				GD.Print(item.Name);
			}
			foreach (var n in sorted) array.Add(n);
			return array;
		}
	}
}
