using Godot;
using System;
[Tool]
public partial class GridContainer : Godot.GridContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//var scene1 = "res://Menu/FreeDuelCellHD.tscn";
		var scene1 = "res://Menu/FreeDuelCell2.tscn";
		var scene = GD.Load<PackedScene>(scene1);
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
