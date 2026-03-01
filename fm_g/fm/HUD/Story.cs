using Godot;
using System;
using System.Collections.Generic; // Necessário para List
using System.Linq; // Necessário para FirstOrDefault

public partial class Story : Node2D
{
	public float TextSpeed = 0.03f;
	private RichTextLabel _textLabel;
	private bool _isTyping = false;
	private bool _waitingForChoice = false; // Nova trava para escolhas
	private List<string> _dialogueLines = new List<string>();
	
	private Sprite2D _background;
	[Export] public Texture2D[] Backgrounds;
	private int _currentBgIndex = 0;

	// Arraste um VBoxContainer do seu Panel para cá no Inspetor
	[Export] public Control ChoiceContainer; 

	public override void _Ready()
	{
		_background = GetNode<Sprite2D>("background");
		_textLabel = GetNode<RichTextLabel>("Panel/RichTextLabel");
		_textLabel.ScrollActive = false;

		// Setup dos diálogos
		_dialogueLines.Add("A wasted effort, boy!");
		_dialogueLines.Add("You lack the power to defeat me!");
		//_dialogueLines.Add("O magro é gay");
		//_dialogueLines.Add("vamos duelar");
		_dialogueLines.Add("<Duel>"); // Gatilho

		AdvanceDialogue();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept") && !_isTyping && !_waitingForChoice)
		{
			AdvanceDialogue();
		}
	}

	private void AdvanceDialogue()
	{
		if (_dialogueLines.Count > 0)
		{
			string nextLine = _dialogueLines[0];
			_dialogueLines.RemoveAt(0);

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
			GetTree().ChangeSceneToFile("res://Scenes/game.tscn");
		}
	}

	private async void StartDialogue(string text)
	{
		_isTyping = true;
		_textLabel.Text = text;
		_textLabel.VisibleCharacters = 0;

		for (int i = 0; i <= text.Length; i++)
		{
			if (!_isTyping) break;
			_textLabel.VisibleCharacters = i;
			await ToSignal(GetTree().CreateTimer(TextSpeed), "timeout");
		}

		_textLabel.VisibleCharacters = text.Length;
		_isTyping = false;
	}

	private void ShowDuelChoices()
	{
		_waitingForChoice = true;
		_textLabel.Text = ""; // Ou deixe vazio se quiser apenas as opções

		// 1. Criar Botão Aceitar
		Button acceptBtn = CreateChoiceButton("<Duel>", true);
		
		// 2. Criar Botão Recusar
		Button refuseBtn = CreateChoiceButton("<Pass>", false);

		ChoiceContainer.AddChild(acceptBtn);
		ChoiceContainer.AddChild(refuseBtn);

		// FOCO: Isso permite que você use as setas do teclado/gamepad imediatamente
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
