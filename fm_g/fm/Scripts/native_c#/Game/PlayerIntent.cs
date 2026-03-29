using QuickType;

public class PlayerIntention
{
    public string WorldPos {get;set;} = "";
    public PlayerIntentEnum Escolha {get;set;}

    public PlayerIntention()
    {
    }
    public PlayerIntention(string Pos, PlayerIntentEnum Decisao)
    {
        WorldPos = Pos;
        Escolha = Decisao;
    }

    public bool EndTurn()
    {
        return this.Escolha == PlayerIntentEnum.EndTurn;
    }
    public bool SelectSpell()
    {
        return this.Escolha == PlayerIntentEnum.SelectSpell;
    }

    public bool ValidIntention()
    {
        return this.Escolha != PlayerIntentEnum.InvalidIntent && this.Escolha != PlayerIntentEnum.EndTurn;
    }

}