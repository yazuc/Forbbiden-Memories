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
}