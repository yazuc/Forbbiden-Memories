public class JsonNpcRoot
{
	public List<JsonCardEntry> deck { get; set; }
	public List<JsonCardEntry> sapow { get; set; }
	public List<JsonCardEntry> bcdpt { get; set; }
	public List<JsonCardEntry> satec { get; set; }
	public JsonNpcInfo npc { get; set; }
}

public class JsonNpcInfo
{
	public string numero { get; set; }
	public string nombre { get; set; }
	public string id { get; set; }
}

public class JsonCardEntry
{
	public string card { get; set; }
	public string prob { get; set; }
	public string rank { get; set; }
}
