using fm;
using Godot;
using System;

public partial class DialogicSingleton : Node
{    
    public override void _Ready()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var dialogic = tree.Root.GetNode("Dialogic");

        dialogic.Connect("timeline_ended", new Callable(this, nameof(OnTimelineEnded)));
    }
    public void StartConversation(string timelinepath)
    {
        // Usamos Engine.GetMainLoop() para chegar na SceneTree 
        // mesmo se este node não estiver na árvore.
        var tree = (SceneTree)Engine.GetMainLoop();
        var dialogic = tree.Root.GetNode("Dialogic");            
        dialogic.Call("start", timelinepath);
    }
    public async void StartConversation(string timelinePath, string label)
    {
        var tree = GetTree();

        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var dialogic = tree.Root.GetNodeOrNull<Node>("Dialogic");

        if (dialogic == null)
        {
            GD.PrintErr("Dialogic node not found.");
            return;
        }

        GD.Print($"Starting: {timelinePath} / {label}");

        dialogic.CallDeferred("start", timelinePath, label);
    }
    private void OnTimelineEnded()
    {
        var world = GetNode<World>("../../World");
        int activeScene = (int)GetVariable("ActiveScene");
        CheckUnlocked();
        if(activeScene > 0)
        {
            GlobalUsings.Instance.activeScene = (ActiveScene)activeScene;
            SetVariable("ActiveScene", -1);
        }

        if(world != null && GlobalUsings.Instance.activeScene == ActiveScene.World)
        {
            GD.Print("Timeline ended, resuming world.");
            world.TickUI(true);
            world.SetProcess(true);
            world.Visible = true;
            _ = ScreenTransition.Instance.FadeIn(0.5f);
            return;
        }
        if(world != null && GlobalUsings.Instance.activeScene == ActiveScene.MainMenu)
        {
            world.Free();
            GD.Print("World freed after dialogue."); 
        }
        GD.Print("Conversation finished!");
    }

    //usage example, seta uma bool no dialogo, aonde caso tenha acessado o dialogo de simon uma vez, essa flag fica true
    //se essa fica está true, então o simon fala outra coisa, ou redireciona pra outro dialogo
    //pra nao ficar cloggado um só dialogo.
    //SetVariable("SimonOnce", true);
    public void SetVariable(string variablePath, Variant value)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var dialogic = tree.Root.GetNode("Dialogic");
        
        // Get the VAR subsystem node
        var varSubsystem = dialogic.GetNode("VAR");

        // Use the 'set_variable' method
        // variablePath is the name you gave it in the Dialogic Editor
        varSubsystem.Call("set_variable", variablePath, value);
    }

    public Variant GetVariable(string variablePath)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var dialogic = tree.Root.GetNode("Dialogic");
        
        var varSubsystem = dialogic.GetNode("VAR");

        return varSubsystem.Call("get_variable", variablePath);
    }

    //unlocks tied to story progression, forr obvious reasons.
    //a ideia ainda é alterar isso, fazer uma void function que consuma as variáveis do dialogic no meio da execução, pra evitar depender do dialogo estar finalizado
    public void CheckUnlocked()
    {
        int index = (int)GetVariable("Story.Unlock");
        GD.Print("Current Unlock Index: " + index);
        if(index > 0)
        {
            GlobalUsings.Instance.EventsToSave.Add(new Event("Unlock", index.ToString(), GlobalUsings.Instance.UserDeck));
        }
        SetVariable("Story.Unlock", 0);            
    }

    public void PausaCena()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var dialogic = tree.Root.GetNode("Dialogic");
    
        var styleSubSystem = dialogic.GetNode("Styles");
        styleSubSystem.Call("hide_layout");
        //styleSubSystem.Call("show_layout");
    }

}