using QuickType;

public class AIMove
{
    public List<Cards> CardToPlay { get;  set; }
    public List<int> IndexCard {get;set;}
    public AIMove()
    {
        CardToPlay = new List<Cards>();
        IndexCard = new List<int>();
    }
}