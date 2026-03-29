using System;
using System.Collections.Generic;
using System.Linq;
using QuickType;

namespace fm
{
    public class AIDecision
    {
        public List<int> CardIds { get; set; } = new List<int>();
        public int TargetZoneIndex { get; set; } = -1;
        public bool IsFaceDown { get; set; } = false;
        public Function.FusionResult? FusionResult { get; set; }
    }

    public class BattleDecision
    {
        public bool EndTurn { get; set; }
        public bool SwitchToDefense { get; set; }
        public int DefenseZoneIndex { get; set; } = -1;
        public int AttackerZoneIndex { get; set; } = -1;
        public int DefenderZoneIndex { get; set; } = -1;

        public bool HasAttack => AttackerZoneIndex >= 0;
        public bool IsDirectAttack => HasAttack && DefenderZoneIndex < 0;
    }

    public class AIEngine
    {
        private const long AttackWeight = 1000;
        private const long DirectDamageBonus = 5000;
        private const long DestroyStrongestBonus = 10000;

        public AIDecision? GetBestMove(List<Cards> hand, FieldZones enemyField, FieldZones ownField)
        {
            if (hand == null || hand.Count == 0 || ownField == null)
                return null;

            int targetMonsterSlot = GetFirstFreeMonsterZone(ownField);
            int targetSpellTrapSlot = GetFirstFreeSpellTrapZone(ownField);

            if (targetMonsterSlot < 0 && targetSpellTrapSlot < 0)
                return null;

            var enemyStrongest = GetStrongestEnemyMonster(enemyField);
            bool enemyEmpty = enemyStrongest == null;

            AIDecision? bestDecision = null;
            long bestScore = long.MinValue;

            foreach (var sequence in GenerateOrderedSubsets(hand))
            {
                var ids = sequence.Select(c => c.Id).ToList();
                var prediction = Function.ProcessChain(string.Join(",", ids), null, true);
                if (prediction == null || prediction.MainCard == null)
                    continue;

                bool isSpellTrap = prediction.MainCard.IsSpellTrap();

                if (isSpellTrap && targetSpellTrapSlot < 0)
                    continue;

                if (!isSpellTrap && targetMonsterSlot < 0)
                    continue;

                if (isSpellTrap)
                {
                    // For now, AI just plays spell/traps face down or equips randomly if possible, basic logic.
                    // Assign a base positive score so it might play it if no better monsters
                    long score = 500;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDecision = new AIDecision
                        {
                            CardIds = ids,
                            TargetZoneIndex = targetSpellTrapSlot,
                            IsFaceDown = true, // Play traps/spells face down by default for simplicity unless logic requires face up
                            FusionResult = prediction
                        };
                    }
                    continue;
                }

                if (prediction.MainCard.Attack <= 0)
                    continue;

                long monsterScore = prediction.MainCard.Attack * AttackWeight;

                if (enemyEmpty)
                    monsterScore += prediction.MainCard.Attack * DirectDamageBonus;

                if (enemyStrongest != null)
                {
                    long enemyValue = GetEffectiveDefense(enemyStrongest);
                    if (prediction.MainCard.Attack > enemyValue)
                        monsterScore += DestroyStrongestBonus + (prediction.MainCard.Attack - enemyValue) * 20;
                }

                if (monsterScore > bestScore)
                {
                    bestScore = monsterScore;
                    bestDecision = new AIDecision
                    {
                        CardIds = ids,
                        TargetZoneIndex = targetMonsterSlot,
                        IsFaceDown = false,
                        FusionResult = prediction
                    };
                }
            }

            if (bestDecision == null)
            {
                var fallback = hand
                    .Where(c => c != null && !c.IsSpellTrap())
                    .OrderByDescending(c => c.Attack)
                    .FirstOrDefault();

                if (fallback != null && targetMonsterSlot >= 0)
                {
                    var prediction = Function.ProcessChain(fallback.Id.ToString(), null, true);
                    if (prediction?.MainCard != null)
                    {
                        bestDecision = new AIDecision
                        {
                            CardIds = new List<int> { fallback.Id },
                            TargetZoneIndex = targetMonsterSlot,
                            IsFaceDown = false,
                            FusionResult = prediction
                        };
                    }
                }
                else
                {
                    var firstCard = hand.FirstOrDefault();
                    if (firstCard != null)
                    {
                        bool isSpellTrap = firstCard.IsSpellTrap();
                        int targetSlot = isSpellTrap ? targetSpellTrapSlot : targetMonsterSlot;

                        if (targetSlot >= 0)
                        {
                            var prediction = Function.ProcessChain(firstCard.Id.ToString(), null, true);
                            bestDecision = new AIDecision
                            {
                                CardIds = new List<int> { firstCard.Id },
                                TargetZoneIndex = targetSlot,
                                IsFaceDown = true, // Play face down as fallback
                                FusionResult = prediction
                            };
                        }
                    }
                }
            }

            return bestDecision;
        }

        private static int GetFirstFreeSpellTrapZone(FieldZones field)
        {
            if (field == null) return -1;
            for (int i = 0; i < FieldZones.SPELL_TRAP_ZONES; i++)
            {
                if (field.SpellTrapZones[i] == null)
                    return i;
            }
            return -1;
        }

        public BattleDecision GetBestBattleDecision(Player currentPlayer, Player opponent)
        {
            if (currentPlayer == null || opponent == null)
                return new BattleDecision { EndTurn = true };

            var attackers = currentPlayer.Field.MonsterZones
                .Select((monster, index) => new { monster, index })
                .Where(x => x.monster != null && !x.monster.HasAttackedThisTurn && x.monster.IsAttackMode)
                .ToList();

            if (!opponent.Field.HasMonster())
            {
                var directAttacker = attackers
                    .Where(x => x.monster.Card != null)
                    .OrderByDescending(x => x.monster.Card?.Attack ?? 0)
                    .FirstOrDefault();

                if (directAttacker != null)
                    return new BattleDecision
                    {
                        AttackerZoneIndex = directAttacker.index,
                        DefenderZoneIndex = -1
                    };

                return new BattleDecision { EndTurn = true };
            }

            var targets = opponent.Field.MonsterZones
                .Select((monster, index) => new { monster, index })
                .Where(x => x.monster != null)
                .ToList();

            var bestFight = attackers
                .SelectMany(att => targets, (att, def) => new
                {
                    attackerIndex = att.index,
                    defenderIndex = def.index,
                    score = GetBattleScore(att.monster, def.monster)
                })
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

            if (bestFight != null && bestFight.score > 0)
            {
                return new BattleDecision
                {
                    AttackerZoneIndex = bestFight.attackerIndex,
                    DefenderZoneIndex = bestFight.defenderIndex
                };
            }

            var riskyMonster = currentPlayer.Field.MonsterZones
                .Select((monster, index) => new { monster, index })
                .Where(x => x.monster != null && IsAtRisk(x.monster, opponent))
                .FirstOrDefault();

            if (riskyMonster != null && riskyMonster.monster.IsAttackMode)
            {
                return new BattleDecision
                {
                    SwitchToDefense = true,
                    DefenseZoneIndex = riskyMonster.index
                };
            }

            return new BattleDecision { EndTurn = true };
        }

        private static int GetFirstFreeMonsterZone(FieldZones field)
        {
            if (field == null) return -1;
            for (int i = 0; i < FieldZones.MONSTER_ZONES; i++)
            {
                if (field.MonsterZones[i] == null)
                    return i;
            }
            return -1;
        }

        private static IEnumerable<List<Cards>> GenerateOrderedSubsets(List<Cards> hand)
        {
            var results = new List<List<Cards>>();
            for (int size = 1; size <= hand.Count; size++)
            {
                BuildPermutations(hand, size, new bool[hand.Count], new List<Cards>(), results);
            }
            return results;
        }

        private static void BuildPermutations(List<Cards> hand, int targetSize, bool[] used, List<Cards> current, List<List<Cards>> results)
        {
            if (current.Count == targetSize)
            {
                results.Add(new List<Cards>(current));
                return;
            }

            for (int i = 0; i < hand.Count; i++)
            {
                if (used[i])
                    continue;

                used[i] = true;
                current.Add(hand[i]);

                BuildPermutations(hand, targetSize, used, current, results);

                current.RemoveAt(current.Count - 1);
                used[i] = false;
            }
        }

        private static FieldMonster? GetStrongestEnemyMonster(FieldZones enemyField)
        {
            if (enemyField == null)
                return null;

            return enemyField.MonsterZones
                .Where(x => x != null)
                .OrderByDescending(x => GetEffectiveDefense(x))
                .FirstOrDefault();
        }

        private static long GetEffectiveDefense(FieldMonster defender)
        {
            if (defender == null || defender.Card == null)
                return 0;

            if (defender.IsFaceDown)
                return defender.Card.Defense;

            return defender.IsAttackMode ? defender.Card.Attack : defender.Card.Defense;
        }

        private static int GetBattleScore(FieldMonster attacker, FieldMonster defender)
        {
            if (attacker == null || defender == null || attacker.Card == null || defender.Card == null)
                return int.MinValue;

            int attackPower = (int)attacker.Card.Attack;
            int defensePower = defender.IsAttackMode ? (int)defender.Card.Attack : (int)defender.Card.Defense;

            if (attackPower > defensePower)
                return 2000 + (attackPower - defensePower);

            if (attackPower == defensePower)
                return 100;

            return -1000 - (defensePower - attackPower);
        }

        private static bool IsAtRisk(FieldMonster monster, Player opponent)
        {
            if (monster == null || monster.Card == null || opponent == null)
                return false;

            long ownValue = monster.IsAttackMode ? monster.Card.Attack : monster.Card.Defense;

            return opponent.Field.MonsterZones
                .Where(x => x != null)
                .Any(def => def.Card != null && def.Card.Attack > ownValue);
        }
    }
}