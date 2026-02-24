using QuickType;
using System.Linq;
namespace fm
{
	public class Player
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int LifePoints { get; set; }
		public List<Cards> Hand { get; set; }
		public List<Cards> Graveyard { get; set; }
		public List<Cards> Deck { get; set; }
		public bool IsEnemy {get; set;}
		public Godot.Collections.Array<Marker3D> SlotsCampo {get;set;}
		public Godot.Collections.Array<Marker3D> SlotsCampoST {get;set;}
		// Field Zones - will be managed by FieldZones class
		public FieldZones Field { get; set; }
		public Label LP { get; set; }
		public Label DeckNro { get; set; }

		public Player(string name, List<Cards> deck, Godot.Collections.Array<Marker3D>  SlotsCampo, Godot.Collections.Array<Marker3D> SlotsCampoST, Label LP, Label You, int startingLP = 8000)
		{
			Name = name;
			LifePoints = startingLP;	
			this.LP = LP;		
			this.DeckNro = You;
			Hand = new List<Cards>();
			Graveyard = new List<Cards>();
			Deck = deck;
			this.SlotsCampo = SlotsCampo;
			this.SlotsCampoST = SlotsCampoST;			
			Field = new FieldZones(SlotsCampo.Select(x => x.Name.ToString()).ToList());
		}			

		public void DrawCard(Cards card)
		{
			if (Deck.Count > 0)
			{
				Hand.Add(card);
				Deck.Remove(card);
				DeckNro.Text = Deck.Count().ToString();
			}
		}
		
		public bool TakeDamage(int dmg){
			var LifeWas = LifePoints;
			LifePoints = LifePoints - dmg;
			if(LifePoints < 0){
				LifePoints = 0;
			}
			if(LP != null){
				LP.Text = LifePoints.ToString();				
			}else{
				GD.Print("LP está null");
			}
			GD.Print($"{Name} tomou {dmg} de dano, e está com {LifePoints} - {LifeWas}.");
			return LifePoints <= 0;
		}

		public void DiscardCard(int card)
		{
			var uniCard = Hand.Where(x => x.Id == card).FirstOrDefault();
			Hand.Remove(uniCard);
		}

		public void SendToGraveyard(Cards card)
		{
			Graveyard.Add(card);
		}

		public bool HasCards() => Deck.Count > 0;
	}
}
