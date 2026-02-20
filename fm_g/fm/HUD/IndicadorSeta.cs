using Godot;
using System;

public partial class IndicadorSeta : Node2D
{
	// Variáveis de configuração
	public Vector2 PosicaoDesejada;
	public bool OlharParaDireita = true;

	[Export] public Vector2 amplitude = new Vector2(2.5f, 0); 
	[Export] public float speed = 15.0f;

	private float _time = 0.0f;
	
	// Referências aos nodes filhos
	public ColorRect rectangle;
	public Sprite2D arrow;

	// Guardaremos as posições LOCAIS iniciais para a oscilação
	private Vector2 _initialRectPos;
	private Vector2 _initialArrowPos;

	public override void _Ready()
	{
		// 1. Referências
		rectangle = GetNode<ColorRect>("ColorRect3");
		arrow = GetNode<Sprite2D>("Arrow");

		// 2. Guardar posições locais (relativas ao pai IndicadorSeta)
		_initialRectPos = rectangle.Position;
		_initialArrowPos = arrow.Position;

		// 3. Posicionamento na tela
		if (PosicaoDesejada == Vector2.Zero)
		{
			Vector2 centro = GetViewportRect().Size / 2f;
			PosicaoDesejada = centro + new Vector2(90, -20);
		}

		GlobalPosition = PosicaoDesejada;
		
		// 4. Inverter o lado (afeta a posição local dos filhos automaticamente)
		Scale = new Vector2(OlharParaDireita ? 1f : -1f, 1f);
	}

	public override void _Process(double delta)
	{
		_time += (float)delta * speed;
		float offsetMultiplier = Mathf.Sin(_time);
		Vector2 currentOffset = amplitude * offsetMultiplier;

		// Aplicamos o movimento na Position (LOCAL)
		// Como alteramos o Scale no Ready, o movimento X vai para o lado certo sozinho!
		arrow.Position = _initialArrowPos + currentOffset;
		rectangle.Position = _initialRectPos + currentOffset;
	}
}
