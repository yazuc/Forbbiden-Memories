using SQLite;

public class NpcCharacter
{
	[PrimaryKey]
	public int Id { get; set; }

	public string Name { get; set; } = "";
	public int Win {get;set;}
	public int Loss {get;set;}
}
