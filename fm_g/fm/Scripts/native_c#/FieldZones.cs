using QuickType;
using Godot;
using System;

namespace fm
{
	public class FieldZones
	{
		public const int MONSTER_ZONES = 5;
		public const int SPELL_TRAP_ZONES = 5;
		public List<string> camposName = new List<string>();
		// Monster Zones: [Index 0-4]
		public FieldMonster[] MonsterZones { get; set; }
		
		// Spell/Trap Zones: [Index 0-4]
		public FieldSpellTrap[] SpellTrapZones { get; set; }
		
		// Field Spell Zone: Only 1
		public Cards? FieldSpell { get; set; }

		public FieldZones(List<string> nomesCampos)
		{
			MonsterZones = new FieldMonster[MONSTER_ZONES];		
			this.camposName = nomesCampos;	
			SpellTrapZones = new FieldSpellTrap[SPELL_TRAP_ZONES];
			FieldSpell = null;
		}
		
		public int HasMonsterOnZone(int Idx){
			return MonsterZones[Idx].Card.Id;
		}
		
		public bool HasMonster(){
			foreach(var item in MonsterZones){
				if(item != null)
					return item != null;
			}
			return false;
		}

		public void DrawFieldState()
		{
			GD.Print("=== Field State ===");
			GD.Print("Monster Zones:");
			for (int i = 0; i < MONSTER_ZONES; i++)
			{
				var monster = MonsterZones[i];
				if (monster != null)
				{
					GD.Print($"- Zone {i}: zn: {monster.zoneIndex} - {monster.zoneName} - {monster.Card.Name} - Status ATK:{monster.Card.Attack} DEF:{monster.Card.Defense} ({(monster.IsAttackMode ? "ATK" : "DEF")}, Turns: {monster.TurnsOnField})");
				}
				else
				{
					GD.Print($"- Zone {i}: Empty");
				}
			}

			GD.Print("Spell/Trap Zones:");
			for (int i = 0; i < SPELL_TRAP_ZONES; i++)
			{
				var spellTrap = SpellTrapZones[i];
				if (spellTrap != null)
				{
					GD.Print($"- Zone {i + 1}: {spellTrap.Card.Name}");
				}
				else
				{
					GD.Print($"- Zone {i + 1}: Empty");
				}
			}
		}
		
		public bool placeCard(int idField, Cards card, bool isAttackMode = true, bool isFaceDown = false, bool ini = false)
		{
			if(idField == -1) return false;
			if (!card.IsSpellTrap())
				return PlaceMonster(idField, card, isAttackMode, isFaceDown, ini);
			else
				return PlaceSpellTrap(idField, card, isFaceDown); 		
		}
		
		public void BotaDeLadinho(string ID, bool DeLadinho)
		{
			GD.Print("bota de ladinho: " + ID + "-" + DeLadinho);
			foreach(var monstro in MonsterZones)
			{
				if(monstro != null && monstro.zoneName == ID)
				{
					GD.Print("monstro zone name" + monstro.zoneName + " - " + ID);
					monstro.IsAttackMode = !DeLadinho;
					DrawFieldState();
					break;	
				}
				
			}
		}
		
		public bool PlaceMonster(int idField, Cards card, bool isAttackMode = true, bool isFaceDown = false, bool ini = false)
		{
			int zoneIndex = idField;
			if (zoneIndex < 0 || zoneIndex >= MONSTER_ZONES)
				return false;

			MonsterZones[zoneIndex] = new FieldMonster 
			{ 
				zoneName = ini ?  $"Carta{zoneIndex + 1}IniM" : $"Carta{zoneIndex + 1}M",
				zoneIndex = zoneIndex,
				Card = card, 
				IsAttackMode = isAttackMode,
				IsFaceDown = isFaceDown,
				TurnsOnField = 0
				
			};
			return true;
		}

		public bool PlaceSpellTrap(int idField, Cards card, bool isFaceDown = false, bool ini = false)
		{
			int zoneIndex = idField;
			if (zoneIndex < 0 || zoneIndex >= SPELL_TRAP_ZONES  )
				return false;

			SpellTrapZones[zoneIndex] = new FieldSpellTrap 
			{ 
				zoneName = ini ?  $"Carta{zoneIndex + 1}IniS" : $"Carta{zoneIndex + 1}S",
				Card = card, 
				IsFaceDown = isFaceDown
			};
			return true;
		}

		public bool RemoveMonster(string zoneName)
		{
			var index = MonsterZones.Where(x => x != null && x.zoneName == zoneName).Select(x => x.zoneIndex).FirstOrDefault();
			MonsterZones[index] = null;						
			return true;
		}
		
		public FieldMonster? GetMonsterInZone(string zoneName) => MonsterZones.FirstOrDefault(x => x != null && x.zoneName == zoneName);		
		public FieldSpellTrap? GetFieldSpellTrap(string zoneName) => SpellTrapZones.FirstOrDefault(x => x != null && x.zoneName == zoneName);
		public Cards? GetCardInZone(string zoneName) => MonsterZones.FirstOrDefault(x => x != null && x.zoneName == zoneName)?.Card;
		public FieldMonster? GetMonsterInZone(
			int zoneIndex) => 
			(zoneIndex >= 0 && zoneIndex < MONSTER_ZONES) ? MonsterZones[zoneIndex] : null;

		public bool HasAvailableMonsterZone() => MonsterZones.Any(z => z == null);
		public bool UpdateMonster(Cards carta, string ZoneIndex)
		{
			var monster = GetMonsterInZone(ZoneIndex);
			if(monster != null)
			{
				monster.Card = carta;
				return true;
			}
			return false;
		}
	}
		

	public class FieldMonster
	{
		public string zoneName {get; set;}
		public int zoneIndex {get;set;}
		public Cards? Card { get; set; }
		public bool IsAttackMode { get; set; }
		public bool IsFaceDown { get; set; }
		public int TurnsOnField { get; set; }
		public bool HasAttackedThisTurn { get; set; }
		
	}

	public class FieldSpellTrap
	{
		public string zoneName {get;set;}
		public Cards? Card { get; set; }
		public bool IsFaceDown { get; set; }
	}
}
