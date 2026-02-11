using System;
using System.IO;
using QuickType;

namespace fm
{
    public class Funcoes
    {
        //to be savefile manager
        public static void WriteCardsToFile(List<QuickType.Cards> cards, string filePath = "output.txt")
        {
            try
            {
                string output = "";
                foreach(var card in cards)
                {
                    output += $"{card.Id},";
                }
                File.WriteAllText(filePath, output);
                Console.WriteLine($"File written successfully to: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing file: {ex.Message}");
            }
        }

        public static List<Cards> LoadUserDeck(string filePath = "output.txt")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return new List<Cards>();
                }
                    
                string content = File.ReadAllText(filePath);
                var cardIds = content.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToList();
                var cards = CardDatabase.Instance.GetAllCards();
                var userDeck = cards.Where(c => cardIds.Contains(c.Id)).ToList();

                return userDeck;                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return new List<Cards>();
            }
        }
    }    
}