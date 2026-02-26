using Godot;
using System;

public partial class TBox : Node2D
{
	private RichTextLabel _textLabel;
	public float TextSpeed = 0.03f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_textLabel = GetNode<RichTextLabel>("Panel/RichTextLabel");

		StartDialogue("Seto Kaiba... I will defeat you!");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private async void StartDialogue(string text)
	{
		_textLabel.Text = text;
		_textLabel.VisibleCharacters = 0;

		for (int i = 0; i <= text.Length; i++)
		{
			_textLabel.VisibleCharacters = i;
			await ToSignal(GetTree().CreateTimer(TextSpeed), "timeout");
		}
	}
}
