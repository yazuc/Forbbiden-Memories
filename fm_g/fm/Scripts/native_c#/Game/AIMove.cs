using QuickType;

public class AIMove
{
    public List<Cards> CardToPlay { get;  set; }
    public List<int> IndexCard {get;set;}
    public bool Defense {get;set;}
    public string AttackerZone {get;set;}
    public string DefenderZone {get;set;}
    public bool FaceUP {get;set;}
    public AIMove()
    {
        CardToPlay = new List<Cards>();
        IndexCard = new List<int>();        
    }
}