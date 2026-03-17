using Godot;
using System;

public partial class InformacaoCarta : HBoxContainer
{
	private static readonly AtlasTexture AtlasBase = GD.Load<AtlasTexture>("res://Resources/types.res");
	private static readonly AtlasTexture AtlasBaseSign = GD.Load<AtlasTexture>("res://Resources/signs.res");
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void DefineRegion(CardTypeEnum typeEnum, int Sign1, int Sign2, string CartaNome)
	{
		var texture = GetNode<TextureRect>("Panel/CardType2/Type");
		var textureG = GetNode<TextureRect>("Panel/CardSign/TextureRect");
		var textureG2 = GetNode<TextureRect>("Panel/CardSign/TextureRect2");
		var nome = GetNode<Label>("Nome");
		nome.Text = CartaNome;
		if (IsInstanceValid(texture))		
			SetAtlasRegion(typeEnum, texture);		
		if(IsInstanceValid(textureG))
			SetAtlasRegionSign(Sign1 - 1, textureG);
		if(IsInstanceValid(textureG2))
			SetAtlasRegionSign(Sign2 - 1, textureG2);
	}

	public void SetAtlasRegion(CardTypeEnum CardType, TextureRect Type)
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
