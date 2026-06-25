using SQLite;

public class User
{
    [PrimaryKey]
    public string ID {get;set;}
    public string Nome {get;set;}
    public string DeckID {get;set;}
    public int Stars {get;set;}
}