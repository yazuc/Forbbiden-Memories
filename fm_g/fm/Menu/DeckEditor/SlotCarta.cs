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
		private static Dictionary<int, AtlasTexture> _typeCache = new();
		private static Dictionary<int, AtlasTexture> _signCache = new();

		private static readonly AtlasTexture AtlasBase = GD.Load<AtlasTexture>("res://Resources/types.res");
		private static readonly AtlasTexture AtlasBaseSign = GD.Load<AtlasTexture>("res://Resources/signs.res");
		public Cards item {get;set;}
		public int index {get;set;}
		
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			Sign = GetNode<TextureRect>("CardSign/TextureRect");
			Sign2 = GetNode<TextureRect>("CardSign/TextureRect2");
			Type = GetNode<TextureRect>("CardType2/Type");
			CardSign = GetNode<Godot.Label>("CardSign/Label");								
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

		public void SetAtlasRegion(CardTypeEnum cardType)
		{
			if (Type == null) return;

			int key = (int)cardType;

			if (!_typeCache.ContainsKey(key))
			{
				var atlas = (AtlasTexture)AtlasBase.Duplicate();
				atlas.Region = new Rect2(key * 16, 0, 16, 16);
				_typeCache[key] = atlas;
			}

			Type.Texture = _typeCache[key];
		}


		public void SetAtlasRegionSign(int sign, TextureRect target)
		{
			if (target == null) return;

			if (!_signCache.ContainsKey(sign))
			{
				var atlas = (AtlasTexture)AtlasBaseSign.Duplicate();
				atlas.Region = new Rect2(sign * 64, 0, 64, 64);
				_signCache[sign] = atlas;
			}

			target.Texture = _signCache[sign];
		}

	}	
}
