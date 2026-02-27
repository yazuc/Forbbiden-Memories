using SQLite;

public class NpcDeck
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	public int NpcId { get; set; }   // FK para NPC

	public int CardId { get; set; }
}
