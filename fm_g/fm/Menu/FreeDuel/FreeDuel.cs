using Godot;
using System;

public partial class FreeDuel : Control
{
	[Export] public GridContainer CharacterGrid;
	[Export] public ScrollContainer ScrollBox;
	[Export] public TextureButton Selector; // O seletor que está fora do scroll

	private int _currentIndex = 0;
	private int _columns;
	private float _inputTimer = 0.0f;
	[Export] public float InputDelay = 0.15f; // Velocidade da repetição

	private Tween _selectorTween;
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_accept"))
		{
			GlobalUsings.Instance.DeckIndex = _currentIndex;
			GlobalUsings.Instance.FadeToBlack(0.3f, GlobalUsings.Instance.Duelo, this);
			//GetTree().ChangeSceneToFile("res://Scenes/game.tscn");
		}
		if (@event.IsActionReleased("ui_cancel"))
		{
			GlobalUsings.Instance.FadeToWhite(0.3f, GetTree().CurrentScene);
			Free();
		}
	}

	public override void _Ready()
	{
		_columns = CharacterGrid.Columns;
		ScrollBox.ScrollVertical = 0; // força topo
		UpdateSelector(false); 
	}

	public override void _Process(double delta)
	{
		HandleContinuousInput((float)delta);
		
		if (CharacterGrid.GetChildCount() > 0)
		{
			var target = CharacterGrid.GetChild<Control>(_currentIndex);
			
			if (_selectorTween == null || !_selectorTween.IsRunning())
			{
				Selector.GlobalPosition = target.GlobalPosition;
				Selector.Size = target.Size;
			}

			UpdateVisibility(target);
		}
	}

	private void HandleContinuousInput(float delta)
	{
		_inputTimer -= delta;

		Vector2I dir = Vector2I.Zero;
		if (Input.IsActionPressed("ui_right")) dir.X += 1;
		else if (Input.IsActionPressed("ui_left")) dir.X -= 1;
		else if (Input.IsActionPressed("ui_down")) dir.Y += 1;
		else if (Input.IsActionPressed("ui_up")) dir.Y -= 1;

		if (dir == Vector2I.Zero)
		{
			_inputTimer = 0;
			return;
		}

		if (_inputTimer <= 0)
		{
			MoveCursor(dir);
			_inputTimer = InputDelay;
		}
	}

	private void MoveCursor(Vector2I dir)
	{
		int total = CharacterGrid.GetChildCount();
		int oldIndex = _currentIndex;

		if (dir.X != 0) _currentIndex += dir.X;
		if (dir.Y != 0) _currentIndex += (dir.Y * _columns);

		_currentIndex = Mathf.Clamp(_currentIndex, 0, total - 1);

		if (oldIndex != _currentIndex)
		{
			UpdateSelector(true);
		}
	}

	private void UpdateSelector(bool animate)
	{
		var target = CharacterGrid.GetChild<Control>(_currentIndex);
		
		if (animate)
		{
			// Mata o tween anterior se existir
			if (_selectorTween != null) _selectorTween.Kill();
			
			_selectorTween = CreateTween();
			// Anima posição e tamanho simultaneamente
			_selectorTween.SetParallel(true);
			_selectorTween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			
			_selectorTween.TweenProperty(Selector, "global_position", target.GlobalPosition, 0.1f);
			_selectorTween.TweenProperty(Selector, "size", target.Size, 0.1f);
		}

		EnsureVisible(target);
	}

	private void EnsureVisible(Control target)
	{
		float cellTop = target.Position.Y;
		float cellBottom = cellTop + target.Size.Y;
		int scrollV = ScrollBox.ScrollVertical;
		float viewHeight = ScrollBox.Size.Y;

		if (cellTop < scrollV)
			ScrollBox.ScrollVertical = (int)cellTop;
		else if (cellBottom > scrollV + viewHeight)
			ScrollBox.ScrollVertical = (int)(cellBottom - viewHeight);
	}

	private void UpdateVisibility(Control target)
	{
		Rect2 scrollRect = ScrollBox.GetGlobalRect();
		Selector.Visible = scrollRect.HasPoint(target.GlobalPosition + (target.Size / 2));
	}
}
