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
					GD.Print($"- Zone {i + 1}: zn: {monster.zoneName} - {monster.Card.Name} - Status ATK:{monster.Card.Attack} DEF:{monster.Card.Defense} ({(monster.IsAttackMode ? "ATK" : "DEF")}, Turns: {monster.TurnsOnField})");
				}
				else
				{
					GD.Print($"- Zone {i + 1}: Empty");
				}
			}

			GD.Print("Spell/Trap Zones:");
			for (int i = 0; i < SPELL_TRAP_ZONES; i++)
			{
				var spellTrap = SpellTrapZones[i];
				if (spellTrap != null)
				{
					GD.Print($"- Zone {i + 1}: {(spellTrap.IsFaceDown ? "Face-down" : spellTrap.Card.Name)}");
				}
				else
				{
					GD.Print($"- Zone {i + 1}: Empty");
				}
			}
		}
		
		public bool placeCard(int idField, Cards card, bool isAttackMode = true, bool isFaceDown = false, bool ini = false)
		{
			if (card.Type != CardTypeEnum.Trap && card.Type != CardTypeEnum.Spell && card.Type != CardTypeEnum.Equipment && card.Type != CardTypeEnum.Ritual)
				return PlaceMonster(idField, card, isAttackMode, isFaceDown, ini);
			else if (card.Type == CardTypeEnum.Spell || card.Type == CardTypeEnum.Trap ||
			 card.Type == CardTypeEnum.Equipment || card.Type == CardTypeEnum.Ritual)
				return PlaceSpellTrap(idField, card, isFaceDown); // For simplicity, we place all spells/traps in the first zone
			return false;
		}
		
		public void BotaDeLadinho(string ID, bool DeLadinho)
		{
			foreach(var monstro in MonsterZones)
			{
				if(monstro != null && monstro.zoneName == ID)
				{
					monstro.IsAttackMode = !DeLadinho;
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
				Card = card, 
				IsAttackMode = isAttackMode,
				IsFaceDown = isFaceDown,
				TurnsOnField = 0
				
			};
			return true;
		}

		public bool PlaceSpellTrap(int idField, Cards card, bool isFaceDown = false)
		{
			int zoneIndex = idField;
			if (zoneIndex < 0 || zoneIndex >= SPELL_TRAP_ZONES  )
				return false;

			SpellTrapZones[zoneIndex] = new FieldSpellTrap 
			{ 
				Card = card, 
				IsFaceDown = isFaceDown
			};
			return true;
		}

		public bool RemoveMonster(string zoneName)
		{
			int zoneIndex = 0;			
			foreach(var item in MonsterZones)
			{
				if(item.zoneName == zoneName){					
					break;
				}
				zoneIndex++;
			}
				

			MonsterZones[zoneIndex] = null;
			return true;
		}

		public FieldMonster? GetMonsterInZone(
			int zoneIndex) => 
			(zoneIndex >= 0 && zoneIndex < MONSTER_ZONES) ? MonsterZones[zoneIndex] : null;

		public bool HasAvailableMonsterZone() => MonsterZones.Any(z => z == null);
	}
		

	public class FieldMonster
	{
		public string zoneName {get; set;}
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
