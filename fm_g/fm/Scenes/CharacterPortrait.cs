using Godot;
using System;

public partial class CharacterPortrait : Control
{
    [Export] public AnimatedSprite2D Sprite;
	
	public override void _Ready()
	{
		// Access the Dialogic Global Signal Bus
		// In Godot 4 / Dialogic 2, the Text Subsystem handles these signals
		Node dialogic = GetNode("/root/Dialogic");
		Node textSubsystem = dialogic.Get("Text").As<Node>();
        Sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Connect to 'about_to_show_text' (Starts talking)
		textSubsystem.Connect("about_to_show_text", Callable.From<Godot.Collections.Dictionary>(OnAboutToShowText));
		
		// Connect to 'text_finished' (Stops talking)
		textSubsystem.Connect("text_finished", Callable.From<Godot.Collections.Dictionary>(OnTextFinished));
        Variant eventVariant = dialogic.Get("Events");
        if (eventVariant.VariantType != Variant.Type.Nil)
        {
            Node eventBus = eventVariant.As<Node>();
            eventBus.Connect("event_started", Callable.From<Godot.Collections.Dictionary>(OnEventStarted));
            // Note: Check if your Dialogic version uses 'event_finished' or 'timeline_ended'
            if (eventBus.HasSignal("event_finished"))
                eventBus.Connect("event_finished", Callable.From<Godot.Collections.Dictionary>(OnEventFinished));
        }
	}

    private void OnEventStarted(Godot.Collections.Dictionary info)
    {
        // info["event"] contains the full Event Resource (TextEvent, CharacterEvent, etc.)
        Resource eventObj = info["event"].As<Resource>();
        
        GD.Print($"--- [TIMELINE EVENT] {eventObj.GetClass()} ---");

        // If you want to see the specific text being spoken:
        if (eventObj.HasMethod("get_text")) {
            string text = eventObj.Call("get_text").AsString();
            GD.Print($"   Content: \"{text}\"");
        }
    }
	private void OnEventFinished(Godot.Collections.Dictionary info)
	{
		// This fires when the text event is TRULY over (after the click)
		// Useful for resetting poses or ending specific 'barks'
		GD.Print("timeline ended");
	}
	private void OnAboutToShowText(Godot.Collections.Dictionary info)
	{
		// info["character"] contains the Resource of who is speaking
		// We only want to animate if THIS instance is the speaker		
		Sprite.Play();
		GD.Print("Talking started via Signal");
	}

	private void OnTextFinished(Godot.Collections.Dictionary info)
	{
		// Stop the mouth when the text box is full/done
		Sprite.Stop();
		Sprite.Frame = 0;
		GD.Print("Talking stopped via Signal");
	}

    // 1. THIS IS CALLED AUTOMATICALLY ON JOIN/UPDATE
    // portraitName comes from the string you typed in the Portrait Tab (Step 1.4)
    public void _update_portrait(Resource character, string portraitName)
    {
		GD.Print(portraitName);
        if (Sprite == null)
            Sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        // If you have different animations for different characters:
        if (Sprite.SpriteFrames.HasAnimation(portraitName))
        {
            Sprite.Animation = portraitName;
        }
        
        // Reset to your 'closed mouth' frame
        Sprite.Stop();
        Sprite.Frame = 0;
    }

	// Fix for the error: Dialogic needs to know the visual boundaries
    public Rect2 _get_covered_rect()
    {
        if (Sprite == null || Sprite.SpriteFrames == null)
            return new Rect2();

        // Get the texture of the current frame to calculate size
        var texture = Sprite.SpriteFrames.GetFrameTexture(Sprite.Animation, Sprite.Frame);
        if (texture == null) return new Rect2();

        Vector2 size = texture.GetSize();
        // Return a Rect centered on the node's position
        return new Rect2(new Vector2(-size.X / 2, -size.Y), size);
    }

	public void _highlight()
    {
        // Example: Make the character fully bright/opaque
        Modulate = new Color(1, 1, 1, 1); 
		Sprite.Play(); 
        // Or if you want them to jump forward slightly:
        // Scale = new Vector2(1.1f, 1.1f);
    }

    // CALLED WHEN ANOTHER CHARACTER STARTS TALKING
    public void _unhighlight()
    {
        // Example: Dim the character slightly (Greyish)
        Modulate = new Color(0.7f, 0.7f, 0.7f, 1);
        // Or reset scale:
        // Scale = new Vector2(1.0f, 1.0f);
    }

    // 2. CALLED WHEN TEXT STARTS TYPING
    public void _play_talk_animation()
    {
		GD.Print("at least got here?");
        // Plays your 0-2 loop
        Sprite.Play(); 
    }

    // 3. CALLED WHEN TEXT STOPS TYPING
    public void _play_think_animation()
    {
        // Back to stopped state
        Sprite.Stop();
        Sprite.Frame = 0;
    }
}