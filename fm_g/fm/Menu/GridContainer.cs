using Godot;
using System;

public partial class GridContainer : Godot.GridContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var scene = GD.Load<PackedScene>("res://Menu/FreeDuelCell2.tscn");
		for(int i = 0; i < 40; i++)
		{
			var cell = scene.Instantiate();
			cell.Call("IrParaIndex", i);
			AddChild(cell);
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
