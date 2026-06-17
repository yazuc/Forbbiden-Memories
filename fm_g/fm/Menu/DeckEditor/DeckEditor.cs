using Godot;
using System;
using System.ComponentModel;

namespace fm;
public partial class DeckEditor : Control
{
	[Export] public Panel selector;
	[Export] public ScrollContainer scroll;    	
	public DeckBuildEnum DeckBuild {get;set;} = DeckBuildEnum.Trunk;
	public int j = 0, k = 0, opt = 0;	
	public bool once = false, SetupDone = false, SetupDoneDeck = false;
	[Export] public Godot.VBoxContainer decklist;	
	private List<SlotCarta> slotCartas = new List<SlotCarta>();
	public List<TextureButton> textureButtons;
	// Called when the node enters the scene tree for the first time.
	public override  void _Ready()
	{
		SetupTrunk();
		HideSlotInGroup(DeckBuild.ToString(), true);
		HideSlotInGroup(DeckBuildEnum.Deck.ToString(), false);
		// textureButtons = GetTree().GetNodesInGroup("button").Cast<TextureButton>().ToList();	
		// textureButtons[0].GrabFocus();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{				
		MoveSelector(opt);		
	}
	public override async void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_left") || @event.IsActionPressed("ui_right"))
		{
			if (@event.IsActionPressed("ui_right"))
			{
				j = opt;
				opt = k;
				GD.Print("called");				
				DeckBuild = DeckBuildEnum.Deck;
				SetupDeck();	
			}
			if (@event.IsActionPressed("ui_left"))
			{			
				k = opt;
				opt = j;
				GD.Print("called left");				
				DeckBuild = DeckBuildEnum.Trunk;
				SetupTrunk();
			}
			GetViewport().SetInputAsHandled();
		}
		if (Input.IsActionJustReleased("ui_cancel"))
		{
			await GlobalUsings.Instance.GoBack();
		}
	}

    public override void _UnhandledInput(InputEvent @event)
    {
		
		// if (@event.IsActionPressed("ui_lb"))
		// {			
		// 	textureButtons = GetTree().GetNodesInGroup("button").Cast<TextureButton>().ToList();	
		// 	textureButtons[0].GrabFocus();
		// 	var index = textureButtons.IndexOf(textureButtons.FirstOrDefault(x => x.HasFocus()));			
		// 	if(index <= 0) index = textureButtons.Count;		
		// 	textureButtons[index - 1].GrabFocus(); 
		// 	Filter((TipoFiltro)index - 1);

		// }
		// if (@event.IsActionPressed("ui_rb"))
		// {
		// 	textureButtons = GetTree().GetNodesInGroup("button").Cast<TextureButton>().ToList();	
		// 	textureButtons[0].GrabFocus();
		// 	var index = textureButtons.IndexOf(textureButtons.FirstOrDefault(x => x.HasFocus()));		
		// 	if(index >= textureButtons.Count - 1) index = -1;			
		// 	textureButtons[index + 1].GrabFocus(); 
		// 	Filter((TipoFiltro)index + 1);
		// }		
		if(@event.IsActionReleased("ui_accept"))
		{
			if(DeckBuild == DeckBuildEnum.Trunk)
			{
				GlobalUsings.Instance.Deck.AddCard(GlobalUsings.Instance.db.GetCardById(GetCardInPos(DeckBuildEnum.Trunk)));
				UpdateNumber(DeckBuildEnum.Trunk);
				GD.Print("add card from trunk to deck if less than 40 cards");
			}
			if(DeckBuild == DeckBuildEnum.Deck)
			{
				GlobalUsings.Instance.Deck.RemoveCard(GetCardInPos(DeckBuildEnum.Deck));
				GD.Print("remove card from deck to trunk");
			}
		}
		if(@event.IsActionPressed("ui_down"))
		{
			if(opt < decklist.GetChildren().OfType<SlotCarta>().Count(x => x.Visible) - 1)
				opt++;				
		}
		if (@event.IsActionPressed("ui_up"))
		{
			if(opt > 0)
				opt--;							
		}			
	
    }

	public void Filter(TipoFiltro tipo)
	{
		if(tipo == TipoFiltro.Numero)
			slotCartas = slotCartas.OrderBy(x => x.item.Id).ToList();		
		if(tipo == TipoFiltro.NumeroSlot)
			slotCartas = slotCartas.OrderBy(x => int.Parse(x.DeckNumber.Text)).ToList();	
		if(tipo == TipoFiltro.Monstro)
			slotCartas = slotCartas.OrderByDescending(x => !x.item.IsSpellTrap()).ToList();
		if(tipo == TipoFiltro.Ataque)
			slotCartas = slotCartas.OrderByDescending(x => x.item.Attack).ToList();
		if(tipo == TipoFiltro.Defesa)
			slotCartas = slotCartas.OrderByDescending(x => x.item.Defense).ToList();
		if(tipo == TipoFiltro.Tipo)
			slotCartas = slotCartas.OrderBy(x => x.item.Type).ToList();		

		for (int i = 0; i < slotCartas.Count; i++)
		{
			decklist.MoveChild(slotCartas[i], i);
		}
		j = 0;
		MoveSelector(j);
	}

	public void Setup()
	{
		var scene1 = "res://Menu/DeckEditor/slot_carta.scn";
		var scene = GD.Load<PackedScene>(scene1);
		int i = 1;
		foreach(var item in GlobalUsings.Instance.Deck.Cards)
		{
			var cell = scene.Instantiate();				
			if(cell is SlotCarta slot)
			{
				decklist.AddChild(slot);
				slot.DeckBuild = DeckBuild;
				slot.Initialize(item, i);		
				slot.AddToGroup(DeckBuild.ToString());
				slotCartas.Add(slot);		
				i++;				
			}
		}	
	}

	public void SetupTrunk()
	{
		if (SetupDone)
		{
			HideSlotInGroup(DeckBuildEnum.Deck.ToString(), false);			
			HideSlotInGroup(DeckBuildEnum.Trunk.ToString(), true);
			slotCartas = slotCartas.OrderBy(x => x.DeckBuild == DeckBuildEnum.Trunk).ToList();
			return;
		}

		var scene1 = "res://Menu/DeckEditor/slot_carta.scn";
		var scene = GD.Load<PackedScene>(scene1);
		int i = 1;
		foreach(var item in GlobalUsings.Instance.db.GetAllCards().OrderBy(x => x.Id))
		{
			var cell = scene.Instantiate();				
			if(cell is SlotCarta slot)
			{
				decklist.AddChild(slot);
				slot.DeckBuild = DeckBuildEnum.Trunk;
				slot.Initialize(item, i);		
				slot.AddToGroup(DeckBuild.ToString());
				slotCartas.Add(slot);		
				i++;				
			}
		}	
		slotCartas = slotCartas.OrderBy(x => x.DeckBuild == DeckBuildEnum.Trunk).ToList();
		SetupDone = true;
	}

	public void SetupDeck()
	{
		if (SetupDoneDeck)
		{
			HideSlotInGroup(DeckBuildEnum.Deck.ToString(), true);
			HideSlotInGroup(DeckBuildEnum.Trunk.ToString(), false);
			slotCartas = slotCartas.OrderBy(x => x.DeckBuild == DeckBuildEnum.Deck).ToList();
			return;
		}

		var scene1 = "res://Menu/DeckEditor/slot_carta.scn";
		var scene = GD.Load<PackedScene>(scene1);
		int i = 1;
		foreach(var item in GlobalUsings.Instance.Deck.Cards)
		{
			var cell = scene.Instantiate();				
			if(cell is SlotCarta slot)
			{
				decklist.AddChild(slot);
				slot.DeckBuild = DeckBuildEnum.Deck;
				slot.Initialize(item, i);		
				slot.AddToGroup(DeckBuild.ToString());
				slotCartas.Add(slot);		
				i++;				
			}
		}	
		SetupDoneDeck = true;	
		slotCartas = slotCartas.OrderBy(x => x.DeckBuild == DeckBuildEnum.Deck).ToList();
		HideSlotInGroup(DeckBuildEnum.Deck.ToString(), true);
		HideSlotInGroup(DeckBuildEnum.Trunk.ToString(), false);
	}
	public void UpdateNumber(DeckBuildEnum deckBuild)
	{

		slotCartas = slotCartas.OrderByDescending(x => x.DeckBuild == deckBuild).ToList();
		var slot = slotCartas[opt];
		slot.UpdateNumbers();
	}

	public int GetCardInPos(DeckBuildEnum deckBuild)
	{

		slotCartas = slotCartas.OrderByDescending(x => x.DeckBuild == deckBuild).ToList();
		var slot = slotCartas[opt];
		if(slot == null || slot.CardNumber.Text == "")
		{
			return 0;
		}
		string CardName = slot.CardNumber.Text;
		var cardID = CardName != null ? int.Parse(CardName) : 0; 
		if(deckBuild == DeckBuildEnum.Trunk)
		{
			AddCarta(slot);
		}
		if(deckBuild == DeckBuildEnum.Deck)
		{
			RemoveCarta(slot);
		}
		
		GD.Print(cardID);
		return cardID;
	}

	public void AddCarta(SlotCarta slot)
	{		
		var deckSlot = slotCartas.FirstOrDefault(x => x.DeckBuild == DeckBuildEnum.Deck && x.CardNumber.Text == "");	
		if(deckSlot != null)
		{
			deckSlot.FillLabel(
				deckSlot.DeckNumber.Text,
				slot.CardNumber.Text,
				slot.CardName.Text,
				slot.CardStats.Text,
				slot.item.Type,
				slot.item.GuardianStarA,
				slot.item.GuardianStarB				
			);
			deckSlot.DeckBuild = DeckBuildEnum.Deck;
			slotCartas.Add(deckSlot);
		}
				
		UpdateVisualDeck(DeckBuildEnum.Deck);
	}

	public void RemoveCarta(SlotCarta slot)
	{				

		slot.FillEmpty();	
		slotCartas.RemoveAt(opt);
		slotCartas.Add(slot);			
		UpdateVisualDeck(DeckBuildEnum.Deck);
	}

	public void UpdateVisualDeck(DeckBuildEnum deckBuild)
	{
		var targetIndices = decklist.GetChildren()
			.OfType<SlotCarta>()
			.Select((slot, index) => new { slot, index })
			.Where(x => x.slot.DeckBuild == deckBuild)
			.Select(x => x.index)
			.ToList();

		for (int i = 0; i < targetIndices.Count; i++)
		{
			decklist.MoveChild(slotCartas[i], i);
		}	
	}

	public void HideSlotInGroup(string group, bool visible = false)
	{
		if(!visible)
			GetTree().CallGroup(group, "hide");
		if(visible)
			GetTree().CallGroup(group, "show");
	}

	public void MoveSelector(int index)
	{
		if (decklist.GetChildCount() == 0)
			return;

		if (index < 0 || index >= decklist.GetChildCount())
			return;

		var slot = decklist.GetChild(index) as HBoxContainer;
		if (slot == null)
		{
			return;
		}
		
		scroll.EnsureControlVisible(slot);
		slot.ForceUpdateTransform();

		var rect = slot.GetGlobalRect();			
		selector.GlobalPosition = rect.Position;
		selector.Size = rect.Size;			
	}
}
