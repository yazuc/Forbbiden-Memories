
namespace fm
{
    public class Rank
    {
        public int StartPoint {get;set;} = 50;        
        public int Turns {get;set;} = 1;
        /// <summary>
        //(When your monster destroys an opponent's monster that is in Attack Mode.)
        /// </summary>
        public int EffectiveAttacks {get;set;} = 0;
        /// <summary>    
        //(When an opponent's monster attacks your monster that is in Defense Mode and your monster is not destroyed.)
        /// </summary>
        public int DefensiveWins {get;set;} = 0;
        /// <summary>
        /// Player placed cards that were facedown
        /// </summary>
        public int FacedownPlays {get;set;} = 0;
        /// <summary>
        /// Attemps to fuse
        /// </summary>
        public int AttemptToFuse {get;set;} = 0;
        /// <summary>
        /// Attemps to equipe
        /// </summary>
        public int AttemptToEquip {get;set;} = 0;
        /// <summary>
        /// Spell use
        /// </summary>
        public int SpellUsed {get;set;} = 0;
        /// <summary>
        /// Traps that activated
        /// </summary>
        public int TriggerTrap {get;set;} = 0;
        /// <summary>
        /// Cards used from the deck, doesn't matter current hand
        /// </summary>
        public int CardsUsed {get;set;} = 5;
        /// <summary>
        /// your current LP
        /// </summary>
        public int RemainingLP {get;set;}

        public Rank()
        {            
        }
        public Rank(string def)
        {
            CardsUsed = 1;
            RemainingLP = 8000;
            Turns = 3;
            EffectiveAttacks = 3;
            DefensiveWins = 4;            
            FacedownPlays = 5;
            AttemptToFuse = 1;
            AttemptToEquip = 1;
            SpellUsed = 1;
            TriggerTrap = 1;
        }

        public void printRank()
        {
            GD.Print($"Turns: {Turns}");
            GD.Print($"Effective Attacks: {EffectiveAttacks}");
            GD.Print($"Defensive Wins: {DefensiveWins}");
            GD.Print($"Facedown Plays: {FacedownPlays}");
            GD.Print($"Attempt To Fuse: {AttemptToFuse}");
            GD.Print($"Attempt To Equip: {AttemptToEquip}");
            GD.Print($"Spell Used: {SpellUsed}");
            GD.Print($"Trigger Trap: {TriggerTrap}");
            GD.Print($"Cards Used: {CardsUsed}");
            GD.Print($"Remaining LP: {RemainingLP}");
        }

        public void SetEndDuel(int turn, int remainingLP, int cardsUsed)
        {
            RemainingLP = remainingLP;
            Turns = turn;
            CardsUsed = cardsUsed;
        }
       
        public void AddEffectiveAttack()
        {
            EffectiveAttacks++;
        }
        public void AddDefensiveWin()
        {
            DefensiveWins++;
        }
        public void AddFacedownPlay()
        {
            FacedownPlays++;
        }
        public void AddAttemptToFuse()
        {
            AttemptToFuse++;    
        }
        public void AddAttemptToEquip()
        {
            AttemptToEquip++;
        }
        public void AddSpellUsed()
        {
            SpellUsed++;
        }
        public void AddTriggerTrap()
        {
            TriggerTrap++;
        }
        
    }    
}