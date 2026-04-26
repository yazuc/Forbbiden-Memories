
namespace fm
{
    public class Rank
    {
        public int StartPoint {get;set;} = 50;        
        public int Turns {get;set;}
        /// <summary>
        //(When your monster destroys an opponent's monster that is in Attack Mode.)
        /// </summary>
        public int EffectiveAttacks {get;set;}
        /// <summary>    
        //(When an opponent's monster attacks your monster that is in Defense Mode and your monster is not destroyed.)
        /// </summary>
        public int DefensiveWins {get;set;}
        /// <summary>
        /// Player placed cards that were facedown
        /// </summary>
        public int FacedownPlays {get;set;}
        /// <summary>
        /// Attemps to fuse
        /// </summary>
        public int AttemptToFuse {get;set;}
        /// <summary>
        /// Attemps to equipe
        /// </summary>
        public int AttemptToEquip {get;set;}
        /// <summary>
        /// Spell use
        /// </summary>
        public int SpellUsed {get;set;}
        /// <summary>
        /// Traps that activated
        /// </summary>
        public int TriggerTrap {get;set;}
        /// <summary>
        /// Cards used from the deck, doesn't matter current hand
        /// </summary>
        public int CardsUsed {get;set;}
        /// <summary>
        /// your current LP
        /// </summary>
        public int RemainingLP {get;set;}
    }    
}