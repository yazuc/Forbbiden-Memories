// Source - https://stackoverflow.com/a/67910524␍
// Posted by MarredCheese, modified by community. See post 'Timeline' for change history␍
// Retrieved 2026-02-10, License - CC BY-SA 4.0␍

using System;
using System.IO;
using System.Threading.Tasks;
using QuickType;

namespace fm
{
	public class Function
	{
		public class FusionResult
		{
			public Cards MainCard { get; set; }
			public bool IsFaceDown {get;set;}
			public bool FusaoAconteceu {get;set;}
			public bool FalhaEquip {get;set;}
			public string WorldPos {get;set;} = string.Empty;
			public List<Cards> AppliedEquips { get; set; } = new List<Cards>();
			public List<Cards> CardsUsed {get;set;} = new List<Cards>();
			public List<FusionStep> Steps { get; set; } = new();
		}
		public class FusionStep
		{
			public Cards PreviousMain { get; set; }
			public Cards IncomingCard { get; set; }
			public Cards ResultCard { get; set; }

			public FusionAction Action { get; set; }

			public List<Cards> EquipsAfterStep { get; set; } = new();
		}


		public static FusionResult ProcessChain(string args, Cards card = null)
		{
			var cards = CardDatabase.Instance.GetAllCards();
			var cardIds = args.Split(',').Select(int.Parse).ToList();
			var queue = new Queue<int>(cardIds);

			if (queue.Count == 0) return null;
			if(card != null)
				queue.Dequeue();

			var result = new FusionResult();
			int currentCardId = card == null ? queue.Dequeue() : card.Id;
			result.MainCard = card != null ? card  : cards.FirstOrDefault(x => x.Id == currentCardId);

			if(result.MainCard != null && card == null)
				result.CardsUsed.Add(result.MainCard);

			while (queue.Count > 0)
			{
				int nextId = queue.Dequeue();
				var nextCard = cards.FirstOrDefault(x => x.Id == nextId);

				if(nextCard != null)
					result.CardsUsed.Add(nextCard);

				var step = new FusionStep
				{
					PreviousMain = result.MainCard,
					IncomingCard = nextCard
				};

				// 1. Tenta Fusão de Monstro primeiro (Transformação)
				if (TryGetFusion(currentCardId, nextId, cards, out int fusedId))
				{
					currentCardId = fusedId;
					result.FusaoAconteceu = true;
					result.MainCard = cards.FirstOrDefault(x => x.Id == currentCardId);
					// Ao fundir, tecnicamente os equips antigos costumam ser perdidos no FM
					result.AppliedEquips.Clear(); 
					step.Action = FusionAction.Fusion;
					step.ResultCard = result.MainCard;
					GD.Print($"Fusão: {nextId} transformou a carta em {fusedId}");
				}
				// 2. Se não fundiu, tenta Equipar
				else if (CanEquip(result.MainCard, nextCard))
				{
					result.AppliedEquips.Add(nextCard);
					step.Action = FusionAction.Equip;
					step.ResultCard = result.MainCard;
					GD.Print($"Equipou {nextCard.Name} em {result.MainCard.Name}");
				}
				// Caso B: MainCard é Equipamento e Next é Monstro (INVERSÃO)
				else if (CanEquip(nextCard, result.MainCard))
				{
					var equipParaGuardar = result.MainCard; // O antigo 'main' era o equip
					
					// O monstro novo assume o posto de MainCard
					result.MainCard = nextCard;
					currentCardId = nextCard.Id;
					
					step.Action = FusionAction.Inversion;
					step.ResultCard = result.MainCard;
					
					result.AppliedEquips.Clear(); // Limpa o que tinha antes
					result.AppliedEquips.Add(equipParaGuardar); // Adiciona o equip que estava esperando
					
					GD.Print($"Inversão: {nextCard.Name} assumiu como Main e recebeu {equipParaGuardar.Name}");
				}
				// 3. Se nada funcionou, a carta anterior é descartada e a nova vira a principal
				else
				{
					if (!nextCard.IsSpellTrap() || result.MainCard.IsSpellTrap() && nextCard.IsSpellTrap())
					{
						currentCardId = nextId;
						result.MainCard = nextCard;
						result.AppliedEquips.Clear();
						step.Action = FusionAction.Nothing;
						step.ResultCard = result.MainCard;
						GD.Print($"Nada aconteceu, nova carta base: {nextId}");
					}
					else
					{
						step.Action = FusionAction.EquipFail;
						step.ResultCard = result.MainCard;
						result.FalhaEquip = true;
					}
				}
				step.EquipsAfterStep = result.AppliedEquips.ToList();
				result.Steps.Add(step);
			}


			foreach(var item in result.AppliedEquips)
			{
				if(result.MainCard != null)
				{
					GD.Print(item.Attack);
					result.MainCard.Attack += item.Attack;			
					result.MainCard.Defense += item.Defense;		
				}
			}

			return result;
		}

		public static bool CanEquip(Cards monster, Cards equip)
		{
			if (monster == null || equip == null) return false;

			// Verifica se a carta vinda da fila é do tipo Equipamento
			// E se o ID dela está na lista de equipamentos aceitos pelo monstro
			return equip.Type == CardTypeEnum.Equipment && monster.Equips.Contains(equip.Id);
		}
		public static Cards Fusion(string args)
		{
			foreach(var item in ProcessChain(args).CardsUsed)
				GD.Print(item.Id);

			CardDatabase.Instance.GetAllCards();
			var cards = CardDatabase.Instance.GetAllCards();

			var cardIds = args.Split(',').Select(int.Parse).ToList();
			var queue = new Queue<int>(cardIds);
			
			if (queue.Count == 0)
				return null;

			var currentCard = queue.Dequeue();

			while (queue.Count > 0)
			{
				var nextCard = queue.Dequeue();
				
				int card1 = Math.Min(currentCard, nextCard);
				int card2 = Math.Max(currentCard, nextCard);
				
				if(TryGetFusion(card1, card2, cards, out int resultCard))
				{
					Console.WriteLine($"Fused {currentCard} + {nextCard} = {resultCard}");
					currentCard = resultCard;
				}
				else
				{
					Console.WriteLine($"No fusion for {currentCard} + {nextCard}, discarding {currentCard}");
					currentCard = nextCard;
				}
			}

			return cards.FirstOrDefault(x => x.Id == currentCard) ?? null;
		}
		
		public static bool TryGetFusion(int card1ID, int card2ID, List<Cards> cards, out int result)
		{
			int card1 = Math.Min(card1ID, card2ID);
			int card2 = Math.Max(card1ID, card2ID);
			result = -1;
			var fusion = cards.FirstOrDefault(x => x.Id == card1);

			if(fusion != null && fusion.Fusions.Any())
			{
				foreach(var item in fusion.Fusions)
				{
					if(item.Card2 == card2)
					{
						result = item.Result;
						return true;
					}                
				}
			}

			return false;
		}
	}  
}
