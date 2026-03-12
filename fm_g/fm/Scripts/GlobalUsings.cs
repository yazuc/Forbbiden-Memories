global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Newtonsoft.Json;
global using Newtonsoft.Json.Converters;
global using System.Text.Json;
public partial class GlobalUsings : Node
{
	public static GlobalUsings Instance { get; private set; }
	public int DeckIndex = 0;
	public int currentBackGround;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
		DeckIndex = 8;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void FadeToBlack(float tempo, string path, Node obj)
	{
		var tween = CreateTween();
			tween.TweenProperty(obj,"modulate", Colors.Black, tempo);		
			tween.Finished += () =>
			{
				GetTree().ChangeSceneToFile(path);
			};
	}
}
