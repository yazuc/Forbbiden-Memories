using SQLite;

public class DropPool
{
	[PrimaryKey]
	public int PoolId { get; set; }

	public int Duelist { get; set; }
    public string PoolType { get; set; }
    public int CardId { get; set; }
    public int CardProbability { get; set; }
}
