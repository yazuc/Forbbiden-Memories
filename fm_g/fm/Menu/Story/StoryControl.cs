using fm;
using Godot;
using System;

public partial class StoryControl : Control
{
	public float TextSpeed = 0.03f;
	public AnimatedSprite2D boneco;
	[Export] public Texture2D[] Backgrounds;
	private int _currentBgIndex = 5;
	private TextureRect _background;
	private RichTextLabel _textLabel;
	private bool _isTyping = false;
	private bool _waitingForChoice = false;
	[Export] public Control ChoiceContainer; 
	public List<string> dialogue = new List<string>();
	public PackedScene scene = GD.Load<PackedScene>("res://Scenes/game.tscn");
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AnimatedSprite2D? sprite = GetNode<AnimatedSprite2D>("SubViewportContainer/SubViewport/AnimatedSprite2D");
		_background = GetNode<TextureRect>("Cena/Background");
		_textLabel = GetNode<RichTextLabel>("Cena/Dialogo/RichTextLabel");
		SetBackgroundByIndex(GlobalUsings.Instance.currentBackGround);
		dialogue.Add("A wasted effort, boy!");
		dialogue.Add("You lack the power to defeat me!");
		dialogue.Add("<Duel>");

		if(sprite != null)		
			boneco = sprite;								
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept") && !_isTyping && !_waitingForChoice)
		{
			AdvanceDialogue();
		}
	}

	private void AdvanceDialogue()
	{
		if (dialogue.Count > 0)
		{
			string nextLine = dialogue[0];
			dialogue.RemoveAt(0);

			if (nextLine == "<Duel>")
			{
				ShowDuelChoices();
			}
			else
			{
				StartDialogue(nextLine);
			}
		}
		else
		{			
			GlobalUsings.Instance.DeckIndex = 8;					
			GetTree().ChangeSceneToFile("res://Scenes/game.tscn");
		}
	}

	private async void StartDialogue(string text)
	{
		_isTyping = true;
		_textLabel.Text = text;
		_textLabel.VisibleCharacters = 0;

		boneco.Play();
		for (int i = 0; i <= text.Length; i++)
		{
			if (!_isTyping) break;
			_textLabel.VisibleCharacters = i;
			await ToSignal(GetTree().CreateTimer(TextSpeed), "timeout");
		}
		boneco.SetFrameAndProgress(0, 0f);
		boneco.Pause();
		_textLabel.VisibleCharacters = text.Length;
		_isTyping = false;
	}

	private void ShowDuelChoices()
	{
		_waitingForChoice = true;
		_textLabel.Text = ""; // Ou deixe vazio se quiser apenas as opções
		
		Button acceptBtn = CreateChoiceButton("<Duel>", true);		
		Button refuseBtn = CreateChoiceButton("<Pass>", false);

		ChoiceContainer.AddChild(acceptBtn);
		ChoiceContainer.AddChild(refuseBtn);

		acceptBtn.GrabFocus();
	}

	private Button CreateChoiceButton(string text, bool isAccept)
	{
		Button btn = new Button();
		btn.Text = "> " + text;
		btn.Alignment = HorizontalAlignment.Left; // Alinha o texto à esquerda
		btn.Flat = true; // Remove a caixa feia do botão, deixando só o texto

		// Estilização: Cor Amarela quando selecionado (Foco)
		btn.AddThemeColorOverride("font_focus_color", Colors.Yellow);
		btn.AddThemeColorOverride("font_hover_color", Colors.Yellow);
		btn.AddThemeColorOverride("font_pressed_color", Colors.Gold);
		
		// Remove a borda de foco azul padrão do Godot para ficar mais limpo
		btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

		// Conecta o clique
		btn.Pressed += () => OnChoiceSelected(isAccept);

		return btn;
	}

	private void OnChoiceSelected(bool accepted)
	{
		// Limpa os botões imediatamente
		foreach (var child in ChoiceContainer.GetChildren()) child.QueueFree();

		if (accepted)
		{
			_waitingForChoice = false;
			AdvanceDialogue(); 
		}
		else
		{
			// Se recusar, o vilão reclama
			StartDialogue("Não fuja! O destino é inevitável!");
			
			// Bloqueia o input por um tempo e depois mostra as opções de novo
			_waitingForChoice = true; 
			GetTree().CreateTimer(2.0f).Timeout += () => ShowDuelChoices();
		}
	}

	private void SetBackgroundByIndex(int index)
	{
		if (index < 0 || index >= Backgrounds.Length) return;
		_currentBgIndex = index;
		_background.Texture = Backgrounds[index];
	}
}
