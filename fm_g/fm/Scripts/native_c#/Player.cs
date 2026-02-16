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
		public Godot.Collections.Array<Marker3D> SlotsCampo {get;set;}
		public Godot.Collections.Array<Marker3D> SlotsCampoST {get;set;}
		// Field Zones - will be managed by FieldZones class
		public FieldZones Field { get; set; }

		public Player(string name, List<Cards> deck, Godot.Collections.Array<Marker3D>  SlotsCampo, Godot.Collections.Array<Marker3D> SlotsCampoST, int startingLP = 8000)
		{
			Name = name;
			LifePoints = startingLP;
			Hand = new List<Cards>();
			Graveyard = new List<Cards>();
			Deck = deck;
			this.SlotsCampo = SlotsCampo;
			this.SlotsCampoST = SlotsCampoST;
			Field = new FieldZones();
		}

		public void DrawCard(Cards card)
		{
			if (Deck.Count > 0)
			{
				Hand.Add(card);
				Deck.Remove(card);
			}
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
