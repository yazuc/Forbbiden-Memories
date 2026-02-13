using System;
using System.Collections.Generic;
using System.Linq;
using QuickType;

namespace fm
{    
    public class FmStarterDeckGenerator
    {
        private readonly Random _rng = new();

        public List<Cards> GenerateStarterDeck(List<Cards> allCards)
        {
            var deck = new List<Cards>();

            // --- FILTER POOLS ---

            var monsterTypes = new[] 
            { 
                CardTypeEnum.Dragon, CardTypeEnum.Spellcaster, CardTypeEnum.Zombie, 
                CardTypeEnum.Warrior, CardTypeEnum.BeastWarrior, CardTypeEnum.Beast, 
                CardTypeEnum.WingedBeast, CardTypeEnum.Fiend, CardTypeEnum.Fairy, 
                CardTypeEnum.Insect, CardTypeEnum.Dinosaur, CardTypeEnum.Reptile, 
                CardTypeEnum.Fish, CardTypeEnum.SeaSerpent, CardTypeEnum.Machine, 
                CardTypeEnum.Thunder, CardTypeEnum.Aqua, CardTypeEnum.Pyro, 
                CardTypeEnum.Rock, CardTypeEnum.Plant 
            };

            var monsters = allCards
                .Where(c => monsterTypes.Contains(c.Type))
                .ToList();

            var coreMonsters = monsters
                .Where(c =>
                    c.Attack >= 1200 &&
                    c.Attack <= 2000 &&
                    c.Level >= 3 &&
                    c.Level <= 6)
                .ToList();

            var fillerMonsters = monsters
                .Where(c =>
                    c.Attack >= 500 &&
                    c.Attack < 1200)
                .ToList();

            var fusionMonsters = coreMonsters
                .Where(c => c.Fusions != null && c.Fusions.Length > 0)
                .ToList();

            var equipSpells = allCards
                .Where(c => c.Type == CardTypeEnum.Equipment)
                .ToList();

            var fieldSpells = allCards
                .Where(c => c.Type == CardTypeEnum.Spell)
                .ToList();

            var boardClears = allCards
                .Where(c => c.Name == "Raigeki" || c.Name == "Dark Hole")
                .ToList();

            // --- ATTRIBUTE BIAS ---
            var dominantAttribute = coreMonsters
                .GroupBy(c => c.Attribute)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            var dominantPool = coreMonsters
                .Where(c => c.Attribute == dominantAttribute)
                .ToList();

            // --- BUILD MONSTER CORE ---

            AddRandom(deck, dominantPool, 12);
            AddRandom(deck, coreMonsters, 6);

            // Add fusion-friendly monsters
            AddRandom(deck, fusionMonsters, 4);

            // Add filler monsters
            AddRandom(deck, fillerMonsters, 6);

            // --- SPELL PACKAGE ---

            if (boardClears.Any())
                deck.Add(boardClears[_rng.Next(boardClears.Count)]);

            if (fieldSpells.Any())
                deck.Add(fieldSpells[_rng.Next(fieldSpells.Count)]);

            AddRandom(deck, equipSpells, _rng.Next(1, 4));

            // --- FILL UNTIL 40 ---
            while (deck.Count < 40)
            {
                var candidate = coreMonsters[_rng.Next(coreMonsters.Count)];
                deck.Add(candidate);
            }

            return deck.Take(40).ToList();
        }

        private void AddRandom(List<Cards> deck, List<Cards> pool, int count)
        {
            if (!pool.Any())
                return;

            for (int i = 0; i < count; i++)
            {
                var card = pool[_rng.Next(pool.Count)];
                deck.Add(card);
            }
        }
    }
}