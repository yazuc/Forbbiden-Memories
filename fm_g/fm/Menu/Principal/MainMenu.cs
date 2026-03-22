using Godot;
using System;

public partial class MainMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export] public VBoxContainer InsideMenu {get;set;}
	public List<TextureButton> textureButtons{get;set;} = new List<TextureButton>();
	public int index = 0;
	public override void _Ready()
	{
		foreach(Node button in InsideMenu.GetChildren())
		{
			textureButtons.Add(button as TextureButton);
		}
		textureButtons[0].GrabFocus();
		//LightUpButton(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var focused = GetViewport().GuiGetFocusOwner();
		int index = textureButtons.IndexOf(focused as TextureButton);

		if (Input.IsActionJustReleased("ui_accept"))
		{
			GlobalUsings.Instance.FadeToBlack(0.5f, DefineRedirect(index), this);
		}
	}

	public void LightUpButton(int pos)
	{
		textureButtons[pos].WarpMouse(textureButtons[pos].Size / 2);		
	}

	public string DefineRedirect(int pos)
	{
		if(pos == 0)
			return GlobalUsings.Instance.Mundo;
		if(pos == 1)
			return GlobalUsings.Instance.Freeduel;
		if(pos == 2)	
			return GlobalUsings.Instance.Deckeditor;
		if(pos == 3)
			return GlobalUsings.Instance.Password;

		return GlobalUsings.Instance.Duelo;
	}
}
