using QuickType;

namespace fm
{
    public class GameDisplay
    {
        public static void DisplayMainMenu()
        {
            Console.Clear();
            Console.WriteLine("╔═════════════════════════════════════╗");
            Console.WriteLine("║   FORBIDDEN MEMORIES REMAKE          ║");
            Console.WriteLine("╚═════════════════════════════════════╝\n");
            Console.WriteLine("1. Start New Game (vs AI)");
            Console.WriteLine("2. Start New Game (vs Player)");
            Console.WriteLine("3. Exit");
            Console.WriteLine("\nSelect an option: ");
        }

        public static void DisplayGameBoard(GameState gameState)
        {
            Console.Clear();
            DisplayPlayerField(gameState.OpponentPlayer, isOpponent: true);
            Console.WriteLine("\n" + new string('═', 60) + "\n");
            DisplayPlayerField(gameState.CurrentPlayer, isOpponent: false);
            Console.WriteLine($"\nCurrent Phase: {gameState.CurrentPhase} | Turn: {gameState.CurrentTurn}");
        }

        private static void DisplayPlayerField(Player player, bool isOpponent)
        {
            string prefix = isOpponent ? "OPPONENT - " : "YOUR - ";
            Console.WriteLine($"{prefix}{player.Name} | LP: {player.LifePoints}");
            Console.WriteLine(new string('-', 60));

            // Display field spells
            if (player.Field.FieldSpell != null)
            {
                Console.WriteLine($"[Field Spell: {player.Field.FieldSpell.Name}]");
            }

            // Display monster zones
            Console.WriteLine("\nMonster Zones:");
            for (int i = 0; i < FieldZones.MONSTER_ZONES; i++)
            {
                var monster = player.Field.MonsterZones[i];
                if (monster != null)
                {
                    string mode = monster.IsAttackMode ? "ATK" : "DEF";
                    Console.WriteLine($"  [{i}] {monster.Card.Name} ({monster.Card.Attack}/{monster.Card.Defense}) [{mode}]");
                }
                else
                {
                    Console.WriteLine($"  [{i}] [Empty]");
                }
            }

            // Display spell/trap zones
            Console.WriteLine("\nSpell/Trap Zones:");
            for (int i = 0; i < FieldZones.SPELL_TRAP_ZONES; i++)
            {
                var spellTrap = player.Field.SpellTrapZones[i];
                if (spellTrap != null)
                {
                    string faceDown = spellTrap.IsFaceDown ? "[Face Down]" : spellTrap.Card.Name;
                    Console.WriteLine($"  [{i}] {faceDown}");
                }
                else
                {
                    Console.WriteLine($"  [{i}] [Empty]");
                }
            }

            // Display hand
            Console.WriteLine($"\nGraveyard: {player.Graveyard.Count} cards");
            Console.WriteLine($"Deck: {player.Deck.Count} cards");
            Console.WriteLine($"Hand ({player.Hand.Count} cards):");
            for (int i = 0; i < player.Hand.Count; i++)
            {
                Console.WriteLine($"  [{i}] {player.Hand[i].Name} ({player.Hand[i].CardCode})");
            }
        }

        public static void DisplayBattleLog(BattleSystem.BattleResult result)
        {
            Console.WriteLine($"\n--- Battle Result ---");
            Console.WriteLine(result.Description);
            if (result.DamageDealt > 0)
                Console.WriteLine($"Damage Dealt: {result.DamageDealt}");
            if (result.AttackerDestroyed)
                Console.WriteLine("Attacker destroyed!");
            if (result.DefenderDestroyed)
                Console.WriteLine("Defender destroyed!");
        }

        public static void DisplayPlayerActions(GameState gameState)
        {
            Console.WriteLine($"\n--- {gameState.CurrentPlayer.Name}'s Actions ---");
            Console.WriteLine("1. Summon Monster");
            Console.WriteLine("2. Activate Spell/Trap");
            Console.WriteLine("3. Attack");
            Console.WriteLine("4. End Turn");
            Console.WriteLine("5. Surrender");
            Console.WriteLine("\nSelect action: ");
        }

        public static void DisplayCardInfo(Cards card)
        {
            Console.WriteLine($"\n╔═ Card Info ═╗");
            Console.WriteLine($"Name: {card.Name}");
            Console.WriteLine($"ID: {card.Id}");
            Console.WriteLine($"Type: {card.Type}");
            if (card.Type != CardTypeEnum.Spell && card.Type != CardTypeEnum.Trap)
            {
                Console.WriteLine($"Attack: {card.Attack}");
                Console.WriteLine($"Defense: {card.Defense}");
                Console.WriteLine($"Level: {card.Level}");
                Console.WriteLine($"Stars: {card.Stars}");
            }
            Console.WriteLine($"Description: {card.Description}");
            Console.WriteLine($"╚════════════╝");
        }

        public static void DisplayTurnStart(GameState gameState)
        {
            Console.WriteLine($"\n{'═',40}");
            Console.WriteLine($"Turn {gameState.CurrentTurn}: {gameState.CurrentPlayer.Name}'s Turn");
            Console.WriteLine($"Phase: {gameState.CurrentPhase}");
            Console.WriteLine($"{'═',40}\n");
        }

        public static void DisplayDuelEnd(GameState gameState)
        {
            Console.Clear();
            Console.WriteLine("╔═════════════════════════════════════╗");
            Console.WriteLine("║         DUEL FINISHED!               ║");
            Console.WriteLine("╚═════════════════════════════════════╝\n");

            if (gameState.Winner != null)
            {
                Console.WriteLine($"Winner: {gameState.Winner.Name}");
                Console.WriteLine($"Final LP: {gameState.Winner.LifePoints}");
                Console.WriteLine($"Loser LP: {(gameState.Winner == gameState.Player1 ? gameState.Player2.LifePoints : gameState.Player1.LifePoints)}");
            }

            Console.WriteLine("\nPress any key to return to main menu...");
        }

        public static void DisplayError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] {message}");
            Console.ResetColor();
        }

        public static void DisplayInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[INFO] {message}");
            Console.ResetColor();
        }

        public static void DisplaySuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[SUCCESS] {message}");
            Console.ResetColor();
        }
    }
}
