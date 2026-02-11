using System;
using System.IO;

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
    }    
}