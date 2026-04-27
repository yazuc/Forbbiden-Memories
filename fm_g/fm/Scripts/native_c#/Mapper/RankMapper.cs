using fm;

public static class RankMapper
{
    private static readonly Dictionary<string, Func<Rank, int>> Map =
    new()
    {
        ["TurnsNumber"] = r => r.Turns,
        ["eff"] = r => r.EffectiveAttacks,
        ["defw"] = r => r.DefensiveWins,
        ["fdp"] = r => r.FacedownPlays,
        ["fus"] = r => r.AttemptToFuse,
        ["equip"] = r => r.AttemptToEquip,
        ["magic"] = r => r.SpellUsed,
        ["trap"] = r => r.TriggerTrap,
        ["Card"] = r => r.CardsUsed,
        ["Lp"] = r => r.RemainingLP
    };


    public static void ToNode(Label root, Rank rank)
    {
        foreach (var entry in Map)
        {
            var label = root.Name == entry.Key ? root : null;
            if (label == null) continue;
            GD.Print("match");
            label.Text = entry.Value(rank).ToString();
        }
    }
    
}