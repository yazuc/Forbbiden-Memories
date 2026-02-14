namespace fm
{
    public class Program
    {
        //forma principal de testar funções, vai ser substituido pelo loop do jogo
        public static async Task Main()
        {            
            var db = CardDatabase.Instance;
            db.SyncJsonToDatabase("cards.json"); // Load cards from JSON into the database if not already loaded
            CardDatabase.Instance.GetAllCards();
            var cards = CardDatabase.Instance.GetAllCards();

            var deck = new Deck();
            deck.LoadDeck(Funcoes.LoadUserDeck("/mnt/Nvme/fm/starter_deck.txt")); // Load the first 40 cards into the deck  


            GameLoop gL = new GameLoop(new Player("Alice", deck.Cards, 8000), new Player("Bob", deck.Cards, 8000));  
            gL.Initialize();
        
            // FmStarterDeckGenerator generator = new FmStarterDeckGenerator();
            // List<QuickType.Cards> starterDeck = generator.GenerateStarterDeck(cards.ToList());          
            // Funcoes.WriteOutputToFile(starterDeck, "starter_deck.txt");

            // string result = await fm.Function.Fusion("177,296,211");
            // Console.WriteLine(result); 
        }
    }
}