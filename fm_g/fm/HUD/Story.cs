using Godot;
using System;

public partial class Story : Node2D
{
	public float TextSpeed = 0.03f;
	private RichTextLabel _textLabel;
	private bool _isTyping = false;
	private string _currentText = "";
	List<string> text = new List<string>();
	private Sprite2D _background;
	[Export] public Texture2D[] Backgrounds;
	private int _currentBgIndex = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var sprite = GetNode<AnimatedSprite2D>("boneco");
		var TBox = GetNode<Node2D>("TBox");
		var _sprite = GetNode<AnimatedSprite2D>("TBox/AnimatedSprite2D");
		_background = GetNode<Sprite2D>("background");
		_textLabel = GetNode<RichTextLabel>("Panel/RichTextLabel");
		_textLabel.ScrollActive = false;
		StartDialogue("A wasted effort, boy!");
		text.Add("You lack the power to defeat me!");
		text.Add("O magro é gay");
		text.Add("vamos duelar");
		text.Add("<Duel>");
		_sprite.Play();
		sprite.Play();
		SetBackgroundByIndex(_currentBgIndex);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(Input.IsActionJustPressed("ui_right")){
			_currentBgIndex++;
			SetBackgroundByIndex(_currentBgIndex);
		}
			
		if (Input.IsActionJustPressed("ui_accept")) // Enter, por exemplo
		{
			if(text.Count() > 0)
			{
				StartDialogue(text.FirstOrDefault());
				text.RemoveAt(0);
			}else
			{
				GetTree().ChangeSceneToFile("res://Scenes/game.tscn");				
			}
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
	
	private void SetBackgroundByIndex(int index)
	{
		if (index < 0 || index >= Backgrounds.Length)
			return;

		_currentBgIndex = index;
		_background.Texture = Backgrounds[index];
	}

}
