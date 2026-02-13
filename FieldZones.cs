using QuickType;

namespace fm
{
    public class FieldZones
    {
        public const int MONSTER_ZONES = 5;
        public const int SPELL_TRAP_ZONES = 5;

        // Monster Zones: [Index 0-4]
        public FieldMonster[] MonsterZones { get; set; }
        
        // Spell/Trap Zones: [Index 0-4]
        public FieldSpellTrap[] SpellTrapZones { get; set; }
        
        // Field Spell Zone: Only 1
        public Cards? FieldSpell { get; set; }

        public FieldZones()
        {
            MonsterZones = new FieldMonster[MONSTER_ZONES];
            SpellTrapZones = new FieldSpellTrap[SPELL_TRAP_ZONES];
            FieldSpell = null;
        }

        public bool PlaceMonster(int zoneIndex, Cards card, bool isAttackMode = true)
        {
            if (zoneIndex < 0 || zoneIndex >= MONSTER_ZONES || MonsterZones[zoneIndex] != null)
                return false;

            MonsterZones[zoneIndex] = new FieldMonster 
            { 
                Card = card, 
                IsAttackMode = isAttackMode,
                TurnsOnField = 0
            };
            return true;
        }

        public bool PlaceSpellTrap(int zoneIndex, Cards card)
        {
            if (zoneIndex < 0 || zoneIndex >= SPELL_TRAP_ZONES || SpellTrapZones[zoneIndex] != null)
                return false;

            SpellTrapZones[zoneIndex] = new FieldSpellTrap 
            { 
                Card = card, 
                IsFaceDown = false
            };
            return true;
        }

        public bool RemoveMonster(int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= MONSTER_ZONES)
                return false;

            MonsterZones[zoneIndex] = null;
            return true;
        }

        public FieldMonster? GetMonsterInZone(int zoneIndex) => 
            (zoneIndex >= 0 && zoneIndex < MONSTER_ZONES) ? MonsterZones[zoneIndex] : null;

        public bool HasAvailableMonsterZone() => MonsterZones.Any(z => z == null);
    }

    public class FieldMonster
    {
        public Cards Card { get; set; }
        public bool IsAttackMode { get; set; }
        public int TurnsOnField { get; set; }
        public bool HasAttackedThisTurn { get; set; }
    }

    public class FieldSpellTrap
    {
        public Cards Card { get; set; }
        public bool IsFaceDown { get; set; }
    }
}
