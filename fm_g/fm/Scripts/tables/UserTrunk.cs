using QuickType;
using SQLite;

public class UserTrunk
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id {get;set;}
    public string UserID {get;set;}
    public int CardID {get;set;}
    public int Quantity {get;set;}
}