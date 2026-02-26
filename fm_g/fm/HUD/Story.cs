using Godot;
using System;

public partial class Story : Node2D
{
	public float TextSpeed = 0.03f;
	private RichTextLabel _textLabel;
	private bool _isTyping = false;
	private string _currentText = "";
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var sprite = GetNode<AnimatedSprite2D>("boneco");
		var TBox = GetNode<Node2D>("TBox");
		var _sprite = GetNode<AnimatedSprite2D>("TBox/AnimatedSprite2D");
		_textLabel = GetNode<RichTextLabel>("TBox/Panel/RichTextLabel");
		_textLabel.ScrollActive = false;
		StartDialogue("A wasted effort, boy! You lack the power to defeat me!");
		_sprite.Play();
		sprite.Play();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_accept")) // Enter, por exemplo
		{
			GetTree().ChangeSceneToFile("res://Scenes/game.tscn");
		}
	}
	
		private async void StartDialogue(string text)
	{
		_isTyping = true;
		_currentText = text;

		_textLabel.Text = text;
		_textLabel.VisibleCharacters = 0;

		for (int i = 0; i <= text.Length; i++)
		{
			if (!_isTyping)
				break;

			_textLabel.VisibleCharacters = i;

			await ToSignal(GetTree().CreateTimer(TextSpeed), "timeout");
		}

		_textLabel.VisibleCharacters = text.Length;
		_isTyping = false;
	}
}
