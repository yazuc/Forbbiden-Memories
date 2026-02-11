using System.Collections.Generic;
using QuickType;
namespace fm
{    
    public class CardDatabase
    {
        private static CardDatabase? _instance;
        private List<Cards> _cards;

        private CardDatabase() { }

        public static CardDatabase Instance
        {
            get
            {
                _instance ??= new CardDatabase();
                return _instance;
            }
        }

        public void LoadCards(string jsonFilePath)
        {
            string json = File.ReadAllText(jsonFilePath);
            _cards = Cards.FromJson(json).ToList();
        }

        public List<Cards> GetAllCards() => _cards;
        public Cards? GetCardById(int id) => _cards.Count() > 0 ? _cards.FirstOrDefault(c => c.Id == id) : new Cards();
    }
}