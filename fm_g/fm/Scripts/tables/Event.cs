using SQLite;
public class Event
{
    [PrimaryKey]
    public int ID {get;set;}
    public string Name {get;set;}
    public string Value {get;set;}
    public string UserID {get;set;}

    public Event()
    {
        
    }
    public Event(string Name,  string Value, string UserID)
    {
        this.Name = Name;
        this.Value = Value;
        this.UserID = UserID;
    }
}