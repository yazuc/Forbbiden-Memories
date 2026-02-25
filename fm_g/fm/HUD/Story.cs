using Godot;
using System;

public partial class Story : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var sprite = GetNode<AnimatedSprite2D>("boneco");
		var TBox = GetNode<Node2D>("TBox");
		var _sprite = GetNode<AnimatedSprite2D>("TBox/AnimatedSprite2D");
		_sprite.Play();
		sprite.Play();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
