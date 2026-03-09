using Godot;
using System;

public partial class ButtonText : TextureRect
{
	[Export] public SpriteFrames Sprite {get;set;}
	public string Current_Animation = "default";
	public double Fps {get;set; } = 0.0;
	public float Refresh_Rate {get;set;}
	public int Frame_index {get;set;} = 0;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Fps = Sprite.GetAnimationSpeed(Current_Animation);
		Refresh_Rate = Sprite.GetFrameDuration(Current_Animation, Frame_index);
		Texture = Sprite.GetFrameTexture("default",0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Fps += delta;
		if(Fps >= 0.3)
		{
			Frame_index = Frame_index > 2 ? 0 : Frame_index;
			Texture = Sprite.GetFrameTexture("default", Frame_index++);
			Fps = 0.0;			
		}
	}
}
