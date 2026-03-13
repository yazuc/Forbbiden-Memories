using Godot;
using QuickType;
using System;
using System.Reflection.Emit;

namespace fm
{
	public partial class SlotCarta : Godot.HBoxContainer
	{
		[Export] public Godot.Label DeckNumber;
		[Export] public Godot.Label CardNumber;
		[Export] public Godot.Label CardName;
		[Export] public Godot.Label CardStats;
		[Export] public Godot.Label CardType;
		[Export] public TextureRect Type;
		[Export] public TextureRect Sign;
		[Export] public TextureRect Sign2;
		[Export] public Godot.Label CardSign;
		[Export] public Godot.ColorRect CardSigns;		

		private static readonly AtlasTexture AtlasBase = GD.Load<AtlasTexture>("res://Resources/types.res");
		private static readonly AtlasTexture AtlasBaseSign = GD.Load<AtlasTexture>("res://Resources/signs.res");
		public Cards item {get;set;}
		public int index {get;set;}
		
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			string srcGodot = "res://starter_deck.txt";
			string srcPath = ProjectSettings.GlobalizePath(srcGodot);
			Sign = GetNode<TextureRect>("CardSign/TextureRect");
			Sign2 = GetNode<TextureRect>("CardSign/TextureRect2");
			CardSign = GetNode<Godot.Label>("CardSign/Label");
			var deck = new Deck();					
			var deckList = Funcoes.LoadUserDeck(srcPath);
			deck.LoadDeck(deckList);									
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
	
		}

		public void Initialize(Cards item, int index)
		{
			this.item = item;
			this.index = index;		

			string stats =  "ATK " + item.Attack.ToString() + "\n" + "DEF " + item.Defense.ToString();
			if(item.Type == CardTypeEnum.Spell || item.Type == CardTypeEnum.Trap || item.Type == CardTypeEnum.Ritual || item.Type == CardTypeEnum.Equipment)
				stats = "";
			FillLabel(
				index.ToString(),
				item.Id.ToString(),
				item.Name,
				stats,
				item.Type,
				item.GuardianStarA,
				item.GuardianStarB
			);
		}

		public void FillLabel(string DeckNumber, string CardNumber, string CardName, string CardStats, CardTypeEnum CardType, GuardianStar CardSign, GuardianStar CardSign2)
		{			
			//Type = GetNode<ColorRect>("CardType").GetChild<TextureRect>(0);
			this.DeckNumber.Text = DeckNumber;
			this.CardNumber.Text = CardNumber;
			this.CardName.Text = CardName;
			this.CardStats.Text = CardStats;
			this.CardType.Text = CardType.ToString();
			this.CardSign.Text = CardSign.ToString();
			SetAtlasRegion(CardType);
			SetAtlasRegionSign((int)CardSign - 1, Sign);
			SetAtlasRegionSign((int)CardSign2 - 1, Sign2);
		}

		public void SetAtlasRegion(CardTypeEnum CardType)
		{
			if(Type == null) return;
			Type.Texture = (AtlasTexture)AtlasBase.Duplicate();
			if (Type.Texture is not AtlasTexture atlas) return;			

			if(atlas == null) return;

			atlas.Region = new Rect2((int)CardType * 16, 0, 16, 16);

			if(Type != null)
				Type.Texture = atlas;
		}

		public void SetAtlasRegionSign(int Sign, TextureRect TypeSign)
		{
			if(TypeSign == null) return;
			TypeSign.Texture = (AtlasTexture)AtlasBaseSign.Duplicate();
			if (TypeSign.Texture is not AtlasTexture atlas) return;			

			if(atlas == null) return;

			atlas.Region = new Rect2(Sign * 64, 0, 64, 64);

			if(TypeSign != null)
				TypeSign.Texture = atlas;
		}
	}	
}
