// Source - https://stackoverflow.com/a/67910524␍
// Posted by MarredCheese, modified by community. See post 'Timeline' for change history␍
// Retrieved 2026-02-10, License - CC BY-SA 4.0␍

using System;
using System.IO;
using System.Threading.Tasks;
using System.IO;
using QuickType;

namespace fm
{
    public class Function
    {
        public static async Task<Cards> Fusion(string args)
        {
            CardDatabase.Instance.GetAllCards();
            var cards = CardDatabase.Instance.GetAllCards();

            var cardIds = args.Split(',').Select(int.Parse).ToList();
            var queue = new Queue<int>(cardIds);
            
            if (queue.Count == 0)
                return null;

            var currentCard = queue.Dequeue();

            while (queue.Count > 0)
            {
                var nextCard = queue.Dequeue();
                
                int card1 = Math.Min(currentCard, nextCard);
                int card2 = Math.Max(currentCard, nextCard);
                
                if(TryGetFusion(card1, card2, cards, out int resultCard))
                {
                    Console.WriteLine($"Fused {currentCard} + {nextCard} = {resultCard}");
                    currentCard = resultCard;
                }
                else
                {
                    Console.WriteLine($"No fusion for {currentCard} + {nextCard}, discarding {currentCard}");
                    currentCard = nextCard;
                }
            }

            return cards.FirstOrDefault(x => x.Id == currentCard) ?? null;
        }
        
        public static bool TryGetFusion(int card1ID, int card2ID, List<Cards> cards, out int result)
        {
            result = -1;
            var fusion = cards.FirstOrDefault(x => x.Id == card1ID);

            if(fusion != null && fusion.Fusions.Any())
            {
                foreach(var item in fusion.Fusions)
                {
                    if(item.Card2 == card2ID)
                    {
                        result = item.Result;
                        return true;
                    }                
                }
            }

            return false;
        }
    }  
}

