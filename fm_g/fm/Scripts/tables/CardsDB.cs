using SQLite;
using System.Collections.Generic;
using System.IO;
using QuickType;

namespace fm
{
	public class CardDatabase
	{
		private static CardDatabase? _instance;
		private SQLiteConnection? _database;

		private CardDatabase() 
		{
			string godotPath = "res://cards.db";
			string dbPath = ProjectSettings.GlobalizePath(godotPath);
			_database = new SQLiteConnection(dbPath);			
			_database.CreateTable<Cards>();
			_database.CreateTable<NpcCharacter>();
			_database.CreateTable<NpcDropEntry>();	
		}

		public static CardDatabase Instance
		{
			get
			{
				_instance ??= new CardDatabase();
				return _instance;
			}
		}

	public void MapAtlasCoordinates()
	{
		const int atlasWidth = 2680;
		const int atlasHeight = 2934;

		const int columnsInPng = 29; // X
		const int rowsInPng = 25;    // Y

		float cardWidth = (float)atlasWidth / columnsInPng;
		float cardHeight = (float)atlasHeight / rowsInPng;

		var allCards = GetAllCards();

		foreach (var card in allCards)
		{
			// IDs começam em 1 → base zero
			int idZeroBased = card.Id - 1;

			int col = idZeroBased % columnsInPng;
			int row = idZeroBased / columnsInPng;

			card.AtlasX = col * cardWidth;
			card.AtlasY = row * cardHeight;

			_database?.Update(card);
		}

		GD.Print("Coordenadas do Atlas mapeadas com sucesso!");
	}


		public void Initialize(string dbPath)
		{
			// Connect to the database
			_database = new SQLiteConnection(dbPath);
			
			// This creates the table based on your Cards class structure 
			// if it doesn't exist already.
			_database.CreateTable<Cards>();
		}

		// Add a single card or a list of cards
		public void InsertCards(List<Cards> cards) => _database?.InsertAll(cards);

		// Fetch all cards
		public List<Cards> GetAllCards() => _database?.Table<Cards>().ToList() ?? new List<Cards>();

		// Fetch a specific card by ID (Much faster than LINQ on a List)
		public Cards? GetCardById(int id) => _database?.Table<Cards>().FirstOrDefault(c => c.Id == id);

		public void SyncJsonToDatabase(string jsonFilePath)
		{
			// 1. Check if we already have data to avoid duplicates
			var existingCount = _database.Table<Cards>().Count();
			if (existingCount > 0)
			{
				Console.WriteLine("Database already has data. Skipping import.");
				return;
			}

			// 2. Read the JSON file
			if (!File.Exists(jsonFilePath))
			{
				//Console.WriteLine($"Error: {jsonFilePath} not found.");
				return;
			}

			string json = File.ReadAllText(jsonFilePath);

			// 3. Deserialize using your QuickType generated method
			// Note: Adjust 'Cards.FromJson' if your QuickType class/method name is different
			var cardArray = Cards.FromJson(json); 

			// 4. Batch insert into SQLite (much faster than inserting one by one)
			_database?.InsertAll(cardArray);
			
			Console.WriteLine($"Successfully imported {cardArray.Length} cards into SQLite!");
		}
		public void ImportAllNpcs(string folderPath)
		{
			string globalPath = ProjectSettings.GlobalizePath(folderPath);

			if (!Directory.Exists(globalPath))
			{
				GD.PrintErr("Pasta de NPCs não encontrada.");
				return;
			}

			var files = Directory.GetFiles(globalPath, "*.json");

			foreach (var file in files)
			{
				//GD.PrintErr(file);
				ImportNpcJson(file);
			}

			GD.Print($"{files.Length} NPCs importados com sucesso!");
		}

		public void ImportNpcJson(string jsonPath)
		{
			GD.Print(jsonPath);

			if (!File.Exists(jsonPath))
			{
				GD.PrintErr("Arquivo NPC não encontrado.");
				return;
			}

			string json = File.ReadAllText(jsonPath);
			GD.Print(jsonPath);
			var npcData = JsonConvert.DeserializeObject<JsonNpcRoot>(json);

			if (npcData == null || npcData.npc == null)
			{
				GD.Print(json);
				GD.PrintErr("JSON inválido.");
				return;
			}

			string npcName = npcData.npc.nombre;
			int npcId = int.Parse(npcData.npc.id);

			// Inserir NPC corretamente
			var npc = new NpcCharacter
			{
				Id = npcId,
				Name = npcName
			};

			_database.InsertOrReplace(npc);

			// Remove drops antigos
			_database.Execute("DELETE FROM NpcDropEntry WHERE NpcId = ?", npcId);

			// Função local para inserir qualquer lista
			void InsertDrops(List<JsonCardEntry> list)
			{
				if (list == null) return;

				foreach (var drop in list)
				{
					var dropEntry = new NpcDropEntry
					{
						NpcId = npcId,
						CardId = int.Parse(drop.card),
						Probability = int.Parse(drop.prob),
						Rank = int.Parse(drop.rank)
					};

					_database.Insert(dropEntry);
				}
			}

			// Inserir todas as categorias
			InsertDrops(npcData.deck);
			InsertDrops(npcData.sapow);
			InsertDrops(npcData.bcdpt);
			InsertDrops(npcData.satec);

			GD.Print($"NPC {npcName} importado com sucesso!");
		}
	}
}
