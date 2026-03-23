using Godot;
using System;

public partial class DialogicSingleton : Node
{
    public void StartConversation(string timelinepath)
    {
        // Usamos Engine.GetMainLoop() para chegar na SceneTree 
        // mesmo se este node não estiver na árvore.
        var tree = (SceneTree)Engine.GetMainLoop();
        var dialogic = tree.Root.GetNode("Dialogic");
                
        dialogic.Call("start", timelinepath);
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

}