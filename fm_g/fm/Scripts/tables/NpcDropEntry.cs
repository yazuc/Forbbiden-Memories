using SQLite;

public class NpcDropEntry
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	public int NpcId { get; set; }   // FK para NPC

	public int CardId { get; set; }

	public int Probability { get; set; }

	public int Rank { get; set; }
}
