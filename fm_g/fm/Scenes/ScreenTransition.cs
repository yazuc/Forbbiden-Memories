public partial class ScreenTransition : CanvasLayer
{
	public static ScreenTransition Instance;

	private ColorRect _rect;

	public override void _Ready()
	{
		Instance = this;
		_rect = GetNode<ColorRect>("ColorRect");
		//_rect.Color = new Color(0, 0, 0, 0); // transparent black

	}

	public async Task FadeOut(float time)
	{
		GD.Print("tentou modular out");
		GD.Print("FadeOut start: ", _rect.Color);
		var tween = CreateTween();
		tween.TweenProperty(_rect, "modulate", new Color(1, 1, 1, 1), time);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	public async Task FadeIn(float time)
	{
		GD.Print("tentou modular in");
		GD.Print("FadeOut start: ", _rect.Color);
		var tween = CreateTween();
		tween.TweenProperty(_rect, "modulate", new Color(1, 1, 1, 0), time);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
}
