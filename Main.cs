namespace fm
{
    public class Program
    {
        public static async Task Main()
        {            
            CardDatabase.Instance.LoadCards("cards.json");
            var cards = CardDatabase.Instance.GetAllCards();
            
            // FmStarterDeckGenerator generator = new FmStarterDeckGenerator();
            // List<QuickType.Cards> starterDeck = generator.GenerateStarterDeck(cards.ToList());          
            // Funcoes.WriteOutputToFile(starterDeck, "starter_deck");

            string result = await fm.Function.Fusion("177,296,211");
            Console.WriteLine(result); 
        }
    }
}