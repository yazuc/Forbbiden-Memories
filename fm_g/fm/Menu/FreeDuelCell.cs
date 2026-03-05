using Godot;
using System;

public partial class FreeDuelCell : TextureButton
{
	[Export] public float escalaDesejada = 5.0f; 
	private float inicioX = 275.6f; 
	private float inicioY = 27.5f;
	private int tamanhoFrame = 53;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.CustomMinimumSize = new Vector2(tamanhoFrame * escalaDesejada, tamanhoFrame * escalaDesejada);
		//this.ExpandMode = ExpandModeEnum.IgnoreSize; // Permite que o TextureRect ignore o tamanho da textura original
		this.StretchMode = StretchModeEnum.Scale;    // Estica a região do Atlas para preencher o CustomMinimumSize			
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
