using QuickType;

namespace fm
{
    public enum TurnPhase
    {
        Draw,
        Main1,
        Battle,
        Main2,
        End
    }

    public enum GameStatus
    {
        NotStarted,
        InProgress,
        Finished
    }

    public class GameState
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public int CurrentTurn { get; set; }
        public Player CurrentPlayer { get; set; }
        public Player OpponentPlayer { get; set; }
        public TurnPhase CurrentPhase { get; set; }
        public GameStatus Status { get; set; }
        public Player? Winner { get; set; }

        public GameState(Player player1, Player player2)
        {
            Player1 = player1;
            Player2 = player2;
            CurrentTurn = 1;
            CurrentPlayer = Player1;
            OpponentPlayer = Player2;
            CurrentPhase = TurnPhase.Draw;
            Status = GameStatus.InProgress;
            Winner = null;
        }

        public void SwitchPlayer()
        {
            var temp = CurrentPlayer;
            CurrentPlayer = OpponentPlayer;
            OpponentPlayer = temp;
            CurrentTurn++;
            CurrentPhase = TurnPhase.Draw;
        }

        public void AdvancePhase()
        {
            CurrentPhase = CurrentPhase switch
            {
                TurnPhase.Draw => TurnPhase.Main1,
                TurnPhase.Main1 => TurnPhase.Battle,
                TurnPhase.Battle => TurnPhase.Main2,
                TurnPhase.Main2 => TurnPhase.End,
                TurnPhase.End => TurnPhase.Draw,
                _ => TurnPhase.Draw
            };
        }

        public void EndGame(Player winner)
        {
            Status = GameStatus.Finished;
            Winner = winner;
        }

        public bool IsGameOver() => Status == GameStatus.Finished || 
                                   Player1.LifePoints <= 0 || 
                                   Player2.LifePoints <= 0;

        public bool CanAttack() => CurrentPhase == TurnPhase.Battle;

        public bool IsMainPhase() => CurrentPhase == TurnPhase.Main1 || CurrentPhase == TurnPhase.Main2;
    }
}
