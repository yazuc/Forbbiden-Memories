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
		public MaoJogador MaoDoJogador;


		public GameState(Player player1, Player player2, MaoJogador MaoDoJogador)
		{
			Player1 = player1;
			Player2 = player2;
			CurrentTurn = 1;
			this.MaoDoJogador = MaoDoJogador;
			CurrentPlayer = Player1;
			OpponentPlayer = Player2;
			CurrentPhase = TurnPhase.Draw;
			Status = GameStatus.InProgress;
			Winner = null;			
			MaoDoJogador.ConfigurarSlots(CurrentPlayer.SlotsCampo, OpponentPlayer.SlotsCampo, CurrentPlayer.SlotsCampoST, OpponentPlayer.SlotsCampoST);
			Player2.IsEnemy = true;
		}

		public void SwitchPlayer()
		{
			GD.Print("caimos no switch");
			if (MaoDoJogador != null) {
				MaoDoJogador.CancelarSelecaoNoCampo(); 
			}
			// Troca de jogadores
			var temp = CurrentPlayer;
			CurrentPlayer = OpponentPlayer;
			OpponentPlayer = temp;

			CurrentTurn++;
			CurrentPhase = TurnPhase.Draw;

			MaoDoJogador.ConfigurarSlots(CurrentPlayer.SlotsCampo, OpponentPlayer.SlotsCampo, CurrentPlayer.SlotsCampoST, OpponentPlayer.SlotsCampoST);
			MaoDoJogador._indiceSelecionado = 0;
			MaoDoJogador._indiceCampoSelecionado = 0;
			ResetFieldFlags(Player1);
			ResetFieldFlags(Player2);
			Input.FlushBufferedEvents();
		}

		private void ResetFieldFlags(Player player)
		{
			// Acessando as zonas que você definiu na classe FieldZones
			var zones = player.Field; // Assumindo que Player tem uma propriedade Field do tipo FieldZones
			
			for (int i = 0; i < FieldZones.MONSTER_ZONES; i++)
			{
				var monster = zones.MonsterZones[i];
				if (monster != null)
				{
					// Reset da flag que você criou na classe FieldMonster
					monster.HasAttackedThisTurn = false;
					
					// Incrementa turnos no campo (útil para efeitos de cartas)
					monster.TurnsOnField++;
				}
			}
		}


		public void AdvancePhase()
		{
			CurrentPhase = CurrentPhase switch
			{
				TurnPhase.Draw => TurnPhase.Main1,
				TurnPhase.Main1 => TurnPhase.Battle,
				TurnPhase.Battle => TurnPhase.End,				
				TurnPhase.End => TurnPhase.Draw,
				_ => TurnPhase.Draw
			};
			GD.Print("Current phase:" + CurrentPhase.ToString());
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
