using Godot;
using System;
using System.Threading.Tasks;

namespace fm
{
	public partial class Main : Node
	{
		[Export] public MaoJogador? MaoVisual;
		[Export] public MaoInimigo? MaoInimigo;
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
		[Export] public Label You;
		[Export] public Label Com;
		public int index_deck {get;set;} 
		public Result resultScreen;
		private GameLoop gL;
		
		public override async void _Ready()
		{						
			resultScreen = GetNode<Result>("Result");
			SlotsCampo = GetSlotsFromGroup("player_monster_slots");
			SlotsCampoIni = GetSlotsFromGroup("enemy_monster_slots");
			SlotsCampoST = GetSlotsFromGroup("player_spell_slot");
			SlotsCampoSTIni = GetSlotsFromGroup("enemy_spell_slot");
			index_deck = GlobalUsings.Instance.DeckIndex;
			var db = GlobalUsings.Instance.db;
			var deckIni = new Deck();
			deckIni.LoadDeck(db.GetDeckByNpcId(index_deck));
			if (MaoVisual != null)
			{				
				gL = new GameLoop(
					new Player("Alice", GlobalUsings.Instance.Deck.Cards, SlotsCampo, SlotsCampoST, LP_You, You, 8000), 
					new Player("Bob", deckIni.Cards, SlotsCampoIni, SlotsCampoSTIni, LP_Com, Com, 100),
					MaoVisual,
					MaoInimigo,
					CameraHand,
					CameraField,
					CameraInimigo,
					CameraPivot,
					resultScreen
				);
				gL.MaoDoJogador.gameLoop = gL;
				gL.MaoDoInimigo.gameLoop = gL;
				gL.Initialize();
			}					
			
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
