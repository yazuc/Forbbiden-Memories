using fm;
using Godot;
using System;

public partial class Result : Control
{
	public Control Screen {get;set;}
	public List<Control> Screens;
	private int index = 0;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Screen = GetNode<Control>("Screen");
		if(Screen != null)
			Screens = Screen.GetChildren().Cast<Control>().ToList();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _UnhandledInput(InputEvent @event)
    {
		if (@event.IsActionPressed("ui_right"))
		{
			Screens[index].Visible = false;
			index++;
			
			if(index > Screens.Count - 1)
				index = 0;

			Screens[index].Visible = true;
		}
		if (@event.IsActionPressed("ui_left"))
		{
			Screens[index].Visible = false;
			index--;
			
			if(index < 0)
				index = Screens.Count() - 1;

			Screens[index].Visible = true;
		}
    }

	public void Setup(Rank rank)
	{
		
	}
	
}
