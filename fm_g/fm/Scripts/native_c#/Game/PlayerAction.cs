using Godot;

namespace fm
{
    public class PlayerAction
    {
        public PlayerActionType Type { get; set; }
        public CardUi Card { get; set; }
        public int SlotIndex { get; set; }
    }
}