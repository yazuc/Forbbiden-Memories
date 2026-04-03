using Godot;
using QuickType;
using System;

public partial class SeletorGuardian : Panel
{
	private static readonly AtlasTexture AtlasBaseSign = GD.Load<AtlasTexture>("res://Resources/signs.res");
	public required Cards CurrentCard {get;set;}
	public Label GuardianName1, GuardianName2;
	public TextureRect GuardianIcon1, GuardianIcon2;
	public TextureButton GuardianButton1, GuardianButton2;
	private TaskCompletionSource<int> _tcsCarta;
	private static Dictionary<int, AtlasTexture> _signCache = new();
	public Action Guardian1, Guardian2;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GuardianName1 = GetNode<Label>("VBoxContainer/HBoxContainer/GuardianButton/GuardianName");
		GuardianName2 = GetNode<Label>("VBoxContainer/HBoxContainer2/GuardianButton/GuardianName");
		GuardianIcon1 = GetNode<TextureRect>("VBoxContainer/HBoxContainer/GuardianButton/GuardianIcon");
		GuardianIcon2 = GetNode<TextureRect>("VBoxContainer/HBoxContainer2/GuardianButton/GuardianIcon");
		GuardianButton1 = GetNode<TextureButton>("VBoxContainer/HBoxContainer/GuardianButton");
		GuardianButton2 = GetNode<TextureButton>("VBoxContainer/HBoxContainer2/GuardianButton");		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Setup(Cards card)
	{
		CurrentCard = card;
		GuardianName1.Text = card.GuardianStarA.ToString();
		GuardianName2.Text = card.GuardianStarB.ToString();
		SetAtlasRegionSign((int)card.GuardianStarA - 1, GuardianIcon1);
		SetAtlasRegionSign((int)card.GuardianStarB - 1, GuardianIcon2);
		GuardianButton1.GrabFocus();
		Guardian1 = () => OnGuardianSelected((int)card.GuardianStarA - 1);
		Guardian2 = () => OnGuardianSelected((int)card.GuardianStarB - 1);
		GuardianButton1.Pressed +=  Guardian1;
		GuardianButton2.Pressed +=  Guardian2;
	}
	public int OnGuardianSelected(int guardianIndex)
	{
		GD.Print("Guardião selecionado: " + guardianIndex);
		_tcsCarta?.TrySetResult(guardianIndex);
		GuardianButton1.Pressed -= Guardian1;
		GuardianButton2.Pressed -= Guardian2;
		return guardianIndex;
	}

	public async Task<int> AguardarConfirmacaoSign()
	{
		_tcsCarta = new TaskCompletionSource<int>();		
		var resultado = await _tcsCarta.Task;		
		return resultado;
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
