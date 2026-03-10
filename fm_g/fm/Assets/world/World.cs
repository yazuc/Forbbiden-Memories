using Godot;
using System;

public partial class World : Node3D
{
	[Export] public Camera3D Camera;
	[Export] public Control MarkerUI;
	[Export] public Node3D Anchors;
	[Export] public AnimatedSprite3D Seletor {get;set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Seletor.Play();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
