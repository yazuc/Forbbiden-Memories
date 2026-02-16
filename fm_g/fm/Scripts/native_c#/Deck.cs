using System;
using System.Linq;
using System.Collections.Generic;
using QuickType;

namespace fm
{
	public class Deck
	{
		public List<Cards> Cards { get; private set; }

		public Deck()
		{
			Cards = new List<Cards>();
		}

		public void LoadDeck(List<Cards> cards)
		{
			var rng = new Random();
			Cards = cards.OrderBy(_ => rng.Next()).Take(40).ToList(); // Shuffle and take up to 40
		}

		public void AddCard(Cards card)
		{
			if (Cards.Count < 40) // Assuming a max deck size of 40
			{
				Cards.Add(card);
			}
			else
			{
				Console.WriteLine("Deck is full. Cannot add more cards.");
			}
		}

		public void RemoveCard(Cards card)
		{
			if (Cards.Contains(card))
			{
				Cards.Remove(card);
			}
			else
			{
				Console.WriteLine("Card not found in deck.");
			}
		}

		public void ClearDeck()
		{
			Cards.Clear();
		}
	}
}
