using Godot;
using System;

public partial class World : Node3D
{
	[Export] public Camera3D Camera;
	[Export] public Control MarkerUI;
	[Export] public Node3D Anchors;
	[Export] public AnimatedSprite3D Seletor {get;set;}
	public PackedScene scene = GD.Load<PackedScene>("res://Menu/Story/Story_Control.tscn");
	
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
	public override async void _Process(double delta)
	{
		if(points.Count == 0) return;
		changePos(index);
		if (Input.IsActionJustPressed("ui_accept"))
		{
			// SetProcess(false);
			// Visible = false;
			// DefineBackground(points[index].Name);	
			// GlobalUsings.Instance.FadeToBlack(0.3f, GlobalUsings.Instance.Story, this);					
			await HandleAccept();			
		}
		
		if (Input.IsActionJustPressed("ui_up"))
			Move(Vector3.Forward);

		if (Input.IsActionJustPressed("ui_down"))
			Move(Vector3.Back);

		if (Input.IsActionJustPressed("ui_right"))
			Move(Vector3.Right);

		if (Input.IsActionJustPressed("ui_left"))
			Move(Vector3.Left);

		if (Input.IsActionJustPressed("ui_cancel"))
		{
			await GlobalUsings.Instance.GoBack();
			// GlobalUsings.Instance.FadeToWhite(0.3f, GetTree().CurrentScene);
			// Free();			
		}
	}

	private async Task HandleAccept()
	{
		SetProcess(false);
		//Visible = false;

		DefineBackground(points[index].Name);

		await GlobalUsings.Instance.FadeToBlack(0.5f, GlobalUsings.Instance.Story, this);
	}


	void Move(Vector3 direction)
	{
		Marker3D current = points[index];
		Marker3D best = null;

		float bestScore = -999f;

		foreach (Marker3D p in points)
		{
			if (p == current)
				continue;

			Vector3 to = (p.GlobalPosition - current.GlobalPosition).Normalized();

			float dot = direction.Dot(to);

			if (dot > 0.5f) // same direction
			{
				float distance = current.GlobalPosition.DistanceTo(p.GlobalPosition);
				float score = dot * 10f - distance;

				if (score > bestScore)
				{
					bestScore = score;
					best = p;
				}
			}
		}

		if (best != null)
		{
			index = points.IndexOf(best);
			changePos(index);
		}
	}

	public void changePos(int pos)
	{
		Vector3 pos3D = points[pos].GlobalPosition;
		pos3D += new Vector3(0,3,0);
		Seletor.Position = pos3D;
	}

	public void DefineBackground(string markerName)
	{		
		if(markerName == "DuelGround")
			GlobalUsings.Instance.currentBackGround = 1;
		if(markerName == "DarkShrine")
			GlobalUsings.Instance.currentBackGround = 34;
		if(markerName == "CardShop")
			GlobalUsings.Instance.currentBackGround = 3;
		if(markerName == "Tower")
			GlobalUsings.Instance.currentBackGround = 2;
		if(markerName == "Pharaoh")
			GlobalUsings.Instance.currentBackGround = 0;
	}
}
