using QuickType;
using SQLite;

public class UserDeck
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id {get;set;}
    public int CardID {get;set;}
    public string DeckID {get;set;}
}