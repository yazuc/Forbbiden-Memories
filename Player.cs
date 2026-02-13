using QuickType;

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
        
        // Field Zones - will be managed by FieldZones class
        public FieldZones Field { get; set; }

        public Player(string name, List<Cards> deck, int startingLP = 8000)
        {
            Name = name;
            LifePoints = startingLP;
            Hand = new List<Cards>();
            Graveyard = new List<Cards>();
            Deck = deck;
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

        public void DiscardCard(Cards card)
        {
            if (Hand.Contains(card))
            {
                Hand.Remove(card);
                Graveyard.Add(card);
            }
        }

        public void SendToGraveyard(Cards card)
        {
            Graveyard.Add(card);
        }

        public bool HasCards() => Deck.Count > 0;
    }
}
