using Godot;
using System;

public partial class World : Node3D
{
	[Export] public Camera3D Camera;
	[Export] public Control MarkerUI;
	[Export] public Node3D Anchors;
	[Export] public AnimatedSprite3D Seletor {get;set;}

	public List<Marker3D> points = new();
	public int index = 0;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Seletor.Play();
		foreach(Node Mark in Anchors.GetChildren())
		{
			points.Add(Mark as Marker3D);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(points.Count == 0) return;
		changePos(index);
		
		if (Input.IsActionJustPressed("ui_up"))
		{			
			index++;
		}
		if(Input.IsActionJustPressed("ui_down"))
			index--;				
	}

	public void changePos(int pos)
	{
		Vector3 pos3D = points[pos].GlobalPosition;
		pos3D += new Vector3(0,3,0);
		Seletor.Position = pos3D;
	}
}
