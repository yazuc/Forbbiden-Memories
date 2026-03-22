using Godot;
using System;

public partial class Password : Control
{
	[Export] public LineEdit PasswordNumber {get;set;}
	[Export] public CardUi cardUi {get;set;}
	private TextureRect _cursor;
	private List<Label> _digits = new();
    private int _activeIndex = 0;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_cursor = GetNode<TextureRect>("HBoxContainer/VBoxContainer2/TextureRect2/Selector");
		foreach (Node child in GetNode("HBoxContainer/VBoxContainer2/TextureRect2/HBoxContainer").GetChildren())
        {
            if (child is Label label)
            {
                label.Text = "0"; // initialize
                _digits.Add(label);
            }
        }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_cursor.Rotation += (float)(delta * 2);
		_cursor.PivotOffset = _cursor.Size / 2;
		if (Input.IsActionJustPressed("ui_accept"))
		{
			var code = GetPassword();
			cardUi.DisplayByCode(code);
		}
	}

	public override async void _Input(InputEvent e)
	{
		if (e.IsActionPressed("ui_right"))
		{
			_activeIndex = (_activeIndex + 1) % _digits.Count;
			UpdateVisual();
		}
		else if (e.IsActionPressed("ui_left"))
		{
			_activeIndex = (_activeIndex - 1 + _digits.Count) % _digits.Count;
			UpdateVisual();
		}
		else if (e.IsActionPressed("ui_up"))
		{
			ChangeDigit(1);
		}
		else if (e.IsActionPressed("ui_down"))
		{
			ChangeDigit(-1);
		}
		else if (e.IsActionPressed("ui_cancel"))
		{
			if(IsNotZero())
				ResetPassword();
			else
			{
				await GlobalUsings.Instance.GoBack();				
			}

		}
	}
	private void ChangeDigit(int delta)
    {
        int value = int.Parse(_digits[_activeIndex].Text);
        value = (value + delta + 10) % 10;
        _digits[_activeIndex].Text = value.ToString();
    }

    private void UpdateVisual()
	{
		for (int i = 0; i < _digits.Count; i++)
		{
			_digits[i].AddThemeColorOverride(
				"font_color",
				i == _activeIndex ? Colors.Yellow : Colors.White
			);
		}

		MoveCursor();
	}
	Vector2 basePos = new Vector2(199, 74);
	float spacing = 90; // example

	Vector2 GetDigitPosition(int index)
	{
		return new Vector2(
			basePos.X + index * spacing,
			basePos.Y
		);
	}
	private void MoveCursor()
	{
		var target = _digits[_activeIndex];

		// Global position (safe for UI layout)
		Vector2 pos = target.GlobalPosition;
		_cursor.PivotOffset = new Vector2(0, 0);
		// Optional offset (to center or adjust)
		pos -= new Vector2(45, (target.Size.Y / 2) - 20); // e.g. below digit

		_cursor.GlobalPosition = pos; //GetDigitPosition(_activeIndex);
	}

	public void ResetPassword()
	{
		foreach(var item in _digits)
			item.Text = "0";
	}

	public bool IsNotZero()
	{
		foreach(var d in _digits)
		{
			if(d.Text != "0")
				return true;
		}
		return false;
	}


    public string GetPassword()
    {
        string result = "";
        foreach (var d in _digits)
            result += d.Text;
        return result;
    }
}
