namespace fm;

public class RankResult
{
    public Rank RankStats;
    public RankEnum Resultado => ResultSetter();

    public int CalculaTurno(int turno)
    {        
        if(turno >= 0 && turno <= 4)        
            return 12;
                
        if(turno >= 5 && turno <= 8)
            return 8;    

        if(turno >= 9 && turno <= 28)
            return 0;
        
        if(turno >= 29 && turno <= 32)
            return -8;
        
        if(turno >= 33 && turno <= 36)
            return -12;
        
        return 0;
    }    

    public int CalculaAtaques(int ataque)
    {
        if(ataque >= 0 && ataque <= 1)
            return 4;
        if(ataque >= 2 && ataque <= 3)
            return 2;
        if(ataque >= 4 && ataque <= 9)
            return 0;
        if(ataque >= 10 && ataque <= 19)
            return -2;
        if(ataque >= 20 && ataque <= 160)
            return -4;


        return 0;
    }

    public int CalculaDefesa(int defesa)
    {
        if(defesa >= 0 && defesa <= 1)
            return 0;
        if(defesa >= 2 && defesa <= 5)
            return -10;
        if(defesa >= 6 && defesa <= 9)
            return -20;
        if(defesa >= 10 && defesa <= 14)
            return -30;
        if(defesa >= 15 && defesa <= 160)
            return -40;
        return 0;
    }

    public int CalculaFacedown(int facedown)
    {
        if(facedown == 0)
            return 0;
        if(facedown >= 1 && facedown <= 10)
            return -2;
        if(facedown >= 11 && facedown <= 20)
            return -4;
        if(facedown >= 21 && facedown <= 30)
            return -6;
        if(facedown >= 31 && facedown <= 36)
            return -8;
        return 0;
    }

    public int CalculaFusion(int fusion)
    {
        if(fusion == 0)
            return 4;
        if(fusion >= 1 && fusion <= 4)
            return 0;
        if(fusion >= 5 && fusion <= 9)
            return -4;
        if(fusion >= 10 && fusion <= 14)
            return -8;
        if(fusion >= 15 && fusion <= 50)
            return -12;
        return 0;
    }

    public int CalculaEquip(int equip)
    {
        if(equip == 0)
            return 4;
        if(equip >= 1 && equip <= 4)
            return 0;
        if(equip >= 5 && equip <= 9)
            return -4;
        if(equip >= 10 && equip <= 14)
            return -8;
        if(equip >= 15)
            return -12;

        return 0;
    }    

    public int CalculaMagic(int magic)
    {
        if(magic == 0)
            return 2;
        if(magic >= 1 && magic <= 3)
            return -4;
        if(magic >= 4 && magic <= 6)
            return -8;
        if(magic >= 7 && magic <= 9)
            return -12;
        if(magic >= 10)
            return -16;

        return 0;
    }  
    public int CalculaTrap(int trap)
    {
        if(trap == 0)
            return 2;
        if(trap >= 1 && trap <= 2)
            return -8;
        if(trap >= 3 && trap <= 4)
            return -16;
        if(trap >= 5 && trap <= 6)
            return -24;
        if(trap >= 7)
            return -32;

        return 0;
    }   

    public int CalculaCard(int card)
    {
        if(card >= 0 && card <= 8)
            return 15;
        if(card >= 9 && card <= 12)
            return 12;
        if(card >= 13 && card <= 32)
            return 0;
        if(card >= 33 && card <= 36)
            return -5;
        if(card >= 37)
            return -7;

        return 0;
    }   

    public int CalculaLP(int lp)
    {
        if(lp >= 0 && lp <= 99)
            return -7;
        if(lp >= 100 && lp <= 999)
            return -5;
        if(lp >= 1000 && lp <= 6999)
            return 0;
        if(lp >= 7000 && lp <= 7999)
            return 4;
        if(lp >= 8000)
            return 6;

        return 0;
    }  

    public int Result => 
        RankStats.StartPoint +
        CalculaTurno(RankStats.Turns) +
        CalculaAtaques(RankStats.EffectiveAttacks) +
        CalculaDefesa(RankStats.DefensiveWins) +
        CalculaFacedown(RankStats.FacedownPlays) + 
        CalculaFusion(RankStats.AttemptToFuse) +
        CalculaEquip(RankStats.AttemptToEquip) +
        CalculaMagic(RankStats.SpellUsed) +
        CalculaTrap(RankStats.TriggerTrap) +
        CalculaCard(RankStats.CardsUsed) +
        CalculaLP(RankStats.RemainingLP);

    public RankEnum ResultSetter()
    {
        var Res = RankEnum.SPOW;
        if(Result >= 90)
            Res = RankEnum.SPOW;
        if(Result >= 80 && Result <= 89)
            Res = RankEnum.APOW;
        if(Result >= 70 && Result <= 79)
            Res = RankEnum.BPOW;
        if(Result >= 60 && Result <= 69)
            Res = RankEnum.CPOW;
        if(Result >= 50 && Result <= 59)
            Res = RankEnum.DPOW;

        if(Result >= 40 && Result <= 49)
            Res = RankEnum.DTEC;
        if(Result >= 30 && Result <= 39)
            Res = RankEnum.CTEC;
        if(Result >= 20 && Result <= 29)
            Res = RankEnum.BTEC;
        if(Result >= 10 && Result <= 19)
            Res = RankEnum.ATEC;
        if(Result >= 0 && Result <= 9)
            Res = RankEnum.STEC;

        return Res;
    }
}