using System.Text.Json;

namespace MementoMoriCodeRedeemer;

public sealed class PlayerRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public PlayerRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        FilePath = Path.Combine(appData, "MementoMoriCodeRedeemer", "players.json");
    }

    public string FilePath { get; }

    public IReadOnlyList<PlayerProfile> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<PlayerProfile>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidOperationException($"玩家庫讀取失敗：{ex.Message}", ex);
        }
    }

    public void Save(IEnumerable<PlayerProfile> players)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(players, JsonOptions);
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidOperationException($"玩家庫儲存失敗：{ex.Message}", ex);
        }
    }
}

public sealed class PlayerProfile
{
    public bool Locked { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
